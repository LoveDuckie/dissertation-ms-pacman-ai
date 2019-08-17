using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Pacman.Simulator;
using System.Diagnostics;
using System.Threading;
using System.Reflection;

namespace PacmanAI
{
	class Program
	{
		private static int gamesPlayed = 0;
		private static int totalScore = 0;
		private static GameState gs;
		private static int gamesToPlay = 100;
		private static long longestGame = 0;

        #region Members
        private static int highestScore = 0;
		private static int lowestScore = int.MaxValue;
        private static int avgScore = 0;

        private static int maxPillsEaten = 0;
        private static int minPillsEaten = int.MaxValue;
        private static int avgPillsEaten = 0;
        private static int pillsEatenTotal = 0;
        
        private static int maxGhostsEaten = 0;
        private static int minGhostsEaten = int.MaxValue;
        private static int avgGhostsEaten = 0;
        private static int totalGhostsEaten = 0;
        #endregion

        private static long lastMs = 0;
		private static long ms = 0;
		private static MemoryStream bestGame = new MemoryStream(); // i'm an idiot ... this should be in a BinaryWriter ...
		private static MemoryStream worstGame = new MemoryStream();
		private static MemoryStream currentGame = new MemoryStream(); // it's also buggy sometimes (nodes showing off by one)
		private static List<int> scores = new List<int>();
        private static string PacmanAIDir = Environment.CurrentDirectory;
        private static BasePacman controller;
        private static bool m_RemainQuiet = false;

        // In regards to the ghosts within the game and the arguments that are 
        // applied.
        private static readonly string[] GHOST_CODES = { "bl", "br", "p", "r" };

        // Ghosts that are going to be in the game
        private static readonly List<string> Ghosts = new List<string>();

        // Bring the thread ID from the appropriate library
        [DllImport("kernel32")]
        static extern int GetCurrentThreadId();

        /// <summary>
        /// Load in the external DLL that is to be used.
        /// </summary>
        /// <param name="filename">The name of the file that we are loading in</param>
        /// <returns>The bytes that are loaded from the DLL file</returns>
        private static byte[] LoadBytes(string filename)
        {
            using (FileStream input = File.OpenRead(filename))
            {
                byte[] bytes = new byte[input.Length];
                input.Read(bytes, 0, bytes.Length);
                return bytes;
            }
        }

        /// <summary>
        /// Attempt to load in the controller that we want to use
        /// </summary>
        /// <param name="name">The type name of the controller in question</param>
        private static void tryLoadController(string name)
        {
            byte[] assemblyBytes = LoadBytes(Path.Combine(PacmanAIDir, "PacmanAI.dll"));
            byte[] assemblyPdbBytes = LoadBytes(Path.Combine(PacmanAIDir, "PacmanAI.pdb"));
            Assembly assembly = Assembly.Load(assemblyBytes, assemblyPdbBytes);
            Type[] _types = assembly.GetTypes();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.BaseType == typeof(BasePacman))
                {
                    BasePacman pacman = (BasePacman)Activator.CreateInstance(type);
                    if (pacman.Name == name)
                    {
                        controller = pacman;
                        gs.Controller = pacman;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Take in the launch arguments that are generated when the program is
        /// launched for the first time
        /// </summary>
        /// <param name="args">The arguments to deal with</param>
        static void HandleArguments(string[] args)
        {
            // If arguments have been specified then determine what
            if (args.Length > 0)
            {
                // Loop through the arguments and do something based on them.
                for (int i = 0; i < args.Length; i++)
                {
                    // Based on what arguments are used then do something
                    switch (args[i])
                    {
                        // How many games do we want simulated?
                        case "-c":
                            if ((i + 1) < args.Length)
                            {
                                int _result = 0;
                                if (int.TryParse(args[i + 1], out _result))
                                {
                                    gamesToPlay = _result;
                                }
                                else
                                {
                                    // Inform ourselves that the number was not found in the argument
                                    Console.WriteLine("Number after the -c argument was not recognised.");
                                }
                            }
                        break;

                        // Called for when we want the agent to be quiet and no log output whatsoever.
                        case "-q":
                            m_RemainQuiet = true;
                        break;

                        case "-g":
                            for (int j = 0; j < i + 4; j++)
                            {
                                // Make sure that the arguments that we are loop through are within radius
                                if (j < args.Length)
                                {
                                    // Make sure that it's a colour we recognise
                                    if (GHOST_CODES.Contains(args[j]))
                                    {
                                        Ghosts.Add(args[j]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            break;

                        // Define the agent that we are after
                        case "-a":
                            // If the argument specifier is within the argument range...
                            if ((i + 1) < args.Length)
                            {
                                // Make sure that the 
                                if (!args[i + 1].Contains("-"))
                                {
                                    // Attempt to load the controller...
                                    tryLoadController(args[i + 1]);
                                }
                            }
                        break;
                    }
                }
            }
        }
        
        static void Main(string[] args) {

            Process _process = Process.GetCurrentProcess();
            bool _takearguments = false;

            // How many arguments have been stored in the game.
            if (args.Length > 0)
            {
                Console.WriteLine("Arguments found.");
                _takearguments = true;
                HandleArguments(args);
            }

            // Required for storing the name of the agent.
            string _agentName = "";
            if (!_takearguments)
            {
                Console.WriteLine("Name of controller: ");
                _agentName = Console.ReadLine();
                Console.Clear();

                // Determine which ghosts are going to be added to the gameplay.
                while (Ghosts.Count < 4)
                {
                    Console.WriteLine(string.Format("({0} Ghosts) - Which ghosts (r) (bl) (br) (p) or (n) / (a)?",Ghosts.Count.ToString()));
                    string _ghostname = Console.ReadLine();

                    // Determine that the ghost name exists first
                    if (GHOST_CODES.Contains(_ghostname))
                    {
                        Ghosts.Add(_ghostname);
                        Console.Clear();
                    }
                    else if (_ghostname == "n") // Cancel out of it
                    {
                        break;
                    }
                    else if (_ghostname == "a")
                    {
                        // Clear out the list of ghosts that are entered already
                        // Do something else with them
                        Ghosts.Clear();
                        Ghosts.Add("bl");
                        Ghosts.Add("r");
                        Ghosts.Add("p");
                        Ghosts.Add("br");
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine("Ghost name is not valid");
                    }
                }

                Console.Clear();
                Console.WriteLine("How many games do you wish to simulate?");
                string _count = Console.ReadLine();
                int _result = 0;
                
                // Determine that the value that has been inputted is
                // in fact valid.
                while (!int.TryParse(_count,out _result))
                {
                    Console.Clear();
                    Console.WriteLine("Please try again: ");
                    _count = Console.ReadLine();

                }

                // Set the new count of games that we want to simulate.
                gamesToPlay = _result;

                Console.Clear();
                string _consoleoutput = "";

                while (_consoleoutput != "n" && _consoleoutput != "y")
                {
                    // Determine if we want to log output to be silence while we do this
                    Console.WriteLine("Silence output?");
                    _consoleoutput = Console.ReadLine();
                }

                if (_consoleoutput == "n")
                {
                    m_RemainQuiet = false;
                }
                else if (_consoleoutput == "y")
                {
                    m_RemainQuiet = true;
                }
            }

            // Get some strange invocation error here.
            // tryLoadController(_agentName);

			int cores = System.Environment.ProcessorCount;
			int gamesForEach = gamesToPlay / cores;
			for( int i = 0; i < cores; i++ ) {
				// add multicore support				
			}

            // Output the available cores.
            Console.WriteLine(string.Format("Cores Available: {0}",System.Environment.ProcessorCount));


			gs = new GameState();
			gs.GameOver += new EventHandler(GameOverHandler);
			gs.StartPlay();

			// DEFINE CONTROLLER //
			//BasePacman controller = new TestPac();
			BasePacman controller = new LucPacScripted();

            // Turn off the logging
            if (controller.GetType() == typeof(LucPac) && m_RemainQuiet)
            {
                LucPac.RemainQuiet = true;
            }

            if (controller.GetType() == typeof(LucPacScripted) && m_RemainQuiet)
            {
                LucPacScripted.RemainQuiet = true;
            }

			//BasePacman controller = new SmartDijkstraPac();
			gs.Controller = controller;

			Stopwatch watch = new Stopwatch();
			int percentage = -1;
			int lastUpdate = 0;
			watch.Start();
			while( gamesPlayed < gamesToPlay ) {
				int newPercentage = (int)Math.Floor(((float)gamesPlayed / gamesToPlay) * 100);
				if( newPercentage != percentage || gamesPlayed - lastUpdate >= 100 ) {
					lastUpdate = gamesPlayed;
					percentage = newPercentage;
					Console.Clear();
					Console.WriteLine("Simulating ... " + percentage + "% (" + gamesPlayed + " : " + gamesToPlay + ")");
					Console.WriteLine(" - Elapsed: " + formatSeconds((watch.ElapsedMilliseconds / 1000.0) + "") + "s, Estimated total: " + formatSeconds(((watch.ElapsedMilliseconds / 1000.0) / percentage * 100) + "") + "s");
					Console.WriteLine(" - Current best: " + highestScore);
					Console.WriteLine(" - Current worst: " + lowestScore);
					if( gamesPlayed > 0 ) {
						Console.WriteLine(" - Current avg.: " + (totalScore / gamesPlayed));
					}
					for( int i = scores.Count - 1; i >= 0 && i > scores.Count - 100; i-- ) {
						Console.Write(scores[i] + ",");
					}
				}
				// update gamestate
				Direction direction = controller.Think(gs);
				gs.Pacman.SetDirection(direction);
				// update stream
				currentGame.WriteByte((byte)Math.Floor(gs.Pacman.Xf));
				currentGame.WriteByte((byte)Math.Floor(gs.Pacman.Yf));
				currentGame.WriteByte((byte)gs.Pacman.Direction);
				currentGame.WriteByte((byte)gs.Pacman.Lives);
				currentGame.WriteByte((byte)(gs.Pacman.Score / 255));
				currentGame.WriteByte((byte)(gs.Pacman.Score % 255));

				foreach( Pacman.Simulator.Ghosts.Ghost g in gs.Ghosts ) {
					currentGame.WriteByte((byte)g.X);
					currentGame.WriteByte((byte)g.Y);
					currentGame.WriteByte((byte)((g.Chasing == true) ? 1 : 0));
					currentGame.WriteByte((byte)((g.Entered == true) ? 1 : 0));
					currentGame.WriteByte((byte)g.Direction);
					currentGame.WriteByte((byte)((g.IsEaten == true) ? 1 : 0));
				}
				// update game
				gs.Update();
				ms += GameState.MSPF;
			}
			watch.Stop();

			// shut down controller
			controller.SimulationFinished();

			// write best/worst to disk
			using( BinaryWriter bw = new BinaryWriter(new FileStream(System.Environment.CurrentDirectory + "/best" + highestScore + ".dat", FileMode.Create)) ) {
				bestGame.WriteTo(bw.BaseStream);
			}
			using( BinaryWriter bw = new BinaryWriter(new FileStream(System.Environment.CurrentDirectory + "/worst" + lowestScore + ".dat", FileMode.Create)) ) {
				worstGame.WriteTo(bw.BaseStream);
			}

			// write results
			using( StreamWriter sw = new StreamWriter(File.Open("scores.txt", FileMode.Create)) ) {
				foreach( int s in scores ) {
					sw.Write(s + "\n");
				}
			}

			// output results
			Console.Clear();
			long seconds = ms / 1000;
			Console.WriteLine("Games played: " + gamesPlayed);
			Console.WriteLine("Avg. score: " + (totalScore / gamesPlayed));
			Console.WriteLine("Highest score: " + highestScore + " points");
			Console.WriteLine("Lowest score: " + lowestScore + " points");
            Console.WriteLine("Max Pills Eaten: " + maxPillsEaten);
            Console.WriteLine("Min Pills Eaten: " + minPillsEaten);
            Console.WriteLine("Average Pills Eaten: " + pillsEatenTotal / gamesPlayed);
            Console.WriteLine("Max Ghosts Eaten: " + maxGhostsEaten);
            Console.WriteLine("Min Ghosts Eaten: " + minGhostsEaten);
            Console.WriteLine("Average Ghosts Eaten: " + totalGhostsEaten / gamesPlayed);
			Console.WriteLine("Longest game: " + ((float)longestGame / 1000.0f) + " seconds");
			Console.WriteLine("Total simulated time: " + (seconds / 60 / 60 / 24) + "d " + ((seconds / 60 / 60) % 24) + "h " + ((seconds / 60) % 60) + "m " + (seconds % 60) + "s");
			Console.WriteLine("Avg. simulated time pr. game: " + ((float)ms / 1000.0f / gamesPlayed) + " seconds");
			Console.WriteLine("Simulation took: " + (watch.ElapsedMilliseconds / 1000.0f) + " seconds");
			Console.WriteLine("Speed: " + (ms / watch.ElapsedMilliseconds) + " (" + ((ms / watch.ElapsedMilliseconds) / 60) + "m " + ((ms / watch.ElapsedMilliseconds) % 60) + " s) simulated seconds pr. second");
			Console.WriteLine("For a total of: " + gamesPlayed / (watch.ElapsedMilliseconds / 1000.0f) + " games pr. second");
			Console.ReadLine();
		}

        public void SaveTestDataToDisk()
        {
            avgScore = totalScore / gamesPlayed;
            avgGhostsEaten = totalGhostsEaten / gamesPlayed;
            avgPillsEaten = pillsEatenTotal / gamesPlayed;

            StreamWriter _writer = new StreamWriter(DateTime.Now.ToString("hhmmss") + ".txt");
            _writer.WriteLine(string.Format("Total Ghosts Eaten: {0}", totalGhostsEaten));
            _writer.WriteLine(string.Format("Average Ghosts Eaten: {0}", avgGhostsEaten));
            _writer.WriteLine(string.Format("Max Ghosts Eaten: {0}", maxGhostsEaten));
            _writer.WriteLine(string.Format("Min Ghosts Eaten: {0}", minGhostsEaten));
            _writer.WriteLine(string.Format("Total Pills Eaten: {0}", pillsEatenTotal));
            _writer.WriteLine(string.Format("Max Pills Eaten: {0}", maxPillsEaten));
            _writer.WriteLine(string.Format("Min Pills Eaten: {0}", minPillsEaten));
            _writer.WriteLine(string.Format("Average Pills Eaten: {0}", avgPillsEaten));
            _writer.WriteLine(string.Format("Max Score: {0}", highestScore));
            _writer.WriteLine(string.Format("Min Score: {0}", lowestScore)); 
            _writer.WriteLine(string.Format("Average Score: {0}", avgScore));

            _writer.Flush();
            _writer.Close();
        }

		private static void GameOverHandler(object sender, EventArgs args) {
			if( ms - lastMs > longestGame )
				longestGame = ms - lastMs;
			if( gs.Pacman.Score > highestScore ) {
				highestScore = gs.Pacman.Score;
				bestGame = currentGame;
			}
			if( gs.Pacman.Score < lowestScore ) {
				lowestScore = gs.Pacman.Score;
				worstGame = currentGame;
			}
            
            totalScore += gs.Pacman.Score;

            if (gs._pillsEaten > maxPillsEaten)
            {
                maxPillsEaten = gs._pillsEaten;
            }

            if (gs._pillsEaten < minPillsEaten)
            {
                minPillsEaten = gs._pillsEaten;
            }

            /// GHOSTS EATEN
            if (gs._ghostsEaten > maxGhostsEaten)
            {
                maxGhostsEaten = gs._ghostsEaten;
            }

            if (gs._ghostsEaten < minGhostsEaten)
            {
                minGhostsEaten = gs._ghostsEaten;
            }

            // Total up the amount of pills that have been eaten.
            pillsEatenTotal += gs._pillsEaten;
            
            totalGhostsEaten += gs._ghostsEaten;

			scores.Add(gs.Pacman.Score);
			currentGame = new MemoryStream();
			//totalScore += gs.Pacman.Score;
			gamesPlayed++;
			lastMs = ms;
		}

		private static string formatSeconds(string s) {
			try {
				return s.Substring(0, s.IndexOf(","));
			} catch {
				return s;
			}
		}
	}
}
