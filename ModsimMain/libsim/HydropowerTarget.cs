using System;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{

    /// <summary>Controls hydropower target calculations.</summary>
    public class HydropowerTarget : IModsimObject, IComparable
    {
        #region Static variables

        /// <summary>The command name used in the xy file.</summary>
        public static string XYCmdName { get { return "hydroDem"; } }
        private static double tolerance = 0.1;
        public static double Tolerance { get { return tolerance; } set { tolerance = value; } }
        public static double stepFraction = 0.1;

        #endregion
        #region Local variables

        // Current state variables
        /// <summary>Specifies the model associated with this instance in order to incorporate timestep information into calculations.</summary>
        private Model model;
        private string name = "";
        private string description = "";
        private int id = -1;
        private List<double> prevEnergy = new List<double>();
        private List<double> prevFlow = new List<double>();
        private List<double> prevControlFlow = new List<double>();
        private object theLock = new object();
        private ModsimUnits _MW = new ModsimUnits(EnergyUnitsType.MJ, ModsimTimeStepType.Seconds); 

        // Variables / objects used for estimating new demand
        /// <summary>Specifies whether the power demand has been matched by the power exported.</summary>
        private bool isConverged = false;

        // Values specified by the user prior to simulation (although they can be changed during simulation)
        /// <summary>The list of turbines and pumps that contribute to the power generation and consumption respectively.</summary>
        private HydropowerUnit[] hydroUnits;
        /// <summary>Specifies the power demands before simulation.</summary>
        private TimeSeries powerTargetsTS = new TimeSeries(TimeSeriesType.Power_Target);

        // Values that can be changed during simulation.
        /// <summary>Specifies the power demands during simulation.</summary>
        public double[,] EnergyTargets = new double[0, 0];
        /// <summary>Specifies the power demand at the current timestep.</summary>
        private double energyDemand;
        /// <summary>Specifies the energy produced by this instance.</summary>
        private double energy;
        /// <summary>Specifies the difference between the power generated and power demand (Power - PowerTarget).</summary>
        private double energyDiff;
        /// <summary>Specifies the flow through all hydropower units that attempt to match this hydropower target.</summary>
        private double flow;

        #endregion
        #region Properties

        // ID and name
        /// <summary>Gets and sets a value specifying this hydropower target's name.</summary>
        public string Name
        {
            get { return this.name; }
            set
            {
                if (value == null) value = "";
                if (this.id != -1)
                    if (this.model.PowerObjects.Exists(this.ModsimObjectType, this.id))
                        this.model.PowerObjects.SetName(this.ModsimObjectType, this.id, ref value);
                this.name = value;
            }
        }
        /// <summary>Gets and sets a value specifying a description for this hydropower target.</summary>
        public string Description
        {
            get { return this.description; }
            set { if (value == null) value = ""; this.description = value; }
        }
        /// <summary>Gets a value specifying this hydropower target's identification number.</summary>
        public int ID
        {
            get { return this.id; }
        }
        /// <summary>Gets whether this hydro target is in the hydropower controller.</summary>
        public bool IsInController
        {
            get { return this.id != -1; }
        }
        /// <summary>Gets the ModsimCollectionType of this instance.</summary>
        public ModsimCollectionType ModsimObjectType { get { return ModsimCollectionType.hydroTarget; } }

        // User-specified values prior to simulation
        /// <summary>Gets all links associated with the hydropower units that define this hydropower target.</summary>
        public Link[] FlowLinks
        {
            get
            {
                List<Link> list = new List<Link>();
                foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                    foreach (Link l in hydroUnit.FlowLinks)
                        if (!list.Contains(l))
                            list.Add(l);
                return list.ToArray();
            }
        }
        /// <summary>Gets the list of hydropower units associated with this hydropower target.</summary>
        public HydropowerUnit[] HydroUnits
        {
            get { return this.hydroUnits; }
            set
            {
                List<HydropowerUnit> hunits = new List<HydropowerUnit>(value);
                for (int i = 0; i < value.Length; i++)
                    if (value[i] == null)
                        hunits.RemoveAt(i);
                this.hydroUnits = hunits.ToArray();
            }
        }
        /// <summary>Gets and sets the HydropowerUnits within this instance represented in a string by their unique names and separated by HydropowerUnit.ForbiddenStringInName.</summary>
        public string HydroUnitsString
        {
            get { return HydropowerUnit.GetHydroUnitsAsString(this.HydroUnits); }
            set { this.hydroUnits = HydropowerUnit.GetHydroUnitsFromString(this.model, value); }
        }
        /// <summary>Gets and sets a value specifying the power demands before simulation.</summary>
        public TimeSeries PowerTargetsTS
        {
            get { return this.powerTargetsTS; }
            set
            {
                if (value == null) value = new TimeSeries(TimeSeriesType.Power_Target);
                this.powerTargetsTS = value;
            }
        }

        // Values during simulation
        public double TimestepHours { get { return model.timeStep.ToTimeSpan(model.mInfo.CurrentBegOfPeriodDate).TotalHours; } }
        /// <summary>Gets and sets a value specifying the power demand at the current timestep.</summary>
        public double EnergyTarget
        {
            get { return this.energyDemand; }
            set 
            { 
                this.energyDemand = value; 
                if (this.EnergyTargets.Length == 0) 
                    return;
                this.EnergyTargets[this.model.mInfo.CurrentModelTimeStepIndex, 0] = value; 
            }
        }
        /// <summary>Gets and sets a value specifying the energy produced by this instance. If negative, the hydropower units are consuming more energy than they are producing.</summary>
        public double Energy
        {
            get { return this.energy; }
            set { this.energy = value; }
        }
        /// <summary>Gets and sets a value specifying the power generated (+) and/or consumed (-) at the current timestep from all hydro units.</summary>
        public double PowerAvg
        {
            get { return HydropowerUnit.DefaultEnergyUnits.ToRate(this.energy, this.model.mInfo.CurrentBegOfPeriodDate, this.model.mInfo.CurrentEndOfPeriodDate, HydropowerUnit.DefaultPowerUnits); }
            set { this.energy = HydropowerUnit.DefaultPowerUnits.Integrate(value, this.model.mInfo.CurrentBegOfPeriodDate, this.model.mInfo.CurrentEndOfPeriodDate, HydropowerUnit.DefaultEnergyUnits); }
        }
        /// <summary>Gets a value specifying the difference between the power generated and power demand (Power - PowerTarget).</summary>
        public double EnergyDiff
        {
            get { return this.energyDiff; }
        }
        /// <summary>Gets a value specifying the flow through all hydropower units that attempt to match this hydropower target.</summary>
        public double Flow
        {
            get { return this.flow; }
        }
        /// <summary>Sums the total power capacity (absolute value) for all units defining this hydropower target.</summary>
        public double PowerCapacity
        {
            get
            {
                double sum = 0.0;
                foreach (HydropowerUnit unit in this.HydroUnits)
                    sum += unit.PowerCapacity;
                return sum;
            }
        }
        /// <summary>Sums the total power capacity in MW (absolute value) for all units defining this hydropower target.</summary>
        public double PowerCapacityMW
        {
            get
            {
                return this.model.PowerUnits.ConvertTo(this.PowerCapacity, _MW); 
            }
        }
        /// <summary>Sums the total energy capacity (absolute value) within the current timestep for all units defining this hydropower target.</summary>
        public double EnergyCapacity
        {
            get
            {
                return PowerCapacity * TimestepHours;
            }
        }
        /// <summary>Sums the total energy capacity in MWh (absolute value) within the current timestep for all units defining this hydropower target.</summary>
        public double EnergyCapacityMWh
        {
            get
            {
                return PowerCapacityMW * TimestepHours;
            }
        }
        /// <summary>Sums the up-reserve capacity for all the units defining this hydropower target.</summary>
        public double UpReserveCapacity
        {
            get
            {
                double sum = 0.0;
                foreach (HydropowerUnit unit in this.HydroUnits)
                    sum += unit.UpReserveCapacity;
                return sum;
            }
        }
        public double UpReserveCapacityMW
        {
            get
            {
                return this.model.PowerUnits.ConvertTo(this.UpReserveCapacity, _MW);
            }
        }
        /// <summary>Sums the down-reserve capacity for all the units defining this hydropower target.</summary>
        public double DownReserveCapacity
        {
            get
            {
                double sum = 0.0;
                foreach (HydropowerUnit unit in this.HydroUnits)
                    sum += unit.DownReserveCapacity;
                return sum;
            }
        }
        public double DownReserveCapacityMW
        {
            get
            {
                return this.model.PowerUnits.ConvertTo(this.DownReserveCapacity, _MW);
            }
        }
        /// <summary>Gets whether this hydropower target is defined only by pumping hydropower units</summary>
        public bool HasOnlyPumps
        {
            get
            {
                foreach (HydropowerUnit unit in this.HydroUnits)
                    if (unit.Type == HydroUnitType.Turbine)
                        return false;
                return true;
            }
        }
        /// <summary>Gets whether this hydropower target is defined only by turbine hydropower units</summary>
        public bool HasOnlyTurbines
        {
            get
            {
                foreach (HydropowerUnit unit in this.HydroUnits)
                    if (unit.Type == HydroUnitType.Pump)
                        return false;
                return true;
            }
        }

        // Solution state
        /// <summary>Gets whether the power demand has converged.</summary>
        public bool IsConverged
        {
            get
            {
                return this.isConverged;
            }
        }

        #endregion

        #region Constructors and copying methods

        /// <summary>Constructs an instance of hydropower target controller by specifying the model and method used to match a power demand.</summary>
        /// <param name="model">The MODSIM model object that hold simulation variables.</param>
        /// <param name="hydroUnits">The hydropower units that will be used to </param>
        public HydropowerTarget(Model model, string name, TimeSeries powerDemands, HydropowerUnit[] hydroUnits)
        {
            this.model = model;
            this.name = name;
            TimeSeries ts;
            if (powerDemands != null)
                ts = powerDemands;
            else
                ts = new TimeSeries(TimeSeriesType.Power_Target);
            this.PowerTargetsTS = ts;
            if (ts.getSize() == 0)
            {
                double hours = 24 * this.model.timeStep.ToTimeSpan(this.model.TimeStepManager.startingDate).TotalDays;
                if (ts.IsFloatType)
                    ts.setDataF(0, hours);
                else
                    ts.setDataL(0, (long)hours);
            }
            this.HydroUnits = hydroUnits;
            this.Update();
        }

        /// <summary>Performs a deep copies of this instance.</summary>
        /// <param name="newModelReference">The new MODSIM model reference.</param>
        /// <remarks>MUST copy HydropowerUnit classes first before calling this copy method!</remarks>
        public HydropowerTarget Copy(Model newModelReference)
        {
            HydropowerTarget retVal = (HydropowerTarget)this.MemberwiseClone();
            retVal.model = newModelReference;

            // power demands
            retVal.PowerTargetsTS = this.PowerTargetsTS.Copy();
            for (int i = 0; i < this.hydroUnits.Length; i++)
                retVal.hydroUnits[i] = newModelReference.hydro.GetHydroUnit(this.hydroUnits[i].ID);

            // array lists
            double[] a = new double[this.prevEnergy.Count];
            this.prevEnergy.CopyTo(a);
            retVal.prevEnergy = new List<double>(a);
            this.prevFlow.CopyTo(a);
            retVal.prevFlow = new List<double>(a);
            this.prevControlFlow.CopyTo(a);
            retVal.prevControlFlow = new List<double>(a);

            // return
            return retVal;
        }


        #endregion

        #region Methods - Retrieve energy info

        /// <summary>Calculates all the energy produced from all hydropower units. If negative, more energy is being consumed than produced.</summary>
        private double GetEnergyTotal()
        {
            return this.GetEnergyGenerated() - this.GetEnergyConsumed();
        }
        /// <summary>Gets the energy generated by all turbine units. Always a positive number.</summary>
        private double GetEnergyGenerated()
        {
            double sum = 0;
            foreach (HydropowerUnit hydroUnit in this.hydroUnits)
                if (hydroUnit.Energy > 0)
                    sum += hydroUnit.Energy;
            return sum;
        }
        /// <summary>Gets the energy consumed by all pumping units. Always a positive number.</summary>
        private double GetEnergyConsumed()
        {
            double sum = 0;
            foreach (HydropowerUnit hydroUnit in this.hydroUnits)
                if (hydroUnit.Energy < 0)
                    sum -= hydroUnit.Energy;
            return sum;
        }
        /// <summary>Gets the energy demand at the current timestep and hydrologic state.</summary>
        private double GetEnergyTarget()
        {
            if (this.EnergyTargets.Length == 0) return 0;
            return this.model.EnergyUnits.ConvertTo(this.EnergyTargets[this.model.mInfo.CurrentModelTimeStepIndex, 0], HydropowerUnit.DefaultEnergyUnits);
        }

        #endregion
        #region Methods - Retrieve water info

        /// <summary>Sums discharge through all turbines.</summary>
        private double GetFlow_Turbine()
        {
            double sum = 0;
            foreach (HydropowerUnit hydroUnit in hydroUnits)
                if (hydroUnit.Type == HydroUnitType.Turbine)
                    sum += hydroUnit.Flow;
            return sum;
        }
        /// <summary>Sums discharge through all pumps.</summary>
        private double GetFlow_Pump()
        {
            double sum = 0;
            foreach (HydropowerUnit hydroUnit in hydroUnits)
                if (hydroUnit.Type == HydroUnitType.Pump)
                    sum += hydroUnit.Flow;
            return sum;
        }

        #endregion
        #region Methods - Estimate water demand to meet power demand

        private double sumFlow(HydropowerUnit[] units)
        {
            double sum = 0;
            foreach (HydropowerUnit unit in units)
                sum += unit.Flow;
            return sum;
        }

        /// <summary>Stores previous power production and water demands from previous iterations.</summary>
        private void StorePrevious(double sumControlFlow)
        {
            // Store previous iteration data (water demand and generated power)
            if (this.model.mInfo == null) return;
            if (this.model.mInfo.Iteration == 0)
            {
                this.prevEnergy.Clear();
                this.prevFlow.Clear();
                this.prevControlFlow.Clear();
            }
            this.prevEnergy.Add(this.EnergyDiff);
            this.prevFlow.Add(sumFlow(this.HydroUnits));
            this.prevControlFlow.Add(sumControlFlow);
            if (this.prevEnergy.Count > this.model.hydro.NumPrevIters)
            {
                this.prevEnergy.RemoveAt(0);
                this.prevFlow.RemoveAt(0);
                this.prevControlFlow.RemoveAt(0);
            }
        }

        /// <summary>Updates the hydropower control link at each iteration</summary>
        private void UpdateControl(Link hlink, Link hcontrol)
        {
            IterativeSolutionTechnique tech = this.model.hydro.IterativeTechnique;
            if (tech == IterativeSolutionTechnique.Nothing)
                return;
            tech = (this.model.mInfo.Iteration > 3 && tech == IterativeSolutionTechnique.SteepestGradient) 
                ? tech : IterativeSolutionTechnique.SuccApprox;
            switch (tech)
            {
                case IterativeSolutionTechnique.SteepestGradient:
                    int num = this.prevEnergy.Count - 1;
                    double currF = Math.Pow(this.energyDiff, 2);
                    double prevF = Math.Pow(this.prevEnergy[num], 2);
                    if (this.flow != this.prevFlow[num])
                    {
                        double diffF = (currF - prevF) / (this.flow - this.prevFlow[num]);
                        hcontrol.mlInfo.hi = hcontrol.mlInfo.hi - Convert.ToInt64(1 / (1.0 + stepFraction * this.model.mInfo.Iteration) * diffF);
                    }
                    break;
                case IterativeSolutionTechnique.SuccApprox:
                    double factor = 1;// model.mInfo.Iteration < 10 ? 1-(10.00 - model.mInfo.Iteration)/10.00 : 1;
                    if (this.energy != 0.0)
                        if (this.energyDemand * this.energy < 0)
                            hcontrol.mlInfo.hi = 0;
                        else
                            hcontrol.mlInfo.hi = (long)Math.Round(this.energyDemand / this.energy * (double) hlink.mlInfo.flow * factor, 0);  //hlink.mlInfo.flow),0);
                    else if (this.energyDemand == 0.0)
                        hcontrol.mlInfo.hi = 0;
                    else
                        hcontrol.mlInfo.hi = hlink.mlInfo.hi;
                    break;
                case IterativeSolutionTechnique.SeparateHydroLinks:
                    hcontrol.mlInfo.cost = 0;
                    hlink.mlInfo.hydroAdditional.mlInfo.cost = 0;
                    break;
                default:
                    break;
            }

        }

        /// <summary>Updates the total power targets and generation for the current timestep.</summary>
        public void Update()
        {
            lock (this.theLock)
            {
                if (this.model.mInfo == null) return;

                // Timestep indices
                this.energyDemand = this.GetEnergyTarget();
                this.energy = this.GetEnergyTotal();
                this.energyDiff = this.Energy - this.EnergyTarget;
                this.flow = this.sumFlow(this.HydroUnits);

                // iterative convergence to hydropower target if it is easy enough
                Link hadd, hcontrol, hinflow;
                string dbgTxt = "";
                if (this.model.hydro.IterativeTechnique != IterativeSolutionTechnique.Nothing)
                {
                    double sumControlFlow = 0;
                    foreach (HydropowerUnit unit in this.hydroUnits)
                        foreach (Link link in unit.FlowLinks)
                        {
                            hadd = link.mlInfo.hydroAdditional;
                            hcontrol = link.mlInfo.hydroControl;
                            hinflow = link.mlInfo.hydroInflow;
                            dbgTxt = "               control hi:" + hcontrol.mlInfo.hi + "  flow:" + hcontrol.mlInfo.flow + "  Outflow " + link.mlInfo.flow;
                            if (this.model.mInfo.Iteration == 0)
                            {
                                // find an initial starting point
                                if (this.model.mInfo.CurrentModelTimeStepIndex == 0)
                                {
                                    hinflow.mlInfo.hi = hinflow.mlInfo.lo = 0;
                                    hcontrol.mlInfo.hi = hcontrol.mlInfo.flow;//1;
                                }
                            }
                            else
                            {
                                // update downstream inflow with previous flow 
                                hinflow.mlInfo.hi = hinflow.mlInfo.lo = (hcontrol.mlInfo.flow
                                    + hadd.mlInfo.flow
                                    + HydropowerUnit.SumFlows(link.mlInfo.hydroSpillLinks));

                                // update hi bound on hcontrol link 
                                UpdateControl(link, hcontrol);
                            }
                            sumControlFlow += hcontrol.mlInfo.flow;
                            dbgTxt += "\n               control new hi:" + hcontrol.mlInfo.hi;
                        }

                    // Update previous information
                    this.StorePrevious(sumControlFlow);

                    // Convergence checks
                    this.isConverged = false; // Math.Abs(this.energyDiff) <= Tolerance;
                    

                    if (this.prevEnergy.Count >= this.model.hydro.NumPrevIters && this.model.hydro.NumPrevIters > 1) // Check previous timesteps for convergence
                    {
                        this.isConverged = true;
                        for (int i =1; i < this.model.hydro.NumPrevIters && this.isConverged; i++)
                            if (Math.Abs(this.prevEnergy[i - 1] - this.prevEnergy[i]) > HydropowerTarget.Tolerance)
                                this.isConverged = false;
                            else if (Math.Abs(this.prevFlow[i - 1] - this.prevFlow[i]) > 0) //HydropowerTarget.Tolerance)
                                this.isConverged = false;
                            else if (Math.Abs(this.prevControlFlow[i - 1] - this.prevControlFlow[i]) > 0) 
                                this.isConverged = false;

                        // Set the convergence if previous timesteps have converged.
                        //if (allConverged)
                        //    this.isConverged = allConverged;
                    }

                    if (false && !this.isConverged && (this.HydroUnitsString == "Reservoir1_hydroUnit" || this.HydroUnitsString == "Union Valley"))
                    {
                        model.FireOnMessage("    Iter "+ this.model.mInfo.Iteration +": Energy Convergence " + this.HydroUnitsString + ": " + this.energyDiff);
                        model.FireOnMessage(dbgTxt);
                    }
                }
            }
        }

        #endregion
        #region Methods - Controller

        /// <summary>Checks to ensure that all timeseries within this hydropower unit has the correct types of units. Changes all units that are not of the correct type to the default units for that TimeSeries type.</summary>
        /// <returns>If all units are correct, returns a blank string. Otherwise, returns a string describing each timeseries that failed.</returns>
        public string CheckUnits()
        {
            if (this.PowerTargetsTS.EnsureUnitsHaveSameType(this.model.PowerUnits))
                return "\n  HydroTarget: " + this.name + ". Power Demands units are now " + this.model.PowerUnits;
            return "";
        }
        /// <summary>Changes the data start date for each of the hydropower target timeseries.</summary>
        /// <param name="NewStartDate">The new data start date.</param>
        public void ChangeStartDate(DateTime NewStartDate, ModsimTimeStep timeStep)
        {
            this.PowerTargetsTS.ChangeStartDate(NewStartDate, timeStep);
        }
        /// <summary>Converts the units of the timeseries within this instance.</summary>
        /// <param name="JustTest">Specifies whether to just test if the timeseries are filled or not.</param>
        public string ConvertAndFillTimeseries(bool JustTest)
        {
            if (!JustTest)
                this.PowerTargetsTS.FillTable(this.model, 0, this.model.EnergyUnits);
            else if (!this.PowerTargetsTS.IsFilled(this.model))
                return "  Hydropower Target: " + this.name + " in the power demands table.\n";
            return "";
        }
        /// <summary>Loads the timeseries array data into the model object.</summary>
        public void LoadTimeseriesData()
        {
            int numts = this.model.TimeStepManager.noModelTimeSteps + this.model.TimeStepManager.noBackRAdditionalTSteps;
            if (this.PowerTargetsTS.getSize() > 0)
            {
                this.EnergyTargets = new double[numts, 1];
                this.model.LoadTimeSeriesArray(this.PowerTargetsTS, ref this.EnergyTargets);
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
                        if (this.EnergyTargets.Length > 0)
                            this.EnergyTargets[j, 0] = this.EnergyTargets[lastTimeStep - 1, 0];
            }
        }
        /// <summary>Initializes the timeseries data into double[,] arrays for the backrouting network.</summary>
        /// <param name="mi1">The original MODSIM model.</param>
        public void InitBackRoutTimeseries(Model mi1)
        {
            int numts = mi1.TimeStepManager.noModelTimeSteps + mi1.TimeStepManager.noBackRAdditionalTSteps;
            HydropowerTarget hydroTarget1 = mi1.hydro.GetHydroTarget(this.ID);
            if (hydroTarget1.EnergyTargets.Length > 0)
                this.EnergyTargets = new double[numts, 1];
        }

        /// <summary>Imports all field values and references except the ID field from sourceDemand to this instance.</summary>
        /// <param name="sourceDemand">The HydropowerTarget defining the fields to be imported.</param>
        public void ImportData(HydropowerTarget sourceDemand)
        {
            this.model = sourceDemand.model;
            this.Name = sourceDemand.Name;
            this.Description = sourceDemand.Description;
            this.hydroUnits = sourceDemand.hydroUnits;
            this.PowerTargetsTS = sourceDemand.PowerTargetsTS;
        }
        /// <summary>Adds this hydropower target to a list shared with the hydropower controller.</summary>
        public void AddToController()
        {
            if (this.id == -1)
                this.model.PowerObjects.Add(this.ModsimObjectType, this, ref this.name, out this.id);
            foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                if (!hydroUnit.IsInController)
                    hydroUnit.AddToController();
        }
        /// <summary>Removes this hydropower target from a list shared with the hydropower controller.</summary>
        public void RemoveFromController()
        {
            this.model.PowerObjects.Remove(this.ModsimObjectType, this.id);
            this.id = -1;
        }
        /// <summary>Set the ID of this hydropower target. Be VERY careful in setting this id.</summary>
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
            HydropowerTarget demand = obj as HydropowerTarget;
            if (demand != null)
                return this.ID.CompareTo(demand.ID);
            else
                throw new ArgumentException("When comparing two HydropowerTargets, need to specify a HydropowerTarget");
        }

        #endregion
    }
}
