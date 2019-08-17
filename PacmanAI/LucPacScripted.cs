using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Diagnostics;

using Pacman.Simulator.Ghosts;
using Pacman.Simulator;

namespace PacmanAI
{
    /// <summary>
    /// Same AI, but with scripted behaviour instead of the MCTS approach
    /// </summary>
    public class LucPacScripted : BasePacman
    {
        #region Propeties
        public static LucPacScripted INSTANCE
        {
            get { return instance; }
            set { instance = value; }
        }
        #endregion

        #region Members
        // The min and max values for the random generation
        public const float RANDOM_INTERVAL_MIN = 5000f;
        public const float RANDOM_INTERVAL_MAX = 7000f;
        
        // A public accessor to the object.
        private static LucPacScripted instance = null;

        public static int WanderChangeCount = 0;
        public static int FleeChangeCount = 0;
        public static int EndgameChangeCount = 0;
        public static int AmbushChangeCount = 0;
        public static int HuntChangeCount = 0;

        // What manhattan distasnce does the AI have to be before we activate the ambush?
        private const int AmbushThreshold = 4;
        private const int FleeChangeThreshold = 3;
        private const int FleeThreshold = 5; // How far ghosts must be before we resume
        private const int EndgameDistanceThreshold = 4;


        // Used for determining if we're at junctions or any new corner within the map.
        protected List<Direction> _previousPossibleDirections;
        
        protected List<Direction> _currentPossibleDirections;

        public static bool RemainQuiet = false;

        protected FiniteState _currentAgentState;
        protected FiniteState _previousAgentState = FiniteState.Wander;
        // Useful for the likes of MCTS and calling other states
        public static GameState _gameState;
        public static GameState _previousGameState;

        protected Stopwatch _roundDuration = new Stopwatch();

        // The finite state machine in action
        protected Dictionary<FiniteState, State> _states = new Dictionary<FiniteState,State>();

        // Used by the stop watch to record how long each game round took to complete
        public long _lastRoundMilliseconds = 0;
        public long _milliseconds = 0;
        public long _lastLifeMilliseconds = 0;

        public const int MaxLogItemsDisplay = 10;
        public List<string> _logOutput = new List<string>();
        #endregion

        #region Constructors
        public LucPacScripted()
            : base("LucPacScripted")
        {
            _currentAgentState = FiniteState.Wander;
            _previousAgentState = _currentAgentState;
            _testStats = new TestStats();

            #region State Initialization
            _states.Add(FiniteState.Wander, new State() 
            { 
                Action = this.Wander, 
                OnSuspend = this.Wander_OnSuspend, 
                OnBegin = this.Wander_OnBegin 
            });
            _states.Add(FiniteState.Ambush, new State()
            {
                Action = this.Ambush,
                OnSuspend = this.Ambush_OnSuspend,
                OnBegin = this.Ambush_OnBegin
            });
            _states.Add(FiniteState.EndGame, new State()
            {
                Action = EndGame,
                OnSuspend = EndGame_OnSuspend,
                OnBegin = EndGame_OnBegin
            });
            _states.Add(FiniteState.Hunt, new State()
            {
                Action = Hunt,
                OnSuspend = Hunt_OnSuspend,
                OnBegin = Hunt_OnBegin
            });
            _states.Add(FiniteState.Flee, new State()
            {
                Action = Flee,
                OnSuspend = Flee_OnSuspend,
                OnBegin = Flee_OnBegin
            });
            #endregion

            // Create the session ID that will be used for testing 
            this._testSessionId = GenerateSessionID();
            this._testStats.SessionID = _testSessionId;

            // Create the directory that the data is going to be stored in 
            _testDataFolder = Directory.CreateDirectory(Environment.CurrentDirectory + string.Format("/{0}", _testSessionId));
            _testImagesFolder = _testDataFolder.CreateSubdirectory("images");
            _testLogFolder = _testDataFolder.CreateSubdirectory("logs");

            instance = this;

            _roundDuration.Start();
            _stopWatch.Start();
        }
        #endregion

        #region Base Methods
        public override void EatPowerPill()
        {
            OutputLog("Power pill eaten!", true, true);
            ChangeState(FiniteState.Hunt, true, _gameState);

            base.EatPowerPill();
        }

        public override void EatenByGhost()
        {
            OutputLog("Eaten by a ghost!", true, true);

            SaveStateAsImage(_gameState, this, "eatenbyghost", true);

            if (_gameState.Pacman.Lives >= 0)
            {
                Utility.SerializeGameState(_gameState, this);
            }
            // Change the state to somewhere else
            ChangeState(FiniteState.Wander, true, _gameState);

            if (_milliseconds - _lastLifeMilliseconds > _testStats.MaxLifeTime)
            {
                _testStats.MaxLifeTime = _milliseconds - _lastLifeMilliseconds;
            }

            if (_milliseconds - _lastLifeMilliseconds < _testStats.MinLifeTime)
            {
                _testStats.MinLifeTime = _milliseconds - _lastLifeMilliseconds;
            }

            _testStats.TotalLifeTime += _milliseconds - _lastLifeMilliseconds;
            _testStats.TotalLives++;

            _testStats.AverageLifeTime = _testStats.TotalLifeTime / _testStats.TotalLives;

            _lastLifeMilliseconds = _milliseconds;

            // Return back to the Wandering state if we haven't already
            ChangeState(FiniteState.Wander, true, _gameState);
            base.EatenByGhost();
        }
        
        #endregion

        public static void SaveStateAsImage(GameState gameState, LucPacScripted controller, string imageName, bool renderMcts)
        {
            if (Visualizer.RenderingImage != null)
            {
                // Clone the image that we are going to be rendering to
                Image _newimage = (Image)Visualizer.RenderingImage.Clone();
                Graphics _drawingObject = Graphics.FromImage(_newimage);
                //          _drawingObject.DrawImage(m_RedBlock, new Point(50, 50));

                // Draw the map, pacman and the ghosts to the image
                gameState.Map.Draw(_drawingObject);
                gameState.Pacman.Draw(_drawingObject, Visualizer.RenderingSprites);
                foreach (Ghost ghost in gameState.Ghosts)
                {
                    ghost.Draw(_drawingObject, Visualizer.RenderingSprites);
                }

                string _fileName;
                if (imageName != string.Empty)
                {
                    _fileName = imageName;
                }
                else
                {
                    _fileName = "screengrab";
                }

                // Save the image out so that we can observe it
                _newimage.Save(string.Format("{2}\\{0}_{1}_{3}.bmp", DateTime.Now.ToString("ddMMyyyyHHmmssff"), _fileName, controller._testImagesFolder.FullName, controller._testStats.TotalGames));
                _newimage.Dispose();
            }
        }


        // For when the game restarts.
        public override void Restart(GameState gameState)
        {
            // Don't update the stats more than 100 times.
            // That's only the amount of games that we want simulated.
            if (_testStats.TotalGames < MaxTestGames)
            {
                Utility.SerializeGameState(gameState, this);

                // Save the image to the same directory as the simulator
                SaveStateAsImage(gameState, this, string.Format("endofround_{0}_{1}_",
                                                         _testStats.TotalGames.ToString(),
                                                         this.Name.ToString()), true);

                // Set the stats.
                _testStats.TotalGhostsEaten += gameState._ghostsEaten;
                _testStats.TotalPillsTaken += gameState._pillsEaten;
                _testStats.TotalScore += gameState.Pacman.Score;
                _testStats.TotalLevelsCleared += gameState.Level;
                _testStats.TotalGames++;

                if (_milliseconds - _lastRoundMilliseconds > _testStats.LongestRoundTime)
                {
                    _testStats.LongestRoundTime = _milliseconds - _lastRoundMilliseconds;
                }

                if (_milliseconds - _lastRoundMilliseconds < _testStats.ShortestRoundTime)
                {
                    _testStats.ShortestRoundTime = _milliseconds - _lastRoundMilliseconds;
                }

                _testStats.TotalRoundTime += _milliseconds - _lastRoundMilliseconds;
                _testStats.AverageRoundTime = _testStats.TotalRoundTime / _testStats.TotalGames;

                _lastRoundMilliseconds = _milliseconds;

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
        /// This is ran at every tick to display up to date stats based on the agent.
        /// </summary>
        public override void UpdateConsole()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("================== LUCPAC-SCRIPTED ==================");
            Console.ForegroundColor = ConsoleColor.Gray;
            string _pacmanPosition = string.Format("Pacman: {0},{1}", _gameState.Pacman.Node.X, _gameState.Pacman.Node.Y);

            //m_GameState.Pacman.ImgX.ToString(),m_GameState.Pacman.ImgY.ToString()

            foreach (var ghost in _gameState.Ghosts)
            {
                Console.WriteLine(string.Format("{0}: {1},{2}", ghost.GetType().ToString(),
                                                                ghost.Node.X,
                                                                ghost.Node.Y));
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("PILLS REMAINING: {0}", _gameState.Map.PillNodes.Where(n => n.Type != Node.NodeType.None && n.Type != Node.NodeType.Wall).Count().ToString()));
            Console.WriteLine(string.Format("PILLS LEFT(INT): {0}", _gameState.Map.PillsLeft.ToString()));
            Console.WriteLine(string.Format("PREVIOUS STATE: {0}", _previousAgentState.ToString()));
            Console.WriteLine(string.Format("STATE: {0}", _currentAgentState.ToString()));

            Console.ForegroundColor = ConsoleColor.Gray;

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
            // Output the test information as it goes a long
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
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=================== LOG ====================");
            Console.ForegroundColor = ConsoleColor.White;

            // Output the items that have been sorted in the list.
            for (int i = 0; i < _logOutput.Count; i++)
            {
                Console.WriteLine(_logOutput[i]);
            }
        }

        public override void EatGhost()
        {
            base.EatGhost();
        }

        #region Ambush
        Node _nearestPowerPill = null;
        bool _goingToPowerPill = false;
        public const int AmbushDistanceThreshold = 5;
        protected Direction Ambush(object sender, EventArgs e, GameState gs)
        {
            var _ghost = StateInfo.NearestGhost(gs);

            if (_ghost != null)
            {
                if (_ghost.Node.ManhattenDistance(gs.Pacman.Node) <
                    AmbushDistanceThreshold)
                {
                    // Small value that will send the controller towards the power pill
                    _goingToPowerPill = true;
                }

            }
           

            // Keep spamming directions if we're still not going to the power pill
            if (!_goingToPowerPill)
            {
                return Direction.Stall;
            }
            else
            {
                StateInfo.PillPath _path = StateInfo.NearestPowerPill(gs.Pacman.Node, gs);
                // Make sure that we are dealing with something legit here
                if (_path != null && _path.PathInfo != null)
                {
                    return _path.PathInfo.Direction;
                }
                else
                {
                    return Direction.Stall;
                }
            }
        }

        protected Direction Ambush_OnBegin(object sender, EventArgs e, GameState gs)
        {
            _nearestPowerPill = null;
            _goingToPowerPill = false;
            return Direction.None;
        }

        protected Direction Ambush_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }

        #endregion
        #region Wander State
        /// <summary>
        /// This is the default state that is entered into when the agent is started
        /// </summary>
        /// <param name="sender">The object that is calling this method</param>
        /// <param name="e">The arguments that are passed along</param>
        /// <param name="gs">The game state object.</param>
        /// <returns>The direction that we want to head in</returns>
        protected Direction Wander(object sender, EventArgs e, GameState gameState)
        {
            Ghost _ghost = StateInfo.NearestGhost(gameState);
            Node _nearestPill = StateInfo.NearestPill(gameState.Pacman.Node, gameState).Target;

            // Make sure that we haven't eaten the last pill before progressing!
            if (_nearestPill != null)
            {
                if (_nearestPill.ManhattenDistance(gameState.Pacman.Node) > EndgameDistanceThreshold)
                {
                    return ChangeState(FiniteState.EndGame, true, gameState);
                }
            }

            /// FLEE
            if (_ghost != null)
            {
                if (_ghost.Node.ManhattenDistance(gameState.Pacman.Node) < FleeChangeThreshold)
                {
                    return ChangeState(FiniteState.Flee, true, gameState);
                }
            }

            /// AMBUSH
            foreach (Node item in gameState.Map.PillNodes)
            {
                // Determine that we are looking at a power pill, if so then change to Ambush
                if (item.ManhattenDistance(gameState.Pacman.Node) < AmbushThreshold && item.Type == Node.NodeType.PowerPill)
                {
                    return ChangeState(FiniteState.Ambush, true, gameState);
                }
            }

            /// WANDER
            if (LucPac.IsJunction(gameState.Pacman.Node.X, gameState.Pacman.Node.Y, gameState))
            {
                Direction _bestDirection = Direction.None;
                int _highestPillCount = 0;

                foreach (Direction item in gameState.Pacman.PossibleDirections())
                {
                    // Set the new value if we have found something that is of a higher value.
                    if (LucPac.CountPillsDirection(item, gameState) > _highestPillCount)
                    {
                        _highestPillCount = LucPac.CountPillsDirection(item, gameState);
                        _bestDirection = item;
                    }
                }

                if (_bestDirection == Direction.None)
                {
                    return TryGoDirection(gameState.Pacman.Direction, gameState);
                }
                else
                {
                    // Return the best direction to head in based on the pill count
                    return _bestDirection;
                }
            }
            else
            {
                return TryGoDirection(gameState.Pacman.Direction,gameState);
            }
        }

        /// <summary>
        /// Return whether or not the node in question has hit a wall
        /// </summary>
        /// <param name="currentPosition">The position that we are checking</param>
        /// <param name="pGameState">The state of the game</param>
        /// <param name="pDirection"></param>
        /// <returns>Return whether or not we have actually hit a wall</returns>
        public static bool HitWall(Node currentPosition,
                                   GameState gameState,
                                   Direction direction)
        {
            if (gameState is null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }
            // Loop through the possible directions at the give node
            // If a direction is the same as the one that Pacman is going in
            // then we've hit a wall
            foreach (Direction item in Node.GetAllPossibleDirections(currentPosition))
            {
                if (item == direction)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Return whether or not there is a ghost within the provided direction.
        /// 
        /// If so, then return
        /// </summary>
        /// <param name="gs">GameState object to be used</param>
        /// <param name="pDirection">The direction that we are checking in</param>
        /// <returns>Return whether or not the ghost is in the given direction</returns>
        private bool IsGhostDirection(GameState gs, Direction pDirection, bool pIsDangerous)
        {
            Node _currentNode = gs.Pacman.Node;

            while (!HitWall(_currentNode, gs, pDirection))
            {
                Node _checknode = gs.Map.GetNodeDirection(_currentNode.X, _currentNode.Y, pDirection);

                if (_checknode == null)
                    return false;

                // Loop through all the ghosts in the map and detemrine if they are behind
                foreach (var ghost in gs.Ghosts)
                {
                    if (ghost.Node.X == _checknode.X &&
                        ghost.Node.Y == _checknode.Y)
                    {
                        // This part could perhaps be consolidated some how
                        if (pIsDangerous)
                        {
                            if (!ghost.Fleeing)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }

                _currentNode = _checknode;
            }

            return false;
        }

        /// <summary>
        /// Return the distance in which the ghost is to the Ms. Pacman agent.
        /// </summary>
        /// <param name="gs">The GameState object.</param>
        /// <param name="pDirection">The direction that we are checking in.</param>
        /// <returns>Returns the distance in which the ghost is in a given direction</returns>
        private int IsGhostDistance(GameState gameState, Direction direction, bool isDangerous)
        {
            Node _currentNode = gameState.Pacman.Node;
            int _distance = 0;

            while (!HitWall(_currentNode, gameState, direction))
            {
                Node _checknode = gameState.Map.GetNodeDirection(_currentNode.X, _currentNode.Y, direction);

                if (_checknode == null)
                    return -1;

                _distance++;

                // Loop through all the ghosts in the map and detemrine if they are behind
                foreach (Ghost ghost in gameState.Ghosts)
                {
                    if (ghost.Node.X == _checknode.X &&
                        ghost.Node.Y == _checknode.Y)
                    {
                        if (isDangerous && !ghost.Fleeing)
                        {
                            return _distance;
                        }
                        else if (!isDangerous)
                        {
                            return _distance;
                        }
                    }
                }

                _currentNode = _checknode;
            }

            // Return the distance that we are to be using.
            return 0;
        }

        /// <summary>
        /// Output the message to perhaps both the console and the text file at teh same time
        /// </summary>
        /// <param name="pLogMessage">The message that we want to emit</param>
        /// <param name="pVerbose">Whether or not we want to display it as a console message</param>
        /// <param name="pDisplayDate">Output a time stamp?</param>
        public void OutputLog(string logMessage, bool verbose, bool displayDate)
        {
            StreamWriter _writer = new StreamWriter(string.Format("{0}/output_{1}.txt", Environment.CurrentDirectory, DateTime.Now.ToString("ddMMyyyy")), true);
            string _currentdate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss ff");
            string _output = "";

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

        #region State Handling
        protected Direction CallState(FiniteState state, GameState gameState)
        {
            #region Old Code
            //Type _type = this.GetType();
            //MethodInfo _methodinfo;

            //_methodinfo = _type.GetMethod(pState.ToString());

            //// Determine first that there is a method for the given enumeration that we are after.
            //if (_methodinfo != null)
            //{
            //    return (Direction)_methodinfo.Invoke(this, new object[] { this, null, gs });
            //}
            //else
            //{
            //    return Direction.Left;
            //}

            // this.m_States[pState].Action.Invoke(this, new object[] { null, null, null },null);
            #endregion

            return _states[state].Action(this, null, gameState);

        }

        // Update the state and lead the output know about it
        public Direction ChangeState(FiniteState pNewState, bool pLog, GameState gs)
        {
            if (pLog)
            {
                OutputLog(string.Format("New State: {0}", pNewState.ToString()), true, true);
            }

            // Based on the counts from the state.
            switch (pNewState)
            {
                case FiniteState.Wander:
                    WanderChangeCount++;
                break;

                case FiniteState.Ambush:
                    AmbushChangeCount++;
                break;

                case FiniteState.EndGame:
                    EndgameChangeCount++;
                break;

                case FiniteState.Hunt:
                    HuntChangeCount++;
                break;

                case FiniteState.Flee:
                    FleeChangeCount++;
                break;
            }

            // Store the previous FSM state.
            _previousAgentState = _currentAgentState;

            // Call the functions that are defined for the respective states.
            _states[_currentAgentState].OnSuspend(this, null, gs);
            _states[pNewState].OnBegin(this, null, gs);
            _currentAgentState = pNewState;
            return CallState(_currentAgentState, gs);
        }
        #endregion

        /// <summary>
        /// Called when the state is changed to Wandering for the first time
        /// </summary>
        /// <param name="sender">Where this function is being called from</param>
        /// <param name="e">The event arguments</param>
        /// <param name="gs">The current game state</param>
        /// <returns></returns>
        public Direction Wander_OnBegin(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }

        /// <summary>
        /// Called when the state is being changed away from the wandering state
        /// </summary>
        /// <param name="sender">Where this function is being called from</param>
        /// <param name="e">The arguments that are to compliment the method</param>
        /// <param name="gs">The game state that tells the agent the current state of the game</param>
        /// <returns>Return the direction that we want to go in</returns>
        public Direction Wander_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }
        #endregion

        #region EndGame State
        /// <summary>
        /// Called when the EndGame behaviour is to begin
        /// </summary>
        /// <param name="sender">Where this method was called from (which object)</param>
        /// <param name="e"></param>
        /// <param name="gs">The game state that we're working with</param>
        /// <returns>Returns the low-level direction of where we want to head in</returns>
        public Direction EndGame_OnBegin(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }

        public Direction EndGame_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }

        /// <summary>
        /// Activated when there are not many pills within the maze environment.
        /// </summary>
        /// <param name="sender">The object that called the argument in question</param>
        /// <param name="e">The arguments that are added in at the same time</param>
        /// <param name="gs">The game state that we want to use</param>
        /// <returns>Returns one of 6 possible directions that the agent can respond to.</returns>
        public Direction EndGame(object sender, EventArgs e, GameState gs)
        {
            StateInfo.PillPath _nearestPill = StateInfo.NearestPill(gs.Pacman.Node, gs);

            // Return the appropriate direction to take to get othe path
            if (_nearestPill != null)
            {
                if (_nearestPill.Target != null)
                {
                    if (_nearestPill.Target.ManhattenDistance(gs.Pacman.Node) < EndgameDistanceThreshold)
                    {
                        return ChangeState(FiniteState.Wander, true, gs);
                    }
                }

                if (_nearestPill.PathInfo != null)
                {
                    return _nearestPill.PathInfo.Direction;
                }
            }

            return TryGoDirection(gs.Pacman.Direction, gs);
        }
        #endregion

        #region Hunt State
        public Direction Hunt(object sender, EventArgs e, GameState gs)
        {
            // Get the nearest ghost in the game.
            Ghost _nearestGhost = StateInfo.NearestEdibleGhost(gs);

            if (_nearestGhost != null)
            {
                // Determine first that it's safe to cahse them
                if (_nearestGhost.Fleeing
                    && _nearestGhost.Entered)
                {
                    Node.PathInfo _path = gs.Pacman.Node.ShortestPath[_nearestGhost.Node.X, _nearestGhost.Node.Y];

                    if (_path != null)
                    {
                        return _path.Direction;
                    }
                    else
                    {
                        return Direction.None;
                    }
                }
            }
            else
            {
                return ChangeState(FiniteState.Wander, true, gs);
            }

            return ChangeState(FiniteState.Wander, true, gs);
        }

        public Direction Hunt_OnBegin(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }

        public Direction Hunt_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }
        #endregion

        #region Flee State
        public Direction Flee_OnBegin(object sender, EventArgs e, GameState gs)
        {
            m_NearestGhost = null;
            return Direction.None;
        }

        public Direction Flee_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }

        private Ghost m_NearestGhost = null;

        /// <summary>
        /// Check ahead in a given direction to see if there is imminent danger (ghost)
        /// </summary>
        /// <param name="pDirection">The direction that we are looking in</param>
        /// <param name="pGameState">The current game state</param>
        /// <returns>Returns whether or not there is immediate danger.</returns>
        private bool CheckAhead(Direction pDirection, GameState pGameState)
        {
            Node _currentNode = pGameState.Pacman.Node;

            // Keep looping until we can determining that there is a ghost at a provided position
            do
            {
                _currentNode = _currentNode.GetNeighbour(pDirection);
                if (pGameState.GhostIsAt(_currentNode.X, _currentNode.Y))
                    return true;
            }
            while (!LucPac.HitWall(_currentNode, pGameState, pDirection));

            return false;
        }

        /// <summary>
        /// Return the distance at which the ghost would be at if it's detected
        /// </summary>
        /// <param name="pDirection">The direction that we are looking in</param>
        /// <param name="pGameState">The game state taht we are using</param>
        /// <returns>Returns the distance value.</returns>
        private float CheckAheadDistance(Direction pDirection, GameState pGameState)
        {
            Node _currentNode = pGameState.Pacman.Node;

            do
            {
                _currentNode = _currentNode.GetNeighbour(pDirection);
                if (pGameState.GhostIsAt(_currentNode.X, _currentNode.Y))
                    return 0f;
                    //return pGameState.Pacman.;
            }
            while (!LucPac.HitWall(_currentNode, pGameState, pDirection));
            return -1f;
        }

        /// <summary>
        /// Simplified version of the function above that is used for determining whether
        /// or not there is a ghost ahead in the direction that is provided.
        /// </summary>
        /// <param name="pGameState">The gamestate that we are analysing</param>
        /// <returns>Returns whether or not there is a ghost ahead in the provided direction</returns>
        private bool CheckAhead(GameState pGameState)
        {
            return CheckAhead(pGameState.Pacman.Direction, pGameState);
        }

        public Direction Flee(object sender, EventArgs e, GameState gs)
        {
            m_NearestGhost = StateInfo.NearestGhost(gs);

            if (m_NearestGhost != null)
            {
                // Determine whether or not the nearest ghost is further away than the flee threshold
                if (m_NearestGhost.Node.ManhattenDistance(gs.Pacman.Node) > FleeThreshold)
                {
                    return ChangeState(FiniteState.Wander, true, gs);
                }
            }
            else
            {
                return ChangeState(FiniteState.Wander, true, gs);
            }

            // Make sure to follow in the same direction
            return TryGoDirection(m_NearestGhost.Direction,gs);
        }
        #endregion

        /// <summary>
        /// Attempt to go in the provided direction otherwise return one else.
        /// </summary>
        /// <param name="pDirection">Direction we are attempting to go in</param>
        /// <returns>The actual direction that can be completed.</returns>
        public Direction TryGoDirection(Direction pDirection,GameState gs)
        {
            List<Direction> _possibleDirections = gs.Pacman.PossibleDirections();

            // Loop through the possible directions and determine which is
            // applicable.
            foreach (var item in _possibleDirections)
            {
                /// Determine whether this is an applicable route
                if (item != GameState.InverseDirection(gs.Pacman.Direction))
                {
                    return item;
                }
            }

            return Direction.None;
        }

        /// <summary>
        /// The entry point for the controller and called on every tikc
        /// </summary>
        /// <param name="gs">The game state that we are going to be focusing on</param>
        /// <returns>Returns the direction that the Pacman agent is meant to be going in</returns>
        public override Direction Think(Pacman.Simulator.GameState gs)
        {
            Direction _returnDirection = Direction.None;

            _currentPossibleDirections = gs.Pacman.PossibleDirections();
            _gameState = gs;
                _returnDirection = CallState(_currentAgentState, gs);
            _previousGameState = gs;
            _previousPossibleDirections = gs.Pacman.PossibleDirections();

            _milliseconds += GameState.MSPF;

            if (!RemainQuiet)
            {
                UpdateConsole();
            }

            return _returnDirection;
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
        }

        public override void Draw(Graphics g, int[] danger)
        {
            base.Draw(g, danger);
        }

    }
}
