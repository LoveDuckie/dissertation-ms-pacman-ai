namespace Pacman.Simulator
{
	partial class Visualizer
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
			this.Picture = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.Picture)).BeginInit();
			this.SuspendLayout();
			// 
			// Picture
			// 
			this.Picture.Location = new System.Drawing.Point(0, -1);
			this.Picture.Name = "Picture";
			this.Picture.Size = new System.Drawing.Size(448, 542);
			this.Picture.TabIndex = 0;
			this.Picture.TabStop = false;
			// 
			// Visualizer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(449, 542);
			this.Controls.Add(this.Picture);
			this.Name = "Visualizer";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Visualizer";
			((System.ComponentModel.ISupportInitialize)(this.Picture)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.PictureBox Picture;
	}
}