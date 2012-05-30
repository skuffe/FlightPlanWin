
namespace FlightPlanWin
{
	class ColourState
	{
		public string Abbreviation { get; set; }
		public int Visibility { get; set; }
		public int Cloudbase { get; set; }

		public ColourState(string Abbreviation, int Visibility, int Cloudbase)
		{
			this.Abbreviation = Abbreviation;
			this.Visibility = Visibility;
			this.Cloudbase = Cloudbase;
		}
	}
}
