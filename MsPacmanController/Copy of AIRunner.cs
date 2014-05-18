using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Pacman.Simulator;
using Pacman.Implementations;

namespace Test1
{
	public unsafe partial class Form1 : Form
	{
		Stopwatch watch = new Stopwatch();
		Queue<long> runningAvg = new Queue<long>();
		int msPerFrame = 0;
		GameState gs;

		Bitmap bitmap = new Bitmap(224,288);		
		int[] colorValues = new int[224*288];		
		const int width = 224, height = 288;
		const int size = width * height;
		const int red = 0xff0000, blue = 0x00ffff, purple = 0xffb6ff, brown = 0xffb655;
		const int edibleBlue = 0x2424ff, edibleWhite = 0xdadaff;
		const int pacman = 0xffff00;
				
		private unsafe void aiRunner() {
			// init game state
			gs = new GameState();
			gs.AutomaticLevelChange = false;
			gs.Replay = true;
			gs.StartPlay();
			BasePacman controller = new SmartDijkstraPac();
			//
			runningPacman = true;			
			while( true ) {
				watch.Start();

				// get window info
				WINDOWINFO info = Comm.GetWindowInfoEasy(msPacmanProcess.MainWindowHandle);
				if( info.dwWindowStatus == 0 ) {
					break;
				}

				// capture frame
				bitmap = (Bitmap)NativeMethods.GetDesktopBitmap(info.rcClient.Left, info.rcClient.Top, width, height, bitmap);
				unsafe {
					BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
					IntPtr ptr = bitmapData.Scan0;
					//Marshal.Copy(ptr, colorValues, 0, width * height);

					Marshal.Copy(ptr, colorValues, 0, size);

					// subtract red maze
					for( int i = 0; i < colorValues.Length; i++ ){
						colorValues[i] -= mazeRedEmpty[i];
					}
					// run test
					test();

					// Copy the RGB values back to the bitmap
					Marshal.Copy(colorValues, 0, ptr, size);

					bitmap.UnlockBits(bitmapData);
				}				

				// update framerate
				if( runningAvg.Count > 8 ) {
					runningAvg.Dequeue();
				}
				if( runningAvg.Count != 0 ) {
					msPerFrame = 0;
					foreach( long ms in runningAvg ) {
						msPerFrame += (int)ms;
					}
					msPerFrame /= runningAvg.Count;
				}
				// test
				Comm.SendKey(controller.Think(gs));
				// update status
				this.BeginInvoke(updateMethod);
				watch.Stop();
				// update running avg
				runningAvg.Enqueue(watch.ElapsedMilliseconds);
				// reset
				watch.Reset();
				Thread.Sleep(30);
			}
			runningPacman = false;
			this.BeginInvoke(updateMethod);
		}

		private bool redFound, blueFound, purpleFound, brownFound, pacmanFound;
		private void test() {
			redFound = false;
			blueFound = false;
			purpleFound = false;
			brownFound = false;
			pacmanFound = false;
			for( int i = 30 * width; i < colorValues.Length - 30 * width; i+=3 ) {
				switch( colorValues[i] ) {
					case red: if( !redFound ) { locate(i, red); } break;
					case blue: if( !blueFound ) { locate(i, blue); } break;
					case purple: if( !purpleFound ) { locate(i, purple); } break;
					case brown: if( !brownFound ) { locate(i, brown); } break;
					case edibleBlue: locate(i, edibleBlue); break;
					case edibleWhite: locate(i, edibleWhite); break;
					case pacman: if( !pacmanFound ) { locate(i, pacman); } break;
				}
			}
		}

		private void locate(int i, int color) {
			int xTotal = 0, yTotal = 0;
			xTotal += locateDir(i, color, 1);
			xTotal += locateDir(i, color, -1);
			yTotal += locateDir(i, color, width);
			yTotal += locateDir(i, color, -width);
			if( xTotal + yTotal > 12 && xTotal > 2 && yTotal > 2 && ( xTotal > 6 || yTotal > 6 ) ) {
				colorValues[i] = 0xffffff;

				colorValues[i+1] = 0xffffff;
				colorValues[i-1] = 0xffffff;
				colorValues[i+width] = 0xffffff;
				colorValues[i-width] = 0xffffff;

				switch( color ) {
					case red: redFound = true; gs.Red.SetPosition(i % width, i / width + 27); break;
					case blue: blueFound = true; gs.Blue.SetPosition(i % width, i / width + 27); break;
					case purple: purpleFound = true; gs.Pink.SetPosition(i % width, i / width + 27); break;
					case brown: brownFound = true; gs.Brown.SetPosition(i % width, i / width + 27); break;
					case pacman: pacmanFound = true; gs.Pacman.SetPosition(i % width, i / width + 27); break;
				}
			}
		}

		private int locateDir(int i, int color, int dir) {
			int total = 0;
			i += dir;
			while( colorValues[i] == color ){
				total++;
				i += dir;
			}
			return total;
		}
	}
}
