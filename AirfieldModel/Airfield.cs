using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightPlanModel
{
	public class Airfield
	{
		public int AirfieldID { get; set; }
		public string Name { get; set; }
		public string ICAO { get; set; }
		public string Country { get; set; }
		public decimal Latitude { get; set; }
		public decimal Longitude { get; set; }
		public int Tilecol { get; set; }
		public int Tilerow { get; set; }

		// Bellow are not-mapped attributes, which means that they are not to be mapped to a database column;
		// .. we will handle filling them out with data ourselves.
		[NotMappedAttribute]
		public string Observation { get; set; }
		[NotMappedAttribute]
		public int? Visibility { get; set; }
		[NotMappedAttribute]
		public int? Cloudbase { get; set; }
		[NotMappedAttribute]
		public string ColourState { get; set; }
		[NotMappedAttribute]
		public string ObservationAge { get; set; }
        [NotMappedAttribute]
        public bool isInvalid { get; set; }
		[NotMappedAttribute]
		public double Distance { get; set; }
	}
}
