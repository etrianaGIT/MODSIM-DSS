using System;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Class for members associated with all links "real" and "artificial"</summary>
    public class MlInfo
    {
        /// <summary>Link flow from the last solver solution</summary>
        public long flow; //This variable stores the minimum infiltrated flow coming from previous downstream time networks. - /This variable stores the integer of the min flow required for the back-routing logic to work correctly.  It sets min flows in the routing links and the demand links.
                          /// <summary>Link flow from the previous iteration solver solution</summary>
        public long flow0;
        /// <summary>Link relative priority used by the solver</summary>
        public long cost;
        /// <summary>Link upper bounds used by the solver</summary>
        public long hi;
        /// <summary>Link lower bounds used by the solver</summary>
        public long lo;
        /// <summary>Used in BackRounting logic on routing and demand links sets lower bound required to make sure enough water is passed ahead in time</summary>
        public long minFlowBackRouting;
        /// <summary>Used in BackRouting logic to tell network the minimum infiltrated flow from previous downstream time networks</summary>
        public long minGWFlowBackRouting;
        /// <summary>True if this link is an ownership link</summary>
        public bool isOwnerLink;
        /// <summary>True if this link is an accrual link</summary>
        public bool isAccrualLink;
        /// <summary>True if this link is a last fill link</summary>
        public bool isLastFillLink;
        /// <summary>True if this link is an artificial link</summary>
        public bool isArtificial;
        /// <summary>True if this link is a reach</summary>
        /// <remarks>All "real" links are reaches; this flag is next to worthless</remarks>
        public bool isReach;
        /// <summary>Linked list of links of "child" links; this is null except for accrual links</summary>
        public LinkList cLinkL; // ownership child links
                                /// <summary>Linkd list of links of "rental" links; this is null except for accrual links with rental agreements</summary>
        public LinkList rLinkL; // Rental child links
                                /// <summary>Array of time step variable capacities read in from XYFile<summary><remarks>  array is loaded before setnet from TimeSeries maxVariable</remarks>
        public long[,] hiVariable; //Array for max variable capacities in the link.  The array size is the number of model time steps
                                   /// <summary>The link associated with a hydropower unit at this link used to pull water through the unit.</summary>
        public Link hydroControl = null;
        /// <summary>The link associated with additional water passing through the hydropower unit at this link.</summary>
        public Link hydroAdditional = null;
        /// <summary>The link associated with the portion of hydropower discharge coming from hydroControl.</summary>
        public Link hydroInflow = null;
        /// <summary>All links that run parallel to the hydropower unit link (spill links).</summary>
        public Link[] hydroSpillLinks;

        /// <summary>Constructor for <c>MlInfo</c>.</summary>
        public MlInfo()
        {
            flow = 0;
            flow0 = 0;
            cost = 0;
            hi = 0;
            lo = 0;
            isOwnerLink = false;
            isAccrualLink = false;
            isLastFillLink = false;
            isArtificial = false;
            isReach = false;
            cLinkL = null; // ownership child links
            rLinkL = null; // Rental child links
        }

    }
}
