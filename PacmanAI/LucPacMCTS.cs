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
        private static LucPacMCTS _instance = null;

        public static LucPacMCTS Instance
        {
            get { return _instance; }
            set { _instance = value; }
        }

        private TreeNode _treeRoot = null;

        public TreeNode TreeRoot
        {
            get { return _treeRoot; }
            set { _treeRoot = value; }
        }

        private bool _isAtJunction = true;
        // Don't bother outputting any logs to the console if this is active.
        // Want to make sure that this is false before we display anything
        public static bool RemainQuiet = false;

        public const int MaxLogItemsDisplay = 10;

        public List<string> _logOutput = new List<string>();

        /// <summary>
        /// Used for rendering the tree branches on the screen.
        /// </summary>
        private Graphics _graphicsDevice;

        private Node _junction = null;

        // Useful for the likes of MCTS and calling other states
        public static GameState _gameState;
        public static GameState _previousGameState;

        public static Image _greenBlock = null;
        public static Image _redBlock = null;
        public static Image _blueBlock = null;

        private int m_MCTSTimeBegin = 0;
        private int m_MCTSTimeEnd = 0;

        #region Constructor
        public LucPacMCTS() : base("LucPacMCTS")
        {
            _greenBlock = Image.FromFile("green_block.png");
            _redBlock = Image.FromFile("red_block.png");
            _blueBlock = Image.FromFile("blue_block.png");

            LucPac._greenBlock = _greenBlock = Image.FromFile("green_block.png");
            LucPac._redBlock = Image.FromFile("red_block.png");
            LucPac._blueBlock = Image.FromFile("blue_block.png");
            _instance = this;

            // Create the session ID that will be used for testing 
            this._testSessionId = GenerateSessionID();
            this._testStats.SessionID = _testSessionId;

            // Create the directory that the data is going to be stored in 
            _testDataFolder = Directory.CreateDirectory(Environment.CurrentDirectory + string.Format("/{0}", _testSessionId));
            _testImagesFolder = _testDataFolder.CreateSubdirectory("images");
            _testLogFolder = _testDataFolder.CreateSubdirectory("logs");

            _stopWatch.Start();

            this.Milliseconds = 0;
            this.LastLifeMilliseconds = 0;
            this.LastRoundMilliseconds = 0;

            this.GameStartTimestamp = DateTime.Now; // For determining how long it took to complete level.
            this.LifeStartTimestamp = GameStartTimestamp;
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
        public void RunSimulation(GameState gameState)
        {
            // Determine first that there is a tree root that we can use
            if (_treeRoot != null)
            {
                // Find the tree node with the best average value to use
                TreeNode _toevaluate = _treeRoot.UCT();
                _toevaluate.AddScore(EvaluateNode(_toevaluate, gameState));

                if (_treeRoot.CountLayers() < TreeNode.LAYER_THRESHOLD)
                {
                    if (_toevaluate.SampleSize == TreeNode.EXPANSION_THRESHOLD)
                    {
                        // Generate a new set of children for us to look at
                        _toevaluate.Expand(false, gameState);
                        if (_toevaluate.Children != null)
                        {
                            // Loop through the children of the new tree node that we used
                            for (int i = 0; i < _toevaluate.Children.Length; i++)
                            {
                                _toevaluate.Children[i].AddScore(EvaluateNode(_toevaluate.Children[i], gameState));
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
            return _treeRoot.CPathAdvanceGame((GameState)pGameState.Clone(), pGameState.Pacman.Direction);
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
            _treeRoot = new TreeNode(pGameState,
                                      this,
                                      null,
                                      new TreeNode().CPathAdvanceGame((GameState)pGameState.Clone(), pGameState.Pacman.Direction).Pacman.Node,
                                      new Direction[] { pGameState.Pacman.Direction }); // Generate a new array of directions

            // Return the score value for advancing in the given direction
            _treeRoot.AddScore(EvaluateNode(_treeRoot, pGameState));

            // Expand the tree root so that adjacent junctions are generated
            _treeRoot.Expand(false, pGameState);
        }


        /// <summary>
        /// Return the direction that we want the Pacman agent to go in
        /// </summary>
        /// <param name="gs">The current gamestate</param>
        /// <returns>Direction</returns>
        public override Direction Think(GameState gs)
        {
            _gameState = gs;

            // Only output information to the console if this has been set to true.
            if (!RemainQuiet)
            {
                UpdateConsole();
            }

            // Keep adding this on at every tick
            Milliseconds += GameState.MSPF;

            if (_isAtJunction)
            {
                _isAtJunction = false;
                PrepareRoot(gs);
                int _completedsimulations = 0;
                int _timetaken = 0;
                
                m_MCTSTimeBegin = Environment.TickCount;
                
                while (_completedsimulations < TreeNode.MaxSimulations)
                {
                    RunSimulation(UpdateToRoot((GameState)gs));
                    _completedsimulations++;
                }
                
                m_MCTSTimeEnd = Environment.TickCount;

                // Return the value for the time that has taken to generate it
                _timetaken = m_MCTSTimeEnd - m_MCTSTimeBegin;

                _testStats.MCTSTotalTime += _timetaken;
                _testStats.MCTSTotalGenerations++;

                if (_timetaken > _testStats.MCTSMaximum)
                {
                    _testStats.MCTSMaximum = _timetaken;
                }

                if (_timetaken < _testStats.MCTSMinimum)
                {
                    _testStats.MCTSMinimum = _timetaken;
                }

                // Generate the new average based on the total MCTS generations done
                // and the total amount of time it's taken
                _testStats.MCTSAverage = _testStats.MCTSTotalTime / _testStats.MCTSTotalGenerations;

                OutputLog(string.Format("MCTS Generation Time: {0} ms", _timetaken.ToString()), true, true);


                _previousGameState = gs;
                return Direction.None;
            }
            else if (IsJunction(gs.Pacman.Node.X, gs.Pacman.Node.Y, gs) || _treeRoot.CurrentPosition == gs.Pacman.Node)
            {
                _isAtJunction = true;

                int _completedsimulations = 0;
                int _timetaken = 0;
                m_MCTSTimeBegin = Environment.TickCount;

                while (_completedsimulations < TreeNode.MaxSimulations)
                {
                    RunSimulation((GameState)gs);
                    _completedsimulations++;
                }

                m_MCTSTimeEnd = Environment.TickCount;
                _timetaken = m_MCTSTimeEnd - m_MCTSTimeBegin;

                _previousGameState = gs;
                return GetNextDirectionFromTree(_treeRoot, false, gs, SelectionParameter.HighestUcb);
            }
            else
            {
                int _completedsimulations = 0;

                while (_completedsimulations < TreeNode.ShallowSimulations)
                {
                    RunSimulation(UpdateToRoot((GameState)gs));
                    _completedsimulations++;
                }

                _previousGameState = gs;

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
            string _pacmanPosition = string.Format("Pacman: {0},{1}", _gameState.Pacman.Node.X, _gameState.Pacman.Node.Y);

            //m_GameState.Pacman.ImgX.ToString(),m_GameState.Pacman.ImgY.ToString()

            foreach (var ghost in _gameState.Ghosts)
            {
                Console.WriteLine(String.Format("{0}: {1},{2}", ghost.GetType().ToString(),
                                                                ghost.Node.X,
                                                                ghost.Node.Y));
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("PILLS REMAINING: {0}", _gameState.Map.PillNodes.Where(n => n.Type != Node.NodeType.None && n.Type != Node.NodeType.Wall).Count().ToString()));
            Console.WriteLine(string.Format("PILLS LEFT(INT): {0}", _gameState.Map.PillsLeft.ToString()));

            Console.WriteLine("=================== TEST DATA ==============");

            if (_testStats.TotalGames >= MaxTestGames)
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
            Console.WriteLine(string.Format("SESSION ID: {0}", _testSessionId));
            Console.WriteLine(string.Format("MAX PILLS EATEN: {0}", _testStats.MaxPillsTaken));
            Console.WriteLine(string.Format("MIN PILLS EATEN: {0}", _testStats.MinPillsTaken));
            Console.WriteLine(string.Format("GAMES PLAYED: {0}", _testStats.TotalGames));
            Console.WriteLine(string.Format("HIGHEST SCORE: {0}", _testStats.MaxScore));
            Console.WriteLine(string.Format("AVERAGE SCORE: {0}", _testStats.AverageScore));
            Console.WriteLine(string.Format("LOWEST SCORE: {0}", _testStats.MinScore));
            Console.WriteLine(string.Format("MINIMUM GHOSTS EATEN: {0}", _testStats.MinGhostsEaten));
            Console.WriteLine(string.Format("AVERAGE GHOSTS EATEN: {0}", _testStats.AverageGhostsEaten));
            Console.WriteLine(string.Format("MAXIMUM GHOSTS EATEN: {0}", _testStats.MaxGhostsEaten));
            Console.WriteLine(string.Format("MIN MCTS GENERATION TIME: {0}", _testStats.MCTSMinimum));
            Console.WriteLine(string.Format("MAX MCTS GENERATION TIME: {0}", _testStats.MCTSMaximum));
            Console.WriteLine(string.Format("AVERAGE MCTS GENERATION TIME: {0}", _testStats.MCTSAverage));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=================== LOG ====================");
            Console.ForegroundColor = ConsoleColor.White;

            // Loop through the list of messages and output them as well
            for (int i = 0; i < _logOutput.Count; i++)
            {
                Console.WriteLine(_logOutput[i]);
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
            _drawingObject.DrawImage(_redBlock, new Point(50, 50));

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
                _drawingObject.DrawImage(_greenBlock, new Point());

                // Draw the output accordingly.
                pController.TreeRoot.Draw(_drawingObject);
                _drawingObject.DrawImage(_redBlock, new Point(pController.TreeRoot.PathNode.CenterX, pController.TreeRoot.PathNode.CenterY));
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
            _newimage.Save(string.Format("{2}\\{0}_{1}_{3}.bmp", DateTime.Now.ToString("ddMMyyyyHHmmssff"), _filename, pController._testImagesFolder.FullName, pController._testStats.TotalGames));
            _newimage.Dispose();
        }

        public override void Restart(GameState gameState)
        {
            // Don't update the stats more than 100 times.
            // That's only the amount of games that we want simulated.
            if (_testStats.TotalGames < MaxTestGames)
            {
                Utility.SerializeGameState(gameState,this);

                // Save the image to the same directory as the simulator
                SaveStateAsImage(gameState, this, string.Format("endofround_{0}_{1}_",
                                                         _testStats.TotalGames.ToString(),
                                                         this.Name.ToString()),true);

                // Set the stats.
                _testStats.TotalGhostsEaten += gameState._ghostsEaten;
                _testStats.TotalPillsTaken += gameState._pillsEaten;
                _testStats.TotalScore += gameState.Pacman.Score;
                _testStats.TotalLevelsCleared += gameState.Level;
                _testStats.TotalGames++;

                if (Milliseconds - LastRoundMilliseconds > _testStats.LongestRoundTime)
                {
                    _testStats.LongestRoundTime = Milliseconds - LastRoundMilliseconds;
                }

                if (Milliseconds - LastRoundMilliseconds < _testStats.ShortestRoundTime)
                {
                    _testStats.ShortestRoundTime = Milliseconds - LastRoundMilliseconds;
                }

                _testStats.TotalRoundTime += Milliseconds - LastRoundMilliseconds;
                _testStats.AverageRoundTime = _testStats.TotalRoundTime / _testStats.TotalGames;

                LastRoundMilliseconds = Milliseconds;

                /// LEVELS
                if (gameState.Level < _testStats.MinLevelsCleared)
                {
                    this._testStats.MinLevelsCleared = gameState.Level;
                }

                if (gameState.Level > _testStats.MaxLevelsCleared)
                {
                    this._testStats.MaxLevelsCleared = gameState.Level;
                }

                /// SCORE
                if (gameState.Pacman.Score < _testStats.MinScore)
                {
                    _testStats.MinScore = gameState.Pacman.Score;
                }

                if (gameState.Pacman.Score > _testStats.MaxScore)
                {
                    _testStats.MaxScore = gameState.Pacman.Score;
                }

                /// PILLS
                if (gameState._pillsEaten < _testStats.MinPillsTaken)
                {
                    _testStats.MinPillsTaken = gameState._pillsEaten;
                }

                if (gameState._pillsEaten > _testStats.MaxPillsTaken)
                {
                    _testStats.MaxPillsTaken = gameState._pillsEaten;
                }

                /// SCORE DIFFERENCE
                if (gameState._ghostsEaten < _testStats.MinGhostsEaten)
                {
                    _testStats.MinGhostsEaten = gameState._ghostsEaten;
                }

                if (gameState._ghostsEaten > _testStats.MaxGhostsEaten)
                {
                    _testStats.MaxGhostsEaten = gameState._ghostsEaten;
                }
            }
            else
            {
                // Test has terminated, display and save the final results.
                if (!TestComplete)
                {
                    _stopWatch.Stop();
                    _testStats.ElapsedMillisecondsTotal = _stopWatch.ElapsedMilliseconds;

                    _testStats.AveragePillsTaken = _testStats.TotalPillsTaken / _testStats.TotalGames;
                    _testStats.AverageScore = _testStats.TotalScore / _testStats.TotalGames;
                    _testStats.AverageGhostsEaten = _testStats.TotalGhostsEaten / _testStats.TotalGames;
                    _testStats.AverageLevelsCleared = _testStats.TotalLevelsCleared / _testStats.TotalGames;

                    SerializeTestStats(_testStats);
                    TestComplete = true;
                }
            }
            
            base.Restart(gameState);
        }

        /// <summary>
        /// Output a message to the log with various arguments
        /// </summary>
        /// <param name="pLogMessage">Message to be displayed</param>
        /// <param name="pVerbose">Is this to be written to the console window?</param>
        /// <param name="pDisplayDate">Display the date with the text log message?</param>
        public virtual void OutputLog(string logMessage, bool verbose, bool displayDate)
        {
            StreamWriter _writer = new StreamWriter(string.Format("{0}\\output_{1}.txt", _testLogFolder.FullName.ToString(), DateTime.Now.ToString("ddMMyyyy")), true);
            string _currentdate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss ff");
            string _output;

            // Determine whether or not we want to display the date.
            if (displayDate)
                _output = string.Format("[{0}]: {1}", _currentdate, logMessage);
            else
                _output = logMessage;

            _writer.WriteLine(_output);

            // Make sure whether or not adding one more message would cause problems.

            if (verbose && !RemainQuiet)
            {
                if (_logOutput.Count + 1 > MaxLogItemsDisplay)
                {
                    _logOutput.Remove(_logOutput.Last());
                }

                // Insert the message to the front
                _logOutput.Insert(0, _output);
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
        private Direction TryGoDirection(GameState gs, Direction newDirection)
        {
            var _directions = gs.Pacman.PossibleDirections();

            // Determine whether or not we are able to go in that direction
            if (_directions.Contains(newDirection))
            {
                return newDirection;
            }
            else
            {
                // Just return the first that is not the inverse of the direction
                // that we are aiming to go in
                foreach (Direction direction in _directions)
                {
                    if (GameState.InverseDirection(direction) != newDirection)
                    {
                        return direction;
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
        public static bool IsJunction(int x, int y, GameState gameState)
        {
            // Check that the coordinates are valid
            if (x < gameState.Map.Nodes.GetLength(0) && x > 0 &&
                y < gameState.Map.Nodes.GetLength(1) && y > 0)
            {
                return gameState.Map.Nodes[x, y].PossibleDirections.Count > 2;
            }

            return false;
        }

        public override void EatenByGhost()
        {
            base.EatenByGhost();
        }

        public override void Draw(Graphics g)
        {
            _graphicsDevice = g;

            // If a junction has been found, then draw a line
            if (_junction != null)
            {
                g.DrawLine(Pens.Green, new Point(_gameState.Pacman.Node.CenterX, _gameState.Pacman.Node.CenterY),
                                       new Point(_junction.CenterX, _junction.CenterY));
            }

            // Draw the debug output if the tree has been generated.
            if (_treeRoot != null)
            {
                _treeRoot.Draw(g);

                g.DrawImage(_redBlock, new Point(_treeRoot.PathNode.CenterX - 2, _treeRoot.PathNode.CenterY - 2));

                g.DrawString(_treeRoot.AverageScore.ToString(), new Font(FontFamily.GenericSansSerif, 10f), Brushes.White,
                             _treeRoot.PathNode.CenterX, _treeRoot.PathNode.CenterY);
            }
            //            g.DrawImage(m_GreenBlock, new Point(m_GameState.Pacman.Node.CenterX, m_GameState.Pacman.Node.CenterY));


            base.Draw(g);
        }

    }
}
