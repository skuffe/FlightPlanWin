using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace FlightPlanWin
{
	class ColourState
	{
		public Color Colour { get; set; }
		public string Abbreviation { get; set; }
		public int Visibility { get; set; }
		public int Cloudbase { get; set; }

		public ColourState(Color Colour, string Abbreviation, int Visibility, int Cloudbase)
		{
			this.Colour = Colour;
			this.Abbreviation = Abbreviation;
			this.Visibility = Visibility;
			this.Cloudbase = Cloudbase;
		}
	}
}
