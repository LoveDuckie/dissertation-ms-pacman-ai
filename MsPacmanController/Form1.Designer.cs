namespace MsPacmanController
{
	partial class Form1
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if( disposing && (components != null) ) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.buttonInit = new System.Windows.Forms.Button();
			this.buttonStart = new System.Windows.Forms.Button();
			this.labelError = new System.Windows.Forms.Label();
			this.labelErrorText = new System.Windows.Forms.Label();
			this.pictureInitFoundGame = new System.Windows.Forms.PictureBox();
			this.labelInitFoundGame = new System.Windows.Forms.Label();
			this.pictureInit32Bit = new System.Windows.Forms.PictureBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.pictureInitLocated = new System.Windows.Forms.PictureBox();
			this.checkBoxAutoPlay = new System.Windows.Forms.CheckBox();
			this.labelFrameRate = new System.Windows.Forms.Label();
			this.pictureFrameRate = new System.Windows.Forms.PictureBox();
			this.labelInitialized = new System.Windows.Forms.Label();
			this.pictureLine = new System.Windows.Forms.PictureBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.labelHighscore = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.labelDown = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.labelUp = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.labelRight = new System.Windows.Forms.Label();
			this.label18 = new System.Windows.Forms.Label();
			this.labelLeft = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.labelFoundDirection = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.labelFoundPosition = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.labelDirection = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.labelAI = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.labelPills = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.labelMaze = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.labelDebug = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.labelState = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.labelLives = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.labelScore = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.checkBoxVisualizer = new System.Windows.Forms.CheckBox();
			this.comboBoxAI = new System.Windows.Forms.ComboBox();
			this.labelAvgScore = new System.Windows.Forms.Label();
			this.label17 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureInitFoundGame)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureInit32Bit)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureInitLocated)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureFrameRate)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureLine)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// buttonInit
			// 
			this.buttonInit.Location = new System.Drawing.Point(13, 13);
			this.buttonInit.Name = "buttonInit";
			this.buttonInit.Size = new System.Drawing.Size(75, 23);
			this.buttonInit.TabIndex = 0;
			this.buttonInit.Text = "Initialize";
			this.buttonInit.UseVisualStyleBackColor = true;
			this.buttonInit.Click += new System.EventHandler(this.buttonInit_Click);
			// 
			// buttonStart
			// 
			this.buttonStart.Enabled = false;
			this.buttonStart.Location = new System.Drawing.Point(13, 278);
			this.buttonStart.Name = "buttonStart";
			this.buttonStart.Size = new System.Drawing.Size(126, 23);
			this.buttonStart.TabIndex = 3;
			this.buttonStart.Text = "Play Ms. Pacman";
			this.buttonStart.UseVisualStyleBackColor = true;
			this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
			// 
			// labelError
			// 
			this.labelError.AutoSize = true;
			this.labelError.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelError.ForeColor = System.Drawing.Color.Red;
			this.labelError.Location = new System.Drawing.Point(12, 207);
			this.labelError.Name = "labelError";
			this.labelError.Size = new System.Drawing.Size(34, 13);
			this.labelError.TabIndex = 4;
			this.labelError.Text = "Error";
			this.labelError.Visible = false;
			// 
			// labelErrorText
			// 
			this.labelErrorText.AutoSize = true;
			this.labelErrorText.Location = new System.Drawing.Point(12, 223);
			this.labelErrorText.MaximumSize = new System.Drawing.Size(150, 0);
			this.labelErrorText.Name = "labelErrorText";
			this.labelErrorText.Size = new System.Drawing.Size(50, 13);
			this.labelErrorText.TabIndex = 5;
			this.labelErrorText.Text = "ErrorText";
			this.labelErrorText.Visible = false;
			// 
			// pictureInitFoundGame
			// 
			this.pictureInitFoundGame.Location = new System.Drawing.Point(13, 71);
			this.pictureInitFoundGame.Name = "pictureInitFoundGame";
			this.pictureInitFoundGame.Size = new System.Drawing.Size(16, 16);
			this.pictureInitFoundGame.TabIndex = 6;
			this.pictureInitFoundGame.TabStop = false;
			// 
			// labelInitFoundGame
			// 
			this.labelInitFoundGame.AutoSize = true;
			this.labelInitFoundGame.Location = new System.Drawing.Point(31, 73);
			this.labelInitFoundGame.Name = "labelInitFoundGame";
			this.labelInitFoundGame.Size = new System.Drawing.Size(128, 13);
			this.labelInitFoundGame.TabIndex = 7;
			this.labelInitFoundGame.Text = "Found Ms. Pacman game";
			// 
			// pictureInit32Bit
			// 
			this.pictureInit32Bit.Location = new System.Drawing.Point(13, 47);
			this.pictureInit32Bit.Name = "pictureInit32Bit";
			this.pictureInit32Bit.Size = new System.Drawing.Size(16, 16);
			this.pictureInit32Bit.TabIndex = 8;
			this.pictureInit32Bit.TabStop = false;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(31, 50);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(106, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "Screen is 32 bit color";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(31, 97);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(99, 13);
			this.label2.TabIndex = 11;
			this.label2.Text = "Located game area";
			// 
			// pictureInitLocated
			// 
			this.pictureInitLocated.Location = new System.Drawing.Point(13, 95);
			this.pictureInitLocated.Name = "pictureInitLocated";
			this.pictureInitLocated.Size = new System.Drawing.Size(16, 16);
			this.pictureInitLocated.TabIndex = 10;
			this.pictureInitLocated.TabStop = false;
			// 
			// checkBoxAutoPlay
			// 
			this.checkBoxAutoPlay.AutoSize = true;
			this.checkBoxAutoPlay.Location = new System.Drawing.Point(94, 17);
			this.checkBoxAutoPlay.Name = "checkBoxAutoPlay";
			this.checkBoxAutoPlay.Size = new System.Drawing.Size(67, 17);
			this.checkBoxAutoPlay.TabIndex = 12;
			this.checkBoxAutoPlay.Text = "Autoplay";
			this.checkBoxAutoPlay.UseVisualStyleBackColor = true;
			this.checkBoxAutoPlay.CheckedChanged += new System.EventHandler(this.checkBoxAutoPlay_CheckedChanged);
			// 
			// labelFrameRate
			// 
			this.labelFrameRate.AutoSize = true;
			this.labelFrameRate.Location = new System.Drawing.Point(31, 181);
			this.labelFrameRate.Name = "labelFrameRate";
			this.labelFrameRate.Size = new System.Drawing.Size(111, 13);
			this.labelFrameRate.TabIndex = 14;
			this.labelFrameRate.Text = "Milliseconds pr. frame:";
			// 
			// pictureFrameRate
			// 
			this.pictureFrameRate.Location = new System.Drawing.Point(13, 179);
			this.pictureFrameRate.Name = "pictureFrameRate";
			this.pictureFrameRate.Size = new System.Drawing.Size(16, 17);
			this.pictureFrameRate.TabIndex = 13;
			this.pictureFrameRate.TabStop = false;
			// 
			// labelInitialized
			// 
			this.labelInitialized.AutoSize = true;
			this.labelInitialized.Location = new System.Drawing.Point(31, 128);
			this.labelInitialized.Name = "labelInitialized";
			this.labelInitialized.Size = new System.Drawing.Size(50, 13);
			this.labelInitialized.TabIndex = 15;
			this.labelInitialized.Text = "Initialized";
			// 
			// pictureLine
			// 
			this.pictureLine.Location = new System.Drawing.Point(13, 120);
			this.pictureLine.Name = "pictureLine";
			this.pictureLine.Size = new System.Drawing.Size(145, 1);
			this.pictureLine.TabIndex = 16;
			this.pictureLine.TabStop = false;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.labelAvgScore);
			this.groupBox1.Controls.Add(this.label17);
			this.groupBox1.Controls.Add(this.pictureBox1);
			this.groupBox1.Controls.Add(this.labelHighscore);
			this.groupBox1.Controls.Add(this.label15);
			this.groupBox1.Controls.Add(this.labelDown);
			this.groupBox1.Controls.Add(this.label13);
			this.groupBox1.Controls.Add(this.labelUp);
			this.groupBox1.Controls.Add(this.label16);
			this.groupBox1.Controls.Add(this.labelRight);
			this.groupBox1.Controls.Add(this.label18);
			this.groupBox1.Controls.Add(this.labelLeft);
			this.groupBox1.Controls.Add(this.label20);
			this.groupBox1.Controls.Add(this.labelFoundDirection);
			this.groupBox1.Controls.Add(this.label12);
			this.groupBox1.Controls.Add(this.labelFoundPosition);
			this.groupBox1.Controls.Add(this.label14);
			this.groupBox1.Controls.Add(this.labelDirection);
			this.groupBox1.Controls.Add(this.label11);
			this.groupBox1.Controls.Add(this.labelAI);
			this.groupBox1.Controls.Add(this.label10);
			this.groupBox1.Controls.Add(this.labelPills);
			this.groupBox1.Controls.Add(this.label9);
			this.groupBox1.Controls.Add(this.labelMaze);
			this.groupBox1.Controls.Add(this.label8);
			this.groupBox1.Controls.Add(this.labelDebug);
			this.groupBox1.Controls.Add(this.label7);
			this.groupBox1.Controls.Add(this.labelState);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.labelLives);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.labelScore);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Location = new System.Drawing.Point(192, 13);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(234, 288);
			this.groupBox1.TabIndex = 17;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Game Info";
			// 
			// pictureBox1
			// 
			this.pictureBox1.Cursor = System.Windows.Forms.Cursors.Hand;
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(205, 41);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(16, 16);
			this.pictureBox1.TabIndex = 30;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
			// 
			// labelHighscore
			// 
			this.labelHighscore.AutoSize = true;
			this.labelHighscore.Location = new System.Drawing.Point(156, 42);
			this.labelHighscore.Name = "labelHighscore";
			this.labelHighscore.Size = new System.Drawing.Size(41, 13);
			this.labelHighscore.TabIndex = 29;
			this.labelHighscore.Text = "[Score]";
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(120, 42);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(32, 13);
			this.label15.TabIndex = 28;
			this.label15.Text = "High:";
			// 
			// labelDown
			// 
			this.labelDown.AutoSize = true;
			this.labelDown.Location = new System.Drawing.Point(81, 203);
			this.labelDown.Name = "labelDown";
			this.labelDown.Size = new System.Drawing.Size(37, 13);
			this.labelDown.TabIndex = 27;
			this.labelDown.Text = "[Type]";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(25, 203);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(38, 13);
			this.label13.TabIndex = 26;
			this.label13.Text = "Down:";
			// 
			// labelUp
			// 
			this.labelUp.AutoSize = true;
			this.labelUp.Location = new System.Drawing.Point(81, 186);
			this.labelUp.Name = "labelUp";
			this.labelUp.Size = new System.Drawing.Size(37, 13);
			this.labelUp.TabIndex = 25;
			this.labelUp.Text = "[Type]";
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(25, 186);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(24, 13);
			this.label16.TabIndex = 24;
			this.label16.Text = "Up:";
			// 
			// labelRight
			// 
			this.labelRight.AutoSize = true;
			this.labelRight.Location = new System.Drawing.Point(81, 169);
			this.labelRight.Name = "labelRight";
			this.labelRight.Size = new System.Drawing.Size(37, 13);
			this.labelRight.TabIndex = 23;
			this.labelRight.Text = "[Type]";
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point(25, 169);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(35, 13);
			this.label18.TabIndex = 22;
			this.label18.Text = "Right:";
			// 
			// labelLeft
			// 
			this.labelLeft.AutoSize = true;
			this.labelLeft.Location = new System.Drawing.Point(81, 152);
			this.labelLeft.Name = "labelLeft";
			this.labelLeft.Size = new System.Drawing.Size(37, 13);
			this.labelLeft.TabIndex = 21;
			this.labelLeft.Text = "[Type]";
			// 
			// label20
			// 
			this.label20.AutoSize = true;
			this.label20.Location = new System.Drawing.Point(25, 152);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(28, 13);
			this.label20.TabIndex = 20;
			this.label20.Text = "Left:";
			// 
			// labelFoundDirection
			// 
			this.labelFoundDirection.AutoSize = true;
			this.labelFoundDirection.Location = new System.Drawing.Point(71, 136);
			this.labelFoundDirection.Name = "labelFoundDirection";
			this.labelFoundDirection.Size = new System.Drawing.Size(55, 13);
			this.labelFoundDirection.TabIndex = 19;
			this.labelFoundDirection.Text = "[Direction]";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(15, 136);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(52, 13);
			this.label12.TabIndex = 18;
			this.label12.Text = "Direction:";
			// 
			// labelFoundPosition
			// 
			this.labelFoundPosition.AutoSize = true;
			this.labelFoundPosition.Location = new System.Drawing.Point(71, 120);
			this.labelFoundPosition.Name = "labelFoundPosition";
			this.labelFoundPosition.Size = new System.Drawing.Size(50, 13);
			this.labelFoundPosition.TabIndex = 17;
			this.labelFoundPosition.Text = "[Position]";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(15, 120);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(49, 13);
			this.label14.TabIndex = 16;
			this.label14.Text = "Pacman:";
			// 
			// labelDirection
			// 
			this.labelDirection.AutoSize = true;
			this.labelDirection.Location = new System.Drawing.Point(71, 240);
			this.labelDirection.Name = "labelDirection";
			this.labelDirection.Size = new System.Drawing.Size(55, 13);
			this.labelDirection.TabIndex = 15;
			this.labelDirection.Text = "[Direction]";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(15, 240);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(52, 13);
			this.label11.TabIndex = 14;
			this.label11.Text = "Direction:";
			// 
			// labelAI
			// 
			this.labelAI.AutoSize = true;
			this.labelAI.Location = new System.Drawing.Point(71, 224);
			this.labelAI.Name = "labelAI";
			this.labelAI.Size = new System.Drawing.Size(57, 13);
			this.labelAI.TabIndex = 13;
			this.labelAI.Text = "[Controller]";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(15, 224);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(20, 13);
			this.label10.TabIndex = 12;
			this.label10.Text = "AI:";
			// 
			// labelPills
			// 
			this.labelPills.AutoSize = true;
			this.labelPills.Location = new System.Drawing.Point(71, 93);
			this.labelPills.Name = "labelPills";
			this.labelPills.Size = new System.Drawing.Size(31, 13);
			this.labelPills.TabIndex = 11;
			this.labelPills.Text = "[Pills]";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(15, 93);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(28, 13);
			this.label9.TabIndex = 10;
			this.label9.Text = "Pills:";
			// 
			// labelMaze
			// 
			this.labelMaze.AutoSize = true;
			this.labelMaze.Location = new System.Drawing.Point(71, 76);
			this.labelMaze.Name = "labelMaze";
			this.labelMaze.Size = new System.Drawing.Size(39, 13);
			this.labelMaze.TabIndex = 9;
			this.labelMaze.Text = "[Maze]";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(15, 76);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(36, 13);
			this.label8.TabIndex = 8;
			this.label8.Text = "Maze:";
			// 
			// labelDebug
			// 
			this.labelDebug.AutoSize = true;
			this.labelDebug.Location = new System.Drawing.Point(71, 265);
			this.labelDebug.Name = "labelDebug";
			this.labelDebug.Size = new System.Drawing.Size(45, 13);
			this.labelDebug.TabIndex = 7;
			this.labelDebug.Text = "[Debug]";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(15, 265);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(42, 13);
			this.label7.TabIndex = 6;
			this.label7.Text = "Debug:";
			// 
			// labelState
			// 
			this.labelState.AutoSize = true;
			this.labelState.Location = new System.Drawing.Point(71, 24);
			this.labelState.Name = "labelState";
			this.labelState.Size = new System.Drawing.Size(38, 13);
			this.labelState.TabIndex = 5;
			this.labelState.Text = "[State]";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(15, 24);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(35, 13);
			this.label6.TabIndex = 4;
			this.label6.Text = "State:";
			// 
			// labelLives
			// 
			this.labelLives.AutoSize = true;
			this.labelLives.Location = new System.Drawing.Point(71, 59);
			this.labelLives.Name = "labelLives";
			this.labelLives.Size = new System.Drawing.Size(38, 13);
			this.labelLives.TabIndex = 3;
			this.labelLives.Text = "[Lives]";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(15, 59);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(35, 13);
			this.label4.TabIndex = 2;
			this.label4.Text = "Lives:";
			// 
			// labelScore
			// 
			this.labelScore.AutoSize = true;
			this.labelScore.Location = new System.Drawing.Point(71, 42);
			this.labelScore.Name = "labelScore";
			this.labelScore.Size = new System.Drawing.Size(41, 13);
			this.labelScore.TabIndex = 1;
			this.labelScore.Text = "[Score]";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(15, 42);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(38, 13);
			this.label3.TabIndex = 0;
			this.label3.Text = "Score:";
			// 
			// checkBoxVisualizer
			// 
			this.checkBoxVisualizer.AutoSize = true;
			this.checkBoxVisualizer.Location = new System.Drawing.Point(15, 256);
			this.checkBoxVisualizer.Name = "checkBoxVisualizer";
			this.checkBoxVisualizer.Size = new System.Drawing.Size(99, 17);
			this.checkBoxVisualizer.TabIndex = 18;
			this.checkBoxVisualizer.Text = "Show visualizer";
			this.checkBoxVisualizer.UseVisualStyleBackColor = true;
			this.checkBoxVisualizer.CheckedChanged += new System.EventHandler(this.checkBoxVisualizer_CheckedChanged);
			// 
			// comboBoxAI
			// 
			this.comboBoxAI.FormattingEnabled = true;
			this.comboBoxAI.Location = new System.Drawing.Point(13, 152);
			this.comboBoxAI.Name = "comboBoxAI";
			this.comboBoxAI.Size = new System.Drawing.Size(146, 21);
			this.comboBoxAI.TabIndex = 19;
			this.comboBoxAI.SelectedIndexChanged += new System.EventHandler(this.comboBoxAI_SelectedIndexChanged);
			// 
			// labelAvgScore
			// 
			this.labelAvgScore.AutoSize = true;
			this.labelAvgScore.Location = new System.Drawing.Point(156, 59);
			this.labelAvgScore.Name = "labelAvgScore";
			this.labelAvgScore.Size = new System.Drawing.Size(41, 13);
			this.labelAvgScore.TabIndex = 32;
			this.labelAvgScore.Text = "[Score]";
			// 
			// label17
			// 
			this.label17.AutoSize = true;
			this.label17.Location = new System.Drawing.Point(123, 59);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(29, 13);
			this.label17.TabIndex = 31;
			this.label17.Text = "Avg:";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(437, 310);
			this.Controls.Add(this.comboBoxAI);
			this.Controls.Add(this.checkBoxVisualizer);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.pictureLine);
			this.Controls.Add(this.labelInitialized);
			this.Controls.Add(this.labelFrameRate);
			this.Controls.Add(this.pictureFrameRate);
			this.Controls.Add(this.checkBoxAutoPlay);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.pictureInitLocated);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.pictureInit32Bit);
			this.Controls.Add(this.labelInitFoundGame);
			this.Controls.Add(this.pictureInitFoundGame);
			this.Controls.Add(this.labelErrorText);
			this.Controls.Add(this.labelError);
			this.Controls.Add(this.buttonStart);
			this.Controls.Add(this.buttonInit);
			this.Name = "Form1";
			this.Text = "Ms. Pacman AI player";
			((System.ComponentModel.ISupportInitialize)(this.pictureInitFoundGame)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureInit32Bit)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureInitLocated)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureFrameRate)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureLine)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonInit;		
		private System.Windows.Forms.Button buttonStart;
		private System.Windows.Forms.Label labelError;
		private System.Windows.Forms.Label labelErrorText;
		private System.Windows.Forms.PictureBox pictureInitFoundGame;
		private System.Windows.Forms.Label labelInitFoundGame;
		private System.Windows.Forms.PictureBox pictureInit32Bit;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.PictureBox pictureInitLocated;
		private System.Windows.Forms.CheckBox checkBoxAutoPlay;
		private System.Windows.Forms.Label labelFrameRate;
		private System.Windows.Forms.PictureBox pictureFrameRate;
		private System.Windows.Forms.Label labelInitialized;
		private System.Windows.Forms.PictureBox pictureLine;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label labelScore;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label labelState;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label labelLives;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label labelDebug;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label labelMaze;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label labelPills;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label labelDirection;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label labelFoundDirection;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label labelFoundPosition;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label labelDown;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label labelUp;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label labelRight;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label labelLeft;
		private System.Windows.Forms.Label label20;
		private System.Windows.Forms.Label labelHighscore;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.CheckBox checkBoxVisualizer;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label labelAI;
		private System.Windows.Forms.ComboBox comboBoxAI;
		private System.Windows.Forms.Label labelAvgScore;
		private System.Windows.Forms.Label label17;
	}
}

