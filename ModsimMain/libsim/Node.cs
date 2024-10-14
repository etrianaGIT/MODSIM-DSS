using System;
using System.Collections.Generic;
using ASquared.SymbolicMath;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Describes the node type.</summary>
    public enum NodeType
    {
        Undefined = 0,
        Reservoir = 1,
        NonStorage = 2,
        Demand = 3,
        Sink = 4
    }
    /// <summary>Describes the type of Demand node.</summary>
    public enum DemandType
    {
        Undefined = -1,
        Consumptive = 0,
        FlowThru = 1
    }
    /// <summary>Describes the type of Reservoir node.</summary>
    public enum ReservoirType
    {
        Undefined = -1,
        Reservoir = 0,
        Storage_Right = 1
    }

    /// <summary>The Node class contains generic information for each node</summary>
    public class Node : IComparable
    {
        #region Instance variables

        /// <summary>Node name or string ID.</summary>
        public string name;
        /// <summary>Node description</summary>
        public string description;
        /// <summary>Node number - numeric ID</summary>
        public int number;
        /// <summary>Node type of this instance</summary>
        public NodeType nodeType;
        /// <summary>Linked list of inflow links</summary>
        public LinkList InflowLinks;
        /// <summary>Linked list of outflow links</summary>
        public LinkList OutflowLinks;
        /// <summary>Flag is true if this is not a child node</summary>
        public bool parentFlag;
        /// <summary>This node's parent node</summary>
        public Node myMother;
        /// <summary>Number of children of this parent</summary>
        public int numChildren;
        /// <summary>Pointer to the next linked child reservoir</summary>
        public Node RESnext;
        /// <summary>Pointer to the previous linked child reservoir</summary>
        public Node RESprev;
        /// <summary>Points to the next node of a list of nodes in the network</summary>
        public Node next;
        /// <summary>Points to the previous node of a list of nodes in the network</summary>
        public Node prev;
        /// <summary>Points to the mnInfo instance for model data</summary>
        public MnInfo mnInfo;
        /// <summary>Pointer to the Mnode instance for interface data</summary>
        public Mnode m;
        /// <summary>GUI graphics data</summary>
        public Gnode graphics;
        /// <summary>Backrouting region this node belongs to</summary>
        public int backRRegionID;
        /// <summary>User-defined class to store data and processes in nodes outside MODSIM.</summary>
        public object Tag;
        /// <summary>Flow symbol for tailwater and reservoir elevation functions</summary>
        private Symbol q = "q";
        /// <summary>Volume symbol for reservoir elevation function</summary>
        private Symbol V = "V"; 
        /// <summary>Specifies the stage-storage relationship. The storage is represented by the variable "V"</summary>
        public Symbol StageStorage = null;
        /// <summary>Specifies the tailwater elevation in relation to reservoir discharge. Reservoir discharge is represented by the variable "q".</summary>
        public Symbol TWElev = null;
        /// <summary>
        /// Unique identifier used for identifying time series and properties in the database even if the name or number change.
        /// copies of the network will have the same GUIs for object representing the same element.
        /// </summary>
        public Guid uid;

        #endregion
        #region Hydropower properties

        /// <summary>Gets whether this node has hydropower defined or not.</summary>
        public bool HasOldHydropowerDefined
        {
            get { return this.nodeType == NodeType.Reservoir && this.m.powmax != 0; }
        }
        /// <summary>
        /// Flows for each of the outflow links. Use OutflowLinkNames to get the names of each outflow link in the same order.
        /// </summary>
        public double[] OutflowLinkFlows
        {
            get
            {
                double[] flows = new double[this.OutflowLinks.Count()];
                for (int i = 0; i < flows.Length; i++)
                    flows[i] = this.OutflowLinks.Item(i).mlInfo.flow;
                return flows;
            }
        }
        /// <summary>
        /// Flows for each of the inflow links. Use InflowLinkNames to get the names of each inflow link in the same order.
        /// </summary>
        public double[] InflowLinkFlows
        {
            get
            {
                double[] flows = new double[this.InflowLinks.Count()];
                for (int i = 0; i < flows.Length; i++)
                    flows[i] = this.InflowLinks.Item(i).mlInfo.flow;
                return flows;
            }
        }
        /// <summary>
        /// Names of outflow links
        /// </summary>
        public string[] OutflowLinkNames
        {
            get
            {
                string[] names = new string[this.OutflowLinks.Count()];
                for (int i = 0; i < names.Length; i++)
                    names[i] = this.OutflowLinks.Item(i).name;
                return names;
            }
        }
        /// <summary>
        /// Names of inflow links
        /// </summary>
        public string[] InflowLinkNames
        {
            get
            {
                string[] names = new string[this.InflowLinks.Count()];
                for (int i = 0; i < names.Length; i++)
                    names[i] = this.InflowLinks.Item(i).name;
                return names;
            }
        }
        /// <summary>
        /// Numbers (IDs) of outflow links
        /// </summary>
        public int[] OutflowLinkNumbers
        {
            get
            {
                int[] ids = new int[this.OutflowLinks.Count()];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = this.OutflowLinks.Item(i).number;
                return ids;
            }
        }
        /// <summary>
        /// Names of inflow links
        /// </summary>
        public int[] InflowLinkNumbers
        {
            get
            {
                int[] ids = new int[this.InflowLinks.Count()];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = this.InflowLinks.Item(i).number;
                return ids;
            }
        }

        #endregion

        #region Constructor and copying methods

        /// <summary>Constructor. Creates Mnode and Gnode instances. Sets mnInfo to null, parentFlag to true, and nodeType to NonStorage.</summary>
        public Node()
        {
            m = new Mnode();
            mnInfo = null;
            nodeType = NodeType.NonStorage;
            parentFlag = true;
            graphics = new Gnode();
            uid = Guid.NewGuid();
        }
        /// <summary>Copy most all the Mnode data from the specified node to this node</summary>
        public void CopyData(Node src)
        {
            int i;
            LagInfo li;
            LagInfo ltmp;
            LagInfo lcur;
            nodeType = src.nodeType;
            uid = src.uid;
            m.max_volume = src.m.max_volume;
            m.min_volume = src.m.min_volume;
            m.starting_volume = src.m.starting_volume;
            m.powmax = src.m.powmax;
            m.elev = src.m.elev;
            m.peakGeneration = src.m.peakGeneration;
            m.seepg = src.m.seepg;
            m.pcap = src.m.pcap;
            m.pcost = src.m.pcost;
            m.spyld = src.m.spyld;
            m.trans = src.m.trans;
            m.Distance = src.m.Distance;
            m.jdstrm = src.m.jdstrm;
            m.pdstrm = src.m.pdstrm;
            m.demDirect = src.m.demDirect;
            m.import = src.m.import;
            m.resBypassL = src.m.resBypassL;
            if (src.m.adaInflowsM != null) m.adaInflowsM = src.m.adaInflowsM.Copy();
            if (src.m.adaTargetsM != null) m.adaTargetsM = src.m.adaTargetsM.Copy();
            if (src.m.adaDemandsM != null) m.adaDemandsM = src.m.adaDemandsM.Copy();
            if (src.m.adaInfiltrationsM != null) m.adaInfiltrationsM = src.m.adaInfiltrationsM.Copy();
            if (src.m.adaForecastsM != null) m.adaForecastsM = src.m.adaForecastsM.Copy();
            if (src.m.adaGeneratingHrsM != null) m.adaGeneratingHrsM = src.m.adaGeneratingHrsM.Copy();
            if (src.m.adaEvaporationsM != null) m.adaEvaporationsM = src.m.adaEvaporationsM.Copy();
            m.hydTable = src.m.hydTable;
            m.resOutLink = src.m.resOutLink;
            m.sysnum = src.m.sysnum;
            m.selected = src.m.selected;
            if (src.m.ResEffCurve != null) m.ResEffCurve = src.m.ResEffCurve.Copy();

            while (m.pumpLagi != null)
            {
                li = m.pumpLagi.next;
                m.pumpLagi = li;
            }

            while (m.infLagi != null)
            {
                li = m.infLagi.next;
                m.infLagi = li;
            }

            ltmp = null;
            lcur = null;
            for (li = src.m.pumpLagi; li != null; li = li.next)
            {
                if (m.pumpLagi != null)
                {
                    lcur = new LagInfo();
                    ltmp.next = lcur;
                    lcur = ltmp;
                    lcur.next = null;
                }
                else
                {
                    lcur = ltmp = m.pumpLagi = new LagInfo();
                    lcur.next = null;
                }
                lcur.location = li.location;
                lcur.percent = li.percent;
                lcur.numLags = li.numLags;
                lcur.lagInfoData = new double[lcur.numLags];
                for (i = 0; i < li.lagInfoData.Length; i++)
                {
                    lcur.lagInfoData[i] = li.lagInfoData[i];
                }
            }

            ltmp = null;
            lcur = null;
            for (li = src.m.infLagi; li != null; li = li.next)
            {
                if (m.infLagi != null)
                {
                    ltmp = new LagInfo();
                    lcur.next = ltmp;
                    lcur = new LagInfo();
                    lcur = ltmp;
                    lcur.next = null;
                }
                else
                {
                    lcur = ltmp = m.infLagi = new LagInfo();
                    lcur.next = null;
                }
                lcur.location = li.location;
                lcur.percent = li.percent;
                lcur.numLags = li.numLags;
                lcur.lagInfoData = new double[lcur.numLags];
                for (i = 0; i < li.lagInfoData.Length; i++)
                {
                    lcur.lagInfoData[i] = li.lagInfoData[i];
                }
            }
            if (src.m.resBalance != null)
            {
                m.resBalance = src.m.resBalance.Clone();
            }
            m.PartialFlows = src.m.PartialFlows;

            m.apoints = new double[src.m.apoints.Length];
            m.cpoints = new long[src.m.apoints.Length];
            m.epoints = new double[src.m.apoints.Length];
            m.hpoints = new long[src.m.apoints.Length];
            for (i = 0; i < src.m.apoints.Length; i++)
            {
                m.apoints[i] = src.m.apoints[i];
                m.cpoints[i] = src.m.cpoints[i];
                m.epoints[i] = src.m.epoints[i];
                m.hpoints[i] = src.m.hpoints[i];
            }

            // Copy power data
            m.flowpts = new long[src.m.flowpts.Length];
            m.twelevpts = new double[src.m.flowpts.Length];
            for (i = 0; i < src.m.flowpts.Length; i++)
            {
                m.flowpts[i] = src.m.flowpts[i];
                m.twelevpts[i] = src.m.twelevpts[i];
            }

            for (i = 0; i < 15; i++)
            {
                m.watchFactors[i] = src.m.watchFactors[i];
                m.watchMaxLinks[i] = src.m.watchMaxLinks[i];
                m.watchMinLinks[i] = src.m.watchMinLinks[i];
                m.watchLnLinks[i] = src.m.watchLnLinks[i];
                m.watchLogLinks[i] = src.m.watchLogLinks[i];
                m.watchExpLinks[i] = src.m.watchExpLinks[i];
                m.watchSqrLinks[i] = src.m.watchSqrLinks[i];
            }

            for (i = 0; i < 10; i++)
            {
                m.idstrmx[i] = src.m.idstrmx[i];
                m.idstrmfraction[i] = src.m.idstrmfraction[i];
            }

            m.demr = new long[src.m.demr.Length];
            m.priority = new long[src.m.priority.Length];
            for (i = 0; i < src.m.demr.Length; i++)
                m.demr[i] = src.m.demr[i]; // priority used to set "cost" on demand link based on hydrologic state
            for (i = 0; i < src.m.priority.Length; i++)
                m.priority[i] = src.m.priority[i]; // reservoir priority sets artifical target storage link "cost"
        }

        #endregion

        #region Local Methods

        /// <summary>Add the specified link to the specified linked list</summary>
        private bool Add(ref LinkList lll, Link l)
        {
            LinkList nll = new LinkList();

            // Allocate a new LinkList item.
            nll.link = l;
            // Connect the new LinkList item into the list.
            nll.next = lll;
            lll = nll;
            return true;
        }
        /// <summary>Remove the specified link from the specified linked list</summary>
        private bool Remove(ref LinkList lll, Link l)
        {
            LinkList prev;
            LinkList del;

            // If the first LinkList item contains the Link pointer, remove it from the list.
            if ((lll).link == l)
            {
                del = lll;
                lll = (lll).next;
            }
            else
            {
                prev = lll;
                while (prev.next != null && (prev.next.link != l))
                    prev = prev.next;

                if (prev.next == null)
                    return false;

                del = prev.next;
                prev.next = prev.next.next;
            }
            return true;
        }
        /// <summary>True if successful in adding specified link to inflow link list</summary>
        public bool AddIn(Link l)
        {
            return Add(ref InflowLinks, l);
        }
        /// <summary>True if successful in adding specified link to outflow link list</summary>
        public bool AddOut(Link l)
        {
            return Add(ref OutflowLinks, l);
        }
        /// <summary>True if successful in removing specified link from inflow link list</summary>
        public bool RemoveIn(Link l)
        {
            return Remove(ref InflowLinks, l);
        }
        /// <summary>True if successful in removing specified link from outflow link list</summary>
        public bool RemoveOut(Link l)
        {
            return Remove(ref OutflowLinks, l);
        }
        /// <summary>Set this node's hydrologic state index to that of the specifed table</summary>
        public void setHydStateIndex(HydrologicStateTable table)
        {
            if (mnInfo != null)
            {
                mnInfo.hydStateIndex = 0;
                if (this.m.hydTable != 0)
                    mnInfo.hydStateIndex = table.StateLevelIndex;
            }
        }
        /// <summary>Fit this node's storage-elevation and tailwater elevation-discharge information to polynomials</summary>
        public void FitPolynomials(double targetR2, double scaleFactor)
        {
            if (this.StageStorage == null)
            {
                double[] capacities = HydropowerElevDef.Capacities(this, scaleFactor);
                double[] elevations = HydropowerElevDef.Elevations(this);
                if (capacities != null && elevations != null && capacities.Length != 0 && elevations.Length == capacities.Length)
                    this.StageStorage = Sym.FitPolynomial(capacities, elevations, this.V, targetR2);
            }
            if (this.TWElev == null)
            {
                double[] qs = HydropowerElevDef.TailwaterDischarges(this);
                double[] elevs = HydropowerElevDef.TailwaterElevations(this);
                if (qs != null && elevs != null && qs.Length != 0 && elevs.Length == qs.Length)
                    this.TWElev = Sym.FitPolynomial(qs, elevs, q, targetR2);
            }
        }
        /// <summary>Fit this node's information to polynomials</summary>
        public void FitPolynomials(double[] StageStorageCoeffs, double[] TwaterElevCoeffs)
        {
            if (StageStorageCoeffs != null && StageStorageCoeffs.Length != 0)
                this.StageStorage = Sym.BuildPolynomial(StageStorageCoeffs, 'V');
            if (TwaterElevCoeffs != null && StageStorageCoeffs.Length != 0)
                this.TWElev = Sym.BuildPolynomial(TwaterElevCoeffs, 'q');
        }
        /// <summary>Gets ending elevation as a function of outflow given an estimate of the outflow.</summary>
        /// <param name="outflowEst">The outflow estimate. </param>
        /// <returns>Returns a symbolic math representation of the ending elevation as a fucntion of outflow "q".</returns>
        public Symbol ElevationFunction(double dischargeEst, double scaleFactor, bool dischargeInto)
        {
            if (this.mnInfo == null)
                return this.StageStorage;

            Symbol endElev;
            double elev;
            elev = HydropowerElevDef.GetElev(this, Convert.ToInt64(this.mnInfo.stend), out endElev);
            if (!dischargeInto)
                return (endElev.Subs("V", (-q + (this.mnInfo.stend / scaleFactor + dischargeEst))) + elev) / 2.0;
            else
                return (endElev.Subs("V", (q + (this.mnInfo.stend / scaleFactor - dischargeEst))) + elev) / 2.0;
        }
        /// <summary>Gets the symbolic representation of the tailwater elevation function (represented as a function of the discharge through the power plant "q").</summary>
        /// <param name="pwrpltDischargeEst">The current estimate of the power plant discharge.</param>
        public Symbol TailwaterElevationFunction(double pwrpltDischargeEst, double ScaleFactor, int monIndex)
        {
            Symbol TWElevFxn;
            double outflow; 
            if (this.m.resOutLink == null) 
                outflow = this.mnInfo.downstrm_release[monIndex] / ScaleFactor;
            else 
                outflow = this.m.resOutLink.mlInfo.flow / ScaleFactor; 
            HydropowerElevDef.GetElev_TailWater(this, (long)outflow, out TWElevFxn);
            return TWElevFxn.Subs("q", q + (outflow - pwrpltDischargeEst)); 
        }

        // IComparable interface method
        public int CompareTo(Object obj)
        {
            if (!obj.GetType().Equals(typeof(Node)))
                throw new ArgumentException("obj is not the same type as this instance.");
            
            //return this.number.CompareTo((obj as Node).number);
            return this.uid.CompareTo((obj as Node).uid);
        }

        #endregion
        #region Shared Methods

        // Node type
        /// <summary>Retrieves the NodeType associated with a specified label.</summary>
        /// <param name="label">Specifies the label of the node.</param>
        /// <returns>Returns the NodeType of a specified node.</returns>
        public static NodeType GetType(string label)
        {
            // Check regular labels
            label = label.ToLower();
            string[] labels = GetLabels(false, false);
            string[] fullLabels = GetLabels(false, true);
            for (int i = 0; i < labels.Length; i++)
                if (label.Equals(labels[i].ToLower()) || label.Equals(fullLabels[i].ToLower()))
                    return (NodeType)i;

            // Check demand and reservoir labels
            if (GetType_Demand(label) != DemandType.Undefined)
                return NodeType.Demand;
            if (GetType_Reservoir(label) != ReservoirType.Undefined)
                return NodeType.Reservoir;

            // If the label isn't found, return undefined...
            return NodeType.Undefined;
        }
        /// <summary>Retrieves the DemandType associated with a specified label.</summary>
        /// <param name="label">Specifies the label of the node.</param>
        /// <returns>Returns the DemandType associated with a specified label.</returns>
        public static DemandType GetType_Demand(string label)
        {
            label = label.ToLower();
            string[] labels = GetLabels_Demand(false, false);
            string[] fullLabels = GetLabels_Demand(false, true);
            for (int i = 0; i < labels.Length; i++)
                if (label.Equals(labels[i].ToLower()) || label.Equals(fullLabels[i].ToLower()))
                    return (DemandType)i;

            return DemandType.Undefined;
        }
        /// <summary>Retrieves the ReservoirType associated with a specified label.</summary>
        /// <param name="label">Specifies the label of the node.</param>
        /// <returns>Returns the ReservoirType associated with a specified label.</returns>
        public static ReservoirType GetType_Reservoir(string label)
        {
            label = label.ToLower();
            string[] labels = GetLabels_Reservoir(false, false);
            string[] fullLabels = GetLabels_Reservoir(false, true);
            for (int i = 0; i < labels.Length; i++)
                if (label.Equals(labels[i].ToLower()) || label.Equals(fullLabels[i].ToLower()))
                    return (ReservoirType)i;

            return ReservoirType.Undefined;
        }
        /// <summary>Attempts to parse the <c>NodeType</c> (and <c>DemandType</c> and <c>ReservoirType</c>) from a string.</summary>
        /// <param name="label">The string to parse into a node type.</param>
        /// <param name="nodeType">The output node type.</param>
        /// <param name="demType">The output demand type. Returns Undefined if the node is not a demand.</param>
        /// <param name="resType">The output reservoir type. Returns Undefined if the node is not a reservoir.</param>
        /// <returns>Returns true if the string was parsed successfully (and if the node type is undefined). Otherwise, returns false.</returns>
        public static bool TryParse(string label, out NodeType nodeType, out DemandType demType, out ReservoirType resType)
        {
            // Get the node type
            nodeType = GetType(label);
            demType = DemandType.Undefined;
            resType = ReservoirType.Undefined;
            if (nodeType == NodeType.Undefined)
                return false;

            // Get the demand type
            if (nodeType == NodeType.Demand)
            {
                demType = GetType_Demand(label);
                if (demType == DemandType.Undefined)
                    return false;
            }

            // Get the reservoir type
            if (nodeType == NodeType.Reservoir)
            {
                resType = GetType_Reservoir(label);
                if (resType == ReservoirType.Undefined)
                    return false;
            }

            return true;
        }

        // Node type arrays
        /// <summary>Gets an array of NodeType with or without the Undefined type.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value from the array.</param>
        /// <returns>Returns an array of NodeType with or without the Undefined type.</returns>
        public static NodeType[] GetTypes(bool removeUndefined)
        {
            List<NodeType> aList = new List<NodeType>();
            foreach (NodeType type in Enum.GetValues(typeof(NodeType)))
            {
                aList.Add(type);
            }
            if (removeUndefined) aList.Remove(NodeType.Undefined);
            return aList.ToArray();
        }
        /// <summary>Gets an array of DemandTypes with or without the Undefined type.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value from the array.</param>
        /// <returns>Returns an array of DemandTypes with or without the Undefined type.</returns>
        public static DemandType[] GetTypes_Demand(bool removeUndefined)
        {
            List<DemandType> aList = new List<DemandType>();
            foreach (DemandType type in Enum.GetValues(typeof(DemandType)))
            {
                aList.Add(type);
            }
            if (removeUndefined) aList.Remove(DemandType.Undefined);
            return aList.ToArray();
        }
        /// <summary>Gets an array of ReservoirTypes with or without the Undefined type.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value from the array.</param>
        /// <returns>Returns an array of ReservoirTypes with or without the Undefined type.</returns>
        public static ReservoirType[] GetTypes_Reservoir(bool removeUndefined)
        {
            List<ReservoirType> aList = new List<ReservoirType>();
            foreach (ReservoirType type in Enum.GetValues(typeof(ReservoirType)))
            {
                aList.Add(type);
            }
            if (removeUndefined) aList.Remove(ReservoirType.Undefined);
            return aList.ToArray();
        }

        // Node labels
        /// <summary>Gets the label for a specified type of node. If a demand node, must specify a DemandType. If a reservoir node, must specify a ReservoirType.</summary>
        /// <param name="nodeType">The type of node.</param>
        /// <returns>Returns the full label for a specified type of node.</returns>
        public static string GetLabel(NodeType nodeType)
        {
            return GetLabel(nodeType, false);
        }
        /// <summary>Gets the label for a specified type of node. If a demand node, must specify a DemandType. If a reservoir node, must specify a ReservoirType.</summary>
        /// <param name="nodeType">The type of node.</param>
        /// <param name="getFullName">Specifies whether to retrieve full node names or short node names. Default value is <c>false</c>.</param>
        /// <returns>Returns the full label for a specified type of node.</returns>
        public static string GetLabel(NodeType nodeType, bool getFullName)
        {
            string label = nodeType.ToString();

            // Stop at the short name...
            if (!getFullName) return label;

            // Get the full name
            switch (nodeType)
            {
                case NodeType.NonStorage: return label;
                case NodeType.Sink: return "Network " + label;
                case NodeType.Reservoir: return GetLabel(NodeType.Reservoir, ReservoirType.Reservoir, getFullName);
                case NodeType.Demand: return GetLabel(NodeType.Demand, DemandType.Consumptive, getFullName);
                default: throw new Exception("Cannot retrieve the full label of an undefined NodeType.");
            }
        }
        /// <summary>Gets the label for a specified type of node. If a demand node, must specify a DemandType. If a reservoir node, must specify a ReservoirType.</summary>
        /// <param name="nodeType">The type of node.</param>
        /// <param name="demType">The demand node type.</param>
        /// <returns>Returns the full label for a specified type of node.</returns>
        public static string GetLabel(NodeType nodeType, DemandType demType)
        {
            return GetLabel(nodeType, demType, false);
        }
        /// <summary>Gets the label for a specified type of node. If a demand node, must specify a DemandType. If a reservoir node, must specify a ReservoirType.</summary>
        /// <param name="nodeType">The type of node.</param>
        /// <param name="demType">The demand node type.</param>
        /// <param name="getFullName">Specifies whether to retrieve full node names or short node names. Default value is <c>false</c>.</param>
        /// <returns>Returns the full label for a specified type of node.</returns>
        public static string GetLabel(NodeType nodeType, DemandType demType, bool getFullName)
        {
            if (nodeType == NodeType.Demand)
                switch (demType)
                {
                    case DemandType.Undefined: return nodeType.ToString();
                    case DemandType.Consumptive: return (getFullName ? demType.ToString().Replace("_", " ") + " " : "") + nodeType.ToString();
                    case DemandType.FlowThru: return demType.ToString().Replace("_", "") + (getFullName ? " " + nodeType.ToString() : "");
                    default: throw new Exception("The DemandType is not defined in Node.GetLabel(NodeType, bool, DemandType).");
                }
            else
                return GetLabel(nodeType, getFullName);
        }
        /// <summary>Gets the label for a specified type of node. If a demand node, must specify a DemandType. If a reservoir node, must specify a ReservoirType.</summary>
        /// <param name="nodeType">The type of node.</param>
        /// <param name="resType">The reservoir node type.</param>
        /// <returns>Returns the full label for a specified type of node.</returns>
        public static string GetLabel(NodeType nodeType, ReservoirType resType)
        {
            return GetLabel(nodeType, resType, false);
        }
        /// <summary>Gets the label for a specified type of node. If a demand node, must specify a DemandType. If a reservoir node, must specify a ReservoirType.</summary>
        /// <param name="nodeType">The type of node.</param>
        /// <param name="resType">The reservoir node type.</param>
        /// <param name="getFullName">Specifies whether to retrieve full node names or short node names. Default value is <c>false</c>.</param>
        /// <returns>Returns the full label for a specified type of node.</returns>
        public static string GetLabel(NodeType nodeType, ReservoirType resType, bool getFullName)
        {
            if (nodeType == NodeType.Reservoir)
                switch (resType)
                {
                    case ReservoirType.Undefined: return nodeType.ToString();
                    case ReservoirType.Reservoir: return nodeType.ToString();
                    case ReservoirType.Storage_Right: return resType.ToString().Replace("_", getFullName ? " " : "") + (getFullName ? " " + nodeType.ToString() : "");
                    default: throw new Exception("The ReservoirType is not defined in GetLabel_Short.");
                }
            else
                return GetLabel(nodeType, getFullName);
        }

        // Arrays of node labels
        /// <summary>Gets an array of all Node type labels.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value. Default value is <c>false</c>.</param>
        /// <returns>Returns an array of all Node type labels.</returns>
        public static string[] GetLabels(bool removeUndefined)
        {
            return GetLabels(removeUndefined, false);
        }
        /// <summary>Gets an array of all Node type labels.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value. Default value is <c>false</c>.</param>
        /// <param name="getFullNames">Specifies whether to retrieve full node names or short node names. Default value is <c>false</c>.</param>
        /// <returns>Returns an array of all Node type labels.</returns>
        public static string[] GetLabels(bool removeUndefined, bool getFullNames)
        {
            return GetLabels(removeUndefined, getFullNames, false);
        }
        /// <summary>Gets an array of all Node type labels.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value. Default value is <c>false</c>.</param>
        /// <param name="getFullNames">Specifies whether to retrieve full node names or short node names. Default value is <c>false</c>.</param>
        /// <param name="getFullSet">Specifies whether to retrieve the full set of node names (including DemandType and ReservoirType nodes). Default value is <c>false</c>.</param>
        /// <returns>Returns an array of all Node type labels.</returns>
        public static string[] GetLabels(bool removeUndefined, bool getFullNames, bool getFullSet)
        {
            // Labels for NodeType 
            NodeType[] nodeTypes = GetTypes(removeUndefined);
            string[] labels = Array.ConvertAll(nodeTypes, element => element == NodeType.Undefined ? NodeType.Undefined.ToString() : GetLabel(element, getFullNames));
            if (!getFullSet) return labels;

            // Add demand and reservoir types 
            List<string> aList = new List<string>(labels);
            aList.AddRange(GetLabels_Demand(removeUndefined, getFullNames));
            aList.AddRange(GetLabels_Reservoir(removeUndefined, getFullNames));

            // Return the string[] representation
            return aList.ToArray();
        }
        /// <summary>Gets an array of labels for all DemandTypes.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value. Default value is <c>false</c>.</param>
        /// <returns>Returns an array of labels for all DemandTypes.</returns>
        public static string[] GetLabels_Demand(bool removeUndefined)
        {
            return GetLabels_Demand(removeUndefined, false);
        }
        /// <summary>Gets an array of labels for all DemandTypes.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value. Default value is <c>false</c>.</param>
        /// <param name="getFullNames">Specifies whether to retrieve full node names or short node names. Default value is <c>false</c>.</param>
        /// <returns>Returns an array of labels for all DemandTypes.</returns>
        public static string[] GetLabels_Demand(bool removeUndefined, bool getFullNames)
        {
            DemandType[] demTypes = GetTypes_Demand(true);
            return Array.ConvertAll(demTypes, element => element == DemandType.Undefined ? DemandType.Undefined.ToString() : GetLabel(NodeType.Demand, element, getFullNames));
        }
        /// <summary>Gets an array of labels for all ReservoirTypes.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value. Default value is <c>false</c>.</param>
        /// <returns>Returns an array of labels for all ReservoirTypes.</returns>
        public static string[] GetLabels_Reservoir(bool removeUndefined)
        {
            return GetLabels_Reservoir(removeUndefined, false);
        }
        /// <summary>Gets an array of labels for all ReservoirTypes.</summary>
        /// <param name="removeUndefined">Specifies whether to remove the Undefined value. Default value is <c>false</c>.</param>
        /// <param name="getFullNames">Specifies whether to retrieve full node names or short node names. Default value is <c>false</c>.</param>
        /// <returns>Returns an array of labels for all ReservoirTypes.</returns>
        public static string[] GetLabels_Reservoir(bool removeUndefined, bool getFullNames)
        {
            ReservoirType[] resTypes = GetTypes_Reservoir(true);
            return Array.ConvertAll(resTypes, element => element == ReservoirType.Undefined ? ReservoirType.Undefined.ToString() : GetLabel(NodeType.Reservoir, element, getFullNames));
        }

        #endregion
    }

    public static class GlobalMembersNode
    {
        public static void TetrisNodePtrs(Node[] nptrs, double[] frac)
        {
            int i;
            int j;

            j = 0;
            for (i = 0; i < 10; i++)
            {
                if (nptrs[i] != null)
                {
                    nptrs[j] = nptrs[i];
                    frac[j] = frac[i];
                    j++;
                }
            }
        }
    }

}
