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
using Pacman.Simulator.Ghosts;
using System.IO;

namespace MsPacmanController
{
	public unsafe partial class Form1 : Form
	{
		Stopwatch watch = new Stopwatch();
		Queue<long> runningAvg = new Queue<long>();
		int msPerFrame = 0;
		GameState gs;

		private static Bitmap sprites;
		Bitmap bitmap = new Bitmap(224,288);		
		int[] colorValues = new int[224*288];		
		const int width = 224, height = 288;
		const int size = width * height;
		const int red = 0xff0000, blue = 0x00ffff, pink = 0xffb6ff, brown = 0xffb655;
		const int edibleBlue = 0x2424ff, edibleWhite = 0xdadaff;
		const int pacman = 0xffff00;
		const int black = 0x000000;
		Visualizer v;		
		string debug = "";
		BasePacman controller = null;
		Direction direction = Direction.None;
		Direction stall = Direction.None;

		private static Bitmap scoreShot;
		private static Graphics scoreGfx;

		private static int[] spritesRgbValues;
		private static int spritesRgbWidth;
		private static int spritesRgbHeight;
		private static int getSpriteColor(int x, int y) {
			return spritesRgbValues[y * spritesRgbWidth + x];
		}
				
		private unsafe void aiRunner() {
			// init game state
			gs = new GameState();
			gs.AutomaticLevelChange = false;
			gs.Replay = true;
			gs.StartPlay();
			if( v != null ) {
				v.SetGameState(gs);
			}
			if( selectedAI != null && selectedAI.Type != null ) {
				controller = (BasePacman)selectedAI.Type.GetConstructor(new Type[] { }).Invoke(new object[] { });
			} else {
				controller = null;
			}
			initializePosInfos();
			scores = new List<int>();
			// load resources
			System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.Stream file = thisExe.GetManifestResourceStream("MsPacmanController.Resources.screenplayer.png");
			sprites = (Bitmap)Bitmap.FromStream(file);
			unsafe {
				spritesRgbWidth = sprites.Width;
				spritesRgbHeight = sprites.Height;
				BitmapData spritesBitmapData = sprites.LockBits(new Rectangle(0, 0, sprites.Width, sprites.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
				IntPtr spritesPtr = spritesBitmapData.Scan0;
				spritesRgbValues = new int[sprites.Width * sprites.Height];
				Marshal.Copy(spritesPtr, spritesRgbValues, 0, sprites.Width * sprites.Height);
				sprites.UnlockBits(spritesBitmapData);
				for( int i = 0; i < spritesRgbValues.Length; i++ ) {
					spritesRgbValues[i] = (int)(((uint)spritesRgbValues[i]) - 0xFF000000);
				}
			}
			// init for scoreshot
			scoreShot = new Bitmap(48, 7);
			scoreGfx = Graphics.FromImage((Image)scoreShot);
			spriteSums = new int[10];
			for( int i = 0; i < 10; i++ ) {
				int sum = 0;
				for( int y = 0; y < 7; y++ ) {
					sum += (getSpriteColor(6 + i * 8, 98 + y) << y);
				}
				spriteSums[i] = sum;
				//Console.WriteLine(i + " = " + sum);
			}
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
					
					Marshal.Copy(ptr, colorValues, 0, size);

					// before we eliminate the maze :)
					currentMaze = findMaze();

					// subtract red maze					
					switch( currentMaze ) {
						case Maze.Red: subtractMaze(mazeRedEmpty); break;
						case Maze.LightBlue: subtractMaze(mazeLightblueEmpty); break;
						case Maze.Brown: subtractMaze(mazeBrownEmpty); break;
						case Maze.DarkBlue: throw new ApplicationException("DarkBlue maze not implemented (do it yourself you lazy fuck ;) )");
						default: subtractMaze(mazeRedEmpty); break; // to eliminate alpha values
					}

					// Copy the RGB values back to the bitmap
					//Marshal.Copy(colorValues, 0, ptr, size);

					bitmap.UnlockBits(bitmapData);
					NativeMethods.DeleteObject(ptr);
				}
										
				// remove alpha
				/*for( int i = 0; i < colorValues.Length; i++ ) {
					colorValues[i] = (int)(((uint)colorValues[i]) - 0xFF000000);
				}*/
								
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

				// find scores				
				int newScore = findScore();
				if( newScore % 10 != 0 ) {
					newScore = -1;
				}
				if( newScore < score ){
					scoreToAddToAvg = score;
					if( score > (int)settings["Highscore"] ) {
						settings["Highscore"] = score;
						settings.Save();
					}
				}
				if( scoreToAddToAvg != -1 && score < 400 && score > 0 ) {
					scores.Add(scoreToAddToAvg);
					if( controller != null ) {
						using( StreamWriter sw = new StreamWriter(File.Open("scores.txt", FileMode.Append)) ) {
							sw.WriteLine(controller.Name + "\t\t" + scoreToAddToAvg);
						}
					}
					scoreToAddToAvg = -1;					
				}
				score = newScore;
				if( score != -1 ) {
					determineState();
					if( currentState == State.StartScreen ) {
						GC.Collect();
						Comm.AddCredit();
						Comm.StartGame();
					}
					// play
					play();
					// update entered/not entered
					foreach( Ghost ghost in gs.Ghosts ) {
						if( ghost.Node.X >= 10 && ghost.Node.X <= 17 &&
							ghost.Node.Y >= 12 && ghost.Node.Y <= 16 ) {
							ghost.SetEntered(false);
						} else if( !ghost.Node.Walkable ) {
							ghost.SetEntered(false);
						} else {
							ghost.SetEntered(true);
						}
					}
					if( controller != null ) {
						// control
						direction = controller.Think(gs);
						//Console.WriteLine(direction);
						if( direction == Direction.Stall ) {
							if( stall == Direction.None ) {
								direction = gs.Pacman.InverseDirection(gs.Pacman.Direction);
								stall = direction;
							} else {
								direction = gs.Pacman.InverseDirection(stall);
								stall = direction;
							}
						} else {
							stall = Direction.None;
						}
						Comm.SendKey(direction);
					}
				} else {
					GC.Collect();
					currentState = State.BetweenLevels;
				}

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

		private void play() {
			updatePills();
			findAll();
		}

		private bool pacmanFound, redFound, blueFound, pinkFound, brownFound, allGhostsFound;
		private Point?[] ediblePositions;
		private void findAll() {
			pacmanFound = false;
			redFound = false;
			blueFound = false;
			pinkFound = false;
			brownFound = false;
			allGhostsFound = false;
			ediblePositions = new Point?[4];
			for( int i = 24 * width; i < colorValues.Length - 20 * width; i++ ) {
				switch( colorValues[i] ) {
					case pacman: if( !pacmanFound ) { findPacman(i); } break;
					case red: if( !redFound ) { findGhost(i, red); } break;
					case blue: if( !blueFound ) { findGhost(i, blue); } break;
					case pink: if( !pinkFound ) { findGhost(i, pink); } break;
					case brown: if( !brownFound ) { findGhost(i, brown); } break;
					case edibleBlue: if( !allGhostsFound ) { findGhost(i, edibleBlue); } break;
					case edibleWhite: if( !allGhostsFound ) { findGhost(i, edibleWhite); } break;
				}
			}
			if( !allGhostsFound ) {
				int ediblePos = 0;
				if( !redFound ) {
					foundEdible(gs.Red, ediblePositions[ediblePos++]);
				}
				if( !blueFound ) {
					foundEdible(gs.Blue, ediblePositions[ediblePos++]);
				}
				if( !pinkFound ) {
					foundEdible(gs.Pink, ediblePositions[ediblePos++]);
				}
				if( !brownFound ) {
					foundEdible(gs.Brown, ediblePositions[ediblePos++]);
				}
			}
		}

		private void findPacman(int i) {
			try {
				// up
				if( colorValues[(i - 11) + 3 * width] == edibleBlue &&
					colorValues[(i - 10) + 3 * width] == red &&
					colorValues[(i - 4) + 2 * width] == black &&
					colorValues[i - 5] == pacman ) {
					gs.Pacman.SetPosition((i % width) - 5, i / width - 20, Direction.Up);
					pacmanFound = true;
					return;
				}
			} catch {
				//bitmap.Save("Error_Up_" + (i % width) + "_" + (i / width) + ".png");
			}
			// right
			try{
				if( colorValues[(i - 6) - 10 * width] == red &&
					colorValues[(i - 6) - 11 * width] == edibleBlue &&
					colorValues[(i - 5) - 4 * width] == black &&
					colorValues[i - 4] == pacman ) {
					gs.Pacman.SetPosition((i % width), i / width - 24, Direction.Right);
					pacmanFound = true;
					return;
				}
			} catch {
				//bitmap.Save("Error_Right_" + (i % width) + "_" + (i / width) + ".png");
			}
			// down
			try {
				if( colorValues[(i + 4) - 3 * width] == black &&
					colorValues[(i + 10) - 4 * width] == red &&
					colorValues[(i + 10) - 5 * width] == edibleBlue &&
					colorValues[i + 12] == pacman ) {
					gs.Pacman.SetPosition((i % width) + 8, i / width - 20, Direction.Down);
					pacmanFound = true;
					return;
				}
			} catch {
				//bitmap.Save("Error_Down_" + (i % width) + "_" + (i / width) + ".png");
			}
			// left
			try {
				if( colorValues[(i + 2) - 10 * width] == red &&
					colorValues[(i + 2) - 11 * width] == edibleBlue ) {
					gs.Pacman.SetPosition((i % width), i / width - 24, Direction.Left);
				}
			} catch {
				//bitmap.Save("Error_Left_" + (i % width) + "_" + (i / width) + ".png");
			}
		}

		private void findGhost(int i, int color) {
			if( colorValues[i + 3] == color &&
				colorValues[(i - 5) + 12 * width] == color &&
				colorValues[i + 8 + 12 * width] == color ) {
				switch( color ) {
					case red: redFound = true; foundGhost(i, gs.Red); break;
					case blue: blueFound = true; foundGhost(i, gs.Blue); break;
					case pink: pinkFound = true; foundGhost(i, gs.Pink); break;
					case brown: brownFound = true; foundGhost(i, gs.Brown); break;
					case edibleBlue:
					case edibleWhite:
						for( int e = 0; e < 4; e++ ) {
							if( ediblePositions[e] == null ) {
								ediblePositions[e] = iToGhostPos(i);
								break;
							}
						}
						break;
				}
			}
		}

		private void foundGhost(int i, Ghost ghost) {
			Direction ghostDirection = Direction.Left;
			if( colorValues[(i - 1) + width] == edibleBlue ) {
				ghostDirection = Direction.Up;
			} else if( colorValues[i + 5 * width] == edibleBlue ) {
				ghostDirection = Direction.Right;
			} else if( colorValues[(i - 1) + 7 * width] == edibleBlue ) {
				ghostDirection = Direction.Down;
			}
			Point p = iToGhostPos(i);
			ghost.SetPosition(p.X, p.Y, ghostDirection);
			ghost.Chasing = true;
			if( redFound && blueFound && pinkFound && brownFound ) {
				allGhostsFound = true;
			}
		}

		private void foundEdible(Ghost ghost, Point? pos) {
			if( pos == null ) return;
			ghost.Chasing = false;
			ghost.RemainingFlee = 1000;
			ghost.SetPosition(pos.Value.X, pos.Value.Y);
		}

		private Point iToGhostPos(int i) {
			return new Point((i % width) + 1, i / width - 16);
		}

		// all the old stuff (that actually works)
		private class PowerPillWatch
		{
			public Node Node;
			public int EmptyCount = 1;
			public PowerPillWatch(Node n) {
				this.Node = n;
			}
		}
		private List<PowerPillWatch> powerPillNodes = new List<PowerPillWatch>();
		private void updatePills() {
			foreach( Node n in gs.Map.PillNodes ) {
				if( n.Type == Node.NodeType.Pill ) {
					if( getColor(n.CenterX, n.CenterY + 23) == 0 ) {
						gs.Map.PillsLeft--;
						n.Type = Node.NodeType.None;
					}
				} else if( n.Type == Node.NodeType.PowerPill ) {
					bool found = false;
					foreach( PowerPillWatch ppw in powerPillNodes ) {
						if( ppw.Node == n ) {
							if( getColor(n.CenterX, n.CenterY + 23) == 0 ) {
								ppw.EmptyCount++;
								if( ppw.EmptyCount > 30 ) {
									gs.Map.PillsLeft--;
									n.Type = Node.NodeType.None;
									powerPillNodes.Remove(ppw);
								}
							} else {
								ppw.EmptyCount = 0;
							}
							found = true;
							break;
						}
					}
					if( !found && getColor(n.CenterX, n.CenterY + 23) == 0 ) {
						powerPillNodes.Add(new PowerPillWatch(n));
					}
				}
			}
		}
		
		// scores
		int score = -1;
		List<int> scores = new List<int>();
		int scoreToAddToAvg = -1;
		int[] spriteSums;
		private int findScore() {
			int score = 0;
			bool foundScore = false;
			for( int i = 0; i < 6; i++ ) {
				int sum = 0;
				for( int y = 0; y < 7; y++ ) {
					int color = colorValues[(y + 9) * width + 6 + 8 + i * 8];
					sum += (color << y);
				}
				for( int n = 0; n < 10; n++ ) {
					if( sum == spriteSums[n] ) {
						foundScore = true;
						score += n * (int)Math.Pow(10, (5 - i));
						break;
					}
				}
			}
			if( !foundScore ) {
				return -1;
			}
			return score;
		}

		private enum State { StartScreen, WaitingToStart, BetweenLevels, Playing, Unknown };
		private static State currentState = State.Unknown;
		private static Maze currentMaze = Maze.None;
		private static int currentPillColor = 0;
		private static int livesLeft = 0;

		private void determineState() {
			// start screen			
			if( getColor(18, 284) == 0xDADAFF ) {
				currentState = State.StartScreen;
				livesLeft = 0;
				currentMaze = Maze.None;
				return;
			}
			// find lives
			livesLeft = gs.Pacman.Lives = findLives();
			// waiting to start
			if( findReady() ) {
				currentState = State.WaitingToStart;
				currentPillColor = findPillColor();
				gs.LoadMaze(currentMaze);
				initializePosInfos();				
				return;
			}			
			// fix: pills registered as eaten without this actually being true
			if( gs.Map.PillsLeft <= 0 || currentMaze == Maze.None || gs.Map.Maze != currentMaze ) {				
				if( currentMaze != Maze.None ) {
					gs.LoadMaze(currentMaze);
				}
			}
			// playing
			// todo
			// set unknown
			currentState = State.Playing;
		}

		private PosInfo[] entityPosInfos = null;
		private PosInfo lastPacman = new PosInfo();
		private PosInfo lastBlue = new PosInfo();
		private PosInfo lastBrown = new PosInfo();
		private PosInfo lastPink = new PosInfo();
		private PosInfo lastRed = new PosInfo();
		private readonly Point safePosition = new Point(110, 115);
		private const int trackingSize = 8;

		private void initializePosInfos() {
			lastPacman = new PosInfo(104, 204, Comm.COLOR_PACMAN, gs.Pacman);
			lastBlue = new PosInfo(89, 133, Comm.COLOR_BLUE, gs.Blue);
			lastBrown = new PosInfo(121, 133, Comm.COLOR_BROWN, gs.Brown);
			lastPink = new PosInfo(105, 133, Comm.COLOR_PINK, gs.Pink);
			lastRed = new PosInfo(105, 109, Comm.COLOR_RED, gs.Red);
			entityPosInfos = new PosInfo[5];
			entityPosInfos[0] = lastPacman;
			entityPosInfos[1] = lastBlue;
			entityPosInfos[2] = lastBrown;
			entityPosInfos[3] = lastPink;
			entityPosInfos[4] = lastRed;
		}

		private void subtractMaze(int[] maze) {
			for( int i = 0; i < colorValues.Length; i++ ) {
				colorValues[i] -= maze[i];
			}
		}

		private bool findReady() {
			for( int y = 0; y < 7; y++ ) {
				for( int x = 0; x < 7; x++ ) {
					int curColor = getColor(x + 89, y + 160);
					int curSpriteColor = getSpriteColor(x, y + 84);
					if( curColor != curSpriteColor ) {
						return false;
					}
				}
			}
			return true;
		}

		private int findLives() {
			for( int i = 4; i >= 0; i-- ) {
				if( getColor(19 + i * 16, 281) == Comm.COLOR_PACMAN ) {
					return i + 1;
				}
			}
			return 0;
		}

		private Maze findMaze() {
			int mazeColor = (int)((uint)getColor(5, 25) - 0xFF000000);
			switch( mazeColor ) {
				case 0xFFB6AA: return Maze.Red;
				case 0x48B6FF: return Maze.LightBlue;
				case 0xDA9155: return Maze.Brown;
				case 0x2424FF: return Maze.DarkBlue; // not sure about color
				default: return Maze.None;
			}
		}

		private int findPillColor() {
			switch( currentMaze ) {
				case Maze.Red: return 0xDADAFF;
				case Maze.LightBlue: return 0xFCFC00;
				case Maze.Brown: return 0xFF0000;
				case Maze.DarkBlue: return 0xDADAFF;
				default: return 0x000000;
			}
		}

		private int getColor(int x, int y) {
			return colorValues[y * width + x];
		}

		private class PosInfo
		{
			public Point LastPosition = new Point(-1, -1);
			public bool Lost = true;
			public readonly int Color = -1;
			public readonly Entity Entity = null;
			public bool Edible = false;
			public bool Blinking = false;

			public PosInfo() { }

			public PosInfo(int x, int y, int color, Entity entity)
				: this(x, y) {
				this.Color = color;
				this.Entity = entity;
			}

			public PosInfo(int x, int y) {
				LastPosition = new Point(x, y);
				if( x != -1 ) {
					Lost = false;
				}
			}
		}
	}
}
