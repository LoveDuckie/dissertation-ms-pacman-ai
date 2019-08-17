using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Pacman.Simulator
{
    /// <summary>
    /// This class is intended to be easily serializable and modified by the Newtonsoft JSON serializer.
    /// </summary>
    public class GameStateSerialize : JsonSerializer
    {
        #region Properties
        public int PacmanX { get; set; }
        public int PacmanY { get; set; }
        public int BlueX { get; set; }
        public int BlueY { get; set; }
        public int RedX { get; set; }
        public int RedY { get; set; }
        public int BrownX { get; set; }
        public int BrownY { get; set; }
        public int PinkX { get; set; }
        public int PinkY { get; set; }

        public bool PinkFleeing { get; set; }
        public bool BlueFleeing { get; set; }
        public bool RedFleeing { get; set; }
        public bool BrownFleeing { get; set; }

        public bool PinkEntered { get; set; }
        public bool BlueEntered { get; set; }
        public bool RedEntered { get; set; }
        public bool BrownEntered { get; set; }

        public int LevelsCleared { get; set; }

        public float RoundDuration { get; set; }
        public int GameOverCount { get; set; }
        public int Score { get; set; }
        public int LivesLeft { get; set; }
        public int Timer { get; set; }
        public DateTime TimeCaptured { get; set; }
        #endregion

        #region Constructors
        public GameStateSerialize()
        {

        }
        #endregion
    }
}
