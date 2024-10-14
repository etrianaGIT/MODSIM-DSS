namespace Csu.Modsim.ModsimModel
{
/// <summary>The MnInfo class contains all Node variables used exclusively by the model</summary>
public class MnInfo
{
    public MnInfo()
    {
        this.gwrtnLink = null;
        parent = false; // TRUE: this is a parent node.
        child = false; // TRUE: this is a child node.
        ownerType = 0;
        //version 7 output arrays
        unreg_inflow = new long[12];
        upstrm_release = new long[12];
        reservoir_evaporation = new long[12];
        downstrm_release = new long[12];
        demand_shortage = new long[12];
        canal_in = new long[12];
        canal_out = new long[12];
        res_spill = new long[12];
        trg_storage = new long[12];
        gw_to_node = new long[12];
        node_to_gw = new long[12];
        iseepr = new long[12];
        irtnflowthruNF_OUT = new long[12]; // NF step rtn for sum flowthrus returning here
        avg_head = new double[12];
        avg_hydropower = new double[12];
        //iteration variables
        irtnflowthruSTG = 0;
        irtnflowthruNF = 0;
        iroutreturn = 0;
        artFlowthruNF = 0;
        artFlowthruSTG = 0;
        nuse = 0;
        ideep0 = 0;
        iseep0 = 0;
        //arrays for the solver
        ia = new long[12];
        ib = new long[12];
        //node time series arrays
        infiltrationrate = new double[0, 0];
        generatinghours = new double[0, 0];
        evaporationrate = new double[0, 0];
        targetcontent = new long[0, 0];
        nodedemand = new long[0, 0];
        inflow = new long[0, 0];
        forecast = new long[0, 0];
        // iteration variables
        head = 0.0;
        demout = 0;
        fldflow = 0;
        pump0 = 0;
        ithruSTG = 0; // Current iter's storage step calculation for flowthru
        ithruSTG0 = 0; // Last iter's storage step calculation for flowthru
        ithruNF = 0; // Current iter's natflow step calculation for flowthru
        ithruNF0 = 0; // Last iter's natflow step calculation for flowthru
        iprel = 0;
        ipinf = 0;
        stend = 0;
        stend0 = 0;
        watchNew = 0;
        clossreturn = 0; //amount returned to this node from all lagged losses (locations) in this time step
        demPrevFlow = null;
        demPrevDepn = null;
        targetExists = false;
        //special real links
        resLastFillLink = null;
        //artificial links
        //special ones first
        chanLossLink = null; // this one is really associated with a link that has channel loss
        routingLink = null; // this one is really associated with a link that is a routing link
        flowThruReturnLink = null; // from a flowtru with bypass credit link when we have ownerships
        gwrtnLink = null; //should be connected to a nonstorage node only
        gwoutLink = null; // should be on demand nodes only, now is used for reservoir seepage as well
        infLink = null; // should be on nonstorage nodes only, now is used for reservoir carryover, negEvap as well
        //reservoir node only
        targetLink = null;
        evapLink = null;
        excessStoLink = null;
        spillLink = null;
        resStgStepStoLink = null; // not implemented now
        hydraulicCapLink = null;
        outLetL = null; // ALink to allow extra flow for min cap on res's
        balanceLinks = null; // ALinks to "balance" targets between reservoirs
        flowThruReleaseLink = null;
        flowThruSTGLink = null;
        //demand node
        demLink = null;
        //artificial nodes
        flowThruAllocNode = null;
        hydraulicCapNode = null;
        resBalanceNode = null;
    }
    /// <summary>True if this node is NOT a "child" node</summary>
    public bool parent;
    /// <summary>True if this node is a "child" node</summary>
    /// <remarks> Used for old child reservoir designation/logic</remarks>
    public bool child;
    /// <summary>For reservoir nodes, defines "type" of reservoir to constrain code based on type</summary>
    /// <remarks>  Defined in SETNET; depending on if this reserovir node has ownership contracts and if
    /// this node is a "child" reservoir" types are defined in "types.h" as
    ///	0 = OLD_MODSIM_RES - no ownerships associated with this reservoir node
    ///	55 = CHILD_ACCOUNT_RES - this reservoir has ownership links defined for one or more accrual links
    ///							into this reservoir and this node is defined as a "child" to a parent reservoir node"
    ///	56 = PARENT_ACCOUNT_RES - this reservoir node has one or more children reservoir nodes
    ///							that have ownership accounts
    ///	57 = NONCH_ACCOUNT_RES - This reservoir has ownership contracts defined to one or more accrual links
    ///							is not a child reservoir, and has no child reservoir nodes
    /// 58 = ZEROSYS_ACCOUNT_RES - This reservoir has accrual links, is not a child reservoir,
    ///							and has a zero system number
    /// </remarks>
    public long ownerType;
    /// <summary>Array of reservoir node output - reservoir content at beginning of the time step</summary>
    public long[] start_storage; // output array reservoir content at beginning of time step
    /// <summary>Array of reservoir node output - reservoir content at end of the time step</summary>
    public long[] end_storage; // output array time step reservoir ending content
    /// <summary>Array of reservoir node output - reservoir desired ending content of the time step</summary>
    public long[] trg_storage; // output array time step reservoir target content
    /// <summary>Array of reservoir node output - reservoir spill of the time step</summary>
    /// <remarks> we NEVER want to see flow here; it is lost from the river system</remarks>
    public long[] res_spill; // output array time step reservoir spill
    /// <summary>Array of reservoir node output - time step reservoir evaporation</summary>
    public long[] reservoir_evaporation; // output array for time step evaporation
    /// <summary>Array of reservoir node output - time step reservoir seepage</summary>
    public long[] iseepr; // output array time step reservoir seepage
    /// <summary>Array of node output - local gain inflow for nonstorage nodes; bypass link flow for a flow thru demand node</summary>
    public long[] unreg_inflow;
    /// <summary>Array of reservoir node output - real link inflow</summary>
    public long[] upstrm_release; // output array time step inflow from upstream reaches
    /// <summary>Array of reservoir node output - bypass link flow</summary>
    public long[] canal_in; // output array time step reservoir bypass link flow
    /*   long   gw_to_node __gc[];/// output array time step groundwater infiltration / seepage from this node
    // we should get rid of this gw to reservoir nodes														 */
    /// <summary>Array of reservoir node output - real link outflow</summary>
    public long[] downstrm_release;
    //demand outflow that does not pass through the outlet works
    //any number of demands can be specified as diverting water directly
    //from the reservoir; these deliveries are subtracted from the sum of the
    //"normal outflow link" and the bypass outflow link
    //HYDRO_REQ and HYDR_SHT are always zero in the ouput now
    /// <summary>Array of reservoir node output - reservoir downstrm_release - demands satisfied that are specified as direct delivery from reservoir (not through outlet works)</summary>
    public long[] canal_out;
    /// <summary>Array of reservoir node output - time step average head (End Elev - TW or Pplant elev)</summary>
    public double[] avg_head; // output array time step average reservoir power head.
    // local output POWR_PK in MWHRS is output as avg_hydropower * specified generating hours
    // local output PWR_2ND in MWHRS is avg_hydropower * (number of hours per timestep - specified generating hours)
    // local output ELEV_END computed based on ending storage for time step
    /// <summary>Array of reservoir node output - time step avg hydropower in KW</summary>
    public double[] avg_hydropower;
    //DEMAND NODES
    //WARNING
    // output array for time step demand - also used in routing code
    // local output SURF_IN is an elaborate computation of satisfied demand from sources that
    // are bazzarely defined as surface supply computation sums local gain, flowthru return,
    // real link inflows, and the reservoir bypass link flow this REALLY should be cleaned up
    // local output GW_IN is computed as total satisfied demand - SURF_IN
    /// <summary>Array of demand node output WAY TOO MANY BAZZARE definitions</summary>
    public long[] demand;
    /// <summary>Node output array - time step GW inflow; should be only nonstorage nodes</summary>
    public long[] node_to_gw;
    /// <summary>Output array of demand node shortage</summary>
    public long[] demand_shortage;
    /// <summary>Output array of GW infiltration (demand node) or seepage (reservoir node)</summary>
    public long[] gw_to_node;
    /* END OF OUTPUT ARRAYS*/

    // reservoir variables
    /// <summary>Reservoir node time step ending content based on last solver flows</summary>
    /// <remarks> Sum of flows through the artificial target storage link and excess storage link</remarks>
    public long stend;
    /// <summary>Previous iteration reservoir node computed ending content</summary>
    public long stend0;
    /// <summary>Child reservoir node outflow needed to meet demands from this node</summary>
    public long demout;
    /// <summary>Child reservoir node outflow to meet target content</summary>
    public long fldflow;
    /// <summary>Reservoir outflow (reservoir outflow link flow + bypass link flow - demands designated as directly from he reservoir)</summary>
    public long iprel;
    /// <summary>Reservoir bypass link flow</summary>
    public long ipinf;
    /// <summary>Reservoir starting forebay elevation.</summary>
    public double starting_elevation;
    /// <summary>Reservoir forebay elevation average (based on content) over the time step</summary>
    public double avg_elevation;
    /// <summary>The elevation of the power plant or of the tailwater.</summary>
    public double tail_elevation;
    /// <summary>Reservoir node time step average forebay elevation - power plant elevation or tail water elevation</summary>
    public double head;
    /// <summary>Reservoir node time step average power produced.</summary>
    public double power;
    /// <summary>Reservoir node time step average reservoir surface area</summary>
    public double area;
    /// <summary>Reservoir node time step evaporation</summary>
    public long evpt;
    /// <summary>Reservoir Node content at beginning of the time step</summary>
    public long start;
    /// <summary>Reservoir Node time step reservoir outlet hydraulic capacity</summary>
    public long hydCap;
    /// <summary>Reservoir node flag true if any target contents are specified</summary>
    public bool targetExists;
    //WARNING
    // I don't like this "min" capacity on reservoirs; force the user to put a very high priority on a level
    // the "min" capacity is what the outLetL is for; let's get rid of it
    /// <summary>Reservoir node link that allows extra flow for minumum capacity</summary>
    public Link outLetL; // ALink to allow extra flow for min cap on res's
    /// <summary>Reservoir node artificial hydraulic capacity constraint link</summary>
    /// <remarks> Node where relOutLink and resBypassL normally end is replaced with an atificial node;
    /// the hydrualicCapLink then begins at this new artificial node and ends at the node where the
    /// resOutLink and resBypassL used to end; upper bounds on the hydraulicCapLink is set to outlet works
    /// hydraulic capacity in the ACEH table based on average time step forebay elevation</remarks>
    public Link hydraulicCapLink;
    /// <summary>Reservoir node last fill link that accrual takes place through for space that contributed to the rental pool and was designated as last fill</summary>
    public Link resLastFillLink;
    /// <summary>Reservoir node artificial spill link</summary>
    /// <remarks>this is a network relief valve that we should never see flow in because the flow disappears from the river system</remarks>
    public Link spillLink;
    /// <summary>Reservoir node artificial target storage link</summary>
    /// <remarks> Flow through the artificial target storage link represents stored water in the reservoir</remarks>
    public Link targetLink;
    /// <summary>Reservoir node artificial evaporation link</summary>
    /// <remarks> Flow through the artificial evaporation link is lost to the river system</remarks>
    public Link evapLink;
    /// <summary>Reservoir node excess storage link, represents water stored in the reservoir
    ///  above the target storage due to downstream channel constraints</summary>
    public Link excessStoLink;
    /// <summary>NOT IMPLEMENTD YET New artificial link to bring water to a reservoir node
    /// in the storage step; accrual links should be turned off so we don't mess with costs
    /// and we don't have to be concerned about the cost in the storage step
    /// May be more than one since accrual can be from more than one source</summary>
    public Link[] resStgStepStoLink;
    /// <summary>NOT IMPLEMENTED YET New artificial link to take away water in the Natural flow step</summary>
    /// Link  *resNFStepStoLink,  // takes reservoir water away in Natural flow step
    /// <summary>Artificial node created when ACEH table has hydraulic capacity enteries</summary>
    /// <remarks> Between the reservoir and node that connected the outflow and bypass links</remarks>
    public Node hydraulicCapNode;
    /// <summary>Artificial node created to connect balance table links from the reservoir</summary>
    /// <remarks>Artificial target storage link is from this node to the artificial storage node</remarks>
    public Node resBalanceNode;

    // Demand variables: time step demand specified BEFORE the iteration sequence
    /// <summary>Demand node demand set before the iteration sequence</summary>
    /// <remarks> Set equal to user specified TimeSeries value for the time step before iteration 0</remarks>
    public long nuse;
    /// <summary>Demand from nuse + watch logic</summary>
    public long watchNew;
    /// <summary>Flow thru demand storage step return (should be nonstorage node)</summary>
    public long irtnflowthruSTG;
    /// <summary>Flow thru demand natural flow step return (should be nonstorage node)</summary>
    public long irtnflowthruNF;
    /// <summary>Flow thru demand flowThruReturnLink natural flow step return</summary>
    internal long artFlowthruNF;
    /// <summary>Flow thru demand flowThruReturnLink storage step return</summary>
    internal long artFlowthruSTG;
    /// <summary>Ouput array flow thru demand NF step return (should be nonstorage node)</summary>
    public long[] irtnflowthruNF_OUT;
    /// <summary>Part of inflow for the current time step that comes from lagged flows from previous time steps (should be nonstorage node)</summary>
    public long iroutreturn;
    /// <summary>Time step demand shortage (should be demand node?)</summary>
    /// <remarks> Used in backrouting code</remarks>
    public long[] ishtm; // time step demand shortage; this WAS only used as a temporary variable
    //  it is now used in routing code
    /// <summary>This node's current time step total infiltration to GW</summary>
    public long ideep0;
    /// <summary>This node's current time step total seepage to GW</summary>
    public long iseep0;
    /// <summary>Demand Node amount of GW pumping in the previous iteration</summary>
    public long pump0;
    /// <summary>Amount of flow thru this node provided in the current storage step solver iteration</summary>
    public long ithruSTG;
    /// <summary>Amount of flow thru this node provided in the previous stroage step solver iteration</summary>
    public long ithruSTG0;
    /// <summary>Amount of flow thru this node provided in the current Natural flow step solver iteration</summary>
    public long ithruNF;
    /// <summary>Amount of flow thru this node provided in the previous Natural flow step solver iteration</summary>
    public long ithruNF0;
    /// <summary>Artificial link that takes water (infitration / seepage / GW depletion) from this node to the GW node</summary>
    public Link gwoutLink;
    /* we should allow this on demand links only we would need new links for reservoir seepage and routing */
    /// <summary>Artificial link that brings water from the GW node to this node</summary>
    /// <remarks> This node should be a nonstorage node only</remarks>
    public Link gwrtnLink;
    /// <summary>Artificial demand link from this node</summary>
    /// <remarks> Flow represents water consumptively used by the demand</remarks>
    public Link demLink;
    /// <summary>Artificial Inflow Link</summary>
    /// <remarks> Flow represents local gain, return from a flow thru / lagged routing flow, negative evaporation, and carryover storage to this node.
    /// RKL would be nice to create a new artificial carryover storage link and use infLink for nonstorage nodes only.</remarks>
    public Link infLink;
    // this should be on nonstorage links only
    //  need a new link for reservoir carryover (start)
    //		*residdemLink,			/// this link takes flow from a demand that does not go through the
    // gwoutLink or the lastNFLink for consumptive demands in nets with
    // storage ownerships
    //		*lastNFLink,			/// This link is for consumptive demands in nets with ownerships;
    // it has a high priority and opens up in stg step with hi = demLink->flow
    // in the previous NF step; hi =0 in NF step
    /// <summary>Artificial channel loss link</summary>
    /// <remarks> This link removes the amount of channel loss from the river that is specified on a real link from this node that the user specifies having channel loss</remarks>
    public Link chanLossLink;
    // if a link has channel loss then the from node needs this chanLossLink
    /// <summary>Artificial routing link</summary>
    /// <remarks> This link removes all flow from the river associated with a real link from this node that is specified as being a routing link; the water is lagged to a specified return location over time with lag coefficients</remarks>
    public Link routingLink;
    /// <summary>Artificial flowthru Return link</summary>
    /// <remarks>  Created if there are ownerships in the data set and this is a flow thru demand node with a bypass credit link</remarks>
    public Link flowThruReturnLink; //Gets hold of the flow through return link from the node.
    //	NodedemLink2Node; // Node to connect artificial demLink to; lastNFLink and residdemLink get
    // connected to the artDemandN
    //WARNING the following flowThruAllocNode, flowThruReleaseLink, and flowThruStgLink are used
    // where we have child reservoirs to constrain outflow
    //  It is unclear what the intent was in creating these constructs and how they are to be used
    //  We need to consider getting rid of them and script the implied functionality
    /// <summary>Artificial node created in data sets with ownership links and flow thru demands with ownerships and child reservoirs</summary>
    /// <remarks> This node is created between the reservoir and the resOutLink on child reservoirs</remarks>
    public Node flowThruAllocNode;
    /// <summary>Networks with flow thru demands with ownerships and child reservoirs This link is between the reservoir and the flowThruAllocNode cost of +10000</summary>
    /// <remarks> Unclear what this is for</remarks>
    public Link flowThruReleaseLink;
    /// <summary>Artificial link between a child reservoir and the flowThruAllocNode</summary>
    /// <remarks> Upper bounds is set to what the flow was in the first storage step</remarks>
    public Link flowThruSTGLink;
    /// <summary>Double subscripted array to store lagged flow from this node's infiltration / seepage to various return locations</summary>
    public long[,] demPrevFlow;
    /// <summary>Double subscripted array to store lagged flow from this nodes depletion on various influence locations</summary>
    public long[,] demPrevDepn;
    /// <summary>Linked list of links representing physical layers in the reserovir</summary>
    /// <remarks> Layers are given incremental priorities that compete with other layers of other reservoirs to hold the water in storage</remarks>
    public LinkList balanceLinks; // ALinks to "balance" targets between reservoirs
    /// <summary>Amount of channel loss returning to this node this time step</summary>
    /// <remarks> Should be associated with nonstroage nodes only</remarks>
    public long clossreturn; // returning channel loss to this node this time step
    /// <summary>This node's hydrologic state index; default =0 for data sets with no hydrologic state tables</summary>
    public int hydStateIndex; //for this node, changes each time step (0,1,2,3,4,5,6) (column in time series)
    // all following arrays are time series input [TimeManager->noModelTimeSteps, number of hydrologic state levels]
    /// <summary>Array of demand gw infiltration rates for each MODEL RUN time step</summary>
    /// <remarks> Implemented assuming TimeSeries::MuliColumn is false - not hydrologic state dependent</remarks>
    public double[,] infiltrationrate;
    /// <summary>Array of assumed hours / day of power generation for each MODEL RUN time step</summary>
    /// <remarks> Implemented assuming TimeSeries::MuliColumn is false - not hydrologic state dependent</remarks>
    public double[,] generatinghours;
    /// <summary>Array of reservoir evaporation rate (af/acre) for each MODEL RUN time step</summary>
    /// <remarks> Implemented assuming TimeSeries::MuliColumn is false - not hydrologic state dependent</remarks>
    public double[,] evaporationrate;
    /// <summary>Array of reservoir target content for each MODEL RUN time step</summary>
    /// <remarks> CAN be double subscripted for hydrologic state (timestep, hydstate)</remarks>
    public long[,] targetcontent;
    /// <summary>Array of demand node demand for each MODEL RUN time step</summary>
    /// <remarks> CAN be double subscripted for hydrologic state (timestep, hydstate)</remarks>
    public long[,] nodedemand;
    /// <summary>Array of reservoir forecasted inflow for each MODEL RUN time step</summary>
    /// <remarks>
    ///Usually input as a volume from this time step through some future date
    ///Implemented assuming TimeSeries::MuliColumn is false - not hydrologic state dependent
    /// </remarks>
    public long[,] forecast;
    /// <summary>Array of node inflow for each MODEL RUN time step</summary>
    /// <remarks>
    /// Should be associated with nonstorage nodes only
    /// implemented assuming TimeSeries::MuliColumn is false - not hydrologic state dependent
    /// </remarks>
    public long[,] inflow;

    /// <summary>Arrays for network solver</summary>
    public long[] ia;
    public long[] ib;
    /// <summary>Calculated reservoir head</summary>
    public double endElevation;
}
}
