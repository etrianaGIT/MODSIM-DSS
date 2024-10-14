using System;
using System.Collections.Generic;
using System.Data;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Identifies the various types of TimeSeries data within MODSIM. This includes node and link timeseries, and a distinction must be made between the two.</summary>
    /// <remarks>If this enum is changed, GetLabel & GetLabels methods will necessarily need to change.</remarks>
    public enum TimeSeriesType
    {
        Undefined = -1,
        NonStorage,         // long     node NonStorage
        Targets,            // long     node Reservoir
        Generating_Hours,   // double   node Reservoir
        Evaporation,        // double   node Reservoir
        Forecast,           // long     node Reservoir
        Power_Target,       // double   node Demand
        Infiltration,       // double   node Demand
        Demand,             // long     node Demand
        Sink,               // long     node Demand
        VariableCapacity,   // long     link Link
        Measured            // double   link Link
    }

    /// <summary>TimeSeries class is the standard MODSIM I/O data structure for variable timestep data.</summary>
    public class TimeSeries
    {
        #region Instance Variables

        /// <summary>DataTable of date / value(s) for time series data</summary>
        /// <remarks> A minimal table has two columns; the first column is a date, second and any optional
        ///  columns are longs if IsFloatType is false (this is the default) or doubles if IsFloatType is true</remarks>
        private DataTable table;
        /// <summary>Index of the previously accessed row of data for performance in lookups</summary>
        private int prevIndex;
        /// <summary>Date associated with prevIndex</summary>
        private DateTime prevDate;
        /// <summary>True if data is doubleing point data (evaporation rate)</summary>
        public bool IsFloatType;
        /// <summary>The type of this instance.</summary>
        public TimeSeriesType Type = TimeSeriesType.Undefined;
        /// <summary>True if there are rows of data that vary for time steps beyond one year</summary>
        public bool VariesByYear;
        /// <summary>NOT IMPLEMENTED Flag if true will tell lookup functions to interpolate values for time steps not defined in the table</summary>
        public bool Interpolate; // this flag if true will tell the lookup functions to interpolate values for time steps not defined in the table
                                 /// <summary>True if the table has mulitple columns of data for each hydrologic state level</summary>
        public bool MultiColumn; //  if true, table has a data column for each hydrologic state level; if false, one data column
                                 /// <summary>Describes the units for this TimeSeries.</summary>
        public ModsimUnits units;
        private static string[] labels = GetLabels(false);
        private static string[] labels_Nodes = GetLabels_Nodes();
        private static string[] labels_Links = GetLabels_Links();

        #endregion

        #region Properties

        /// <summary>Gets or sets the DataTable for this time series.</summary>
        public DataTable dataTable
        {
            get
            {
                return table;
            }
            set
            {
                this.table = value;
            }
        }
        /// <summary>Gets whether this timeseries can have negative values within it.</summary>
        public bool CanHaveNegativeValues
        {
            get
            {
                return this.Type == TimeSeriesType.Evaporation || this.Type == TimeSeriesType.Power_Target;
            }
        }
        /// <summary>Gets an array of labels for TimeSeries associated with MODSIM nodes.</summary>
        public static string[] Labels_NodeTimeSeriesTypes
        {
            get
            {
                return labels_Nodes;
            }
        }
        /// <summary>Gets an array of labels for TimeSeries associated with MODSIM links.</summary>
        public static string[] Labels_LinkTimeSeriesTypes
        {
            get
            {
                return labels_Links;
            }
        }

        #endregion

        #region Constructor

        /// <summary>Constructor for <c>TimeSeries</c> for a specified type of <c>TimeSeries</c> data.</summary>
        /// <remarks>
        /// First date in time series MUST be dataStartDate.
        /// If VariesByYear is true, redundent data is not generally specified
        ///    i.e. if two or more consecutive dates have the same value, only the first needs to be
        ///         specified
        /// If VariesByYear is false.  first date still MUST be dataStartDate.
        ///   only one year of data should be entered. dates CANNOT extend beyond one year
        ///   the year part of the date is not used; during simulation, time series
        ///     year is set = simulation date year
        ///  data can be discontinous i.e. jan,feb,mar,july,sep.
        ///  </remarks>
        public TimeSeries(TimeSeriesType type)
        {
            SetupTable(type);
        }
        /// <summary>Creates the TimeSeries DataTable without any rows.</summary>
        public void SetupTable(TimeSeriesType type)
        {
            this.prevIndex = 0;
            this.Type = type;
            this.IsFloatType = (type == TimeSeriesType.Generating_Hours || type == TimeSeriesType.Infiltration || type == TimeSeriesType.Evaporation || type == TimeSeriesType.Power_Target);
            this.Interpolate = false;
            this.VariesByYear = false;
            this.MultiColumn = false;
            this.table = new DataTable();
            this.table.Columns.Add("Date", typeof(DateTime));
            this.table.Columns["Date"].Unique = true;
            this.table.Columns.Add("HS0", this.IsFloatType ? typeof(double) : typeof(long));
            this.table.PrimaryKey = new DataColumn[] { this.table.Columns[0] };
            this.prevDate = DateTime.MaxValue;
        }
        /// <summary>Copies this instance.</summary>
        /// <returns>Returns a copy of this instance.</returns>
        public TimeSeries Copy()
        {
            TimeSeries newTS = (TimeSeries)this.MemberwiseClone();
            newTS.table = this.table.Copy();
            if (this.units != null)
            {
                newTS.units = this.units.Copy();
            }
            return newTS;
        }

        #endregion

        #region Local Methods

        /// <summary>Returns true if there is a negative value in the timeseries. Otherwise, returns false. Sets all negative values to zero in timeseries that cannot have negative values. Informs user where a value changes.</summary>
        public bool HasNegativeValues()
        {
            bool HasNeg = false;
            for (int j = 1; j < this.table.Columns.Count; j++)
            {
                for (int i = 0; i < this.table.Rows.Count; i++)
                {
                    if (Convert.ToDouble(this.table.Rows[i][j]) < 0.0)
                    {
                        if (!this.CanHaveNegativeValues)
                        {
                            Model.FireOnErrorGlobal(string.Format(" Timeseries data at row {0} is negative ({1}), setting to zero", i + 1, Convert.ToDouble(this.table.Rows[i][j])));
                            this.table.Rows[i][j] = 0;
                        }
                        HasNeg = true;
                    }
                }
            }
            return HasNeg;
        }
        /// <summary>Returns true if there is a positive value in the timeseries. Otherwise, returns false.</summary>
        public bool HasPositiveValues()
        {
            for (int j = 1; j < this.table.Columns.Count; j++)
            {
                for (int i = 0; i < this.table.Rows.Count; i++)
                {
                    if (Convert.ToDouble(this.table.Rows[i][j]) > 0.0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>Sets the date column to read/write and returns the data table for this time series</summary>
        public DataTable GetTable()
        {
            DataColumn col = table.Columns[0];
            col.ReadOnly = false;
            return table;
        }
        /// <summary>Adds an empty row to the data table</summary>
        public void AddEmptyRow()
        {
            DataRow row = table.NewRow();
            row[0] = TimeManager.missingDate;
            for (int i = 1; i < table.Columns.Count; i++) // skip date column
            {
                if (IsFloatType)
                {
                    row[i] = 0.0;
                }
                else
                {
                    row[i] = 0;
                }
            }
            table.Rows.Add(row);
        }
        /// <summary>Adds an empty column to the data table</summary>
        public void AddEmptyColumn()
        {
            int nexths = table.Columns.Count - 1;
            string hslable = string.Concat("HS", Convert.ToString(nexths));
            table.Columns.Add(hslable, typeof(long));
            int numcols = table.Columns.Count;
            int numrows = table.Rows.Count;
            DataColumn col = table.Columns[numcols - 1];
            col.ReadOnly = false;
            for (int i = 0; i < numrows; i++)
            {
                if (IsFloatType)
                {
                    table.Rows[i][numcols - 1] = 0.0;
                }
                else
                {
                    table.Rows[i][numcols - 1] = 0;
                }
            }
        }
        /// <summary>Checks if the data type and first date are correct; if so, set this timeseries data table to the specified table</summary>
        public void setDataF(DataTable dt, DateTime dataStartDate)
        {
            if (this.IsFloatType == false)
            {
                throw new ArgumentException("You are trying to put double data into a long timeseries");
            }
            int numrows = dt.Rows.Count;
            if (numrows > 0)
            {
                DateTime firstrowdate = System.Convert.ToDateTime(dt.Rows[0][0]);
                if (firstrowdate != dataStartDate)
                {
                    throw new Exception("The first row MUST have the model TimeManager::dataStartDate");
                }
                if (dt.Columns.Count > 2)
                {
                    this.MultiColumn = true;
                    Model.FireOnErrorGlobal("TimeSeries MultiColumn set to true");
                }
                DateTime lastrowdate = System.Convert.ToDateTime(dt.Rows[numrows - 1][0]);
                if (lastrowdate > dataStartDate.AddYears(1))
                {
                    this.VariesByYear = true;
                    Model.FireOnErrorGlobal("TimeSeries VariesByYear set to true");
                }
            }
            this.table = dt;
        }
        /// <summary>Checks if the data type and first date are correct; if so, set this timeseries data table to the specified table</summary>
        public void setDataL(DataTable dt, DateTime dataStartDate)
        {
            if (this.IsFloatType == true)
            {
                throw new ArgumentException("You are trying to put long data into a double timeseries");
            }
            int numrows = dt.Rows.Count;
            if (numrows > 0)
            {
                DateTime firstrowdate = System.Convert.ToDateTime(dt.Rows[0][0]);
                if (firstrowdate != dataStartDate)
                {
                    throw new Exception("The first row MUST have the model TimeManager::dataStartDate");
                }
                if (dt.Columns.Count > 2)
                {
                    this.MultiColumn = true;
                    Model.FireOnErrorGlobal("TimeSeries MultiColumn set to true");
                }
                DateTime lastrowdate = System.Convert.ToDateTime(dt.Rows[numrows - 1][0]);
                if (lastrowdate > dataStartDate.AddYears(1))
                {
                    this.VariesByYear = true;
                    Model.FireOnErrorGlobal("TimeSeries VariesByYear set to true");
                }
            }
            this.table = dt;
        }
        /// <summary>Sets the specified index row value (all columns of data) to the specified data value. Sets each column (except the date column) of an indexed row to the specified value.</summary>
        /// <param name="index">The zero-based row index.</param>
        /// <param name="data">The data value.</param>
        public void setDataF(int index, double data)
        {
            int numhs = this.table.Columns.Count - 1; // first column is date
            for (int j = 0; j < numhs; j++)
            {
                setDataF(index, j, data);    // hs is incremented by one in setData(int, int, long)
            }
        }
        /// <summary>Sets the specifed row index and data column index to the specified data value.</summary>
        /// <param name="index">The zero-based row index.</param>
        /// <param name="hydStateIndex">The zero-based hydrologic state index.</param>
        /// <param name="data">The data value.</param>
        public void setDataF(int index, int hydStateIndex, double data)
        {
            while (index > table.Rows.Count - 1)
            {
                AddEmptyRow();
            }
            while (hydStateIndex > table.Columns.Count - 2)
            {
                AddEmptyColumn();
            }
            table.Rows[index][hydStateIndex + 1] = data;
        }
        /// <summary>Gets the first data column value (or hydrologic state) from the specifed index row.</summary>
        /// <param name="index">The zero-based row index.</param>
        public double getDataF(int index)
        {
            return getDataF(index, 0);
        }
        /// <summary>Gets the value in the specifed index row and data column.</summary>
        /// <param name="index">The zero-based row index.</param>
        /// <param name="hydStateIndex">The zero-based hydrologic state index.</param>
        public double getDataF(int index, int hydStateIndex)
        {
            if (index < table.Rows.Count)
            {
                return Convert.ToDouble(table.Rows[index][hydStateIndex + 1]);    // date in first column
            }
            else
            {
                return 0.0;
            }
        }
        /// <summary>Sets all columns (except the date column) at the specified row index to the specified data value.</summary>
        /// <param name="index">The zero-based row index.</param>
        /// <param name="data">The data value.</param>
        public void setDataL(int index, long data)
        {
            int numhs = this.table.Columns.Count - 1; // first column is date
            for (int j = 0; j < numhs; j++)
            {
                setDataL(index, j, data);    // hs is incremented by one in setData(int, int, long)
            }
        }
        /// <summary>Gets the value in the specifed row and data column.</summary>
        /// <param name="index">The zero-based row index.</param>
        /// <param name="hydStateIndex">The zero-based hydrologic state index.</param>
        /// <param name="data">The data value.</param>
        public void setDataL(int index, int hydStateIndex, long data)
        {
            while (index > table.Rows.Count - 1)
            {
                AddEmptyRow();
            }
            while (hydStateIndex > table.Columns.Count - 2)
            {
                AddEmptyColumn();
            }
            table.Rows[index][hydStateIndex + 1] = data;
        }
        /// <summary>Gets the value in the specified row from the first column (or hydrologic state).</summary>
        /// <param name="index">The zero-based row index.</param>
        public long getDataL(int index)
        {
            return getDataL(index, 0);
        }
        /// <summary>Gets the value in the specified row and column (or hydrologic state).</summary>
        /// <param name="index">The zero-based row index.</param>
        /// <param name="hydStateIndex">The zero-based hydrologic state index.</param>
        public long getDataL(int index, int hydStateIndex)
        {
            long value;
            if (index < table.Rows.Count && hydStateIndex < table.Columns.Count - 1)
            {
                value = Convert.ToInt64(table.Rows[index][hydStateIndex + 1]);    // date in first column
            }
            else
            {
                value = 0;
            }
            return value;
        }
        /// <summary>Sets the date in the table at the specified row index.</summary>
        public void setDate(int index, DateTime date)
        {
            while (index > table.Rows.Count - 1)
            {
                AddEmptyRow();
            }
            table.Rows[index][0] = date;
        }
        /// <summary>Gets the date in the table at the specified row index.</summary>
        public DateTime getDate(int index)
        {
            try
            {
                DateTime dt = DateTime.Parse(table.Rows[index][0].ToString());
                return dt; //(DateTime)table.Rows[index][0];
            }catch (Exception ex)
            {
                string txt = ex.Message;
                return new DateTime();
            }
        }
        /// <summary>Gets the number of rows in the data table.</summary>
        public int getSize()
        {
            return table.Rows.Count;
        }
        /// <summary>Gets the number of columns in the data table (including the date column).</summary>
        public int getNumCol()
        {
            return table.Columns.Count;
        }
        /// <summary>Gets the index number associated with the specified date.</summary>
        public int GetTsIndex(DateTime timeStepDate)
        {
            int i;
            int index;
            if (timeStepDate < prevDate)
            {
                index = 0;
            }
            else
            {
                index = prevIndex;
            }

            int numRows = getSize();
            if (numRows == 1)
            {
                return 0;
            }
            if (numRows > 0)
            {
                DateTime firstRowDate = getDate(0);
                if (timeStepDate < firstRowDate)
                {
                    string msg = string.Concat("The date ", timeStepDate.ToString(), " is  before the earliest date ", firstRowDate.ToString());
                    Model.FireOnErrorGlobal(msg);
                    throw new Exception(msg);
                }
                DateTime adjYearDate = timeStepDate;
                DateTime rowDate;
                if (VariesByYear == false)
                {
                    DateTime lastRowDate = getDate(numRows - 1);
                    if (DateTime.Compare(timeStepDate, lastRowDate) > 0)
                    {
                        try
                        {
                            if (firstRowDate.Month == 1 && firstRowDate.Day == 1)
                            {
                                if (DateTime.DaysInMonth(firstRowDate.Year, timeStepDate.Month) < timeStepDate.Day)
                                {
                                    adjYearDate = new DateTime(firstRowDate.Year, timeStepDate.Month, DateTime.DaysInMonth(firstRowDate.Year, timeStepDate.Month));
                                }
                                else
                                {
                                    adjYearDate = new DateTime(firstRowDate.Year, timeStepDate.Month, timeStepDate.Day);
                                }
                            }
                            else
                            {
                                if (timeStepDate.Month > firstRowDate.Month)
                                {
                                    if (DateTime.DaysInMonth(firstRowDate.Year, timeStepDate.Month) < timeStepDate.Day)
                                    {
                                        adjYearDate = new DateTime(firstRowDate.Year, timeStepDate.Month, DateTime.DaysInMonth(firstRowDate.Year, timeStepDate.Month));
                                    }
                                    else
                                    {
                                        adjYearDate = new DateTime(firstRowDate.Year, timeStepDate.Month, timeStepDate.Day);
                                    }
                                }
                                else if (timeStepDate.Month == firstRowDate.Month)
                                {
                                    if (timeStepDate.Day >= firstRowDate.Day)
                                    {
                                        if (DateTime.DaysInMonth(firstRowDate.Year, timeStepDate.Month) < timeStepDate.Day)
                                        {
                                            adjYearDate = new DateTime(firstRowDate.Year, timeStepDate.Month, DateTime.DaysInMonth(firstRowDate.Year, timeStepDate.Month));
                                        }
                                        else
                                        {
                                            adjYearDate = new DateTime(firstRowDate.Year, timeStepDate.Month, timeStepDate.Day);
                                        }
                                    }
                                    else
                                    {
                                        return numRows - 1;
                                    }
                                }
                                else
                                {
                                    if (DateTime.DaysInMonth(firstRowDate.Year + 1, timeStepDate.Month) < timeStepDate.Day)
                                    {
                                        adjYearDate = new DateTime(firstRowDate.Year + 1, timeStepDate.Month, DateTime.DaysInMonth(firstRowDate.Year + 1, timeStepDate.Month));
                                    }
                                    else
                                    {
                                        adjYearDate = new DateTime(firstRowDate.Year + 1, timeStepDate.Month, timeStepDate.Day);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string msg = string.Concat("ERROR: contact program developers.");
                            Model.FireOnErrorGlobal(msg);
                            throw new Exception(msg + Environment.NewLine + Environment.NewLine + ex.Message);
                        }
                        if (prevIndex == (numRows - 1))
                        {
                            if (DateTime.Compare(adjYearDate, getDate(prevIndex)) < 0)
                            {
                                index = 0;    // start at beginning of table
                            }
                        }
                        else
                        {
                            if (DateTime.Compare(adjYearDate, getDate(prevIndex + 1)) < 0)
                            {
                                index = 0;    // start at beginning of table
                            }
                        }
                    }
                }
                for (i = index; i < numRows; i++)
                {
                    rowDate = getDate(i);
                    if (DateTime.Compare(rowDate, adjYearDate) > 0)
                    {
                        index = i - 1;
                        break;
                    }
                    if (DateTime.Compare(rowDate, adjYearDate) == 0)
                    {
                        index = i;
                        break;
                    }
                }
                if (i == numRows)
                {
                    index = i - 1;
                }
            }
            prevDate = getDate(index);
            prevIndex = index;
            return index;
        }
        /// <summary>Locates the row index of a date within the table.</summary>
        /// <param name="date">The date to look for.</param>
        /// <param name="rowIndex">If the date were to be found in the table, this is the index it would be.</param>
        /// <returns>Returns true if the exact date was found, and false if not. The row index will be located wherever the missing date would be located.</returns>
        public bool FindDate(DateTime date, out int rowIndex)
        {
            rowIndex = 0;
            if (this.table.Rows.Count == 0)
            {
                return false;
            }

            // Search
            int i = 0;
            while (date < DateTime.Parse(this.table.Rows[i][0].ToString()))
            {
                i++;
            }

            // This is the row where the date would be found
            rowIndex = i;
            if (date == DateTime.Parse(this.table.Rows[i][0].ToString()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>Updates the table with a new data start date.</summary>
        /// <param name="NewStartDate">The new data start date to update the table to.</param>
        public void ChangeStartDate(DateTime NewStartDate, ModsimTimeStep timeStep)
        {
            // Quick error checks
            if (this.table.Rows.Count == 0)
            {
                return;
            }
            if (this.Type == TimeSeriesType.Targets)
            {
                NewStartDate = timeStep.IncrementDate(NewStartDate);
            }
            if (DateTime.Parse(this.table.Rows[0][0].ToString()) == NewStartDate)
            {
                return;
            }

            // If there's only one value in the table just change the date
            if (this.table.Rows.Count == 1)
            {
                this.table.Rows[0][0] = NewStartDate;
                return;
            }

            // Check if NewStartDate is in the datatable, if it is, use existing data
            DataRow dr = this.table.NewRow();
            string query = "Date = #" + NewStartDate.ToString(TimeManager.DateFormat) + "#";
            DataRow[] rows = this.table.Select(query);
            if (rows.Length == 1)
            {
                if (this.VariesByYear)
                {
                    // Clear the timeseries date prior to the NewStartDate
                    for (int i = 0; i < this.table.Rows.Count; i++)
                    {
                        if (NewStartDate > DateTime.Parse(this.table.Rows[i][0].ToString()))
                        {
                            this.table.Rows.RemoveAt(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                dr.ItemArray = rows[0].ItemArray;
                this.table.Rows.Remove(rows[0]);
                this.table.Rows.InsertAt(dr, 0);
                return;
            }

            // Insert the new start date since it doesn't exist in the table
            if (this.VariesByYear)
            {
                dr[0] = NewStartDate;
                for (int j = 1; j < this.table.Columns.Count; j++)
                {
                    dr[j] = 0.0;
                }
                this.table.Rows.InsertAt(dr, 0);
            }

            // Make the variesbyyear = false dates greater than the NewStartDate
            // BLL: this code is not really correct we should do a better job, but I'm out of time
            if (!this.VariesByYear)
            {
                for (int i = 0; i < this.table.Rows.Count; i++)
                {
                    DateTime date = DateTime.Parse(this.table.Rows[i][0].ToString());
                    date = date.AddYears(NewStartDate.Year - date.Year);
                    if (date < NewStartDate)
                    {
                        date = date.AddYears(1);
                    }
                    this.table.Rows[i][0] = date;
                }
                this.table.DefaultView.Sort = "Date";
                this.table = this.table.DefaultView.ToTable();
            }
        }

        // Helper functions for FillTable()
        /// <summary>Gets the minimum date between the two DateTime instances.</summary>
        /// <returns>Returns the minimum date of the two DateTime instances.</returns>
        private static DateTime MinDate(DateTime Date1, DateTime Date2)
        {
            if (Date1 < Date2)
            {
                return Date1;
            }
            else
            {
                return Date2;
            }
        }
        /// <summary>Shifts the dates in tables of timeseries that do not vary by year. The dates are shifted to equal a specified start date.</summary>
        /// <param name="ToStartDate">The start date to which the dates should be shifted. This should be the simulation start date.</param>
        private void ShiftTableDates(DateTime ToStartDate)
        {
            // Error checks
            if (this.VariesByYear)
            {
                throw new Exception("Dates are only shifted in timeseries processing for timeseries that do not vary by year.");
            }
            if (this.table.Rows.Count == 0 || this.table.Columns.Count == 0)
            {
                return;
            }

            // Define the variables
            DateTime dataStart = DateTime.Parse(this.table.Rows[0][0].ToString());
            DateTime dataEnd = dataStart.AddYears(1);
            DateTime currDate;
            int yearsDiff = ToStartDate.Year - dataStart.Year;
            int NumOfRows = this.table.Rows.Count;
            int i;

            // Shift the dates
            for (i = 0; i < NumOfRows && (currDate = DateTime.Parse(this.table.Rows[i][0].ToString())) < dataEnd; i++)
            {
                currDate = currDate.AddYears(yearsDiff); // add the difference in years
                if (currDate < ToStartDate)
                {
                    currDate = currDate.AddYears(1);    // add a year if one date didn't quite make it
                }
                this.table.Rows[i][0] = currDate;
            }

            // Delete all rows on and after InputEndDate
            for (int j = i; j < NumOfRows; j++)
            {
                this.table.Rows[i].Delete();
            }

            // Go back through the table to ensure that dates are aligned correctly with the starting date
            DateTime startDatePlusOneYear = ToStartDate.AddYears(1);
            for (i = 0; i < this.table.Rows.Count; i++)
            {
                if ((currDate = DateTime.Parse(this.table.Rows[i][0].ToString())) >= startDatePlusOneYear)
                {
                    this.table.Rows[i][0] = currDate.AddYears(-1);
                }
            }

            // Resort the table.
            this.table.DefaultView.Sort = this.table.Columns[0].ColumnName + " ASC";
            this.table = this.table.DefaultView.ToTable();

        }
        /// <summary>Duplicates the first year's data a specified number of times.</summary>
        /// <param name="NumOfYearsToAdd">The number of times to duplicate the first year's data.</param>
        private void DuplicateFirstYearData(int NumOfYearsToAdd)
        {
            // Error checks
            if (this.VariesByYear)
            {
                throw new Exception("Dates are only shifted in timeseries processing for timeseries that do not vary by year.");
            }
            if (this.table.Rows.Count == 0)
            {
                return;
            }

            // Save the data from the first year to add to the beginning of the timeseries
            int NumOfRows = this.table.Rows.Count;
            int NumOfCols = this.table.Columns.Count;
            object[] begVals = this.table.Rows[NumOfRows - 1].ItemArray;

            // Add the years of data
            DataRow dr;
            for (int i = 1; i <= NumOfYearsToAdd; i++)
            {
                for (int j = 0; j < NumOfRows; j++)
                {
                    dr = this.table.NewRow();
                    dr[0] = (DateTime.Parse(this.table.Rows[j][0].ToString())).AddYears(i);
                    for (int k = 1; k < NumOfCols; k++)
                    {
                        dr[k] = this.table.Rows[j][k];
                    }
                    this.table.Rows.Add(dr);
                }
            }

            // Add repeating data to the beginning
            dr = this.table.NewRow();
            dr[0] = Convert.ToDateTime(begVals[0]).AddYears(-1);
            for (int j = 1; j < this.table.Columns.Count; j++)
            {
                dr[j] = begVals[j];
            }
            this.table.Rows.InsertAt(dr, 0);
        }
        /// <summary>Pads the beginning of the table with data for interpolation purposes.</summary>
        /// <param name="start">The start date of the simulation.</param>
        /// <param name="timeStep">The timestep of the simulation.</param>
        /// <param name="defaultValue">The value to place at the beginning of the table.</param>
        private void PadBegOfTable(DateTime start, ModsimTimeStep timeStep, double defaultValue)
        {
            DateTime origstart;
            if (this.Type == TimeSeriesType.Targets)
            {
                origstart = start;
                start = timeStep.IncrementDate(start); // Dates start at the beginning of the next timestep (i.e., the end of the current timestep).
            }
            else
            {
                origstart = start.AddMilliseconds(-1);
            }

            // If the data starts at the simulation start date or greater, add data to the beginning of the table.
            if (DateTime.Parse(this.table.Rows[0][0].ToString()) >= start)
            {
                DataRow dr = this.table.NewRow();
                dr[0] = origstart;
                for (int j = 1; j < this.table.Columns.Count; j++)
                {
                    dr[j] = defaultValue;
                }
                this.table.Rows.InsertAt(dr, 0);
            }
        }
        /// <summary>Pads the end of the table with data for interpolation purposes.</summary>
        /// <param name="end">The date at which the timeseries should end.</param>
        private void PadEndOfTable(DateTime end)
        {
            int NumOfRows = this.table.Rows.Count;
            int NumOfCols = this.table.Columns.Count;
            int i = (DateTime.Parse(this.table.Rows[NumOfRows - 1][0].ToString()) == end) ? 1 : 0;
            while (i != 2)
            {
                DataRow dr = this.table.NewRow();
                dr[0] = end.AddYears(i);
                for (int j = 1; j < NumOfCols; j++)
                {
                    dr[j] = this.table.Rows[NumOfRows - 1][j];
                }
                this.table.Rows.Add(dr);
                i++;
            }
        }
        /// <summary>Prepares the timeseries table to be processed.</summary>
        /// <param name="start">The start date of the simulation.</param>
        /// <param name="end">The end date of the simulation.</param>
        /// <param name="timeStep">The timestep of the simulation.</param>
        /// <param name="defaultValue">The default value of the timeseries which will be placed at the beginning of the datatable.</param>
        /// <remarks>This method exits if the table does not have any rows of data in it.</remarks>
        private void PrepareTable(DateTime start, DateTime end, ModsimTimeStep timeStep, double defaultValue)
        {
            // Exit if bad conditions
            if (this.table.Rows.Count == 0 || this.table.Columns.Count == 0)
            {
                return;
            }

            // Sort the table
            this.table.DefaultView.Sort = this.table.Columns[0].ColumnName + " ASC";
            this.table = this.table.DefaultView.ToTable();

            // First, add years to the timeseries table
            if (!this.VariesByYear)
            {
                ShiftTableDates(start);
                DuplicateFirstYearData(end.Year - start.Year + 1);
            }
            else
            {
                // Cut all extra data at the beginning out...
                while (this.table.Rows.Count > 1 && DateTime.Parse(this.table.Rows[1][0].ToString()) < start)
                {
                    this.table.Rows.RemoveAt(0);
                }

                // Add data to the end of the table for interpolation purposes.
                PadEndOfTable(end);
            }
            // Add data to the beginning of the table for interpolation purposes.
            PadBegOfTable(start, timeStep, defaultValue);
        }
        /// <summary>Gets the row containing data at or before the date at the start of the current timestep.</summary>
        /// <param name="currRow">The current zero-based row index.</param>
        /// <param name="date">The date at the start of the current timestep.</param>
        /// <param name="AtOrAfter">If true, the row at or after the specified date will be returned.</param>
        /// <returns>Returns the zero-based row index at or before <c>date</c>.</returns>
        private int GetRow(int currRow, DateTime date, bool AtOrAfter)
        {
            int NumOfRows = this.table.Rows.Count;
            for (; currRow < NumOfRows; currRow++)
            {
                if (DateTime.Parse(this.table.Rows[currRow][0].ToString()) > date)
                {
                    while (currRow >= 0 && DateTime.Parse(this.table.Rows[currRow][0].ToString()) > date)
                    {
                        currRow--;
                    }
                    if (currRow < 0)
                    {
                        throw new Exception("The model starting date should be within the timeseries table. Contact MODSIM team if this error persists.");    // PrepareTable needs to be called before this portion of the routine
                    }
                    if (AtOrAfter)
                    {
                        currRow++;
                    }
                    break;
                }
            }
            if (currRow == NumOfRows)
            {
                currRow--;
            }
            return currRow;
        }
        /// <summary>Fills an array of dates and an array of data between two dates near <c>currRow</c>.</summary>
        /// <param name="currRow">The current row of within the DataTable of this instance.</param>
        /// <param name="TimeStepStart">The date at the start of the timestep.</param>
        /// <param name="TimeStepEnd">The date at the end of the timestep.</param>
        /// <param name="dates">The array of dates.</param>
        /// <param name="data">A nested array of data.</param>
        /// <returns>Returns the row at the end of the current section.</returns>
        private int GetData(int currRow, DateTime TimeStepStart, DateTime TimeStepEnd, out DateTime[] dates, out double[][] data)
        {
            // Get the starting and ending rows
            int startRow = GetRow(currRow, TimeStepStart, false);
            int endRow = GetRow(startRow, TimeStepEnd, true);
            if (endRow < startRow)
            {
                throw new Exception("When searching for a date, the ending row ended up being smaller than the starting row. This should not be. Contact the MODSIM team if this error persists.");
            }

            // Get the data
            int NumOfCols = this.table.Columns.Count;
            dates = new DateTime[endRow - startRow + 1];
            data = new double[NumOfCols - 1][];
            for (int j = 1; j < NumOfCols; j++)
            {
                data[j - 1] = new double[endRow - startRow + 1];
            }
            for (currRow = startRow; currRow <= endRow; currRow++)
            {
                dates[currRow - startRow] = DateTime.Parse(this.table.Rows[currRow][0].ToString());
                for (int j = 1; j < NumOfCols; j++)
                {
                    data[j - 1][currRow - startRow] = Convert.ToDouble(this.table.Rows[currRow][j]);
                }
            }

            // Return the row at the end of the data.
            return endRow;
        }
        /// <summary>Gets an array of values at a specified date. If desired, will return the value that is interpolated from the table.</summary>
        /// <param name="currRow">The current row of within the DataTable of this instance.</param>
        /// <param name="AtDate">The date at which the value from the DataTable is to be extracted.</param>
        /// <param name="interpolate">Specifies whether to interpolate between rows if the date is not found to match.</param>
        /// <param name="dataVals"></param>
        /// <returns></returns>
        private int GetData(int currRow, DateTime AtDate, bool interpolate, out double[] dataVals)
        {
            currRow = GetRow(currRow, AtDate, false);
            dataVals = new double[this.table.Columns.Count - 1];
            if (interpolate && currRow + 1 < this.table.Rows.Count && AtDate != DateTime.Parse(this.table.Rows[currRow][0].ToString()))
            {
                for (int j = 1; j < this.table.Columns.Count; j++)
                {
                    double y2 = Convert.ToDouble(this.table.Rows[currRow + 1][j]);
                    double y1 = Convert.ToDouble(this.table.Rows[currRow][j]);
                    DateTime x2 = DateTime.Parse(this.table.Rows[currRow + 1][0].ToString());
                    DateTime x1 = DateTime.Parse(this.table.Rows[currRow][0].ToString());
                    dataVals[j - 1] = (y2 - y1) / x2.Subtract(x1).TotalDays * AtDate.Subtract(x1).TotalDays + y1;
                }
            }
            else
            {
                for (int j = 1; j < this.table.Columns.Count; j++)
                {
                    dataVals[j - 1] = Convert.ToDouble(this.table.Rows[currRow][j]);
                }
            }
            return currRow;
        }
        /// <summary>Determines whether the TimeSeries is already filled by checking the number of rows in the table and the units of this instance.</summary>
        /// <param name="model">The model for which this timeseries is being prepared.</param>
        /// <returns>Returns true if this TimeSeries is filled (has same number of rows and same units).</returns>
        public bool IsFilled(Model model)
        {
            return IsFilled(model.TimeStepManager.startingDate, model.timeStep, model.TimeStepManager.noModelTimeSteps, model.GetDefaultUnits(this.Type));
        }
        /// <summary>Determines whether the TimeSeries is already filled by checking the number of rows in the table and the units of this instance.</summary>
        /// <param name="ModelStartDate">The simulation starting date.</param>
        /// <param name="timeStep">The simulation timestep.</param>
        /// <param name="NumOfTimeSteps">The number of timesteps that the timeseries should be filling.</param>
        /// <param name="defaultUnits">The default units with which this timeseries is compared.</param>
        /// <returns>Returns true if this TimeSeries is filled (has same number of rows and same units).</returns>
        public bool IsFilled(DateTime ModelStartDate, ModsimTimeStep timeStep, int NumOfTimeSteps, ModsimUnits defaultUnits)
        {
            if (this.Type == TimeSeriesType.Undefined)
            {
                throw new Exception("Cannot check the filled status of an undefined TimeSeries type.");
            }

            bool unitsAreEqual = defaultUnits == null || this.units == null || this.units.Equals(defaultUnits);
            if (unitsAreEqual)
            {
                if (this.table.Rows.Count == 0)
                {
                    return true;
                }
                if (this.table.Rows.Count != NumOfTimeSteps)
                {
                    return false;
                }

                // Check the equality of each date in the table...
                int i;
                DateTime currDate;
                for (currDate = ModelStartDate, i = 0; i < NumOfTimeSteps; currDate = timeStep.IncrementDate(currDate), i++)
                {
                    if (!this.getDate(i).Equals(currDate))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>Checks to see if the units of this instance have the same type as <c>compareUnits</c>.</summary>
        /// <param name="compareUnits">The units to which to compare the units of this instance.</param>
        /// <returns>If this.units or compareUnits are null or the units are of the same spatial type, returns true. Otherwise, returns false.</returns>
        public bool UnitsHaveSameType(ModsimUnits compareUnits)
        {
            return (this.units == null || compareUnits == null || this.units.MajorUnitsTypeEquals(compareUnits.MajorUnits.GetType()));
        }
        /// <summary>Checks to see if the units of this instance are of the same type as <c>compareUnits</c>, and if not, or if null, changes the units to <c>compareUnits</c>.</summary>
        /// <param name="compareUnits">The units to which to compare the units of this instance.</param>
        /// <returns>Returns true if the units of this instance were changed. Otherwise, returns false.</returns>
        public bool EnsureUnitsHaveSameType(ModsimUnits compareUnits)
        {
            if (!this.UnitsHaveSameType(compareUnits))
            {
                this.units = compareUnits;
                return true;
            }
            return false;
        }
        /// <summary>Adds a row to the beginning of the table (if the table has no rows) with the default value all the way across.</summary>
        /// <param name="ModelStartDate">The starting date of the model.</param>
        /// <param name="defaultValue">The default value to place in the beginning row.</param>
        public void AddBegRow(DateTime ModelStartDate, double defaultValue)
        {
            if (this.table.Rows.Count == 0)
            {
                DataRow dr = this.table.NewRow();
                dr[0] = ModelStartDate;
                for (int j = 1; j < this.table.Columns.Count; j++)
                {
                    dr[j] = defaultValue;
                }
                this.table.Rows.Add(dr);
            }
        }
        /// <summary>Determines whether all the data in the timeseries table is equal to the defaul maximum capacity. If so, this method clears all the rows from the table and returns true.</summary>
        /// <param name="table">The timeseries table to check</param>
        public bool DataIsAllMaxDefaultCapacity()
        {
            for (int j = 1; j < this.table.Columns.Count; j++)
            {
                for (int i = 0; i < this.table.Rows.Count; i++)
                {
                    if (Convert.ToDouble(this.table.Rows[i][j]) != Model.RefModel.defaultMaxCap)
                    {
                        return false;
                    }
                }
            }
            this.table.Rows.Clear();
            return true;
        }

        // Fills table and converts units.
        /// <summary>Updates the timeseries table with every date in the modeling time period and fills the data in between user-provided dates. If <c>toUnits</c> is provided, the units of this instance are converted.</summary>
        /// <param name="model">The MODSIM model for which the TimeSeries is being filled.</param>
        /// <param name="defaultValue">The default value for a timeseries that does not have data at the beginning.</param>
        /// <returns>Returns true if the table is already filled or has no data. Otherwise, returns false.</returns>
        public bool FillTable(Model model, double defaultValue)
        {
            return FillTable(model, defaultValue, null);
        }
        /// <summary>Updates the timeseries table with every date in the modeling time period and fills the data in between user-provided dates. If <c>toUnits</c> is provided, the units of this instance are converted.</summary>
        /// <param name="model">The MODSIM model for which the TimeSeries is being filled.</param>
        /// <param name="defaultValue">The default value for a timeseries that does not have data at the beginning.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns true if the table is already filled or has no data. Otherwise, returns false.</returns>
        public bool FillTable(Model model, double defaultValue, ModsimUnits toUnits)
        {
            if (this.VariesByYear)
            {
                return FillTable(model.TimeStepManager.startingDate, model.TimeStepManager.endingDate, model.timeStep, defaultValue, toUnits);
            }
            else
            {
                return FillTable(model.TimeStepManager.startingDate, model.TimeStepManager.startingDate.AddYears(1), model.timeStep, defaultValue, toUnits);
            }
        }
        /// <summary>Updates the timeseries table with every date in the modeling time period and fills the data in between user-provided dates. If <c>toUnits</c> is provided, the units of this instance are converted.</summary>
        /// <param name="model">The MODSIM model for which the TimeSeries is being filled.</param>
        /// <param name="defaultValue">The default value for a timeseries that does not have data at the beginning.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <param name="defaultValueUnits">The units to convert the initial volume to.</param>
        /// <returns>Returns true if the table is already filled or has no data. Otherwise, returns false.</returns>
        public bool FillTable(Model model, double defaultValue, ModsimUnits toUnits, ModsimUnits defaultValueUnits)
        {
            if (this.VariesByYear)
            {
                return FillTable(model.TimeStepManager.startingDate, model.TimeStepManager.endingDate, model.timeStep, defaultValue, toUnits, defaultValueUnits);
            }
            else
            {
                return FillTable(model.TimeStepManager.startingDate, model.TimeStepManager.startingDate.AddYears(1), model.timeStep, defaultValue, toUnits, defaultValueUnits);
            }
        }
        /// <summary>Updates the timeseries table with every date in the modeling time period and fills the data in between user-provided dates. If <c>toUnits</c> is provided, the units of this instance are converted.</summary>
        /// <param name="ModelStartDate">The date at which the filled timeseries will start.</param>
        /// <param name="ModelEndDate">The date at which the filled timeseries will end.</param>
        /// <param name="timeStep">The timestep for which the dates will be filled.</param>
        /// <param name="defaultValue">The default value of the data.</param>
        /// <returns>Returns true if the table is already filled or has no data. Otherwise, returns false.</returns>
        public bool FillTable(DateTime ModelStartDate, DateTime ModelEndDate, ModsimTimeStep timeStep, double defaultValue)
        {
            return FillTable(ModelStartDate, ModelEndDate, timeStep, defaultValue, null);
        }
        /// <summary>Updates the timeseries table with every date in the modeling time period and fills the data in between user-provided dates. If <c>toUnits</c> is provided, the units of this instance are converted.</summary>
        /// <param name="ModelStartDate">The date at which the filled timeseries will start.</param>
        /// <param name="ModelEndDate">The date at which the filled timeseries will end.</param>
        /// <param name="timeStep">The timestep for which the dates will be filled.</param>
        /// <param name="defaultValue">The default value for a timeseries that does not have data at the beginning.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns true if the table is already filled or has no data. Otherwise, returns false.</returns>
        public bool FillTable(DateTime ModelStartDate, DateTime ModelEndDate, ModsimTimeStep timeStep, double defaultValue, ModsimUnits toUnits)
        {
            // error checks
            if (this.Type == TimeSeriesType.Undefined)
            {
                throw new Exception("The TimeSeries type needs to be defined before filling and converting the data.");
            }
            if (this.table.Rows.Count < 1 || this.table.Columns.Count < 2)
            {
                return true;
            }
            bool hasOneRow = this.table.Rows.Count == 1;
            this.EnsureUnitsHaveSameType(toUnits); // ensure the units are of the same type.

            // Get this.table read for processing... Make sure that data start and end dates are within range
            this.PrepareTable(ModelStartDate, ModelEndDate, timeStep, defaultValue);

            // Instantiate parameters to fill new DataTable
            bool isTarget = (this.Type == TimeSeriesType.Targets);
            bool IntegrateValues = !isTarget && this.Type != TimeSeriesType.Forecast && this.Type != TimeSeriesType.Infiltration && this.Type != TimeSeriesType.Sink && this.Type != TimeSeriesType.Undefined;
            bool ConvertValues = this.units != null && toUnits != null && this.Type != TimeSeriesType.Forecast && this.Type != TimeSeriesType.Infiltration && this.Type != TimeSeriesType.Sink && this.Type != TimeSeriesType.Undefined;
            if (!ConvertValues)
            {
                toUnits = null;
            }
            bool isRate = (this.units != null && this.units.IsRate);
            if (IntegrateValues && !isRate)
            {
                IntegrateValues = false;
            }
            int NumOfCols = this.table.Columns.Count;
            int currCol, currRow = 0;
            DateTime PrevDate = ModelStartDate;
            DateTime CurrDate = !isTarget ? ModelStartDate : timeStep.IncrementDate(ModelStartDate);
            DateTime NextDate = timeStep.IncrementDate(CurrDate);
            DateTime[] dates = null;
            double[][] data = null;
            DataRow dr;

            // clone current table
            DataTable newtbl = this.table.Clone();

            // Increment the dates and fill the table
            while (CurrDate <= ModelEndDate)
            {
                if (!isTarget && CurrDate == ModelEndDate)
                {
                    break;
                }

                // Get the dates and data from between the current date and the next date
                if (!IntegrateValues || !ConvertValues)
                {
                    data = new double[1][];
                    currRow = this.GetData(currRow, CurrDate, this.Interpolate, out data[0]);
                }
                else
                {
                    currRow = this.GetData(currRow, CurrDate, NextDate, out dates, out data);
                }

                // Add the integrated and converted data to the new table
                dr = newtbl.NewRow();
                dr[0] = isTarget ? PrevDate : CurrDate;
                if (!IntegrateValues || !ConvertValues)
                {
                    for (currCol = 1; currCol < NumOfCols; currCol++)
                    {
                        dr[currCol] = ConvertValues ? this.units.ConvertTo(data[0][currCol - 1], toUnits, CurrDate) : data[0][currCol - 1];
                    }
                }
                else
                {
                    for (currCol = 1; currCol < NumOfCols; currCol++)
                    {
                        dr[currCol] = this.units.Integrate(data[currCol - 1], dates, CurrDate, NextDate, this.Interpolate, toUnits);
                    }
                }
                newtbl.Rows.Add(dr);

                PrevDate = CurrDate;
                CurrDate = NextDate;
                NextDate = timeStep.IncrementDate(NextDate);
            }

            // Set the table in the timeseries
            this.table.Dispose();
            this.table = newtbl;

            // Update ts.units to match the updated units
            if (ConvertValues)
            {
                this.units = toUnits;
            }

            return false;
        }
        /// <summary>Updates the timeseries table with every date in the modeling time period and fills the data in between user-provided dates. If <c>toUnits</c> is provided, the units of this instance are converted.</summary>
        /// <param name="ModelStartDate">The date at which the filled timeseries will start.</param>
        /// <param name="ModelEndDate">The date at which the filled timeseries will end.</param>
        /// <param name="timeStep">The timestep for which the dates will be filled.</param>
        /// <param name="defaultValue">The default value for a timeseries that does not have data at the beginning.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <param name="defaultValueUnits">The units to convert the initial volume to.</param>
        /// <returns>Returns true if the table is already filled or has no data. Otherwise, returns false.</returns>
        public bool FillTable(DateTime ModelStartDate, DateTime ModelEndDate, ModsimTimeStep timeStep, double defaultValue, ModsimUnits toUnits, ModsimUnits defaultValueUnits)
        {
            // Error checks
            if (toUnits.Type != defaultValueUnits.Type)
            {
                throw new Exception("The toUnits variable needs to be the same type as defaultValueUnits.");
            }
            if (this.Type == TimeSeriesType.Undefined)
            {
                throw new Exception("The TimeSeries type needs to be defined before filling and converting the data.");
            }
            if (this.table.Rows.Count == 0)
            {
                return true;    // exit if there's no data...
            }

            // Convert the initial volume
            if (toUnits != null)
            {
                if (this.units == null)
                {
                    this.units = toUnits;
                }
                defaultValue = this.units.ConvertFrom(defaultValue, defaultValueUnits);
            }

            // Fill the rest of the table.
            return FillTable(ModelStartDate, ModelEndDate, timeStep, defaultValue, toUnits);
        }

        // Mathematical operations on data
        /// <summary>Multiplies each element in the timeseries by the specified factor.</summary>
        /// <param name="factor">The factor used to scale each element in the timeseries.</param>
        public void Scale(double factor)
        {
            for (int i = 0; i < this.table.Rows.Count; i++)
            {
                for (int j = 1; j < this.table.Columns.Count; j++)
                {
                    {
                        this.table.Rows[i][j] = factor * (double)this.table.Rows[i][j];
                    }
                }
            }
        }

        #endregion
        #region Shared Methods

        /// <summary>Gets a timeseries type from a string.</summary>
        /// <param name="label">The string defining a timeseries type.</param>
        public static TimeSeriesType GetType(string label)
        {
            label = label.ToLower().Replace(' ', '_');  // This convention needs to be exactly opposite that found in the GetLabel(type) method!!!
            for (int i = 0; i < labels.Length; i++)
            {
                if (label.Equals(labels[i].ToLower()))
                {
                    return (TimeSeriesType)i;
                }
            }
            return TimeSeriesType.Undefined;
        }
        /// <summary>Gets the pre-defined label for <c>type</c>.</summary>
        /// <param name="type">Specifies the type for which to obtain a label.</param>
        /// <returns>Returns the pre-defined label for <c>type</c>.</returns>
        public static string GetLabel(TimeSeriesType type)
        {
            return type.ToString().Replace('_', ' ');
        }
        /// <summary>Gets an array of all pre-defined labels for the <c>TimeSeriesType</c> enumerator with or without the Undefined value.</summary>
        /// <returns>Returns an array of all pre-defined labels for the <c>TimeSeriesType</c> enumerator with or without the Undefined value.</returns>
        public static string[] GetLabels(bool removeUndefined)
        {
            List<TimeSeriesType> aList = new List<TimeSeriesType>();
            foreach (TimeSeriesType type in Enum.GetValues(typeof(TimeSeriesType)))
            {
                aList.Add(type);
            }
            if (removeUndefined) aList.Remove(TimeSeriesType.Undefined);
            return aList.ConvertAll(x => x.ToString().Replace('_', ' ')).ToArray();
        }
        /// <summary>Gets an array of all pre-defined labels for the <c>TimeSeriesType</c> enumerator that are specifically found within MODSIM Nodes.</summary>
        /// <returns>Returns an array of all pre-defined labels for the <c>TimeSeriesType</c> enumerator that are specifically found within MODSIM Nodes.</returns>
        public static string[] GetLabels_Nodes()
        {
            List<string> aList = new List<string>(GetLabels(true));
            aList.Remove(TimeSeriesType.VariableCapacity.ToString());
            return aList.ToArray();
        }
        /// <summary>Gets an array of all pre-defined labels for the <c>TimeSeriesType</c> enumerator that are specifically found within MODSIM Links.</summary>
        /// <returns>Returns an array of all pre-defined labels for the <c>TimeSeriesType</c> enumerator that are specifically found within MODSIM Links.</returns>
        public static string[] GetLabels_Links()
        {
            return new string[1] { TimeSeriesType.VariableCapacity.ToString() };
        }

        #endregion

    }
}
