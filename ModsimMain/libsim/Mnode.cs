namespace Csu.Modsim.ModsimModel
{
    /* RKL
    /// maybe we should have a class for each of the 4 node types; no more inflows to demands /reservoirs
    /// no more demands for reservoirs; we would no longer have a bunch of null pointers to each node;
    RKL */
    /// <summary>The Mnode class contains all the information for a Node that will be used in the model, and can be edited in the interface.</summary>
    public class Mnode
    {
        // Constructor
        private void Defaults()
        {
            for (int i = 0; i < priority.Length; i++)
            {
                priority[i] = 100;
                demr[i] = 100;
            }
            for (int i = 0; i < 10; i++)
                idstrmfraction[i] = (double)1.0;
            for (int i = 0; i < 15; i++)
                watchFactors[i] = (double)1.0;
            hydTable = 0; // RKL this should be zero by default
        }
        public Mnode()
        {
            lastFillLink = null;
            dem = new long[1]; // annual demand based on hydrologic state;
            idstrmx = new Node[10]; // flowthru nodes
            infil = new double[12]; // infiltration rates
            apoints = new double[0]; // ACH points: area
            cpoints = new long[0]; // ACH points: capacity
            epoints = new double[0]; // ACH points: elevation
            hpoints = new long[0]; // ACH points: hydraulic capacity
            flowpts = new long[0]; // Reservoir outflows - for calculating TW
            twelevpts = new double[0]; // Reservoir tailwater elevations
            //qt = new long[0]; // first row of efficenty table (flows)
            //ht = new double[0]; // first column of head in efficncy table
            //efft = new double[0, 0]; // efficiency - depends on heads and flows
            demd = new double[1, 12]; // distribution of annual demand
            demr = new long[1]; // Priorities based on hydrologic states; sets artificial demand link cost
            idstrmfraction = new double[10]; // flowthru fractions
            /* watch logic 15 is hard wired into the spreadsheets, xy file, linkRemove*/
            watchFactors = new double[15];
            watchMaxLinks = new Link[15];
            watchMinLinks = new Link[15];
            watchLnLinks = new Link[15];
            watchLogLinks = new Link[15];
            watchExpLinks = new Link[15];
            watchSqrLinks = new Link[15];
            watchPowLinks = new Link[15];
            dist = new double[12]; // distribution of imports over 12 mnths or wks
            priority = new long[1]; // res priority, w/ hydrologic states
            Defaults();
            // all time series defauls are false for VariesByYear, Interpolate, and MultiColumn
            // long time series
            adaDemandsM = new TimeSeries(TimeSeriesType.Demand);
            adaInflowsM = new TimeSeries(TimeSeriesType.NonStorage);
            adaTargetsM = new TimeSeries(TimeSeriesType.Targets);
            adaForecastsM = new TimeSeries(TimeSeriesType.Forecast);
            // double time series
            adaEvaporationsM = new TimeSeries(TimeSeriesType.Evaporation);
            adaGeneratingHrsM = new TimeSeries(TimeSeriesType.Generating_Hours);
            adaInfiltrationsM = new TimeSeries(TimeSeriesType.Infiltration);
            this.ResEffCurve = null;
        }

        #region Reservoir Nodes Input

        /// <summary>Reservoir maximum capacity - cannot be exceeded</summary>
        /// <remarks> Any excess that cannot pass downstream is spilled out of the river system</remarks>
        public long max_volume;
        /// <summary>MINIMUM reservoir content; use of this is discouraged; use a reservoir level with a very high priority</summary>
        /// <remarks> Set as lower bounds of the artificial target storage link</remarks>
        public long min_volume; /* RKL: we should get rid of min_volume; force the user to define a very high priority layer RKL */
        /// <summary>Reservoir content at the beginning of the first simulation time step</summary>
        public long starting_volume;
        /// <summary>Reservoir volume units</summary>
        public ModsimUnits reservoir_units;
        /// <summary>Array of reservoir surface area corresponding to indexed elevation</summary>
        public double[] apoints;
        /// <summary>Reservoir Area Units</summary>
        public ModsimUnits area_units;
        /// <summary>Array of reservoir content corresponding to indexed elevation</summary>
        public long[] cpoints;
        /// <summary>Reservoir Capacity Units</summary>
        public ModsimUnits capacity_units;
        /// <summary>Array of reservoir forebay elevation</summary>
        public double[] epoints;
        /// <summary>Array of hydraulic capacity at indexed elevation</summary>
        public long[] hpoints;
        /// <summary>Reservoir Hydraulic Capacity Units</summary>
        public ModsimUnits hcapacity_units;
        /// <summary>Array of flow in the tailwater indexed elevation</summary>
        public long[] flowpts;
        /// <summary>Array of tail water elevation</summary>
        public double[] twelevpts;
        /// <summary>Maximum KW of the powerplant</summary>
        public double powmax;
        /// <summary>Static elevation of powerplant</summary>
        public double elev;
        /// <summary>Flag to compute peak power based on head only</summary>
        public bool peakGeneration;
        /// <summary>Static reservoir seepage rate (unitless, or af/af) vs time step average content</summary>
        public double seepg;
        /// <summary>Demand node pumping capacity</summary>
        public long pcap;
        /// <summary>Demand node pumping capacity units</summary>
        public ModsimUnits pcapUnits;
        /// <summary>Demand node pumping relative priority</summary>
        public long pcost;
        /// <summary>Specific yield for model generated lag coefficients.</summary>
        public double spyld;
        /// <summary>Transmissivity for model generated lag coefficients.</summary>
        public double trans;
        /// <summary>Distance to influence location for model generated lag coefficients</summary>
        public double Distance;
        /// <summary>Reservoir hydropower information</summary>
        public PowerEfficiencyCurve ResEffCurve;

        #endregion

        #region Demand Nodes

        /// <summary>OLD array of annual demand based on hydrologic state</summary>
        /// <remarks> Version 7 xy file (056) support</remarks>
        public long[] dem;
        /// <summary>Exchange credit link to set demand</summary>
        ///Linkldstrm;
        /// RKL NOTE ldstrm is not used; it is superceded by watch links
        /// <summary>Exchange Credit Node for specifing demand</summary>
        public Node jdstrm;
        /// <summary>Flow Thru demand bypass credit link</summary>
        public Link pdstrm;
        /// <summary>Flow Thru demand return location nodes</summary>
        public Node[] idstrmx;
        /// <summary>OLD array of demand infiltration rates to GW</summary>
        /// <remarks> Version 7 xy file (056) support</remarks>
        public double[] infil;
        /// <summary>OLD array of demand distributions</summary>
        /// <remarks> Version 7 xy file (056) support</remarks>
        public double[,] demd;
        /// <summary>Demand node priorities by hydrologic state</summary>
        /// <remarks> translated to cost on artificial demand link</remarks>
        public long[] demr;
        /// <summary>The reservoir node that this demand node physically removes water from
        /// the reservoir pool without going through the outlet works</summary>
        public Node demDirect;
        /// <summary>Flow Thru demand fraction to each return location</summary>
        public double[] idstrmfraction;
        /// <summary>Specifies whether the demand node has a hydropower target defined.</summary>
        public HydropowerTarget HydroTarget = null;

        #endregion

        #region Watch Logic

        //watch logic 15 is hard wired into the spreadsheets, xy file, linkRemove

        // Note in all but the Max and Min cases, the sum of all specified links' flows are used with  associated function and factor; for example if one specifies three links in the watchSqrLinks function, then the sum of the three link flows is squared and taken times the fifth factor listed
        /// <summary>Demand node array of watch logic factors; one factor for each watch type</summary>
        /// <remarks>Watch logic dynamically sets demand based on sum of link flows multiplied by factors for each watch "type" ; example if one specifies two links in the watchMaxLinks and one link in the watchMinLinks arrays, the maximum flow of the two watchMaxLinks times the first factor would be added to the flow in the watchMinLinks times the second factor<remarks>
        public double[] watchFactors;
        /// <summary>Array of links to use with Math::Max to set this node demand</summary>
        /// <remarks> The maximum flow of the specified links is factored with the first watchFactor</remarks>
        public Link[] watchMaxLinks;
        /// <summary>Array of links to use with Math::Min to set this node demand</summary>
        /// <remarks> The minimum flow of the specified links is factored with the second watchFactor</remarks>
        public Link[] watchMinLinks;
        /// <summary>Array of links that the flows are summed, used with the log function, and finally factored with the third watchFactor to set this node demand</summary>
        public Link[] watchLnLinks;
        /// <summary>Array of links that the flows are summed, used with the log function, and finally factored with the fourth watchFactor to set this node demand</summary>
        public Link[] watchLogLinks;
        /// <summary>Array of links that the flows are summed, used with the base10 log function, and finally factored with the fifth watchFactor to set this node demand</summary>
        public Link[] watchExpLinks;
        /// <summary>Array of links that the flows are summed, used with the power function with exponient 2, and finally factored with the sixth watchFactor to set this node demand</summary>
        public Link[] watchSqrLinks;
        /// <summary>Array of links that the flows are summed, used with the power function with exponient powvalue, and finally factored with the seventh watchFactor to set this node demand</summary>
        public Link[] watchPowLinks;
        /// <summary>Exponient used in the power function with watchPowLinks</summary>
        public double powvalue;

        #endregion

        #region Instance Variables

        /* RKL import and dist should eventually go away; have inflows similar to demands with no hydrologic state table have inflows be a time series that can repeat each year RKL */
        /// <summary>OLD annual inflow</summary>
        public long import; //* amount of water that shows up here */
        /// <summary>OLD array of distribution factors for import</summary>
        public double[] dist; //* distribution over 12 mnths or wks*/
        /// <summary>Array of reservoir node priorities (per hydrologic state)</summary>
        /// <remarks> translated to a cost on the artificial target storage link</remarks>
        public long[] priority;
        /// <summary>Reservoir bypass link for storage account reservoirs</summary>
        public Link resBypassL;
        /// <summary>Numeric hydrologic table ID referred to by this node</summary>
        /// <remarks> flow represents flow passing through the reservoir without being stored</remarks>
        public int hydTable;
        /// <summary>Reservoir node storage release link</summary>
        /// <remarks> Sum of link flow in resOutLink and resBypassLink is total outflow</remarks>
        public Link resOutLink;
        /// <summary>Reservoir Last Fill Link</summary>
        public Link lastFillLink;
        /// <summary>Reservoir system number to pool physical and accounts when balance is called</summary>
        public int sysnum;
        /// <summary>Flag to select this node for output</summary>
        public bool selected;
        /// <summary>GW pumping lagInfo data structure</summary>
        public LagInfo pumpLagi;
        /// <summary>Demand GW infiltration lagInfo data structure</summary>
        public LagInfo infLagi;
        /// <summary>Reservoir incremental priority layers data structure</summary>
        public ResBalance resBalance;
        /// <summary>Nonstorage node array of partial inflows</summary>
        /// <remarks> PartialFlows are derived from a previous model run and represent lagged state information
        /// from time steps previous to the beginning of this simulation run</remarks>
        public long[] PartialFlows;
        /// <summary>TimeSeries of demand node demands</summary>
        public TimeSeries adaDemandsM;
        /// <summary>TimeSeries of nonstorage node inflow</summary>
        public TimeSeries adaInflowsM;
        /// <summary>TimeSeries of reservoir node target content</summary>
        public TimeSeries adaTargetsM;
        /// <summary>TimeSeries of reservoir node forecast</summary>
        public TimeSeries adaForecastsM;
        /// <summary>TimeSeries of reservoir node evaporation rate</summary>
        public TimeSeries adaEvaporationsM;
        /// <summary>TimeSeries of reservoir node hours of power generation</summary>
        public TimeSeries adaGeneratingHrsM;
        /// <summary>TimeSeries of demand node infiltration rates to GW</summary>
        public TimeSeries adaInfiltrationsM;
        /// <summary>Flag to pump water only in the Storage Step iteration</summary>
        public bool GWStorageOnly;


        #endregion

        #region non-storage Nodes
        /// <summary>
        /// Points to the node with inflow which a portion will be implemented at this node.
        /// </summary>
        public Node inflowFracNode;
        /// <summary>
        /// Fraction of the inflow in the inflowFracNode that will be simulated at this node.
        /// </summary>
        public double inflowFactor;
        #endregion
    }
}
