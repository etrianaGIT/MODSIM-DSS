using System;

namespace Csu.Modsim.ModsimIO
{
	//LegacyTimestep classs providees a bridge between the
	// pre-historic modsim xy file and current modsim xy file.
	// pre-historic used minor, and major to describe a list of
	// dates with a class originally called TimeStep
	public class LegacyTimeStep
	{
		public int major;
		public int minor;
		public int tsType;
		public bool InXYFile;
		public LegacyTimeStep()
		{
			major = 1;
			minor = 1;
			InXYFile = true;
		}
		//Converts a monthly LegacyTimeStep to a DateTime
		// used for accural dates, account balance dates, rent pool dates
		public DateTime ToMonthlyDate(DateTime startDate)
		{
			int month = startDate.AddMonths(this.major - 1).Month;
			DateTime rval = new DateTime(1900, month, 1);
			return rval;
		}

	}
}
