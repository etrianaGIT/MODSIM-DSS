using System;

namespace Csu.Modsim.ModsimModel
{
    public class TimePeriod
    {
        private Model model; 
        private DateTime startDate;
        private TimeSpan length;

        /// <summary>Gets a value specifying the start date of the time period.</summary>
        public DateTime StartDate
        {
            get { return this.startDate; }
        }
        /// <summary>Gets a value specifying the length of the time period.</summary>
        public TimeSpan Length
        {
            get { return this.length; }
        }
        /// <summary>Gets the end date of the time period.</summary>
        public DateTime EndDate
        {
            get { return this.startDate.Add(this.length); } 
        }
        /// <summary>Gets the fraction of time that this time period instance overlaps with the current model timestep (overlapped time span divided by the timespan of the current model timestep).</summary>
        public double FractionWithinCurrentTimestep
        {
            get
            {
                DateTime start = this.StartDate;
                DateTime end = this.EndDate;
                DateTime tsStart = this.model.mInfo.CurrentBegOfPeriodDate;
                DateTime tsEnd = this.model.mInfo.CurrentEndOfPeriodDate;

                // Exit early for dates that don't overlap
                if (end < tsStart || tsEnd < start)
                    return 0;

                // Get the maximum starting date of downtime for the current timestep
                if (tsStart > start)
                    start = tsStart;

                // Get the minimum ending date of downtime for the current timestep
                if (tsEnd < end)
                    end = tsEnd;

                // Calculate factor
                TimeSpan span = end.Subtract(start);
                TimeSpan tsSpan = tsEnd.Subtract(tsStart);
                return span.TotalDays / tsSpan.TotalDays * 100;
            }
        }
        /// <summary>Gets whether this time period contains the ending date of the current model timestep. CurrentEndOfPeriodDate lies within [this.StartDate, this.EndDate)</summary>
        public bool ContainsEndOfCurrentTimeStep
        {
            get { return this.StartDate <= this.model.mInfo.CurrentEndOfPeriodDate && this.model.mInfo.CurrentEndOfPeriodDate < this.EndDate; }
        }
        /// <summary>Gets whether this time period contains the beginning date of the current model timestep. CurrentBegOfPeriodDate lies within [this.StartDate, this.EndDate)</summary>
        public bool ContainsBegOfCurrentTimeStep
        {
            get { return this.StartDate <= this.model.mInfo.CurrentBegOfPeriodDate && this.model.mInfo.CurrentBegOfPeriodDate < this.EndDate; }
        }

        /// <summary>Builds a TimePeriod class.</summary>
        /// <param name="model">The MODSIM model.</param>
        public TimePeriod(Model model, DateTime StartDate, TimeSpan Length)
        {
            if (model == null) throw new Exception("The model passed to this instance cannot be null.");
            this.model = model;
            this.startDate = StartDate;
            this.length = Length;
        }

        /// <summary>Copies this instance and assign new refereneces to the new model object.</summary>
        /// <param name="newModelReference">The new MODSIM model reference.</param>
        public TimePeriod Copy(Model newModelReference)
        {
            TimePeriod retVal = (TimePeriod)this.MemberwiseClone();
            retVal.model = newModelReference;
            return retVal;
        }
    }
}
