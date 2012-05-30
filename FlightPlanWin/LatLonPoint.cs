
namespace FlightPlanWin
{
	class LatLonPoint
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }

		public LatLonPoint(double Latitude, double Longitude)
		{
			this.Latitude = Latitude;
			this.Longitude = Longitude;
		}
	}
}
