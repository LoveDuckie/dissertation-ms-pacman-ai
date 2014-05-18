using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Diagnostics;

using Pacman.Simulator;

using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pacman.Simulator
{
    [Serializable()]
	public abstract class BasePacman
	{
		public readonly string Name;
        public TestStats m_TestStats = new TestStats();
        public Stopwatch m_Stopwatch = new Stopwatch();
        public bool m_TestComplete = false;
        public bool m_RecordStats = false;

        protected string m_TestSessionID = "";

        protected DateTime m_GameStart;
        protected DateTime m_GameEnd;
        protected DateTime m_LifeStart;

        protected long m_LastRoundMS = 0;
        protected long m_MS = 0;
        protected long m_LastLifeMS = 0;

        // Max games that are to be tested.
        public const int MAX_TEST_GAMES = 100;

        public DirectoryInfo m_TestDataFolder = null;
        public DirectoryInfo m_TestImagesFolder = null;
        public DirectoryInfo m_TestLogFolder = null;

		public BasePacman(string name) {
			this.Name = name;
            this.m_TestStats = new TestStats();
        }

        public virtual string GenerateSessionID()
        {
            string _input = DateTime.Now.ToString("ddMMyyyyHHmmssff");
            string _result = "";

            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                _result = BitConverter.ToString(md5.ComputeHash(ASCIIEncoding.Default.GetBytes(_input)));
            }

            // Return the string with minor fixes.
            return string.Format("session_{0}", _result.Replace("-", string.Empty));
        }

        // Put the test stats object into a serialized JSON text file.
        public void SerializeTestStats(TestStats pStats)
        {
            StreamWriter _writer = new StreamWriter(
                string.Format("{0}\\endoftest_{1}.txt", 
                m_TestLogFolder.FullName, 
                DateTime.Now.ToString("hhmmddss")));
            string _jsonoutput = JsonConvert.SerializeObject(pStats,Formatting.Indented);

            _writer.WriteLine(_jsonoutput);

            _writer.Flush();
            _writer.Close();
        }


		public abstract Direction Think(GameState gs);

		public virtual void EatPill() { }
		public virtual void EatPowerPill() { }
		public virtual void EatGhost() { }
		public virtual void EatenByGhost() { }
        public virtual void EatenByGhost(GameState gs) { }
        public virtual void BeforeEatenByGhost() { }
		public virtual void LevelCleared() { }

        public virtual void UpdateConsole()
        {
            // nothing goes on here at least.
        }

        public virtual void Restart(GameState gs) { 
            
        }

		public virtual void Draw(Graphics g) { }
		public virtual void Draw(Graphics g, int[] danger) { }
		public virtual void WriteData(StreamWriter sw, int sector) { }

		public virtual void SimulationFinished() { }
	}
}
