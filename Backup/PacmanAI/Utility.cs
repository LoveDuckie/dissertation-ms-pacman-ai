using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Web;
using System.Drawing;

using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

using Pacman.Simulator;

namespace PacmanAI
{
    public class Utility
    {
        //[field: NonSerializedAttribute()]
        public static Image m_GreenBlock = Image.FromFile("green_block.png");
        public static Image m_RedBlock = Image.FromFile("red_block.png");
        public static Image m_BlueBlock = Image.FromFile("blue_block.png");

        // Serialize the game state into a JSON format.
        public static void SerializeGameState(GameState gs, BasePacman pController)
        {
            // Doesn't seem like a neater way of doing this unfortunately :/
            GameStateSerialize _serializeObject = new GameStateSerialize()
            {
                PacmanX = gs.Pacman.Node.X,
                PacmanY = gs.Pacman.Node.Y,

                BlueX = gs.Blue.Node.X,
                BlueY = gs.Blue.Node.Y,

                PinkX = gs.Pink.Node.X,
                PinkY = gs.Pink.Node.Y,

                BrownX = gs.Brown.Node.X,
                BrownY = gs.Brown.Node.Y,

                RedX = gs.Red.Node.X,
                RedY = gs.Red.Node.Y,

                RedEntered = gs.Red.Entered,
                BlueEntered = gs.Blue.Entered,
                BrownEntered = gs.Brown.Entered,
                PinkEntered = gs.Pink.Entered,

                PinkFleeing = gs.Pink.Fleeing,
                BrownFleeing = gs.Brown.Fleeing,
                BlueFleeing = gs.Blue.Fleeing,
                RedFleeing = gs.Red.Fleeing,

                LevelsCleared = gs.Level,

                LivesLeft = gs.Pacman.Lives,
                Timer = (int)gs.Timer,
                Score = gs.Pacman.Score,
                TimeCaptured = DateTime.Now,
                GameOverCount = gs.m_GameOverCount
            };

            // Return the string output from serializing our object.
            string _output = JsonConvert.SerializeObject(_serializeObject, Formatting.Indented);
            //string _outputTwo = JsonConvert.SerializeObject(gs, Formatting.Indented, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Error });

            // Output the JSON serialization to the text file.
            StreamWriter _writer = new StreamWriter(string.Format("{3}\\gamestate_{0}_{1}_{2}.txt", 
                pController.Name.ToString(), 
                DateTime.Now.ToString("hhmmddss"), 
                pController.m_TestStats.TotalGames.ToString(),
                pController.m_TestLogFolder.FullName.ToString()), false);
            _writer.WriteLine(_output);

            _writer.Flush();
            _writer.Close();
        }


    }
}
