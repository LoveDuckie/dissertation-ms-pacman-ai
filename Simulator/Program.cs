using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Pacman.Simulator
{
	class Program
	{
		private static Visualizer visualizer;
		private static Thread visualizerThread;

        // For argument based modifications ot the game state
        private static string AGENT_NAME = "";
        private static List<string> GHOSTS_AVAILABLE = new List<string>();
        private static bool TAKEN_ARGUMENTS = false;

        // For checking that the ghosts placed in the arguments are valid.
        private readonly static string[] GHOST_ALLOWED = { "bl", "br", "p", "r" };

		static void Main(string[] args) {
			Console.WriteLine("Simulator started");
            Console.WriteLine("Finding arguments...");
            //Console.WriteLine("Press Enter to exit");

            #region Argument Handling
            // If more than one argument has been set, then do something
            if (args.Length > 0)
            {
                TAKEN_ARGUMENTS = true;
                Console.WriteLine("Arguments found!");
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-a":
                            if ((i + 1) < args.Length)
                            {
                                // Make sure that it's not another argument we're working with
                                if (!args[i + 1].Contains("-"))
                                {
                                    // Take in the name argument as the name of the agent to load
                                    AGENT_NAME = args[i + 1];
                                }
                            }
                        break;

                        case "-g":
                            // Loop through the ghosts arguments and determine if they are appropriate
                            for (int j = 0; j < i + 4; j++)
                            {
                                // Make sure we're still within the bounds of the available arguments.
                                if (j < args.Length)
                                {
                                    //  Determine the that ghost is legit and then do something
                                    if (!args[j].Contains("-") &&
                                        Array.IndexOf(GHOST_ALLOWED, args[j]) > -1)
                                    {
                                        GHOSTS_AVAILABLE.Add(args[j]);
                                    }
                                }
                            }
                        break;
                    }
                }
            }
            else
            {
                TAKEN_ARGUMENTS = false;
                Console.WriteLine("No arguments found.");
            }
            #endregion

            if (AGENT_NAME == "")
            {
                while (AGENT_NAME == "")
                {
                    Console.Clear();
                    Console.WriteLine("What is the name of the agent?");
                    AGENT_NAME = Console.ReadLine();
                }
            }

            startVisualizer();
						
			while( true ) {
				string input = Console.ReadLine();
				switch(input){
					case "":
						//visualizerThread.Abort(); // buggy ... catch and close down gracefully
						//System.Threading.Thread.CurrentThread.Abort();
						break;
					case "restart":
					case "r":
						// support this
						break;
				}

			}
		}

		private static void startVisualizer() {
			visualizerThread = new System.Threading.Thread(delegate() {
				visualizer = new Visualizer();
                
                // Set the visualizer form title to the agent class name
                if (AGENT_NAME != "")
                {
                    visualizer.Text = AGENT_NAME;
                }
                System.Windows.Forms.Application.Run(visualizer);
			});
			visualizerThread.Start();
		}

		private static void trace(params object[] list) {
			foreach( object o in list ) {
				try {
					Console.WriteLine(o.ToString() + ", ");
				} catch( Exception e ) {
					Console.WriteLine(e.Message + ", ");
				}				
			}
		}
	}
}
