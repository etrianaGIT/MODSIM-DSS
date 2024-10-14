using System;

namespace Csu.Modsim.ModsimModel
{
/// <summary>MinfoStr contains model related info.</summary>
public class MinfoStr
{
    /// <summary>Constructor</summary>
    public MinfoStr()
    {
        this.realNodesList = new Node[0];
        this.demList = new Node[0];
        this.resList = new Node[0];
        this.childList = new Node[0];
        this.parentList = new Node[0];
        this.importNodes = new Node[0];
        this.inflowNodes = new Node[0];

        this.realLinkList = new Link[0];
        this.ownerList = new Link[0];
        this.variableCapLinkList = new Link[0];
        this.accrualLinkList = new Link[0];
        this.lastFillLinkList = new Link[0];

        this.nList = new Node[0];
        this.lList = new Link[0];

        this.outputNodeList = new Node[0];
        this.outputLinkList = new Link[0];

        this.ownerChanlossList = new Link[0];
    }

    /// <summary>Initialize for default values</summary>
    /// <remarks>Initialize allocates some nodeInfo space in the model. It requires a Model* type parameter.</remarks>
    public bool Initialize(Model mi)
    {
        /* add artificial inflow node */
        this.artInflowN = mi.AddNewNode(false);
        this.artInflowN.mnInfo = new MnInfo();
        this.artInflowN.name = "ArtificialNode_Inflow";

        /* artificial storage node */
        this.artStorageN = mi.AddNewNode(false);
        this.artStorageN.mnInfo = new MnInfo();
        this.artStorageN.name = "ArtificialNode_Storage";

        /* add artificial demand node */
        this.artDemandN = mi.AddNewNode(false);
        this.artDemandN.mnInfo = new MnInfo();
        this.artDemandN.name = "ArtificialNode_Demand";

        /* add artificial spill node */
        this.artSpillN = mi.AddNewNode(false);
        this.artSpillN.mnInfo = new MnInfo();
        this.artSpillN.name = "ArtificialNode_Spill";

        /* add artificial mass balance node */
        this.artMassN = mi.AddNewNode(false);
        this.artMassN.mnInfo = new MnInfo();
        this.artMassN.name = "ArtificialNode_MassBalance";

        /* add artificial pumping node */
        this.artGroundWatN = mi.AddNewNode(false);
        this.artGroundWatN.mnInfo = new MnInfo();
        this.artGroundWatN.name = "ArtificialNode_GroundWater";

        this.SMOOTHHYDCAP = 20;
        this.GWSMOOTH = 500;
        this.ACCUMSHTLIMIT = 30;
        this.SMOOTHFLOTHRU = 40;
        this.SMOOTHOPER = 30;

        return true;
    }
    public bool ada_feasible;
    /// <summary>THE artificial inflow node</summary>
    public Node artInflowN; //Artifical Groundwater Node - / Artifical Group ownership Node:      Grouped storage links connect to here - / Artifical Mass Balance node - /Artifical Spill Node - / Artifical Demand node - / Artifical Storage Node - / Artifical Inflow Node
    /// <summary>THE artificial storage node</summary>
    public Node artStorageN;
    /// <summary>THE artificial demand node</summary>
    public Node artDemandN;
    /// <summary>THE artificial spill node</summary>
    public Node artSpillN;
    /// <summary>The artificial mass balance node</summary>
    public Node artMassN;
    /// <summary>THE artificial group ownership node</summary>
    /// <remarks> ALL the (atrificial) group ownership links representing the storage spaces shared by multiple ownership links with GroupNumber > 0 (regardless of which accrual link) both begin and end at this node </remarks>
    public Node artGroupN;
    /// <summary>THE artificial groundwater node</summary>
    public Node artGroundWatN;
    /// <summary>Artificial link from the artificial storage node to the artificial mass balance node</summary>
    public Link stoToMassBal; // - /Artifical - /Artifical - / Artifical Link: massBalance to - /Artifical Link: spill to Mass Balance Node - /Artifical Link: storage to Mass Balance Node
    /// <summary>Artificial link from the artificial demand node to the artificial mass balance node</summary>
    public Link demToMassBal;
    /// <summary>Artificial link from the artificial spill node to the artificial mass balance node</summary>
    public Link spillToMassBal;
    /// <summary>Artificial link from the artificial mass balance node to the artificial inflow node</summary>
    public Link massBalToInf;
    /// <summary>Artificial link from the artificial inflow node to the artificial GW node</summary>
    public Link infToGwater;
    /// <summary>Artificial link from the artificial GW node to the artificial inflow node</summary>
    public Link gwaterToInf;
    /// <summary>Array of demand nodes in the data set</summary>
    public Node[] demList; // list of demand nodes - includes sinks
    /// <summary>Array of reservoir nodes in the data set</summary>
    public Node[] resList; //list of all reservoir nodes - unsorted
    /// <summary>Array of "child" reservoir nodes</summary>
    /// <remarks> Child reservoirs USED to represent a storage priority (a block of space in a reservoir with a priority date). Child reservoir logic MAY be dropped in future versions of MODSIM; data sets should be updated to use scripting code in order to handle desired functionality </remarks>
    public Node[] childList; //list of chile reservoir nodes
    /// <summary>Array of "parent" reservoirs</summary>
    /// <remarks> We usd to have "child" reservoir constructs where the physical "parent" reservoir would have one or more children that represent individual storage priorities </remarks>
    public Node[] parentList;
    /// <summary>Array of "ownership" links that represent storage contracts</summary>
    public Link[] ownerList; //list of owership links
    /// <summary>Array of "real" nodes (not artificial)</summary>
    public Node[] realNodesList; //
    /// <summary>Array of nodes with imports</summary>
    /// <remarks> Imports are obsolete data structures and should not be used</summary>
    public Node[] importNodes; //list of nodes with import; should be null in version8
    /// <summary>Array of nodes with artificial inflow links</summary>
    /// <remarks> All real nodes are inflowNodes </remarks>
    public Node[] inflowNodes; //list of nodes with inflow link
    /// <summary>Array of "real" (not artificial) links in the data set</summary>
    public Link[] realLinkList; // list of real links; links created by user
    /// <summary>Array of links with variable (input TimeSeries) maximum capacity</summary>
    public Link[] variableCapLinkList; //list of links wiht varible capacity
    /// <summary>Array of accrual links</summary>
    /// <remarks> should NOT include any links that USED to point to themselves (lastfill, resOutflow) </remarks>
    public Link[] accrualLinkList; //list of accrual links; link that any link has this link as it's parent; including last fill links? reservoir outflow links?
    /// <summary>Array of last fill links</summary>
    public Link[] lastFillLinkList; //list of last fill links; link to a reservoir with parent = itself
    /// <summary>Array of all nodes including artifical</summary>
    public Node[] nList; //list of all ndoes
    /// <summary>Array of all links including artificial</summary>
    public Link[] lList; //list of all links
    /// <summary>Array of nodes specified for output</summary>
    public Node[] outputNodeList; //list of links that have output
    /// <summary>Array of links specified for output</summary>
    public Link[] outputLinkList; //list of links in output
    /// <summary>Array of ownership links that have channel loss charged</summary>
    public Link[] ownerChanlossList; //list of ownership links that have channel lose charged
    /// <summary>Used in operate; address of GW convergence flags</summary>
    public bool convgw;
    public bool convgSTEND;
    public bool convgFTHRU;
    /// <summary>Test flag for convergence; Used for groundwater, flow thru, and channel loss convergence criteria</summary>
    /// <remarks> Channel loss and gw convergence use the same mi->gw_conv facotr for convergence </remarks>
    public bool convg;
    /// <summary>Test for convergence on number of iterations. False, if interation is less than various minimums based on what constructs are used in the data set.</summary>
    public bool convg1;
    /// <summary>Test flag for convergence on "watch logic"</summary>
    /// <remarks> Convergence wired to .05 (5%) should be settable in a XYFile command </remarks>
    public bool convgWatch;
    /// <summary>Specifies whether or not the hydropower controller has converged.</summary>
    public bool convgHydro;
    /// <summary>Flag used in data sets with ownership links; true if solution results in shortage to one or more ownership links</summary>
    public bool hasAccumsht;
    /// <summary>Convenience flag; true if the data set has flow thru demands with ownership links</summary>
    /// <remarks> If true, we trigger the second storage step in the iteration sequence </remarks>
    public bool hasFTOwners;
    /// <summary>Current time step index (starts with zero) within the simulation run</summary>
    public int CurrentModelTimeStepIndex;
    /// <summary>Date and time at the beginning of the current timestep within the simulation run.</summary>
    public DateTime CurrentBegOfPeriodDate;
    /// <summary>Date and time at the end of the current timestep within the simulation run.</summary>
    public DateTime CurrentEndOfPeriodDate;
    /// <summary>Current array index (starts with zero and goes to the maximum time steps in memory).  It's used during simulation only</summary>
    public int CurrentArrayIndex;
    /// <summary>Current iteration in the solution sequence</summary>
    public int Iteration;
    /// <summary>MonthIndex and YearCounter are old convenience time step counters; they should go away</summary>
    public int MonthIndex; // zero based month index (zero to 7 daily, zero to 12 monthly)
    /// <summary>MonthIndex and YearCounter are old convenience time step counters; they should go away</summary>
    public int YearCounter; // Index begins with 1 ( first year in input time series data from xy file)
    /// <summary>for hydraulic capacity</summary>
    public int SMOOTHHYDCAP;
    /// <summary>groundwater</summary>
    public int GWSMOOTH;
    /// <summary>accumulated demand shortage</summary>
    public int ACCUMSHTLIMIT;
    /// <summary>limit flow thru</summary>
    public int SMOOTHFLOTHRU;
    public int SMOOTHOPER;
}
}
