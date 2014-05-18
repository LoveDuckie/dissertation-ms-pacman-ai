using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

namespace Pacman.Simulator
{

    /// <summary>
    /// These stats are serialized once the testing has been completed.
    /// </summary>
    public class TestStats : JsonSerializer
    {
        public int MinLevelsCleared = 0;
        public int MaxLevelsCleared = 0;
        public int AverageLevelsCleared = 0;
        public int TotalLevelsCleared = 0;

        public string SessionID = "";

        public long ElapsedMillisecondsTotal = 0;

        public int TotalGames = 0;

        // The amount that each of the ghost kills the Pac-Man agent.
        public int RedKills = 0;
        public int PinkKills = 0;
        public int BlueKills = 0;
        public int BrownKills = 0;

        public int TotalPillsTaken = 0;
        public int MaxPillsTaken = 0;
        public int MinPillsTaken = int.MaxValue;
        public int AveragePillsTaken = 0;

        public int TotalGhostsEaten = 0;
        public int MaxGhostsEaten = 0;
        public int MinGhostsEaten = int.MaxValue;
        public int AverageGhostsEaten = 0;

        // Used for recording how long each game round takes.
        public float LongestRoundTime = 0;
        public float ShortestRoundTime = float.MaxValue;
        public float AverageRoundTime = 0;
        public float TotalRoundTime = 0;

        public float MinLifeTime = float.MaxValue;
        public float MaxLifeTime = 0;
        public float AverageLifeTime = 0;
        public float TotalLifeTime = 0;
        public int TotalLives = 0;

        public int MCTSTotalGenerations = 0;
        public int MCTSMaximum = 0;
        public int MCTSMinimum = int.MaxValue;
        public int MCTSAverage = 0;
        public int MCTSTotalTime = 0;

        public int TotalScore = 0;
        public int AverageScore = 0;
        public int MinScore = int.MaxValue;
        public int MaxScore = 0;

        public void Reset()
        {

        }
    }
}
