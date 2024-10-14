using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    #region Constants

    public static class DefineConstants
    {
        public const long MODSIM_MAX_YEARS = 3000;
        public const long NAMELEN = 128;
        public const int MAXLAG = 1200;
        public const long FALSE = 0;
        public const long OLD_MODSIM_RES = 0;
        public const long CHILD_ACCOUNT_RES = 55;
        public const long PARENT_ACCOUNT_RES = 56;
        public const long NONCH_ACCOUNT_RES = 57;
        public const long ZEROSYS_ACCOUNT_RES = 58;
        //public const int UNKNOWN_NODE_TYPE = 0;
        //public const int RESERVOIR = 1;
        //public const int NONSTORAGE = 2;
        //public const int DEMAND = 3;
        //public const int SINK = 4;
        public const double ROFF = 0.4999999;
        public const long NULL = 0;
        public const long SELECT_LINK_ALL = 0;
        public const long SELECT_LINK_ACCRUAL = 1;
        public const long SELECT_LINK_NATURAL_FLOW = 2;
        public const long SELECT_LINK_STORAGE = 3;
        public const long ADA_TYPE_NONE = -1;
        public const long ADA_TYPE_INF = 0;
        public const long ADA_TYPE_DEM = 1;
        public const long ADA_TYPE_EVP = 2;
        public const long ADA_TYPE_GEN = 3;
        public const long ADA_TYPE_TRG = 4;
        public const long ADA_TYPE_PRIO = 5;
        public const long ADA_TYPE_MIN = 6;
        public const long ADA_TYPE_MAX = 7;
        public const long ADA_TYPE_CST = 8;
        public const long ADA_TYPE_PRT = 9;
        public const long ADA_TYPE_IFL = 10;
        public const long ADA_TYPE_PLG = 11;
        public const long ADA_TYPE_ILG = 12;
        public const long ADA_TYPE_PLC = 13;
        public const long ADA_TYPE_ILC = 14;
        public const long ADA_TYPE_VOL = 15;
        public const long ADA_TYPE_SRF = 16;
        public const long ADA_TYPE_ELV = 17;
        public const long ADA_TYPE_MVL = 18;
        public const long ADA_TYPE_IVL = 19;
        public const long ADA_TYPE_PCP = 20;
        public const long ADA_TYPE_DST = 21;
        public const long ADA_TYPE_FORC = 22;
        public const long MMSG_STATUS = 0;
        public const long MMSG_WARNING = 1;
        public const long MMSG_ERROR = 2;
        public const long MMSG_INFO = 3;
        public const int PRD = 0;
        public const int SUBPRD = 2;
        public const long DNODES = 2000;
        public const int REAL_LINK = 1;
        public const int INFLOW_LINK = 2;
        public const int TARGSTO_LINK = 3;
        public const int FINALSTO_LINK = 4;
        public const int DEMAND_LINK = 5;
        public const int SPILL_LINK = 7;
        public const int GWOUT_LINK = 8;
        public const int GWIN_LINK = 9;
        public const int EVAP_LINK = 10;
        public const int FTRTN_LINK = 11;
        public const long SMOOTHITERATION = 990;
        public const long NL1 = 16000;
        public const long NRES1 = 400;
        public const long NJA = 5000;
        public const long NUM_OUTPUT_FILES = 6;
        public const long OUT_FILE2 = 0;
        public const long ACCOUNT_PRN2 = 1;
        public const long DEMAND_PRN2 = 2;
        public const long GROUNDWATER_PRN2 = 3;
        public const long LINK_PRN2 = 4;
        public const long RESERVOIR_PRN2 = 5;
        public const long MAXLINKSRELAX = 70000;
        public const long MAXNODESRELAX = 10000*4;
        public const long NODATAVALUE = 99999999;
        public const long COST_SUPERLARGE = 999999999;
        public const long COST_LARGER = 288888888;
        public const long COST_LARGE = 99999999;
        public const long COST_LARGE2 = 98888888;
        public const long COST_LARGE3 = 98999999;
        public const long COST_MED = 9999999;
        public const long COST_MEDSMALL = 2999999;
        public const long COST_MEDSMALL2 = 2000000;
        public const long COST_SMALL = 999999;
        public const long COST_SMALLER = 399999;
        public const long COST_SUPERSMALL = 99999;
        public const long COST_SINK = 4999;
        public const long MAXAUCTIONPASS = 16;
        public const long EPSFACTOR = 3;
        public const int USEAUCTION = 0;
        public const int USEINCREMENT = 0;
        //public const long SHOWLOG = 0;
        public const long MATRIXSIZE = 12;
        public const long MAXLAGNETS = 10;
        public const long MAXREGIONS = 100;
        public const long m_APS_NEXT_RESOURCE_VALUE = 102;
        public const long m_APS_NEXT_COMMAND_VALUE = 40001;
        public const long m_APS_NEXT_CONTROL_VALUE = 1001;
        public const long m_APS_NEXT_SYMED_VALUE = 101;
        public const long LARGESTVALUE = long.MaxValue;
        private readonly static long SqrtLV = (long)Math.Sqrt((double)LARGESTVALUE);
        private readonly static int LenSqrtLV = SqrtLV.ToString().Length;
        public readonly static long origDefMaxLinkCap = SqrtLV - SqrtLV % (long)Math.Pow(10.0, (double)(LenSqrtLV - 1)) - 1;
    }

    #endregion

    #region Enums

    public enum LinkSelectType : int
    {
        All = 0,
        Accrual = 1,
        Natural_Flow = 2,
        Storage = 3
    }
    public enum ModsimRunType : int
    {
        Explicit_Targets = 1,
        Conditional_Rules = 0
    }

    #endregion

    /// <summary>The Model Class contains all the data used by Modsim</summary>
    /// <remarks>This class is the main holder for the MODSIM network. It's used to pass all nodes, links, and network wide variables</remarks>
    public class Model
    {
        // Instance variables
        #region General Model information instance variables

        /// <summary>Used to pass error messsages when a function does not have access the main Model object.</summary>
        public static Model RefModel;
        /// <summary>Specifies the type of model run.</summary>
        /// <remarks>Hydrologic State Tables are used for Conditional Rules only.</remarks>
        public ModsimRunType runType;
        /// <summary>Dump output for all nodes (0) or only for selected nodes (1).</summary>
        public int iplot;
        /// <summary>The Title for this model.</summary>
        public string name;
        /// <summary>The XY File path and name.</summary>
        public string fname;
        /// <summary>Reference to the Model Information class structure</summary>
        public MinfoStr mInfo;
        
        public TimeSeriesInfo timeseriesInfo;

        #endregion
        #region GUI instance variables
        /// <summary>First X coordinate for the network extents</summary>
        public int dimensions_x1;
        /// <summary>First Y coordinate for the network extents</summary>
        public int dimensions_y1;
        /// <summary>Second X coordinate for the network extents</summary>
        public int dimensions_x2;
        /// <summary>Second Y coordinate for the network extents</summary>
        public int dimensions_y2;
        /// <summary>Pointer to the list of annotations on the network canvas</summary>
        public AnnotateList Annotations;
        /// <summary>GUI graphics data</summary>
        public GModel graphics;
        /// <summary>
        /// Scale factors to convert GUI X units to map units (for external GIS projection)
        /// </summary>
        public double geoScaleFactorX = 1.0;
        /// <summary>
        /// Scale factors to convert GUI Y units to map units (for external GIS projection)
        /// </summary>
        public double geoScaleFactorY = 1.0;
        #endregion
        #region Timeseries and Units instance variables

        /// <summary>Specifies information about the timestep of the Model.</summary>
        public ModsimTimeStep timeStep;
        /// <summary>Pointer to the simulation TimeManager data structure</summary>
        /// <remarks> TimeManager contains a table of beginning and ending dates as well as
        /// indecies for each time step of data and the simulation run</remarks>
        public TimeManager TimeStepManager;

        /// <summary>Specifies the types of units to use: Metric if true. English if false.</summary>
        public bool UseMetricUnits = false;
        /// <summary>The flow units that node dialogs start with.</summary>
        public ModsimUnits startingFlowUnits;
        /// <summary>The flow units that node dialogs start with.</summary>
        public ModsimUnits startingStorageUnits;
        /// <summary>Gets an array of timesteps used by MODSIM depending on <c>UseMetricUnits</c>.</summary>
        public string[] Labels_Timesteps
        {
            get
            {
                List<string> labels = new List<string>(ModsimTimeStep.GetLabels(this.UseMetricUnits, RemoveTypes.UserDefined));
                if (this.timeStep.TSType == ModsimTimeStepType.UserDefined)
                {
                    labels.Insert(0, this.timeStep.Label);
                    labels.Remove(ModsimTimeStep.DefaultUserDefLabel);
                }
                return labels.ToArray();
            }
        }
        /// <summary>Gets an array of timesteps used by MODSIM depending on <c>UseMetricUnits</c> without "seconds".</summary>
        public string[] Labels_TimestepsWoSeconds
        {
            get
            {
                List<string> labels = new List<string>(ModsimTimeStep.GetLabels(this.UseMetricUnits, RemoveTypes.Seconds));
                if (this.timeStep.TSType == ModsimTimeStepType.UserDefined)
                {
                    labels.Insert(0, this.timeStep.Label);
                    labels.Remove(ModsimTimeStep.DefaultUserDefLabel);
                }
                return labels.ToArray();

            }
        }
        /// <summary>Gets an array of volumetric units used by MODSIM depending on <c>UseMetricUnits</c>.</summary>
        public string[] Labels_VolumeUnits
        {
            get
            {
                if (this.UseMetricUnits)
                {
                    return ModsimUnits.GetLabels(ModsimUnitsType.Volume, UnitsSystemType.Metric);
                }
                else
                {
                    return ModsimUnits.GetLabels(ModsimUnitsType.Volume, UnitsSystemType.English);
                }
            }
        }
        /// <summary>Gets an array of area units used by MODSIM depending on <c>UseMetricUnits</c>.</summary>
        public string[] Labels_AreaUnits
        {
            get
            {
                if (this.UseMetricUnits)
                {
                    return ModsimUnits.GetLabels(ModsimUnitsType.Area, UnitsSystemType.Metric);
                }
                else
                {
                    return ModsimUnits.GetLabels(ModsimUnitsType.Area, UnitsSystemType.English);
                }
            }
        }
        /// <summary>Gets an array of length units used by MODSIM depending on <c>UseMetricUnits</c>.</summary>
        public string[] Labels_LengthUnits
        {
            get
            {
                if (this.UseMetricUnits)
                {
                    return ModsimUnits.GetLabels(ModsimUnitsType.Length, UnitsSystemType.Metric);
                }
                else
                {
                    return ModsimUnits.GetLabels(ModsimUnitsType.Length, UnitsSystemType.English);
                }
            }
        }
        /// <summary>Gets an array of time units used by MODSIM depending on <c>UseMetricUnits</c>.</summary>
        public string[] Labels_TimeUnits
        {
            get
            {
                if (this.UseMetricUnits)
                {
                    return ModsimUnits.GetLabels(ModsimUnitsType.Time, UnitsSystemType.Metric);
                }
                else
                {
                    return ModsimUnits.GetLabels(ModsimUnitsType.Time, UnitsSystemType.English);
                }
            }
        }
        /// <summary>Gets an array of power units used by MODSIM.</summary>
        public string[] Labels_EnergyUnits
        {
            get
            {
                return ModsimUnits.GetLabels(ModsimUnitsType.Energy, UnitsSystemType.Metric);
            }
        }
        /// <summary>Gets the water flow rate units used in MODSIM core routines.</summary>
        public ModsimUnits FlowUnits
        {
            get
            {
                return new ModsimUnits(this.StorageUnits.MajorUnits, this.timeStep);
            }
        }
        /// <summary>Gets the area rate units (e.g., used for transmissivity in groundwater modeling) in MODSIM core routines.</summary>
        public ModsimUnits AreaRateUnits
        {
            get
            {
                return new ModsimUnits(this.AreaUnits.MajorUnits, this.timeStep);
            }
        }
        /// <summary>Gets the length rate units used in MODSIM core routines.</summary>
        public ModsimUnits LengthRateUnits
        {
            get
            {
                return new ModsimUnits(this.LengthUnits.MajorUnits, this.timeStep);
            }
        }
        /// <summary>Gets the power units used in MODSIM core routines.</summary>
        public ModsimUnits PowerUnits
        {
            get
            {
                return ModsimUnits.GetDefaultUnits(ModsimUnitsType.EnergyRate, this.UseMetricUnits).Copy();
            }
        }
        /// <summary>Gets the time rate units used in MODSIM core routines.</summary>
        public ModsimUnits TimeRateUnits
        {
            get
            {
                return new ModsimUnits(this.TimeUnits.MajorUnits, this.timeStep);
            }
        }
        /// <summary>Gets the water storage units used in MODSIM core routines.</summary>
        public ModsimUnits StorageUnits
        {
            get
            {
                return ModsimUnits.GetDefaultUnits(ModsimUnitsType.Volume, this.UseMetricUnits).Copy();
            }
        }
        /// <summary>Gets the area units used in MODSIM core routines.</summary>
        public ModsimUnits AreaUnits
        {
            get
            {
                return ModsimUnits.GetDefaultUnits(ModsimUnitsType.Area, this.UseMetricUnits).Copy();
            }
        }
        /// <summary>Gets the length units used in MODSIM core routines.</summary>
        public ModsimUnits LengthUnits
        {
            get
            {
                return ModsimUnits.GetDefaultUnits(ModsimUnitsType.Length, this.UseMetricUnits).Copy();
            }
        }
        /// <summary>Gets the energy units used in MODSIM core routines.</summary>
        public ModsimUnits EnergyUnits
        {
            get
            {
                return ModsimUnits.GetDefaultUnits(ModsimUnitsType.Energy, this.UseMetricUnits).Copy();
            }
        }
        /// <summary>Gets the time units used in MODSIM core routines.</summary>
        public ModsimUnits TimeUnits
        {
            get
            {
                return ModsimUnits.GetDefaultUnits(ModsimUnitsType.Time, this.UseMetricUnits).Copy();
            }
        }
        /// <summary>Specifies whether the model should convert and fill TimeSeries just before running the solver.</summary>
        public bool ConvertUnitsAndFillTimeSeries;
        public void SetTargetContent(Node reservoir, int index, object value)
        {
            SetTargetContent(reservoir, index, Convert.ToInt64(value));
        }
        public void SetTargetContent(Node reservoir, int index, long value)
        {
            if (index < 0 || index >= reservoir.mnInfo.targetcontent.GetLength(0))
            {
                return;
            }
            for (int j = 0; j < reservoir.mnInfo.targetcontent.GetLength(1); j++)
            {
                reservoir.mnInfo.targetcontent[index, 0] = value;
            }
        }
        ///// <summary>Object to host the new timeseries at the model level.</summary>
        //public DataSet modelTimeSeries;
        /// <summary>
        /// Dictionary for central timeseries datasets.  
        /// This is designed to work wiht current timeseries for links and nodes.
        /// </summary>
        public Dictionary<string,DataSet> m_TimeSeriesTbls;
        /// <summary>
        /// Contains the name of the active time series scenario. For legacy networks the default is "Default" with an sceanrio ID = 0.
        /// The scenarios will be saved in the database and the scenario id will be used to name the database table.
        /// </summary>
        public string activeTSScenario;

        #endregion
        #region Model accuracy and solver parameter instance variables

        // Initial values
        /// <summary>Saves the initial priority to sort by</summary>
        public long WRinitialpriority;

        // Convergence properties
        /// <summary>User set maximum number of iterations</summary>
        public int maxit;
        /// <summary>Flag if true does not complain at maxit</summary>
        public int nomaxitmessage;
        /// <summary>GW convergence tolerance factor</summary>
        public double gw_cp;
        /// <summary>Flow Thru converence tolerance factor</summary>
        public double flowthru_cp;
        /// <summary>Storage evaporation convergence tolerance factor</summary>
        public double evap_cp;
        /// <summary>Restart on infeasible solution</summary>
        public short infeasibleRestart;

        // Model Accuracy
        /// <summary>The accuracy of data in terms of the number of digits after the decimal point. If accuracy = 3, then expect an accuracy in the data of about 0.000</summary>
        public int accuracy;
        /// <summary>The number that all data will be multiplied by when scaled to achieved the specified 'accuracy' in terms of the number of digits after the decimal point.</summary>
        public double ScaleFactor
        {
            get
            {
                return CalcScaleFactor();
            }
        }
        /// <summary>Calculates ScaleFactor given the specified 'Model.accuracy'.</summary>
        /// <returns>Returns the calculated ScaleFactor.</returns>
        /// <remarks>This method also sets Model.ScaleFactor like so: ScaleFactor = (long)Math.Pow(10.0, accuracy);</remarks>
        public long CalcScaleFactor()
        {
            return CalcScaleFactor(this.accuracy);
        }
        /// <summary>Calculates ScaleFactor given the specified 'digAfterDecPoint'.</summary>
        /// <param name="digAfterDecPoint">Specifies the number of digits after the decimal place.</param>
        /// <returns>Returns the ScaleFactor like so: (long)Math.Pow(10.0, digAfterDecPoint);</returns>
        /// <remarks>Does not set Model.ScaleFactor... Unlike the overloaded method CalcScaleFactor()</remarks>
        public long CalcScaleFactor(int digAfterDecPoint)
        {
            return (long)Math.Pow(10.0, digAfterDecPoint);
        }

        #endregion
        #region Hydrologic State instance variables
        /// <summary>Array of hydrologic state tables</summary>
        public HydrologicStateTable[] HydStateTables;
        /// <summary>OLD xy file support number of hydrologic tables</summary>
        /// <remarks>WARNING this is barely used and should go away...</remarks>
        public int numHydTables;
        /// <summary>Flag to indicate if hydrologic table factors are constant through time</summary>
        public int constMonthly;
        #endregion
        #region Nodes and links instance variables and properties

        /// <summary>Specifies whether the MODSIM network is set for runtime.</summary>
        public bool NetworkIsSetForRuntime = false;
        /// <summary>Specifies whether the MODSIM network will generate spill links for reservoir nodes.</summary>
        public bool IncludeSpillLinks = true;
        /// <summary>returns number of real nodes in the data set</summary>
        public int NodeCount
        {
            get
            {
                return real_nodes;
            }
        }
        /// <summary>returns number of real links in the data set</summary>
        public int LinkCount
        {
            get
            {
                return real_links;
            }
        }
        /// <summary>number of real nodes in the network</summary>
        public int real_nodes;
        /// <summary>number of real links in the network</summary>
        public int real_links;
        /// <summary>Pointer to the network's first node</summary>
        public Node firstNode;
        /// <summary>Pointer to the network's first link</summary>
        public Link firstLink;
        /// <summary>Gets an array of all nodes sorted by the node number.</summary>
        public Node[] Nodes_All
        {
            get
            {
                List<Node> list = new List<Node>();
                for (Node n = this.firstNode; n != null; n = n.next)
                {
                    list.Add(n);
                }
                list.Sort();
                return list.ToArray();
            }
        }
        /// <summary>Gets an array of all reservoir nodes sorted by the node number.</summary>
        public Node[] Nodes_Reservoirs
        {
            get
            {
                List<Node> list = new List<Node>();
                for (Node n = this.firstNode; n != null; n = n.next)
                {
                    if (n.nodeType == NodeType.Reservoir)
                    {
                        list.Add(n);
                    }
                }
                list.Sort();
                return list.ToArray();
            }
        }
        /// <summary>Gets an array of all demand nodes sorted by the node number.</summary>
        public Node[] Nodes_Demand
        {
            get
            {
                List<Node> list = new List<Node>();
                for (Node n = this.firstNode; n != null; n = n.next)
                {
                    if (n.nodeType == NodeType.Demand)
                    {
                        list.Add(n);
                    }
                }
                list.Sort();
                return list.ToArray();
            }
        }
        /// <summary>Gets an array of all NOnStorage nodes sorted by the node number.</summary>
        public Node[] Nodes_NonStorage
        {
            get
            {
                List<Node> list = new List<Node>();
                for (Node n = this.firstNode; n != null; n = n.next)
                {
                    if (n.nodeType == NodeType.NonStorage)
                    {
                        list.Add(n);
                    }
                }
                list.Sort();
                return list.ToArray();
            }
        }
        /// <summary>Gets an array of all sink nodes sorted by the node number.</summary>
        public Node[] Nodes_Sink
        {
            get
            {
                List<Node> list = new List<Node>();
                for (Node n = this.firstNode; n != null; n = n.next)
                {
                    if (n.nodeType == NodeType.Sink)
                    {
                        list.Add(n);
                    }
                }
                list.Sort();
                return list.ToArray();
            }
        }
        /// <summary>Gets an array of all links sorted by the link number.</summary>
        public Link[] Links_All
        {
            get
            {
                List<Link> list = new List<Link>();
                for (Link l = this.firstLink; l != null; l = l.next)
                {
                    list.Add(l);
                }
                list.Sort();
                return list.ToArray();
            }
        }
        /// <summary>Gets an array of all artificial links sorted by the link number (only works during simulation).</summary>
        public Link[] Links_Artificial
        {
            get
            {
                List<Link> list = new List<Link>();
                for (Link l = this.firstLink; l != null; l = l.next)
                {
                    if (l.mlInfo != null && l.mlInfo.isArtificial)
                    {
                        list.Add(l);
                    }
                }
                list.Sort();
                return list.ToArray();
            }
        }
        /// <summary>Gets an array of all real links sorted by the link number.</summary>
        public Link[] Links_Real
        {
            get
            {
                List<Link> list = new List<Link>();
                for (Link l = this.firstLink; l != null; l = l.next)
                {
                    if (l.mlInfo == null || !l.mlInfo.isArtificial)
                    {
                        list.Add(l);
                    }
                }
                list.Sort();
                return list.ToArray();
            }
        }
        /// <summary>Number of nodes (real and artificial) +1</summary>
        public int NextNodeNum;
        /// <summary>Number of links (real and artificial) +1</summary>
        public int NextLinkNum;
        /// <summary>The default maximum capacity for the model.</summary>
        private long DefMaxLinkCap = DefineConstants.origDefMaxLinkCap;
        /// <summary>The default maximum capacity for all links (real and artificial).</summary>
        public long defaultMaxCap
        {
            get
            {
                return DefMaxLinkCap;
            }
            set
            {
                DefMaxLinkCap = value;
            }
        }
        /// <summary>Gets a larger value than default maximum link capacity to check link capacities against.</summary>
        public long defaultMaxCap_Super
        {
            get
            {
                return Math.Min(long.MaxValue, (this.DefMaxLinkCap + 1) * 3 - 1);
            }
        }
        /// <summary>Linked list of all routing links in the network<summary>
        public LinkList routingLinks;

        #endregion
        #region Routing instance variables
        // nlags, maxLags, MAXLAG all these should go away; everthing SHOULD be dynamic; not quite there yet
        /// <summary>Represents maximum number of infiltration, routing, depletion lags for the entire network; read in from XYFile</summary>
        /// <remarks> min of nlags and MAXLAGS gets set to maxLags;  nlags is used for shifting link routing flows after convergence</remarks>
        public int nlags;
        /// <summary>Zero for model generated lag coefficients, 1 for user specified</summary>
        public int useLags;
        /// <summary>Minimum of nlags (read in from XYfile) or MAXLAGS</summary>
        /// <remarks> maxLags is used in gwater for computing routed links mrlInfo->linkPrevflow loops</remarks>
        public int maxLags;
        #endregion
        #region Backrouting instance variables
        /// <summary>Flag that indicates the network is to solve backrouting solution</summary>
        public bool backRouting;
        /// <summary>Flag that indicates the network is to solve backrouting with storage accounts</summary>
        public bool storageAccountsWithBackRouting;
        #endregion
        #region Water and storage rights instance variables

        /// <summary>Flag if true "relax" the accrual restrictions; don't restrict to space availability</summary>
        public short relaxAccrual;
        /// <summary>Date of the year (after convergence) when the next accrual season starts</summary>
        /// <remarks> Preownaccrual is set to stglft; accrual for the next season is set to the "bank account"
        /// storage left for each contract</remarks>
        public DateTime accrualDate;
        /// <summary>List of dates when the rent pool routine is called each year</summary>
        /// <remarks> During the first rent pool date after accrualDate, any unused rent pool water
        /// is given back to the original contributors
        /// NOTE rent pool is called before the iteration sequence</remarks>
        public DateList rentPoolDates;
        /// <summary>List of dates when (after convergence) the sum of "paper" accounts for a reservoir
        /// system are adjusted to match physical water</summary>
        public DateList accBalanceDates;
        /// <summary>Date of year when lnktot for links with seasonal capacity is reset to zero</summary>
        public DateTime seasCapDate;
        /// <summary>Specifies whether the model has owner links.</summary>
        public bool HasOwnerLinks;
        /// <summary>Specifies whether the model has rent links.</summary>
        public bool HasRentLinks;
        /// <summary>Flag to allow the last fill rent pool data structures to be active</summary>
        public bool ExtLastFillRentActive;
        /// <summary>Flag to allow storage allocation constructs to be created and solved</summary>
        public bool ExtStorageRightActive;
        /// <summary>Flag to indicate that bypass and outflow links are constructed manually for
        ///  account reservoirs</summary>
        public bool ExtManualStorageRightActive;
        /// <summary>Flag to allow access to water rights tools</summary>
        public bool ExtWaterRightsActive; // Interface flag to display Water Rights info (Not used in the model)
        #endregion
        #region Input/Output instance variables
        /// <summary>USED ONLY IN XYFile stuff SHOULD NOT BE USED - WILL GO AWAY</summary>
        public int Nyears;
        /// <summary>Pointer to the output selection data structure</summary>
        public OutputControlInfo controlinfo;
        /// <summary>Tag to hold the modelOutputSupport Class used in operate to write the model output.</summary>
        /// <remarks> This object is designed to be used in the custom code.  It's initialized when model runs (It's first available in OnIntiailize()) </remarks>
        public object OutputSupportClass;
        /// <summary>Specifies the input version of the model.</summary>
        public InputVersion inputVersion;
        /// <summary>Specifies the output version of the model.</summary>
        public OutputVersion outputVersion;
        #endregion
        #region Hydropower variables

        /// <summary>Holds collections of objects, IDs, and names</summary>
        public ModsimCollection PowerObjects = new ModsimCollection();
        /// <summary>Controls hydropower calculations and power demands.</summary>
        public HydropowerController hydro;
        /// <summary>Gets whether this model has hydropower defined within any of its reservoir nodes.</summary>
        public bool HasOldHydropowerDefined
        {
            get
            {
                foreach (Node n in this.Nodes_Reservoirs)
                {
                    if (n.HasOldHydropowerDefined)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion

        // Constructor and copying methods
        #region Constructor Methods

        /// <summary>Model Constructor</summary>
        public Model()
        {
            // Reference to the first instance of the model class.
            if (RefModel == null)
            {
                RefModel = this;
            }
            Defaults();
            controlinfo = new OutputControlInfo();
            Annotations = new AnnotateList();
            graphics = new GModel();
            this.hydro = new HydropowerController(this);
            //modelTimeSeries = new DataSet();
            
            m_TimeSeriesTbls = new Dictionary<string, DataSet>();
            ////model timeseries
            //modelTimeSeries = InitializeNewTimeseries();
            timeseriesInfo = new TimeSeriesInfo("");
        }
        /// <summary>Sets all of the model struct's default values.</summary>
        private void Defaults()
        {
            // Model descriptions
            fname = "Untitled";
            name = "BASIC TITLE";
            // empty model run structures
            mInfo = null;
            // data
            accuracy = 0;
            iplot = 0;
            UseMetricUnits = false;
            // graphics
            dimensions_x1 = dimensions_y1 = 0;
            dimensions_x2 = 10 * 1024 * 2;
            dimensions_y2 = 900 * 3;
            // empty network
            real_nodes = 0;
            real_links = 0;
            firstNode = null;
            firstLink = null;
            NextNodeNum = 1;
            NextLinkNum = 1;
            // Time steps
            Nyears = 1; // Used in XYFile reader stuff only, should go away
            timeStep = ModsimUnits.DefaultTimeStep;
            TimeStepManager = new TimeManager();
            TimeStepManager.startingDate = new DateTime(1980, 1, 1);
            TimeStepManager.dataStartDate = new DateTime(1980, 1, 1);
            TimeStepManager.dataEndDate = new DateTime(1981, 1, 1);
            TimeStepManager.endingDate = new DateTime(1981, 1, 1);
            TimeStepManager.UpdateTimeStepsInfo(timeStep);
            // Units
            startingFlowUnits = FlowUnits;
            startingStorageUnits = StorageUnits;
            ConvertUnitsAndFillTimeSeries = true;
            // Storage allocation dates
            accrualDate = new DateTime(1980, 1, 1);
            seasCapDate = new DateTime(1980, 1, 1);
            rentPoolDates = new DateList();
            accBalanceDates = new DateList();
            // lag coeffecients
            maxLags = 0;
            nlags = 5;
            useLags = 1;
            // Hydrologic State
            runType = ModsimRunType.Explicit_Targets;
            constMonthly = 0;
            numHydTables = 1;
            HydStateTables = new HydrologicStateTable[0];
            // convergence
            maxit = 100;
            nomaxitmessage = 0;
            gw_cp = 0.05;
            flowthru_cp = 0.00005;
            evap_cp = 0.01;
            infeasibleRestart = 0;
            // Option flags
            ExtStorageRightActive = false;
            ExtManualStorageRightActive = false;
            ExtWaterRightsActive = false;
            ExtLastFillRentActive = false;
            relaxAccrual = 0;
            backRouting = false;
            HasOwnerLinks = false;
            HasRentLinks = false;
            //  timeseries
            timeseriesInfo = new TimeSeriesInfo("");
            
        }

        ///// <summary>Initialize table for combined timeseries implementation.</summary>
        //private DataSet InitializeNewTimeseries()
        //{
        //    string[] tables = new string[] {"TS_Observations"};
        //    DataSet DTbls = new DataSet();
        //    for (int i = 0; i < tables.Length; i++)
        //    {
        //        DataTable DTbl = new DataTable(tables[i]);
        //        DTbl.Columns.Add("Date", Type.GetType("System.DateTime"));
        //        DTbl.PrimaryKey = new DataColumn[] {DTbl.Columns[0]};
        //        DTbls.Tables.Add(DTbl);
        //    }
        //    DataTable DTblInfo= new DataTable("LinksTSInfo");
        //    DTblInfo.Columns.Add("Node", typeof(string));
        //    DTblInfo.Columns.Add("Units", typeof(string));
        //    DTblInfo.Columns.Add("VariesByYear", typeof(bool));
        //    DTblInfo.Columns.Add("Interpolate", typeof(bool));
        //    DTblInfo.PrimaryKey = new DataColumn[] { DTblInfo.Columns[0] };
        //    DTbls.Tables.Add(DTblInfo);
        //    return DTbls;
        //}

        #endregion
        #region Duplicating methods
        /// <summary>Creates a complete copy of this instance of the Model class.</summary>
        /// <returns>Returns the copy of the current instance of the Model class.</returns>
        public Model Clone()
        {
            return Clone(true);
        }
        /// <summary>Creates a copy of this instance of the Model class.</summary>
        /// <param name="custom">Specifies whether to use custom data processing, or .Net Clone() classes to create the clone.</param>
        /// <returns>Returns the copy of the current instance of the Model class.</returns>
        public Model Clone(bool custom)
        {
            Link l;
            Link l2;
            Node n;
            Node n2;
            int i;
            LagInfo lInfo;
            int j;
            Node lastN;
            Link lastL;
            LinkList ll;
            LinkList ll2;
            LinkList ll2tmp;
            LinkList ll2head;

            Model dupModel = new Model();
            dupModel.name = this.name;

            // Find the last node
            lastN = null;
            for (n = this.firstNode; n != null; n = n.next)
            {
                lastN = n;
            }
            lastL = null;
            for (l = this.firstLink; l != null; l = l.next)
            {
                lastL = l;
            }
            // Nodes are always first
            for (n = lastN; n != null; n = n.prev)
            {
                n2 = dupModel.AddNewNode(true);
                if (custom)
                {
                    n2.CopyData(n);
                    n2.number = n.number;
                    n2.m.selected = n.m.selected;
                    n2.name = string.Copy(n.name);
                    n2.description = string.Copy(n.description);
                    n2.nodeType = n.nodeType;
                    n2.backRRegionID = n.backRRegionID;
                    n2.parentFlag = n.parentFlag;
                    n2.numChildren = n.numChildren;
                    n2.mnInfo = new MnInfo();
                    n2.myMother = n.myMother;
                    n2.RESnext = n.RESnext;
                    n2.RESprev = n.RESprev;
                }
                else
                {
                    // n2 = n.Copy();
                }
            }
            for (l = this.firstLink; l != null; l = l.next)
            {
                l2 = dupModel.AddNewLink(true);
                if (custom)
                {
                    l2.CopyData(l);
                    l2.number = l.number;
                    l2.name = string.Copy(l.name);
                    l2.m.selected = l.m.selected;
                    l2.mlInfo = new MlInfo();
                }
                else
                {
                    // l2 = l.Copy();
                }
            }
            // Clean up the in and out link connections
            for (n = lastN; n != null; n = n.prev)
            {
                n2 = dupModel.FindNode(n.number);
                ll2 = null;
                ll2tmp = null;
                ll2head = null;
                for (ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    ll2tmp = ll2;
                    ll2 = new LinkList();
                    ll2.next = null;
                    if (ll2tmp != null)
                    {
                        ll2tmp.next = ll2;
                    }
                    else
                    {
                        ll2head = ll2;
                    }
                    ll2.link = dupModel.FindLink(ll.link.number);
                }
                n2.InflowLinks = ll2head;
                ll2 = null;
                ll2tmp = null;
                ll2head = null;
                for (ll = n.OutflowLinks; ll != null; ll = ll.next)
                {
                    ll2tmp = ll2;
                    ll2 = new LinkList();
                    ll2.next = null;
                    if (ll2tmp != null)
                    {
                        ll2tmp.next = ll2;
                    }
                    else
                    {
                        ll2head = ll2;
                    }
                    ll2.link = dupModel.FindLink(ll.link.number);
                }
                n2.OutflowLinks = ll2head;
            }
            for (l = this.firstLink; l != null; l = l.next)
            {
                l2 = dupModel.FindLink(l.number);
                l2.to = dupModel.FindNode(l.to.number);
                l2.from = dupModel.FindNode(l.from.number);
            }

            //// Clone all value instance variables
            //if (!custom)
            //    dupModel = (Model)this.MemberwiseClone();

            // Change the references
            for (l = dupModel.firstLink; l != null; l = l.next)
            {
                FixLinkReference(dupModel, ref (l.m.accrualLink));
                FixLinkReference(dupModel, ref (l.m.exchangeLimitLinks));
                FixLinkReference(dupModel, ref (l.m.linkConstraintUPS));
                FixLinkReference(dupModel, ref (l.m.linkConstraintDWS));
                FixLinkReference(dupModel, ref (l.m.linkChannelLoss));
                FixNodeReference(dupModel, ref (l.m.returnNode));
            }
            for (n = dupModel.firstNode; n != null; n = n.next)
            {
                FixLinkReference(dupModel, ref (n.m.lastFillLink));
                FixLinkReference(dupModel, ref (n.m.pdstrm));
                FixLinkReference(dupModel, ref (n.m.resBypassL));
                FixLinkReference(dupModel, ref (n.m.resOutLink));
                for (i = 0; i < 15; i++)
                {
                    FixLinkReference(dupModel, ref (n.m.watchExpLinks[i]));
                    FixLinkReference(dupModel, ref (n.m.watchLnLinks[i]));
                    FixLinkReference(dupModel, ref (n.m.watchLogLinks[i]));
                    FixLinkReference(dupModel, ref (n.m.watchMaxLinks[i]));
                    FixLinkReference(dupModel, ref (n.m.watchMinLinks[i]));
                    FixLinkReference(dupModel, ref (n.m.watchPowLinks[i]));
                    FixLinkReference(dupModel, ref (n.m.watchSqrLinks[i]));
                }
                // Nodes
                FixNodeReference(dupModel, ref (n.myMother));
                FixNodeReference(dupModel, ref (n.RESnext));
                FixNodeReference(dupModel, ref (n.RESprev));
                FixNodeReference(dupModel, ref (n.m.demDirect));
                for (i = 0; i < 10; i++)
                {
                    FixNodeReference(dupModel, ref (n.m.idstrmx[i]));
                }
                FixNodeReference(dupModel, ref (n.m.jdstrm));
                for (lInfo = n.m.infLagi; lInfo != null; lInfo = lInfo.next)
                {
                    FixNodeReference(dupModel, ref (lInfo.location));
                }
            }

            dupModel.HydStateTables = this.HydStateTables;
            dupModel.HydStateTables = new HydrologicStateTable[this.HydStateTables.Length];
            for (i = 0; i < this.HydStateTables.Length; i++)
            {
                dupModel.HydStateTables[i] = new HydrologicStateTable();
                dupModel.HydStateTables[i].Reservoirs = new Node[this.HydStateTables[i].NumReservoirs];
                int numstates = this.HydStateTables[i].NumHydBounds + 1;
                int numdates = this.HydStateTables[i].hydDates.Count;
                for (j = 0; j < dupModel.HydStateTables[i].NumReservoirs; j++)
                {
                    dupModel.HydStateTables[i].Reservoirs[j] = dupModel.FindNode(this.HydStateTables[i].Reservoirs[j].number);
                }
                dupModel.HydStateTables[i].hydBounds = new double[numstates - 1, numdates];
                for (int _state = 0; _state < numstates - 1; _state++)
                {
                    for (int _month = 0; _month < numdates; _month++)
                    {
                        dupModel.HydStateTables[i].hydBounds[_state, _month] = this.HydStateTables[i].hydBounds[_state, _month];
                    }
                }
            }

            dupModel.UseMetricUnits = this.UseMetricUnits;
            if (this.timeStep != null)
            {
                dupModel.timeStep = this.timeStep.Copy();
            }
            dupModel.ConvertUnitsAndFillTimeSeries = this.ConvertUnitsAndFillTimeSeries;
            dupModel.runType = this.runType;
            dupModel.iplot = this.iplot;
            dupModel.real_nodes = this.real_nodes;
            dupModel.Nyears = this.Nyears;
            dupModel.nlags = this.nlags;
            dupModel.useLags = this.useLags;
            dupModel.dimensions_x1 = this.dimensions_x1;
            dupModel.dimensions_y1 = this.dimensions_y1;
            dupModel.dimensions_x2 = this.dimensions_x2;
            dupModel.dimensions_y2 = this.dimensions_y2;
            dupModel.maxLags = this.maxLags;
            dupModel.maxit = this.maxit;
            dupModel.accuracy = this.accuracy;
            // Defaults
            dupModel.defaultMaxCap = this.defaultMaxCap;
            // Names
            dupModel.controlinfo = this.controlinfo.Copy();
            // Events

            // Time:
            dupModel.TimeStepManager.dataStartDate = this.TimeStepManager.dataStartDate;
            dupModel.TimeStepManager.startingDate = this.TimeStepManager.startingDate;
            dupModel.TimeStepManager.endingDate = this.TimeStepManager.endingDate;
            dupModel.TimeStepManager.dataEndDate = this.TimeStepManager.dataEndDate;
            dupModel.TimeStepManager.UpdateTimeStepsInfo(this.timeStep);
            dupModel.backRouting = this.backRouting;

            // Power
            this.PowerObjects.Copy(dupModel);
            dupModel.hydro = this.hydro.Copy(dupModel);

            return dupModel;
        }
        #endregion

        // Other methods
        #region Nodes and links methods

        /// <summary>Ensures that the specified Link reference is pointed to by the specified Model.</summary>
        /// <param name="model">The Model in which the link should be referenced.</param>
        /// <param name="l">The Link to which the specified Model should be referencing.</param>
        public static void FixLinkReference(Model model, ref Link l)
        {
            if (l != null)
            {
                l = model.FindLink(l.number);
            }
        }
        /// <summary>Ensures that the specified Node reference is pointed to by the specified Model.</summary>
        /// <param name="model">The Model in which the link should be referenced.</param>
        /// <param name="n">The Node to which the specified Model should be referencing.</param>
        public static void FixNodeReference(Model model, ref Node n)
        {
            if (n != null)
            {
                n = model.FindNode(n.number);
            }
        }
        /// <summary>Adds a link to the network AFTER memory has been allocated</summary>
        /// <remarks>This function adds the specified Link to the network.  If the isRealLink flag is set, the real_links Link counter is updated. This function is called from AddNewLink which allocates memory for the new link before this function is called; so you could do the memory allocation in some other code and call this function directly.</remarks>
        public void AddLink(Link l, bool isRealLink)
        {
            Link prev;

            if (firstLink == null)
            {
                firstLink = l;
            }
            else
            {
                prev = firstLink;
                while (prev.next != null)
                {
                    prev = prev.next;
                }
                prev.next = l;
                l.prev = prev;
            }
            l.number = NextLinkNum;
            if (isRealLink)
            {
                real_links++;
            }
            NextLinkNum += 1;
        }
        /// <summary>Add a new link to the network</summary>
        /// <remarks> This function allocates memory for the new link, calls AddLink to add it to the network, and returns the address of the new link. If the isRealLink flag is set, the real_links Link counter is updated.  If an error occurs, an error message is set and NULL is returned.</remarks>
        public Link AddNewLink(bool isRealLink)
        {
            Link l = new Link();
            AddLink(l, isRealLink);
            return l;
        }
        /// <summary>Add a new real link between the specified nodes</summary>
        /// <remarks>AddNewRealLink is used to add a new link and attach it to the network.</remarks>
        public Link AddNewRealLink(Node fromNode, Node toNode)
        {
            Link newLink = new Link();
            newLink.to = toNode;
            newLink.from = fromNode;

            AddLink(newLink, true);
            UpdateOutIn(fromNode, toNode, newLink);
            return newLink;
        }
        /// <summary>Add real link</summary>
        /// <remarks>AddRealLink is used to add a user created link and attach it to the network.</remarks>
        public void AddRealLink(Link link)
        {
            AddLink(link, true);
            UpdateOutIn(link.from, link.to, link);
        }
        private void UpdateOutIn(Node fromNode, Node toNode, Link link)
        {
            // update outflow links for fromNode
            if (fromNode.OutflowLinks == null)
            {
                fromNode.OutflowLinks = new LinkList();
                fromNode.OutflowLinks.link = link;
            }
            else
            {
                fromNode.AddOut(link);
            }
            // update inflow links for toNode
            if (toNode.InflowLinks == null)
            {
                toNode.InflowLinks = new LinkList();
                toNode.InflowLinks.link = link;
            }
            else
            {
                toNode.AddIn(link);
            }
        }
        /// <summary>Allocates memory, calls AddNode, returns address of new node</summary>
        /// <remarks>This function allocates a new Node object, adds it to the network, and returns its address. If the isRealNode flag is set, the real_nodes Node counter is updated. If an error occurs, an error message is set and NULL is returned.</remarks>
        public Node AddNewNode(bool isRealNode)
        {
            Node newnode = new Node();
            AddNode(newnode, isRealNode);
            return newnode;
        }
        /// <summary>Adds new node to the network.</summary>
        /// <remarks>If the isRealNode flag is set, the real_nodes Node counter is updated.</remarks>
        public void AddNode(Node newnode, bool isRealNode)
        {
            newnode.next = firstNode;
            firstNode = newnode;
            if (firstNode.next != null)
            {
                firstNode.next.prev = firstNode;
            }
            firstNode.prev = null;
            newnode.number = NextNodeNum;
            NextNodeNum += 1;
            if (isRealNode)
            {
                real_nodes++;
            }
        }
        /// <summary>return the link specified by name</summary>
        public Link FindLink(string name, bool silent = false)
        {
            for (Link l = firstLink; l != null; l = l.next)
            {
                if (l.name.Equals(name))
                {
                    return l;
                }
            }

            if (!silent)
            {
                FireOnError(string.Concat("Could not find link ", name));
            }

            return null;
        }
        /// <summary>return the link specified by number</summary>
        public Link FindLink(int number, bool silent = false)
        {
            for (Link l = firstLink; l != null; l = l.next)
            {
                if (l.number == number)
                {
                    return l;
                }
            }

            if (!silent)
            {
                FireOnError(string.Concat("Could not find link ", number.ToString()));
            }

            return null;
        }
        /// <summary>return the link specified by unique identifier</summary>
        public Link FindLink(Guid uid, bool silent = false)
        {
            for (Link l = firstLink; l != null; l = l.next)
            {
                if (l.uid == uid)
                {
                    return l;
                }
            }

            if (!silent)
            {
                FireOnError(string.Concat("Could not find link ", uid.ToString()));
            }
            return null;
        }

        /// <summmary>link name exists in model</summmary>
        public bool LinkNameExists(string name, bool silent = false)
        {
            return (FindLink(name, silent) != null);
        }
        /// <summmary>link number exists in model</summmary>
        public bool LinkNumberExists(int number, bool silent = false)
        {
            return (FindLink(number, silent) != null);
        }
        /// <summary>return the node specified by name</summary>
        public Node FindNode(string name, bool silent = false)
        {
            for (Node n = firstNode; n != null; n = n.next)
            {
                if (n.name != null && n.name.Equals(name))
                {
                    return n;
                }
            }

            if (!silent)
            {
                FireOnError(string.Concat("Could not find node ", name));
            }

            return null;
        }
        /// <summary>return the node specified by number</summary>
        public Node FindNode(int number, bool silent = false)
        {
            for (Node n = firstNode; n != null; n = n.next)
            {
                if (n.number == number)
                {
                    return n;
                }
            }

            if (!silent)
            {
                FireOnError(string.Concat("Could not find node ", number.ToString()));
            }

            return null;
        }
        /// <summary>return the node specified by unique identifier</summary>
        public Node FindNode(Guid uid, bool silent = false)
        {
            for (Node n = firstNode; n != null; n = n.next)
            {
                if (n.uid == uid)
                {
                    return n;
                }
            }

            if (!silent)
            {
                FireOnError(string.Concat("Could not find node ", uid.ToString()));
            }

            return null;
        }

        /// <summmary>node name exists in model</summmary>
        public bool NodeNameExists(string name, bool silent = false)
        {
            return (FindNode(name, silent) != null);
        }
        /// <summmary>node number exists in model</summmary>
        public bool NodeNumberExists(int number, bool silent = false)
        {
            return (FindNode(number, silent) != null);
        }
        /// <summary>Finds all links parallel to the specified link and returns an array containing all parallel links along with the specified link (unless specified to not include).</summary>
        /// <param name="l">The specified link for which to find parallel neighbors.</param>
        public Link[] FindParallelLinks(Link l)
        {
            return FindParallelLinks(l, true);
        }
        /// <summary>Finds all links parallel to the specified link and returns an array containing all parallel links along with the specified link (unless specified to not include).</summary>
        /// <param name="l">The specified link for which to find parallel neighbors.</param>
        public Link[] FindParallelLinks(Link l, bool includeLinkL)
        {
            if (l == null)
            {
                return null;
            }
            List<Link> list = new List<Link>();
            for (LinkList ll = l.from.OutflowLinks; ll != null; ll = ll.next)
            {
                if (ll.link.to == l.to)
                {
                    if (ll.link != l || includeLinkL)
                    {
                        list.Add(ll.link);
                    }
                }
            }
            return list.ToArray();
        }
        public int GetNumLinksUsingHydTable(int index)
        {
            int num = 0;

            for (Link l = firstLink; l != null; l = l.next)
            {
                if (l.m.hydTable == index)
                {
                    num++;
                }
            }

            return num;
        }
        public int GetNumNodesUsingHydTable(int index)
        {
            int num = 0;

            for (Node n = firstNode; n != null; n = n.next)
            {
                if (n.m.hydTable == index)
                {
                    num++;
                }
            }

            return num;
        }
        /// <summary>Returns true if the specified link is successfully removed from the network</summary>
        /// <remarks>This function removes the specified Link from the Model's list of Links, and sets any pointers in the network to NULL if they pointed to this Link.  Note that this function does NOT free the Link from memory - it detaches all connections to the Link in the network.  Any Links whose numbers are greater than the Link being removed get their number decremented by one in this function.  If the Link is successfully removed, TRUE is returned.  Otherwise, an error message is set and FALSE is returned.</remarks>
        public bool Remove(Link Link1, bool isRealLink)
        {
            Link holdlink;
            Link holdback;
            int deletedlink;
            int i;
            // Do some preliminary work...
            if (Link1 == null)
            {
                FireOnError("Attempted to remove a NULL link from Model.");
                return false;
            }
            // Disconnect the Link from its from and to node...
            if (!Link1.Disconnect())
            {
                return false;
            }
            deletedlink = Link1.number;
            if (firstLink == Link1)
            {
                firstLink = firstLink.next;
            }
            else
            {
                for (holdlink = firstLink.next, holdback = firstLink; holdlink != null;)
                {
                    if (holdlink == Link1)
                    {
                        holdback.next = holdlink.next;
                        holdlink = holdlink.next;
                    }
                    else
                    {
                        holdback = holdlink;
                        holdlink = holdlink.next;
                    }
                }
            }
            for (holdlink = firstLink; holdlink != null; holdlink = holdlink.next)
            {
                if (holdlink.number > deletedlink)
                {
                    holdlink.number--;
                }
            }
            for (Link lh = firstLink; lh != null; lh = lh.next)
            {
                // Update all of the lnktotadd numbers.
                if (lh.m.exchangeLimitLinks == Link1)
                {
                    lh.m.exchangeLimitLinks = null;
                }

                if (lh.m.accrualLink == Link1)
                {
                    lh.m.accrualLink = null;
                }
            }
            for (Node np = firstNode; np != null; np = np.next)
            {
                if (np.m.pdstrm == Link1)
                {
                    np.m.pdstrm = null;
                }
                if (np.m.resBypassL == Link1)
                {
                    np.m.resBypassL = null;
                }
                if (np.m.resOutLink == Link1)
                {
                    np.m.resOutLink = null;
                }
                if (np.m.lastFillLink == Link1)
                {
                    np.m.lastFillLink = null;
                }
                for (i = 0; i < 15; i++)
                {
                    if (np.m.watchMaxLinks[i] == Link1)
                    {
                        np.m.watchMaxLinks[i] = null;
                    }
                    if (np.m.watchMinLinks[i] == Link1)
                    {
                        np.m.watchMinLinks[i] = null;
                    }
                    if (np.m.watchLnLinks[i] == Link1)
                    {
                        np.m.watchLnLinks[i] = null;
                    }
                    if (np.m.watchLogLinks[i] == Link1)
                    {
                        np.m.watchLogLinks[i] = null;
                    }
                    if (np.m.watchExpLinks[i] == Link1)
                    {
                        np.m.watchExpLinks[i] = null;
                    }
                    if (np.m.watchSqrLinks[i] == Link1)
                    {
                        np.m.watchSqrLinks[i] = null;
                    }
                }
            }
            // Update the Link counters...
            NextLinkNum--;

            if (isRealLink)
            {
                real_links--;
            }

            return true;
        }
        /// <summary>Returns true if the specified node is successfully removed from the network</summary>
        /// <remarks>This function removes the specified Node from the Model's list of Nodes, and sets any pointers in the network to NULL if they pointed to this Node.  Note that this function does NOT free the Node from memory - it detaches all connections to the Node in the network.  Any Nodes whose numbers are greater than the Node being removed get their number decremented by one in this function.  If the Node is successfully removed, TRUE is returned.  Otherwise, an error message is set and FALSE is returned.</remarks>
        public bool Remove(Node node, bool isRealNode)
        {
            Link hold;
            Node nodehold;
            Node backp;
            Link lh;
            int i;
            LagInfo lagInfo;
            LagInfo nextLagInfo;
            int deletednum;
            // Do some preliminary stuff...
            if (node == null)
            {
                FireOnError("Attempted to remove a NULL node from Model.");
                return false;
            }
            deletednum = node.number;
            // Destroy all links that are connected to this node.
            for (hold = firstLink; hold != null;)
            {
                if (hold.from == node)
                {
                    Remove(hold, isRealNode);
                    hold = firstLink;
                }
                else
                {
                    hold = hold.next;
                }
            }
            for (hold = firstLink; hold != null;)
            {
                if (hold.to == node)
                {
                    Remove(hold, isRealNode);
                    hold = firstLink;
                }
                else
                {
                    hold = hold.next;
                }
            }
            // If this node is a reservoir, remove its children and some other stuff...
            if (node.nodeType == NodeType.Reservoir)
            {
                if (!RemoveReservoir(node))
                {
                    return false;
                }
            }
            for (nodehold = firstNode, backp = firstNode; nodehold != null; nodehold = nodehold.next)
            {
                if (nodehold == node)
                {
                    if (backp == nodehold)
                    {
                        firstNode = node.next;
                        nodehold = firstNode;
                        backp = firstNode;
                        if (firstNode != null)
                        {
                            firstNode.prev = null;
                        }
                    }
                    else if (nodehold.next != null)
                    {
                        backp.next = nodehold.next;
                        node.next.prev = backp;
                        nodehold = backp;
                    }
                    else
                    {
                        backp.next = null;
                        nodehold = backp;
                    }
                }
                backp = nodehold;
                if (nodehold == null)
                {
                    break;
                }
            }
            // Remove any references in any other Nodes to this Node...
            for (nodehold = firstNode; nodehold != null; nodehold = nodehold.next)
            {
                if (nodehold.number > deletednum)
                {
                    nodehold.number--;
                }
                // Delete any LagInfo's that specify this node as a return location.
                lagInfo = nodehold.m.infLagi;
                while (lagInfo != null)
                {
                    nextLagInfo = lagInfo.next;
                    if (lagInfo.location == node)
                    {
                        nodehold.m.infLagi = nodehold.m.infLagi.RemoveFromList(lagInfo);
                        //delete(lagInfo);
                    }
                    lagInfo = nextLagInfo;
                }
                // Delete any LagInfo's that specify this node as a return location.
                lagInfo = nodehold.m.pumpLagi;
                while (lagInfo != null)
                {
                    nextLagInfo = lagInfo.next;
                    if (lagInfo.location == node)
                    {
                        nodehold.m.pumpLagi = nodehold.m.pumpLagi.RemoveFromList(lagInfo);
                        //delete lagInfo;
                    }
                    lagInfo = nextLagInfo;
                }
                for (i = 0; i < 10; i++)
                {
                    if (nodehold.m.idstrmx[i] == node)
                    {
                        nodehold.m.idstrmx[i] = null;
                        nodehold.m.idstrmfraction[i] = (double)(1.0);
                    }
                }
                GlobalMembersNode.TetrisNodePtrs(nodehold.m.idstrmx, nodehold.m.idstrmfraction);
                if (nodehold.myMother == node)
                {
                    nodehold.myMother = null;
                }
                if (nodehold.m.demDirect == node)
                {
                    nodehold.m.demDirect = null;
                }
            }
            // Remove any references in any Links to this Node...
            for (lh = firstLink; lh != null; lh = lh.next)
            {
                if (lh.m.returnNode == node)
                {
                    lh.m.returnNode = null;
                }
            }
            // Update the node counters...
            NextNodeNum--;
            if (isRealNode)
            {
                real_nodes--;
            }
            //modified = true;
            return true;
        }
        /// <summary>Returns true if the specified reservoir node is successfully removed from the network</summary>
        public bool RemoveReservoir(Node N)
        {
            Node N1 = null;
            Node N2 = null;
            Node N3 = null;
            Node N4 = null;

            if (N.nodeType != NodeType.Reservoir)
            {
                FireOnError("Error in RemoveReservoir --- found a non-reservoir.");
                return false;
            }
            // check resnext and resprev pointers
            N1 = N.RESprev;
            N2 = N.RESnext;
            N3 = N.myMother;
            /* count is done on any load/save event  -MB 12/17/93
            * dangerous, should go and count again
            *
            * someone deleted a parent reservoir
            * we could reassign parenthood, or just free all the children of
            * their parents
            * this routine makes the children into separate reservoirs
            */
            if (N3 == N)
            {
                // Need to find all the children and deparent.
                for (N4 = N1; N4 != N;)
                {
                    N4.myMother = null;
                    N4.parentFlag = false;
                    N4.RESnext = N4.RESprev = N4;
                    N4 = N4.RESnext;
                }
            }
            else if (N3 != null)
            {
                // this is the simple one - deleting a child.
                N3.numChildren = N3.numChildren - 1; // N3 is parent of this child.
                if (N3.numChildren <= 0)
                {
                    // deleting last child.
                    N3.myMother = null;
                    N3.RESnext = N3;
                    N3.RESprev = N3;
                }
            }
            // If these pointers are =, this is a parent node.
            if ((N1 != N2) && N1 != null && N2 != null)
            {
                N1.RESnext = N2;
                N2.RESprev = N1;
            }
            else if (N1 == N)
            {
                N2.RESnext = N2;
                N1.RESprev = N2;
            }
            return true;
        }
        /// <summary>Sets the m.selected flag to true for ALL node and links</summary>
        public void SelectAll()
        {
            int nc = 0;
            int lc = 0;
            // Select every node and link in the network.
            for (Node n = firstNode; n != null; n = n.next)
            {
                n.m.selected = true;
                nc++;
            }
            for (Link l = firstLink; l != null; l = l.next)
            {
                l.m.selected = true;
                lc++;
            }
        }
        /// <summary>Sets the m.selected flag to true for all child reservoir nodes</summary>
        public void SelectChildNodes()
        {
            int nc = 0;
            // Select all child nodes.
            for (Node n = firstNode; n != null; n = n.next)
            {
                if (n.myMother != null)
                {
                    n.m.selected = true;
                    nc++;
                }
            }
        }
        /// <summary>Sets the m.selected flag to true if the link is of the type specifed</summary>
        /// <remarks> Type is one of SELECT_LINK_ALL, SELECT_LINK_ACCRUAL,
        ///  SELECT_LINK_NATURAL_FLOW, and SELECT_LINK_STORAGE</remarks>
        public void SelectLinks(int type)
        {
            int lc = 0;

            for (Link l = firstLink; l != null; l = l.next)
            {
                if ((type == DefineConstants.SELECT_LINK_ALL) || ((type == DefineConstants.SELECT_LINK_ACCRUAL) && IsAccrualLink(l)) || ((type == DefineConstants.SELECT_LINK_NATURAL_FLOW) && l.IsNaturalFlowLink()) || ((type == DefineConstants.SELECT_LINK_STORAGE) && l.IsStorageLink()))
                {
                    l.m.selected = true;
                    lc++;
                }
            }
        }
        /// <summary>Sets the m.selected flag to true if the node is of the specified type</summary>
        public void SelectNodes(NodeType type)
        {
            // Select all nodes whose type fits the requested node type.
            for (Node n = firstNode; n != null; n = n.next)
            {
                if (type == NodeType.Undefined || type == n.nodeType)
                {
                    n.m.selected = true;
                }
            }
        }
        /// <summary>Sets the m.selected flag to false for all links and nodes</summary>
        public void SelectNone()
        {
            // De-select every node and link in the network.
            for (Node n = firstNode; n != null; n = n.next)
            {
                n.m.selected = false;
            }
            for (Link l = firstLink; l != null; l = l.next)
            {
                l.m.selected = false;
            }
        }

        #endregion
        #region Water and storage rights methods
        /// <summary>Return true if the specified link ends at a reservoir and there is a link that has m.accrualLink set to the specified link</summary>
        public bool IsAccrualLink(Link lnk)
        {
            if (lnk.to.nodeType == NodeType.Reservoir)
            {
                for (Link cl = firstLink; cl != null; cl = cl.next)
                {
                    if (cl.m.accrualLink == lnk)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
        #region Timeseries methods

        /// <summary>Checks to ensure that all timeseries have the correct types of units. Changes all units that are not of the correct type to the default units for that TimeSeries type.</summary>
        /// <returns>If all units are correct, returns a blank string. Otherwise, returns a string describing each timeseries that failed.</returns>
        public string CheckUnits()
        {
            if (TimeStepManager == null || TimeStepManager.noDataTimeSteps == 0)
            {
                throw new System.Exception("TimeStepManager must be defined before you can load data arrays.");
            }

            // Define local variables
            string msg = string.Empty;

            // Loop through nodes and load TimeSeries
            this.FireOnMessage("Checking timeseries units.");
            for (Node n = this.firstNode; n != null; n = n.next)
            {
                if (n.m == null)
                {
                    throw new Exception("Cannot check timeseries units until n.m is defined.");
                }

                switch (n.nodeType)
                {
                    case NodeType.Reservoir:
                        if (n.m.adaTargetsM.EnsureUnitsHaveSameType(this.StorageUnits))
                        {
                            msg += "\n  Node: " + n.name + ". Target Storage units are now " + this.StorageUnits;
                        }
                        if (n.m.adaEvaporationsM.EnsureUnitsHaveSameType(this.LengthRateUnits))
                        {
                            msg += "\n  Node: " + n.name + ". Evaporation units are now " + this.LengthRateUnits;
                        }
                        if (n.m.adaGeneratingHrsM.EnsureUnitsHaveSameType(this.TimeRateUnits))
                        {
                            msg += "\n  Node: " + n.name + ". Generating Hours units are now " + this.TimeRateUnits;
                        }
                        if (n.m.adaForecastsM.EnsureUnitsHaveSameType(null))
                        {
                            msg += "\n  Node: " + n.name + ". Runoff Forecast units are now Dimensionless";
                        }
                        if (n.m.adaInfiltrationsM.EnsureUnitsHaveSameType(null))
                        {
                            msg += "\n  Node: " + n.name + ". Infiltrations units are now Dimensionless";
                        }
                        break;
                    case NodeType.NonStorage:
                        if (n.m.adaInflowsM.EnsureUnitsHaveSameType(this.FlowUnits))
                        {
                            msg += "\n  Node: " + n.name + ". Inflow units are now " + this.FlowUnits;
                        }
                        break;
                    case NodeType.Demand:
                        if (n.m.adaInfiltrationsM.EnsureUnitsHaveSameType(null))
                        {
                            msg += "\n  Node: " + n.name + ". Infiltrations units are now Dimensionless";
                        }
                        if (n.m.adaDemandsM.EnsureUnitsHaveSameType(this.FlowUnits))
                        {
                            msg += "\n  Node: " + n.name + ". Flow Demand units are now " + this.FlowUnits;
                        }
                        break;
                    case NodeType.Sink:
                        break;
                    default:
                        throw new Exception("Node type is undefined.");
                }
            }

            // Loop through links and load TimeSeries.
            for (Link l = this.firstLink; l != null; l = l.next) // int i = 0; i < this.mInfo.variableCapLinkListLen; i++)
            {
                if (l.m == null)
                {
                    throw new Exception("Cannot check units for timeseries until l.m is defined.");
                }
                if (l.m.maxVariable.EnsureUnitsHaveSameType(this.FlowUnits))
                {
                    msg += "\n  Link: " + l.name + ". Variable Capacity units are now " + this.FlowUnits;
                }
            }

            // Hydropower units and demands
            msg += this.hydro.CheckUnits();

            // Inform user of negative values in any node or link
            if (!string.IsNullOrEmpty(msg))
            {
                this.FireOnError("The following timeseries did not have the correct types of units. These were updated to the default units of their associated type:" + msg);
            }

            return msg;
        }
        /// <summary>Converts data that is not timeseries related, such as min, max, and starting volume as well as area-capacity-elevation-discharge curves.</summary>
        /// <param name="direction">The direction desired to convert units.</param>
        public void ConvertNonTSData()
        {
            // Non-timeseries data associated with reservoir nodes
            for (Node n = this.firstNode; n != null; n = n.next)
            {
                if (n.nodeType == NodeType.Reservoir)
                {
                    n.m.min_volume = Convert.ToInt64(this.StorageUnits.ConvertFrom(n.m.min_volume, n.m.reservoir_units));
                    n.m.max_volume = Convert.ToInt64(this.StorageUnits.ConvertFrom(n.m.max_volume, n.m.reservoir_units));
                    n.m.starting_volume = Convert.ToInt64(this.StorageUnits.ConvertFrom(n.m.starting_volume, n.m.reservoir_units));
                    if (n.m.ResEffCurve != null)
                    {
                        n.m.ResEffCurve.ConvertUnits(this.FlowUnits, this.LengthUnits);
                    }
                    n.m.apoints = Array.ConvertAll(n.m.apoints, element => this.AreaUnits.ConvertFrom(element, n.m.area_units));
                    //n.m.cpoints = Array.ConvertAll(n.m.cpoints, element => (element == this.defaultMaxCap) ? this.defaultMaxCap : Convert.ToInt64(this.StorageUnits.ConvertFrom(element, n.m.capacity_units)));
                    n.m.cpoints = Array.ConvertAll(n.m.cpoints, element =>  Convert.ToInt64(this.StorageUnits.ConvertFrom(element, n.m.capacity_units)));
                    //n.m.hpoints = Array.ConvertAll(n.m.hpoints, element => (element == this.defaultMaxCap) ? this.defaultMaxCap : Convert.ToInt64(this.FlowUnits.ConvertFrom(element, n.m.hcapacity_units)));
                    n.m.hpoints = Array.ConvertAll(n.m.hpoints, element =>  Convert.ToInt64(this.FlowUnits.ConvertFrom(element, n.m.hcapacity_units)));
                    n.m.pcap = Convert.ToInt64(this.FlowUnits.ConvertFrom(n.m.pcap, n.m.pcapUnits));
                }
            }
            // Convert hydropower non-timeseries units as well
            if (this.hydro.IsActive)
            {
                this.hydro.ConvertNonTSUnits();
            }
        }
        /// <summary>Converts and fills all nodes and links that have TimeSeries data.</summary>
        /// <returns>When <c>JustTest</c> is true, this method returns a string with a list of the links and nodes that are not filled. Otherwise, it returns a blank string.</returns>
        public string ConvertAndFill(bool JustTest)
        {
            if (TimeStepManager == null || TimeStepManager.noDataTimeSteps == 0)
            {
                throw new System.Exception("TimeStepManager must be defined before you can load data arrays.");
            }

            // Define local variables
            string msg = string.Empty;

            // Loop through nodes and load TimeSeries
            this.FireOnMessage("Converting and filling node timeseries.");
            if (!JustTest)
            {
                this.ConvertNonTSData();
            }
            for (Node n = this.firstNode; n != null; n = n.next)
            {
                if (n.m == null)
                {
                    throw new Exception("Cannot load time series arrays until n.m is defined.");
                }

                switch (n.nodeType)
                {
                    case NodeType.Reservoir:
                        if (!JustTest)
                        {
                            n.m.adaTargetsM.FillTable(this, n.m.starting_volume, this.StorageUnits);
                            n.m.adaEvaporationsM.FillTable(this, 0.0, this.LengthRateUnits);
                            n.m.adaGeneratingHrsM.AddBegRow(this.TimeStepManager.startingDate, this.timeStep.ToTimeSpan(this.TimeStepManager.startingDate).TotalHours);
                            n.m.adaGeneratingHrsM.FillTable(this, 24.0, this.TimeRateUnits);
                            n.m.adaForecastsM.FillTable(this, 0.0);
                            n.m.adaInfiltrationsM.FillTable(this, 0.0);
                        }
                        else
                        {
                            if (!n.m.adaTargetsM.IsFilled(this))
                            {
                                msg += "  Node: " + n.name + " in the Targets table.\n";
                            }
                            if (!n.m.adaEvaporationsM.IsFilled(this))
                            {
                                msg += "  Node: " + n.name + " in the Evaporation table.\n";
                            }
                            if (!n.m.adaGeneratingHrsM.IsFilled(this))
                            {
                                msg += "  Node: " + n.name + " in the Generating Hours table.\n";
                            }
                            if (!n.m.adaForecastsM.IsFilled(this))
                            {
                                msg += "  Node: " + n.name + " in the Runoff Forecasts table.\n";
                            }
                            if (!n.m.adaInfiltrationsM.IsFilled(this))
                            {
                                msg += "  Node: " + n.name + " in the Infiltrations table.\n";
                            }
                        }
                        break;
                    case NodeType.NonStorage:
                        if (!JustTest)
                        {
                            n.m.adaInflowsM.FillTable(this, 0.0, this.FlowUnits);
                        }
                        else if (!n.m.adaInflowsM.IsFilled(this))
                        {
                            msg += "  Node: " + n.name + " in the Inflows table.\n";
                        }
                        break;
                    case NodeType.Demand:
                        if (!JustTest)
                        {
                            n.m.adaInfiltrationsM.FillTable(this, 0.0);
                            n.m.adaDemandsM.FillTable(this, 0.0, this.FlowUnits);
                        }
                        else
                        {
                            if (!n.m.adaInfiltrationsM.IsFilled(this))
                            {
                                msg += "  Node: " + n.name + " in the Infiltrations table.\n";
                            }
                            if (!n.m.adaDemandsM.IsFilled(this))
                            {
                                msg += "  Node: " + n.name + " in the Demands table.\n";
                            }
                        }
                        break;
                    case NodeType.Sink:
                        break;
                    default:
                        throw new Exception("Node type is undefined.");
                }
            }

            // Loop through links and load TimeSeries.
            this.FireOnMessage("Converting and filling link timeseries.");
            for (Link l = this.firstLink; l != null; l = l.next)
            {
                if (l.m == null)
                {
                    throw new Exception("Cannot load time series arrays until l.m is defined.");
                }
                if (!JustTest)
                {
                    l.m.maxVariable.FillTable(this, (double)this.defaultMaxCap, this.FlowUnits);
                    l.m.adaMeasured.FillTable(this, (double)-999, this.FlowUnits);
                }
                else if (!l.m.maxVariable.IsFilled(this))
                {
                    msg += "  Link: " + l.name;
                }
            }

            // Convert hydropower units...
            if (this.hydro.IsActive)
            {
                msg += this.hydro.ConvertAndFillTimeseries(JustTest);
            }

            // Inform user of negative values in any node or link
            if (!JustTest)
            {
                FireOnMessage("Finished processing time series.");
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                FireOnError("The following links and nodes did not have filled and/or data in MODSIM default units:\n" + msg);
            }

            return msg;
        }
        /// <summary>Loads model arrays for all links and nodes that have TimeSeries data.</summary>
        public void LoadArraysFromTimeSeries()
        {
            if (TimeStepManager == null || TimeStepManager.noDataTimeSteps == 0)
            {
                throw new System.Exception("TimeStepManager must be defined before you can load data arrays.");
            }

            // Define local variables
            string msg = "";
            int numts = TimeStepManager.noModelTimeSteps + TimeStepManager.noBackRAdditionalTSteps;

            // Loop through nodes and load TimeSeries
            this.FireOnMessage("Loading timeseries from nodes into the solver.");
            for (Node n = this.firstNode; n != null; n = n.next)
            {
                if (n.m == null || n.mnInfo == null)
                {
                    throw new Exception("Cannot load time series arrays until n.m and n.mnInfo are defined.");
                }

                switch (n.nodeType)
                {
                    case NodeType.Reservoir:
                        if (n.m.adaTargetsM.getSize() > 0)
                        {
                            int numhs = n.m.adaTargetsM.getNumCol() - 1;
                            n.mnInfo.targetcontent = new long[numts, numhs]; // hydrologic state
                            if (n.m.adaTargetsM.HasNegativeValues())
                            {
                                msg += "  Node: " + n.name + " in the Targets table.\n";
                            }
                            LoadTimeSeriesArray(n.m.adaTargetsM, ref n.mnInfo.targetcontent);
                        }
                        if (n.m.adaEvaporationsM.getSize() > 0)
                        {
                            n.mnInfo.evaporationrate = new double[numts, 1]; // assumed not to be multicolumn
                                                                             // Evaporation values can be negative. Don't include check for negative values here.
                            LoadTimeSeriesArray(n.m.adaEvaporationsM, ref n.mnInfo.evaporationrate);
                        }
                        if (n.m.adaGeneratingHrsM.getSize() > 0)
                        {
                            n.mnInfo.generatinghours = new double[numts, 1]; // assumed not to be multicolumn
                            if (n.m.adaGeneratingHrsM.HasNegativeValues())
                            {
                                msg += "  Node: " + n.name + " in the Generating Hours table.\n";
                            }
                            LoadTimeSeriesArray(n.m.adaGeneratingHrsM, ref n.mnInfo.generatinghours);
                        }
                        if (n.m.adaForecastsM.getSize() > 0)
                        {
                            n.mnInfo.forecast = new long[numts, 1]; // assumed not to be multicolumn
                            if (n.m.adaForecastsM.HasNegativeValues())
                            {
                                msg += "  Node: " + n.name + " in the Runoff Forecasts table.\n";
                            }
                            LoadTimeSeriesArray(n.m.adaForecastsM, ref n.mnInfo.forecast);
                        }
                        if (n.m.adaInfiltrationsM.getSize() > 0)
                        {
                            n.mnInfo.infiltrationrate = new double[numts, 1]; // assumed not to be multicolumn
                            if (n.m.adaInfiltrationsM.HasNegativeValues())
                            {
                                msg += "  Node: " + n.name + " in the Groundwater Infiltration table.\n";
                            }
                            LoadTimeSeriesArray(n.m.adaInfiltrationsM, ref n.mnInfo.infiltrationrate);
                        }
                        n.mnInfo.start_storage = new long[numts]; // Stores the reservoir volumes at beginning of the TS
                        n.mnInfo.end_storage = new long[numts]; // Stores the reservoir volumes at beginning of the TS
                        break;
                    case NodeType.NonStorage:
                        if (n.m.adaInflowsM.getSize() > 0)
                        {
                            n.mnInfo.inflow = new long[numts, 1]; // assumed not to be multicolumn
                            if (n.m.adaInflowsM.HasNegativeValues())
                            {
                                msg += "  Node: " + n.name + " in the Inflows table.\n";
                            }
                            if (n.m.inflowFracNode != null)
                                LoadTimeSeriesArray(n.m.adaInflowsM, ref n.mnInfo.inflow, n.m.inflowFracNode.m.adaInflowsM, n.m.inflowFactor); //The inflow fraction will be added to the user defined timeseries
                            else
                            {
                                double activeFract = GetActiveInflowFraction(n);
                                LoadTimeSeriesArray(n.m.adaInflowsM, ref n.mnInfo.inflow, activeFraction: activeFract);
                            }
                        }
                        else
                        {
                            if (n.m.inflowFracNode != null)
                            {
                                n.mnInfo.inflow = new long[numts, 1]; // assumed not to be multicolumn
                                LoadTimeSeriesArray(n.m.inflowFracNode.m.adaInflowsM, ref n.mnInfo.inflow, activeFraction: n.m.inflowFactor);
                            }
                        }
                        break;
                    case NodeType.Demand:
                        if (n.m.adaInfiltrationsM.getSize() > 0)
                        {
                            n.mnInfo.infiltrationrate = new double[numts, 1]; // assumed not to be multicolumn
                            if (n.m.adaInfiltrationsM.HasNegativeValues())
                            {
                                msg += "  Node: " + n.name + " in the Groundwater Infiltration table.\n";
                            }
                            LoadTimeSeriesArray(n.m.adaInfiltrationsM, ref n.mnInfo.infiltrationrate);
                        }
                        if (n.m.adaDemandsM.getSize() > 0)
                        {
                            int numhs = n.m.adaDemandsM.getNumCol() - 1;
                            n.mnInfo.nodedemand = new long[numts, numhs]; // hydrologic state
                            if (n.m.adaDemandsM.HasNegativeValues())
                            {
                                msg += "  Node: " + n.name + " in the Demands table.\n";
                            }
                            LoadTimeSeriesArray(n.m.adaDemandsM, ref n.mnInfo.nodedemand);
                        }
                        //Initialize the arrays for storing simulation results. Used in BackRouting
                        n.mnInfo.ishtm = new long[numts]; // Stores the shortage in the demand node
                        n.mnInfo.demand = new long[numts]; // Stores the water supplied to the demand node
                        break;
                    case NodeType.Sink:
                        n.mnInfo.nodedemand = new long[numts, 1]; // assumed not to be multicolumn
                        for (int j = 0; j < numts; j++)
                        {
                            n.mnInfo.nodedemand[j, 0] = this.defaultMaxCap; //* CalcScaleFactor();    // 99999999;
                        }
                        n.mnInfo.ishtm = new long[numts]; // Stores the shortage in the demand node
                        n.mnInfo.demand = new long[numts]; // Stores the water supplied to the demand node
                        break;
                    default:
                        throw new Exception("Node type is undefined.");
                }
            }

            // Loop through links and load TimeSeries.
            this.FireOnMessage("Loading timeseries from links into the solver.");
            for (int i = 0; i < this.mInfo.variableCapLinkList.Length; i++)
            {
                Link l = this.mInfo.variableCapLinkList[i];
                l.mlInfo.hiVariable = new long[numts, 1];
                if (l.m.maxVariable.HasNegativeValues())
                {
                    msg += "  Link: " + l.name + " in the Maximum Capacity table.\n";
                }
                LoadTimeSeriesArray(l.m.maxVariable, ref l.mlInfo.hiVariable);
            }

            // Inform user of negative values in any node or link
            if (!string.IsNullOrEmpty(msg))
            {
                FireOnError("Negative values were found in timeseries of the following nodes and links: \n" + msg);
            }

            // End
            FireOnMessage("Finished processing time series.");
        }

        private double GetActiveInflowFraction(Node n)
        {
            if (n.nodeType != NodeType.NonStorage) return 1;
            double usedFraction = 0;
            string msg = "";
            foreach (Node _n in this.Nodes_NonStorage)
            {
                if (_n.m.inflowFracNode == n)
                {
                    usedFraction += _n.m.inflowFactor;
                    msg += "|" + _n.name;
                }
            }
            if (Math.Round(usedFraction, 4) > 1)
            {
                FireOnError("Inflow fractions assigned for node: " + n.name + " are greater than one (" + usedFraction + ").\n");
                throw new Exception("Inflows distribution to the non-storage nodes failed. Execution aborted.");
            }
            if (usedFraction > 0)
                FireOnMessage("Processing distribution of " + usedFraction + " of inflows for node :" + n.name + " into " + msg);
            return 1 - usedFraction;
        }
        /// <summary>Loads a TimeSeries to an array of type double.</summary>
        public void LoadTimeSeriesArray(TimeSeries ts, ref double[,] array)
        {
            LoadTimeSeriesArray(ts, ref array, true);
        }
        /// <summary>Loads a TimeSeries to an array of type double.</summary>
        public void LoadTimeSeriesArray(TimeSeries ts, ref double[,] array, bool variesByYear)
        {
            // Exit if the TimeSeries does not contain data.
            if (ts.getSize() <= 0)
            {
                return;
            }

            // Loop through all the timesteps and place the data into the arrays...
            int hs;
            int ncol = ts.getNumCol() - 1;
            DateTime date;
            int numts = this.TimeStepManager.noModelTimeSteps + this.TimeStepManager.noBackRAdditionalTSteps;
            if (!variesByYear)
            {
                numts = Math.Min(ts.getSize(), numts);
            }

            for (int index = 0; index < numts; index++)
            {
                date = this.TimeStepManager.Index2Date(index, TypeIndexes.ModelIndex);
                if (date != TimeManager.missingDate) // Valid dates are not achieved in backrouting routine. So, check for those.
                {
                    for (hs = 0; hs < ncol; hs++)
                    {
                        array[index, hs] = ts.getDataF(GetTsIndex(ts, date), hs);
                    }
                }
            }
        }
        /// <summary>Function loads a single specified TimeSeries to a specifed array.</summary>
        public void LoadTimeSeriesArray(TimeSeries ts, ref long[,] array, TimeSeries tsAdd = null, double fraction = 0, double activeFraction = 1)
        {
            LoadTimeSeriesArray(ts, ref array, true, tsAdd, fraction, activeFraction);
        }
        /// <summary>Function loads a single specified TimeSeries to a specifed array.</summary>
        public void LoadTimeSeriesArray(TimeSeries ts, ref long[,] array, bool variesByYear, TimeSeries tsAdd = null, double fraction = 0, double activeFraction = 1)
        {
            // Exit if the TimeSeries does not contain data.
            if (ts.getSize() <= 0)
            {
                return;
            }

            // Loop through all the timesteps and place the data into the arrays...
            int hs;
            int ncol = ts.getNumCol() - 1;
            DateTime date;
            int numts = this.TimeStepManager.noModelTimeSteps + this.TimeStepManager.noBackRAdditionalTSteps;
            if (!variesByYear)
            {
                numts = Math.Min(ts.getSize(), numts);
            }

            for (int index = 0; index < numts; index++)
            {
                date = this.TimeStepManager.Index2Date(index, TypeIndexes.ModelIndex);
                if (date != TimeManager.missingDate) // Valid dates are not achieved in backrouting routine. So, check for those.
                {
                    for (hs = 0; hs < ncol; hs++)
                    {
                        if(tsAdd!=null)
                            array[index, hs] = (long) Math.Round(ts.getDataL(GetTsIndex(ts, date), hs) + fraction * tsAdd.getDataL(GetTsIndex(tsAdd, date), hs),0);
                        else
                            array[index, hs] = (long) Math.Round(ts.getDataL(GetTsIndex(ts, date), hs) * activeFraction,0);
                    }
                }
            }
        }
        /// <summary>Initialize time series for back routing</summary>
        public void InitBackRoutArraysFromTimeSeries(Model mi1)
        {
            if (TimeStepManager == null || TimeStepManager.noDataTimeSteps == 0)
            {
                throw new System.Exception("TimeStepManager must be defined before you can load data arrays");
            }

            Node n;
            int numts = mi1.TimeStepManager.noModelTimeSteps + mi1.TimeStepManager.noBackRAdditionalTSteps;
            // Load demand nodes infiltration rates
            for (n = this.firstNode; n != null; n = n.next)
            {
                if (n.m == null || n.mnInfo == null)
                {
                    throw new Exception("Cannot load time series arrays until n.m and n.mnInfo are defined");
                }

                Node n1 = mi1.FindNode(n.number);
                switch (n.nodeType)
                {
                    case NodeType.Reservoir:
                        if (n1.mnInfo.targetcontent.Length > 0)
                        {
                            int numhs = n1.mnInfo.targetcontent.GetLength(1);
                            n.mnInfo.targetcontent = new long[numts, numhs]; // hydrologic state
                        }
                        if (n1.mnInfo.evaporationrate.Length > 0)
                        {
                            n.mnInfo.evaporationrate = new double[numts, 1];    // assumed not to be multicolumn
                        }
                        if (n1.mnInfo.generatinghours.Length > 0)
                        {
                            n.mnInfo.generatinghours = new double[numts, 1];    // assumed not to be multicolumn
                        }
                        if (n1.mnInfo.inflow.Length > 0)
                        {
                            n.mnInfo.inflow = new long[numts, 1];    // assumed not to be multicolumn
                        }
                        n.mnInfo.start_storage = new long[numts]; // Stores the reservoir volumes at beginning of the TS
                        n.mnInfo.end_storage = new long[numts]; // Stores the reservoir volumes at beginning of the TS
                        break;
                    case NodeType.NonStorage:
                        if (n1.mnInfo.inflow.Length > 0)
                        {
                            n.mnInfo.inflow = new long[numts, 1];    // assumed not to be multicolumn
                        }
                        if (n1.mnInfo.forecast.Length > 0) // suggest we put forcasts on nonstorage node
                        {
                            n.mnInfo.forecast = new long[numts, 1];    // assumed not to be multicolumn
                        }
                        break;
                    case NodeType.Demand:
                        if (n1.mnInfo.infiltrationrate.Length > 0)
                        {
                            n.mnInfo.infiltrationrate = new double[numts, 1];    // assumed not to be multicolumn
                        }
                        if (n1.mnInfo.nodedemand.Length > 0)
                        {
                            int numhs = n1.mnInfo.nodedemand.GetLength(1);
                            n.mnInfo.nodedemand = new long[numts, numhs]; // hydrologic state
                        }
                        if (n1.mnInfo.infiltrationrate.Length > 0)
                        {
                            int numhs = n1.mnInfo.infiltrationrate.GetLength(1);
                            n.mnInfo.infiltrationrate = new double[numts, numhs]; // hydrologic state
                            Array.Copy(n1.mnInfo.infiltrationrate, 0, n.mnInfo.infiltrationrate, 0, n.mnInfo.infiltrationrate.Length);
                        }
                        n.mnInfo.ishtm = new long[numts]; // Stores the shortage in the demand node
                        n.mnInfo.demand = new long[numts]; // Stores the water supplied to the demand node
                        break;
                    case NodeType.Sink:

                        n.mnInfo.nodedemand = new long[numts, 1]; // hydrologic state
                        for (int j = 0; j < numts; j++)
                        {
                            n.mnInfo.nodedemand[j, 0] = this.defaultMaxCap;    // 99999999;
                        }
                        n.mnInfo.ishtm = new long[numts]; // Stores the shortage in the demand node
                        n.mnInfo.demand = new long[numts]; // Stores the water supplied to the demand node
                        break;
                    default:
                        throw new Exception("Node type is undefined");
                }
            }

            // The backrouting procedure relies on solving the network without link capacities for a few iterations... Therefore, infeasible sums are found until iter == 3 generally... It would probably work more quickly if it didn't do this... <AQD 03/22/2012>
            if (this.mInfo.variableCapLinkList.Length > 0)
            {
                //BLL: backrouting isn't working anyway trying to cleanup these length variables
                //this.mInfo.variableCapLinkListLen = 0;
            }
        }
        /// <summary>Return the time step index for the given TimeSeries and date</summary>
        /// <remarks> This function simply calls the GetTsIndex function for the TimeSeries class</remarks>
        public int GetTsIndex(TimeSeries ts, DateTime thisdate)
        {
            return ts.GetTsIndex(thisdate);
        }

        #endregion
        #region Units methods

        /// <summary>Retrieves an array of labels corresponding to a <c>TimeSeriesType</c>. Depends on <c>this.UseMetricUnits</c> as well.</summary>
        /// <param name="type">The type for which to retrieve labels.</param>
        /// <returns>Returns an array of labels corresponding to a <c>TimeSeriesType</c>. Depends on <c>this.UseMetricUnits</c> as well.</returns>
        public string[] GetLabels(TimeSeriesType type)
        {
            switch (type)
            {
                case TimeSeriesType.NonStorage:
                case TimeSeriesType.Demand:
                case TimeSeriesType.Sink:
                case TimeSeriesType.VariableCapacity:
                case TimeSeriesType.Targets:
                    return this.Labels_VolumeUnits;
                case TimeSeriesType.Evaporation:
                    return this.Labels_LengthUnits;
                case TimeSeriesType.Generating_Hours:
                    return new string[1] { ModsimUserDefinedTimeStepType.hours.ToString() };
                case TimeSeriesType.Infiltration:
                case TimeSeriesType.Forecast:
                    return null;
                case TimeSeriesType.Power_Target:
                    return this.Labels_EnergyUnits;
                default:
                    throw new Exception("Cannot get labels for an undefined TimeSeries type: " + type.ToString());
            }
        }
        /// <summary>Retrieves the current timestep label. If the major units are cubic feet or cubic meters, this returns "second".</summary>
        /// <param name="majorUnits">The string representation of major units.</param>
        /// <returns>Returns the timestep label.</returns>
        private string GetTimeStep(string majorUnits)
        {
            ModsimUnits units = new ModsimUnits(majorUnits);
            if (units.MajorUnitsEquals(VolumeUnitsType.cf) || units.MajorUnitsEquals(VolumeUnitsType.cm))
            {
                return ModsimUnits.GetLabel(ModsimUserDefinedTimeStepType.seconds);
            }
            else
            {
                return this.timeStep.Label;
            }
        }
        /// <summary>Gets the labels for a given <c>TimeSeriesType</c> and has the option to add timestep labels to each element in the array.</summary>
        /// <param name="type">The type of TimeSeries data.</param>
        /// <param name="AddTimeStepLabels">Specifies whether to add timestep labels.</param>
        /// <returns>Returns the labels for a given <c>TimeSeriesType</c>.</returns>
        public string[] GetLabels(TimeSeriesType type, bool AddTimeStepLabels)
        {
            string[] labels = GetLabels(type);
            if (labels == null)
            {
                return null;
            }
            return Array.ConvertAll(labels, element => element + (AddTimeStepLabels ? "/" + GetTimeStep(element) : ""));
        }
        /// <summary>Gets the timestep labels for a specified TimeSeries type.</summary>
        /// <param name="type">The type of TimeSeries.</param>
        /// <returns>Returns the timestep labels for a specified TimeSeries type.</returns>
        public string[] GetTimeStepLabels(TimeSeriesType type)
        {
            switch (type)
            {
                case TimeSeriesType.NonStorage:
                case TimeSeriesType.Demand:
                case TimeSeriesType.Sink:
                case TimeSeriesType.VariableCapacity:
                case TimeSeriesType.Evaporation:
                case TimeSeriesType.Generating_Hours:
                case TimeSeriesType.Power_Target:
                    return this.Labels_Timesteps;
                case TimeSeriesType.Targets:
                case TimeSeriesType.Infiltration:
                case TimeSeriesType.Forecast:
                    return null;
                default:
                    throw new Exception("Cannot get labels for an undefined TimeSeries type: " + type.ToString());
            }
        }
        /// <summary>Retrieves the default units corresponding to a <c>TimeSeriesType</c>. Depends on <c>this.UseMetricUnits</c> as well.</summary>
        /// <param name="type">The type for which to retrieve labels.</param>
        /// <returns>Returns the default units corresponding to a <c>TimeSeriesType</c>. Depends on <c>this.UseMetricUnits</c> as well.</returns>
        public ModsimUnits GetDefaultUnits(TimeSeriesType type)
        {
            switch (type)
            {
                case TimeSeriesType.NonStorage:
                case TimeSeriesType.Demand:
                case TimeSeriesType.Sink:
                case TimeSeriesType.VariableCapacity:
                case TimeSeriesType.Measured:
                    return this.FlowUnits;
                case TimeSeriesType.Targets:
                    return this.StorageUnits;
                case TimeSeriesType.Evaporation:
                    return this.LengthRateUnits;
                case TimeSeriesType.Generating_Hours:
                    return this.TimeRateUnits;
                case TimeSeriesType.Infiltration:
                case TimeSeriesType.Forecast:
                    return null;
                case TimeSeriesType.Power_Target:
                    return this.PowerUnits;
                default:
                    throw new Exception("Cannot get default units for an undefined TimeSeries type: " + type.ToString());
            }
        }

        #endregion
        #region Input/Output methods

        /// <summary>Checks all incompatibilities between the model's variables and its output version.</summary>
        /// <param name="ModsimModel">The model that is going to be saved to file. </param>
        /// <returns>Returns true if any incompatibility is found.</returns>
        public bool HasIncompatibilities()
        {
            // Changes made at version 8.2.0
            if (this.outputVersion.Type < OutputVersionType.V8_2)
            {
                // timesteps and links...
                if (this.defaultMaxCap != 99999999 ||       //default max. capacity
                        (this.accuracy != 0 && this.accuracy != 2) ||  // model accuracy
                        (this.timeStep.TSType != ModsimTimeStepType.Daily // timesteps
                         && this.timeStep.TSType != ModsimTimeStepType.FiveDays
                         && this.timeStep.TSType != ModsimTimeStepType.TenDays
                         && this.timeStep.TSType != ModsimTimeStepType.Weekly
                         && this.timeStep.TSType != ModsimTimeStepType.Monthly))
                {
                    return true;
                }

                // units
                // this is hard... add eventually if it becomes a problem for users.

            }
            // Changes made at version 8.3.2
            if (this.outputVersion.Type < OutputVersionType.V8_3_0)
            {
                if (this.hydro.HydroUnits.Length > 0
                        || this.hydro.HydroTargets.Length > 0)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        // Events, delegates, and message pumps
        #region Events, delegates, and message pumps to occur during the solution process

        public event EventHandler Compliant_Init;
        public event EventHandler Compliant_IterBottom;
        public event EventHandler Compliant_IterTop;
        public event EventHandler Compliant_TimestepTop;
        public event EventHandler Compliant_PreConvergenceCheck;
        public event EventHandler Compliant_Converged;
        public event EventHandler Compliant_ConvergedFinal;
        public event EventHandler Compliant_End;
        public event EventHandler<MessageEventArgs> Compliant_OnMessage;
        public event EventHandler<MessageEventArgs> Compliant_OnModsimError;
        public event EventHandler Compliant_PreConvertUnits;
        public event EventHandler Compliant_PostConvertUnits;
        public event EventHandler Compliant_PreBalance;
        public event EventHandler Compliant_PostBalance;
        public event EventHandler Compliant_PreTimeSeriesLoad;
        public event EventHandler Compliant_PostTimeSeriesLoad;
        public event EventHandler Compliant_BackRoutPreTimeSeriesLoad;
        public event EventHandler Compliant_BackRoutPostTimeSeriesLoad;
        public event EventHandler Compliant_BackRoutInit;
        public event EventHandler Compliant_BackRoutIterTop;
        public event EventHandler Compliant_BackRoutIterBottom;
        public event EventHandler Compliant_BackRoutConverged;
        public event EventHandler Compliant_BackRoutEnd;
        public event EventHandler Compliant_OnDebug;


        public delegate void PreConvertUnitsDelegate();
        /// <summary>Event fired prior to converting model units.</summary>
        public event PreConvertUnitsDelegate PreConvertUnits;
        public delegate void PostConvertUnitsDelegate();
        /// <summary>Event fired after converting model units.</summary>
        public event PostConvertUnitsDelegate PostConvertUnits;
        public delegate void InitDelegate();
        /// <summary>Event called prior to time step loop to allow for scripting prior to it.</summary>
        /// <remarks>This is called in operate after the time step index and some other things that are not reset each time step are initialized</remarks>
        public event InitDelegate Init;
        public delegate void TimestepTopDelegate();
        /// <summary>Event fired at the beginning of each timestep.</summary>
        public event TimestepTopDelegate TimestepTop;
        public delegate void IterTopDelegate();
        /// <summary>Scripting at the top of the iteration sequence</summary>
        /// <remarks> If you want to do something at the beginning of each time step, do it here inside an if block for mi->Iteration == 0; Also for scripting each iteration</remarks>
        public event IterTopDelegate IterTop;
        public delegate void IterBottomDelegate();
        /// <summary>Scripting at the bottom of the iteration sequence before calling the solver; set things like lInfo->hi lInfo->cost</summary>
        /// <remarks> Setting certian things like node demand and so on may have no effect here because the model code between IterTop and IterBottom uses input data and so on to set link bounds and costs on artificial links that utilmately drive the solver</remarks>
        public event IterBottomDelegate IterBottom;
        public delegate void PreBalanceDelegate();
        public event PreBalanceDelegate PreBalance;
        public delegate void PostBalanceDelegate();
        public event PostBalanceDelegate PostBalance;
        public delegate void PreTimeSeriesLoadDelegate();
        public event PreTimeSeriesLoadDelegate PreTimeSeriesLoad;
        public delegate void PostTimeSeriesLoadDelegate();
        public event PostTimeSeriesLoadDelegate PostTimeSeriesLoad;
        public delegate void BackRoutPreTimeSeriesLoadDelegate(Model mi1);
        public event BackRoutPreTimeSeriesLoadDelegate BackRoutPreTimeSeriesLoad;
        public delegate void BackRoutPostTimeSeriesLoadDelegate(Model mi1);
        public event BackRoutPostTimeSeriesLoadDelegate BackRoutPostTimeSeriesLoad;
        public delegate void PreConvergenceCheckDelegate();
        /// <summary>Scripting before convergence check during iterations</summary>
        public event PreConvergenceCheckDelegate PreConvergenceCheck;
        public delegate void ConvergedDelegate();
        /// <summary>Scripting on time step convergence; significant things that happen after convergence include account balance, accrual date check, seasonal capacity check, and loading output arrays</summary>
        /// <remarks> One can check something, set convergence to false and continue</remarks>
        public event ConvergedDelegate Converged;
        public event ConvergedDelegate ConvergedFinal;
        public delegate void EndDelegate();
        /// <summary>Scripting after all time steps are complete; Partial flows are written out after this is called</summary>
        public event EndDelegate End;
        public delegate void BackRoutInitDelegate(Model miBackRout, double[,] regRoutCoef);
        public event BackRoutInitDelegate BackRoutInit;
        public delegate void BackRoutIterTopDelegate(Model miBackRout, double[,] regRoutCoef);
        public event BackRoutIterTopDelegate BackRoutIterTop;
        public delegate void BackRoutIterBottomDelegate(Model miBackRout, double[,] regRoutCoef);
        public event BackRoutIterBottomDelegate BackRoutIterBottom;
        public delegate void BackRoutConvergedDelegate(Model miBackRout, double[,] regRoutCoef);
        public event BackRoutConvergedDelegate BackRoutConverged;
        public delegate void BackRoutEndDelegate(Model miBackRout, double[,] regRoutCoef);
        public event BackRoutEndDelegate BackRoutEnd;
        public delegate void OnModsimErrorDelegate(string message);
        public event OnModsimErrorDelegate OnModsimError;
        public delegate void OnMessageDelegate(string message);
        public event OnMessageDelegate OnMessage;
        public delegate void OnDebugDelegate(string message);
        public event OnDebugDelegate OnDebug;
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FirePreConvertUnits()
        {
            if (this.PreConvertUnits != null)
            {
                this.PreConvertUnits();
            }
            if (this.Compliant_PreConvertUnits != null)
            {
                this.Compliant_PreConvertUnits(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FirePostConvertUnits()
        {
            if (this.PostConvertUnits != null)
            {
                this.PostConvertUnits();
            }
            if (this.Compliant_PostConvertUnits != null)
            {
                this.Compliant_PostConvertUnits(this, new EventArgs());
            }

        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireInit()
        {
            if (Init != null)
            {
                Init();
            }
            if (this.Compliant_Init != null)
            {
                this.Compliant_Init(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireTimestepTop()
        {
            if (TimestepTop != null)
            {
                TimestepTop();
            }
            if (this.Compliant_TimestepTop != null)
            {
                this.Compliant_TimestepTop(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireIterTop()
        {
            if (IterTop != null)
            {
                IterTop();
            }
            if (this.Compliant_IterTop != null)
            {
                this.Compliant_IterTop(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireIterBottom()
        {
            if (IterBottom != null)
            {
                IterBottom();
            }
            if (this.Compliant_IterBottom != null)
            {
                this.Compliant_IterBottom(this, new EventArgs());
            }
        }
        public void FirePreBalance()
        {
            if (PreBalance != null)
            {
                PreBalance();
            }
            if (this.Compliant_PreBalance != null)
            {
                this.Compliant_PreBalance(this, new EventArgs());
            }
        }
        public void FirePostBalance()
        {
            if (PostBalance != null)
            {
                PostBalance();
            }
            if (this.Compliant_PostBalance != null)
            {
                this.Compliant_PostBalance(this, new EventArgs());
            }
        }
        public void FirePreTimeSeriesLoad()
        {
            if (PreTimeSeriesLoad != null)
            {
                PreTimeSeriesLoad();
            }
            if (this.Compliant_PreTimeSeriesLoad != null)
            {
                this.Compliant_PreTimeSeriesLoad(this, new EventArgs());
            }
        }
        public void FirePostTimeSeriesLoad()
        {
            if (PostTimeSeriesLoad != null)
            {
                PostTimeSeriesLoad();
            }
            if (this.Compliant_PostTimeSeriesLoad != null)
            {
                this.Compliant_PostTimeSeriesLoad(this, new EventArgs());
            }
        }
        public void FireBackRoutPreTimeSeriesLoad(Model mi1)
        {
            if (this.BackRoutPreTimeSeriesLoad != null)
            {
                this.BackRoutPreTimeSeriesLoad(mi1);
            }
            if (this.Compliant_BackRoutPreTimeSeriesLoad != null)
            {
                this.Compliant_BackRoutPreTimeSeriesLoad(this, new EventArgs());
            }
        }
        public void FireBackRoutPostTimeSeriesLoad(Model mi1)
        {
            if (this.BackRoutPostTimeSeriesLoad != null)
            {
                BackRoutPostTimeSeriesLoad(mi1);
            }
            if (this.Compliant_BackRoutPostTimeSeriesLoad != null)
            {
                this.Compliant_BackRoutPostTimeSeriesLoad(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FirePreConvergenceCheck()
        {
            if (PreConvergenceCheck != null)
            {
                PreConvergenceCheck();
            }
            if (this.Compliant_PreConvergenceCheck != null)
            {
                this.Compliant_PreConvergenceCheck(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireConverged()
        {
            if (Converged != null)
            {
                Converged();
            }
            if (this.Compliant_Converged != null)
            {
                this.Compliant_Converged(this, new EventArgs());
            }
        }
        public void FireConvergedFinal()
        {
            if (ConvergedFinal != null)
            {
                ConvergedFinal();
            }
            if (this.Compliant_ConvergedFinal != null)
            {
                this.Compliant_ConvergedFinal(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireEnd()
        {
            if (End != null)
            {
                End();
            }
            if (this.Compliant_End != null)
            {
                this.Compliant_End(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        //Scripting for BackRouting only
        public void FireBackRoutInit(Model miBackRout, double[,] regRoutCoef)
        {
            if (BackRoutInit != null)
            {
                BackRoutInit(miBackRout, regRoutCoef);
            }
            if (this.Compliant_BackRoutInit != null)
            {
                this.Compliant_BackRoutInit(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireBackRoutIterTop(Model miBackRout, double[,] regRoutCoef)
        {
            if (BackRoutIterTop != null)
            {
                BackRoutIterTop(miBackRout, regRoutCoef);
            }
            if (this.Compliant_BackRoutIterTop != null)
            {
                this.Compliant_BackRoutIterTop(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireBackRoutIterBottom(Model miBackRout, double[,] regRoutCoef)
        {
            if (BackRoutIterBottom != null)
            {
                BackRoutIterBottom(miBackRout, regRoutCoef);
            }
            if (this.Compliant_BackRoutIterBottom != null)
            {
                this.Compliant_BackRoutIterBottom(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireBackRoutConverged(Model miBackRout, double[,] regRoutCoef)
        {
            if (BackRoutConverged != null)
            {
                BackRoutConverged(miBackRout, regRoutCoef);
            }
            if (this.Compliant_BackRoutConverged != null)
            {
                this.Compliant_BackRoutConverged(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to fire the scripting code</summary>
        public void FireBackRoutEnd(Model miBackRout, double[,] regRoutCoef)
        {
            if (BackRoutEnd != null)
            {
                BackRoutEnd(miBackRout, regRoutCoef);
            }
            if (this.Compliant_BackRoutEnd != null)
            {
                this.Compliant_BackRoutEnd(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to report something for debugging</summary>
        public void FireOnDebug(string message)
        {
            if (OnDebug != null)
            {
                OnDebug(message);
            }
            if (this.Compliant_OnDebug != null)
            {
                this.Compliant_OnDebug(this, new EventArgs());
            }
        }
        /// <summary>Internal MODSIM function to send an error message</summary>
        public void FireOnError(string message)
        {
            if (OnModsimError != null)
            {
                OnModsimError(message);
            }
            if (this.Compliant_OnModsimError != null)
            {
                this.Compliant_OnModsimError(this, new MessageEventArgs(message));
            }
        }
        /// <summary>Internal MODSIM function to send a console message</summary>
        public void FireOnMessage(string message)
        {
            if (OnMessage != null)
            {
                OnMessage(message);
            }
            if (this.Compliant_OnMessage != null)
            {
                this.Compliant_OnMessage(this, new MessageEventArgs(message));
            }
        }
        /// <summary>FireOnErrorGlobal is used to report error messages.
        ///It can be called by functions that do not have
        /// access to a model class by calling the Static Function</summary>
        public static void FireOnErrorGlobal(string msg)
        {
            if (RefModelIsDeclared())
            {
                RefModel.FireOnError(msg);
            }
        }
        /// <summary>FireOnMessageGlobal is used to report information
        /// it can be called by functions that do not have
        /// access to a model class by calling the Static Function</summary>
        public static void FireOnMessageGlobal(string msg)
        {
            if (RefModelIsDeclared())
            {
                RefModel.FireOnMessage(msg);
            }
        }
        private static bool RefModelIsDeclared()
        {
            return (Model.RefModel != null);
        }

        #endregion
    }

    public class MessageEventArgs : EventArgs
    {
        private string msg;
        public MessageEventArgs()
        {
            this.msg = "";
        }
        public MessageEventArgs(string message)
        {
            this.msg = message;
        }
        public string Message
        {
            get
            {
                return this.msg;
            }
            set
            {
                this.msg = value;
            }
        }

    }
}

