using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Csu.Modsim.ModsimModel
{
    public enum HydropowerQueryType : int { Reservoir, Link, PowerEff }
    public enum ElevationFunctionType : int { Estimate, PiecewiseLinear, Polynomial }
    public enum IterativeSolutionTechnique : int { Nothing, SuccApprox, SteepestGradient, SeparateHydroLinks } 

    public class HydropowerController
    {
        #region Static varaibles 

        /// <summary>Gets a value specifying the command name used in the XY file.</summary>
        public static string XYCmdName { get { return "hydroCtrl"; } }
        /// <summary>Specifies the target r-squared value (coefficient of determination) for polynomials automatically fitted.</summary>
        public static double PolynomialTargetRsquared = 0.999;
        /// <summary>Specifies whether the hydropower calculations should be based off of fitted polynomial head functions or linearly interpolated user-input values.</summary>
        public static ElevationFunctionType ElevType = ElevationFunctionType.Estimate;

        #endregion 
        #region Instance variables

        private Model model;
        /// <summary>Specifies whether the model has the advanced hydropower extension activated or not.</summary>
        public bool IsActive = false;
        /// <summary>Specifies whether the reservoir units have been upgraded to the new hydropower units structure.</summary>
        public bool WasUpgraded = false;
        /// <summary>Specifies the number of previous iterations that will be used to check convergence in hydropower optimization algorithms. This value must be greater than 1 to check convergence.</summary>
        public int NumPrevIters = 3;
        /// <summary>Specifies the number of previous timesteps to be stored when checking pump-turbine mode.</summary>
        public int NumPrevTSteps = 5;
        /// <summary>Specifies the tolerance of convergence for hydropower optimization algorithms.</summary>
        public double Tolerance = 0.1; // 1.1;
        /// <summary>An event fired when this hydropower controller is updating.</summary>
        public event EventHandler Updating;
        /// <summary>Specifies whether to use the algorithm that solves the hydropower solution iteratively.</summary>
        public IterativeSolutionTechnique IterativeTechnique = IterativeSolutionTechnique.Nothing;
        public bool IterativeSuppressSettingHiBounds = false;

        #endregion
        #region Properties

        /// <summary>Gets an array of all the efficiency tables in this controller.</summary>
        public PowerEfficiencyCurve[] EfficiencyCurves
        {
            get 
            {
                List<object> efficiencies = this.model.PowerObjects.GetObjectList(ModsimCollectionType.powerEff);
                return efficiencies.ConvertAll(x => (PowerEfficiencyCurve)x).ToArray();
            }
        }
        /// <summary>Gets an array of all the hydropower units.</summary>
        public HydropowerUnit[] HydroUnits
        {
            get 
            {
                List<object> units = this.model.PowerObjects.GetObjectList(ModsimCollectionType.hydroUnit);
                return units.ConvertAll(x => (HydropowerUnit)x).ToArray(); 
            }
        }
        /// <summary>Gets an array of all the hydropower targets.</summary>
        public HydropowerTarget[] HydroTargets
        {
            get 
            {
                List<object> targets = this.model.PowerObjects.GetObjectList(ModsimCollectionType.hydroTarget);
                return targets.ConvertAll(x => (HydropowerTarget)x).ToArray(); 
            }
        }
        /// <summary>Gets whether the hydropower controller will update the outputs.</summary>
        public bool WillUpdateOutputs
        {
            get
            {
                int iodd = this.model.mInfo.Iteration % 2;
                int nmown = this.model.mInfo.ownerList.Length;
                return this.IsActive && (nmown <= 0 || iodd <= 0) && Updating != null;
            }
        }

        #endregion

        #region Constructor and copying method

        /// <summary>Creates a new instance of the updater.</summary>
        /// <param name="mi">The model for which this updater is set.</param>
        public HydropowerController(Model mi)
        {
            this.model = mi;
        }

        /// <summary>Copies this instance of hydropower controller and defines the new references.</summary>
        public HydropowerController Copy(Model newModelReference)
        {
            HydropowerController newCtrler = (HydropowerController)this.MemberwiseClone();
            newCtrler.model = newModelReference;
            return newCtrler;
        }

        #endregion

        #region Tables and querying hydropower targets

        // Tables for hydropower targets
        /// <summary>Builds a DataTable containing all the hydropower targets.</summary>
        public HydropowerControllerDataSet.HydroTargetsInfoDataTable HydroTargetsTable
        {
            get { return this.ToTable(this.HydroTargets); }
        }
        /// <summary>Builds a DataTable containing all the hydropower targets.</summary>
        /// <param name="hydroUnits">The list of hydropower targets to place into a table.</param>
        public HydropowerControllerDataSet.HydroTargetsInfoDataTable ToTable(HydropowerTarget[] hydroTargets)
        {
            HydropowerControllerDataSet.HydroTargetsInfoDataTable dt = new HydropowerControllerDataSet.HydroTargetsInfoDataTable();
            foreach (HydropowerTarget hydroTarget in hydroTargets)
            {
                if (hydroTarget != null)
                {
                    dt.AddHydroTargetsInfoRow(
                        hydroTarget.ID,
                        hydroTarget.Name,
                        hydroTarget.Description,
                        HydropowerUnit.GetHydroUnitsAsString(hydroTarget.HydroUnits),
                        hydroTarget.PowerTargetsTS.Copy()); 
                }
            }
            return dt;
        }
        /// <summary>Gets an array of HydropowerTargets from a table.</summary>
        /// <param name="dt">The table containing HydropowerTarget definitions.</param>
        /// <param name="sort">Specifies whether or not to sort the table.</param>
        public HydropowerTarget[] GetHydroTargetsFromTable(HydropowerControllerDataSet.HydroTargetsInfoDataTable dt, bool sort, bool UpdateController)
        {
            if (dt == null) return null;
            DataRow[] rows = dt.Select("", sort ? dt.HydroTargetIDColumn.ColumnName + " ASC" : "");
            List<HydropowerTarget> list = new List<HydropowerTarget>();
            HydropowerTarget demand;
            foreach (DataRow row in rows)
            {
                try
                {
                    demand = this.GetRowHydroTarget((HydropowerControllerDataSet.HydroTargetsInfoRow)row, UpdateController);
                }
                catch { demand = null; }
                if (demand != null)
                    list.Add(demand);
            }
            return list.ToArray();
        }
        /// <summary>Converts a DataTable row to a new instance of a HydropowerTarget.</summary>
        /// <param name="row">The row within the DataTable to convert.</param>
        public HydropowerTarget GetRowHydroTarget(HydropowerControllerDataSet.HydroTargetsInfoRow row, bool UpdateController)
        {
            HydropowerTarget hydroTarget;
            HydropowerUnit[] hydroUnits = HydropowerUnit.GetHydroUnitsFromString(this.model, row.HydroUnits);
            TimeSeries powDem;
            if (row.PowerTargetsHidden != null && !row.PowerTargetsHidden.GetType().Equals(typeof(System.DBNull)))
                powDem = (TimeSeries)row.PowerTargetsHidden;
            else
                powDem = new TimeSeries(TimeSeriesType.Power_Target); 

            hydroTarget = new HydropowerTarget(this.model, row.HydroTargetName, powDem, hydroUnits);
            hydroTarget.Description = row.Description;
            if (UpdateController && this.HydroTargetExists(row.HydroTargetID))
            {
                HydropowerTarget demandInCtrl = this.GetHydroTarget(row.HydroTargetID);
                demandInCtrl.ImportData(hydroTarget);
                return demandInCtrl;
            }
            else if (UpdateController)
            {
                hydroTarget.AddToController();
            }
            else
            {
                hydroTarget.SetID(row.HydroTargetID);
            }
            return hydroTarget;
        }
        /// <summary>Updates all of the fields of the specified row except the hydropower target ID field with the specified hydropower target values.</summary>
        /// <param name="row">The row to update.</param>
        /// <param name="hydroTarget">The hydropower target used to set fields in the row.</param>
        public void SetRowHydroTarget(HydropowerControllerDataSet.HydroTargetsInfoRow row, HydropowerTarget hydroTarget)
        {
            if (row == null) return;
            if (hydroTarget == null)
            {
                try
                {
                    HydropowerTarget rowHydroTarget = this.GetRowHydroTarget(row, true);
                    rowHydroTarget.RemoveFromController();
                    row.Delete();
                    return;
                }
                catch { return; }
            }
            else
            {
                row.HydroTargetName = hydroTarget.Name;
                row.Description = hydroTarget.Description;
                row.HydroUnits = HydropowerUnit.GetHydroUnitsAsString(hydroTarget.HydroUnits);
                row.PowerTargetsHidden = hydroTarget.PowerTargetsTS.Copy();
            }
        }
        /// <summary>Updates the <c>HydropowerTarget</c> objects within the controller based on user-input data. If the demand does not exist in the controller, it is added to the controller.</summary>
        /// <param name="dt">The table that the user updated.</param>
        /// <returns>If the updates are successful, returns true; otherwise, returns false.</returns>
        public bool UpdateHydroTargetsInController(HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            try
            {
                foreach (HydropowerControllerDataSet.HydroTargetsInfoRow row in dt.Select("", dt.HydroTargetIDColumn.ColumnName + " ASC"))
                    this.GetRowHydroTarget(row, true);
                return true;
            }
            catch (Exception ex)
            {
                this.model.FireOnError("Error occurred when updating hydropower targets from the hydropower targets list form: " + ex.Message);
                return false;
            }
        }

        // Query hydropower targets
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="hydroUnit">One of the hydropower units that defines power and energy for hydropower targets.</param>
        public HydropowerTarget[] QueryHydroTargets(HydropowerUnit hydroUnit)
        {
            return this.QueryHydroTargets(hydroUnit, this.HydroTargetsTable);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="hydroUnit">One of the hydropower units that defines power and energy for hydropower targets.</param>
        /// <param name="dt">The hydropower targets datatable to query.</param>
        public HydropowerTarget[] QueryHydroTargets(HydropowerUnit hydroUnit, HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            string colName = dt.HydroUnitsColumn.ColumnName;
            return this.QueryHydroTargets(
                colName + " = '" + hydroUnit.Name + "' OR "
                + colName + " LIKE '" + hydroUnit.Name + "|%' OR "
                + colName + " LIKE '%|" + hydroUnit.Name + "' OR "
                + colName + " LIKE '%|" + hydroUnit.Name + "|%'", dt);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="hydroUnits">An array of hydropower units that defines power and energy for hydropower targets.</param>
        public HydropowerTarget[] QueryHydroTargets(HydropowerUnit[] hydroUnits)
        {
            return this.QueryHydroTargets(hydroUnits, this.HydroTargetsTable);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="hydroUnits">An array of hydropower units that defines power and energy for hydropower targets.</param>
        /// <param name="dt">The hydropower targets datatable to query.</param>
        public HydropowerTarget[] QueryHydroTargets(HydropowerUnit[] hydroUnits, HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            if (hydroUnits == null) return new HydropowerTarget[0];
            List<HydropowerTarget> hydroTargetsList = new List<HydropowerTarget>();
            foreach (HydropowerUnit hydroUnit in hydroUnits)
                foreach (HydropowerTarget hydroTarget in this.QueryHydroTargets(hydroUnit, dt))
                    if (!hydroTargetsList.Contains(hydroTarget)) 
                        hydroTargetsList.Add(hydroTarget);
            return hydroTargetsList.ToArray();
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="links">A list of links that might define flow through any of the hydropower units.</param>
        public HydropowerTarget[] QueryHydroTargets(Link[] links)
        {
            return this.QueryHydroTargets(links, this.HydroTargetsTable);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="links">A list of links that might define flow through any of the hydropower units.</param>
        /// <param name="dt">The hydropower targets datatable to query.</param>
        public HydropowerTarget[] QueryHydroTargets(Link[] links, HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            return this.QueryHydroTargets(this.QueryHydroUnits(links), dt);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="nodes">A list of reservoir nodes that might define either the upper or lower water elevation across any of the hydropower units.</param>
        public HydropowerTarget[] QueryHydroTargets(Node[] nodes)
        {
            return this.QueryHydroTargets(nodes, this.HydroTargetsTable);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="nodes">A list of reservoir nodes that might define either the upper or lower water elevation across any of the hydropower units.</param>
        /// <param name="dt">The hydropower targets datatable to query.</param>
        public HydropowerTarget[] QueryHydroTargets(Node[] nodes, HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            return this.QueryHydroTargets(this.QueryHydroUnits(nodes), dt);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="link">One of the links that defines flow through any of the hydropower units.</param>
        public HydropowerTarget[] QueryHydroTargets(Link link)
        {
            return this.QueryHydroTargets(link, this.HydroTargetsTable);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="link">One of the links that defines flow through any of the hydropower units.</param>
        /// <param name="dt">The hydropower targets datatable to query.</param>
        public HydropowerTarget[] QueryHydroTargets(Link link, HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            return this.QueryHydroTargets(this.QueryHydroUnits(link), dt);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="node">The reservoir node that defines either the upper or lower water elevation across any of the hydropower units.</param>
        public HydropowerTarget[] QueryHydroTargets(Node node)
        {
            return this.QueryHydroTargets(node, this.HydroTargetsTable);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="node">The reservoir node that defines either the upper or lower water elevation across any of the hydropower units.</param>
        /// <param name="dt">The hydropower targets datatable to query.</param>
        public HydropowerTarget[] QueryHydroTargets(Node node, HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            HydropowerUnit[] hydroUnits = this.QueryHydroUnits(node); 
            if (hydroUnits == null) return new HydropowerTarget[0]; 
            return this.QueryHydroTargets(hydroUnits, dt);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="query">The user-specified query used to query the table.</param>
        public HydropowerTarget[] QueryHydroTargets(string query)
        {
            return this.QueryHydroTargets(query, this.HydroTargetsTable);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="query">The query string used to query the table.</param>
        /// <param name="dt">The hydropower targets table to query.</param>
        public HydropowerTarget[] QueryHydroTargets(string query, string sort)
        {
            return this.QueryHydroTargets(query, sort, this.HydroTargetsTable);
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="query">The query string used to query the table.</param>
        /// <param name="dt">The hydropower targets datatable to query.</param>
        public HydropowerTarget[] QueryHydroTargets(string query, HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            if (query == null) return this.HydroTargets;
            return Array.ConvertAll(dt.Select(query), row => this.GetHydroTarget(((HydropowerControllerDataSet.HydroTargetsInfoRow)row).HydroTargetID));
        }
        /// <summary>Gets a list of hydropower targets associated with the specified hydropower unit, link, node, or user-defined query.</summary>
        /// <param name="query">The query string used to query the table.</param>
        /// <param name="sort">The sort string used to sort the table (contains the column name and the sort direction).</param>
        /// <param name="dt">The hydropower targets datatable to query.</param>
        public HydropowerTarget[] QueryHydroTargets(string query, string sort, HydropowerControllerDataSet.HydroTargetsInfoDataTable dt)
        {
            if (sort == null) sort = "";
            if (query == null) return this.HydroTargets;
            return Array.ConvertAll(dt.Select(query, sort), row => this.GetHydroTarget(((HydropowerControllerDataSet.HydroTargetsInfoRow)row).HydroTargetID));
        }

        #endregion
        #region Tables and querying hydropower units

        // Tables for hydropower units
        /// <summary>Builds a DataTable containing all the hydropower units.</summary>
        public HydropowerControllerDataSet.HydroUnitsInfoDataTable HydroUnitsTable
        {
            get { return this.ToTable(this.HydroUnits); }
        }
        /// <summary>Builds a DataTable containing all the hydropower units.</summary>
        /// <param name="hydroUnits">The list of hydropower units to place into a table.</param>
        public HydropowerControllerDataSet.HydroUnitsInfoDataTable ToTable(HydropowerUnit[] hydroUnits)
        {
            HydropowerControllerDataSet.HydroUnitsInfoDataTable dt = new HydropowerControllerDataSet.HydroUnitsInfoDataTable();
            foreach (HydropowerUnit hydroUnit in hydroUnits)
            {
                if (hydroUnit != null)
                {
                    dt.AddHydroUnitsInfoRow(
                        hydroUnit.ID,
                        hydroUnit.Name,
                        hydroUnit.Description,
                        hydroUnit.PowerCapacity,
                        hydroUnit.Type.ToString(),
                        hydroUnit.FlowLinksString,
                        hydroUnit.ElevDefnFrom.Type.ToString(),
                        hydroUnit.ElevDefnFrom.Reservoir.name,
                        hydroUnit.ElevDefnTo.Type.ToString(),
                        hydroUnit.ElevDefnTo.Reservoir.name,
                        hydroUnit.EfficiencyCurve.Name,
                        hydroUnit.PeakGenerationOnly,
                        hydroUnit.GeneratingHoursTS.Copy());
                }
            }
            return dt;
        }
        /// <summary>Gets an array of HydropowerUnits from a table.</summary>
        /// <param name="dt">The table containing HydropowerUnit definitions.</param>
        /// <param name="sort">Specifies whether or not to sort the table.</param>
        public HydropowerUnit[] GetHydroUnitsFromTable(HydropowerControllerDataSet.HydroUnitsInfoDataTable dt, bool sort, bool UpdateController)
        {
            if (dt == null) return null;
            DataRow[] rows = dt.Select("", sort ? dt.HydroUnitIDColumn.ColumnName + " ASC" : "");
            List<HydropowerUnit> list = new List<HydropowerUnit>();
            HydropowerUnit unit;
            foreach (DataRow row in rows)
            {
                try { unit = this.GetRowHydroUnit((HydropowerControllerDataSet.HydroUnitsInfoRow)row, UpdateController); }
                catch { unit = null; }
                if (unit != null) list.Add(unit);
            }
            return list.ToArray();
        }
        /// <summary>Converts a DataTable row to a new instance of a HydropowerUnit.</summary>
        /// <param name="row">The row within the DataTable to convert.</param>
        public HydropowerUnit GetRowHydroUnit(HydropowerControllerDataSet.HydroUnitsInfoRow row, bool UpdateController)
        {
            HydropowerUnit hydroUnit;
            Link[] links = Link.GetLinksFromString(this.model, row.FlowLinks);
            Node fromRes = this.model.FindNode(row.FromRes);
            Node toRes = this.model.FindNode(row.ToRes);
            HydropowerElevDef fromResDef = new HydropowerElevDef(fromRes, HydropowerElevDef.GetType(row.FromElevType));
            HydropowerElevDef toResDef = new HydropowerElevDef(toRes, HydropowerElevDef.GetType(row.ToElevType));
            HydroUnitType type = HydropowerUnit.GetType(row.HydroUnitType);
            PowerEfficiencyCurve effCurve = this.GetEfficiencyCurve(row.PowerEff);
            double powerCap = row.PowerCapacity;
            bool peakGenOnly = row.PeakGenOnly;
            TimeSeries genHours;
            if (row.GenHoursHidden != null && !row.GenHoursHidden.GetType().Equals(typeof(System.DBNull)))
                genHours = (TimeSeries)row.GenHoursHidden;
            else
                genHours = new TimeSeries(TimeSeriesType.Generating_Hours); 

            hydroUnit = new HydropowerUnit(this.model, row.HydroUnitName, links, fromResDef, toResDef, effCurve, type, powerCap, genHours, peakGenOnly);
            hydroUnit.Description = row.Description;
            if (UpdateController && this.HydroUnitExists(row.HydroUnitID))
            {
                HydropowerUnit unitInCtrl = this.GetHydroUnit(row.HydroUnitID);
                unitInCtrl.ImportData(hydroUnit);
                return unitInCtrl;
            }
            else if (UpdateController)
            {
                hydroUnit.AddToController();
            }
            else
            {
                hydroUnit.SetID(row.HydroUnitID);
            }
            return hydroUnit;
        }
        /// <summary>Updates all of the fields of the specified row except the hydropower unit ID field with the specified hydropower unit values.</summary>
        /// <param name="row">The row to update.</param>
        /// <param name="hydroUnit">The hydropower unit used to set fields in the row.</param>
        public void SetRowHydroUnit(HydropowerControllerDataSet.HydroUnitsInfoRow row, HydropowerUnit hydroUnit)
        {
            if (row == null) return;
            if (hydroUnit == null)
            {
                try
                {
                    HydropowerUnit rowHydroUnit = this.GetRowHydroUnit(row, true);
                    rowHydroUnit.RemoveFromController();
                    row.Delete();
                    return;
                }
                catch { return; }
            }
            else
            {
                row.HydroUnitName = hydroUnit.Name;
                row.Description = hydroUnit.Description;
                row.PowerCapacity = hydroUnit.PowerCapacity;
                row.HydroUnitType = hydroUnit.Type.ToString();
                row.FlowLinks = string.Join(Link.ForbiddenStringInName, Array.ConvertAll(hydroUnit.FlowLinks, link => link.name));
                row.FromElevType = hydroUnit.ElevDefnFrom.Type.ToString();
                row.FromRes = hydroUnit.ElevDefnFrom.Reservoir.name;
                row.ToElevType = hydroUnit.ElevDefnTo.Type.ToString();
                row.ToRes = hydroUnit.ElevDefnTo.Reservoir.name;
                row.PowerEff = hydroUnit.EfficiencyCurve.Name;
                row.PeakGenOnly = hydroUnit.PeakGenerationOnly;
                row.GenHoursHidden = hydroUnit.GeneratingHoursTS.Copy();
            }
        }
        /// <summary>Updates the <c>HydropowerUnit</c> objects within the controller based on user-input data. If the unit does not exist in the controller, it is added to the controller.</summary>
        /// <param name="dt">The table that the user updated.</param>
        /// <returns>If the updates are successful, returns true; otherwise, returns false.</returns>
        public bool UpdateHydroUnitsInController(HydropowerControllerDataSet.HydroUnitsInfoDataTable dt)
        {
            try
            {
                foreach (HydropowerControllerDataSet.HydroUnitsInfoRow row in dt.Select("", dt.HydroUnitIDColumn.ColumnName + " ASC"))
                    this.GetRowHydroUnit(row, true);
                return true;
            }
            catch (Exception ex)
            {
                this.model.FireOnError("Error occurred when updating hydropower units from the hydropower units list form: " + ex.Message);
                return false;
            }
        }

        // Query hydropower units
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="effCurve">The efficiency table that might define turbine/pump efficiencies.</param>
        public HydropowerUnit[] QueryHydroUnits(PowerEfficiencyCurve effCurve)
        {
            return this.QueryHydroUnits(effCurve, this.HydroUnitsTable); 
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="effCurve">The efficiency table that might define turbine/pump efficiencies.</param>
        /// <param name="dt">The hydropower units datatable to query.</param>
        public HydropowerUnit[] QueryHydroUnits(PowerEfficiencyCurve effCurve, HydropowerControllerDataSet.HydroUnitsInfoDataTable dt)
        {
            if (effCurve == null) return null; 
            return this.QueryHydroUnits(dt.PowerEffColumn.ColumnName + " = '" + effCurve.Name + "'", dt);
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="links">A list of links that might define flow through hydropower units.</param>
        public HydropowerUnit[] QueryHydroUnits(Link[] links)
        {
            return this.QueryHydroUnits(links, this.HydroUnitsTable); 
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="links">A list of links that might define flow through hydropower units.</param>
        /// <param name="dt">The hydropower units datatable to query.</param>
        public HydropowerUnit[] QueryHydroUnits(Link[] links, HydropowerControllerDataSet.HydroUnitsInfoDataTable dt)
        {
            List<HydropowerUnit> list = new List<HydropowerUnit>();
            foreach (Link link in links)
                foreach (HydropowerUnit hydroUnit in this.QueryHydroUnits(link, dt))
                    if (!list.Contains(hydroUnit))
                        list.Add(hydroUnit);
            return list.ToArray();
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="link">One of the links that defines flow through the hydropower units.</param>
        public HydropowerUnit[] QueryHydroUnits(Link link)
        {
            return this.QueryHydroUnits(link, this.HydroUnitsTable);
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="link">One of the links that defines flow through the hydropower units.</param>
        /// <param name="dt">The hydropower units datatable to query.</param>
        public HydropowerUnit[] QueryHydroUnits(Link link, HydropowerControllerDataSet.HydroUnitsInfoDataTable dt)
        {
            string colName = dt.FlowLinksColumn.ColumnName;
            return this.QueryHydroUnits(
                colName + " = '" + link.name + "' OR "
                + colName + " LIKE '" + link.name + "|%' OR "
                + colName + " LIKE '%|" + link.name + "' OR "
                + colName + " LIKE '%|" + link.name + "|%'", dt);
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="nodes">A list of reservoir nodes that might define either the upper or lower water elevation across a hydropower unit.</param>
        public HydropowerUnit[] QueryHydroUnits(Node[] nodes)
        {
            return this.QueryHydroUnits(nodes, this.HydroUnitsTable);
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="nodes">A list of reservoir nodes that might define either the upper or lower water elevation across a hydropower unit.</param>
        /// <param name="dt">The hydropower units datatable to query.</param>
        public HydropowerUnit[] QueryHydroUnits(Node[] nodes, HydropowerControllerDataSet.HydroUnitsInfoDataTable dt)
        {
            List<HydropowerUnit> list = new List<HydropowerUnit>();
            foreach (Node node in nodes)
                foreach (HydropowerUnit hydroUnit in this.QueryHydroUnits(node, dt))
                    if (!list.Contains(hydroUnit))
                        list.Add(hydroUnit);
            return list.ToArray();
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="node">The reservoir node that defines either the upper or lower water elevation across the hydropower unit.</param>
        public HydropowerUnit[] QueryHydroUnits(Node node)
        {
            return this.QueryHydroUnits(node, this.HydroUnitsTable);
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="node">The reservoir node that defines either the upper or lower elevation across the hydropower unit.</param>
        /// <param name="dt">The hydropower units datatable to query.</param>
        public HydropowerUnit[] QueryHydroUnits(Node node, HydropowerControllerDataSet.HydroUnitsInfoDataTable dt)
        {
            if (node.nodeType == NodeType.Reservoir) 
                return this.QueryHydroUnits(dt.ToResColumn.ColumnName + " = '" + node.name + "' OR " + dt.FromResColumn.ColumnName + " = '" + node.name + "'", dt);
            return null;
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="query">The user-specified query used to query the table.</param>
        public HydropowerUnit[] QueryHydroUnits(string query)
        {
            return this.QueryHydroUnits(query, this.HydroUnitsTable);
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="query">The query string used to query the table.</param>
        /// <param name="dt">The hydropower units table to query.</param>
        public HydropowerUnit[] QueryHydroUnits(string query, string sort)
        {
            return this.QueryHydroUnits(query, sort, this.HydroUnitsTable);
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="query">The query string used to query the table.</param>
        /// <param name="dt">The hydropower units table to query.</param>
        public HydropowerUnit[] QueryHydroUnits(string query, HydropowerControllerDataSet.HydroUnitsInfoDataTable dt)
        {
            if (query == null) return this.HydroUnits;
            return Array.ConvertAll(dt.Select(query), row => this.GetHydroUnit(((HydropowerControllerDataSet.HydroUnitsInfoRow)row).HydroUnitID));
        }
        /// <summary>Gets a list of hydropower units referencing the specified link, node, efficiency table or user-defined query.</summary>
        /// <param name="query">The query string used to query the table.</param>
        /// <param name="sort">The sort string used to sort the table (contains the column name and the sort direction).</param>
        /// <param name="dt">The hydropower units table to query.</param>
        public HydropowerUnit[] QueryHydroUnits(string query, string sort, HydropowerControllerDataSet.HydroUnitsInfoDataTable dt)
        {
            if (sort == null) sort = "";
            if (query == null) return this.HydroUnits;
            return Array.ConvertAll(dt.Select(query, sort), row => this.GetHydroUnit(((HydropowerControllerDataSet.HydroUnitsInfoRow)row).HydroUnitID));
        }

        #endregion
        #region Hydropower objects

        // Retrieving single objects
        /// <summary>Retrieves a <c>HydropowerTarget</c> from within the hydropower controller given a name or ID number.</summary>
        /// <param name="ID">The ID of the <c>HydropowerTarget</c>.</param>
        public HydropowerTarget GetHydroTarget(int ID)
        {
            return (HydropowerTarget)this.model.PowerObjects[ModsimCollectionType.hydroTarget, ID];
        }
        /// <summary>Retrieves a <c>HydropowerTarget</c> from within the hydropower controller given a name or ID number.</summary>
        /// <param name="name">The name of the <c>HydropowerTarget</c>.</param>
        public HydropowerTarget GetHydroTarget(string name)
        {
            return (HydropowerTarget)this.model.PowerObjects[ModsimCollectionType.hydroTarget, name];
        }
        /// <summary>Retrieves a <c>HydropowerUnit</c> from within the hydropower controller given a name or ID number.</summary>
        /// <param name="ID">The ID of the <c>HydropowerUnit</c>.</param>
        public HydropowerUnit GetHydroUnit(int ID)
        {
            return (HydropowerUnit)this.model.PowerObjects[ModsimCollectionType.hydroUnit, ID];
        }
        /// <summary>Retrieves a <c>HydropowerUnit</c> from within the hydropower controller given a name or ID number.</summary>
        /// <param name="name">The name of the <c>HydropowerUnit</c>.</param>
        public HydropowerUnit GetHydroUnit(string name)
        {
            return (HydropowerUnit)this.model.PowerObjects[ModsimCollectionType.hydroUnit, name];
        }
        /// <summary>Retrieves a <c>PowerEfficiencyCurve</c> from within the hydropower controller given a name or ID number.</summary>
        /// <param name="ID">The ID of the <c>PowerEfficiencyCurve</c>.</param>
        public PowerEfficiencyCurve GetEfficiencyCurve(int ID)
        {
            return (PowerEfficiencyCurve)this.model.PowerObjects[ModsimCollectionType.powerEff, ID];
        }
        /// <summary>Retrieves a <c>PowerEfficiencyCurve</c> from within the hydropower controller given a name or ID number.</summary>
        /// <param name="name">The name of the <c>PowerEfficiencyCurve</c>.</param>
        public PowerEfficiencyCurve GetEfficiencyCurve(string name)
        {
            return (PowerEfficiencyCurve)this.model.PowerObjects[ModsimCollectionType.powerEff, name];
        }
        /// <summary>Determines whether a particular HydropowerTarget exists within the hydropower controller.</summary>
        /// <param name="ID">The ID of the object to search for.</param>
        public bool HydroTargetExists(int ID)
        {
            return this.model.PowerObjects.Exists(ModsimCollectionType.hydroTarget, ID);
        }
        /// <summary>Determines whether a particular HydropowerTarget exists within the hydropower controller.</summary>
        /// <param name="name">The name of the object to search for.</param>
        public bool HydroTargetExists(string name)
        {
            return this.model.PowerObjects.Exists(ModsimCollectionType.hydroTarget, name);
        }
        /// <summary>Determines whether a particular HydropowerUnit exists within the hydropower controller.</summary>
        /// <param name="ID">The ID of the object to search for.</param>
        public bool HydroUnitExists(int ID)
        {
            return this.model.PowerObjects.Exists(ModsimCollectionType.hydroUnit, ID);
        }
        /// <summary>Determines whether a particular HydropowerUnit exists within the hydropower controller.</summary>
        /// <param name="name">The name of the object to search for.</param>
        public bool HydroUnitExists(string name)
        {
            return this.model.PowerObjects.Exists(ModsimCollectionType.hydroUnit, name);
        }
        /// <summary>Determines whether a particular PowerEfficiencyCurve exists within the hydropower controller.</summary>
        /// <param name="ID">The ID of the object to search for.</param>
        public bool EfficiencyCurveExists(int ID)
        {
            return this.model.PowerObjects.Exists(ModsimCollectionType.powerEff, ID);
        }
        /// <summary>Determines whether a particular PowerEfficiencyCurve exists within the hydropower controller.</summary>
        /// <param name="name">The name of the object to search for.</param>
        public bool EfficiencyCurveExists(string name)
        {
            return this.model.PowerObjects.Exists(ModsimCollectionType.powerEff, name);
        }

        #endregion
        #region Timeseries methods

        /// <summary>Checks to ensure that all timeseries within the hydropower controller have the correct types of units. Changes all units that are not of the correct type to the default units for that TimeSeries type.</summary>
        /// <returns>If all units are correct, returns a blank string. Otherwise, returns a string describing each timeseries that failed.</returns>
        public string CheckUnits()
        {
            string msg = "";

            // Hydropower units generating hours
            foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                msg += hydroUnit.CheckUnits();

            // Hydropower targets 
            foreach (HydropowerTarget hydroTarget in this.HydroTargets)
                msg += hydroTarget.CheckUnits();

            return msg;
        }
        /// <summary>Changes the data start date for each of the hydropower target timeseries.</summary>
        /// <param name="NewStartDate">The new data start date.</param>
        public void ChangeStartDate(DateTime NewStartDate, ModsimTimeStep timeStep)
        {
            foreach (HydropowerTarget hydroTarget in this.HydroTargets)
                hydroTarget.ChangeStartDate(NewStartDate, timeStep);
        }
        /// <summary>Converts units of non-timeseries data in hydropower unit definitions to those required for model simulation.</summary>
        public void ConvertNonTSUnits()
        {
            foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                hydroUnit.ConvertNonTSUnits();
        }
        /// <summary>Converts units of power demand timeseries data within hydropower unit definitions.</summary>
        public string ConvertAndFillTimeseries(bool JustTest)
        {
            string msg = "";

            // Hydropower units generating hours
            foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                msg += hydroUnit.ConvertAndFillTimeSeries(JustTest);

            // Hydropower targets 
            foreach (HydropowerTarget hydroTarget in this.HydroTargets)
                msg += hydroTarget.ConvertAndFillTimeseries(JustTest);

            return msg;
        }
        /// <summary>Loads the timeseries data into double[,] arrays.</summary>
        public void LoadTimeseriesData()
        {
            foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                hydroUnit.LoadTimeseriesData();

            foreach (HydropowerTarget hydroTarget in this.HydroTargets)
                hydroTarget.LoadTimeseriesData();
        }
        /// <summary>Extends the timeseries of mi2 with the information from mi1.</summary>
        public void extendTimeSeries()
        {
            foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                hydroUnit.ExtendTimeseries();

            foreach (HydropowerTarget hydroTarget in this.HydroTargets)
                hydroTarget.ExtendTimeseries();
        }
        /// <summary>Initializes the timeseries data into double[,] arrays for the backrouting network.</summary>
        /// <param name="mi1">The original MODSIM model.</param>
        public void InitBackRoutTimeseriesData(Model mi1)
        {
            foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                hydroUnit.InitBackRoutTimeseries(mi1);

            foreach (HydropowerTarget hydroTarget in this.HydroTargets)
                hydroTarget.InitBackRoutTimeseries(mi1);
        }
        /// <summary>Automatically fits all stage-storage or stage-discharge relationships within every reservoir that does not have them already defined.</summary>
        public void FitReservoirPolynomials()
        {
            // Fit the stage-storage relationship within each reservoir
            Node[] reservoirs = this.model.Nodes_Reservoirs;
            for (int i = 0; i < reservoirs.Length; i++)
                reservoirs[i].FitPolynomials(HydropowerController.PolynomialTargetRsquared, this.model.ScaleFactor); 
        }

        #endregion
        #region Methods for simulation

        public void SetHydroLinkCosts(double weight)
        {
            weight = Math.Abs(weight);
            foreach (HydropowerTarget targ in this.model.hydro.HydroTargets)
                foreach (Link l in targ.FlowLinks)
                    l.m.cost += -Convert.ToInt64(weight); 
        }
        
        /// <summary>Updates the power info at each hydropower unit.</summary>
        public void Update()
        {

            // update hydropower information 
            if (this.WillUpdateOutputs)
            {
                EventHandler e = Updating;
                if (e != null) e(this, new EventArgs()); 

                // Update all hydropower units
                foreach (HydropowerUnit hydroUnit in this.HydroUnits)
                    hydroUnit.Update();

                // Update all hydropower targets
                foreach (HydropowerTarget hydroTarget in this.HydroTargets)
                    hydroTarget.Update();
            }
        }
        /// <summary>Checks whether all hydropower units with power demand have converged.</summary>
        public bool IsConverged()
        {
            foreach (HydropowerTarget hydroTarget in this.HydroTargets)
                if (!hydroTarget.IsConverged)
                    return false;
            return true;
        }

        #endregion
    }
}
