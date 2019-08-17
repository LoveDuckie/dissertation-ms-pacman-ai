using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Security.Cryptography;
using System.Diagnostics;

using Newtonsoft.Json;

namespace Pacman.Simulator
{
    [Serializable()]
	public abstract class BasePacman
	{
		public readonly string Name;

        public TestStats _testStats = new TestStats();
        public Stopwatch _stopWatch = new Stopwatch();
        private bool testComplete = false;
        private bool recordStats = false;

        protected string _testSessionId = "";

        private DateTime gameStartTimestamp;
        private DateTime gameEndTimestamp;
        private DateTime lifeStartTimestamp;

        private long lastRoundMilliseconds = 0;
        private long milliseconds = 0;
        private long lastLifeMilliseconds = 0;

        // Max games that are to be tested.
        public const int MaxTestGames = 100;

        public DirectoryInfo _testDataFolder = null;
        public DirectoryInfo _testImagesFolder = null;
        public DirectoryInfo _testLogFolder = null;

        public bool TestComplete { get => testComplete; set => testComplete = value; }
        public bool RecordStats { get => recordStats; set => recordStats = value; }
        protected long LastLifeMilliseconds { get => lastLifeMilliseconds; set => lastLifeMilliseconds = value; }
        protected long Milliseconds { get => milliseconds; set => milliseconds = value; }
        protected long LastRoundMilliseconds { get => lastRoundMilliseconds; set => lastRoundMilliseconds = value; }
        protected DateTime LifeStartTimestamp { get => lifeStartTimestamp; set => lifeStartTimestamp = value; }
        protected DateTime GameEndTimestamp { get => gameEndTimestamp; set => gameEndTimestamp = value; }
        protected DateTime GameStartTimestamp { get => gameStartTimestamp; set => gameStartTimestamp = value; }

        #region Constructors
        /// <summary>
        ///     
        /// </summary>
        /// <param name="name"></param>
        public BasePacman(string name)
        {
            this.Name = name;
            this._testStats = new TestStats();
        } 
        #endregion

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
                _testLogFolder.FullName, 
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
