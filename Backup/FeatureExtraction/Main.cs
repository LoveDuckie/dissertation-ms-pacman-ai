using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FeatureExtraction
{
	public partial class Main : Form
	{
		public Main() {
			InitializeComponent();
		}

		private void buttonRun_Click(object sender, EventArgs e) {
			// load resources
			System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.Stream file = thisExe.GetManifestResourceStream("FeatureExtraction.Resources.screenplayer.png");
			Bitmap sprites = (Bitmap)Bitmap.FromStream(file);
			pictureBoxSource.Image = sprites;
			// 
			int i = 6;
			Bitmap result = new Bitmap(14, 14);
			for( int x = 0; x < 14; x++ ) {
				for( int y = 0; y < 14; y++ ) {
					if( sprites.GetPixel(x + i * 14, y) == sprites.GetPixel(x + i * 14 + 14, y) &&
						sprites.GetPixel(x + i * 14, y) == sprites.GetPixel(x + i * 14 + 28, y) ) {
						result.SetPixel(x, y, sprites.GetPixel(x + i * 14, y));
					}
				}
			}
			if( false ) {
				for( int x = 0; x < 14; x++ ) {
					for( int y = 0; y < 14; y++ ) {
						if( result.GetPixel(x, y) == sprites.GetPixel(x + 0 * 14, y) ||
							result.GetPixel(x, y) == sprites.GetPixel(x + 1 * 14, y) ||
							result.GetPixel(x, y) == sprites.GetPixel(x + 2 * 14, y) ||
							result.GetPixel(x, y) == sprites.GetPixel(x + 9 * 14, y) ||
							result.GetPixel(x, y) == sprites.GetPixel(x + 10 * 14, y) ||
							result.GetPixel(x, y) == sprites.GetPixel(x + 11 * 14, y) ||
							result.GetPixel(x, y) == sprites.GetPixel(x + 3 * 14, y) ||
							result.GetPixel(x, y) == sprites.GetPixel(x + 4 * 14, y) ||
							result.GetPixel(x, y) == sprites.GetPixel(x + 5 * 14, y) ) {
							result.SetPixel(x, y, Color.Transparent);
						}
					}
				}
			}
			result.Save("Test.png");
			Bitmap bigResult = new Bitmap(112,112);
			Graphics g = Graphics.FromImage(bigResult);
			g.ScaleTransform(8.0f,8.0f);
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.DrawImage(result, new Point(0,0));
			pictureBoxResult.Image = bigResult;
		}
	}
}
