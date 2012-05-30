using System;

namespace FlightPlanWin
{
	class GeoCalc
	{
		private const int EARTH_RADIUS = 3438;
		private const double DEG_TO_RAD = (Math.PI) / 180.0;

		public static double RhumbDistance(LatLonPoint from, LatLonPoint to)
		{
			double rhumbBearing = RhumbBearing(from, to);
			double distance = 0.0;
			double deltaCoLat = (to.Latitude - from.Latitude) * DEG_TO_RAD;

			if (rhumbBearing == 90.0 || rhumbBearing == 270.0)
				distance = Math.Abs(EARTH_RADIUS * Math.Cos(from.Latitude) * ((to.Longitude - from.Longitude) * DEG_TO_RAD));
			else
				distance = Math.Abs((deltaCoLat * EARTH_RADIUS) / Math.Cos(rhumbBearing * DEG_TO_RAD));

			return distance;
		}

		public static double RhumbBearing(LatLonPoint from, LatLonPoint to)
		{
			double rhumbBearing = 0.0;
			double deltaLong = (to.Longitude * DEG_TO_RAD) - (from.Longitude * DEG_TO_RAD);
			double latFactor1 = Math.Log(Math.Tan((Math.PI / 4.0) + ((from.Latitude * DEG_TO_RAD) / 2.0)));
			double latFactor2 = Math.Log(Math.Tan((Math.PI / 4.0) + ((to.Latitude * DEG_TO_RAD) / 2.0)));
			double deltaLatFactor = latFactor2 - latFactor1;

			if (deltaLatFactor == 0 && deltaLong == 0)
				rhumbBearing = 0;
			else
				rhumbBearing = (360 + ((Math.Atan2(deltaLong, deltaLatFactor) * 180.0) / Math.PI)) % 360;

			return rhumbBearing;
		}

	}
}
