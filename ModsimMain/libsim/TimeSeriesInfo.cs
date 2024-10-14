using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>
    /// Class to hold the variables used to manage the time series stored in a database
    /// </summary>
    public class TimeSeriesInfo
    {
        public TimeSeriesInfo(string dbPath, bool xyFileTimeSeries = true, int defaultScnID = 0)
        {
            this.xyFileTimeSeries = xyFileTimeSeries;
            activeScn = defaultScnID;
            this.dbPath = dbPath;
        }
        /// <summary>Timeseries management flag
        /// This flag indicates if the timeseries are stored in the xyfile, or in a project database.</summary>
        public bool xyFileTimeSeries = true;
        /// <summary>
        /// variable indicating the active time series scenario
        /// </summary>
        public int activeScn;
        /// <summary>
        /// Relative path to the time series database. The path is relative to the XY file location. 
        /// </summary>
        public string dbPath;
        /// <summary>
        /// Full path to the current file. It uses the XY file loaded location to define the path
        /// this variable is used to adjust relative paths when saving the XY to a different directory
        /// </summary>
        public string dbFullPath;
    }
}
