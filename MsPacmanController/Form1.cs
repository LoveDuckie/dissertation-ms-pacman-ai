using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Threading;
using Pacman.Simulator;
using MsPacmanController.Properties;

namespace MsPacmanController
{
	public partial class Form1 : Form
	{
		// assembly
		Assembly assembly = Assembly.GetExecutingAssembly();
		Settings settings = Properties.Settings.Default;
		// icons
		Bitmap iconSuccess;
		Bitmap iconFailure;
		Bitmap iconUnknown;
		Bitmap line;
		// mazes
		Bitmap bitmapMazeRedEmpty;
		int[] mazeRedEmpty = new int[size];
		Bitmap bitmapMazeLightblueEmpty;
		int[] mazeLightblueEmpty = new int[size];
		Bitmap bitmapMazeBrownEmpty;
		int[] mazeBrownEmpty = new int[size];
		// lists
		List<PictureBox> pictureInitBoxes = new List<PictureBox>();
		// Microsoft Ms. Pacman process
		Process msPacmanProcess;
		// game position
		Rectangle gameRect;
		// running pacman
		volatile bool runningPacman = false;
		// ignore flash messing with unhandled exceptions
		[DllImport("kernel32.dll")]
		static extern IntPtr SetUnhandledExceptionFilter(IntPtr lpFilter);
		// update ui
		delegate void UpdateDelegate();
		UpdateDelegate updateMethod;
		// run ai thread
		Thread aiThread;
		Thread vThread;

		public Form1() {
			IntPtr SaveFilter = SetUnhandledExceptionFilter(IntPtr.Zero);
			InitializeComponent();
			SetUnhandledExceptionFilter(SaveFilter);			
			// set position
			this.Left = 100;
			this.Top = 100;
			// assign update delegate
			updateMethod = update;
			// setup icons			
			iconSuccess = loadRes("success.png");
			iconFailure = loadRes("failure.png");
			iconUnknown = loadRes("unknown.png");
			line = loadRes("line.bmp");
			pictureLine.Image = line;
			// setup mazes
			bitmapMazeRedEmpty = loadRes("red_maze_empty.png");
			getScan(bitmapMazeRedEmpty, mazeRedEmpty);
			bitmapMazeLightblueEmpty = loadRes("lightblue_maze_empty.png");
			getScan(bitmapMazeLightblueEmpty, mazeLightblueEmpty);
			bitmapMazeBrownEmpty = loadRes("brown_maze_empty.png");
			getScan(bitmapMazeBrownEmpty, mazeBrownEmpty);
			// setup picture init boxes
			pictureInitBoxes.AddMultiple(pictureInit32Bit,pictureInitFoundGame,pictureInitLocated,pictureFrameRate);
			// reset
			reset();
			// catch close event
			FormClosing += new FormClosingEventHandler(ClosingEvent);
			// checkboxes
			checkBoxAutoPlay.Checked = (bool)settings["Autoplay"];
			checkBoxVisualizer.Checked = (bool)settings["Visualizer"];
			// error check highscore
			if( ((int)settings["Highscore"]) % 10 != 0 ) {
				settings["Highscore"] = 0;
				settings.Save();
			}
			// ai selection
			comboBoxAI.AutoCompleteSource = AutoCompleteSource.ListItems;
			comboBoxAI.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
			comboBoxAI.Items.Add(new AIItem("None",null));						
			comboBoxAI.Items.Add(new AIItem("RandomPac (Orig)", typeof(Pacman.Implementations.RandomPac)));
			comboBoxAI.Items.Add(new AIItem("SmartPac (Orig)", typeof(Pacman.Implementations.SmartPac)));
			comboBoxAI.Items.Add(new AIItem("SmartDijkstraPac (Orig)", typeof(Pacman.Implementations.SmartDijkstraPac)));

			foreach( string s in Directory.GetFiles(@"../Controllers/") ) {
				if( Path.GetExtension(s) == ".dll" ) {
					string path = Path.GetFullPath(s);
					Assembly dll = Assembly.LoadFile(path);
					Type[] types = dll.GetTypes();
					foreach( Type t in types ) {
						if( t.BaseType == typeof(BasePacman) ) {
							comboBoxAI.Items.Add(new AIItem(t.ToString(), t));
						}
					}
				}
			}

			foreach( object o in comboBoxAI.Items ) {
				AIItem i = o as AIItem;
				if( i.ToString() == (string)settings["SelectedAI"] ) {
					comboBoxAI.SelectedItem = i;
					break;
				}
			}
		}

		public class AIItem
		{
			private string text;
			public readonly Type Type;

			public AIItem(string text, Type type) {
				this.text = text;
				this.Type = type;
			}

			public override string ToString() {
				return text;
			}
		}

		void ClosingEvent(object sender, FormClosingEventArgs e) {
			if( vThread != null ) {
				vThread.Abort();
			}
		}

		private Bitmap loadRes(string name) {
			return (Bitmap)Bitmap.FromStream(assembly.GetManifestResourceStream("MsPacmanController.Images."+name));
		}

		private void getScan(Bitmap bitmap, int[] target) {
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
			IntPtr ptr = bitmapData.Scan0;
			Marshal.Copy(ptr, target, 0, size);
			bitmap.UnlockBits(bitmapData);
		}

		private void reset() {
			msPacmanProcess = null;
			labelError.Visible = false;
			labelErrorText.Visible = false;
			labelInitialized.Visible = false;
			foreach( PictureBox picture in pictureInitBoxes ) {
				picture.Image = iconUnknown;
			}
			buttonStart.Enabled = false;
			buttonStart.Text = "Play Ms. Pacman";
		}

		private void success(PictureBox picture) {
			picture.Image = iconSuccess;
		}

		private void failure(PictureBox picture, string error) {
			picture.Image = iconFailure;
			labelError.Visible = true;
			labelErrorText.Visible = true;
			labelErrorText.Text = error;
		}

		private void buttonInit_Click(object sender, EventArgs e) {
			reset();
			
			// 32 bit color
			if( Screen.PrimaryScreen.BitsPerPixel != 32 ) {
				failure(pictureInit32Bit, "The screen must be set to 32 bit colors");
				return;
			} else {
				success(pictureInit32Bit);
			}

			// ms. pacman is loaded
			// find process
			Process[] processes = Process.GetProcesses();
			foreach( Process p in processes ) {
				if( p.MainWindowTitle.StartsWith("Ms. Pac-Man") ) {
				//if( p.MainWindowTitle.StartsWith("WebPacMan") ) {
					msPacmanProcess = p;
				}
			}
			// set as foreground window
			try {
				Comm.ShowWindow(msPacmanProcess.MainWindowHandle, (int)WindowShowStyle.Restore);				
				Comm.SetWindowPos(msPacmanProcess.MainWindowHandle, (IntPtr)Comm.HWND_TOP, this.Left + this.Width, this.Top, 0, 0, 0);
				success(pictureInitFoundGame);
			} catch {
				failure(pictureInitFoundGame, "Please make sure Microsoft Ms. Pacman is running");
				return;
			}

			// entire window: 234, 344
			// working area: 224, 288
			// locate game area
			try{
				WINDOWINFO info = Comm.GetWindowInfoEasy(msPacmanProcess.MainWindowHandle);
				gameRect = new Rectangle(info.rcClient.Left, info.rcClient.Top, info.rcClient.Width, info.rcClient.Height);
				success(pictureInitLocated);
			} catch( Exception ex ){
				failure(pictureInitLocated, ex.Message);
			}

			// start window
			if( checkBoxVisualizer.Checked && vThread == null ) { // seeing problems? remember to debug with this first!
				vThread = new System.Threading.Thread(delegate() {
					v = new Visualizer();					
					v.Location = new Point(this.Right, this.Top + this.Height);
					v.StartPosition = FormStartPosition.Manual;
					try {
						System.Windows.Forms.Application.Run(v);
					} catch { }
				});
				vThread.Start();
				System.Threading.Thread.Sleep(2000);
			}
			
			// success
			buttonStart.Enabled = true;
			labelInitialized.Visible = true;

			// autoplay
			if( checkBoxAutoPlay.Checked ) {
				buttonStart_Click(null, null);
			} else {
				this.BringToFront();
			}
		}

		private void buttonStart_Click(object sender, EventArgs e) {
			Comm.SetForegroundWindow(msPacmanProcess.MainWindowHandle);
			aiThread = new Thread(aiRunner);
			aiThread.Start();
			buttonStart.Enabled = false;
			buttonStart.Text = "Playing ...";
		}

		private void update() {
			if( !runningPacman ) {				
				reset();
				return;
			}
			labelFrameRate.Text = "Milliseconds pr. frame: " + msPerFrame;
			if( msPerFrame < 8 ) {
				pictureFrameRate.Image = iconSuccess;
			} else if( msPerFrame < 16 ) {
				pictureFrameRate.Image = iconUnknown;
			} else {
				pictureFrameRate.Image = iconFailure;
			}
			// game info
			labelScore.Text = score + "";
			labelHighscore.Text = score > (int)settings["Highscore"] ? score + "" : settings["Highscore"] + "";
			if( scores.Count > 0 ) {
				labelAvgScore.Text = ((int)scores.Average()) + "";
			}

			labelState.Text = currentState.ToString();
			labelLives.Text = livesLeft + "";
			labelMaze.Text = currentMaze + "";
			labelPills.Text = gs.Map.PillsLeft + "";
			// pacman
			labelFoundDirection.Text = gs.Pacman.Direction.ToString();
			labelFoundPosition.Text = gs.Pacman.Node.X + ", " + gs.Pacman.Node.Y + "  (" + gs.Pacman.Node.CenterX + ", " + (gs.Pacman.Node.CenterY+24) + ")";
			labelLeft.Text = gs.Pacman.Node.GetNode(Direction.Left).Type.ToString();
			labelRight.Text = gs.Pacman.Node.GetNode(Direction.Right).Type.ToString();
			labelUp.Text = gs.Pacman.Node.GetNode(Direction.Up).Type.ToString();
			labelDown.Text = gs.Pacman.Node.GetNode(Direction.Down).Type.ToString();
			// ai
			if( controller == null ) {
				labelAI.Text = "None";
			} else {
				labelAI.Text = controller.Name;
			}
			labelDirection.Text = direction.ToString();
			// debug
			labelDebug.Text = debug;
		}

		private void checkBoxAutoPlay_CheckedChanged(object sender, EventArgs e) {
			settings["Autoplay"] = checkBoxAutoPlay.Checked;
			settings.Save();
		}

		private void checkBoxVisualizer_CheckedChanged(object sender, EventArgs e) {
			settings["Visualizer"] = checkBoxVisualizer.Checked;
			settings.Save();
		}

		private void pictureBox1_Click(object sender, EventArgs e) {
			settings["Highscore"] = 0;
			settings.Save();
			labelHighscore.Text = score > (int)settings["Highscore"] ? score + "" : settings["Highscore"] + "";
		}

		private AIItem selectedAI = null;
		private void comboBoxAI_SelectedIndexChanged(object sender, EventArgs e) {
			selectedAI = (AIItem)comboBoxAI.SelectedItem;
			settings["SelectedAI"] = comboBoxAI.SelectedItem.ToString();
			settings.Save();
		}

	}
}
