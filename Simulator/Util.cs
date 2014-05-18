using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace Pacman.Simulator
{
	public static class Util
	{
		public static Image LoadImage(string name) {
			System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.Stream file = thisExe.GetManifestResourceStream("Pacman.Simulator.Resources." + name);
			return Image.FromStream(file);
		}
	}
}
