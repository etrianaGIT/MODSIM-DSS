using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ASquared.SymbolicMath;

namespace Csu.Modsim.ModsimModel
{
    public enum ElevType { Forebay, Tailwater }
    public enum HydroUnitType { Pump, Turbine }

    /// <summary>Contains references and information to be able calculate hydropower production.</summary>
    public class HydropowerUnit : IModsimObject, IComparable
    {
        #region Static variables and properties (defaults)

        /// <summary>The command name used in the xy file.</summary>
        public static string XYCmdName { get { return "hydroUnit"; } }
        public static string ForbiddenStringInName { get { return "|"; } }

        // Units used to convert flow, head, and efficiency to hydropower
        /// <summary>Gets the default flow units used in calculating hydropower generation.</summary>
        public readonly static ModsimUnits DefaultFlowUnits = new ModsimUnits(VolumeUnitsType.cf, ModsimTimeStepType.Seconds);
        /// <summary>Gets the default length units used in calculating hydropower generation.</summary>
        public readonly static ModsimUnits DefaultHeadUnits = new ModsimUnits(LengthUnitsType.feet);
        /// <summary>Gets the default units for electrical power generation.</summary>
        public readonly static ModsimUnits DefaultPowerUnits = new ModsimUnits(EnergyUnitsType.kJ, ModsimTimeStepType.Seconds);
        /// <summary>Gets the default units for electrical energy generation.</summary>
        public readonly static ModsimUnits DefaultEnergyUnits = new ModsimUnits(EnergyUnitsType.MWh);
        /// <summary>Gets the default downtime TimeSpan.</summary>
        public readonly static TimeSpan DefaultDownTime = new TimeSpan(0);
        /// <summary>Gets the conversion factor for converting lb-ft/sec to kW.</summary>
        public static double kWperlbftsec { get { return 0.745699872 / 550.0; } }
        /// <summary>Gets the specific weight of water (62.4 lb/ft^3).</summary>
        public static double SpecWeightH2O { get { return 62.4; } }

        #endregion

        #region Local variables

        // hydro unit info
        private Model model;
        private string name = "";
        private string description = "";
        private int id = -1;
        private HydroUnitType type;

        // definitions of hydro unit inputs
        private Link[] flowLinks;
        private HydropowerElevDef fromElevDef;
        private HydropowerElevDef toElevDef;
        private PowerEfficiencyCurve effCurve;
        private TimeSeries generatingHoursTS = new TimeSeries(TimeSeriesType.Generating_Hours);
        private double powerCap = 0.0;
        private bool peakGenerationOnly = false;

        // hydro unit power calculation
        private double flow;
        private double head;
        private double eff;
        private double power = 0.0;
        private double energy;
        private double[,] generatingHours = new double[0, 0];

        // hydro unit control variables
        private DateTime downtimeStart = TimeManager.missingDate;
        private TimeSpan downtime = HydropowerUnit.DefaultDownTime;
        public bool NoFlowDuringDowntime = true;
        private long[] origHi = new long[0], origLo = new long[0];

        // events for programmatic interaction
        public delegate void PrePowerCalculationDelegate();
        public event PrePowerCalculationDelegate PrePowerCalculation;
        public delegate void PostPowerCalculationDelegate();
        public event PostPowerCalculationDelegate PostPowerCalculation;

        #endregion
        #region Local Properties

        // General properties of the hydro unit
        /// <summary>Gets and sets the name of the hydro unit.</summary>
        public string Name
        {
            get { return this.name; }
            set
            {
                if (value == null) value = "";
                if (value.Contains(HydropowerUnit.ForbiddenStringInName))
                    throw new Exception("Cannot have " + HydropowerUnit.ForbiddenStringInName + " contained within the name of this hydropower unit.");
                if (this.id != -1)
                    if (this.model.PowerObjects.Exists(this.ModsimObjectType, this.id))
                        this.model.PowerObjects.SetName(this.ModsimObjectType, this.id, ref value);
                this.name = value;
            }
        }
        /// <summary>Gets and sets the description of this hydro unit.</summary>
        public string Description
        {
            get { return this.description; }
            set { if (value == null) value = ""; this.description = value; }
        }
        /// <summary>Gets the identification number of the hydro unit.</summary>
        public int ID
        {
            get { return this.id; }
        }
        /// <summary>Gets whether this hydro unit is in the hydropower controller.</summary>
        public bool IsInController
        {
            get { return this.id != -1; }
        }
        /// <summary>Gets the ModsimCollectionType of this instance.</summary>
        public ModsimCollectionType ModsimObjectType { get { return ModsimCollectionType.hydroUnit; } }
        /// <summary>Gets and sets whether this hydropower unit is a pump. If false, it is a turbine.</summary>
        public HydroUnitType Type
        {
            get { return this.type; }
            set { this.type = value; }
        }
        /// <summary>Gets all enumerations of type <c>HydroUnitType</c>.</summary>
        public static HydroUnitType[] Types
        {
            get { return (HydroUnitType[])Enum.GetValues(typeof(HydroUnitType)); }
        }
        /// <summary>Gets the enumeration type names for <c>HydroUnitType</c>.</summary>
        public static string[] TypeNames
        {
            get { return Array.ConvertAll(Types, element => element.ToString()); }
        }

        // Objects that define the various parts or calculations of the hydro unit
        /// <summary>Gets and sets the link that defines the flow through the turbine or pump.</summary>
        public Link[] FlowLinks
        {
            get { return this.flowLinks; }
            set { this.flowLinks = value; }
        }
        /// <summary>Gets and sets the flow links as a string separated by Link.ForbiddenStringInName.</summary>
        public string FlowLinksString
        {
            get { return Link.GetLinksAsString(this.FlowLinks); }
            set { this.FlowLinks = Link.GetLinksFromString(this.model, value); }
        }
        /// <summary>Gets and sets the efficiency curve associated with this instance.</summary>
        public PowerEfficiencyCurve EfficiencyCurve
        {
            get { return this.effCurve; }
            set { this.effCurve = value; }
        }
        /// <summary>Gets and sets the elevation definition at the intake to the pump or turbine.</summary>
        public HydropowerElevDef ElevDefnFrom
        {
            get { return this.fromElevDef; }
            set { this.fromElevDef = value; }
        }
        /// <summary>Gets and sets the elevation definition at the outlet of the pump or turbine.</summary>
        public HydropowerElevDef ElevDefnTo
        {
            get { return this.toElevDef; }
            set { this.toElevDef = value; }
        }
        /// <summary>Symbol that represents flow through the unit</summary>
        private Symbol q = "q";

        // Control variables
        /// <summary>Gets and sets a value that specifies the start of the downtime for the unit.</summary>
        public DateTime DowntimeStart
        {
            get { return this.downtimeStart; }
            set { this.downtimeStart = value; }
        }
        /// <summary>Gets and sets a value that specifies the length of downtime for a unit.</summary>
        public TimeSpan Downtime
        {
            get { return this.downtime; }
            set { this.downtime = value; }
        }
        /// <summary>Gets the factor of how much downtime there is for the current timestep.</summary>
        public double DowntimeFactor
        {
            get
            {
                if (this.downtimeStart.Equals(TimeManager.missingDate) || this.downtime == HydropowerUnit.DefaultDownTime)
                    return 0;

                // Define dates
                DateTime downStart = this.downtimeStart;
                DateTime downEnd = this.downtimeStart.Add(this.downtime);
                DateTime tsStart = this.model.mInfo.CurrentBegOfPeriodDate;
                DateTime tsEnd = this.model.mInfo.CurrentEndOfPeriodDate;

                // Exit early for dates that don't overlap
                if (downEnd < tsStart || tsEnd < downStart)
                    return 0;

                // Get the maximum starting date of downtime for the current timestep
                if (tsStart > downStart) downStart = tsStart;

                // Get the minimum ending date of downtime for the current timestep
                if (tsEnd < downEnd) downEnd = tsEnd;

                // Calculate factor
                TimeSpan downSpan = downEnd.Subtract(downStart);
                TimeSpan tsSpan = this.model.timeStep.ToTimeSpan(tsStart);
                return downSpan.TotalDays / tsSpan.TotalDays;
            }
        }

        // Properties that define the current state of the hydro unit
        /// <summary>Gets the elevation of the pump or turbine inflow.</summary>
        public double ElevationFrom
        {
            get { return this.fromElevDef.Elev; }
        }
        /// <summary>Gets the elevation of the pump or turbine outflow.</summary>
        public double ElevationTo
        {
            get { return this.toElevDef.Elev; }
        }
        /// <summary>Gets the function of head with respect to flow.</summary>
        public Symbol HeadFunction
        {
            get
            {
                Symbol from, to;
                Node resFrom = this.fromElevDef.Reservoir, resTo = this.toElevDef.Reservoir;
                double flow = this.model.FlowUnits.ConvertFrom(this.Flow, DefaultFlowUnits);
                from = (this.fromElevDef.Type == ElevType.Forebay) ? resFrom.ElevationFunction(flow, this.model.ScaleFactor, false) : resFrom.TailwaterElevationFunction(flow, this.model.ScaleFactor, this.model.mInfo.MonthIndex);
                to = (this.toElevDef.Type == ElevType.Forebay) ? resTo.ElevationFunction(flow, this.model.ScaleFactor, true) : resTo.TailwaterElevationFunction(flow, this.model.ScaleFactor, this.model.mInfo.MonthIndex);
                if (this.Type == HydroUnitType.Turbine)
                    return from - to;
                else
                    return to - from;
            }
        }
        /// <summary>Gets and sets the head across the turbine or pump. This value should only be manually changed by handling the <c>PrePowerCalculation</c> event, which occurs just before <c>Power</c> is calculated.</summary>
        public double Head
        {
            get { return this.head; }
            set { this.head = value; }
        }
        /// <summary>Gets the function of efficiency with respect to flow.</summary>
        public Symbol EfficiencyFunction
        {
            get
            {
                return Sym.FitPolynomial(this.effCurve.Flows, this.effCurve.GetEfficienciesAtHead(this.Head), q, HydropowerController.PolynomialTargetRsquared);
            }
        }
        /// <summary>Gets and sets the efficiency of the turbine or pump. This value should only be manually changed by handling the <c>PrePowerCalculation</c> event, which occurs just before <c>Power</c> is calculated.</summary>
        public double Efficiency
        {
            get { return this.eff; }
            set { this.eff = value; }
        }
        /// <summary>Gets and sets the flow through the turbine or pump. This value should only be manually changed by handling the <c>PrePowerCalculation</c> event, which occurs just before <c>Power</c> is calculated.</summary>
        public double Flow
        {
            get { return this.flow; }
            set { this.flow = value; }
        }
        /// <summary>Gets and sets the power generated (positive) or consumed (negative) by this instance.</summary>
        public double Power
        {
            get { return this.power; }
            set { this.power = value; this.energy = this.GetEnergy(); }
        }
        /// <summary>Gets the energy generated (positive) or consumed (negative) by this instance.</summary>
        public double Energy
        {
            get { return this.energy; }
        }
        /// <summary>Gets and sets the absolute power generating capacity of the hydropower unit.</summary>
        public double PowerCapacity
        {
            get { return this.powerCap; }
            set { this.powerCap = value; }
        }
        /// <summary>Gets the amount of energy that can be produced in the current timestep from this hydropower unit.</summary>
        public double EnergyCapacity
        {
            get
            {
                return this.model.PowerUnits.Integrate(this.PowerCapacity, 
                    this.model.mInfo.CurrentBegOfPeriodDate, 
                    this.model.mInfo.CurrentEndOfPeriodDate, 
                    this.model.EnergyUnits);
            }
        }
        /// <summary>Gets the energy that could potentially returned to the grid by this hydropower unit for the current timestep.</summary>
        public double UpReserveCapacity
        {
            get
            {
                if (this.Type == HydroUnitType.Pump)
                    return -this.Power; 
                else
                    return this.PowerCapacity - this.Power;
            }
        }
        /// <summary>Gets the energy that could potentially extracted from the grid by this hydropower unit for the current timestep.</summary>
        public double DownReserveCapacity
        {
            get
            {
                if (this.Type == HydroUnitType.Pump)
                    return this.PowerCapacity + this.Power; 
                else
                    return this.Power;
            }
        }
        /// <summary>Gets and sets whether this unit is used only for peak generation.</summary>
        public bool PeakGenerationOnly
        {
            get { return this.peakGenerationOnly; }
            set { this.peakGenerationOnly = value; }
        }
        /// <summary>Gets and sets the generating hours timeseries.</summary>
        public TimeSeries GeneratingHoursTS
        {
            get { return this.generatingHoursTS; }
            set { if (value != null) this.generatingHoursTS = value; else this.generatingHoursTS = new TimeSeries(TimeSeriesType.Generating_Hours); }
        }
        /// <summary>Gets and sets the generating hours for the current timestep.</summary>
        public double GeneratingHours
        {
            get
            {
                if (this.generatingHours != null && this.generatingHours.Length > 0)
                    return this.generatingHours[this.model.mInfo.CurrentModelTimeStepIndex, 0];
                else
                    return this.model.timeStep.ToTimeSpan(this.model.mInfo.CurrentBegOfPeriodDate).TotalHours;
            }
            set
            {
                if (this.generatingHours == null || this.generatingHours.Length == 0)
                    this.generatingHours = new double[this.model.TimeStepManager.noModelTimeSteps + this.model.TimeStepManager.noBackRAdditionalTSteps, 1];
                this.generatingHours[this.model.mInfo.CurrentModelTimeStepIndex, 0] = value;
            }
        }

        #endregion

        #region Constructors, and copying methods

        // Constructors
        /// <summary>Constructs an instance of the Hydropower class and defines the upper and lower elevations for the elevation head calculation.</summary>
        /// <param name="model">The model containing this hydropower unit.</param>
        /// <param name="name">The name of this hydropower unit.</param>
        /// <param name="flowLinks">The link that defines the flow moving through this hydropower unit.</param>
        /// <param name="FromElevDefn">The elevation head at the intake to the pump or turbine.</param>
        /// <param name="ToElevDefn">The elevation head at the outlet of the pump or turbine.</param>
        /// <param name="EfficiencyCurve">The efficiency curve associated with this hydropower unit.</param>
        /// <param name="type">Specifies the type of this hydropower unit (pump or turbine).</param>
        /// <param name="PowerCapacity">Specifies the power capacity of this hydropower unit.</param>
        /// <param name="peakGenerationOnly">Specifies whether this unit release flow only during generating hours.</param>
        /// <param name="generatingHours">Specifies the generating hours timeseries for this hydropower unit.</param>
        public HydropowerUnit(Model model, string name, Link[] flowLinks, HydropowerElevDef FromElevDefn, HydropowerElevDef ToElevDefn, PowerEfficiencyCurve EfficiencyCurve, HydroUnitType type, double PowerCapacity, TimeSeries generatingHoursTS, bool peakGenerationOnly)
        {
            this.name = name;
            this.flowLinks = flowLinks;
            this.flow = 0;
            this.fromElevDef = FromElevDefn;
            this.toElevDef = ToElevDefn;
            this.effCurve = EfficiencyCurve;
            this.Type = type;
            this.model = model;
            this.powerCap = PowerCapacity;
            this.peakGenerationOnly = peakGenerationOnly;
            TimeSeries ts = generatingHoursTS;
            if (ts == null)
                ts = new TimeSeries(TimeSeriesType.Generating_Hours);
            if (ts.getSize() == 0) 
            {
                double hours = 24 * this.model.timeStep.ToTimeSpan(this.model.TimeStepManager.startingDate).TotalDays;
                if (ts.IsFloatType) 
                    ts.setDataF(0, hours); 
                else 
                    ts.setDataL(0, (long)hours);
            }
            this.GeneratingHoursTS = ts;
            this.flow = 1.0;
            this.eff = 1.0;
            this.head = this.GetHead();
        }

        // Convert from reservoir unit...
        /// <summary>Converts a reservoir hydro unit to a <c>HydropowerUnit</c>.</summary>
        /// <param name="model">The MODSIM model housing both the reservoir node and this hydropower unit.</param>
        /// <param name="reservoir">The reservoir node with hydropower information.</param>
        /// <param name="flowLinks">The list of flow links that will define discharge from the hydropower unit.</param>
        public static HydropowerUnit FromReservoirUnit(Model model, Node reservoir, Link[] flowLinks)
        {
            if (reservoir == null)
                throw new NullReferenceException("Cannot create a hydropower unit from a null reservoir node reference.");
            if (flowLinks == null)
                throw new NullReferenceException("Cannot create a hydropower unit without the array of links that define discharge through the hydropower unit.");
            if (reservoir.m.ResEffCurve == null)
                reservoir.m.ResEffCurve = new PowerEfficiencyCurve(model);
            if (!reservoir.m.ResEffCurve.IsInController)
                reservoir.m.ResEffCurve.Name = model.PowerObjects.GetUniqueName(ModsimCollectionType.powerEff, reservoir.name + "_effCurve");
            return new HydropowerUnit(model, model.PowerObjects.GetUniqueName(ModsimCollectionType.hydroUnit, reservoir.name + "_hydroUnit"), flowLinks, new HydropowerElevDef(reservoir, ElevType.Forebay), new HydropowerElevDef(reservoir, ElevType.Tailwater), reservoir.m.ResEffCurve, HydroUnitType.Turbine, reservoir.m.powmax, reservoir.m.adaGeneratingHrsM, reservoir.m.peakGeneration);
        }

        // Copying methods
        /// <summary>Makes a new copy of this instance.</summary>
        /// <param name="NewModelRef">The new model reference</param>
        public HydropowerUnit Copy(Model NewModelRef)
        {
            HydropowerUnit retVal = (HydropowerUnit)this.MemberwiseClone();
            retVal.generatingHoursTS = this.generatingHoursTS.Copy();
            retVal.generatingHours = new double[this.generatingHours.GetLength(0), this.generatingHours.GetLength(1)];
            retVal.generatingHours = (double[,])this.generatingHours.Clone();
            retVal.model = NewModelRef;
            if (this.effCurve != null)
                retVal.effCurve = this.effCurve.Copy(NewModelRef);
            if (this.flowLinks != null)
                for (int i = 0; i < this.flowLinks.Length; i++)
                    retVal.flowLinks[i] = NewModelRef.FindLink(this.flowLinks[i].number);
            if (this.fromElevDef != null)
                retVal.fromElevDef = this.fromElevDef.Copy(NewModelRef);
            if (this.toElevDef != null)
                retVal.toElevDef = this.toElevDef.Copy(NewModelRef);
            return retVal;
        }

        #endregion

        #region Methods to help construct hydropower unit

        /// <summary>Gets the <c>HydroUnitType</c> from its name.</summary>
        /// <param name="TypeName">The name of a <c>HydroUnitType</c>.</param>
        public static HydroUnitType GetType(string TypeName)
        {
            try
            {
                if (Enum.IsDefined(typeof(HydroUnitType), TypeName))
                    return (HydroUnitType)Enum.Parse(typeof(HydroUnitType), TypeName);
            }
            catch
            { }
            return HydroUnitType.Turbine;
        }
        /// <summary>Retrieves an array of hydropower units named within a string separated by HydropowerUnit.ForbiddenStringInName.</summary>
        /// <param name="model">The model from which to retrieve the HydropowerUnits.</param>
        /// <param name="hydroUnitsString">The string that lists the names of HydropowerUnits.</param>
        public static HydropowerUnit[] GetHydroUnitsFromString(Model model, string hydroUnitsString)
        {
            string[] names = hydroUnitsString.Split(HydropowerUnit.ForbiddenStringInName.ToCharArray(), StringSplitOptions.None);
            List<HydropowerUnit> list = new List<HydropowerUnit>();
            HydropowerUnit h;
            foreach (string name in names)
                if ((h = model.hydro.GetHydroUnit(name)) != null)
                    list.Add(h);
            return list.ToArray();
        }
        /// <summary>Builds a string from HydropowerUnit names and separates them with HydropowerUnit.ForbiddenStringInName.</summary>
        /// <param name="hydroUnits">The array of HydropowerUnits to place in a string.</param>
        public static string GetHydroUnitsAsString(HydropowerUnit[] hydroUnits)
        {
            if (hydroUnits.Length == 0) return "";
            StringBuilder s = new StringBuilder();
            foreach (HydropowerUnit h in hydroUnits)
                if (h != null)
                    s.Append(h.Name + HydropowerUnit.ForbiddenStringInName);
            return s.ToString().TrimEnd(HydropowerUnit.ForbiddenStringInName.ToCharArray());
        }

        #endregion
        #region Methods to control hydropower unit

        /// <summary>Saves the upper and lower bounds on the links.</summary>
        private void SaveHiAndLo()
        {
            this.origHi = new long[this.flowLinks.Length];
            this.origLo = new long[this.flowLinks.Length];
            for (int i = 0; i < this.flowLinks.Length; i++)
            {
                this.origHi[i] = this.flowLinks[i].mlInfo.hi;
                this.origLo[i] = this.flowLinks[i].mlInfo.lo;
            }
        }
        /// <summary>Resets the upper and lower bounds on the links.</summary>
        private void ResetHiAndLo()
        {
            if (this.origHi.Length != 0)
                for (int i = 0; i < this.flowLinks.Length; i++)
                    this.flowLinks[i].mlInfo.hi = this.origHi[i];
            if (this.origLo.Length != 0)
                for (int i = 0; i < this.flowLinks.Length; i++)
                    this.flowLinks[i].mlInfo.lo = this.origLo[i];
            this.origHi = new long[0];
            this.origLo = new long[0];
        }
        /// <summary>Takes this hydro unit offline until the specified downtime is achieved or until <c>PutOnline()</c> is called.</summary>
        public void TakeOffline()
        {
            this.TakeOffline(this.model.TimeStepManager.endingDate.Subtract(this.model.mInfo.CurrentBegOfPeriodDate));
        }
        /// <summary>Takes this hydro unit offline until the specified downtime is achieved or until <c>PutOnline()</c> is called.</summary>
        public void TakeOffline(TimeSpan downtime)
        {
            this.downtimeStart = this.model.mInfo.CurrentBegOfPeriodDate;
            this.downtime = downtime;
            this.SaveHiAndLo();
            if (this.NoFlowDuringDowntime)
                foreach (Link l in this.flowLinks)
                    l.mlInfo.hi = l.mlInfo.lo = 0;
        }
        /// <summary>Puts this hydro unit online indefinitely until <c>TakeOffline()</c> is called.</summary>
        public void PutOnline()
        {
            this.downtimeStart = TimeManager.missingDate;
            this.downtime = new TimeSpan(0);
            this.ResetHiAndLo();
        }

        #endregion
        #region Methods to retrieve/update information

        // Retrieval methods
        /// <summary>Gets the current flow through <c>FlowLinks</c> in default flow units (cfs).</summary>
        private double GetFlow()
        {
            return this.model.FlowUnits.ConvertTo(SumFlows(this.flowLinks) / this.model.ScaleFactor, DefaultFlowUnits, this.model.mInfo.CurrentBegOfPeriodDate);
        }
        /// <summary>Calculates the elevation head between the upper and lower reservoirs defined by this hydropower unit.</summary>
        private double GetHead()
        {
            return this.model.LengthUnits.ConvertTo(Math.Abs(this.ElevationFrom - this.ElevationTo), DefaultHeadUnits);
        }
        /// <summary>Gets the efficiency from the power efficiency curve with the current flow and head.</summary>
        /// <remarks>Local variables <c>flow</c> and <c>head</c> must be defined before this is called.</remarks>
        private double GetEfficiency()
        {
            return this.effCurve.GetEfficiency(this.flow, DefaultFlowUnits, this.head, DefaultHeadUnits, false);
        }
        /// <summary>Calculates power generation (positive) or consumption (negative) for this hydropower unit.</summary>
        private double GetPower()
        {
            if (this.Type == HydroUnitType.Pump && this.ElevationFrom > this.ElevationTo
                || this.Type == HydroUnitType.Turbine && this.ElevationFrom < this.ElevationTo) return 0.0;
            return GetPower(this.flow, DefaultFlowUnits, this.head, DefaultHeadUnits, this.eff, this.powerCap, DefaultPowerUnits, this.Type, this.model.mInfo.CurrentBegOfPeriodDate, this.GeneratingHours, this.peakGenerationOnly, this.model.timeStep);
        }
        /// <summary>Calculates the energy produced (positive) or consumed (negative) from this hydropower unit.</summary>
        private double GetEnergy()
        {
            DateTime start = this.model.mInfo.CurrentBegOfPeriodDate;
            DateTime end = start.AddHours(this.GeneratingHours);
            return DefaultPowerUnits.Integrate(this.power, start, end, DefaultEnergyUnits) * (1 - this.DowntimeFactor);
        }

        // Update methods
        /// <summary>Updates the inputs for the RootFinder instance variable.</summary>
        public void Update()
        {
            // Set power parameters
            this.flow = this.GetFlow();
            this.head = this.GetHead();
            this.eff = this.GetEfficiency(); // flow and head must be defined before GetEfficiency is called.

            // Calculate power
            if (this.PrePowerCalculation != null) this.PrePowerCalculation();
            this.power = this.GetPower();
            this.energy = this.GetEnergy();
            if (this.PostPowerCalculation != null) this.PostPowerCalculation();
        }

        #endregion
        #region Methods - Control lists

        /// <summary>Checks to ensure that all timeseries within this hydropower unit has the correct types of units. Changes all units that are not of the correct type to the default units for that TimeSeries type.</summary>
        /// <returns>If all units are correct, returns a blank string. Otherwise, returns a string describing each timeseries that failed.</returns>
        public string CheckUnits()
        {
            if (this.generatingHoursTS.EnsureUnitsHaveSameType(this.model.TimeRateUnits))
                return "\n  HydroUnit: " + this.name + ". Generating Hours units are now " + this.model.TimeRateUnits;
            return "";
        }
        /// <summary>Converts units of non-timeseries data within this instance to those required for model simulation.</summary>
        public void ConvertNonTSUnits()
        {
            this.effCurve.ConvertUnits(this.model.FlowUnits, this.model.LengthUnits);
        }
        /// <summary>Converts the units for timeseries within this instance, i.e., generating hours.</summary>
        /// <param name="JustTest">Specifies whether to just test if the timeseries are filled or not.</param>
        public string ConvertAndFillTimeSeries(bool JustTest)
        {
            if (!JustTest)
                this.generatingHoursTS.FillTable(this.model, this.model.timeStep.ToTimeSpan(this.model.TimeStepManager.startingDate).TotalHours, this.model.TimeRateUnits, this.model.TimeRateUnits);
            else if (!this.generatingHoursTS.IsFilled(this.model))
                return "  Hydropower Unit: " + this.name + " in the generating hours table.\n";
            return "";
        }
        /// <summary>Loads the timeseries array data into the model object.</summary>
        public void LoadTimeseriesData()
        {
            int numts = this.model.TimeStepManager.noModelTimeSteps + this.model.TimeStepManager.noBackRAdditionalTSteps;
            if (this.generatingHoursTS.getSize() > 0)
            {
                this.generatingHours = new double[numts, 1];
                this.generatingHoursTS.HasNegativeValues();
                this.model.LoadTimeSeriesArray(this.generatingHoursTS, ref this.generatingHours);
            }
        }
        /// <summary>Extends the timeseries data for this instance.</summary>
        public void ExtendTimeseries()
        {
            if (this.model.TimeStepManager.noBackRAdditionalTSteps > 0)
            {
                int lastTimeStep = this.model.TimeStepManager.noModelTimeSteps;
                for (int j = lastTimeStep; j < lastTimeStep + this.model.TimeStepManager.noBackRAdditionalTSteps; j++)
                    if (this.model.TimeStepManager.Index2Date(j, TypeIndexes.ModelIndex) == TimeManager.missingDate)
                        if (this.generatingHours.Length > 0)
                            this.generatingHours[j, 0] = this.generatingHours[lastTimeStep - 1, 0];
            }
        }
        /// <summary>Initializes the timeseries data into double[,] arrays for the backrouting network.</summary>
        /// <param name="mi1">The original MODSIM model.</param>
        public void InitBackRoutTimeseries(Model mi1)
        {
            int numts = mi1.TimeStepManager.noModelTimeSteps + mi1.TimeStepManager.noBackRAdditionalTSteps;
            HydropowerUnit hydroUnit1 = mi1.hydro.GetHydroUnit(this.ID);
            if (hydroUnit1.generatingHours.Length > 0)
                this.generatingHours = new double[numts, 1];
        }

        // Lists
        /// <summary>Imports all field values and references except the ID field from sourceUnit to this instance.</summary>
        /// <param name="sourceUnit">The HydropowerUnit defining the fields to be imported.</param>
        public void ImportData(HydropowerUnit sourceUnit)
        {
            this.model = sourceUnit.model;
            this.Name = sourceUnit.Name;
            this.FlowLinks = sourceUnit.FlowLinks;
            this.fromElevDef = sourceUnit.fromElevDef;
            this.toElevDef = sourceUnit.toElevDef;
            this.effCurve = sourceUnit.effCurve;
            this.type = sourceUnit.type;
            this.PowerCapacity = sourceUnit.PowerCapacity;
            this.GeneratingHoursTS = sourceUnit.generatingHoursTS;
            this.peakGenerationOnly = sourceUnit.peakGenerationOnly;
        }
        /// <summary>Adds this hydropower unit to a list shared with the hydropower controller.</summary>
        public void AddToController()
        {
            if (this.id == -1)
                this.model.PowerObjects.Add(this.ModsimObjectType, this, ref this.name, out this.id);
            if (!this.effCurve.IsInController)
                this.effCurve.AddToController();
        }
        /// <summary>Removes this hydropower unit from a list shared with the hydropower controller.</summary>
        public void RemoveFromController()
        {
            this.model.PowerObjects.Remove(this.ModsimObjectType, this.id);
            this.id = -1;
        }
        /// <summary>Set the ID of this hydropower unit. Be VERY careful in setting this id.</summary>
        /// <param name="id">The ID</param>
        public void SetID(int id)
        {
            this.id = id;
        }

        #endregion
        #region Methods - IComparable interface

        /// <summary>Compares two HydropowerUnits according to their unique IDs.</summary>
        /// <param name="obj">The object to compare this HydropowerUnit with.</param>
        public int CompareTo(object obj)
        {
            // < 0, this is before obj 
            // = 0, this is in same position as obj
            // > 0, this is after obj
            if (obj == null) return 1;
            HydropowerUnit unit = obj as HydropowerUnit;
            if (unit != null)
                return this.ID.CompareTo(unit.ID);
            else
                throw new ArgumentException("When comparing two HydropowerUnits, need to specify a HydropowerUnit");
        }

        #endregion
        #region Static methods to calculate power

        // Static calculation methods
        public static long SumFlows(Link[] links)
        {
            long sum = 0;
            foreach (Link link in links)
                sum += link.mlInfo.flow;
            return sum;
        }
        ///// <summary>Gets the hydropower generated (turbine, positive) or consumed (pump, negative) associated with a flow and head value.</summary>
        ///// <param name="flow">The flow moving through the turbine.</param>
        ///// <param name="flowUnits">The units of flow.</param>
        ///// <param name="head">The head or energy potential behind the turbine.</param>
        ///// <param name="headUnits">The units of elevation.</param>
        ///// <param name="efficiency">The turbine efficiency at the particular flow and head value.</param>
        ///// <param name="powerCapacity">The maximum power that can be generated or consumed by the unit.</param>
        ///// <param name="toUnits">The units of energy rate (i.e., power) to convert to.</param>
        ///// <param name="type">Specifies which mode the hydropower unit using.</param>
        ///// <returns>Returns the calculated hydropower generated (turbine) or consumed (pump) given a head, flow, and efficiency.</returns>
        ///// <remarks>This does not allow for calculation of the specific weight of water according to temperature, which might be a good addition eventually.</remarks>
        //public static double GetPower(double flow, ModsimUnits flowUnits, double head, ModsimUnits headUnits, double efficiency, double powerCapacity, ModsimUnits toUnits, HydroUnitType type)
        //{
        //    if (efficiency <= 0.0) return 0.0;
        //    flow = Math.Abs(flow);
        //    head = Math.Abs(head);
        //    return Math.Min(powerCapacity, toUnits.ConvertFrom(flowUnits.ConvertTo(flow, DefaultFlowUnits) * headUnits.ConvertTo(head, DefaultHeadUnits) * SpecWeightH2O * kWperlbftsec * (type == HydroUnitType.Pump ? -1.0 / efficiency : efficiency), DefaultPowerUnits));
        //}

        /// <summary>Gets the hydropower generated (turbine, positive) or consumed (pump, negative) associated with a flow and head value.</summary>
        /// <param name="flow">The flow moving through the turbine.</param>
        /// <param name="flowUnits">The units of flow.</param>
        /// <param name="head">The head or energy potential behind the turbine.</param>
        /// <param name="headUnits">The units of elevation.</param>
        /// <param name="efficiency">The turbine efficiency at the particular flow and head value.</param>
        /// <param name="powerCapacity">The maximum power that can be generated or consumed by the unit.</param>
        /// <param name="toUnits">The units of energy rate (i.e., power) to convert to.</param>
        /// <param name="type">Specifies which mode the hydropower unit using.</param>
        /// <param name="date">The date at which the timestep length is calculated.</param>
        /// <returns>Returns the calculated hydropower generated (turbine) or consumed (pump) given a head, flow, and efficiency.</returns>
        /// <remarks>This does not allow for calculation of the specific weight of water according to temperature, which might be a good addition eventually.</remarks>
        public static double GetPower(double flow, ModsimUnits flowUnits, double head, ModsimUnits headUnits, double efficiency, double powerCapacity, ModsimUnits toUnits, HydroUnitType type, DateTime date)
        {
            if (efficiency <= 0.0) return 0.0;
            flow = Math.Abs(flow);
            head = Math.Abs(head);
            return Math.Min(powerCapacity, toUnits.ConvertFrom(flowUnits.ConvertTo(flow, DefaultFlowUnits, date) * headUnits.ConvertTo(head, DefaultHeadUnits) * SpecWeightH2O * kWperlbftsec * (type == HydroUnitType.Pump ? -1.0 / efficiency : efficiency), DefaultPowerUnits, date));
            //return toUnits.ConvertFrom(flowUnits.ConvertTo(flow, DefaultFlowUnits, date) * headUnits.ConvertTo(head, DefaultHeadUnits) * SpecWeightH2O * kWperlbftsec * (type == HydroUnitType.Pump ? -1.0 / efficiency : efficiency), DefaultPowerUnits, date);
        }
        /// <summary>Gets the hydropower generated (turbine, positive) or consumed (pump, negative) associated with a flow and head value.</summary>
        /// <param name="flow">The flow moving through the turbine.</param>
        /// <param name="flowUnits">The units of flow.</param>
        /// <param name="head">The head or energy potential behind the turbine.</param>
        /// <param name="headUnits">The units of elevation.</param>
        /// <param name="efficiency">The turbine efficiency at the particular flow and head value.</param>
        /// <param name="powerCapacity">The maximum power that can be generated or consumed by the unit.</param>
        /// <param name="toUnits">The units of energy rate (i.e., power) to convert to.</param>
        /// <param name="type">Specifies which mode the hydropower unit using.</param>
        /// <param name="date">The date at which the timestep length is calculated.</param>
        /// <param name="genHours">Specifies the number of generating hours that the unit is producing.</param>
        /// <param name="peakGenOnly">Specifies whether releases are made during peak generation only.</param>
        /// <param name="timeStep">Specifies the model timestep for the unit being calculated.</param>
        /// <returns>Returns the calculated hydropower generated (turbine) or consumed (pump) given a head, flow, and efficiency.</returns>
        /// <remarks>This does not allow for calculation of the specific weight of water according to temperature, which might be a good addition eventually.</remarks>
        public static double GetPower(double flow, ModsimUnits flowUnits, double head, ModsimUnits headUnits, double efficiency, double powerCapacity, ModsimUnits toUnits, HydroUnitType type, DateTime date, double genHours, bool peakGenOnly, ModsimTimeStep timeStep)
        {
            if (genHours == 0) return 0;
            if (peakGenOnly) flow *= timeStep.ToTimeSpan(date).TotalHours / genHours;
            return GetPower(flow, flowUnits, head, headUnits, efficiency, powerCapacity, toUnits, type, date);
        }

        #endregion
    }

    /// <summary>Defines elevation source for hydropower calculations.</summary>
    public class HydropowerElevDef
    {
        #region Local variables

        private ElevType type;
        private Node reservoir;
        private double elev; // This value should only be changed by custom code, because it overrides any other elevation definition if it's not null.
        private double minelev;
        private double maxelev;
        private static Symbol q = "q";

        #endregion
        #region Local properties

        // Properties
        /// <summary>Gets and sets the <c>ElevType</c> of this instance.</summary>
        public ElevType Type
        {
            get { return this.type; }
            set { this.type = value; }
        }
        /// <summary>Gets all the <c>ElevType</c> enumerations.</summary>
        public static ElevType[] Types
        {
            get { return (ElevType[])Enum.GetValues(typeof(ElevType)); }
        }
        /// <summary>Gets the enumeration type names for <c>ElevType</c>.</summary>
        public static string[] TypeNames
        {
            get { return Array.ConvertAll(Types, element => element.ToString()); }
        }
        /// <summary>Gets and sets the reservoir associated with this elevation definition.</summary>
        public Node Reservoir
        {
            get { return this.reservoir; }
            set { this.reservoir = value; }
        }
        /// <summary>Gets and sets the elevation from the reservoir or from user-defined elevation values.</summary>
        public double Elev
        {
            get
            {
                if (elev != double.MinValue) return elev; // Used if custom code has changed the elevation value

                switch (this.type)
                {
                    case ElevType.Forebay:
                        if (this.reservoir.mnInfo != null)
                            return this.reservoir.mnInfo.avg_elevation;
                        else
                            return GetElev(this.reservoir, this.reservoir.m.starting_volume);
                    case ElevType.Tailwater:
                        if (this.reservoir.mnInfo != null)
                        {
                            if (this.reservoir.m.twelevpts.Length != 0)
                                return this.reservoir.mnInfo.tail_elevation;
                            else
                                return this.reservoir.m.elev;
                        }
                        else if (this.reservoir.m.twelevpts.Length != 0)
                            return this.reservoir.m.twelevpts[0];
                        else
                            return this.reservoir.m.elev;
                    default:
                        throw new Exception("The specified ElevType is not defined: " + this.Type.ToString());
                }
            }
            set { this.elev = value; }
        }
        /// <summary>Gets the starting elevation from the reservoir.</summary>
        public double StartingElev
        {
            get
            {
                switch (this.type)
                {
                    case ElevType.Forebay:
                        if (this.reservoir.mnInfo != null)
                            return this.reservoir.mnInfo.starting_elevation;
                        else
                            return GetElev(this.reservoir, this.reservoir.m.starting_volume);
                    case ElevType.Tailwater:
                        if (this.reservoir.mnInfo != null)
                        {
                            if (this.reservoir.m.twelevpts.Length != 0)
                                return this.reservoir.mnInfo.tail_elevation;
                            else
                                return this.reservoir.m.elev;
                        }
                        else if (this.reservoir.m.twelevpts.Length != 0)
                            return this.reservoir.m.twelevpts[0];
                        else
                            return this.reservoir.m.elev;
                    default:
                        throw new Exception("The specified ElevType is not defined: " + this.Type.ToString());
                }
            }
            set { this.elev = value; }
        }

        #endregion

        #region Constructors

        // Constructors
        /// <summary>Constructs a new instance to define elevations from a reservoir or from user-defined elevation values.</summary>
        /// <param name="Reservoir">The reservoir at which elevations are defined.</param>
        /// <param name="UseTailWaterElev">Specifies whether to use tail water elevations for this definition. If false, the top of the reservoir will define the elevation.</param>
        public HydropowerElevDef(Node Reservoir, ElevType type)
        {
            this.type = type;
            this.reservoir = Reservoir;
            this.elev = double.MinValue;
            this.minelev = HydropowerElevDef.GetElev(Reservoir, Reservoir.m.min_volume);
            this.maxelev = HydropowerElevDef.GetElev(Reservoir, Reservoir.m.max_volume);
        }

        /// <summary>Makes a new copy of this instance.</summary>
        /// <param name="NewReservoirRef">Specifies a new reservoir node reference if desired.</param>
        /// <returns>Returns a the copied instance.</returns>
        public HydropowerElevDef Copy(Model newModelReference)
        {
            HydropowerElevDef retVal = (HydropowerElevDef)this.MemberwiseClone();
            if (retVal.reservoir != null)
                retVal.reservoir = newModelReference.FindNode(retVal.reservoir.number);
            return retVal;
        }

        #endregion

        #region Table lookup methods

        /// <summary>Gets the <c>ElevType</c> from its name.</summary>
        /// <param name="TypeName">The name of an <c>ElevType</c> value.</param>
        public static ElevType GetType(string TypeName)
        {
            try
            {
                if (Enum.IsDefined(typeof(ElevType), TypeName))
                    return (ElevType)Enum.Parse(typeof(ElevType), TypeName);
            }
            catch
            { }
            return ElevType.Forebay;
        }
        /// <summary>Calculates the number of points (must have at least one non-zero value) in the ACEH table.</summary>
        /// <param name="n">The node containing the ACEH table.</param>
        public static int NumACEHPoints(Node n)
        {
            if (n.m.cpoints == null) return 0;
            for (int i = 0; i < n.m.cpoints.Length; i++)
                if (n.m.cpoints[i] > 0)
                    return n.m.cpoints.Length;
            return 0;
        }
        /// <summary>Gets the reservoir capacities from the ACEH table found in Node n.</summary>
        public static double[] Capacities(Node n, double scaleFactor)
        {
            if (NumACEHPoints(n) == 0) return null;
            return Array.ConvertAll(n.m.cpoints, capacity => (double)capacity);
        }
        /// <summary>Gets the reservoir area values (as a function of reservoir volume) from the ACEH table found in Node n.</summary>
        public static double[] Areas(Node n)
        {
            if (NumACEHPoints(n) == 0) return null;
            return n.m.apoints;
        }
        /// <summary>Gets the reservoir elevations (as a function of reservoir volume) from the ACEH table found in Node n.</summary>
        public static double[] Elevations(Node n)
        {
            if (NumACEHPoints(n) == 0) return null;
            return n.m.epoints;
        }
        /// <summary>Gets the array of discharges values that are used to find tailwater elevation.</summary>
        public static double[] TailwaterDischarges(Node n)
        {
            if (n.m.twelevpts == null || n.m.twelevpts.Length == 0 || n.m.flowpts == null || n.m.flowpts.Length == 0)
                return new double[] { 0.0 };
            else
                return Array.ConvertAll(n.m.flowpts, flow => (double)flow);
        }
        /// <summary>Gets the array of tailwater elevation values (as a function of TailwaterDischarges).</summary>
        public static double[] TailwaterElevations(Node n)
        {
            if (n.m.twelevpts == null || n.m.twelevpts.Length == 0 || n.m.flowpts == null || n.m.flowpts.Length == 0)
                return new double[] { n.m.elev };
            else
                return n.m.twelevpts;
        }
        /// <summary>Gets the reservoir hydraulic capacity values (as a function of reservoir volume) from the ACEH table found in Node n.</summary>
        public static long[] HydraulicCapacities(Node n)
        {
            if (NumACEHPoints(n) == 0) return null;
            return n.m.hpoints;
        }
        public static void GetData(Node n, long volume, out double area, out double elev, out long hydCap)
        {
            Symbol e;
            GetData(n, volume, out area, out elev, out hydCap, out e);
        }
        /// <summary>Gets the area, elevation, and hydraulic capacity associated with a specified volume.</summary>
        /// <param name="volume">The model volume value (includes the scaling factor) to look up in the a/e/c table.</param>
        /// <param name="area">The area value associated with the specified volume value.</param>
        /// <param name="elev">The elevation value associated with the specified volume value.</param>
        /// <param name="hydCap">The hydraulic capacity value associated with the specified volume value.</param>
        /// <param name="n">The reservoir node for which the elevation is to be calculated.</param>
        public static void GetData(Node n, long volume, out double area, out double elev, out long hydCap, out Symbol ElevFxn)
        {
            long i;
            long isave = 1;
            bool found = false;
            double x1;
            double x2;
            double x3;
            double x4;
            double x5;
            double y1;
            double z1;
            double q1;
            long numPoints;
            ElevationFunctionType etype = (HydropowerController.ElevType == ElevationFunctionType.Polynomial && n.StageStorage == null) ? ElevationFunctionType.PiecewiseLinear : HydropowerController.ElevType;

            // Count the number of ACEH points
            numPoints = NumACEHPoints(n);

            /* STEP 01 - BASED ON RES VOL DETERMINE area AND HEAD */
            if (numPoints == 0)
            {
                hydCap = DefineConstants.NODATAVALUE; // 99999999; // open bounds
                area = 0.0;
                elev = 0.0;
                switch (etype)
                {
                    case ElevationFunctionType.Estimate:
                    case ElevationFunctionType.PiecewiseLinear:
                        ElevFxn = 0.0;
                        break;
                    case ElevationFunctionType.Polynomial:
                        ElevFxn = n.StageStorage;
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented hydropower controller elevation function type: " + HydropowerController.ElevType.ToString());
                }
                return;
            }
            if (numPoints == 1)
            {
                area = n.m.apoints[0];
                hydCap = n.m.hpoints[0];
                elev = n.m.epoints[0];
                switch (etype)
                {
                    case ElevationFunctionType.Estimate:
                    case ElevationFunctionType.PiecewiseLinear:
                        ElevFxn = elev;
                        break;
                    case ElevationFunctionType.Polynomial:
                        ElevFxn = n.StageStorage;
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented hydropower controller elevation function type: " + HydropowerController.ElevType.ToString());
                }
                return;
            }
            for (i = 0; (!found && (i < numPoints)); i++)
            {
                if (volume == n.m.cpoints[i])
                {
                    hydCap = n.m.hpoints[i];
                    area = n.m.apoints[i];
                    switch (etype)
                    {
                        case ElevationFunctionType.Estimate:
                            elev = n.m.epoints[i];
                            ElevFxn = elev;
                            break;
                        case ElevationFunctionType.PiecewiseLinear:
                            elev = n.m.epoints[i];
                            ElevFxn = GetCurrElevFunction(n, i, i);
                            break;
                        case ElevationFunctionType.Polynomial:
                            ElevFxn = n.StageStorage;
                            elev = n.StageStorage.Eval((double)volume);
                            break;
                        default:
                            throw new NotImplementedException("Unimplemented hydropower controller elevation function type: " + HydropowerController.ElevType.ToString());
                    }
                    return;
                }
                else if (volume < n.m.cpoints[i])
                {
                    isave = i;
                    found = true;
                }
            }

            /* STEP 02
            ** IF VOL BETWEEN POINTS INTERPOLATE
            ** FOR area AND HEAD
            */

            if (isave == 0 && found)
            {
                area = n.m.apoints[0];
                switch (etype)
                {
                    case ElevationFunctionType.Estimate:
                    case ElevationFunctionType.PiecewiseLinear:
                        elev = n.m.epoints[0];
                        ElevFxn = elev;
                        break;
                    case ElevationFunctionType.Polynomial:
                        ElevFxn = n.StageStorage;
                        elev = n.StageStorage.Eval((double)volume);
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented hydropower controller elevation function type: " + HydropowerController.ElevType.ToString());
                }
                hydCap = n.m.hpoints[0];
                return;
            }

            if (i == numPoints && !found)
            {
                area = n.m.apoints[numPoints - 1];
                switch (etype)
                {
                    case ElevationFunctionType.Estimate:
                    case ElevationFunctionType.PiecewiseLinear:
                        elev = n.m.epoints[numPoints - 1];
                        ElevFxn = elev;
                        break;
                    case ElevationFunctionType.Polynomial:
                        elev = n.StageStorage.Eval((double)volume);
                        ElevFxn = n.StageStorage;
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented hydropower controller elevation function type: " + HydropowerController.ElevType.ToString());
                }
                hydCap = n.m.hpoints[numPoints - 1];
                return;
            }

            x1 = (double)(n.m.cpoints[isave] - n.m.cpoints[isave - 1]);
            y1 = (double)(n.m.apoints[isave] - n.m.apoints[isave - 1]);
            z1 = (double)(n.m.epoints[isave] - n.m.epoints[isave - 1]);
            q1 = (double)(n.m.hpoints[isave] - n.m.hpoints[isave - 1]);
            x2 = (double)(volume - n.m.cpoints[isave - 1]);
            if (x1 != 0.0)
                x3 = (x2 / x1) * y1;
            else
                x3 = 0.0;
            area = n.m.apoints[isave - 1] + (long)(x3 + DefineConstants.ROFF);
            switch (etype)
            {
                case ElevationFunctionType.Estimate:
                    if (x1 != 0.0)
                        x4 = (x2 / x1) * z1;
                    else
                        x4 = 0.0;
                    elev = n.m.epoints[isave - 1] + x4;
                    ElevFxn = elev;
                    break;
                case ElevationFunctionType.PiecewiseLinear:
                    ElevFxn = GetCurrElevFunction(n, isave - 1, isave);
                    if (x1 != 0.0)
                        x4 = (x2 / x1) * z1;
                    else
                        x4 = 0.0;
                    elev = n.m.epoints[isave - 1] + x4;
                    break;
                case ElevationFunctionType.Polynomial:
                    ElevFxn = n.StageStorage;
                    elev = n.StageStorage.Eval(volume);
                    break;
                default:
                    throw new NotImplementedException("Unimplemented hydropower controller elevation function type: " + HydropowerController.ElevType.ToString());
            }
            if (x1 != 0.0)
                x5 = (x2 / x1) * q1;
            else
                x5 = 0.0;
            hydCap = n.m.hpoints[isave - 1] + (long)(x5 + DefineConstants.ROFF);
        }
        /// <summary>Gets the elevation from the reservoir area/elevation/capacity table.</summary>
        /// <param name="n">The reservoir node for which the elevation is to be calculated.</param>
        /// <param name="volume">The volume value to look up in the a/e/c table.</param>
        /// <returns>Returns the elevation that corresponds with the specified volume.</returns>
        public static double GetElev(Node n, long volume)
        {
            Symbol s;
            return GetElev(n, volume, out s);
        }
        /// <summary>Gets the elevation from the reservoir area/elevation/capacity table.</summary>
        /// <param name="n">The reservoir node for which the elevation is to be calculated.</param>
        /// <param name="volume">The volume value to look up in the a/e/c table.</param>
        /// <returns>Returns the elevation that corresponds with the specified volume.</returns>
        public static double GetElev(Node n, long volume, out Symbol ElevFxn)
        {
            double elev;
            if (HydropowerController.ElevType == ElevationFunctionType.Polynomial && n.StageStorage != null)
            {
                ElevFxn = n.StageStorage;
                elev = ElevFxn.Eval((double)volume);
            }
            else
            {
                long lng;
                double dbl;
                GetData(n, volume, out dbl, out elev, out lng, out ElevFxn);
            }
            return elev;
        }
        /// <summary>Gets the elevation of the tailwater for a specified node.</summary>
        /// <param name="n">The reservoir node for which the elevation is to be calculated.</param>
        /// <param name="flow">The flow value in Model.FlowUnits (not scaled).</param>
        /// <returns>Returns the elevation of the tail water that corresponds with the specified volume.</returns>
        public static double GetElev_TailWater(Node n, long flow)
        {
            Symbol s;
            return GetElev_TailWater(n, flow, out s);
        }
        /// <summary>Gets the elevation of the tailwater for a specified node.</summary>
        /// <param name="n">The reservoir node for which the elevation is to be calculated.</param>
        /// <param name="flow">The flow value in Model.FlowUnits (not scaled).</param>
        /// <returns>Returns the elevation of the tail water that corresponds with the specified volume.</returns>
        public static double GetElev_TailWater(Node n, long flow, out Symbol TWElevFxn)
        {
            if (HydropowerController.ElevType == ElevationFunctionType.Polynomial && n.TWElev != null)
            {
                TWElevFxn = n.TWElev;
                return TWElevFxn.Eval((double)flow);
            }

            int i;
            double x1;
            double x2;
            double z1;
            int numPoints;

            // Get the number of points (if they're all <= zero, then return 0) 
            numPoints = 0;
            for (i = 0; i < n.m.flowpts.Length; i++)
                if (n.m.flowpts[i] > 0)
                {
                    numPoints = n.m.flowpts.Length;
                    break;
                }
            if (numPoints == 0)
            {
                TWElevFxn = 0.0;
                return 0.0;
            }
            if (numPoints == 1 || flow <= n.m.flowpts[0])
            {
                TWElevFxn = n.m.twelevpts[0];
                return n.m.twelevpts[0];
            }

            // Find the first flow value in the array less than the one we're searching for
            for (i = 1; i < numPoints; i++)
            {
                if (flow == n.m.flowpts[i])
                {
                    TWElevFxn = n.m.twelevpts[i];
                    return n.m.twelevpts[i];
                }
                else if (n.m.flowpts[i - 1] < flow && flow < n.m.flowpts[i])
                    break;
            }

            // Exit at the end
            if (i == numPoints)
            {
                TWElevFxn = n.m.twelevpts[i - 1];
                return n.m.twelevpts[i - 1];
            }

            // Interpolate between elevation points based on specified flow
            x1 = (double)(n.m.flowpts[i] - n.m.flowpts[i - 1]);
            x2 = (double)(flow - n.m.flowpts[i - 1]);
            z1 = (double)(n.m.twelevpts[i] - n.m.twelevpts[i - 1]);
            if (x1 == 0.0)
            {
                TWElevFxn = n.m.twelevpts[i - 1];
                return n.m.twelevpts[i - 1];
            }
            else
            {
                double tw_elev = z1 * x2 / x1 + n.m.twelevpts[i - 1];
                if (HydropowerController.ElevType == ElevationFunctionType.Estimate)
                    TWElevFxn = tw_elev;
                else
                    TWElevFxn = z1 * (q - n.m.flowpts[i - 1]) / x1 + n.m.twelevpts[i - 1];
                return tw_elev;
            }
        }
        /// <summary>Gets a linear function with respect to volume at </summary>
        /// <param name="n">The node at which the elevation is being calculated</param>
        /// <param name="left">The 'left' index within the array of elevation points that the volume estimate falls within.</param>
        /// <param name="right">The 'right' index within the array of elevation points that the volume estimate falls within.</param>
        /// <param name="Vest">The estimate of the volume at a particular timestep.</param>
        public static Symbol GetCurrElevFunction(Node n, long left, long right)
        {
            Symbol ElevFxn;
            Symbol V = 'V';
            long middle = -1;
            if (left == right)
            {
                if (left == 0)
                    right = 1;
                else if (right == n.m.epoints.Length - 1)
                    left = n.m.epoints.Length - 2;
                else
                {
                    middle = left;
                    left--;
                    right++;
                }
            }
            if (middle == -1)
                ElevFxn = (n.m.epoints[right] - n.m.epoints[left]) / (n.m.cpoints[right] - n.m.cpoints[left]) * (V - n.m.cpoints[left]) + n.m.epoints[left];
            else
                ElevFxn = Sym.FitPolynomial(new double[] { n.m.cpoints[left], n.m.cpoints[middle], n.m.cpoints[right] }, new double[] { n.m.epoints[left], n.m.epoints[middle], n.m.epoints[right] }, V, (int)2);
            return ElevFxn;
        }

        #endregion
    }

}
