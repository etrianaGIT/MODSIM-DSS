using System;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Systems of units.</summary>
    public enum UnitsSystemType : int { All = -1, English = 0, Metric = 1 }
    /// <summary>Volumetric units in MODSIM.</summary>
    public enum VolumeUnitsType : int { Undefined = -1, AF = 0, kAF = 1, cf = 2, MG = 3, kCM = 4, MCM = 5, cm = 6 }
    /// <summary>Units of area in MODSIM.</summary>
    public enum AreaUnitsType : int { Undefined = -1, Acres = 0, kAcres = 1, SqFeet = 2, kSqMeters = 3, MSqMeters = 4, SqMeters = 5 }
    /// <summary>Units of length in MODSIM.</summary>
    public enum LengthUnitsType : int { Undefined = -1, feet = 0, inches = 1, meters = 2, centimeters = 3, millimeters = 4 }
    /// <summary>Units of energy in MODSIM.</summary>
    public enum EnergyUnitsType : int { Undefined = -1, MWh = 0, kWh = 1, GWh = 2, BTU = 3, kJ = 4, MJ = 5, GJ = 6 }
    /// <summary>Units specific to MODSIM</summary>
    public enum ModsimUnitsType : int { Undefined = -1, Volume, VolumeRate, Area, AreaRate, Length, LengthRate, Time, TimeRate, Energy, EnergyRate }

    /// <summary>The class that helps control unit conversions within MODSIM.</summary>
    public class ModsimUnits
    {
        #region Shared default labels

        private readonly static string[] volumeLabels = new string[] { "acre-ft", "10³ acre-ft", "ft³", "10⁶gal", "1000 m³", "10⁶m³", "m³" };
        private readonly static string[] areaLabels = new string[] { "acres", "10³ acres", "ft²", "1000 m²", "10⁶m²", "m²" };
        private readonly static string[] lengthLabels = new string[] { "ft", "in", "m", "cm", "mm" };
        private readonly static string[] timeLabels = ModsimUserDefinedTimeStep.GetUserDefTSTypeNames(true);
        private readonly static string[] energyLabels = new string[] { "MWh", "kWh", "GWh", "BTU", "kJ", "MJ", "GJ" };
        private readonly static string[] volumeLabels_English = new string[] { "acre-ft", "10³ acre-ft", "ft³", "10⁶gal" };
        private readonly static string[] areaLabels_English = new string[] { "acres", "10³ acres", "ft²" };
        private readonly static string[] lengthLabels_English = new string[] { "ft", "in" };
        private readonly static string[] volumeLabels_Metric = new string[] { "1000 m³", "10⁶m³", "m³" };
        private readonly static string[] areaLabels_Metric = new string[] { "1000 m²", "10⁶m²", "m²" };
        private readonly static string[] lengthLabels_Metric = new string[] { "m", "cm", "mm" };

        #endregion
        #region Shared default ModsimUnits

        private readonly static ModsimTimeStep defaultTimeStep = new ModsimTimeStep(ModsimTimeStepType.Monthly);
        private readonly static ModsimUnits defaultVolumeUnits_English = new ModsimUnits(VolumeUnitsType.AF);
        private readonly static ModsimUnits defaultVolumeRateUnits_English = new ModsimUnits(VolumeUnitsType.AF, defaultTimeStep);
        private readonly static ModsimUnits defaultAreaUnits_English = new ModsimUnits(AreaUnitsType.Acres);
        private readonly static ModsimUnits defaultAreaRateUnits_English = new ModsimUnits(AreaUnitsType.SqFeet, ModsimTimeStepType.Daily);
        private readonly static ModsimUnits defaultLengthUnits_English = new ModsimUnits(LengthUnitsType.feet);
        private readonly static ModsimUnits defaultLengthRateUnits_English = new ModsimUnits(LengthUnitsType.feet, ModsimTimeStepType.Daily);
        private readonly static ModsimUnits defaultVolumeUnits_Metric = new ModsimUnits(VolumeUnitsType.kCM);
        private readonly static ModsimUnits defaultVolumeRateUnits_Metric = new ModsimUnits(VolumeUnitsType.kCM, defaultTimeStep);
        private readonly static ModsimUnits defaultAreaUnits_Metric = new ModsimUnits(AreaUnitsType.kSqMeters);
        private readonly static ModsimUnits defaultAreaRateUnits_Metric = new ModsimUnits(AreaUnitsType.SqMeters, ModsimTimeStepType.Daily);
        private readonly static ModsimUnits defaultLengthUnits_Metric = new ModsimUnits(LengthUnitsType.meters);
        private readonly static ModsimUnits defaultLengthRateUnits_Metric = new ModsimUnits(LengthUnitsType.meters, ModsimTimeStepType.Daily);
        private readonly static ModsimUnits defaultTimeUnits = new ModsimUnits(ModsimUserDefinedTimeStepType.hours);
        private readonly static ModsimUnits defaultTimeRateUnits = new ModsimUnits(ModsimUserDefinedTimeStepType.hours, ModsimTimeStepType.Daily);
        private readonly static ModsimUnits defaultEnergyUnits = new ModsimUnits(EnergyUnitsType.MWh);
        private readonly static ModsimUnits defaultEnergyRateUnits = new ModsimUnits(EnergyUnitsType.kJ, ModsimTimeStepType.Seconds);

        #endregion
        #region Shared Options

        private static bool useSpecialLabel = true;
        /// <summary>Gets and sets whether or not to use special labels when establishing labels for ModsimUnits instances.</summary>
        public static bool UseSpecialLabel
        {
            get
            {
                return useSpecialLabel;
            }
            set
            {
                useSpecialLabel = value;
            }
        }

        #endregion

        #region Local instance variables

        private ModsimUnitsType unitsType = ModsimUnitsType.Undefined;
        private object majorUnits = ModsimUnitsType.Undefined;
        private string label = ModsimUnitsType.Undefined.ToString();
        private string majorUnitsLabel = ModsimUnitsType.Undefined.ToString();
        private ModsimTimeStep timeStep = null;
        private bool isRate = false;
        private bool isMetric = false;

        #endregion
        #region Properties

        /// <summary>Type of majorUnits (flow rate, evaporation rate, volume, or area) of this instance.</summary>
        public ModsimUnitsType Type
        {
            get { return unitsType; }
        }
        /// <summary>The enumerator value associated with the particular type of majorUnits majorUnits for this instance (a value within the enumerators <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</summary>
        public object MajorUnits
        {
            get { return this.majorUnits; }
        }
        /// <summary>The timestep associated with this instance.</summary>
        public ModsimTimeStep TimeStep
        {
            get { return timeStep; }
        }
        /// <summary>Gets whether the current instance specifies majorUnits of rate or not.</summary>
        /// <value>True, if the instance is in majorUnits of rate; false, if not.</value>
        public bool IsRate
        {
            get { return isRate; }
        }
        /// <summary>Gets the units label for this instance.</summary>
        /// <remarks>If the static property UseSpecialLabel is set to true (which is default), it will return the special label (cfs, cms, MGD, kW, etc.).</remarks>
        public string Label
        {
            get { return this.label; }
        }
        /// <summary>Gets the majorUnits label for this instance.</summary>
        public string MajorUnitsLabel
        {
            get { return this.majorUnitsLabel; }
        }
        /// <summary>Gets whether this instance is in the metric system of units.</summary>
        public bool IsMetric
        {
            get { return this.isMetric; }
        }

        #endregion

        #region Constructors

        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        public ModsimUnits(VolumeUnitsType type)
            : this((object)type, null)
        {
        }
        public ModsimUnits(VolumeUnitsType type, ModsimTimeStep timeStep)
            : this((object)type, timeStep)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        public ModsimUnits(AreaUnitsType type)
            : this((object)type, null)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <param name="timeStep">Specifies the timestep associated with the majorUnits... This does not necessarily automatically create rate type majorUnits.</param>
        public ModsimUnits(AreaUnitsType type, ModsimTimeStep timeStep)
            : this((object)type, timeStep)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        public ModsimUnits(LengthUnitsType type)
            : this((object)type, null)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <param name="timeStep">Specifies the timestep associated with the majorUnits... This does not necessarily automatically create rate type majorUnits.</param>
        public ModsimUnits(LengthUnitsType type, ModsimTimeStep timeStep)
            : this((object)type, timeStep)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        public ModsimUnits(EnergyUnitsType type)
            : this((object)type, null)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <param name="timeStep">Specifies the timestep associated with the majorUnits... This does not necessarily automatically create rate type majorUnits.</param>
        public ModsimUnits(EnergyUnitsType type, ModsimTimeStep timeStep)
            : this((object)type, timeStep)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        public ModsimUnits(ModsimUserDefinedTimeStepType type)
            : this((object)type, null)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <param name="timeStep">Specifies the timestep associated with the majorUnits... This does not necessarily automatically create rate type majorUnits.</param>
        public ModsimUnits(ModsimUserDefinedTimeStepType type, ModsimTimeStep timeStep)
            : this((object)type, timeStep)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        public ModsimUnits(ModsimTimeStepType type)
            : this((object)type, null)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="type">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <param name="timeStep">Specifies the timestep associated with the majorUnits... This does not necessarily automatically create rate type majorUnits.</param>
        public ModsimUnits(ModsimTimeStepType type, ModsimTimeStep timeStep)
            : this((object)type, timeStep)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="enumValue">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        public ModsimUnits(object type)
            : this(type, null)
        {
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and timestep.</summary>
        /// <param name="enumValue">Specifies the value of the type of unit to specify (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <param name="timeStep">Specifies the timestep associated with the majorUnits... This does not necessarily automatically create rate type majorUnits.</param>
        public ModsimUnits(object type, ModsimTimeStep timeStep)
        {
            this.majorUnits = type;
            this.isRate = (timeStep != null);
            this.unitsType = GetType(type, this.isRate);
            this.timeStep = timeStep;
            this.label = GetLabel(type, timeStep);
            this.majorUnitsLabel = GetLabel(this.majorUnits);
            this.isMetric = IsMetricUnit(type);
        }
        /// <summary>Constructs an instance of <c>ModsimUnits</c> from a specified type and label.</summary>
        /// <param name="label">Specifies the label (using the pre-defined naming convention) from which to create the <c>ModsimUnits</c> class.</param>
        public ModsimUnits(string label)
        {
            if (!TryParse(label, out this.majorUnits, out this.timeStep, out this.unitsType)) throw new Exception("The specified label (" + label + ") could not be parsed into ModsimUnits.");
            this.isRate = IsRateUnit(this.unitsType);
            this.label = GetLabel(this.majorUnits, this.timeStep);
            this.isMetric = IsMetricUnit(this.majorUnits);
            this.majorUnitsLabel = GetLabel(this.majorUnits);
        }

        // Copy
        /// <summary>Copies this instance of <c>ModsimUnits</c>.</summary>
        /// <returns>Returns the copied instance.</returns>
        public ModsimUnits Copy()
        {
            ModsimUnits retVal = (ModsimUnits)this.MemberwiseClone();
            if (this.timeStep != null) retVal.timeStep = this.timeStep.Copy();
            return retVal;
        }

        #endregion

        #region Shared methods to handle labels

        /// <summary>Retrieves the regular label if a special label is provided (e.g., cfs or kW, which would normally be cf/s or kJ/s). If the label does not represent a special label, <c>specialLabel</c> is returned.</summary>
        /// <param name="specialLabel">The label of the units... They can be special or not.</param>
        /// <returns>Returns the regular label if a special label is provided (e.g., cfs or kW, which would normally be cf/s or kJ/s). If the label does not represent a special label, <c>specialLabel</c> is returned.</returns>
        private static string GetRegularLabel(string specialLabel)
        {
            string label = specialLabel.ToLower();
            if (label.Equals("cfs") || label.Equals("cms") || label.Equals("mgd"))
                specialLabel = specialLabel.Substring(0, 2) + "/" + specialLabel.Substring(2, 1);
            else if (label.Equals("kw") || label.Equals("mw") || label.Equals("gw"))
                specialLabel = specialLabel.Replace("w", "j/s").Replace("W", "J/s");
            return specialLabel;
        }
        /// <summary>Gets the <c>ModsimUnitsType</c> from an enumerator type associated with MODSIM majorUnits.</summary>
        /// <param name="enumType">The majorUnits type: either <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>.</param>
        /// <returns>Returns the <c>ModsimUnitsType</c> from the specified enumerator type.</returns>
        public static ModsimUnitsType GetType(object type, bool isRate)
        {
            if (IsVolumeUnit(type))
                return isRate ? ModsimUnitsType.VolumeRate : ModsimUnitsType.Volume;
            else if (IsAreaUnit(type))
                return isRate ? ModsimUnitsType.AreaRate : ModsimUnitsType.Area;
            else if (IsLengthUnit(type))
                return isRate ? ModsimUnitsType.LengthRate : ModsimUnitsType.Length;
            else if (IsTimeUnit(type))
                return isRate ? ModsimUnitsType.TimeRate : ModsimUnitsType.Time;
            else if (IsEnergyUnit(type))
                return isRate ? ModsimUnitsType.EnergyRate : ModsimUnitsType.Energy;
            else
                throw new Exception("Unrecognized majorUnits type: " + type.ToString());
        }
        /// <summary>Gets an array of all values within <c>ModsimUnitType</c>.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the <c>ModsimUnitType.Undefined</c> value.</param>
        /// <returns>Return an array of all values within <c>ModsimUnitType</c>.</returns>
        public static ModsimUnitsType[] GetTypes(bool removeUndefined)
        {
            List<ModsimUnitsType> aList = new List<ModsimUnitsType>();
            foreach (ModsimUnitsType type in Enum.GetValues(typeof(ModsimUnitsType)))
            {
                aList.Add(type);
            }
            if (removeUndefined) aList.Remove(ModsimUnitsType.Undefined);
            return aList.ToArray();
        }
        /// <summary>Gets the array of labels associated with a specified <c>ModsimUnitsType</c>.</summary>
        /// <param name="unitsType">Specifies the <c>ModsimUnitsType</c>.</param>
        /// <returns>Returns the labels associated with the specified <c>ModsimUnitsType</c>.</returns>
        public static string[] GetLabels(ModsimUnitsType unitsType, UnitsSystemType systemType)
        {
            switch (unitsType)
            {
                case ModsimUnitsType.Volume:
                case ModsimUnitsType.VolumeRate:
                    switch (systemType) { case UnitsSystemType.English: return volumeLabels_English; case UnitsSystemType.Metric: return volumeLabels_Metric; case UnitsSystemType.All: default: return volumeLabels; }
                case ModsimUnitsType.Area:
                case ModsimUnitsType.AreaRate:
                    switch (systemType) { case UnitsSystemType.English: return areaLabels_English; case UnitsSystemType.Metric: return areaLabels_Metric; case UnitsSystemType.All: default: return areaLabels; }
                case ModsimUnitsType.Length:
                case ModsimUnitsType.LengthRate:
                    switch (systemType) { case UnitsSystemType.English: return lengthLabels_English; case UnitsSystemType.Metric: return lengthLabels_Metric; case UnitsSystemType.All: default: return lengthLabels; }
                case ModsimUnitsType.Time:
                case ModsimUnitsType.TimeRate:
                    return ModsimUserDefinedTimeStep.GetUserDefTSTypeNames(true);
                case ModsimUnitsType.Energy:
                case ModsimUnitsType.EnergyRate:
                    return energyLabels;
                default:
                    throw new Exception("The enumerator type is not defined within the GetLabels function.");
            }
        }
        /// <summary>Gets the array of labels associated with a specified <c>ModsimUnitsType</c>.</summary>
        /// <param name="unitsType">Specifies the <c>ModsimUnitsType</c>.</param>
        /// <returns>Returns the labels associated with the specified <c>ModsimUnitsType</c>.</returns>
        public static string[] GetLabels(ModsimUnitsType unitsType, UnitsSystemType systemType, bool ToLower)
        {
            if (ToLower)
                return Array.ConvertAll(GetLabels(unitsType, systemType), element => element.ToLower());
            else
                return GetLabels(unitsType, systemType);
        }
        /// <summary>Gets the array of type names associated with a specified <c>ModsimUnitsType</c>.</summary>
        /// <param name="unitsType">Specifies the <c>ModsimUnitsType</c>.</param>
        /// <param name="systemType">Specifies the system of units.</param>
        /// <returns>Returns the labels associated with the specified <c>ModsimUnitsType</c>.</returns>
        public static string[] GetTypeNames(ModsimUnitsType unitsType, UnitsSystemType systemType)
        {
            List<string> names = new List<string>();
            switch (unitsType)
            {
                case ModsimUnitsType.Volume:
                case ModsimUnitsType.VolumeRate:
                    switch (systemType) { case UnitsSystemType.English: names.Add(VolumeUnitsType.AF.ToString()); names.Add(VolumeUnitsType.kAF.ToString()); names.Add(VolumeUnitsType.cf.ToString()); names.Add(VolumeUnitsType.MG.ToString()); break; case UnitsSystemType.Metric: names.Add(VolumeUnitsType.kCM.ToString()); names.Add(VolumeUnitsType.MCM.ToString()); names.Add(VolumeUnitsType.cm.ToString()); break; case UnitsSystemType.All: default: names.AddRange(Enum.GetNames(typeof(VolumeUnitsType))); names.Remove(VolumeUnitsType.Undefined.ToString()); break; }
                    break;
                case ModsimUnitsType.Area:
                case ModsimUnitsType.AreaRate:
                    switch (systemType) { case UnitsSystemType.English: names.Add(AreaUnitsType.Acres.ToString()); names.Add(AreaUnitsType.kAcres.ToString()); names.Add(AreaUnitsType.SqFeet.ToString()); break; case UnitsSystemType.Metric: names.Add(AreaUnitsType.kSqMeters.ToString()); names.Add(AreaUnitsType.MSqMeters.ToString()); names.Add(AreaUnitsType.SqMeters.ToString()); break; case UnitsSystemType.All: default: names.AddRange(Enum.GetNames(typeof(AreaUnitsType))); names.Remove(AreaUnitsType.Undefined.ToString()); break; }
                    break;
                case ModsimUnitsType.Length:
                case ModsimUnitsType.LengthRate:
                    switch (systemType) { case UnitsSystemType.English: names.Add(LengthUnitsType.feet.ToString()); names.Add(LengthUnitsType.inches.ToString()); break; case UnitsSystemType.Metric: names.Add(LengthUnitsType.meters.ToString()); names.Add(LengthUnitsType.centimeters.ToString()); names.Add(LengthUnitsType.millimeters.ToString()); break; case UnitsSystemType.All: default: names.AddRange(Enum.GetNames(typeof(LengthUnitsType))); names.Remove(LengthUnitsType.Undefined.ToString()); break; }
                    break;
                case ModsimUnitsType.Time:
                case ModsimUnitsType.TimeRate:
                    return ModsimUserDefinedTimeStep.GetUserDefTSTypeNames(true);
                case ModsimUnitsType.Energy:
                case ModsimUnitsType.EnergyRate:
                    names.AddRange(Enum.GetNames(typeof(EnergyUnitsType)));
                    names.Remove(EnergyUnitsType.Undefined.ToString());
                    break;
                default:
                    throw new Exception("The enumerator type is not defined within the GetLabels function.");
            }
            return names.ToArray();
        }
        /// <summary>Gets the array of type names associated with a specified <c>ModsimUnitsType</c>.</summary>
        /// <param name="unitsType">Specifies the <c>ModsimUnitsType</c>.</param>
        /// <param name="systemType">Specifies the system of units.</param>
        /// <param name="ToLower">Specifies whether to retrieve the type names after converting all letters to lower case.</param>
        /// <returns>Returns the labels associated with the specified <c>ModsimUnitsType</c>.</returns>
        public static string[] GetTypeNames(ModsimUnitsType unitsType, UnitsSystemType systemType, bool ToLower)
        {
            if (ToLower)
                return Array.ConvertAll(GetTypeNames(unitsType, systemType), element => element.ToLower());
            else
                return GetTypeNames(unitsType, systemType);
        }
        /// <summary>Gets majorUnits and timestep information from a label. Checks all types if <c>type</c> is provided as an output variable.</summary>
        /// <param name="label">The label from which to retrieve majorUnits information.</param>
        /// <param name="majorUnits">The output majorUnits value associated with the specified enumType and label: either majorUnits of volume or area.</param>
        /// <param name="timeStep">The timestep of the label. If no timestep is specified, <c>timeStep</c> will be returned null.</param>
        /// <param name="type">The type of MODSIM majorUnits to parse to.</param>
        private static bool TryParse(string label, out object units, out ModsimTimeStep timeStep, ModsimUnitsType type)
        {
            label = label.ToLower(); // Convert the specified label to lower case
            string[] labels = GetLabels(type, UnitsSystemType.All, true); // Retrieve all type labels in lower case
            string[] enumNames = GetTypeNames(type, UnitsSystemType.All, true); // Retrieve all type names in lower case

            // Deal with special cases 
            label = GetRegularLabel(label);

            // Split the label up into timestep and majorUnits units...
            string[] str = label.Split('(')[0].Split('/');
            units = VolumeUnitsType.Undefined;
            timeStep = null;

            // First, find the timeStep from the label... This may not exist because all units don't necessarily contain timestep information
            if (str.Length > 1)
                timeStep = ModsimTimeStep.FromLabel(str[1]);
            if (IsRateUnit(type) && timeStep == null)
                return false;
            else if (!IsRateUnit(type) && timeStep != null)
                return false;

            // Second, find the volumetric (or area) majorUnits from the label.
            if (str.Length > 0)
                for (int i = 0; i < labels.Length; i++)
                    if (str[0].Equals(labels[i]) || str[0].Equals(enumNames[i]))
                        switch (type)
                        {
                            case ModsimUnitsType.VolumeRate:
                            case ModsimUnitsType.Volume: units = (VolumeUnitsType)i; return true;
                            case ModsimUnitsType.AreaRate:
                            case ModsimUnitsType.Area: units = (AreaUnitsType)i; return true;
                            case ModsimUnitsType.LengthRate:
                            case ModsimUnitsType.Length: units = (LengthUnitsType)i; return true;
                            case ModsimUnitsType.TimeRate:
                            case ModsimUnitsType.Time: units = (ModsimUserDefinedTimeStepType)i; return true;
                            case ModsimUnitsType.EnergyRate:
                            case ModsimUnitsType.Energy: units = (EnergyUnitsType)i; return true;
                            default: return false;
                        }

            return false;
        }
        /// <summary>Gets majorUnits and timestep information from a label. Checks all types if <c>type</c> is provided as an output variable.</summary>
        /// <param name="label">The label from which to retrieve majorUnits information.</param>
        /// <param name="majorUnits">The output majorUnits value associated with the specified enumType and label: either majorUnits of volume or area.</param>
        /// <param name="timeStep">The timestep of the label. If no timestep is specified, <c>timeStep</c> will be returned null.</param>
        public static bool TryParse(string label, out object units, out ModsimTimeStep timeStep, out ModsimUnitsType type)
        {
            ModsimUnitsType[] types = GetTypes(true);
            units = VolumeUnitsType.Undefined;
            timeStep = null;
            type = ModsimUnitsType.Undefined;
            foreach (ModsimUnitsType i in types)
                if (TryParse(label, out units, out timeStep, i))
                {
                    type = i;
                    return true;
                }
            return false;
        }
        /// <summary>Gets a label from a specified enumerator value (only volume majorUnits are returned).</summary>
        /// <param name="enumValue">The majorUnits units for which to get the label (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <returns>Returns the pre-defined majorUnits label.</returns>
        public static string GetLabel(object units)
        {
            return GetLabel(units, null);
        }
        /// <summary>Gets a label from a specified enumerator value (volume majorUnits over timestep majorUnits are returned).</summary>
        /// <param name="units">The majorUnits units for which to get the label (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <returns>Returns the pre-defined majorUnits label.</returns>
        public static string GetLabel(object units, ModsimTimeStep timeStep)
        {
            // The type of majorUnits
            bool isRate = (timeStep != null);
            ModsimUnitsType type = GetType(units, isRate);
            isRate = (isRate && IsRateUnit(type));

            // Get the correct set of labels for the specified unit type.
            string majorUnits = "";
            string[] labels = GetLabels(type, UnitsSystemType.All);
            int i = (int)units;
            if (i < 0 || i >= labels.Length) // Undefined value.
                throw new Exception("Cannot assign a label from an undefined enumerator value.");
            else
                majorUnits = labels[i];

            // Return a special label if desired
            if (useSpecialLabel && isRate && timeStep.TSType == ModsimTimeStepType.Seconds)
            {
                if (MajorUnitsMatch(units, VolumeUnitsType.cf))
                    return "cfs";
                else if (MajorUnitsMatch(units, VolumeUnitsType.cm))
                    return "cms";
                else if (MajorUnitsMatch(units, VolumeUnitsType.MG))
                    return "MGD";
                else if (MajorUnitsMatch(units, EnergyUnitsType.kJ))
                    return "kW";
                else if (MajorUnitsMatch(units, EnergyUnitsType.MJ))
                    return "MW";
                else if (MajorUnitsMatch(units, EnergyUnitsType.GJ))
                    return "GW";
            }
            // Regular labels
            if (isRate)
                return majorUnits + "/" + timeStep.Label;
            else  // Return just the volume or area majorUnits for non-rate type majorUnits
                return majorUnits;
        }
        /// <summary>Creates a new instance from a units label.</summary>
        /// <param name="label">The label for which to create a new <c>ModsimUnits</c> instance.</param>
        /// <returns>Returns a new instance from a units label.</returns>
        public static ModsimUnits FromLabel(string label)
        {
            return new ModsimUnits(label);
        }

        #endregion
        #region Shared methods to check units type

        // For any units within this class...
        /// <summary>Determines whether the specified majorUnits units are in the metric system of units.</summary>
        /// <param name="units">The majorUnits units (a value within the enumerators <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <returns>Returns true if the units are metric. Otherwise, false.</returns>
        public static bool IsMetricUnit(object units)
        {
            if (units.GetType().Equals(typeof(VolumeUnitsType)))
                return (VolumeUnitsType)units >= VolumeUnitsType.kCM;
            else if (units.GetType().Equals(typeof(AreaUnitsType)))
                return (AreaUnitsType)units >= AreaUnitsType.kSqMeters;
            else if (units.GetType().Equals(typeof(LengthUnitsType)))
                return (LengthUnitsType)units >= LengthUnitsType.meters;
            else if (units.GetType().Equals(typeof(EnergyUnitsType)))
                return (EnergyUnitsType)units != EnergyUnitsType.BTU;
            else if (units.GetType().Equals(typeof(ModsimUserDefinedTimeStepType)))
                return true;
            else if (units.GetType().Equals(typeof(ModsimTimeStepType)))
                return true;
            else
                throw new Exception("Unrecognized majorUnits type.");
        }
        /// <summary>Determines if the specified units type is a rate type.</summary>
        /// <param name="type">The type to check whether it is a rate type.</param>
        /// <returns>Returns true if the specified units type is a rate; otherwise, returns false.</returns>
        public static bool IsRateUnit(ModsimUnitsType type)
        {
            return type.ToString().EndsWith("Rate");
        }

        // For specific units within this class...
        /// <summary>Determines if the specified units is a volume type.</summary>
        /// <param name="units">The units to test.</param>
        /// <returns>Returns true if the units are of a volume type. Otherwise, returns false.</returns>
        public static bool IsVolumeUnit(object units)
        {
            return units.GetType().Equals(typeof(VolumeUnitsType));
        }
        /// <summary>Determines if the specified units is a are type.</summary>
        /// <param name="units">The units to test.</param>
        /// <returns>Returns true if the units are of a area type. Otherwise, returns false.</returns>
        public static bool IsAreaUnit(object units)
        {
            return units.GetType().Equals(typeof(AreaUnitsType));
        }
        /// <summary>Determines if the specified units is a length type.</summary>
        /// <param name="units">The units to test.</param>
        /// <returns>Returns true if the units are of a length type. Otherwise, returns false.</returns>
        public static bool IsLengthUnit(object units)
        {
            return units.GetType().Equals(typeof(LengthUnitsType));
        }
        /// <summary>Determines if the specified units is an energy type.</summary>
        /// <param name="units">The units to test.</param>
        /// <returns>Returns true if the units are of an energy type. Otherwise, returns false.</returns>
        public static bool IsEnergyUnit(object units)
        {
            return units.GetType().Equals(typeof(EnergyUnitsType));
        }
        /// <summary>Determines if the specified units is a time type (includes user-defined).</summary>
        /// <param name="units">The units to test.</param>
        /// <returns>Returns true if the units are of a time type (includes user-defined). Otherwise, returns false.</returns>
        public static bool IsTimeUnit(object units)
        {
            Type enumType = units.GetType();
            return enumType.Equals(typeof(ModsimUserDefinedTimeStepType)) || enumType.Equals(typeof(ModsimTimeStepType)) || enumType.Equals(typeof(ModsimUserDefinedTimeStep)) || enumType.Equals(typeof(ModsimTimeStep));
        }
        /// <summary>Determines if the specified units is a user-defined time type.</summary>
        /// <param name="units">The units to test.</param>
        /// <returns>Returns true if the units are of a user-defined time type. Otherwise, returns false.</returns>
        public static bool IsUserDefTimeUnit(object units)
        {
            Type enumType = units.GetType();
            return enumType.Equals(typeof(ModsimUserDefinedTimeStepType)) || enumType.Equals(typeof(ModsimUserDefinedTimeStep));
        }
        /// <summary>Determines if the specified units is a <c>ModsimUnits</c> type.</summary>
        /// <param name="units">The units to test.</param>
        /// <returns>Returns true if the units are of a <c>ModsimUnits</c> type. Otherwise, returns false.</returns>
        public static bool IsModsimUnit(object units)
        {
            return units.GetType().Equals(typeof(ModsimUnits));
        }

        #endregion
        #region Shared methods and properties to retrieve default units

        private static ModsimUnits defaultVolumeUnits(bool UseMetric)
        {
            if (UseMetric)
                return defaultVolumeUnits_Metric;
            else
                return defaultVolumeUnits_English;
        }
        private static ModsimUnits defaultVolumeRateUnits(bool UseMetric)
        {
            if (UseMetric)
                return defaultVolumeRateUnits_Metric;
            else
                return defaultVolumeRateUnits_English;
        }
        private static ModsimUnits defaultAreaUnits(bool UseMetric)
        {
            if (UseMetric)
                return defaultAreaUnits_Metric;
            else
                return defaultAreaUnits_English;
        }
        private static ModsimUnits defaultAreaRateUnits(bool UseMetric)
        {
            if (UseMetric)
                return defaultAreaRateUnits_Metric;
            else
                return defaultAreaRateUnits_English;
        }
        private static ModsimUnits defaultLengthUnits(bool UseMetric)
        {
            if (UseMetric)
                return defaultLengthUnits_Metric;
            else
                return defaultLengthUnits_English;
        }
        private static ModsimUnits defaultLengthRateUnits(bool UseMetric)
        {
            if (UseMetric)
                return defaultLengthRateUnits_Metric;
            else
                return defaultLengthRateUnits_English;
        }
        /// <summary>Retrieves the default <c>ModsimUnits</c> used for a specified type and system.</summary>
        /// <param name="unitsType">The type of units.</param>
        /// <param name="UseMetric">If true, returns the default Metric units; otherwise, returns the default English units. For special types, such as Time, TimeRate, Energy, and EnergyRate, the same units are returned regardless of what value <c>UseMetric</c> is.</param>
        /// <returns>Returns the default <c>ModsimUnits</c> used for a specified type and system of units.</returns>
        public static ModsimUnits GetDefaultUnits(ModsimUnitsType unitsType, bool UseMetric)
        {
            switch (unitsType)
            {
                case ModsimUnitsType.Volume: return defaultVolumeUnits(UseMetric);
                case ModsimUnitsType.VolumeRate: return defaultVolumeRateUnits(UseMetric);
                case ModsimUnitsType.Area: return defaultAreaUnits(UseMetric);
                case ModsimUnitsType.AreaRate: return defaultAreaRateUnits(UseMetric);
                case ModsimUnitsType.Length: return defaultLengthUnits(UseMetric);
                case ModsimUnitsType.LengthRate: return defaultLengthRateUnits(UseMetric);
                case ModsimUnitsType.Time: return defaultTimeUnits;
                case ModsimUnitsType.TimeRate: return defaultTimeRateUnits;
                case ModsimUnitsType.Energy: return defaultEnergyUnits;
                case ModsimUnitsType.EnergyRate: return defaultEnergyRateUnits;
                default: throw new Exception("No default units exist for undefined types of units.");
            }
        }
        public static ModsimTimeStep DefaultTimeStep
        {
            get
            {
                return defaultTimeStep;
            }
        }

        #endregion

        #region Overrides

        /// <summary>Returns the label of this instance.</summary>
        /// <returns>Returns the label of this instance.</returns>
        public override string ToString()
        {
            return this.Label;
        }
        /// <summary>Checks equality between this instance of <c>ModsimUnits</c> and another instance by checking the majorUnits units and timestep... They do not have to have the same label.</summary>
        /// <returns>Returns true if the two instances have the same units; otherwise, false.</returns>
        public override bool Equals(object units)
        {
            if (units == null || !this.GetType().Equals(units.GetType()))
                return false;
            ModsimUnits u = (ModsimUnits)units;
            return u.Type == this.Type && u.MajorUnitsEquals(this.MajorUnits) && u.TimeStep == this.TimeStep;
        }
        /// <summary>Gets the hash code from the base class.</summary>
        /// <returns>Returns the base class's hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
        #region Operators

        // Implicit operators
        /// <summary>Creates a new units type from a string containing the label for the <c>ModsimUnits</c> class.</summary>
        /// <param name="label">The label assiged to this instance of <c>ModsimUnits</c>.</param>
        /// <returns>Returns the <c>ModsimUnits</c> instance associated with the specified <c>label</c>.</returns>
        public static implicit operator ModsimUnits(string label)
        {
            if (label == "" || label == null) return null;
            return new ModsimUnits(label);
        }
        /// <summary>Creates a new instance of <c>ModsimUnits</c> from a specified type.</summary>
        /// <param name="type">Specifies the type of ModsimUnits to create.</param>
        /// <returns>Returns a new instance of <c>ModsimUnits</c> from a specified type.</returns>
        public static implicit operator ModsimUnits(VolumeUnitsType type)
        {
            return new ModsimUnits(type);
        }
        /// <summary>Creates a new instance of <c>ModsimUnits</c> from a specified type.</summary>
        /// <param name="type">Specifies the type of ModsimUnits to create.</param>
        /// <returns>Returns a new instance of <c>ModsimUnits</c> from a specified type.</returns>
        public static implicit operator ModsimUnits(AreaUnitsType type)
        {
            return new ModsimUnits(type);
        }
        /// <summary>Creates a new instance of <c>ModsimUnits</c> from a specified type.</summary>
        /// <param name="type">Specifies the type of ModsimUnits to create.</param>
        /// <returns>Returns a new instance of <c>ModsimUnits</c> from a specified type.</returns>
        public static implicit operator ModsimUnits(LengthUnitsType type)
        {
            return new ModsimUnits(type);
        }
        /// <summary>Creates a new instance of <c>ModsimUnits</c> from a specified type.</summary>
        /// <param name="type">Specifies the type of ModsimUnits to create.</param>
        /// <returns>Returns a new instance of <c>ModsimUnits</c> from a specified type.</returns>
        public static implicit operator ModsimUnits(EnergyUnitsType type)
        {
            return new ModsimUnits(type);
        }
        /// <summary>Creates a new instance of <c>ModsimUnits</c> from a specified type.</summary>
        /// <param name="type">Specifies the type of ModsimUnits to create.</param>
        /// <returns>Returns a new instance of <c>ModsimUnits</c> from a specified type.</returns>
        public static implicit operator ModsimUnits(ModsimTimeStepType type)
        {
            return new ModsimUnits(type);
        }
        /// <summary>Creates a new instance of <c>ModsimUnits</c> from a specified type.</summary>
        /// <param name="type">Specifies the type of ModsimUnits to create.</param>
        /// <returns>Returns a new instance of <c>ModsimUnits</c> from a specified type.</returns>
        public static implicit operator ModsimUnits(ModsimUserDefinedTimeStepType type)
        {
            return new ModsimUnits(type);
        }
        /// <summary>Gets the label for an instance of <c>ModsimUnits</c>.</summary>
        /// <param name="units">The instance of <c>ModsimUnits</c>.</param>
        /// <returns>Returns the label for <c>units</c>.</returns>
        public static implicit operator string(ModsimUnits units)
        {
            return units == null ? "" : units.Label;
        }

        // Explicit operators (make sure the user knows they're losing information in this conversion)
        /// <summary>Gets the MajorUnits as a specific type.</summary>
        /// <param name="units">The units to retrieve.</param>
        /// <returns>Returns the MajorUnits as a specific type.</returns>
        public static explicit operator VolumeUnitsType(ModsimUnits units)
        {
            return (VolumeUnitsType)units.MajorUnits;
        }
        /// <summary>Gets the MajorUnits as a specific type.</summary>
        /// <param name="units">The units to retrieve.</param>
        /// <returns>Returns the MajorUnits as a specific type.</returns>
        public static explicit operator AreaUnitsType(ModsimUnits units)
        {
            return (AreaUnitsType)units.MajorUnits;
        }
        /// <summary>Gets the MajorUnits as a specific type.</summary>
        /// <param name="units">The units to retrieve.</param>
        /// <returns>Returns the MajorUnits as a specific type.</returns>
        public static explicit operator LengthUnitsType(ModsimUnits units)
        {
            return (LengthUnitsType)units.MajorUnits;
        }
        /// <summary>Gets the MajorUnits as a specific type.</summary>
        /// <param name="units">The units to retrieve.</param>
        /// <returns>Returns the MajorUnits as a specific type.</returns>
        public static explicit operator EnergyUnitsType(ModsimUnits units)
        {
            return (EnergyUnitsType)units.MajorUnits;
        }
        /// <summary>Gets the MajorUnits as a specific type.</summary>
        /// <param name="units">The units to retrieve.</param>
        /// <returns>Returns the MajorUnits as a specific type.</returns>
        public static explicit operator ModsimUserDefinedTimeStepType(ModsimUnits units)
        {
            return (ModsimUserDefinedTimeStepType)units.MajorUnits;
        }
        /// <summary>Gets the MajorUnits as a specific type.</summary>
        /// <param name="units">The units to retrieve.</param>
        /// <returns>Returns the MajorUnits as a specific type.</returns>
        public static explicit operator ModsimTimeStepType(ModsimUnits units)
        {
            return (ModsimTimeStepType)units.MajorUnits;
        }

        // Equality operators
        /// <summary>Checks equality between two instances of <c>ModsimUnits</c>. They do not have to have the same label.</summary>
        /// <returns>Returns true if the two instances have the same majorUnits and temporal units; otherwise, returns false.</returns>
        public static bool operator ==(ModsimUnits a, ModsimUnits b)
        {
            if ((object)a == null || (object)b == null)
                if ((object)a == null && (object)b == null)
                    return true;
                else return false;
            else
                return a.Equals(b);
        }
        /// <summary>Checks equality between two instances of <c>ModsimUnits</c>. They do not have to have the same label.</summary>
        /// <returns>Returns false if the two instances have the same majorUnits and temporal units; otherwise, returns true.</returns>
        public static bool operator !=(ModsimUnits a, ModsimUnits b)
        {
            return !(a == b);
        }

        #endregion

        #region Local methods to check for equality

        /// <summary>Compares this instance's majorUnits with a specified majorUnits.</summary>
        /// <param name="units">The majorUnits units to compare to (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <returns>Returns true if this instance's majorUnits units are the same as the specified majorUnits units.</returns>
        public bool MajorUnitsEquals(object units)
        {
            return MajorUnitsMatch(this.majorUnits, units);
        }
        /// <summary>Compares this instance's majorUnits type with a specified majorUnits type.</summary>
        /// <param name="type">The majorUnits units to compare to (a value from <c>VolumeUnitsType</c>, <c>AreaUnitsType</c>, <c>LengthUnitsType</c>, or <c>EnergyUnitsType</c>).</param>
        /// <returns>Returns true if this instance's majorUnits units are the same as the specified majorUnits units.</returns>
        public bool MajorUnitsTypeEquals(Type type)
        {
            return this.majorUnits.GetType().Equals(type);
        }
        /// <summary>Compares two majorUnits values.</summary>
        /// <returns>Returns true if this instance's majorUnits units are the same as the specified majorUnits units.</returns>
        public static bool MajorUnitsMatch(object a, object b)
        {
            return a.GetType().Equals(b.GetType()) && (int)a == (int)b;
        }
        ////We will want to add a compatibility check eventually... it's very complicated though... Not worth the time right now...
        ///// <summary>Checks whether this instance has incompatibilities with the specified version type.</summary>
        ///// <param name="versionType">The version for which to check compatibility.</param>
        //public bool HasIncompatibilities(OutputVersionType versionType)
        //{

        //}

        #endregion
        #region Units conversion and integration

        /// <summary>Converts a value to specified majorUnits and timestep. <c>toUnits</c> needs to be of the same enumeration type of this instance. Uses an average time span if the date is not provided.</summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns the converted value.</returns>
        public double ConvertTo(double value, ModsimUnits toUnits)
        {
            return value * ConversionFactors.Generic(this, toUnits);
        }
        /// <summary>Converts a value to specified majorUnits and timestep. <c>toUnits</c> need to be of the same enumeration type of this instance. Uses an average time span if the date is not provided.</summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <param name="date">The date at which the timestep is evaluated. If not specified, an average timestep is used for the conversion.</param>
        /// <returns>Returns the converted value.</returns>
        public double ConvertTo(double value, ModsimUnits toUnits, DateTime date)
        {
            return value * ConversionFactors.Generic(this, toUnits, date);
        }
        /// <summary>Converts a value to specified majorUnits and timestep. <c>fromUnits</c> needs to be of the same enumeration type of this instance. Uses an average time span if the date is not provided.</summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="fromUnits">The units to convert to.</param>
        /// <returns>Returns the converted value.</returns>
        public double ConvertFrom(double value, ModsimUnits fromUnits)
        {
            return value * ConversionFactors.Generic(fromUnits, this);
        }
        /// <summary>Converts a value to specified majorUnits and timestep. <c>fromUnits</c> needs to be of the same enumeration type of this instance. Uses an average time span if the date is not provided.</summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="fromUnits">The units to convert to.</param>
        /// <param name="date">The date at which the timestep is evaluated. If not specified, an average timestep is used for the conversion.</param>
        /// <returns>Returns the converted value.</returns>
        public double ConvertFrom(double value, ModsimUnits fromUnits, DateTime date)
        {
            return value * ConversionFactors.Generic(fromUnits, this, date);
        }
        /// <summary>Integrates a rate value between two dates. The output is in the specified (toUnits.MajorUnits). If no units are specified, the output value is in the same MajorUnits of this instance.</summary>
        /// <param name="rateValue">The rate value to integrate.</param>
        /// <param name="StartDate">The start date of integration.</param>
        /// <param name="EndDate">The end date of integration.</param>
        /// <returns>Returns the integrated rate value, which is now in volumetric majorUnits...</returns>
        public double Integrate(double rateValue, DateTime StartDate, DateTime EndDate)
        {
            if (!this.isRate) throw new Exception("This instance of ModsimUnits (" + this.Label + ") must be a rate in order to integrate values within it.");
            if (StartDate > EndDate) throw new Exception("Starting date cannot be greater than the ending date when integrating flows.");
            return rateValue * EndDate.Subtract(StartDate).TotalDays / this.timeStep.ToTimeSpan(StartDate).TotalDays;
        }
        /// <summary>Integrates a rate value between two dates. The output is in the specified (toUnits.MajorUnits). If no units are specified, the output value is in the same MajorUnits of this instance.</summary>
        /// <param name="rateValue">The rate value to integrate.</param>
        /// <param name="StartDate">The start date of integration.</param>
        /// <param name="EndDate">The end date of integration.</param>
        /// <param name="toUnits">The majorUnits to convert to. Must be a rate-type unit.</param>
        /// <returns>Returns the integrated rate value, which is now in volumetric majorUnits...</returns>
        public double Integrate(double rateValue, DateTime StartDate, DateTime EndDate, object MajorUnits)
        {
            double val = Integrate(rateValue, StartDate, EndDate);
            if (MajorUnits == null) return val;
            if (IsModsimUnit(MajorUnits))
                return val * ConversionFactors.Generic(this.MajorUnits, ((ModsimUnits)MajorUnits).MajorUnits);
            else
                return val * ConversionFactors.Generic(this.MajorUnits, MajorUnits);
        }
        /// <summary>Calculates the left-hand Reimann sum for an array of rate values <c>rateValues</c> until <c>EndDate</c>. Output is in the specified <c>toUnits.MajorUnits</c>. If no units are specified, the volume is in the same MajorUnits as this instance.</summary>
        /// <param name="rateValues">An array of flows to integrate.</param>
        /// <param name="dates">An array of dates over which the integration takes place (assumed to be sorted).</param>
        /// <param name="StartDate">The starting date of the integration. Does not have to fall on a date within <c>dates</c>.</param>
        /// <param name="EndDate">The ending date of the integration. Does not have to fall on a date within <c>dates</c>.</param>
        /// <returns>Returns the volume of flows over the period of interest.</returns>
        public double Integrate(double[] rateValues, DateTime[] dates, DateTime StartDate, DateTime EndDate, bool Interpolate)
        {
            // Data check
            int numOfVals;
            if ((numOfVals = rateValues.Length) != dates.Length) throw new Exception("The flows and dates arrays must be the same length.");
            if (numOfVals == 0) return 0.0;
            if (StartDate >= EndDate) throw new Exception("The starting date must be less than the ending date for integration.");
            // Get initial values
            int i = 0;
            double sum = 0.0;
            // Integrate the first value
            for (i = 0; i < numOfVals; i++)
                if (dates[i] > StartDate)
                {
                    if (i == 0) break;
                    double val = !Interpolate ? rateValues[i - 1] : (rateValues[i] - rateValues[i - 1]) / (dates[i].Subtract(dates[i - 1]).TotalDays) * (StartDate.Subtract(dates[i - 1]).TotalDays) + rateValues[i - 1];
                    if (dates[i] >= EndDate) return Integrate(val, StartDate, EndDate);
                    sum += Integrate(val, StartDate, dates[i]);
                    break;
                }
            if (i == numOfVals) i--;
            // Integrate all but the last value
            for (; i + 1 < numOfVals && dates[i + 1] < EndDate; i++)
                sum += Integrate(rateValues[i], dates[i], dates[i + 1]);
            // Integrate the last value
            sum += Integrate(Interpolate && i + 1 < numOfVals ? (rateValues[i + 1] - rateValues[i]) / (dates[i + 1].Subtract(dates[i]).TotalDays) * (EndDate.Subtract(dates[i]).TotalDays) + rateValues[i] : rateValues[i], dates[i], EndDate);
            return sum;
        }
        /// <summary>Calculates the left-hand Reimann sum for an array of rate values <c>rateValues</c> until <c>EndDate</c>. Output is in the specified <c>toUnits.MajorUnits</c>. If no units are specified, the volume is in the same MajorUnits as this instance.</summary>
        /// <param name="rateValues">An array of flows to integrate.</param>
        /// <param name="dates">An array of dates over which the integration takes place (assumed to be sorted).</param>
        /// <param name="StartDate">The starting date of the integration. Does not have to fall on a date within <c>dates</c>.</param>
        /// <param name="EndDate">The ending date of the integration. Does not have to fall on a date within <c>dates</c>.</param>
        /// <param name="toUnits">The majorUnits to convert to. Must be a rate-type unit.</param>
        /// <returns>Returns the volume of flows over the period of interest.</returns>
        public double Integrate(double[] rateValues, DateTime[] dates, DateTime StartDate, DateTime EndDate, bool Interpolate, object MajorUnits)
        {
            double val = Integrate(rateValues, dates, StartDate, EndDate, Interpolate);
            if (MajorUnits == null) return val;
            if (IsModsimUnit(MajorUnits))
                return val * ConversionFactors.Generic(this.MajorUnits, ((ModsimUnits)MajorUnits).MajorUnits);
            else
                return val * ConversionFactors.Generic(this.MajorUnits, MajorUnits);
        }
        /// <summary>Calculates the rate of a value specified between to dates.</summary>
        /// <param name="value">The value.</param>
        /// <param name="StartDate">The start date.</param>
        /// <param name="EndDate">The end date.</param>
        /// <param name="toUnits">The rate units to convert the units to.</param>
        public double ToRate(double value, DateTime StartDate, DateTime EndDate, ModsimUnits toUnits)
        {
            if (!toUnits.IsRate) throw new Exception(toUnits.Label + " is not a rate. You must specify a rate to convert these units to a rate.");
            if (this.isRate) return this.ConvertTo(value, toUnits, StartDate);
            return toUnits.ConvertFrom(value / EndDate.Subtract(StartDate).TotalDays, new ModsimUnits(this.MajorUnits, new ModsimTimeStep(ModsimTimeStepType.Daily)), StartDate);
        }

        #endregion
    }

    /// <summary>Calculates conversion factors for majorUnits units.</summary>
    public class ConversionFactors
    {
        #region Generic Conversion

        /// <summary>Calculates a conversion factor for a generic set of units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Generic(object fromUnits, object toUnits)
        {
            if (fromUnits == null || toUnits == null) return 1.0; 
            if (ModsimUnits.IsModsimUnit(fromUnits) && ModsimUnits.IsModsimUnit(toUnits))
            {
                ModsimUnits fUnits = (ModsimUnits)fromUnits;
                ModsimUnits tUnits = (ModsimUnits)toUnits;
                if (fUnits.IsRate && tUnits.IsRate)
                    return Generic(fUnits.MajorUnits, tUnits.MajorUnits) / Time(fUnits.TimeStep, tUnits.TimeStep);
                else
                    return Generic(fUnits.MajorUnits, tUnits.MajorUnits);
            }
            else if (ModsimUnits.IsVolumeUnit(fromUnits) && ModsimUnits.IsVolumeUnit(toUnits))
                return Volume((VolumeUnitsType)fromUnits, (VolumeUnitsType)toUnits);
            else if (ModsimUnits.IsAreaUnit(fromUnits) && ModsimUnits.IsAreaUnit(toUnits))
                return Area((AreaUnitsType)fromUnits, (AreaUnitsType)toUnits);
            else if (ModsimUnits.IsLengthUnit(fromUnits) && ModsimUnits.IsLengthUnit(toUnits))
                return Length((LengthUnitsType)fromUnits, (LengthUnitsType)toUnits);
            else if (ModsimUnits.IsEnergyUnit(fromUnits) && ModsimUnits.IsEnergyUnit(toUnits))
                return Energy((EnergyUnitsType)fromUnits, (EnergyUnitsType)toUnits);
            else if (ModsimUnits.IsUserDefTimeUnit(fromUnits) && ModsimUnits.IsUserDefTimeUnit(toUnits))
            {
                if (fromUnits.GetType().Equals(typeof(ModsimUserDefinedTimeStepType)))
                    fromUnits = new ModsimUserDefinedTimeStep((ModsimUserDefinedTimeStepType)fromUnits);
                if (toUnits.GetType().Equals(typeof(ModsimUserDefinedTimeStepType)))
                    toUnits = new ModsimUserDefinedTimeStep((ModsimUserDefinedTimeStepType)toUnits);
                return Time((ModsimUserDefinedTimeStep)fromUnits, (ModsimUserDefinedTimeStep)toUnits);
            }
            else if (ModsimUnits.IsTimeUnit(fromUnits) && ModsimUnits.IsTimeUnit(toUnits))
            {
                if (fromUnits.GetType().Equals(typeof(ModsimTimeStepType)))
                    fromUnits = new ModsimTimeStep((ModsimTimeStepType)fromUnits);
                if (toUnits.GetType().Equals(typeof(ModsimTimeStepType)))
                    toUnits = new ModsimTimeStep((ModsimTimeStepType)toUnits);
                return Time((ModsimTimeStep)fromUnits, (ModsimTimeStep)toUnits);
            }
            else
                throw new Exception(string.Format("Unable to find a conversion factor with the specified inputs of type {0} and {1}.", fromUnits, toUnits));
        }
        /// <summary>Calculates a conversion factor for a generic set of units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <param name="date">The date at which the factor is calculated if the units contain timestep information.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Generic(object fromUnits, object toUnits, DateTime date)
        {
            if (fromUnits == null || toUnits == null) return 1.0; 
            if (ModsimUnits.IsModsimUnit(fromUnits) && ModsimUnits.IsModsimUnit(toUnits))
            {
                ModsimUnits fUnits = (ModsimUnits)fromUnits;
                ModsimUnits tUnits = (ModsimUnits)toUnits;
                if (fUnits.IsRate && tUnits.IsRate)
                    return Generic(fUnits.MajorUnits, tUnits.MajorUnits, date) / Time(fUnits.TimeStep, tUnits.TimeStep, date);
                else
                    return Generic(fUnits.MajorUnits, tUnits.MajorUnits, date);
            }
            else if (ModsimUnits.IsUserDefTimeUnit(fromUnits) && ModsimUnits.IsUserDefTimeUnit(toUnits))
                return Time((ModsimUserDefinedTimeStep)fromUnits, (ModsimUserDefinedTimeStep)toUnits, date);
            else if (ModsimUnits.IsTimeUnit(fromUnits) && ModsimUnits.IsTimeUnit(toUnits))
                return Time((ModsimTimeStep)fromUnits, (ModsimTimeStep)toUnits, date);
            else
                return Generic(fromUnits, toUnits);
        }

        #endregion
        #region Spatial Conversion

        /// <summary>Calculates a conversion factor for a set of volume units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Volume(VolumeUnitsType fromUnits, VolumeUnitsType toUnits)
        {
            // First, convert everything to AF...
            double factor = 1.0;
            switch (fromUnits)
            {
                case VolumeUnitsType.AF: break;
                case VolumeUnitsType.kAF: factor *= 1000.0; break;
                case VolumeUnitsType.cf: factor /= 43560.0; break;
                case VolumeUnitsType.MG: factor *= 3.06888327742973; break;
                case VolumeUnitsType.kCM: factor *= 0.810713193789913; break;
                case VolumeUnitsType.MCM: factor *= 810.713193789913; break;
                case VolumeUnitsType.cm: factor *= 0.000810713193789913; break;
                default: throw new Exception("A conversion factor cannot be calculated for an undefined type: " + fromUnits.ToString());
            }

            // Second, convert everything to the desired units...
            switch (toUnits)
            {
                case VolumeUnitsType.AF: break;
                case VolumeUnitsType.kAF: factor /= 1000.0; break;
                case VolumeUnitsType.cf: factor *= 43560.0; break;
                case VolumeUnitsType.MG: factor /= 3.06888327742973; break;
                case VolumeUnitsType.kCM: factor /= 0.810713193789913; break;
                case VolumeUnitsType.MCM: factor /= 810.713193789913; break;
                case VolumeUnitsType.cm: factor /= 0.000810713193789913; break;
                default: throw new Exception("A conversion factor cannot be calculated for an undefined type: " + toUnits.ToString());
            }
            return factor;
        }
        /// <summary>Calculates a conversion factor for a set of area units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Area(AreaUnitsType fromUnits, AreaUnitsType toUnits)
        {
            // First, convert everything to acres
            double factor = 1.0;
            switch (fromUnits)
            {
                case AreaUnitsType.Acres: break;
                case AreaUnitsType.kAcres: factor *= 1000.0; break;
                case AreaUnitsType.SqFeet: factor /= 43560.0; break;
                case AreaUnitsType.kSqMeters: factor *= 0.247105381467165; break;
                case AreaUnitsType.MSqMeters: factor *= 247.105381467165; break;
                case AreaUnitsType.SqMeters: factor *= 0.000247105381467165; break;
                default: throw new Exception("A conversion factor cannot be calculated for an undefined type: " + fromUnits.ToString());
            }

            // Second, convert everything to desired units
            switch (toUnits)
            {
                case AreaUnitsType.Acres: break;
                case AreaUnitsType.kAcres: factor /= 1000.0; break;
                case AreaUnitsType.SqFeet: factor *= 43560.0; break;
                case AreaUnitsType.kSqMeters: factor /= 0.247105381467165; break;
                case AreaUnitsType.MSqMeters: factor /= 247.105381467165; break;
                case AreaUnitsType.SqMeters: factor /= 0.000247105381467165; break;
                default: throw new Exception("A conversion factor cannot be calculated for an undefined type: " + toUnits.ToString());
            }
            return factor;
        }
        /// <summary>Calculates a conversion factor for a set of length units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Length(LengthUnitsType fromUnits, LengthUnitsType toUnits)
        {
            // First, convert everything to feet 
            double factor = 1.0;
            switch (fromUnits)
            {
                case LengthUnitsType.feet: break;
                case LengthUnitsType.inches: factor /= 12.0; break;
                case LengthUnitsType.meters: factor *= 3.28083989501312; break;
                case LengthUnitsType.centimeters: factor *= 0.0328083989501312; break;
                case LengthUnitsType.millimeters: factor *= 0.00328083989501312; break;
                default: throw new Exception("A conversion factor cannot be calculated for an undefined type: " + fromUnits.ToString());
            }

            // Second, convert everything to desired units
            switch (toUnits)
            {
                case LengthUnitsType.feet: break;
                case LengthUnitsType.inches: factor *= 12.0; break;
                case LengthUnitsType.meters: factor /= 3.28083989501312; break;
                case LengthUnitsType.centimeters: factor /= 0.0328083989501312; break;
                case LengthUnitsType.millimeters: factor /= 0.00328083989501312; break;
                default: throw new Exception("A conversion factor cannot be calculated for an undefined type: " + toUnits.ToString());
            }
            return factor;
        }

        #endregion
        #region Energy Conversion

        /// <summary>Calculates a conversion factor for a set of energy units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Energy(EnergyUnitsType fromUnits, EnergyUnitsType toUnits)
        {
            // First, convert everything to MWh
            double factor = 1.0;
            switch (fromUnits)
            {
                case EnergyUnitsType.MWh: break;
                case EnergyUnitsType.kWh: factor *= 0.001; break;
                case EnergyUnitsType.GWh: factor *= 1000; break;
                case EnergyUnitsType.BTU: factor /= 3412141.63; break;
                case EnergyUnitsType.kJ: factor /= 3600000; break;
                case EnergyUnitsType.MJ: factor /= 3600; break;
                case EnergyUnitsType.GJ: factor /= 3.6; break;
                default: throw new Exception("A conversion factor cannot be calculated for an undefined type: " + fromUnits.ToString());
            }

            // Second, convert everything to desired units
            switch (toUnits)
            {
                case EnergyUnitsType.MWh: break;
                case EnergyUnitsType.kWh: factor /= 0.001; break;
                case EnergyUnitsType.GWh: factor /= 1000; break;
                case EnergyUnitsType.BTU: factor *= 3412141.63; break;
                case EnergyUnitsType.kJ: factor *= 3600000; break;
                case EnergyUnitsType.MJ: factor *= 3600; break;
                case EnergyUnitsType.GJ: factor *= 3.6; break;
                default: throw new Exception("A conversion factor cannot be calculated for an undefined type: " + toUnits.ToString());
            }
            return factor;
        }

        #endregion
        #region Temporal Conversion

        /// <summary>Calculates a conversion factor for a set of timestep units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Time(ModsimUserDefinedTimeStep fromUnits, ModsimUserDefinedTimeStep toUnits)
        {
            if (fromUnits == null || toUnits == null) return 1.0; 
            return fromUnits.AverageTimeSpan.TotalDays / toUnits.AverageTimeSpan.TotalDays;
        }
        /// <summary>Calculates a conversion factor for a set of timestep units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <param name="date">The date over which the time units are being converted. If not specified, average time spans are used.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Time(ModsimUserDefinedTimeStep fromUnits, ModsimUserDefinedTimeStep toUnits, DateTime date)
        {
            if (fromUnits == null || toUnits == null) return 1.0;
            return fromUnits.ToTimeSpan(date).TotalDays / toUnits.ToTimeSpan(date).TotalDays;
        }
        /// <summary>Calculates a conversion factor for a set of timestep units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Time(ModsimTimeStep fromUnits, ModsimTimeStep toUnits)
        {
            if (fromUnits == null || toUnits == null) return 1.0;
            return fromUnits.AverageTimeSpan.TotalDays / toUnits.AverageTimeSpan.TotalDays;
        }
        /// <summary>Calculates a conversion factor for a set of timestep units. This factor should be multiplied by the value to convert.</summary>
        /// <param name="fromUnits">The units to convert from.</param>
        /// <param name="toUnits">The units to convert to.</param>
        /// <param name="date">The date over which the time units are being converted. If not specified, average time spans are used.</param>
        /// <returns>Returns the conversion factor.</returns>
        public static double Time(ModsimTimeStep fromUnits, ModsimTimeStep toUnits, DateTime date)
        {
            if (fromUnits == null || toUnits == null) return 1.0;
            return fromUnits.ToTimeSpan(date).TotalDays / toUnits.ToTimeSpan(date).TotalDays;
        }

        #endregion
    }
}
