using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;

using Pacman.Simulator;
using Pacman.Simulator.Ghosts;

using Newtonsoft.Json;

namespace PacmanAI
{
    // States to be active within the simulation.
    public enum FiniteState
    {
        Flee = 1,
        Hunt,
        Wander,
        Ambush,
        EndGame,
        EndGameEnhanced
    }

    public enum SelectionParameter
    {
        MostVisited = 1,
        HighestUcb,
        UcbAndMv
    }

    // Delegate declaration used for our Finite State Methods
    public delegate Direction StateEventHandler (object sender, EventArgs e, GameState gs);

    //  
    public delegate void StateOperation(object sender, EventArgs e, GameState gs);

    // The methods to be stored in our dictionary full of methods.
    [Serializable]
    public struct State
    {
        private FiniteState stateType;

        public FiniteState StateType { get => stateType; set => stateType = value; }

        // Called every tick
        public StateEventHandler Action { get; set; }
        // Called when the state is meant to be terminated
        public StateEventHandler OnSuspend { get; set; }
        // Called when the state has begun
        public StateEventHandler OnBegin { get; set; }
    }

    /** Used for the MCTS simulations **/
    [Serializable]
    public class TreeNode
    {
        #region Constants
        /// <summary>
        /// 
        /// </summary>
        private const float BalancePar = 100000f;

        #region Previous Configuration
        /** What does the visit count have to be before we expand the child? **/
        public const int EXPANSION_THRESHOLD = 3; // Used for determining how many branches that we want

        public const int LAYER_THRESHOLD = 7;

        /** Points given for completing the level **/
        public const int COMPLETE_REWARD = 1000;

        /** How much do we penalize the scoring? **/
        public const int DEATH_PENALTY = 10000;

        /** How many times do we want to randomly simulate the cycles? **/
        public const int MAX_CYCLES = 5;
        #endregion

        #region Current Configuration
        ///** What does the visit count have to be before we expand the child? **/
        //public const int EXPANSION_THRESHOLD = 15; // Used for determining how many branches that we want

        ///** How many layers in total can there be? **/
        //public const int LAYER_THRESHOLD = 12;

        ///** Points given for completing the level **/
        //public const int COMPLETE_REWARD = 10000;

        ///** How much do we penalize the scoring? **/
        //public const int DEATH_PENALTY = 10000;

        ///** How many times do we want to randomly simulate the cycles? **/
        //public const int MAX_CYCLES = 5;
        #endregion

        #region LucPacMCTS

        ///** What does the visit count have to be before we expand the child? **/
        //public const int EXPANSION_THRESHOLD = 5; // Used for determining how many branches that we want

        //public const int LAYER_THRESHOLD = 7;

        ///** Points given for completing the level **/
        //public const int COMPLETE_REWARD = 1000;

        ///** How much do we penalize the scoring? **/
        //public const int DEATH_PENALTY = 10000;

        ///** How many times do we want to randomly simulate the cycles? **/
        //public const int MAX_CYCLES = 5;
        #endregion

        /** How long we wait for simulation before cutting it off?**/
        public const float MaxTickRate = 39f; // At which point shall we stop simulations?
        
        public const int MaxSimulations = 7;
        public const int ShallowSimulations = 3;
        #endregion

        #region Members
        /** MCTS tree nodes and children that are relevant to this one. **/
        private TreeNode[] _children = null;
        private TreeNode _parent = null;

        /** The node within the maze **/
        private Node _currentPosition;

        // The layer that the children is on.
        public int m_Layer = 0;
        
        // The game state that we are focusing on
        protected GameState _currentGameState;     
        protected BasePacman _pacMan; // The current PacMan AI
        private Direction[] _directions; // A list of directions take

        // The amount of times that the node has been visited
        protected int _sampleSize = 0;
        protected double _averageScore = 0f;
        protected float UCB = 0f; // The rating of the pathnode and how successful it is
        #endregion

        #region Properties
        public TreeNode[] Children
        {
            get { return _children; }
            set { _children = value; }
        }

        public Direction[] Directions
        {
            get { return _directions; }
            set { _directions = value; }
        }

        public Node CurrentPosition
        {
            get { return _currentPosition; }
            set { _currentPosition = value; }
        }
        public double AverageScore
        {
            get { return _averageScore; }
            set { _averageScore = value; }
        }

        public int SampleSize
        {
            get { return _sampleSize; }
            set { _sampleSize = value; }
        }

        public Node PathNode
        {
            get { return _currentPosition; }
            set { _currentPosition = value; }
        }
        #endregion

        #region Constructors
        public TreeNode()
        {
            this._parent = null;
            this._currentGameState = null;
            this._pacMan = null;
        }

        /// <summary>
        /// The main constructor and entry point for the MCTS
        /// </summary>
        /// <param name="pGameState">The gamestate as is before entry</param>
        /// <param name="pPacMan">The controller that is using this</param>
        /// <param name="pParent">The parent node of this one</param>
        /// <param name="pCurrentPosition">The position of this node</param>
        public TreeNode(GameState gameState, BasePacman pacMan, TreeNode parent, Node currentPosition, Direction[] directions)
        {
            // Assign values;
            _currentGameState = gameState;
            _pacMan = pacMan;
            _currentPosition = currentPosition;
            _directions = directions; // Store the member variables that are going to be used.

            if (parent == null)
            {
                // Set up a new parent object to be used.
                this._parent = new TreeNode();
                _parent.SampleSize = 0;
                _parent.AverageScore = 0;
            }
            else
            {
                this._parent = parent;
            }
        }
        #endregion


        /// <summary>
        /// Find the next junction from the given position by using the provided direction
        /// </summary>
        /// <param name="pCurrentPosition">Current node in the maze</param>
        /// <param name="pGameState">The state of the game</param>
        /// <param name="pDirection">The direction in which we are going to be moving</param>
        /// <returns>Return the node of the next junction within the provided direction</returns>
        public Node GetNextJunction(Node currentPosition, GameState gameState, Direction direction)
        {
            Node _toreturn = currentPosition;

            // Use this instead and lose the reference dependency.
            gameState.Map.GetNodeDirection(_toreturn.X, _toreturn.Y, direction);

            // Determine the next appropriate junction on a line
            do
            {
                if (_toreturn.GetNeighbour(direction).Type != Node.NodeType.Wall)
                {
                    _toreturn = _toreturn.GetNeighbour(direction);
                }
                else
                {
                    break;
                }
            }
            while (!LucPac.IsJunction(_toreturn.X, _toreturn.Y, gameState) &&
                   !LucPac.HitWall(_toreturn, gameState, direction));

            return _toreturn;
        }

        /// <summary>
        /// Count the branches that are within the tree in total.
        /// </summary>
        /// <returns>Returns the total layers that are within the tree.</returns>
        public int CountLayers()
        {
            int _count = 0;

            if (_children != null)
            {
                if (_children.Length > 0)
                {
                    _count += 1;

                    foreach (TreeNode item in _children)
                    {
                        if (item.Children != null)
                        {
                            if (item.Children.Length > 0)
                            {
                                _count += item.CountLayers();
                            }
                        }
                        break;
                    }
                }
            }

            return _count;
        }

        /// <summary>
        /// Recursively count the children through the tree.
        /// </summary>
        /// <returns>Return the total number of children.</returns>
        public int CountChildren()
        {
            int _count = 0;

            if (_children != null)
            {
                if (_children.Length > 0)
                {
                    _count = _children.Length;

                    foreach (var item in _children)
                    {
                        _count += item.CountChildren();
                    }
                }
            }

            return _count;
        }

        /// <summary>
        /// Advance the game state one junction in advance of the current Pac-Man position.
        /// 
        /// Alternatively if we hit a wall of a C-Path, then use that.
        /// </summary>
        /// <param name="pGameState">The game state that we are going to be advancing.</param>
        /// <returns>Returns the newly updated game state.</returns>
        public GameState UpdateToRoot(GameState gameState)
        {
            return CPathAdvanceGame((GameState)gameState.Clone(), gameState.Pacman.Direction);
        }

        /// <summary>
        /// Updates the game accordingly.
        /// </summary>
        /// <param name="pGameState">The game state that we are going to be carrying out 
        /// operations on</param>
        /// <returns>Returns the state after it is modified.</returns>
        public GameState UpdateGame(GameState gameState)
        {
            // Store the values that we require
            int _currentlevel = gameState.Level;
            
            // Loop through the directions and update the game state to where we are meant to be 
            // at the moment
            for (int i = 0; i < _directions.Length; i++)
            {
                if (_currentlevel < gameState.Level)
                    break;

                // Carry out the game.
                gameState = CPathAdvanceGame(gameState, _directions[i]);
            }

            return gameState;
        }

        /// <summary>
        /// Keep the game moving based on the current game state
        /// and the direction that they are asked to go in
        /// </summary>
        /// <param name="pGame">The game state that we are going to be simulating</param>
        /// <param name="pDirection">The direction that the pacman controller is going to be heading</param>
        public GameState CPathAdvanceGame(GameState game, Direction direction)
        {
            // The current game over count of the game
            int _gameoverCount = game.m_GameOverCount;
            Point _nodeEntered = new Point(game.Pacman.Node.X,
                                           game.Pacman.Node.Y);
            while (!LucPac.HitWall(game.Pacman.Node, game, direction)
                   && _gameoverCount == game.m_GameOverCount)
            {
                // Do a loop inbetween to determine if there is sa collsiion with a wall or the likes
                // Check to see whether or not there has been a game over state detected.

                bool _oldspotCondition;
                // Prevent the problem that we had originally where it recognized
                // on the second tick that we were still in the same spot.
                if (_nodeEntered.X == game.Pacman.Node.X &&
                    _nodeEntered.Y == game.Pacman.Node.Y)
                {
                    _oldspotCondition = true;
                }
                else
                {
                    _oldspotCondition = false;
                }

                if (LucPac.IsJunction(game.Pacman.Node.X, game.Pacman.Node.Y, game) && !_oldspotCondition)
                    break;

                game.AdvanceGame(direction);
            }

            // LucPac.SaveStateAsImage(pGame, LucPac.INSTANCE, "advancegame");

            // return _gameStateClone();
            return game;
        }

        /// <summary>
        /// This function generates the UCB score that we want to deal with
        /// </summary>
        /// <param name="pUCBScore">The score value that we are using to average out</param>
        public void AddScore(int pUCBScore)
        {
            double _totalscore = _averageScore * _sampleSize;
            _totalscore += pUCBScore;

            // Increase the sample size for averaging out the sore that was generated.
            _sampleSize++;
            _averageScore = _totalscore / _sampleSize;

            UpdateUCB();

            // If this is not the root, then carry out the adding
            // of scores above
            if (_parent != null)
            {
                _parent.AddScore(pUCBScore);
            }
        }

        /// <summary>
        /// Basic function for drawing the debug output of the MCTS branch
        /// </summary>
        /// <param name="g">Used for drawing items</param>
        public void Draw(Graphics g)
        {
            // Make sure that the parent is not null before we attempt this
            if (_parent != null)
            {
                if (_parent.PathNode != null)
                {
                    // Depending on whether or not the path is a bad one, choose the appropriate colour to use.
                    Brush _drawbrush = AverageScore < 0 ? Brushes.Red : Brushes.Green;

                    // Draw a line from this point to another.
                    g.DrawLine(new Pen(_drawbrush, 5f),
                               new Point(this._currentPosition.CenterX, this._currentPosition.CenterY),
                               new Point(_parent.PathNode.CenterX, _parent.PathNode.CenterY));

                    g.DrawImage(LucPac._greenBlock, new Point(this._currentPosition.CenterX - 2, this._currentPosition.CenterY - 2));
                    g.DrawString(AverageScore.ToString(), new Font(FontFamily.GenericSansSerif, 10f), Brushes.White, _currentPosition.CenterX, _currentPosition.CenterY);
                    
                    // Output the last direction that was taken to get to the node that we are after.
                    //g.DrawString(m_Directions[m_Directions.Length - 1].ToString(), new Font(FontFamily.GenericSansSerif, 10f), Brushes.White, m_CurrentPosition.CenterX, m_CurrentPosition.CenterY);
                }    
            }

            // Loop through the children if they exist and draw them accordingly.
            if (_children != null)
            {
                if (_children.Length > 0)
                {
                    // Loop through the children and draw the items
                    for (int i = 0; i < _children.Length; i++)
                    {
                        if (_children[i] != null)
                        {
                            _children[i].Draw(g);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// An enhanced method of evaluating nodes as suggested by Auer et al.
        /// 
        /// Does not make use of a balance par for the sake of optimization
        /// 
        /// TODO: Consider reimplmenting the Balance Par.
        /// </summary>
        public void UpdateUCBTuned()
        {
            if (_parent != null)
            {
                // I think I've fixed it this time.
                UCB = (float)(AverageScore + 
                                  Math.Sqrt(Math.Log(this._parent.SampleSize) / 
                                  this.SampleSize) * 
                                  Math.Min(1/4,Math.Pow(AverageScore,2) - Math.Pow(AverageScore,2) + 
                                  Math.Sqrt(2*Math.Log(_parent.SampleSize)/
                                  this.SampleSize))
                                );
            }
            else
            {
                UCB = 0;
            }
        }

        /// <summary>
        /// Recursive function for updating the UCB1 value
        /// </summary>
        public void UpdateUCB()
        {
            if (this._parent != null)
            {
               UCB = (float)
                       (this.AverageScore +
                        TreeNode.BalancePar *
                        Math.Sqrt(
                        Math.Log(this._parent.SampleSize) / this.SampleSize)
                    );
            }
            else
            {
                UCB = 0;
            }
            
            // Loop through the children and update their upper confidence bounds as well
            if (_children != null)
            {
                // Loop through and recursively update the UCBs
                for (int i = 0; i < _children.Length; i++)
		        {
    			    _children[i].UpdateUCB(); 
		        }
            }
        }
       
        // Recursively traverse through the MCTS to find the child with the best UCB at each layer
        public TreeNode UCT()
        {
            TreeNode _bestchild = null;
            float _bestUCB = float.MinValue;

            // Have arrived at the leaf, stop here
            if (IsLeaf())
                return this;

            // Not sure why it is necessary to have two separate values for this
            // but I will figure this out later.
            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i].UCB > _bestUCB)
                {
                    _bestchild = _children[i];
                    _bestUCB = _children[i].UCB;
                }
            }

            return _bestchild.UCT(); // Keep looping
        }

        // return whether or not this is the last child within the branch.
        public bool IsLeaf()
        {
            return this._children == null || this._children.Length == 0;
        }

        /// <summary>
        /// Expand
        /// </summary>
        /// <param name="pAllowInverseDirection">Allow the branching of tree nodes in the inverse direction</param>
        /// <param name="pGameState">The game state that we are going to be evaluating</param>
        /// <returns>The new array of children that are to be expanding the current node</returns>
        public TreeNode[] Expand(bool allowInverseDirection, GameState gameState)
        {
            // Only do the leg work here if the child array is null
            if (_children == null)
            {
                // Generate a new array of children based on the possible directions of pacman

                List<Direction> _possibleDirections = Node.GetAllPossibleDirections(_currentPosition);

                // Remove the possible direction from the list
                // so that it doesn't cause problems when generating children
                if (!allowInverseDirection)
                {
                    if (_directions.Length > 1)
                    {
                        _possibleDirections.Remove(GameState.InverseDirection(_directions[_directions.Length - 1]));
                    }
                }

                _children = new TreeNode[_possibleDirections.Count];

                for (int i = 0; i < _possibleDirections.Count; i++)
                {
                    Direction[] _newdirections = new Direction[_directions.Length + 1];

                    for (int j = 0; j < _directions.Length; j++)
                    {
                        _newdirections[j] = _directions[j];
                    }

                    _newdirections[_newdirections.Length - 1] = _possibleDirections[i];

                    Node _nextjunction = GetNextJunction(_currentPosition, gameState, _possibleDirections[i]);

                    _children[i] = new TreeNode(gameState, _pacMan, this, _nextjunction, _newdirections);
                
                }
                
            }

            // Return the children
            return _children;
        }       
    }

    [Serializable()]
    public class LucPac : BasePacman, ICloneable
    {
        #region Properties
        public static LucPac Instance
        {
            get { return _instance; }
            set { _instance = value; }
        }

        public Direction CurrentDirection
        {
            get { return _currentDirection; }
            set { _currentDirection = value; }
        }
        #endregion

        #region Members
        // The manhattan distance in which the ghost must be until we can continue with the ambush
        // approach.
        public const int GhostAmbushRange = 5;

        //  
        private const int FleeDistance = 2;

        //  
        public const int SimulateGamesCount = 100;

        // Used by the stop watch to record how long each game round took to complete

        private static LucPac _instance = null;

        private const bool IsDebugging = true;
        
        #region Static Pens
        // Static pen objects that are going to be used for drawing debug lines.
        [NonSerialized()]
        private static Pen RED_PEN = new Pen(Color.Red);
        
        [NonSerialized()]
        private static Pen GREEN_PEN = new Pen(Color.Green);

        [NonSerialized()]
        private static Pen BLUE_PEN = new Pen(Color.Blue);
        #endregion

        // Useful for the likes of MCTS and calling other states
        public static GameState _gameState;
        public static GameState _previousGameState;

        private int _mctsTimeBegin = 0;
        private int _mctsTimeEnd = 0;

        // Don't bother outputting any logs to the console if this is active.
        // Want to make sure that this is false before we display anything
        public static bool RemainQuiet = false;

        protected Stopwatch _roundDuration = new Stopwatch();

        public const int MaxLogItemsDisplay = 10;
        public List<string> _logOutput = new List<string>();

        private Direction _currentDirection;

        public bool _isAtJunction = true;

        //
        protected List<Direction> _previousPossibleDirections;

        //
        protected List<Direction> _currentPossibleDirections;

        //  
        private FiniteState currentState;

        //
        protected FiniteState _previousState = FiniteState.Wander;

        //[field: NonSerializedAttribute()]
        private Graphics _graphicsDevice;

        // The base of the MCTS search tree
        protected TreeNode _treeRoot;

        protected TreeNode _endGameRoot;

        private TreeNode huntRoot;

        // The node within the tree that we are actively looking at.
        private TreeNode _focus;

        public TreeNode TreeRoot
        {
            get { return _treeRoot; }
            set { _treeRoot = value; }
        }

        protected FiniteState CurrentState { get => currentState; set => currentState = value; }
        public TreeNode Focus { get => _focus; set => _focus = value; }
        protected TreeNode HuntRoot { get => huntRoot; set => huntRoot = value; }
        public bool ForceRegeneration { get => _forceRegeneration; set => _forceRegeneration = value; }
        public Ghost NearestGhostWander { get => _nearestGhostWander; set => _nearestGhostWander = value; }

        // How many pills should there be left in the level until we change to the
        // end game state?
        private const int PillCountEndGameThreshold = 25;
        public const int SimulationTimePerTick = 39; // How many milliseconds can we run for before letting go
        private const int AmbushDistanceThreshold = 5;
        
        // The minimum distance that the agent has to be in with the ghosts in order to 
        // to leave the fleeing state.
        public const int FleeChangeThreshold = 5;
        public const int FleeThreshold = 7;
        public const int EndGameDistance = 4;

        // The finite states that are to be used in this
        protected Dictionary<FiniteState, State> _states;

        private readonly Node _junction = null;

        //[field: NonSerializedAttribute()]
        public static Image _greenBlock = null;
        public static Image _redBlock = null;
        public static Image _blueBlock = null;
        #endregion

        #region Constructors
        public LucPac() : base("LucPac") {
            // The finite states that are to be used
            _states = new Dictionary<FiniteState, State>();
            TestComplete = false;

            #region Loading Images
            _greenBlock = Image.FromFile("green_block.png");
            _redBlock = Image.FromFile("red_block.png");
            _blueBlock = Image.FromFile("blue_block.png");
            #endregion

            _roundDuration.Start();

            _instance = this;

            #region Adding States
            // For usage with the MCTS tree.
            _states.Add(FiniteState.EndGameEnhanced, new State()
            {
                StateType = FiniteState.EndGameEnhanced,
                OnBegin = EndGameEnhanced_OnBegin,
                OnSuspend = EndGameEnhanced_OnSuspend,
                Action = EndGameEnhanced
            });

            _states.Add(FiniteState.Wander, new State() 
            { 
                StateType = FiniteState.Wander, 
                OnBegin = Wander_OnBegin, 
                OnSuspend = Wander_OnSuspend, 
                Action = Wander 
            });
            
            _states.Add(FiniteState.Hunt, new State() 
            { 
                StateType = FiniteState.Hunt, 
                OnSuspend = Hunt_OnSuspend, 
                OnBegin = Hunt_OnBegin, 
                Action = Hunt 
            });
            
            _states.Add(FiniteState.Ambush, new State() 
            { 
                Action = Ambush,
                OnBegin = Ambush_OnBegin,
                OnSuspend = Ambush_OnSuspend,
                StateType = FiniteState.Ambush 
            });

            // Activated when the ghosts are adjacent to our controller
            _states.Add(FiniteState.Flee, new State() 
            { 
                Action = Flee, 
                OnSuspend = Flee_OnSuspend, 
                OnBegin = Flee_OnBegin, 
                StateType = FiniteState.Flee 
            });
            
            // Load in the end game state
            _states.Add(FiniteState.EndGame, new State() 
            { 
                Action = EndGame,
                OnSuspend = EndGame_OnSuspend,
                OnBegin = EndGame_OnBegin,
                StateType = FiniteState.EndGame 
            });
            #endregion

            // Create the session ID that will be used for testing 
            this._testSessionId = GenerateSessionID();
            this._testStats.SessionID = _testSessionId;

            // Create the directory that the data is going to be stored in 
            _testDataFolder = Directory.CreateDirectory(Environment.CurrentDirectory + string.Format("/{0}", _testSessionId));
            _testImagesFolder = _testDataFolder.CreateSubdirectory("images");
            _testLogFolder = _testDataFolder.CreateSubdirectory("logs");

            _stopWatch.Start();

            CurrentState = FiniteState.Wander;
            
            OutputLog("======= AI STARTED =======", true, true);
            GameStartTimestamp = DateTime.Now; // For determining how long it took to complete level.
            LifeStartTimestamp = GameStartTimestamp;
        }
        #endregion


        /// <summary>
        /// Save a given game state to an image
        /// </summary>
        /// <param name="pGameState">The game state that we wish to render it to</param>
        /// <param name="pController">The agent controller that is being accessed.</param>
        /// <param name="pImageName">The name of the image that is being saved</param>
        /// <param name="pRenderMCTS">Parameter defining whether or not we want the MCTS tree to be rendered</param>
        public static void SaveStateAsImage(GameState gameState, LucPac controller, string imageName, bool renderMcts)
        {
            //Bitmap _image = new Bitmap(448, 542, g);
            //_image.Save(string.Format("mctsoutcome_{0}.bmp", DateTime.Now.ToString("ddMMyyyyHHmmssff")));

            // Clone the image that we are going to be rendering to
            Image _newimage = (Image)Visualizer.RenderingImage.Clone();
            Graphics _drawingObject = Graphics.FromImage(_newimage);
            _drawingObject.DrawImage(_redBlock, new Point(50, 50));

            // Draw the map, pacman and the ghosts to the image
            gameState.Map.Draw(_drawingObject);
            gameState.Pacman.Draw(_drawingObject, Visualizer.RenderingSprites);
            foreach (Ghost item in gameState.Ghosts)
            {
                item.Draw(_drawingObject, Visualizer.RenderingSprites);
            }

            // Determine whether or not the tree root is valid before
            // continuing


            if (controller.TreeRoot != null)
            {
                _drawingObject.DrawImage(_greenBlock, new Point());

                // Draw the output accordingly.
                controller.TreeRoot.Draw(_drawingObject);
                _drawingObject.DrawImage(_redBlock, new Point(controller.TreeRoot.PathNode.CenterX, controller.TreeRoot.PathNode.CenterY));
            }

            string _fileName;
            if (imageName != "")
            {
                _fileName = imageName;
            }
            else
            {
                _fileName = "screengrab";
            }

            // Save the image out so that we can observe it
            _newimage.Save(string.Format("{2}\\{0}_{1}_{3}.bmp", DateTime.Now.ToString("ddMMyyyyHHmmssff"), _fileName, controller._testImagesFolder.FullName, controller._testStats.TotalGames));
            //_image.Dispose();
            _newimage.Dispose();
        }


        /// <summary>
        /// This is called when the game has restarted.
        /// </summary>
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
        /// Simulate the game for a fixed amount of cycles
        /// </summary>
        /// <param name="pGameState">The game state that we are going to be simulated in</param>
        /// <param name="pController">The controller that will determine the behaviour</param>
        /// <param name="pSteps">The amount of game ticks that we are going to be doing</param>
        /// <returns>The new game state after simulation</returns>
        public static GameState SimulateGame(GameState pGameState, BasePacman pController, int pSteps)
        {
            int _currentlevel = pGameState.Level;
            int _gameoverCount = pGameState.m_GameOverCount;
            GameState _gameStateCloned = (GameState) pGameState.Clone();

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

        /// <summary>
        /// Determine if we have arrived at a L junction based on the X and Y coordinate provided.
        /// </summary>
        /// <param name="pX">The X coordinate</param>
        /// <param name="pY">The Y coordinate</param>
        /// <param name="pGameState">The GameState that we are observing</param>
        /// <returns>Returns whether or not the node at the given coordinates is an L junction</returns>
        public static bool IsLJunction(int pX, int pY, GameState pGameState)
        {
            if (pX < pGameState.Map.Nodes.GetLength(0) && pX > 0 &&
                pY < pGameState.Map.Nodes.GetLength(1) && pY > 0)
            {
                var _possibleDirections = Node.GetAllPossibleDirections(pGameState.Map.Nodes[pX, pY]);

                // Determine first that there are two possible directions 
                // that we can take and then make sure that the current direction
                // that Pacman is going is not available either otherwise that wouldn't
                // make sense.
                if (_possibleDirections.Count == 2 && 
                    !_possibleDirections.Contains(pGameState.Pacman.Direction))
                {
                    return true;                
                }
            }

            return false;
        }

        /// <summary>
        /// Return the next junction in a given direction
        /// </summary>
        /// <param name="pCurrentPosition">The current position of where we are starting our search</param>
        /// <param name="pGameState">The game state that we are searching in</param>
        /// <param name="pDirection">The direction that we are searching in</param>
        /// <returns>Return the node that is considered to be the next junction</returns>
        public static Node GetNextJunction(Node pCurrentPosition, GameState pGameState, Direction pDirection)
        {
            Node _toreturn = null;
            do
            {
                if (_toreturn == null)
                {
                    _toreturn = pCurrentPosition;
                }
                else
                {   // Grab the neighbour within the given direction
                    _toreturn = _toreturn.GetNeighbour(pDirection);
                    
                }
            }
            while (!IsJunction(_toreturn.X, _toreturn.Y, pGameState) &&
                   !HitWall(_toreturn,pGameState,pDirection));

            return _toreturn;
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
        /// Simulate the outcome of the given node and do something about it
        /// </summary>
        /// <param name="pNode">The node that we're carrying out simulations on</param>
        /// <param name="pGameState">The game state that we are cloning</param>
        /// <returns>Return the score that was generated from carrying out simulations on the given node</returns>
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

            // Determine whether or not the pacman got onto the next level.
            if (_currentlevel < pGameState.Level)
            {
                _score += TreeNode.COMPLETE_REWARD;
            }

            _score += (_gamestateCopied.Pacman.Score - pGameState.Pacman.Score);

            return _score;
        }

        // Prepare the root node of the MCTS algorithm that will deal with everything ese
        public void PrepareRoot(GameState pGameState, FiniteState pCurrentState)
        {
            // Grab the score from the performance of the tree node that we are evaluating
            switch (pCurrentState)
            {
                case FiniteState.EndGameEnhanced:

                    break;

                case FiniteState.Wander:
                    _treeRoot = new TreeNode(pGameState,
                          this,
                          null,
                          new TreeNode().CPathAdvanceGame((GameState)pGameState.Clone(), pGameState.Pacman.Direction).Pacman.Node,
                          new Direction[] { pGameState.Pacman.Direction }); // Generate a new array of directions

                    // Return the score value for advancing in the given direction
                    _treeRoot.AddScore(EvaluateNode(_treeRoot, pGameState));

                    // Expand the tree root so that adjacent junctions are generated
                    _treeRoot.Expand(false, pGameState);
                break;
            }

        }

        /// <summary>
        /// Serialize the game state in the form of a JSON string
        /// </summary>
        /// <param name="pGameState">The game state in question</param>
        /// <param name="pOutputConsole">Do we send this to the console?</param>
        /// <returns>The JSON string of the game state so that it can then be serialized back into an object</returns>
        public static string SerializeGameState(GameState pGameState, bool pOutputConsole)
        {
            GameStateSerialize _newoutput = new GameStateSerialize();
            _newoutput.BlueEntered = pGameState.Blue.Entered;
            //_newoutput.BluePosition = string.Format("{0},{1}",pGameState.Blue.X,pGameState.Blue.Y);

            string _output = JsonConvert.SerializeObject(pGameState,Formatting.Indented,
                                new JsonSerializerSettings()
                                {
                                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                                });
            if (pOutputConsole)
                Console.WriteLine(_output);

            return _output;
        }

        /// <summary>
        /// Called at every tick
        /// </summary>
        /// <param name="gs">A copy of the game state instance</param>
        /// <returns>Returns the direction that we want to go in</returns>
        public override Direction Think(GameState gs)
        {
            Direction _returnDirection;
            
            //GameState _temp = ObjectClone.Clone<GameState>(gs);
            
            _currentPossibleDirections = gs.Pacman.PossibleDirections();
            _gameState = gs;
                _returnDirection = _states[CurrentState].Action.Invoke(null,null,gs);
            _previousGameState = gs; // Used for comparing two different gamestates for thing
            _previousPossibleDirections = gs.Pacman.PossibleDirections();

            // Keep adding this up on each tick.
            Milliseconds += GameState.MSPF;            

            if (!RemainQuiet)
            {
                UpdateConsole();
            }
           return _returnDirection;
        }

        /// <summary>
        /// Basic function that will return whether or not the two lists of directions are the same
        /// </summary>
        /// <param name="pDirectionsOne">The first list of directions to compare with</param>
        /// <param name="pDirectionsTwo">The second list of directions to compare with</param>
        /// <returns>
        /// Returns false if the two lists are not the same, therefore we're at a new junction
        /// Returns true if they are the same, therefore "Keep Calm and Carry On"
        /// </returns>
        public static bool CompareDirections(List<Direction> pDirectionsOne, List<Direction> pDirectionsTwo)
        {
            // If they are of varying sizes, then they are definitely not going to be the same.
            if (pDirectionsOne.Count != pDirectionsTwo.Count)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < pDirectionsOne.Count; i++)
                {
                    if (!pDirectionsOne.Contains(pDirectionsTwo[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #region Finite States
        #region Wander
        public virtual Direction Wander_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.Stall;
        }

        public virtual Direction Wander_OnBegin(object sender, EventArgs e, GameState gs)
        {
            // Set this to null so that there is a new pill that has to be searched.
            PillNode = null;
            _isAtJunction = true;

            // Make sure that the previous state wasn't "Wander".
            // Then make the tree regenerate.
            if (_previousState != FiniteState.Wander)
            {
                ForceRegeneration = true;
            }

            return Direction.Stall;
        }

        /// <summary>
        /// Returns the tree node that has the highest visit count and the
        /// UCB score as well
        /// </summary>
        /// <param name="pRootNode">The node that we are going to observe</param>
        /// <returns>Return the tree node that has the highest UCB and SampleCount</returns>
        public TreeNode HighestUCBandMV(TreeNode pRootNode)
        {
            TreeNode _highestUCBandMV = null;

            // Determine whether or not the root node and its children is null
            if (pRootNode != null)
            {
                if (pRootNode.Children != null)
                {
                    foreach (var item in pRootNode.Children)
                    {
                        // If the value is null, just chuck in the first item
                        if (_highestUCBandMV == null)
                        {
                            _highestUCBandMV = item;
                        }
                        else if (_highestUCBandMV.AverageScore < item.AverageScore &&
                                 _highestUCBandMV.SampleSize < item.SampleSize)
                        {
                            _highestUCBandMV = item;
                        }
                    }
                }
            }

            return _highestUCBandMV == null ? pRootNode : _highestUCBandMV;
        }

        public TreeNode HighestUCBTreeNode(TreeNode pRootNode)
        {
            TreeNode _highestUCB = null;

            // Ensure that we are appropriately dealing with the right things
            if (pRootNode != null)
            {
                if (pRootNode.Children != null)
                {
                    for (int i = 0; i < pRootNode.Children.Length; i++)
                    {
                        if (_highestUCB == null)
                        {
                            _highestUCB = pRootNode.Children[i];
                        }
                        else if (pRootNode.Children[i].AverageScore > _highestUCB.AverageScore)
                        {
                            _highestUCB = pRootNode.Children[i];
                        }
                    }
                }
            }

            // Return the tree node that has the highest UCB score
            return _highestUCB;

            // Make sure that the node is not null
            //if (_highestScoring != null)
            //{
            //    Node.PathInfo _pathreturn = _highestScoring.CurrentPosition.ShortestPath[gs.Pacman.Node.X, gs.Pacman.Node.Y];
            //    if (_pathreturn != null)
            //    {
            //        return _pathreturn.Direction;
            //    }
            //}
        }

        /// <summary>
        /// Based on the tree selection parameter, return the direciton in question
        /// </summary>
        /// <param name="pRootNode">The tree node that we aim to extract the next direction from</param>
        /// <param name="pSelectionParameter">The rule the determines how we choose the next direction</param>
        /// <returns>The direction for us to head in</returns>
        public TreeNode GetNextDirection(TreeNode pRootNode, SelectionParameter pSelectionParameter)
        {
            // Determine how we are going to chose the nodes within the list
            switch (pSelectionParameter)
            {
                case SelectionParameter.HighestUcb:
                    return HighestUCBTreeNode(pRootNode);
                    
                case SelectionParameter.MostVisited:
                    return MostVisitedTreeNode(pRootNode);

                case SelectionParameter.UcbAndMv:
                    return HighestUCBandMV(pRootNode);
            }

            return pRootNode;
        }

        /// <summary>
        /// Basic method for returning the most sampled tree node
        /// </summary>
        /// <param name="pTreeNode">Return the tree node that has been sampled the most</param>
        /// <returns>The tree node that has the highet SampleSize</returns>
        public TreeNode MostVisitedTreeNode(TreeNode pTreeNode)
        {
            TreeNode _mostvisited = null;

            // Determine firstr that the object is not null.
            if (pTreeNode != null)
            {
                // Loop through the children
                for (int i = 0; i < pTreeNode.Children.Length; i++)
                {
                    if (_mostvisited == null)
                    {
                        _mostvisited = pTreeNode.Children[i];
                    }
                    else if (_mostvisited.SampleSize < pTreeNode.Children[i].SampleSize)
                    {
                        // Return the value that is the most visited
                        _mostvisited = pTreeNode.Children[i];
                    }
                }
            }

            return _mostvisited;
        }

        /// <summary>
        /// Return whether the highest scoring child in the tree is 0
        /// 
        /// If so, then we can react to that.
        /// </summary>
        /// <param name="m_TreeRoot">The tree root that we are going to be observing</param>
        /// <returns>Returns whether or not the highest scoring child is 0</returns>
        public bool ZeroBasedChildren(TreeNode _treeRoot)
        {
            // Loop through the children and determine the highest scoring child
            foreach (TreeNode child in _treeRoot.Children)
            {
                if (child.AverageScore > 0)
                    return true;
            }

            return false;
        }

        public Node PillNode = null;
        private bool _forceRegeneration = false;
        private Ghost _nearestGhostWander = null;

        protected virtual Direction Wander(object sender, EventArgs e, GameState gameState)
        {
            // Change to the EndGame state so that we find the shortestpath instead.
            //if (gs.Map.PillsLeft < PILL_COUNT_ENDGAME_THRESHOLD)
            //{
            //    return ChangeState(FiniteState.EndGame, true, gs);
            //}

            // Determiner whether or not the highest tree node is either equal to or less than 0
            if (_treeRoot != null)
            {
                Node _nearestPill = StateInfo.NearestPill(gameState.Pacman.Node, gameState).Target;

                if (_nearestPill != null)
                {
                    if (_nearestPill.ManhattenDistance(gameState.Pacman.Node) > EndGameDistance)
                    {
                        return ChangeState(FiniteState.EndGame, true, gameState);
                    }
                }
            }

            // Determine first taht the nearest ghost is not close.
            NearestGhostWander = StateInfo.NearestGhost(gameState);
            if (NearestGhostWander != null)
            {
                if (NearestGhostWander.Node.ManhattenDistance(gameState.Pacman.Node) < FleeChangeThreshold)
                {
                    return ChangeState(FiniteState.Flee, true, gameState);
                }
            }

            #region New Implementation
            PillNode = StateInfo.NearestPowerPill(gameState.Pacman.Node, gameState).Target;
            //// Check to see if there is a power pill nearby.
            if (PillNode != null)
            {
                // Determine the manhattan distance between the node that Pacman is occupying
                // and that of the pill that we are focusing on
                if (PillNode.ManhattenDistance(gameState.Pacman.Node) < AmbushDistanceThreshold)
                {
                    return ChangeState(FiniteState.Ambush, true, gameState);
                }
            }
            #endregion

            #region Old Code
            //// Activated if in the previous frame it mentioned that we were at a junction
            //if (m_IsAtJunction)
            //{
            //    PrepareRoot(gs);
                
            //    m_IsAtJunction = false;

            //    // Only simulate a certain amount of times.
            //    int _completedsimulations = 0;
            //    while (_completedsimulations < TreeNode.MAX_SIMULATIONS)
            //    {
            //        RunSimulation(UpdateToRoot((GameState)gs));
            //        _completedsimulations++;
            //    }

            //    // Save an image of the state to the file
            //    SaveStateAsImage(m_GameState, this, "");

            //    // Carry on as normal if we're heading on a C path
            //    return Direction.None;
            //}
            //else if (IsJunction(gs.Pacman.Node.X, gs.Pacman.Node.Y, gs) && !m_IsAtJunction)
            //{
            //    m_IsAtJunction = true;
            //    if (m_TreeRoot != null)
            //    {
            //        // Only simulate a certain amount of times.
            //        int _completedsimulations = 0;
            //        while (_completedsimulations < TreeNode.MAX_SIMULATIONS)
            //        {
            //            RunSimulation((GameState)gs.Clone());
            //            _completedsimulations++;
            //        }
            //    }

            //    return GetNextDirectionFromTree(m_TreeRoot, false, gs, SelectionParameter.HighestUCB);
            //}
            //else
            //{
            //    // Only simulate a certain amount of times.
            //    int _completedsimulations = 0;
            //    while (_completedsimulations < TreeNode.MAX_SIMULATIONS)
            //    {
            //        RunSimulation(UpdateToRoot((GameState)gs));
            //        _completedsimulations++;
            //    }

            //    return Direction.None;
            //}
            #endregion

            // Has this been set to true from the previous tick.
            if (_isAtJunction)
            {
                _isAtJunction = false;
                
                PrepareRoot(gameState,FiniteState.Wander);
                
                int _completedsimulations = 0;
                int _timetaken = 0;
                
                _mctsTimeBegin = Environment.TickCount;
                
                // Fixed constraint hte simulations.
                // Could otherwise be latency based to ensure fluidity, but
                // we have the luxury of a simulator so MCTS calculations get priority over
                // gameplay code.
                while (_completedsimulations < TreeNode.MaxSimulations)
                {
                    // Advance the game one junction ahead, and then proceed to carry out calculations there.
                    // Alternatively, if there is no junction ahead, then it will just do it based on junctions
                    // with 2 possible directions. That'll occur because the HitWall() condition will be met in the while() loop.
                    RunSimulation(UpdateToRoot((GameState)gameState),FiniteState.Wander);
                    _completedsimulations++;
                }
                
                _mctsTimeEnd = Environment.TickCount;

                // Return the value for the time that has taken to generate it
                _timetaken = _mctsTimeEnd - _mctsTimeBegin;

                _testStats.MCTSTotalTime += _timetaken;
                _testStats.MCTSTotalGenerations++;

                // Take the time take and determine if it exceeds the max
                // or minimum.
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
                return Direction.None;
            }
            else if (IsJunction(gameState.Pacman.Node.X, gameState.Pacman.Node.Y, gameState) || _treeRoot.CurrentPosition == gameState.Pacman.Node)
            {
                _isAtJunction = true;

                int _completedsimulations = 0;
                int _timetaken = 0;
                
                _mctsTimeBegin = Environment.TickCount;
                
                while (_completedsimulations < TreeNode.MaxSimulations)
                {
                    RunSimulation((GameState)gameState,FiniteState.Wander);
                    _completedsimulations++;
                }

                _mctsTimeEnd = Environment.TickCount;
                _timetaken = _mctsTimeEnd - _mctsTimeBegin;


                //ChangeState(FiniteState.EndGame, true, gs);
                
                return GetNextDirectionFromTree(_treeRoot, false, gameState, SelectionParameter.HighestUcb);
            }
            else
            {
                int _completedsimulations = 0;
                while (_completedsimulations < TreeNode.ShallowSimulations)
                {
                    RunSimulation(UpdateToRoot((GameState)gameState),FiniteState.Wander);
                    _completedsimulations++;
                }

                // Carry on as normal.
                return TryGoDirection(gameState, gameState.Pacman.Direction);
            }

        }
        #endregion

        #region Ambush
        private bool _goingToPowerPill = false;

        protected Direction Ambush_OnBegin(object sender, EventArgs e, GameState gs)
        {
            return Direction.Stall;
        }

        protected Direction Ambush_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            _goingToPowerPill = false;

            return Direction.Stall;
        }

        // Move left and right until the adjacency parameter of the ghosts are met.
        protected Direction Ambush(object sender, EventArgs e, GameState gs)
        {
            // Determine where all the ghosts are and whether or not they are within distance
            foreach (Ghost item in gs.Ghosts)
            {
                if (item.Node.ManhattenDistance(gs.Pacman.Node) < 
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
        #endregion

        #region Flee
        public virtual Direction Flee_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.Stall;
        }
        
        public virtual Direction Flee_OnBegin(object sender, EventArgs e, GameState gs)
        {
            _nearestGhost = null;
            _runningDirection = gs.Pacman.Direction;
            return Direction.None;
        }

        // Attempt to go within the provided direction. If it's not possible, then
        // return the next nearest direction.
        private Direction TryGoDirection(GameState gs, Direction pDirection)
        {
            var _directions = gs.Pacman.PossibleDirections();

            // Change the direction if possible.
            if (pDirection == Direction.None)
            {
                pDirection = gs.Pacman.Direction;
            }

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
        /// Return whether or not the Pacman agent is being surrounded by ghosts.
        /// </summary>
        /// <param name="gs">The GameState</param>
        /// <returns>Returns whether or not there are ghosts within the nodes next to Pacman</returns>
        private bool GhostsSurrounding(GameState gs)
        {
            // Loop through the surrounding area and determine whether or not there is a 
            // ghost in the most adjacent nodes.
            for (int x = (gs.Pacman.Node.X - 1); x < gs.Pacman.Node.X + 2; x++)
            {
                for (int y = (gs.Pacman.Node.Y - 1); y < (gs.Pacman.Node.Y + 2); y++)
                {
                    // Make sure that the coordinates are within the bounds
                    if (x < Map.Width && x >= 0 &&
                        y < Map.Height && y >= 0)
                    {
                        // Loop again through the ghosts and determine if they match the coordinates.
                        for (int i = 0; i < gs.Ghosts.Length; i++)
                        {
                            if (gs.Ghosts[i].Node.X == x &&
                                gs.Ghosts[i].Node.Y == y)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
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
                Node _checknode = gs.Map.GetNodeDirection(_currentNode.X,_currentNode.Y,pDirection);

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
        /// <returns></returns>
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

        private Ghost _nearestGhost = null;
        private Direction _runningDirection = Direction.None;
        public virtual Direction Flee(object sender, EventArgs e, GameState gameState)
        {
            // Return the nearest ghost to Pacman within the level.
            _nearestGhost = StateInfo.NearestGhost(gameState);
            _runningDirection = gameState.Pacman.Direction;

            // Determine if there is a ghost within the provided direction.
            if (IsGhostDirection(gameState, gameState.Pacman.Direction,true))
            {
                _runningDirection = GameState.InverseDirection(gameState.Pacman.Direction);
            }
            else if (gameState.Pacman.PossibleDirections().Count > 2)
            {
                var _possibleDirections = gameState.Pacman.PossibleDirections();

                // Loop through the directions within the list.
                foreach (Direction item in _possibleDirections)
                {
                    // Make sure the direction is not the current one that Pacman is running in, nor is it the inverse.
                    if (item != gameState.Pacman.Direction && 
                        item != GameState.InverseDirection(gameState.Pacman.Direction) && 
                        !IsGhostDirection(gameState,item,true))
                    {
                        _runningDirection = item;
                        break;
                    }
                }
            }

            Node _nearestPill = StateInfo.NearestPill(gameState.Pacman.Node, gameState).Target;

            // Determine whether or not the nearest ghost is available.
            if (_nearestGhost != null)
            {
                if (_nearestGhost.Node.ManhattenDistance(gameState.Pacman.Node) > FleeThreshold)
                {
                    if (_nearestPill.ManhattenDistance(gameState.Pacman.Node) > 4)
                    {
                        return ChangeState(FiniteState.EndGame, true, gameState);
                    }
                    else
                    {
                        return ChangeState(FiniteState.Wander, true, gameState);
                    }
                }
            }

            return TryGoDirection(gameState, _runningDirection);
        }
        #endregion

        #region EndGame Enhanced
        public const int UCB_SCORE_THRESHOLD = 5;
        /// <summary>
        /// Generate a path but determine the danger of said path.
        /// </summary>
        /// <param name="pRoot">The root of the evaluation tree that we intend to use</param>
        /// <param name="gs">The game state that we are going to utilizing.</param>
        /// <returns>Returns the best direciton between the path and the tree.</returns>
        public Direction BestDirection(TreeNode pRoot, GameState gs)
        {
            Node _nearestPill = StateInfo.NearestPill(gs.Pacman.Node, gs).Target;
            Node.PathInfo _nearestPillDirection = _nearestPill.ShortestPath[gs.Pacman.Node.X, gs.Pacman.Node.Y];
            
            Direction _pathDirection = _nearestPillDirection == null ? Direction.None : _nearestPillDirection.Direction; 

            // Loop through the children and find the direction that was generated first.
            for (int i = 0; i < pRoot.Children.Length; i++)
            {
                Direction _treeDir = pRoot.Children[i].Directions[i + 1];

                // Determine first that the direction that we want to go in is the same
                // as the tree dir
                if (_pathDirection != Direction.None && _treeDir == _pathDirection)
                {
                    // Make sure that the score is at least above 5.
                    if (pRoot.Children[i].AverageScore > UCB_SCORE_THRESHOLD)
                    {

                    }
                    else
                    {
                        GetNextDirectionFromTree(pRoot, false, gs, SelectionParameter.HighestUcb);
                    }
                }
            }

            return Direction.None;
        }
        
        /// <summary>
        /// For making use of MCTS for ghost avoidance when walking in the End Game state
        /// </summary>
        /// <param name="sender">The object that is calling the method</param>
        /// <param name="e">The event arguments.</param>
        /// <param name="gs">GameState</param>
        /// <returns>Returns the next appropriate direction.</returns>
        public virtual Direction EndGameEnhanced(object sender, EventArgs e, GameState gs)
        {    
            // Prepare the root and perform simulations if we've recognized to be at a junciton
            if (_isAtJunction)
            {
                // Set back to false so that it can be triggered again when we arrive
                // at a function
                _isAtJunction = false;

                PrepareRoot(gs, FiniteState.EndGameEnhanced);

                int _completedsimulations = 0;
                while (_completedsimulations < TreeNode.ShallowSimulations)
                {
                    RunSimulation(UpdateToRoot(gs), FiniteState.EndGameEnhanced);
                }

                return Direction.None;
            }
            else if (IsJunction(gs.Pacman.Node.X, gs.Pacman.Node.Y, gs))
            {
                _isAtJunction = true;

                int _completedsimulations = 0;
                while (_completedsimulations < TreeNode.ShallowSimulations)
                {
                    RunSimulation(gs, FiniteState.EndGameEnhanced);
                    _completedsimulations++;
                }

                return BestDirection(_endGameRoot, gs);
            }
            else
            {
                int _completedsimulations = 0;
                while (_completedsimulations < TreeNode.ShallowSimulations)
                {
                    RunSimulation(gs, FiniteState.EndGameEnhanced);
                }
            }

            return Direction.None;
        }

        public virtual Direction EndGameEnhanced_OnBegin(object sender, EventArgs e, GameState gs)
        {
            // Initialize the end game
            _endGameRoot = new TreeNode(gs, this, new TreeNode(), gs.Pacman.Node, new Direction[] { gs.Pacman.Direction });

            return Direction.None;
        }

        public virtual Direction EndGameEnhanced_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.None;
        }
        #endregion

        #region Hunt
        public virtual Direction Hunt_OnBegin(object sender, EventArgs e, GameState gs)
        {
            _nearestGhostDistance = int.MaxValue;
            m_FocusGhost = null;
            HuntRoot = null;

            return Direction.Stall;
        }

        public virtual Direction Hunt_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            return Direction.Stall;
        }

        /// <summary>
        /// Generate the root tree node for the hunt state.
        /// </summary>
        /// <param name="gs">GameState object</param>
        public void PrepareHuntRoot(GameState gs)
        {
            HuntRoot = new TreeNode(gs, this, new TreeNode(), gs.Pacman.Node, new Direction[] { gs.Pacman.Direction });
        }

        /// <summary>
        /// This will be ran before the controller is told that it has been eaten by a ghost.
        /// </summary>
        public override void BeforeEatenByGhost()
        {
            base.BeforeEatenByGhost();
        }

        public Ghost m_FocusGhost = null;
        private int _nearestGhostDistance = int.MaxValue;
        public virtual Direction Hunt(object sender, EventArgs e, GameState gs)
        {
            if (m_FocusGhost != null)
            {
                if (m_FocusGhost.IsEaten || m_FocusGhost.RemainingFlee == 0)
                {
                    m_FocusGhost = null;
                    _nearestGhostDistance = int.MaxValue;
                }
            }

            // Only hunt for a ghost if it hasn't been eaten yet.
            if (m_FocusGhost == null)
            {
                foreach (var item in gs.Ghosts)
                {
                    // Determine the nearest ghost that we should be targetting atm
                    // Only want to care about ones that are the closest.
                    if (_nearestGhostDistance > item.Node.ManhattenDistance(gs.Pacman.Node) && 
                        item.Entered &&
                        !item.Chasing)
                    {
                        m_FocusGhost = item;
                        _nearestGhostDistance = item.Node.ManhattenDistance(gs.Pacman.Node);
                    }
                }
            }

            // Change the state back to wandering if we couldn't find a suitable ghost to target.
            if (m_FocusGhost == null)
            {
                return ChangeState(FiniteState.Wander, true, gs);
            }

            // Based on these two conditions, we can consider it being dangerous to chase the ghost.
            if (!m_FocusGhost.Fleeing)
            {
                OutputLog("RETURNING TO WANDER", true, true);

                return ChangeState(FiniteState.Wander, true, gs);
            }

            // Return the shortest path to the ghost in question
            Node.PathInfo _ghostpath = gs.Pacman.Node.ShortestPath[m_FocusGhost.Node.X, m_FocusGhost.Node.Y];

            if (_ghostpath != null)
            {
                return _ghostpath.Direction;
            }
            else
            {
                return Direction.Stall;
            }
        }
        #endregion

        #region EndGame
        public virtual Direction EndGame_OnBegin(object sender, EventArgs e, GameState gs)
        {
            // Clear up the values that may have been used from the previous time this state was entered.
            _nearestNode = null;
            return Direction.Stall;
        }

        public virtual Direction EndGame_OnSuspend(object sender, EventArgs e, GameState gs)
        {
            _isAtJunction = true;
            return Direction.Stall;
        }

        private Node _nearestNode = null;

        public virtual Direction EndGame(object sender, EventArgs e, GameState gs)
        {
            Node.PathInfo _shortestpath = null;
            Ghost _nearestGhost = StateInfo.NearestGhost(gs); // Grab the nearest ghost from us.

            if (_nearestNode == null)
            {
                _nearestNode = StateInfo.NearestPill(gs.Pacman.Node, gs).Target;
            }

            if (_nearestNode != null)
            {
                // Return the shortest path
                _shortestpath = gs.Pacman.Node.ShortestPath[_nearestNode.X, _nearestNode.Y];
            }
            
            // Determine if the ghost is in the given direction.
            if (IsGhostDirection(gs, gs.Pacman.Direction, true) && 
                IsGhostDistance(gs,gs.Pacman.Direction,true) < FleeChangeThreshold)
            {
                return ChangeState(FiniteState.Flee, true, gs);
            }

            // Activate the MCTS algorithm here
            if (IsJunction(gs.Pacman.Node.X, gs.Pacman.Node.Y, gs))
            {

            }
            
            if (_shortestpath != null)
            {
                return _shortestpath.Direction;
            }
            else
            {
                return Direction.None;
            }
        }
        #endregion
        #endregion

        #region Member Functions
        
        /// <summary>
        /// Output a message to the log with various arguments
        /// </summary>
        /// <param name="pLogMessage">Message to be displayed</param>
        /// <param name="pVerbose">Is this to be written to the console window?</param>
        /// <param name="pDisplayDate">Display the date with the text log message?</param>
        public virtual void OutputLog(string pLogMessage, bool pVerbose, bool pDisplayDate)
        {
            StreamWriter _writer = new StreamWriter(string.Format("{0}\\output_{1}.txt",_testLogFolder.FullName,DateTime.Now.ToString("ddMMyyyy")), true);
            string _currentdate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss ff");
            string _output = "";

            // Determine whether or not we want to display the date.
            if (pDisplayDate)
                _output = string.Format("[{0}]: {1}", _currentdate, pLogMessage);
            else
                _output = pLogMessage;

            _writer.WriteLine(_output);

            // Make sure whether or not adding one more message would cause problems.

            if (pVerbose && !RemainQuiet)
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
        /// Output hte stats to a text file.
        /// </summary>
        public void SaveStatsText()
        {
            StreamWriter _writer = new StreamWriter(string.Format("{0}/{2}_testlog_{1}.txt", Environment.CurrentDirectory, DateTime.Now.ToString("ddMMyyyy"),this.Name.ToString()), true);
            _writer.WriteLine("===== LUCPAC TEST RESULTS =====");

            // Output the stats to the file in question
            _writer.WriteLine(string.Format("MAX PILLS EATEN: {0}", _testStats.MaxPillsTaken));
            _writer.WriteLine(string.Format("MIN PILLS EATEN: {0}", _testStats.MinPillsTaken));
            _writer.WriteLine(string.Format("AVERAGE PILLS EATEN: {0}", _testStats.AveragePillsTaken));
            _writer.WriteLine(string.Format("GAMES PLAYED: {0}", _testStats.TotalGames));
            _writer.WriteLine(string.Format("HIGHEST SCORE: {0}", _testStats.MaxScore));
            _writer.WriteLine(string.Format("AVERAGE SCORE: {0}", _testStats.AverageScore));
            _writer.WriteLine(string.Format("LOWEST SCORE: {0}", _testStats.MinScore));
            _writer.WriteLine(string.Format("MINIMUM GHOSTS EATEN: {0}", _testStats.MinGhostsEaten));
            _writer.WriteLine(string.Format("MAX GHOSTS EATEN: {0}", _testStats.MaxGhostsEaten));
            _writer.WriteLine(string.Format("AVERAGE GHOSTS EATEN: {0}", _testStats.AverageGhostsEaten));
            _writer.WriteLine(string.Format("MIN MCTS TIME: {0}", _testStats.MCTSMinimum));
            _writer.WriteLine(string.Format("MAX MCTS TIME: {0}", _testStats.MCTSMaximum));
            _writer.WriteLine(string.Format("AVERAGE MCTS TIME: {0}", _testStats.MCTSAverage));

            _writer.Flush();
            _writer.Close();
        }

        public bool IsGhostBehind(GameState gs, int pCheckDistance)
        {
            Node _pacmanPosition = gs.Pacman.Node;

            Ghost _nearestGhost = StateInfo.NearestGhost(gs);

            return false;
        }

        // Update the console screen, prevent appearance of spam.
        public override void UpdateConsole()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine("================== LUCPAC ==================");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            string _pacmanPosition = string.Format("Pacman: {0},{1}",_gameState.Pacman.Node.X, _gameState.Pacman.Node.Y);

            //m_GameState.Pacman.ImgX.ToString(),m_GameState.Pacman.ImgY.ToString()

            foreach (var ghost in _gameState.Ghosts)
            {
                Console.WriteLine(String.Format("{0}: {1},{2}", ghost.GetType().ToString(), 
                                                                ghost.Node.X, 
                                                                ghost.Node.Y));
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("PILLS REMAINING: {0}",_gameState.Map.PillNodes.Where(n=>n.Type != Node.NodeType.None && n.Type != Node.NodeType.Wall).Count().ToString()));
            Console.WriteLine(string.Format("PILLS LEFT(INT): {0}", _gameState.Map.PillsLeft.ToString()));
            Console.WriteLine(string.Format("PREVIOUS STATE: {0}", _previousState.ToString()));
            Console.WriteLine(string.Format("STATE: {0}",CurrentState.ToString()));

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
            Console.WriteLine(string.Format("SESSION ID: {0}",_testSessionId));
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
        /// Call the method that is responsible for the enumerator
        /// </summary>
        /// <param name="pState">The state that we want to call the method of</param>
        /// <returns>The direction that the Pacman AI is to go in</returns>
        protected Direction CallState(FiniteState pState, GameState gs)
        {
            return _states[pState].Action(this, null, gs);
        }

        /// <summary>
        /// Change the direction in which the controller is going
        /// </summary>
        /// <param name="pNewDirection">The direction that the controller is going in</param>
        /// <param name="pLog">Do we output this to the log?</param>
        public void ChangeDirection(Direction pNewDirection, bool pLog)
        {
            // Determine whether or not to display the direction to the log
            if (pLog)
            {
                OutputLog(string.Format("New Direction: {0}", pNewDirection.ToString()), true, true);
            }

            _currentDirection = pNewDirection;
        }


        // Update the state and lead the output know about it
        public Direction ChangeState(FiniteState pNewState, bool pLog, GameState gs)
        {
            if (pLog)
            {
                OutputLog(string.Format("New State: {0}", pNewState.ToString()), true, true);
            }

            // Store the previous FSM state.
            _previousState = CurrentState;
            
            // Call the functions that are defined for the respective states.
            _states[CurrentState].OnSuspend(this, null, gs);
            _states[pNewState].OnBegin(this, null, gs);
            CurrentState = pNewState;
            CallState(CurrentState,gs);
            return Direction.None;
        }
        #endregion

        #region Overrides
        public override void EatPill()
        {
            if (CurrentState == FiniteState.EndGame)
            {
                ChangeState(FiniteState.Wander, true, _gameState);
            }

            base.EatPill();
        }

        /// <summary>
        /// Update the game state so that it commences in the given direction of the Ms. Pac-Man agent.
        /// i.e. at the next junction or point where there is one more or directions to go in.
        /// </summary>
        /// <param name="pGameState">The newly advanced game state.</param>
        /// <returns>The newly updated game state.</returns>
        public GameState UpdateToRoot(GameState pGameState)
        {
            return _treeRoot.CPathAdvanceGame((GameState)pGameState.Clone(), pGameState.Pacman.Direction);
        }

        /// <summary>
        /// Carry out simulations and node evaluations based on the chosen UCB scoring system
        /// of choice.
        /// </summary>
        /// <param name="pGameState">The chosen game state. If we're at a junction, we'll advance the game first so it starts from the junction ahead.</param>
        /// <param name="pCurrentState"></param>
        public void RunSimulation(GameState pGameState, FiniteState pCurrentState)
        {
            TreeNode _focus = null;

            // Define which one to focus on.
            switch (pCurrentState)
            {
                case FiniteState.Wander:
                    _focus = this._treeRoot;
                break;

                case FiniteState.EndGameEnhanced:
                    _focus = this._endGameRoot;
                break;
            }

            // Determine first that there is a tree root that we can use
            if (_treeRoot != null)
            {
                // Find the tree node with the best average value to use
                TreeNode _toevaluate = _treeRoot.UCT();

                _toevaluate.AddScore(EvaluateNode(_toevaluate, pGameState));

                // Implemented to see if we can determine whether or not 
                // the layer threshold has any impact on the scoring of the tree.
                if (_treeRoot.CountLayers() < TreeNode.LAYER_THRESHOLD)
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

        public override void EatPowerPill()
        {
            OutputLog("Power pill eaten!", true, true);

            // Considering that we have just eaten a pill, we want to go for the ghosts.
            ChangeState(FiniteState.Hunt,true,_gameState);

            SaveStateAsImage(_gameState, this, "eatenpowerpill",true);

            base.EatPowerPill();
        }

        public override void EatenByGhost()
        {
            OutputLog("Eaten by a ghost!", true, true);

            SaveStateAsImage(_gameState, this, "eatenbyghost",true);

            if (_gameState.Pacman.Lives >= 0)
            {
                Utility.SerializeGameState(_gameState, this);
            }
                // Change the state to somewhere else
            ChangeState(FiniteState.Wander,true,_gameState);

            if (Milliseconds - LastLifeMilliseconds > _testStats.MaxLifeTime)
            {
                _testStats.MaxLifeTime = Milliseconds - LastLifeMilliseconds;
            }

            if (Milliseconds - LastLifeMilliseconds < _testStats.MinLifeTime)
            {
                _testStats.MinLifeTime = Milliseconds - LastLifeMilliseconds;
            }

            _testStats.TotalLifeTime += Milliseconds - LastLifeMilliseconds;
            _testStats.TotalLives++;

            _testStats.AverageLifeTime = _testStats.TotalLifeTime / _testStats.TotalLives;

            LastLifeMilliseconds = Milliseconds;

            base.EatenByGhost();
        }

        public override void EatGhost()
        {
            //SaveStateAsImage(m_GameState, this, "eatenghost");
            base.EatGhost();
        }

        /// <summary>
        /// Event that is executed when the level is cleared.
        /// </summary>
        public override void LevelCleared()
        {
            OutputLog("===== LEVEL CLEARED =====", true, true);

            ChangeState(FiniteState.Wander, true, _gameState);

            base.LevelCleared();
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

                g.DrawString(_treeRoot.AverageScore.ToString(), new Font(FontFamily.GenericSansSerif,10f), Brushes.White,
                             _treeRoot.PathNode.CenterX, _treeRoot.PathNode.CenterY);
            }

            // Render out the EndGame tree that is to be used
            // for determining the safety of the agent.
            if (_endGameRoot != null)
            {
                _endGameRoot.Draw(g);

                g.DrawImage(_redBlock, new Point(_endGameRoot.PathNode.CenterX - 2, _endGameRoot.PathNode.CenterY - 2));

                g.DrawString(_endGameRoot.AverageScore.ToString(), new Font(FontFamily.GenericSansSerif, 10f), Brushes.White,
                             _endGameRoot.PathNode.CenterX, _endGameRoot.PathNode.CenterY);
            }

//            g.DrawImage(m_GreenBlock, new Point(m_GameState.Pacman.Node.CenterX, m_GameState.Pacman.Node.CenterY));

            base.Draw(g);
        }

        // Overrided method for writing some data to the text file.
        public override void WriteData(System.IO.StreamWriter sw, int sector)
        {
            sw.WriteLine("This is some new value");
            
            base.WriteData(sw, sector);
        }
        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return null;
        }

        #endregion
    }
}
