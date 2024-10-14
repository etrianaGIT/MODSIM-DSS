using System;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Hydrologic State Tables are used to set node and link values based on a "system state". Usually a "system" is one or more reservoir nodes where forecast and contents is summed. The summation is divided by sum of max volumes of listed reservoirs. This factor is compared to this table's set of hydBounds for the current time step date. The result is an index to use in selecting the current time step value for the nodes /links that have this table as their m->hydTable. Their can be any number of tables defined. Each table may have 1 or more reservoirs; reservoirs may or may not have forecasts / contents. Each table's hydBounds may have any number of states (number of columns) by up to the number of time steps in a year number of rows</summary>
    public class HydrologicStateTable 
    {
        /// <summary>The derived hydrologic state index for the current time step.</summary>
        /// <remarks>Values vary from 0 to 6, where 0 is dry and 6 is wet.</remarks>
        public int StateLevelIndex; 
        /// <summary>Specifies whether there are different hydrologic state boundaries for different timesteps.</summary>
        public bool VariesByTimeStep; 
        /// <summary>List of dates. One for each row of hydBounds.</summary>
        public DateList hydDates; 
        /// <summary>Factors that are the boundaries between each state index.</summary>
        public double[,] hydBounds; 
        /// <summary>Identification name for this table.</summary>
        public string TableName;
        /// <summary>Array of reservoir nodes</summary>
        /// <remarks>Reservoir nodes' contents and forecasts are summed. This sum is divided by the sum of maximum volumes of the same reservoirs. The resulting fraction is compared with the hydBounds associated with the current time step to define the time step hydrologic state index for this table.</remarks>
        public Node[] Reservoirs; 

        /// <summary>Builds a new instance.</summary>
        public HydrologicStateTable()
        {
            StateLevelIndex = 0;
            VariesByTimeStep = true;
            Reservoirs = new Node[0];
            hydBounds = new double[0, 0];
            hydDates = new DateList();
        }

        /// <summary>Gets the number of reservoirs.</summary>
        public int NumReservoirs
        {
            get
            {
                return Reservoirs.Length;
            }
        }
        /// <summary>Gets the number of hydrologic boundaries.</summary>
        public int NumHydBounds
        {
            get
            {
                return hydBounds.GetLength(0);
            }
        }
        /// <summary>Return the index of this hydrologic state table for the specifed data</summary>
        public bool IsLeapYear(int yr)
        {
            if ((yr % 400) == 0)
                return true;
            if ((yr % 100) == 0)
                return false;
            if ((yr % 4) == 0)
                return true;
            return false;
        }
        /// <summary>Gets the hydroligic state table index associated with a specified date.</summary>
        /// <param name="date">The date for which to find the hydrologic state table index.</param>
        /// <returns>Returns the hydroligic state table index associated with a specified date.</returns>
        public int HydTableDateIndex(DateTime date)
        {
            int i;
            int numdates = hydDates.Count;
            if (numdates == 0)
                new System.Exception("No Hydrologic State Table Dates defined");
            if (numdates == 1)
                return 0;
            int year = hydDates.Item(0).Year;
            int month = date.Month;
            int day = date.Day;
            if (hydDates.Item(0).Month != 1 || (hydDates.Item(0).Month == 1 && hydDates.Item(0).Day != 1))
            {
                if (month < hydDates.Item(0).Month || (month == hydDates.Item(0).Month && day < hydDates.Item(0).Day))
                {
                    if (hydDates.Item(numdates - 1).Year > year + 1)
                    {
                        throw new System.Exception("Problem with dates in Hydrologic State Table");
                    }
                    year = hydDates.Item(numdates - 1).Year;
                }
            }
            if (month == 2 && day == 29)
            {
                if (!IsLeapYear(year))
                    day = 28;
            }
            DateTime thisdate = new DateTime(year, month, day);
            for (i = 0; i < hydDates.Count; i++)
            {
                if (hydDates.Item(i) > thisdate)
                {
                    return i - 1;
                }
                else if (hydDates.Item(i) == thisdate)
                {
                    return i;
                }
            }
            return i - 1;
        }
    }
}
