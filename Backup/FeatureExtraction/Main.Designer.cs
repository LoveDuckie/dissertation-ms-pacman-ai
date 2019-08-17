namespace FeatureExtraction
{
	partial class Main
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
			this.pictureBoxResult = new System.Windows.Forms.PictureBox();
			this.pictureBoxSource = new System.Windows.Forms.PictureBox();
			this.buttonRun = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxResult)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxSource)).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBoxResult
			// 
			this.pictureBoxResult.Location = new System.Drawing.Point(12, 33);
			this.pictureBoxResult.Name = "pictureBoxResult";
			this.pictureBoxResult.Size = new System.Drawing.Size(112, 112);
			this.pictureBoxResult.TabIndex = 0;
			this.pictureBoxResult.TabStop = false;
			// 
			// pictureBoxSource
			// 
			this.pictureBoxSource.Location = new System.Drawing.Point(12, 160);
			this.pictureBoxSource.Name = "pictureBoxSource";
			this.pictureBoxSource.Size = new System.Drawing.Size(584, 398);
			this.pictureBoxSource.TabIndex = 1;
			this.pictureBoxSource.TabStop = false;
			// 
			// buttonRun
			// 
			this.buttonRun.Location = new System.Drawing.Point(12, 4);
			this.buttonRun.Name = "buttonRun";
			this.buttonRun.Size = new System.Drawing.Size(75, 23);
			this.buttonRun.TabIndex = 2;
			this.buttonRun.Text = "Run";
			this.buttonRun.UseVisualStyleBackColor = true;
			this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(779, 603);
			this.Controls.Add(this.buttonRun);
			this.Controls.Add(this.pictureBoxSource);
			this.Controls.Add(this.pictureBoxResult);
			this.Name = "Main";
			this.Text = "Feature Extraction";
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxResult)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxSource)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBoxResult;
		private System.Windows.Forms.PictureBox pictureBoxSource;
		private System.Windows.Forms.Button buttonRun;
	}
}

