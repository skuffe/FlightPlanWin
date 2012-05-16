using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;

namespace AirfieldModel
{
	public partial class FlightPlanContext : DbContext
	{
		public DbSet<Airfield> Airfields { get; set; }
	}
}
