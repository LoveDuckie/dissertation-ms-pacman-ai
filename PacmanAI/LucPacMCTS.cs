using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

using Pacman.Simulator;
using Pacman.Simulator.Ghosts;

namespace PacmanAI
{
    /// <summary>
    /// Carry out some pure MCTS approach when it comes to generating the research
    /// </summary>
    public class LucPacMCTS : BasePacman
    {
        private static LucPacMCTS instance = null;

        public static LucPacMCTS INSTANCE
        {
            get { return instance; }
            set { instance = value; }
        }

        private TreeNode m_TreeRoot = null;

        public TreeNode TreeRoot
        {
            get { return m_TreeRoot; }
            set { m_TreeRoot = value; }
        }

        private bool m_IsAtJunction = true;
        // Don't bother outputting any logs to the console if this is active.
        // Want to make sure that this is false before we display anything
        public static bool REMAIN_QUIET = false;

        public const int MAX_LOG_ITEMS_DISPLAY = 10;
        public List<string> m_LogOutput = new List<string>();

        private Direction m_CurrentDirection;

        /// <summary>
        /// Used for rendering the tree branches on the screen.
        /// </summary>
        private Graphics m_GraphicsDevice;

        private Node m_Junction = null;

        // Useful for the likes of MCTS and calling other states
        public static GameState m_GameState;
        public static GameState m_PreviousGameState;

        public static Image m_GreenBlock = null;
        public static Image m_RedBlock = null;
        public static Image m_BlueBlock = null;

        private int m_MCTSTimeBegin = 0;
        private int m_MCTSTimeEnd = 0;

        #region Constructor
        public LucPacMCTS() : base("LucPacMCTS")
        {
            m_GreenBlock = Image.FromFile("green_block.png");
            m_RedBlock = Image.FromFile("red_block.png");
            m_BlueBlock = Image.FromFile("blue_block.png");

            LucPac.m_GreenBlock = m_GreenBlock = Image.FromFile("green_block.png");
            LucPac.m_RedBlock = Image.FromFile("red_block.png");
            LucPac.m_BlueBlock = Image.FromFile("blue_block.png");
            instance = this;

            // Create the session ID that will be used for testing 
            this.m_TestSessionID = GenerateSessionID();
            this.m_TestStats.SessionID = m_TestSessionID;

            // Create the directory that the data is going to be stored in 
            m_TestDataFolder = Directory.CreateDirectory(Environment.CurrentDirectory + string.Format("/{0}", m_TestSessionID));
            m_TestImagesFolder = m_TestDataFolder.CreateSubdirectory("images");
            m_TestLogFolder = m_TestDataFolder.CreateSubdirectory("logs");

            m_Stopwatch.Start();

            this.m_MS = 0;
            this.m_LastLifeMS = 0;
            this.m_LastRoundMS = 0;

            m_GameStart = DateTime.Now; // For determining how long it took to complete level.
            m_LifeStart = m_GameStart;
        }
        #endregion

        /// <summary>
        /// Runs the evaluation process so that the UCB scoring can be done to it
        /// </summary>
        /// <param name="pNode">The node that we are starting the evaluation from</param>
        /// <param name="pGameState">The game state that we are observing</param>
        /// <returns>The score that is generated.</returns>
        public int EvaluateNode(TreeNode pNode, GameState pGameState)
        {
            // The score weighting for the MCTS tree in question
            int _score = 0;
            GameState _gamestateCopied = (GameState)pGameState.Clone();

            //_gamestateCopied.ElapsedTime += 5;

            int _livesremaining = _gamestateCopied.Pacman.Lives;
            int _currentlevel = _gamestateCopied.Level;

            // Update the game state from the current node.
            _gamestateCopied = pNode.UpdateGame(_gamestateCopied);

            // Determine whether or not the lives remaining has changed.
            if (_livesremaining > _gamestateCopied.Pacman.Lives)
            {
                _score -= TreeNode.DEATH_PENALTY;
            }

            /** Process the simulation roll out using a random controller for the PacMan **/
            if (_currentlevel == pGameState.Level)
            {
                // Using a random pacman AI controller, determine the health of this state
                // for the amount of cycles that is provided.
                _gamestateCopied = SimulateGame(_gamestateCopied, new SimRandomPac(), TreeNode.MAX_CYCLES);
            }

            _score += (_gamestateCopied.Pacman.Score - pGameState.Pacman.Score);

            // Determine whether or not the pacman got onto the next level.
            if (_currentlevel < pGameState.Level)
                _score += TreeNode.COMPLETE_REWARD;

            return _score;
        }

        public static GameState SimulateGame(GameState pGameState, BasePacman pController, int pSteps)
        {
            int _currentlevel = pGameState.Level;
            int _gameoverCount = pGameState.m_GameOverCount;
            GameState _gameStateCloned = (GameState)pGameState.Clone();

            // Set the random controller to the game state that we are focusing on
            _gameStateCloned.Controller = pController;

            // Loop through the maximum amount of steps and then perform the 
            // simulation on the game state
            while (pSteps-- > 0
                   && _gameStateCloned.Level == _currentlevel
                   && _gameoverCount == _gameStateCloned.m_GameOverCount)
            {
                _gameStateCloned.UpdateSimulated(_gameStateCloned);
            }

            // SaveStateAsImage(_gameStateCloned, LucPac.INSTANCE, "_simulatedgame");

            return _gameStateCloned;
        }

        /// <summary>
        /// When we want to exploit the best nodes within the tree
        /// </summary>
        /// <param name="pGameState">The game state that we are performing work on</param>
        public void RunSimulation(GameState pGameState)
        {
            // Determine first that there is a tree root that we can use
            if (m_TreeRoot != null)
            {
                // Find the tree node with the best average value to use
                TreeNode _toevaluate = m_TreeRoot.UCT();
                _toevaluate.AddScore(EvaluateNode(_toevaluate, pGameState));

                if (m_TreeRoot.CountLayers() < TreeNode.LAYER_THRESHOLD)
                {
                    if (_toevaluate.SampleSize == TreeNode.EXPANSION_THRESHOLD)
                    {
                        // Generate a new set of children for us to look at
                        _toevaluate.Expand(false, pGameState);
                        if (_toevaluate.Children != null)
                        {
                            // Loop through the children of the new tree node that we used
                            for (int i = 0; i < _toevaluate.Children.Length; i++)
                            {
                                _toevaluate.Children[i].AddScore(EvaluateNode(_toevaluate.Children[i], pGameState));
                            }
                        }
                    }
                }
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// Return whether or not we have hit a wall within the provided game state.
        /// </summary>
        /// <param name="pCurrentPosition">The current position of the Pac-Man agent</param>
        /// <param name="pGameState">The current game state</param>
        /// <param name="pDirection">The direction that Ms. Pac-Man is heading in</param>
        /// <returns></returns>
        public static bool HitWall(Node pCurrentPosition, GameState pGameState, Direction pDirection)
        {
            // Loop through the possible directions at the give node
            // If a direction is the same as the one that Pacman is going in
            // then we've hit a wall
            foreach (var item in Node.GetAllPossibleDirections(pCurrentPosition))
            {
                if (item == pDirection)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Update the game state back to the root
        /// </summary>
        /// <param name="pGameState">The newly advanced game state.</param>
        /// <returns>The newly updated game state.</returns>
        public GameState UpdateToRoot(GameState pGameState)
        {
            return m_TreeRoot.CPathAdvanceGame((GameState)pGameState.Clone(), pGameState.Pacman.Direction);
        }

        public static int CountPillsDirection(Direction pDirection, GameState pGameState)
        {
            Node _currentPosition = pGameState.Pacman.Node;
            int _pillCount = 0;

            // Keep going in a certain direction until we determine whether or not that we have hit a wall.
            while (!HitWall(_currentPosition, pGameState, pDirection))
            {
                _currentPosition = _currentPosition.GetNeighbour(pDirection);

                // Determine whether or not the pill that we are looking at is either a power pill
                // or just a normal pill
                if (_currentPosition.Type == Node.NodeType.Pill ||
                    _currentPosition.Type == Node.NodeType.PowerPill)
                {
                    _pillCount++;
                }
            }

            return _pillCount;
        }

        // Prepare the root node of the MCTS algorithm that will deal with everything ese
        public void PrepareRoot(GameState pGameState)
        {
            // Grab the score from the performance of the tree node that we are evaluating
            m_TreeRoot = new TreeNode(pGameState,
                                      this,
                                      null,
                                      new TreeNode().CPathAdvanceGame((GameState)pGameState.Clone(), pGameState.Pacman.Direction).Pacman.Node,
                                      new Direction[] { pGameState.Pacman.Direction }); // Generate a new array of directions

            // Return the score value for advancing in the given direction
            m_TreeRoot.AddScore(EvaluateNode(m_TreeRoot, pGameState));

            // Expand the tree root so that adjacent junctions are generated
            m_TreeRoot.Expand(false, pGameState);
        }


        /// <summary>
        /// Return the direction that we want the Pacman agent to go in
        /// </summary>
        /// <param name="gs">The current gamestate</param>
        /// <returns>Direction</returns>
        public override Direction Think(GameState gs)
        {
            m_GameState = gs;

            // Only output information to the console if this has been set to true.
            if (!REMAIN_QUIET)
            {
                UpdateConsole();
            }

            // Keep adding this on at every tick
            m_MS += GameState.MSPF;

            if (m_IsAtJunction)
            {
                m_IsAtJunction = false;
                PrepareRoot(gs);
                int _completedsimulations = 0;
                int _timetaken = 0;
                
                m_MCTSTimeBegin = Environment.TickCount;
                
                while (_completedsimulations < TreeNode.MAX_SIMULATIONS)
                {
                    RunSimulation(UpdateToRoot((GameState)gs));
                    _completedsimulations++;
                }
                
                m_MCTSTimeEnd = Environment.TickCount;

                // Return the value for the time that has taken to generate it
                _timetaken = m_MCTSTimeEnd - m_MCTSTimeBegin;

                m_TestStats.MCTSTotalTime += _timetaken;
                m_TestStats.MCTSTotalGenerations++;

                if (_timetaken > m_TestStats.MCTSMaximum)
                {
                    m_TestStats.MCTSMaximum = _timetaken;
                }

                if (_timetaken < m_TestStats.MCTSMinimum)
                {
                    m_TestStats.MCTSMinimum = _timetaken;
                }

                // Generate the new average based on the total MCTS generations done
                // and the total amount of time it's taken
                m_TestStats.MCTSAverage = m_TestStats.MCTSTotalTime / m_TestStats.MCTSTotalGenerations;

                OutputLog(string.Format("MCTS Generation Time: {0} ms", _timetaken.ToString()), true, true);


                m_PreviousGameState = gs;
                return Direction.None;
            }
            else if (IsJunction(gs.Pacman.Node.X, gs.Pacman.Node.Y, gs) || m_TreeRoot.CurrentPosition == gs.Pacman.Node)
            {
                m_IsAtJunction = true;

                int _completedsimulations = 0;
                int _timetaken = 0;
                m_MCTSTimeBegin = Environment.TickCount;

                while (_completedsimulations < TreeNode.MAX_SIMULATIONS)
                {
                    RunSimulation((GameState)gs);
                    _completedsimulations++;
                }

                m_MCTSTimeEnd = Environment.TickCount;
                _timetaken = m_MCTSTimeEnd - m_MCTSTimeBegin;

                m_PreviousGameState = gs;
                return GetNextDirectionFromTree(m_TreeRoot, false, gs, SelectionParameter.HighestUCB);
            }
            else
            {
                int _completedsimulations = 0;

                while (_completedsimulations < TreeNode.SHALLOW_SIMULATIONS)
                {
                    RunSimulation(UpdateToRoot((GameState)gs));
                    _completedsimulations++;
                }

                m_PreviousGameState = gs;

                // Carry on as normal.
                return TryGoDirection(gs, gs.Pacman.Direction);
            }
        }

        public override void UpdateConsole()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine("================== LUCPAC-MCTS ==================");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            string _pacmanPosition = string.Format("Pacman: {0},{1}", m_GameState.Pacman.Node.X, m_GameState.Pacman.Node.Y);

            //m_GameState.Pacman.ImgX.ToString(),m_GameState.Pacman.ImgY.ToString()

            foreach (var ghost in m_GameState.Ghosts)
            {
                Console.WriteLine(String.Format("{0}: {1},{2}", ghost.GetType().ToString(),
                                                                ghost.Node.X,
                                                                ghost.Node.Y));
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("PILLS REMAINING: {0}", m_GameState.Map.PillNodes.Where(n => n.Type != Node.NodeType.None && n.Type != Node.NodeType.Wall).Count().ToString()));
            Console.WriteLine(string.Format("PILLS LEFT(INT): {0}", m_GameState.Map.PillsLeft.ToString()));

            Console.WriteLine("=================== TEST DATA ==============");

            if (m_TestStats.TotalGames >= MAX_TEST_GAMES)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("** TEST COMPLETE **");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("** TEST IN PROGRESS **");
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(string.Format("SESSION ID: {0}", m_TestSessionID));
            Console.WriteLine(string.Format("MAX PILLS EATEN: {0}", m_TestStats.MaxPillsTaken));
            Console.WriteLine(string.Format("MIN PILLS EATEN: {0}", m_TestStats.MinPillsTaken));
            Console.WriteLine(string.Format("GAMES PLAYED: {0}", m_TestStats.TotalGames));
            Console.WriteLine(string.Format("HIGHEST SCORE: {0}", m_TestStats.MaxScore));
            Console.WriteLine(string.Format("AVERAGE SCORE: {0}", m_TestStats.AverageScore));
            Console.WriteLine(string.Format("LOWEST SCORE: {0}", m_TestStats.MinScore));
            Console.WriteLine(string.Format("MINIMUM GHOSTS EATEN: {0}", m_TestStats.MinGhostsEaten));
            Console.WriteLine(string.Format("AVERAGE GHOSTS EATEN: {0}", m_TestStats.AverageGhostsEaten));
            Console.WriteLine(string.Format("MAXIMUM GHOSTS EATEN: {0}", m_TestStats.MaxGhostsEaten));
            Console.WriteLine(string.Format("MIN MCTS GENERATION TIME: {0}", m_TestStats.MCTSMinimum));
            Console.WriteLine(string.Format("MAX MCTS GENERATION TIME: {0}", m_TestStats.MCTSMaximum));
            Console.WriteLine(string.Format("AVERAGE MCTS GENERATION TIME: {0}", m_TestStats.MCTSAverage));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=================== LOG ====================");
            Console.ForegroundColor = ConsoleColor.White;

            // Loop through the list of messages and output them as well
            for (int i = 0; i < m_LogOutput.Count; i++)
            {
                Console.WriteLine(m_LogOutput[i]);
            }
        }

        /// <summary>
        /// Save a given game state to an image
        /// </summary>
        /// <param name="pGameState">The game state that we wish to render it to</param>
        /// <param name="pController">The controller that we are obtaining information from</param>
        /// <param name="pImageName">The name of the image once we write it to disk</param>
        /// <param name="pRenderMCTS">Whether or not we render the tree for the MCTS agent</param>
        public static void SaveStateAsImage(GameState pGameState, LucPacMCTS pController, string pImageName, bool pRenderMCTS)
        {
            // Clone the image that we are going to be rendering to
            Image _newimage = (Image)Visualizer.RenderingImage.Clone();
            Graphics _drawingObject = Graphics.FromImage(_newimage);
            _drawingObject.DrawImage(m_RedBlock, new Point(50, 50));

            // Draw the map, pacman and the ghosts to the image
            pGameState.Map.Draw(_drawingObject);
            pGameState.Pacman.Draw(_drawingObject, Visualizer.RenderingSprites);
            foreach (var item in pGameState.Ghosts)
            {
                item.Draw(_drawingObject, Visualizer.RenderingSprites);
            }

            // Determine whether or not the tree root is valid before
            // continuing
            if (pController.TreeRoot != null)
            {
                _drawingObject.DrawImage(m_GreenBlock, new Point());

                // Draw the output accordingly.
                pController.TreeRoot.Draw(_drawingObject);
                _drawingObject.DrawImage(m_RedBlock, new Point(pController.TreeRoot.PathNode.CenterX, pController.TreeRoot.PathNode.CenterY));
            }

            string _filename = "";
            if (pImageName != "")
            {
                _filename = pImageName;
            }
            else
            {
                _filename = "screengrab";
            }

            // Save the image out so that we can observe it
            _newimage.Save(string.Format("{2}\\{0}_{1}_{3}.bmp", DateTime.Now.ToString("ddMMyyyyHHmmssff"), _filename, pController.m_TestImagesFolder.FullName, pController.m_TestStats.TotalGames));
            _newimage.Dispose();
        }

        public override void Restart(GameState gs)
        {
            // Don't update the stats more than 100 times.
            // That's only the amount of games that we want simulated.
            if (m_TestStats.TotalGames < MAX_TEST_GAMES)
            {
                Utility.SerializeGameState(gs,this);

                // Save the image to the same directory as the simulator
                SaveStateAsImage(gs, this, string.Format("endofround_{0}_{1}_",
                                                         m_TestStats.TotalGames.ToString(),
                                                         this.Name.ToString()),true);

                // Set the stats.
                m_TestStats.TotalGhostsEaten += gs.m_GhostsEaten;
                m_TestStats.TotalPillsTaken += gs.m_PillsEaten;
                m_TestStats.TotalScore += gs.Pacman.Score;
                m_TestStats.TotalLevelsCleared += gs.Level;
                m_TestStats.TotalGames++;

                if (m_MS - m_LastRoundMS > m_TestStats.LongestRoundTime)
                {
                    m_TestStats.LongestRoundTime = m_MS - m_LastRoundMS;
                }

                if (m_MS - m_LastRoundMS < m_TestStats.ShortestRoundTime)
                {
                    m_TestStats.ShortestRoundTime = m_MS - m_LastRoundMS;
                }

                m_TestStats.TotalRoundTime += m_MS - m_LastRoundMS;
                m_TestStats.AverageRoundTime = m_TestStats.TotalRoundTime / m_TestStats.TotalGames;

                m_LastRoundMS = m_MS;

                /// LEVELS
                if (gs.Level < m_TestStats.MinLevelsCleared)
                {
                    this.m_TestStats.MinLevelsCleared = gs.Level;
                }

                if (gs.Level > m_TestStats.MaxLevelsCleared)
                {
                    this.m_TestStats.MaxLevelsCleared = gs.Level;
                }

                /// SCORE
                if (gs.Pacman.Score < m_TestStats.MinScore)
                {
                    m_TestStats.MinScore = gs.Pacman.Score;
                }

                if (gs.Pacman.Score > m_TestStats.MaxScore)
                {
                    m_TestStats.MaxScore = gs.Pacman.Score;
                }

                /// PILLS
                if (gs.m_PillsEaten < m_TestStats.MinPillsTaken)
                {
                    m_TestStats.MinPillsTaken = gs.m_PillsEaten;
                }

                if (gs.m_PillsEaten > m_TestStats.MaxPillsTaken)
                {
                    m_TestStats.MaxPillsTaken = gs.m_PillsEaten;
                }

                /// SCORE DIFFERENCE
                if (gs.m_GhostsEaten < m_TestStats.MinGhostsEaten)
                {
                    m_TestStats.MinGhostsEaten = gs.m_GhostsEaten;
                }

                if (gs.m_GhostsEaten > m_TestStats.MaxGhostsEaten)
                {
                    m_TestStats.MaxGhostsEaten = gs.m_GhostsEaten;
                }
            }
            else
            {
                // Test has terminated, display and save the final results.
                if (!m_TestComplete)
                {
                    m_Stopwatch.Stop();
                    m_TestStats.ElapsedMillisecondsTotal = m_Stopwatch.ElapsedMilliseconds;

                    m_TestStats.AveragePillsTaken = m_TestStats.TotalPillsTaken / m_TestStats.TotalGames;
                    m_TestStats.AverageScore = m_TestStats.TotalScore / m_TestStats.TotalGames;
                    m_TestStats.AverageGhostsEaten = m_TestStats.TotalGhostsEaten / m_TestStats.TotalGames;
                    m_TestStats.AverageLevelsCleared = m_TestStats.TotalLevelsCleared / m_TestStats.TotalGames;

                    SerializeTestStats(m_TestStats);
                    m_TestComplete = true;
                }
            }
            
            base.Restart(gs);
        }

        /// <summary>
        /// Output a message to the log with various arguments
        /// </summary>
        /// <param name="pLogMessage">Message to be displayed</param>
        /// <param name="pVerbose">Is this to be written to the console window?</param>
        /// <param name="pDisplayDate">Display the date with the text log message?</param>
        public virtual void OutputLog(string pLogMessage, bool pVerbose, bool pDisplayDate)
        {
            StreamWriter _writer = new StreamWriter(string.Format("{0}\\output_{1}.txt", m_TestLogFolder.FullName.ToString(), DateTime.Now.ToString("ddMMyyyy")), true);
            string _currentdate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss ff");
            string _output = "";

            // Determine whether or not we want to display the date.
            if (pDisplayDate)
                _output = string.Format("[{0}]: {1}", _currentdate, pLogMessage);
            else
                _output = pLogMessage;

            _writer.WriteLine(_output);

            // Make sure whether or not adding one more message would cause problems.

            if (pVerbose && !REMAIN_QUIET)
            {
                if (m_LogOutput.Count + 1 > MAX_LOG_ITEMS_DISPLAY)
                {
                    m_LogOutput.Remove(m_LogOutput.Last());
                }

                // Insert the message to the front
                m_LogOutput.Insert(0, _output);
            }

            // Clean up the writer afterwards.
            _writer.Flush();
            _writer.Dispose();
            _writer.Close();
        }

        /// <summary>
        /// Grab the next direction to go in based on the information generated by the tree
        /// </summary>
        /// <param name="pNode">The tree node that we are going to determine has the best UCB scoring child</param>
        /// <returns>The next direction that has to be taken from the search tree</returns>
        public Direction GetNextDirectionFromTree(TreeNode pNode, bool pPowerPillBias, GameState gs, SelectionParameter pParameter)
        {
            TreeNode _highestScoring = null;

            // Ensure that we are appropriately dealing with the right things
            if (pNode != null)
            {
                // Make sure that the tree node in question has children
                if (pNode.Children != null)
                {
                    // Loop through the children and determine which one has the best score
                    for (int i = 0; i < pNode.Children.Length; i++)
                    {
                        // If the value is null, just assign it to being the first value anyway.
                        if (_highestScoring == null)
                        {
                            _highestScoring = pNode.Children[i];
                        }
                        else if (pNode.Children[i].AverageScore > _highestScoring.AverageScore)
                        {
                            _highestScoring = pNode.Children[i];
                        }
                    }
                }
            }

            OutputLog(string.Format("Chosen direction is {0}", _highestScoring.Directions[1].ToString()), true, true);
            return _highestScoring.Directions[1];
            //return Direction.Stall;
        }


        // Attempt to go within the provided direction. If it's not possible, then
        // return the next nearest direction.
        private Direction TryGoDirection(GameState gs, Direction pDirection)
        {
            var _directions = gs.Pacman.PossibleDirections();

            // Determine whether or not we are able to go in that direction
            if (_directions.Contains(pDirection))
            {
                return pDirection;
            }
            else
            {
                // Just return the first that is not the inverse of the direction
                // that we are aiming to go in
                foreach (var dir in _directions)
                {
                    if (GameState.InverseDirection(dir) != pDirection)
                    {
                        return dir;
                    }
                }
            }

            return Direction.None;
        }

        /// <summary>
        /// Determine that the node in the graph is a junction
        /// </summary>
        /// <param name="pX">The X coordinate within the graph</param>
        /// <param name="pY">The Y coordinate within the graph</param>
        /// <returns>Return whether or not it's a junction at the given position</returns>
        public static bool IsJunction(int pX, int pY, GameState pGameState)
        {
            // Check that the coordinates are valid
            if (pX < pGameState.Map.Nodes.GetLength(0) && pX > 0 &&
                pY < pGameState.Map.Nodes.GetLength(1) && pY > 0)
            {
                return pGameState.Map.Nodes[pX, pY].PossibleDirections.Count > 2;
            }

            return false;
        }

        public override void EatenByGhost()
        {
            base.EatenByGhost();
        }

        public override void Draw(Graphics g)
        {
            m_GraphicsDevice = g;

            // If a junction has been found, then draw a line
            if (m_Junction != null)
            {
                g.DrawLine(Pens.Green, new Point(m_GameState.Pacman.Node.CenterX, m_GameState.Pacman.Node.CenterY),
                                       new Point(m_Junction.CenterX, m_Junction.CenterY));
            }

            // Draw the debug output if the tree has been generated.
            if (m_TreeRoot != null)
            {
                m_TreeRoot.Draw(g);

                g.DrawImage(m_RedBlock, new Point(m_TreeRoot.PathNode.CenterX - 2, m_TreeRoot.PathNode.CenterY - 2));

                g.DrawString(m_TreeRoot.AverageScore.ToString(), new Font(FontFamily.GenericSansSerif, 10f), Brushes.White,
                             m_TreeRoot.PathNode.CenterX, m_TreeRoot.PathNode.CenterY);
            }
            //            g.DrawImage(m_GreenBlock, new Point(m_GameState.Pacman.Node.CenterX, m_GameState.Pacman.Node.CenterY));


            base.Draw(g);
        }

    }
}
