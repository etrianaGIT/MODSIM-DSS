using System;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{

    /// <summary>Types of time steps available in MODSIM (up through version 8.1).</summary>
    /// <remarks>To update timestep types to the most recent version of MODSIM, use ModsimTimeStep.ConvertPrevTypeToCurrentType.</remarks>
    public enum ModsimTimeStepType_V8_1 : int
    {
        Monthly = 0,
        Weekly = 1,
        Daily = 2,
        TenDays = 4,
        FiveDays = 5
    }
    /// <summary>Types of time steps available in MODSIM.</summary>
    /// <remarks>Sorted by timestep length.</remarks>
    public enum ModsimTimeStepType : int
    {
        Undefined = -1,         //Undefined = -1,
        Seconds = 0,            //Monthly = 0,
        FifteenMin = 1,         //Weekly = 1,
        Hourly = 2,             //Daily = 2,
        Daily = 3,              //TenDays = 4,
        FiveDays = 4,           //FiveDays = 5,
        Weekly = 5,             //Hourly = 6,
        TenDays = 6,            //FifteenMin = 7,
        Monthly = 7,            //TenMin = 8,
        UserDefined = 999       //UserDefined = 999
    }
    /// <summary>Specifies the types of labels to remove when retrieving a list of timestep labels.</summary>
    public enum RemoveTypes : int
    {
        Seconds,
        UserDefined,
        SecondsAndUserDefined,
        None
    }

    /// <summary>Handles MODSIM's timesteps.</summary>
    public class ModsimTimeStep : ModsimUserDefinedTimeStep
    {
        #region Shared instance variables

        private static ModsimTimeStepType privDefaultModsimType = ModsimTimeStepType.Undefined;
        private static TimeSpan privDefaultTimeSpan = TimeSpan.Zero;
        private static int privDefaultTSsForV7Output = 12;

        #endregion
        #region Local instance variables

        private ModsimTimeStepType privModsimType = privDefaultModsimType;
        private TimeSpan privTimeSpan = privDefaultTimeSpan;
        private string privLabel = ModsimUserDefinedTimeStep.DefaultUndefinedLabel;
        private int privTSsForV7Output = privDefaultTSsForV7Output;

        #endregion
        #region Properties

        /// <summary>Gets the type of the current instance.</summary>
        public ModsimTimeStepType TSType { get { return privModsimType; } }
        /// <summary>Gets the label of this instance.</summary>
        public string Label { get { return privLabel; } }

        #endregion
        #region Overridden Properties

        /// <summary>Gets the average timespan within the timestep. This is needed because for monthly timesteps, the TimeSpans differ.</summary>
        /// <remarks>The average TimeSpan is based on the average of the timestep every four years (including leap year).</remarks>
        public override TimeSpan AverageTimeSpan
        {
            get
            {
                if (privModsimType == ModsimTimeStepType.Monthly)
                    return TimeSpan.FromDays(30.4375);
                else if (privModsimType == ModsimTimeStepType.TenDays)
                    return TimeSpan.FromDays(10.14583); // 10.14583 = [3(8 day) + 1(9 day) + 112(10 day) + 28(11 day)] / [3 + 1 + 112 + 28] every four years...
                else if (privModsimType == ModsimTimeStepType.FiveDays)
                    return TimeSpan.FromDays(5.072917); // 5.072917 = [3(3 day) + 1(4 day) + 256(5 day) + 28(6 day)] / [3 + 1 + 256 + 28] every four years...
                else
                    return privTimeSpan;
            }
        }

        #endregion

        #region Constructors

        // Regular constructors
        /// <summary>Builds a new instance of ModsimTimeStep with a specified TimeStepType.</summary>
        /// <param name="TimeStep">The timestep defining the current instance.</param>
        public ModsimTimeStep(ModsimTimeStepType type)
            : base(GetUDSpan(type), GetUDType(type))
        {
            this.privModsimType = type;
            this.privTimeSpan = ToTimeSpan(new DateTime(2012, 1, 1));
            this.privLabel = GetLabel(type);
            this.privTSsForV7Output = GetTSsForV7Output(type);
        }
        /// <summary>Infers a new instance of ModsimTimeStep from a specified TimeSpan.</summary>
        /// <param name="UDType">Specifies the ModsimUserDefinedTimeStepType to use when creating an instance.</param>
        /// <remarks>The ModsimTimeStepType associated with this span is inferred with an accuracy of 1 millisecond.</remarks>
        public ModsimTimeStep(ModsimUserDefinedTimeStepType UDType)
            : this(1.0, UDType)
        {
        }
        /// <summary>Infers a new instance of ModsimTimeStep from a specified TimeSpan.</summary>
        /// <param name="span">Specifies a TimeSpan for the new instance of ModsimTimeStep.</param>
        /// <param name="UDType">Specifies the ModsimUserDefinedTimeStepType to use when creating an instance.</param>
        /// <remarks>The ModsimTimeStepType associated with this span is inferred with an accuracy of 1 millisecond.</remarks>
        public ModsimTimeStep(double span, ModsimUserDefinedTimeStepType UDtype)
            : base(span, UDtype)
        {
            this.privModsimType = ModsimTimeStepType.UserDefined;
            this.privTimeSpan = ToTimeSpan(new DateTime(2012, 1, 1));
            this.privLabel = GetUserDefLabel(span, UDtype);
            this.privTSsForV7Output = GetTSsForV7Output(this.privModsimType);
        }

        // Copy
        /// <summary>Copies this instance of <c>ModsimTimeStep</c>.</summary>
        /// <returns>Returns the copied instance.</returns>
        public new ModsimTimeStep Copy()
        {
            return (ModsimTimeStep)this.MemberwiseClone();
        }

        #endregion
        #region Operators

        /// <summary>Creates a new instance from a timestep label.</summary>
        /// <param name="label">The timestep label.</param>
        /// <returns>Returns a new instance from a timestep label.</returns>
        public static implicit operator ModsimTimeStep(string label)
        {
            return ModsimTimeStep.FromLabel(label);
        }
        /// <summary>Creates a new instance from a specified type.</summary>
        /// <param name="type">The type to create.</param>
        /// <returns>Returns a new instance associated with the specified type.</returns>
        public static implicit operator ModsimTimeStep(ModsimTimeStepType type)
        {
            return new ModsimTimeStep(type);
        }
        /// <summary>Creates a new instance from a user-defined timestep.</summary>
        /// <param name="UserDefTimeStep">The user-defined timestep.</param>
        public static ModsimTimeStep FromUserDefTimeStep(ModsimUserDefinedTimeStep UserDefTimeStep)
        {
            return new ModsimTimeStep(UserDefTimeStep.UserDefSpan, UserDefTimeStep.UserDefTSType); 
        }

        #endregion

        #region Overridden conversion methods

        /// <summary>Converts the current instance to a TimeSpan.</summary>
        /// <param name="date">The date from which the TimeSpan is calculated.</param>
        /// <returns>Returns the TimeSpan associated with this instance.</returns>
        public override TimeSpan ToTimeSpan(DateTime date)
        {
            if (privModsimType == ModsimTimeStepType.UserDefined)
                return base.ToTimeSpan(date);
            else
                return ConvertToTimeSpan(privModsimType, date);
        }
        /// <summary>Increments the date according to the timestep.</summary>
        /// <param name="date">The date to increment.</param>
        /// <returns>Returns the incremented DateTime value.</returns>
        public override DateTime IncrementDate(DateTime date)
        {
            return date.Add(ToTimeSpan(date));
        }

        #endregion
        #region Shared Conversion Methods

        // Individual values
        /// <summary>Gets a double referring to the TimeSpan associated with the specified ModsimTimeStepType.</summary>
        /// <param name="type">Specifies the ModsimTimeStepType from which to get the TimeSpan.</param>
        /// <returns>Returns a double referring to the TimeSpan associated with the specified ModsimTimeStepType.</returns>
        private static double GetUDSpan(ModsimTimeStepType type)
        {
            switch (type)
            {
                case ModsimTimeStepType.Seconds:
                    return 1;
                case ModsimTimeStepType.FifteenMin:
                    return 15;
                case ModsimTimeStepType.Hourly:
                    return 1;
                case ModsimTimeStepType.Daily:
                    return 1;
                case ModsimTimeStepType.FiveDays:
                    return 5;
                case ModsimTimeStepType.Weekly:
                    return 7;
                case ModsimTimeStepType.TenDays:
                    return 10;
                case ModsimTimeStepType.Monthly:
                    return 1;
                case ModsimTimeStepType.UserDefined:
                    throw new Exception("Cannot infer a user-defined TimeSpan from a ModsimTimeStepType.");
                case ModsimTimeStepType.Undefined:
                    throw new Exception("Cannot retrieve a span for an undefined type.");
                default:
                    throw new Exception("Unrecognized ModsimTimeStepType: " + type.ToString());
            }
        }
        /// <summary>Gets a double referring to the TimeSpan associated with the specified ModsimTimeStepType.</summary>
        /// <param name="type">Specifies the ModsimTimeStepType from which to get the TimeSpan.</param>
        /// <returns>Returns a double referring to the TimeSpan associated with the specified ModsimTimeStepType.</returns>
        private static ModsimUserDefinedTimeStepType GetUDType(ModsimTimeStepType type)
        {
            switch (type)
            {
                case ModsimTimeStepType.Undefined:
                    throw new Exception("Cannot build a ModsimUserDefinedTimeStep with an undefined Type.");
                case ModsimTimeStepType.Seconds:
                    return ModsimUserDefinedTimeStepType.seconds;
                case ModsimTimeStepType.FifteenMin:
                    return ModsimUserDefinedTimeStepType.minutes;
                case ModsimTimeStepType.Hourly:
                    return ModsimUserDefinedTimeStepType.hours;
                case ModsimTimeStepType.Daily:
                    return ModsimUserDefinedTimeStepType.days;
                case ModsimTimeStepType.FiveDays:
                    return ModsimUserDefinedTimeStepType.days;
                case ModsimTimeStepType.Weekly:
                    return ModsimUserDefinedTimeStepType.days;
                case ModsimTimeStepType.TenDays:
                    return ModsimUserDefinedTimeStepType.days;
                case ModsimTimeStepType.Monthly:
                    return ModsimUserDefinedTimeStepType.months;
                case ModsimTimeStepType.UserDefined:
                    throw new Exception("Cannot infer a ModsimUserDefinedTimeStep without a specified TimeSpan.");
                default:
                    throw new Exception("Unrecognized ModsimTimeStepType.");
            }
        }
        /// <summary>Converts the specified type to a pre-defined or user-defined name or label of the ModsimTimeStepType.</summary>
        /// <param name="type">The ModsimTimeStepType desired to convert to string. </param>
        /// <returns>Returns the name or label of the ModsimTimeStepType.</returns>
        /// <remarks>If the user-defined type is specified, the default ("user-defined") will be returned.</remarks>
        public static string GetLabel(ModsimTimeStepType type)
        {
            if (type == ModsimTimeStepType.UserDefined)
                return ModsimUserDefinedTimeStep.DefaultUserDefLabel;
            else
                return GetLabel(type, 1, ModsimUserDefinedTimeStepType.Undefined);
        }
        /// <summary>Creates a label from the time span and user-defined timestep type.</summary>
        /// <param name="span">Specifies the time span.</param>
        /// <param name="userDefType">Specifies the user-defined type associated with span.</param>
        /// <returns>Returns the name or label of the ModsimTimeStepType.</returns>
        public static string GetLabel(double span, ModsimUserDefinedTimeStepType userDefType)
        {
            return GetUserDefLabel(span, userDefType);
        }
        /// <summary>Converts the specified type to a pre-defined or user-defined name or label of the ModsimTimeStepType.</summary>
        /// <param name="type">The ModsimTimeStepType desired to convert to string. </param>
        /// <param name="span">Specifies the TimeSpan for UserDefined types.</param>
        /// <param name="userDefType">Specifies the user-defined type associated with span.</param>
        /// <returns>Returns the name or label of the ModsimTimeStepType.</returns>
        private static string GetLabel(ModsimTimeStepType type, double span, ModsimUserDefinedTimeStepType userDefType)
        {
            switch (type)
            {
                case ModsimTimeStepType.Seconds:
                    return "second";
                case ModsimTimeStepType.FifteenMin:
                    return "15 minutes";
                case ModsimTimeStepType.Hourly:
                    return "hour";
                case ModsimTimeStepType.Daily:
                    return "day";
                case ModsimTimeStepType.FiveDays:
                    return "5 days";
                case ModsimTimeStepType.Weekly:
                    return "week";
                case ModsimTimeStepType.TenDays:
                    return "10 days";
                case ModsimTimeStepType.Monthly:
                    return "month";
                case ModsimTimeStepType.UserDefined:
                    return GetUserDefLabel(span, userDefType);
                default:
                    throw new Exception("Cannot retrieve a label for ModsimTimeStepType." + type.ToString() + ".");
            }
        }
        /// <summary>Converts a label to a ModsimTimeStepType.</summary>
        /// <param name="label">The label to convert.</param>
        /// <returns>Returns the ModsimTimeStepType with the same label.</returns>
        public static ModsimTimeStepType GetTSType(string label)
        {
            try
            {
                ModsimTimeStepType retVal = (ModsimTimeStepType)Enum.Parse(typeof(ModsimTimeStepType), label, true);
                if (Enum.IsDefined(typeof(ModsimTimeStepType), retVal))
                    return retVal;
            }
            catch {}

            label = label.ToLower();
            string[] labels = Array.ConvertAll(GetLabels(true, RemoveTypes.UserDefined), element => element.ToLower());
            string[] types = Array.ConvertAll(GetTSTypeNames(true, RemoveTypes.UserDefined), element => element.ToLower());
            for (int i = 0; i < types.Length; i++)
                if (label.Equals(types[i]) || label.Equals(labels[i]))
                    return (ModsimTimeStepType)i;

            // Nonstandard labels...
            switch (label)
            {
                case "s":
                case "sec":
                    return ModsimTimeStepType.Seconds;
                case "hr":
                    return ModsimTimeStepType.Hourly;
                case "d":
                case "dy":
                    return ModsimTimeStepType.Daily;
                case "wk":
                    return ModsimTimeStepType.Weekly;
                case "mon":
                    return ModsimTimeStepType.Monthly;
            }

            return ModsimTimeStepType.UserDefined;
        }
        /// <summary>Converts old timestep types to the new timestep type.</summary>
        /// <param name="oldType">The old timestep type to convert.</param>
        /// <returns>Returns the new timestep type.</returns>
        public static ModsimTimeStepType GetTSType(ModsimTimeStepType_V8_1 oldType)
        {
            switch (oldType)
            {
                case ModsimTimeStepType_V8_1.Daily:
                    return ModsimTimeStepType.Daily;
                case ModsimTimeStepType_V8_1.FiveDays:
                    return ModsimTimeStepType.FiveDays;
                case ModsimTimeStepType_V8_1.TenDays:
                    return ModsimTimeStepType.TenDays;
                case ModsimTimeStepType_V8_1.Weekly:
                    return ModsimTimeStepType.Weekly;
                case ModsimTimeStepType_V8_1.Monthly:
                    return ModsimTimeStepType.Monthly;
                default:
                    throw new Exception(oldType.ToString() + " is an unrecognized type within the code.");
            }
        }
        /// <summary>Parses a ModsimTimeStepType into an older version (before 8.2.0).</summary>
        /// <param name="type">The ModsimTimeStepType to convert to an older version.</param>
        /// <returns>Returns the old type (the ModsimTimeStepType_V8_1 enum).</returns>
        public static ModsimTimeStepType_V8_1 GetOldType(ModsimTimeStepType type)
        {
            switch (type)
            {
                case ModsimTimeStepType.Daily:
                    return ModsimTimeStepType_V8_1.Daily;
                case ModsimTimeStepType.FiveDays:
                    return ModsimTimeStepType_V8_1.FiveDays;
                case ModsimTimeStepType.Weekly:
                    return ModsimTimeStepType_V8_1.Weekly;
                case ModsimTimeStepType.TenDays:
                    return ModsimTimeStepType_V8_1.TenDays;
                case ModsimTimeStepType.Monthly:
                    return ModsimTimeStepType_V8_1.Monthly;
                default:
                    throw new Exception("Could not parse type " + type.ToString() + " into an older timestep type.");
            }
        }
        /// <summary>Converts new timestep types to the old timestep type.</summary>
        /// <param name="newType">The new timestep type to convert.</param>
        /// <param name="oldType">The output older version timestep.</param>
        /// <returns>Returns true if able to convert. Otherwise, returns false (when the new timestep type is not one of the old timestep types).</returns>
        public static bool TryGetOldType(ModsimTimeStepType newType, out ModsimTimeStepType_V8_1 oldType)
        {
            oldType = ModsimTimeStepType_V8_1.Monthly;
            try
            {
                oldType = GetOldType(newType);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>Converts a ModsimTimeStepType to a TimeSpan.</summary>
        /// <param name="type">The ModsimTimeStepType to convert to a TimeSpan.</param>
        /// <returns>Returns the TimeSpan associated with the specified ModsimTimeStepType.</returns>
        /// <remarks>If the ModsimTimeStepType is specified as 'UserDefined' or 'Monthly' (needs a date to be able to convert), an exception will be thrown.</remarks>
        private static TimeSpan ConvertToTimeSpan(ModsimTimeStepType type)
        {
            if (type == ModsimTimeStepType.Monthly || type == ModsimTimeStepType.TenDays || type == ModsimTimeStepType.FiveDays)
                throw new Exception("Must specify a date to convert Monthly, TenDays, or FiveDays to TimeSpan");
            else
                return ConvertToTimeSpan(type, new DateTime(0));
        }
        /// <summary>Converts a ModsimTimeStepType to a TimeSpan.</summary>
        /// <param name="type">The ModsimTimeStepType to convert to a TimeSpan.</param>
        /// <param name="date">The date used when calculating the TimeSpan for a ModsimTimeStepType of 'Monthly'.</param>
        /// <returns>Returns the TimeSpan associated with the specified ModsimTimeStepType.</returns>
        /// <remarks>If the ModsimTimeStepType is specified as 'UserDefined', an exception will be thrown.</remarks>
        private static TimeSpan ConvertToTimeSpan(ModsimTimeStepType type, DateTime date)
        {
            switch (type)
            {
                case ModsimTimeStepType.Undefined:
                    return privDefaultTimeSpan;
                case ModsimTimeStepType.UserDefined:
                    throw new Exception("Cannot convert ModsimTimeStepType.UserDefined to TimeSpan.");
                case ModsimTimeStepType.Monthly:
                    return TimeSpan.FromDays(DateTime.DaysInMonth(date.Year, date.Month));
                case ModsimTimeStepType.Weekly:
                    return TimeSpan.FromDays(7);
                case ModsimTimeStepType.Daily:
                    return TimeSpan.FromDays(1);
                case ModsimTimeStepType.TenDays:
                    if (date.Day < 21)
                        return TimeSpan.FromDays(10);
                    else
                        return TimeSpan.FromDays(DateTime.DaysInMonth(date.Year, date.Month) - date.Day + 1);
                case ModsimTimeStepType.FiveDays:
                    if (date.Day < 26)
                        return TimeSpan.FromDays(5);
                    else
                        return TimeSpan.FromDays(DateTime.DaysInMonth(date.Year, date.Month) - date.Day + 1);
                case ModsimTimeStepType.Hourly:
                    return TimeSpan.FromHours(1);
                case ModsimTimeStepType.FifteenMin:
                    return TimeSpan.FromMinutes(15);
                case ModsimTimeStepType.Seconds:
                    return TimeSpan.FromSeconds(1);
                default:
                    throw new Exception("A particular ModsimTimeStepType (" + type.ToString() + ") is undefined in code.");
            }
        }
        /// <summary>Creates an instance from a label... Can be user-defined label using the pre-defined naming convention.</summary>
        /// <param name="label">The label from which to create a new instance.</param>
        /// <returns>Returns the <c>ModsimTimeStep</c> associated with the label.</returns>
        public new static ModsimTimeStep FromLabel(string label)
        {
            if (label == "" || label == null) return null;
            ModsimTimeStepType TSType = GetTSType(label);
            if (TSType == ModsimTimeStepType.UserDefined)
            {
                ModsimUserDefinedTimeStep UserDefTS = ModsimUserDefinedTimeStep.FromLabel(label);
                return new ModsimTimeStep(UserDefTS.UserDefSpan, UserDefTS.UserDefTSType);
            }
            else
                return new ModsimTimeStep(TSType);
        }

        // Arrays
        /// <summary>Retrieves all ModsimTimeStepTypes in an array.</summary>
        /// <returns>Returns all ModsimTimeStepTypes in an array.</returns>
        public static ModsimTimeStepType[] GetTSTypes(bool removeUndefined)
        {
            List<ModsimTimeStepType> aList = new List<ModsimTimeStepType>();
            foreach (ModsimTimeStepType type in Enum.GetValues(typeof(ModsimTimeStepType)))
            {
                aList.Add(type);
            }
            if (removeUndefined) aList.Remove(ModsimTimeStepType.Undefined);
            return aList.ToArray();
        }
        /// <summary>Retrieves all the type names from the <c>ModsimTimeStepType</c> enum.</summary>
        /// <returns>Returns all the type names from the <c>ModsimTimeStepType</c> enum.</returns>
        public static string[] GetTSTypeNames()
        {
            return GetTSTypeNames(true);
        }
        /// <summary>Retrieves all the type names from the <c>ModsimTimeStepType</c> enum.</summary>
        /// <param name="useMetric">If true, Metric units type names are returned. Otherwise, English units type names are returned.</param>
        /// <returns>Returns all the type names from the <c>ModsimTimeStepType</c> enum.</returns>
        public static string[] GetTSTypeNames(bool useMetric)
        {
            return GetTSTypeNames(useMetric, RemoveTypes.None);
        }
        /// <summary>Retrieves all the type names from the <c>ModsimTimeStepType</c> enum.</summary>
        /// <param name="useMetric">If true, Metric units type names are returned. Otherwise, English units type names are returned.</param>
        /// <param name="typesToRemove">The timestep types to remove from the array.</param>
        /// <returns>Returns all the type names from the <c>ModsimTimeStepType</c> enum.</returns>
        public static string[] GetTSTypeNames(bool useMetric, RemoveTypes typesToRemove)
        {
            List<string> aList = new List<string>(Enum.GetNames(typeof(ModsimTimeStepType)));
            aList.Remove(ModsimTimeStepType.Undefined.ToString());
            if (!useMetric)
            {
                aList.Remove(ModsimTimeStepType.FiveDays.ToString());
                //aList.Remove(ModsimTimeStepType.TenDays.ToString());
            }
            if (typesToRemove == RemoveTypes.Seconds || typesToRemove == RemoveTypes.SecondsAndUserDefined)
                aList.Remove(ModsimTimeStepType.Seconds.ToString());
            if (typesToRemove == RemoveTypes.UserDefined || typesToRemove == RemoveTypes.SecondsAndUserDefined)
                aList.Remove(ModsimTimeStepType.UserDefined.ToString());
            return aList.ToArray();
        }
        /// <summary>Retrieves all labels for ModsimTimeStepType in an array (except Undefined).</summary>
        /// <returns>Returns all labels for ModsimTimeStepType in an array (except Undefined).</returns>
        public static string[] GetLabels()
        {
            ModsimTimeStepType[] types = GetTSTypes(true);
            return Array.ConvertAll(types, element => GetLabel(element));
        }
        /// <summary>Retrieves labels for a specified system of units.</summary>
        /// <param name="useMetric">If true, Metric units labels are returned. Otherwise, English units labels are returned.</param>
        /// <returns>Returns labels for a specified system of units.</returns>
        public static string[] GetLabels(bool useMetric)
        {
            return GetLabels(useMetric, RemoveTypes.None);
        }
        /// <summary>Retrieves labels for a specified system of units.</summary>
        /// <param name="useMetric">If true, Metric units labels are returned. Otherwise, English units labels are returned.</param>
        /// <param name="typesToRemove">The timestep types to remove from the array.</param>
        /// <returns>Returns labels for a specified system of units.</returns>
        public static string[] GetLabels(bool useMetric, RemoveTypes typesToRemove)
        {
            List<string> aList = new List<string>(GetLabels());
            if (!useMetric)
            {
                aList.Remove(GetLabel(ModsimTimeStepType.FiveDays));
                //aList.Remove(GetLabel(ModsimTimeStepType.TenDays));
            }
            if (typesToRemove == RemoveTypes.Seconds || typesToRemove == RemoveTypes.SecondsAndUserDefined)
                aList.Remove(GetLabel(ModsimTimeStepType.Seconds));
            if (typesToRemove == RemoveTypes.UserDefined || typesToRemove == RemoveTypes.SecondsAndUserDefined)
                aList.Remove(GetLabel(ModsimTimeStepType.UserDefined));
            return aList.ToArray();
        }

        #endregion

        #region Legacy version compatability

        /// <summary>Number of time steps per Version 7 output flush.</summary>
        /// <returns>Returns the number of timesteps flushed to output for Version 7.</returns>	  
        /// <remarks> In the model monthly time step case it will  return 12 (minor) per year(mayor time step)</remarks>
        public int NumOfTSsForV7Output
        {
            get
            {
                return privTSsForV7Output;
            }
        }
        public static int GetTSsForV7Output(ModsimTimeStepType type)
        {
            switch (type)
            {
                case ModsimTimeStepType.Monthly:
                    return 12;
                case ModsimTimeStepType.Weekly:
                    return 12; // For Version 7 output, the max array length is 12 
                case ModsimTimeStepType.Daily:
                    return 7;
                case ModsimTimeStepType.TenDays:
                    return 3;
                case ModsimTimeStepType.FiveDays:
                    return 6;
                case ModsimTimeStepType.Hourly:
                    return 12; // For Version 7 output, the max array length is 12 
                case ModsimTimeStepType.FifteenMin:
                    return 4;
                case ModsimTimeStepType.Seconds:
                    return 10;
                case ModsimTimeStepType.UserDefined:
                    return 10; // This is just a default...
                default:
                    throw new Exception("Version 7 output is undefined in the code for the selected ModsimTimeStepType: " + type.ToString());
            }
        }

        #endregion
    }
}
