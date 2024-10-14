using System;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Types of user-defined timesteps allowed.</summary>
    /// <remarks>Sorted by timestep length.</remarks>
    public enum ModsimUserDefinedTimeStepType : int
    {
        Undefined = -1,
        seconds = 0,
        minutes = 1,
        hours = 2,
        days = 3,
        weeks = 4,
        months = 5,
        years = 6
    }

    public class ModsimUserDefinedTimeStep
    {
        #region Shared instance variables

        private static string privDefaultLabel = "user-defined";
        private static double privDefaultTimeSpan = 0.0;
        private static double privDefaultAllowableErrorInDays = 1.157e-08; // slightly less than 1 millisecond (1.15740740741e-08)
        private static string privDefaultUndefinedLabel = "undefined";

        #endregion
        #region Local instance variables

        private string privLabel = "";
        private double privSpan = privDefaultTimeSpan;
        private ModsimUserDefinedTimeStepType privType = ModsimUserDefinedTimeStepType.Undefined;

        #endregion

        #region Properties

        /// <summary>Gets or sets the label of this instance. Can be overridden.</summary>
        public string UserDefLabel
        {
            get
            {
                return privLabel;
            }
            set
            {
                privLabel = value;
            }
        }
        /// <summary>Gets or sets the span associated with the ModsimUserDefinedTimeStepType of this instance.</summary>
        public double UserDefSpan
        {
            get
            {
                return privSpan;
            }
            set
            {
                privSpan = value;
            }
        }
        /// <summary>Gets or sets the ModsimUserDefinedTimeStepType of this instance.</summary>
        public ModsimUserDefinedTimeStepType UserDefTSType
        {
            get
            {
                return privType;
            }
            set
            {
                privType = value;
            }
        }

        #endregion
        #region Virtual properties

        /// <summary>Gets the average TimeSpan of this instance.</summary>
        public virtual TimeSpan AverageTimeSpan
        {
            get
            {
                if (privType <= ModsimUserDefinedTimeStepType.weeks)
                    return ToTimeSpan(new DateTime(0));
                else
                {
                    int span = (int)privSpan;
                    switch (privType)
                    {
                        case ModsimUserDefinedTimeStepType.months:
                            return TimeSpan.FromDays(30.4375 * span);
                        case ModsimUserDefinedTimeStepType.years:
                            return TimeSpan.FromDays(365.25 * span);
                        default:
                            throw new Exception("An unrecognized type was passed to this method.");
                    }
                    //// Average over 4 years (this does not capture timesteps that do not hit the 4 year mark exactly)
                    //DateTime startdate = new DateTime(2012, 1, 1);
                    //DateTime enddate = new DateTime(2016, 1, 1);
                    //DateTime date;
                    //int ctr = 0;
                    //for (date = startdate; date < enddate; date = IncrementDate(date))
                    //    ctr++;
                    //return TimeSpan.FromDays(date.Subtract(startdate).TotalDays / (double)ctr);
                }
            }
        }

        #endregion
        #region Shared properties

        /// <summary>Gets or sets the default undefined label. Default is "undefined".</summary>
        public static string DefaultUndefinedLabel
        {
            get
            {
                return privDefaultUndefinedLabel;
            }
            set
            {
                privDefaultUndefinedLabel = value;
            }
        }
        /// <summary>Gets or sets the default user-defined label. Default is "user-defined".</summary>
        public static string DefaultUserDefLabel
        {
            get
            {
                return privDefaultLabel;
            }
            set
            {
                privDefaultLabel = value;
            }
        }
        /// <summary>Gets and sets the allowable error when converting doubles or TimeSpans to ModsimUserDefinedTimeStep.</summary>
        /// <value>A double-precision value. Its default is a little more than 1 millisecond accuracy... If the user specifies a timestep of 1 day and 0.5 milliseconds, than the timestep will automatically be converted to 1 day unless this instance variable is reduced.</value>
        public static double AllowableErrorInDays
        {
            get
            {
                return privDefaultAllowableErrorInDays;
            }
            set
            {
                privDefaultAllowableErrorInDays = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>Builds a new instance of ModsimTimeStep with a user-defined time step.</summary>
        /// <param name="type">Specifies the ModsimUserDefinedTimeStepType to use when creating an instance.</param>
        /// <remarks>The ModsimTimeStepType associated with this span is inferred with an accuracy of 1 millisecond.</remarks>
        public ModsimUserDefinedTimeStep(ModsimUserDefinedTimeStepType type) : this(1.0, type)
        {
        }
        /// <summary>Builds a new instance of ModsimTimeStep with a user-defined time step.</summary>
        /// <param name="span">Specifies a TimeSpan for the new instance of a timestep.</param>
        /// <param name="type">Specifies the ModsimUserDefinedTimeStepType to use when creating an instance.</param>
        /// <remarks>The ModsimTimeStepType associated with this span is inferred with an accuracy of 1 millisecond.</remarks>
        public ModsimUserDefinedTimeStep(double span, ModsimUserDefinedTimeStepType type)
        {
            this.privType = type;
            this.privSpan = span;
            this.privLabel = GetUserDefLabel(span, type);

            // Make sure span is a whole number for months and years (it doesn't make sense to have a timestep of 1.6 months...) 
            if (type > ModsimUserDefinedTimeStepType.weeks)
                if (span - (double)((int)span) > AllowableErrorInDays / AverageTimeSpan.TotalDays)
                    throw new Exception("Cannot define a monthly or yearly timestep with a non-integer span.");
        }

        #endregion

        #region Overrides

        /// <summary>Gets the label of this instance.</summary>
        /// <returns>Returns the label of this instance.</returns>
        public override string ToString()
        {
            return UserDefLabel;
        }
        /// <summary>Checks equality between this instance of <c>ModsimUserDefinedTimeStep</c> and another instance by checking the timestep. They do not have to have the same label.</summary>
        /// <returns>Returns true if the two instances have the same time spans and timestep types; otherwise, false.</returns>
        public override bool Equals(object timeStep)
        {
            if (timeStep == null || !this.GetType().Equals(timeStep.GetType()))
                return false;
            ModsimUserDefinedTimeStep ts = (ModsimUserDefinedTimeStep)timeStep;
            return (Math.Abs(ts.UserDefSpan - this.UserDefSpan) < AllowableErrorInDays && ts.UserDefTSType == this.UserDefTSType);
        }
        /// <summary>Gets the hash code from the base class.</summary>
        /// <returns>Returns the base class's hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
        #region Operators

        /// <summary>Checks equality between two instances of <c>ModsimUserDefinedTimeStep</c>. They do not have to have the same label.</summary>
        /// <returns>Returns true if the two instances have the same time spans and timestep types; otherwise, returns false.</returns>
        public static bool operator ==(ModsimUserDefinedTimeStep a, ModsimUserDefinedTimeStep b)
        {
            if ((object)a == null || (object)b == null)
                if ((object)a == null && (object)b == null)
                    return true;
                else return false;
            else
                return a.Equals(b);
        }
        /// <summary>Checks equality between two instances of <c>ModsimUserDefinedTimeStep</c>. They do not have to have the same label.</summary>
        /// <returns>Returns false if the two instances have the same time spans and timestep types; otherwise, returns true.</returns>
        public static bool operator !=(ModsimUserDefinedTimeStep a, ModsimUserDefinedTimeStep b)
        {
            return !(a == b);
        }
        /// <summary>Creates a new instance from a timestep label.</summary>
        /// <param name="label">The timestep label.</param>
        /// <returns>Returns a new instance from a timestep label.</returns>
        public static implicit operator ModsimUserDefinedTimeStep(string label)
        {
            return ModsimUserDefinedTimeStep.FromLabel(label);
        }
        /// <summary>Creates a <c>ModsimUserDefinedTimeStep</c> from a type.</summary>
        /// <param name="type">The type of <c>ModsimUserDefinedTimeStep</c> instance to create.</param>
        /// <returns>Returns a new instance of <c>ModsimUserDefineTimeStep</c> associated with the specified type.</returns>
        public static implicit operator ModsimUserDefinedTimeStep(ModsimUserDefinedTimeStepType type)
        {
            return new ModsimUserDefinedTimeStep(type);
        }

        #endregion

        #region Local methods

        /// <summary>Copies this instance of ModsimUserDefinedTimeStep.</summary>
        public ModsimUserDefinedTimeStep Copy()
        {
            return (ModsimUserDefinedTimeStep)this.MemberwiseClone();
        }
        /// <summary>Gets whether the current instance is a timestep containing sub-daily portions.</summary>
        public bool HasPartialDays
        {
            get
            {
                if (privType < ModsimUserDefinedTimeStepType.days)
                    return true;
                else if (privType > ModsimUserDefinedTimeStepType.weeks)
                    return false;
                else if (privType == ModsimUserDefinedTimeStepType.days)
                    return privSpan - (double)((int)privSpan) > AllowableErrorInDays;
                else if (privType == ModsimUserDefinedTimeStepType.weeks)
                    return privSpan - (double)((int)privSpan) > AllowableErrorInDays / 7.0;
                else
                    throw new Exception("Unrecognized ModsimUserDefinedTimeStepType enum element.");
            }
        }

        #endregion
        #region Local conversion methods

        /// <summary>Converts the current instance to a TimeSpan.</summary>
        /// <param name="date">The date from which the TimeSpan is calculated.</param>
        /// <returns>Returns the TimeSpan associated with this instance.</returns>
        public virtual TimeSpan ToTimeSpan(DateTime date)
        {
            return ConvertToTimeSpan(privSpan, privType, date);
        }
        /// <summary>Increments the date by the current timestep instance.</summary>
        /// <param name="date">The date from which to increment</param>
        /// <returns>Returns the incremented date.</returns>
        public virtual DateTime IncrementDate(DateTime date)
        {
            return date.Add(ToTimeSpan(date));
        }

        #endregion
        #region Shared conversion methods

        /// <summary>Gets the corresponding ModsimUserDefinedTimeStepType to the specified label or name.</summary>
        /// <param name="label">Specifies the label of the ModsimUserDefinedTimeStepType enum.</param>
        /// <returns>Returns the corresponding ModsimUserDefinedTimeStepType to the specified label or name.</returns>
        public static ModsimUserDefinedTimeStepType GetUserDefTSType(string label)
        {
            try
            {
                ModsimUserDefinedTimeStepType retVal = (ModsimUserDefinedTimeStepType)Enum.Parse(typeof(ModsimUserDefinedTimeStepType), label, true);
                if (Enum.IsDefined(typeof(ModsimUserDefinedTimeStepType), retVal))
                    return retVal;
                retVal = (ModsimUserDefinedTimeStepType)Enum.Parse(typeof(ModsimUserDefinedTimeStepType), label + "s", true);
                if (Enum.IsDefined(typeof(ModsimUserDefinedTimeStepType), retVal))
                    return retVal;
            }
            catch {}

            return ModsimUserDefinedTimeStepType.Undefined;
        }
        /// <summary>Retrieves all ModsimUserDefinedTimeStepType names in an array.</summary>
        /// <returns>Returns all ModsimUserDefinedTimeStepType names in an array.</returns>
        public static string[] GetUserDefTSTypeNames(bool removeUndefined)
        {
            List<ModsimUserDefinedTimeStepType> aList = new List<ModsimUserDefinedTimeStepType>();
            foreach (ModsimUserDefinedTimeStepType type in Enum.GetValues(typeof(ModsimUserDefinedTimeStepType)))
            {
                aList.Add(type);
            }
            if (removeUndefined) aList.Remove(ModsimUserDefinedTimeStepType.Undefined);
            return aList.ConvertAll(x => x.ToString()).ToArray();
        }
        /// <summary>Converts any positive TimeSpan to a label according to a pre-defined pattern.</summary>
        /// <param name="span">The TimeSpan to convert to a label.</param>
        /// <returns>Returns a label for the TimeSpan.</returns>
        public static string GetUserDefLabel(double span, ModsimUserDefinedTimeStepType type)
        {
            if (span == 1.0)
                return type.ToString().TrimEnd('s');
            else 
                return span.ToString("R") + " " + type.ToString();
        }
        /// <summary>Converts a user-defined timestep to a System.TimeSpan.</summary>
        /// <param name="span">The span of the </param>
        /// <param name="type"></param>
        /// <param name="date">The date from which the TimeSpan is calculated.</param>
        /// <returns>Returns the TimeSpan associated with this instance.</returns>
        public static TimeSpan ConvertToTimeSpan(double span, ModsimUserDefinedTimeStepType type, DateTime date)
        {
            double totNumDays;
            int intSpan; 
            switch (type)
            {
                case ModsimUserDefinedTimeStepType.Undefined:
                    throw new Exception("Cannot get TimeSpan on undefined ModsimUserDefinedTimeStepType");
                //case ModsimUserDefinedTimeStepType.milliseconds:
                //    return TimeSpan.FromMilliseconds(span);
                case ModsimUserDefinedTimeStepType.seconds:
                    return TimeSpan.FromSeconds(span);
                case ModsimUserDefinedTimeStepType.minutes:
                    return TimeSpan.FromMinutes(span);
                case ModsimUserDefinedTimeStepType.hours:
                    return TimeSpan.FromHours(span);
                case ModsimUserDefinedTimeStepType.days:
                    return TimeSpan.FromDays(span);
                case ModsimUserDefinedTimeStepType.weeks:
                    return TimeSpan.FromDays(span * 7);
                case ModsimUserDefinedTimeStepType.months:
                    totNumDays = 0;
                    intSpan = (int)span;
                    for (int i = 0; i < intSpan; i++)
                    {
                        totNumDays += DateTime.DaysInMonth(date.Year, date.Month);
                        date = date.AddMonths(1);
                    }
                    return TimeSpan.FromDays(totNumDays);
                case ModsimUserDefinedTimeStepType.years:
                    totNumDays = 0;
                    intSpan = (int)span;
                    for (int i = 0; i < intSpan; i++)
                    {
                        totNumDays += DateTime.IsLeapYear(date.Year) ? 366 : 365;
                        date = date.AddYears(1);
                    }
                    return TimeSpan.FromDays(totNumDays);
                default:
                    throw new Exception("A ModsimUserDefinedTimeStepType is undefined.");
            }
        }
        /// <summary>Parses a label (using the pre-defined conventions) into a <c>ModsimUserDefinedTimeStep</c>.</summary>
        /// <param name="label">The label from which to create an instance of <c>ModsimUserDefinedTimeStep</c>.</param>
        /// <returns>Returns the <c>ModsimUserDefinedTimeStep</c> associated with the specified label.</returns>
        public static ModsimUserDefinedTimeStep FromLabel(string label)
        {
            if (label == "" || label == null) return null;
            double span = 1.0;
            if (label.Contains(" "))
            {
                string[] str = label.Split(' ');
                if (!double.TryParse(str[0], out span))
                    span = 1.0;
                label = str[1];
            }
            return new ModsimUserDefinedTimeStep(span, GetUserDefTSType(label));
        }

        #endregion
    }
}
