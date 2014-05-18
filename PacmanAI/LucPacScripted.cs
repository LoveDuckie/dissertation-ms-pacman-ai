using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


        public static int WANDER_CHANGE_COUNT = 0;
        public static int FLEE_CHANGE_COUNT = 0;
        public static int ENDGAME_CHANGE_COUNT = 0;
        public static int AMBUSH_CHANGE_COUNT = 0;
        public static int HUNT_CHANGE_COUNT = 0;

        // What manhattan distasnce does the AI have to be before we activate the ambush?
        private const int AMBUSH_THRESHOLD = 4;
        private const int FLEE_CHANGE_THRESHOLD = 3;
        private const int FLEE_THRESHOLD = 5; // How far ghosts must be before we resume
        private const int ENDGAME_DISTANCE_THRESHOLD = 4;


        // Used for determining if we're at junctions or any new corner within the map.
        protected List<Direction> m_PreviousPossibleDirections;
        
        protected List<Direction> m_CurrentPossibleDirections;

        public static bool REMAIN_QUIET = false;

        protected FiniteState m_CurrentState;
        protected FiniteState m_PreviousFSMState = FiniteState.Wander;
        // Useful for the likes of MCTS and calling other states
        public static GameState m_GameState;
        public static GameState m_PreviousGameState;

        protected Stopwatch m_RoundDuration = new Stopwatch();

        // The finite state machine in action
        protected Dictionary<FiniteState, State> m_States = new Dictionary<FiniteState,State>();

        // Used by the stop watch to record how long each game round took to complete
        public long m_LastRoundMS = 0;
        public long m_MS = 0;
        public long m_LastLifeMS = 0;

        public const int MAX_LOG_ITEMS_DISPLAY = 10;
        public List<string> m_LogOutput = new List<string>();
        #endregion

        #region Constructors
        public LucPacScripted()
            : base("LucPacScripted")
        {
            m_CurrentState = FiniteState.Wander;
            m_PreviousFSMState = m_CurrentState;
            
            // Initiate a new object of the test stats.
            m_TestStats = new TestStats();

            #region State Initialization
            /// States that are involved.
            m_States.Add(FiniteState.Wander, new State() 
            { 
                Action = this.Wander, 
                OnSuspend = this.Wander_OnSuspend, 
                OnBegin = this.Wander_OnBegin 
            });
            m_States.Add(FiniteState.Ambush, new State()
            {
                Action = this.Ambush,
                OnSuspend = this.Ambush_OnSuspend,
                OnBegin = this.Ambush_OnBegin
            });
            m_States.Add(FiniteState.EndGame, new State()
            {
                Action = EndGame,
                OnSuspend = EndGame_OnSuspend,
                OnBegin = EndGame_OnBegin
            });
            m_States.Add(FiniteState.Hunt, new State()
            {
                Action = Hunt,
                OnSuspend = Hunt_OnSuspend,
                OnBegin = Hunt_OnBegin
            });
            m_States.Add(FiniteState.Flee, new State()
            {
                Action = Flee,
                OnSuspend = Flee_OnSuspend,
                OnBegin = Flee_OnBegin
            });
            #endregion

            // Create the session ID that will be used for testing 
            this.m_TestSessionID = GenerateSessionID();
            this.m_TestStats.SessionID = m_TestSessionID;

            // Create the directory that the data is going to be stored in 
            m_TestDataFolder = Directory.CreateDirectory(Environment.CurrentDirectory + string.Format("/{0}", m_TestSessionID));
            m_TestImagesFolder = m_TestDataFolder.CreateSubdirectory("images");
            m_TestLogFolder = m_TestDataFolder.CreateSubdirectory("logs");

            instance = this;

            m_RoundDuration.Start();
            m_Stopwatch.Start();
        }
        #endregion

        #region Base Methods
        public override void EatPowerPill()
        {
            OutputLog("Power pill eaten!", true, true);
            ChangeState(FiniteState.Hunt, true, m_GameState);

            base.EatPowerPill();
        }

        public override void EatenByGhost()
        {
            OutputLog("Eaten by a ghost!", true, true);

            SaveStateAsImage(m_GameState, this, "eatenbyghost", true);

            if (m_GameState.Pacman.Lives >= 0)
            {
                Utility.SerializeGameState(m_GameState, this);
            }
            // Change the state to somewhere else
            ChangeState(FiniteState.Wander, true, m_GameState);

            if (m_MS - m_LastLifeMS > m_TestStats.MaxLifeTime)
            {
                m_TestStats.MaxLifeTime = m_MS - m_LastLifeMS;
            }

            if (m_MS - m_LastLifeMS < m_TestStats.MinLifeTime)
            {
                m_TestStats.MinLifeTime = m_MS - m_LastLifeMS;
            }

            m_TestStats.TotalLifeTime += m_MS - m_LastLifeMS;
            m_TestStats.TotalLives++;

            m_TestStats.AverageLifeTime = m_TestStats.TotalLifeTime / m_TestStats.TotalLives;

            m_LastLifeMS = m_MS;

            // Return back to the Wandering state if we haven't already
            ChangeState(FiniteState.Wander, true, m_GameState);
            base.EatenByGhost();
        }
        
        #endregion

        public static void SaveStateAsImage(GameState pGameState, LucPacScripted pController, string pImageName, bool pRenderMCTS)
        {
            if (Visualizer.RenderingImage != null)
            {
                // Clone the image that we are going to be rendering to
                Image _newimage = (Image)Visualizer.RenderingImage.Clone();
                Graphics _drawingObject = Graphics.FromImage(_newimage);
                //          _drawingObject.DrawImage(m_RedBlock, new Point(50, 50));

                // Draw the map, pacman and the ghosts to the image
                pGameState.Map.Draw(_drawingObject);
                pGameState.Pacman.Draw(_drawingObject, Visualizer.RenderingSprites);
                foreach (var item in pGameState.Ghosts)
                {
                    item.Draw(_drawingObject, Visualizer.RenderingSprites);
                }

                string _filename = "";
                if (pImageName != string.Empty)
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
        }


        // For when the game restarts.
        public override void Restart(GameState gs)
        {
            // Don't update the stats more than 100 times.
            // That's only the amount of games that we want simulated.
            if (m_TestStats.TotalGames < MAX_TEST_GAMES)
            {
                Utility.SerializeGameState(gs, this);

                // Save the image to the same directory as the simulator
                SaveStateAsImage(gs, this, string.Format("endofround_{0}_{1}_",
                                                         m_TestStats.TotalGames.ToString(),
                                                         this.Name.ToString()), true);

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
        /// This is ran at every tick to display up to date stats based on the agent.
        /// </summary>
        public override void UpdateConsole()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("================== LUCPAC-SCRIPTED ==================");
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
            Console.WriteLine(string.Format("PREVIOUS STATE: {0}", m_PreviousFSMState.ToString()));
            Console.WriteLine(string.Format("STATE: {0}", m_CurrentState.ToString()));

            Console.ForegroundColor = ConsoleColor.Gray;

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
            // Output the test information as it goes a long
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
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=================== LOG ====================");
            Console.ForegroundColor = ConsoleColor.White;

            // Output the items that have been sorted in the list.
            for (int i = 0; i < m_LogOutput.Count; i++)
            {
                Console.WriteLine(m_LogOutput[i]);
            }
        }

        public override void EatGhost()
        {
            base.EatGhost();
        }

        #region Ambush
        Node _nearestPowerPill = null;
        bool m_GoingToPowerPill = false;
        public const int AMBUSH_DISTANCE_THRESHOLD = 5;
        protected Direction Ambush(object sender, EventArgs e, GameState gs)
        {
            var _ghost = StateInfo.NearestGhost(gs);

            if (_ghost != null)
            {
                if (_ghost.Node.ManhattenDistance(gs.Pacman.Node) <
                    AMBUSH_DISTANCE_THRESHOLD)
                {
                    // Small value that will send the controller towards the power pill
                    m_GoingToPowerPill = true;
                }

            }
           

            // Keep spamming directions if we're still not going to the power pill
            if (!m_GoingToPowerPill)
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
            m_GoingToPowerPill = false;
            return Direction.None;
        }

        protected Direction Ambush_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }
        #endregion

        #region Wander State
        Node _nearestPillNode = null;
        /// <summary>
        /// This is the default state that is entered into when the agent is started
        /// </summary>
        /// <param name="sender">The object that is calling this method</param>
        /// <param name="e">The arguments that are passed along</param>
        /// <param name="gs">The game state object.</param>
        /// <returns>The direction that we want to head in</returns>
        protected Direction Wander(object sender, EventArgs e, GameState gs)
        {
            Ghost _ghost = StateInfo.NearestGhost(gs);
            Node _nearestPill = StateInfo.NearestPill(gs.Pacman.Node, gs).Target;

            // Make sure that we haven't eaten the last pill before progressing!
            if (_nearestPill != null)
            {
                if (_nearestPill.ManhattenDistance(gs.Pacman.Node) > ENDGAME_DISTANCE_THRESHOLD)
                {
                    return ChangeState(FiniteState.EndGame, true, gs);
                }
            }

            /// FLEE
            if (_ghost != null)
            {
                if (_ghost.Node.ManhattenDistance(gs.Pacman.Node) < FLEE_CHANGE_THRESHOLD)
                {
                    return ChangeState(FiniteState.Flee, true, gs);
                }
            }

            /// AMBUSH
            foreach (var item in gs.Map.PillNodes)
            {
                // Determine that we are looking at a power pill, if so then change to Ambush
                if (item.ManhattenDistance(gs.Pacman.Node) < AMBUSH_THRESHOLD && item.Type == Node.NodeType.PowerPill)
                {
                    return ChangeState(FiniteState.Ambush, true, gs);
                }
            }

            /// WANDER
            if (LucPac.IsJunction(gs.Pacman.Node.X, gs.Pacman.Node.Y, gs))
            {
                Direction _bestDirection = Direction.None;
                int _highestPillCount = 0;

                foreach (var item in gs.Pacman.PossibleDirections())
                {
                    // Set the new value if we have found something that is of a higher value.
                    if (LucPac.CountPillsDirection(item, gs) > _highestPillCount)
                    {
                        _highestPillCount = LucPac.CountPillsDirection(item, gs);
                        _bestDirection = item;
                    }
                }

                if (_bestDirection == Direction.None)
                {
                    return TryGoDirection(gs.Pacman.Direction, gs);
                }
                else
                {
                    // Return the best direction to head in based on the pill count
                    return _bestDirection;
                }
            }
            else
            {
                return TryGoDirection(gs.Pacman.Direction,gs);
            }

            return Direction.None;
        }

        /// <summary>
        /// Return whether or not the node in question has hit a wall
        /// </summary>
        /// <param name="pCurrentPosition">The position that we are checking</param>
        /// <param name="pGameState">The state of the game</param>
        /// <param name="pDirection"></param>
        /// <returns>Return whether or not we have actually hit a wall</returns>
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
        private int IsGhostDistance(GameState gs, Direction pDirection, bool pIsDangerous)
        {
            Node _currentNode = gs.Pacman.Node;
            int _distance = 0;

            while (!HitWall(_currentNode, gs, pDirection))
            {
                Node _checknode = gs.Map.GetNodeDirection(_currentNode.X, _currentNode.Y, pDirection);

                if (_checknode == null)
                    return -1;

                _distance++;

                // Loop through all the ghosts in the map and detemrine if they are behind
                foreach (var ghost in gs.Ghosts)
                {
                    if (ghost.Node.X == _checknode.X &&
                        ghost.Node.Y == _checknode.Y)
                    {
                        if (pIsDangerous && !ghost.Fleeing)
                        {
                            return _distance;
                        }
                        else if (!pIsDangerous)
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
        public void OutputLog(string pLogMessage, bool pVerbose, bool pDisplayDate)
        {
            StreamWriter _writer = new StreamWriter(string.Format("{0}/output_{1}.txt", Environment.CurrentDirectory, DateTime.Now.ToString("ddMMyyyy")), true);
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

        #region State Handling
        protected Direction CallState(FiniteState pState, GameState gs)
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

            return m_States[pState].Action(this, null, gs);

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
                    WANDER_CHANGE_COUNT++;
                break;

                case FiniteState.Ambush:
                    AMBUSH_CHANGE_COUNT++;
                break;

                case FiniteState.EndGame:
                    ENDGAME_CHANGE_COUNT++;
                break;

                case FiniteState.Hunt:
                    HUNT_CHANGE_COUNT++;
                break;

                case FiniteState.Flee:
                    FLEE_CHANGE_COUNT++;
                break;
            }

            // Store the previous FSM state.
            m_PreviousFSMState = m_CurrentState;

            // Call the functions that are defined for the respective states.
            m_States[m_CurrentState].OnSuspend(this, null, gs);
            m_States[pNewState].OnBegin(this, null, gs);
            m_CurrentState = pNewState;
            return CallState(m_CurrentState, gs);
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
                    if (_nearestPill.Target.ManhattenDistance(gs.Pacman.Node) < ENDGAME_DISTANCE_THRESHOLD)
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
                if (m_NearestGhost.Node.ManhattenDistance(gs.Pacman.Node) > FLEE_THRESHOLD)
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

            m_CurrentPossibleDirections = gs.Pacman.PossibleDirections();
            m_GameState = gs;
                _returnDirection = CallState(m_CurrentState, gs);
            m_PreviousGameState = gs;
            m_PreviousPossibleDirections = gs.Pacman.PossibleDirections();

            m_MS += GameState.MSPF;

            if (!REMAIN_QUIET)
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
