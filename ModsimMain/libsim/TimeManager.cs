using System;
using System.Data;
using System.Globalization;
using System.Threading;

namespace Csu.Modsim.ModsimModel
{
    public enum TypeIndexes : int
    {
        ModelIndex = 1,
        DataIndex = 2
    }

    /// <summary>Manages a list of all time steps and indexes to each time step.</summary>
    public class TimeManager
    {
        private readonly static string defDateFormat = "MM/dd/yyyy HH:mm:ss";
        private readonly static string defDateFormat_ShortDate = "MM/dd/yyyy";
        private readonly static string defDateFormat_ShortDate_GUI = "M/d/yyyy";
        private readonly static string defDateFormat_NoYear = "MM/dd HH:mm:ss";
        private readonly static string defDateFormat_Old = "MM/dd/yyyy HH:mm";
        private readonly static string defDBDateFormat = "yyyy-MM-dd HH:mm:ss"; 
        /// <summary>Gets the date format used for MODSIM dates.</summary>
        public static string DateFormat { get { return defDateFormat; } }
        /// <summary>Gets the short date format used for MODSIM dates.</summary>
        public static string DateFormat_ShortDate { get { return defDateFormat_ShortDate; } }
        /// <summary>Gets the short date format used for MODSIM GUI dates.</summary>
        public static string DateFormat_ShortDate_GUI { get { return defDateFormat_ShortDate_GUI; } }
        /// <summary>Gets the date format used for MODSIM dates when varies-by-year is false.</summary>
        public static string DateFormat_NoYear { get { return defDateFormat_NoYear; } }
        /// <summary>Gets the date format used for MODSIM 8.1 or earlier.</summary>
        public static string DateFormat_Old { get { return defDateFormat_Old; } }
        /// <summary>The date that is returned if a date is missing or not found.</summary>
        public readonly static DateTime missingDate = new DateTime();
        /// <summary>Gets the date format used by the SQLite database.</summary>
        public static string DateFormat_DB { get { return defDBDateFormat; } }

        private void CreateTimeStepTable(bool UseDataEndDate)
        {
            if (endingDate == missingDate || startingDate == missingDate || dataEndDate == missingDate || dataStartDate == missingDate)
                return;
            timeStepsList = new DataTable();
            timeStepsList.Columns.Add("IniDate", typeof(DateTime));
            timeStepsList.Columns.Add("EndDate", typeof(DateTime));
            timeStepsList.Columns.Add("ModelTSIndex", typeof(int));
            timeStepsList.Columns.Add("DataTSIndex", typeof(int));
            timeStepsList.Columns.Add("YearIndex", typeof(int));
            timeStepsList.Columns.Add("MonthIndex", typeof(int));
            timeStepsList.Columns.Add("MidDate", typeof(DateTime));
            timeStepsList.Columns.Add("Duration", typeof(double));
            timeStepsList.PrimaryKey = new DataColumn[] { timeStepsList.Columns["IniDate"] };
            if (endingDate > dataEndDate)
                dataEndDate = endingDate;
            int minTS = 0;
            int mayTS = 1;
            int modelIndex = 0;
            int dataIndex = 0;
            //add the starting date
            DateTime iniLoopDate = startingDate;
            bool modeledTime = false;
            DataRow newRow;
            m_noModelTimeSteps = 0;
            DateTime endLoopDate = startingDate;
            DateTime StopDate = UseDataEndDate ? dataEndDate : endingDate;
            while (endLoopDate < StopDate)
            {
                if (iniLoopDate == startingDate) //Found starting simulation date
                    modeledTime = true;
                newRow = timeStepsList.NewRow();
                endLoopDate = m_TimeStep.IncrementDate(iniLoopDate);

                //Count of the number of model time steps.
                if ((iniLoopDate >= startingDate) && (iniLoopDate <= endingDate))
                    m_noModelTimeSteps += 1;

                newRow["IniDate"] = iniLoopDate;
                newRow["EndDate"] = endLoopDate;
                //Calculate Mid Date and time step duration in seconds
                System.TimeSpan diff1 = new System.TimeSpan();
                diff1 = endLoopDate.Subtract(iniLoopDate);
                DateTime m_MidDate = iniLoopDate.AddSeconds(diff1.TotalSeconds / 2);
                newRow["MidDate"] = m_MidDate;
                newRow["Duration"] = diff1.TotalSeconds;
                iniLoopDate = endLoopDate;
                if (modeledTime)
                {
                    if (minTS == m_TimeStep.NumOfTSsForV7Output)
                    {
                        minTS = 0;
                        mayTS += 1;
                    }
                    newRow["ModelTSIndex"] = modelIndex;
                    newRow["MonthIndex"] = minTS;
                    newRow["YearIndex"] = mayTS;
                }
                newRow["DataTSIndex"] = dataIndex;
                timeStepsList.Rows.Add(newRow);
                if (modeledTime)
                {
                    modelIndex += 1;
                    minTS += 1;
                }
                dataIndex += 1;
            }
        }
        private ModsimTimeStep m_TimeStep;
        /// <summary>DataTable of dates and indices</summary>
        public DataTable timeStepsList; //Contains a list of time steps for both data and simulation.  It keeps the relationship between dates and time step indexes ( including mon and iy variables).
        /// <summary>Earliest date of any TimeSeries data</summary>
        public DateTime dataStartDate; //Date the begin piece of data
        /// <summary>Latest date of any TimeSeries data</summary>
        public DateTime dataEndDate; //Date the end piece of data
        /// <summary>Date of the beginning of the first time step of the simulation run</summary>
        public DateTime startingDate; // date the simulation begins
        /// <summary>Date of the beginning of the last time step of the simulation run</summary>
        public DateTime endingDate; // last date of the simulation
        /// <summary>Returns the maximum number of time steps in the data set</summary>
        public int noDataTimeSteps //Total number of time steps that are entered by the user in the interface.
        {
            get
            {
                return m_noDataTimeSteps;
            }
        }
        /// <summary>Returns the number of model run time steps</summary>
        public int noModelTimeSteps //Total number of time steps to be modeled. This value can be less or equal to the data time steps.
        {
            get
            {
                return m_noModelTimeSteps;
            }
        }
        /// <summary>Returns m_noBackRAdditionalTSteps indicates the number of addtional time steps needed for backrouting</summary>
        public int noBackRAdditionalTSteps //Additional time steps that are need it to run a problem with Back Routing. This value is mainly used to size the arrays.
        {
            get
            {
                return m_noBackRAdditionalTSteps;
            }
        }
        /// <summary>Resets the default DataTable of dates and indices for the specified model time step</summary>
        public void UpdateTimeStepsInfo(ModsimTimeStep timeStep)
        {
            // Set the culture and UI culture before
            // the call to InitializeComponent.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            m_TimeStep = timeStep;
            CreateTimeStepTable(true);
            m_noDataTimeSteps = timeStepsList.Rows.Count;
            CreateTimeStepTable(false);
            m_noBackRAdditionalTSteps = 0;
        }
        /// <summary>Returns the index for the specified BEGINNING of time step date; index is either data or model index as specifed</summary>
        public int Date2Index(DateTime myDate, TypeIndexes indexType)
        {
            string query = "IniDate = #" + myDate.ToString(TimeManager.DateFormat) + "#";
            DataRow[] rows = timeStepsList.Select(query);
            if (rows.Length > 0)
            {
                if (rows.Length == 1)
                {
                    if (indexType == TypeIndexes.ModelIndex)
                        return System.Convert.ToInt32(rows[0]["ModelTSIndex"]);
                    else
                        return System.Convert.ToInt32(rows[0]["DataTSIndex"]);
                }
                else
                    return -1;
            }
            return -1;
        }
        /// <summary>Returns the index for the specified ENDING of time step date; index is either data or model as specified</summary>
        public int EndDate2Index(DateTime myDate, TypeIndexes indexType)
        {
            string query = "EndDate = #" + myDate.ToString(TimeManager.DateFormat) + "#";
            DataRow[] rows = timeStepsList.Select(query);
            if (rows.Length > 0)
            {
                if (rows.Length == 1)
                {
                    if (indexType == TypeIndexes.ModelIndex)
                        return System.Convert.ToInt32(rows[0]["ModelTSIndex"]);
                    else
                        return System.Convert.ToInt32(rows[0]["DataTSIndex"]);
                }
                else
                    return 0;
            }
            else
                return 0;
        }
        /// <summary>Returns the BEGINNING of time step date for the specifed index; index is either data or model as specified</summary>
        public DateTime Index2Date(int myIndex, TypeIndexes indexType)
        {
            DataRow row = timeStepsList.Rows[myIndex];
            if (row != null)
            {
                return Convert.ToDateTime(row["IniDate"]);
            }
            return missingDate;
        }
        /// <summary>Returns the ending data for the timestep specified by myIndex, which is either indexing data or modeled timesteps.</summary>
        public DateTime Index2EndDate(int myIndex, TypeIndexes indexType)
        {
            DataRow row = timeStepsList.Rows[myIndex];
            if (row != null)
            {
                return Convert.ToDateTime(row["EndDate"]);
            }
            return missingDate;
        }
        /// <summary>Returns the starting date of the time step following myIniDate.</summary>
        public DateTime GetNextIniDate(DateTime myIniDate)
        {
            DateTime nextDate = m_TimeStep.IncrementDate(myIniDate);
            if (nextDate > endingDate)
                return endingDate;
            return nextDate;
        }
        /// <summary>Returns the year index for a given timestep starting date.</summary>
        public int GetYearIndex(DateTime myDate)
        {
            string query = "IniDate = #" + myDate.ToString(TimeManager.DateFormat) + "#";
            DataRow[] rows = timeStepsList.Select(query);
            if (rows.Length > 0)
            {
                if (rows.Length == 1)
                    return System.Convert.ToInt32(rows[0]["YearIndex"]);
                else
                    return -1;
            }
            return -1;
        }
        /// <summary>Returns "Year/Week/Quarter" index for a given time step index; index is either data or model as specified</summary>
        public int GetYearIndex(int myIndex, TypeIndexes indexType)
        {
            int rval = -1;

            DataRow row = timeStepsList.Rows[myIndex];
            if (row != null)
            {
                rval = Convert.ToInt32(row["YearIndex"]);
            }
            return rval;
        }
        /// <summary>Returns "Month/Week/Day" index for a given BEGINNING of time step date</summary>
        public int GetMonthIndex(DateTime myDate)
        {
            DataRow[] rows = timeStepsList.Select("IniDate = #" + myDate.Month.ToString() + "/" + myDate.Day.ToString() + "/" + myDate.Year.ToString() + "#");
            if (rows.Length > 0)
            {
                if (rows.Length == 1)
                {
                    return System.Convert.ToInt32(rows[0]["MonthIndex"]);
                }
                else
                {
                    return -1;
                    //throw new System::Exception("Current date not found in the time steps table");
                }
            }
            return -1;
        }
        /// <summary>Returns "Month/Week/Day" index for a given time step index; index is either data or model as specified</summary>
        public int GetMonthIndex(int myIndex, TypeIndexes indexType)
        {
            int rval = -1;

            DataRow row = timeStepsList.Rows[myIndex];
            if (row != null)
            {
                rval = Convert.ToInt32(row["MonthIndex"]);
            }
            return rval;

        }
        /// <summary>Extends the DataTable by the specified number of time steps</summary>
        public void ExtendTSTable(ModsimTimeStep timeStep, int maxLags)
        {
            DateTime loopDate = endingDate;
            m_noBackRAdditionalTSteps = 0;
            for (int i = 0; i < maxLags; i++)
                loopDate = timeStep.IncrementDate(loopDate);

            if (loopDate > endingDate)
            {
                DateTime extDateEnd = loopDate;
                DateTime iniLoopDate = endingDate;
                DataRow row = timeStepsList.Rows[timeStepsList.Rows.Count - 1];
                int minTS = System.Convert.ToInt32(row["MonthIndex"]);
                int mayTS = System.Convert.ToInt32(row["YearIndex"]);
                int modelIndex = System.Convert.ToInt32(row["ModelTSIndex"]);
                int dataIndex = System.Convert.ToInt32(row["DataTSIndex"]);
                while (iniLoopDate < extDateEnd)
                {
                    minTS += 1;
                    modelIndex += 1;
                    dataIndex += 1;
                    DataRow newRow = timeStepsList.NewRow();
                    newRow["IniDate"] = iniLoopDate;
                    iniLoopDate = timeStep.IncrementDate(iniLoopDate);
                    newRow["EndDate"] = iniLoopDate;
                    if (minTS == timeStep.NumOfTSsForV7Output)
                    {
                        minTS = 0;
                        mayTS += 1;
                    }
                    newRow["ModelTSIndex"] = modelIndex;
                    newRow["DataTSIndex"] = dataIndex;
                    newRow["MonthIndex"] = minTS;
                    newRow["YearIndex"] = mayTS;
                    timeStepsList.Rows.Add(newRow);
                }
            }
            //It needed even if we don't reach the max in time series for the time series arrays
            m_noBackRAdditionalTSteps = maxLags;
        }

        protected int m_noModelTimeSteps;
        protected int m_noDataTimeSteps;
        protected int m_noBackRAdditionalTSteps;
    }
}
