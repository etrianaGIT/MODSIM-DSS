using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Csu.Modsim.ModsimModel
{
    public enum LinkReportInfoType
    {
        ALL = 0,
        MLINK = 1,
        MLINFO = 2,
        MRLINFO = 3
    }

    /// <summary>The Link class contains all the generic information for a link including pointers to subclasses constructor creates Mlink subclass.</summary>
    public class Link : IComparable
    {
        /*enum linktype (general=0, natural_flow=1, storage_contract=2, group_contract=3,
        accrual=4, routing=5, res_bypass=6, res_outflow=7, channel_loss=8,
        rental=9);*/
        public Link()
        {
            graphics = new Glink();
            m = new Mlink();
            Defaults();
        }
        public readonly static string ForbiddenStringInName = "|";
        /// <summary>Retrieves an array of links found within a string separated by Link.ForbiddenStringInName.</summary>
        /// <param name="model">The model from which to retrieve the Links.</param>
        /// <param name="links">The string that list the links for a hydropower unit.</param>
        public static Link[] GetLinksFromString(Model model, string links)
        {
            string[] names = links.Split(Link.ForbiddenStringInName.ToCharArray(), StringSplitOptions.None);

            List<Link> list = new List<Link>();
            foreach (string name in names)
            {
                Link l = model.FindLink(name);
                if (l != null)
                {
                    list.Add(l);
                }
            }
            return list.ToArray();
        }
        /// <summary>Builds a string from link names and separates them with Link.ForbiddenStringInName.</summary>
        /// <param name="links">The array of links to place in a string.</param>
        public static string GetLinksAsString(Link[] links)
        {
            if (links.Length == 0)
            {
                return "";
            }
            StringBuilder s = new StringBuilder();
            foreach (Link l in links)
            {
                if (l != null)
                {
                    s.Append(l.name + Link.ForbiddenStringInName);
                }
            }
            return s.ToString().TrimEnd(Link.ForbiddenStringInName.ToCharArray());
        }
        private string m_name;
        /// <summary>Unique name of the link.</summary>
        public string name
        {
            get
            {
                return this.m_name;
            }
            set
            {
                if (value.Contains(ForbiddenStringInName))
                {
                    throw new Exception("The link name cannot contain \"" + ForbiddenStringInName + "\".");
                }
                this.m_name = value;
            }
        }
        /// <summary>Link type 0=general, 1=water rights, 2=storage</summary>
        public int type; // general=0, water rights=1, storage=2
                         /// <summary>Unique nunmeric ID</summary>
        public int number; // link identification number
                           /// <summary>Node of origin</summary>
        public Node from; // node of origin
                          /// <summary>Node of destination</summary>
        public Node to; // node of destination
                        /// <summary>Graphics data</summary>
        public Glink graphics; // graphics data
                               /// <summary>data passed to/from the XY file used by interface and model</summary>
        public Mlink m; // date structures for interface and model
                        /// <summary>next link in a multi link list, null if not a multi link</summary>
        public Link next;
        /// <summary>previous link in a multi link list; null if not a multi link</summary>
        public Link prev;
        /// <summary>Flag used in storage allocation logic THIS SHOULD GO AWAY</summary>
        public short touched; // used in the storage owners water distribution
                              /// <summary>pointer to model data structures used by most all links</summary>
        public MlInfo mlInfo; //Model (only) link info (all links artificial and real)
                              /// <summary>pointer to model data structures used by links created in the interface (real links)</summary>
        public MrlInfo mrlInfo; //Model real link info
        
        /// <summary>
        /// Unique identifier used for identifying time series and properties in the database even if the name or number change.
        /// copies of the network will have the same GUIs for object representing the same element.
        /// </summary>
        public Guid uid;
                                /// <summary>Copy data (including most Mlink data) from a source link to this link</summary>
        public void CopyData(Link src)
        {
            type = src.type;
            uid = src.uid;
            m.maxConstant = src.m.maxConstant;
            m.min = src.m.min;
            m.cost = src.m.cost;
            m.loss_coef = src.m.loss_coef;
            m.spyldc = src.m.spyldc;
            m.transc = src.m.transc;
            m.distc = src.m.distc;
            m.returnNode = src.m.returnNode;
            //m->laglink          = src->m->laglink;
            m.lnkallow = src.m.lnkallow;
            m.accrualLink = src.m.accrualLink;
            m.waterRightsRank = src.m.waterRightsRank;
            m.adminNumber = src.m.adminNumber;
            m.touchedSorted = src.m.touchedSorted;
            m.lastFill = src.m.lastFill;
            m.exchangeLimitLinks = src.m.exchangeLimitLinks;
            m.hydTable = src.m.hydTable;
            m.flagSecondStgStepOnly = src.m.flagSecondStgStepOnly;
            m.flagStorageStepOnly = src.m.flagStorageStepOnly;
            m.flagSTGeqNF = src.m.flagSTGeqNF;
            m.linkConstraintUPS = src.m.linkConstraintUPS;
            m.linkConstraintDWS = src.m.linkConstraintDWS;
            m.linkChannelLoss = src.m.linkChannelLoss;
            if (src.m.maxVariable != null)
            {
                m.maxVariable = src.m.maxVariable.Copy();
            }
            for (int i = 0; i < DefineConstants.MAXLAG; i++)
            {
                m.lagfactors[i] = src.m.lagfactors[i];
            }
            m.waterRightsDate = src.m.waterRightsDate;
            for (int i = 0; i < 7; i++)
            {
                m.rentLimit[i] = src.m.rentLimit[i];
            }
            m.capacityOwned = src.m.capacityOwned;
            m.groupNumber = src.m.groupNumber;
            m.initialStglft = src.m.initialStglft;
            m.relativeUseOrder = src.m.relativeUseOrder;
            if (src.m.adaMeasured != null)
            {
                m.adaMeasured = src.m.adaMeasured.Copy();
            }
            m.lLayer = src.m.lLayer;
        }
        /// <summary>set default values</summary>
        /// <remarks> We used to fill Mlink structures; this was moved to the Mlink constructor </remarks>
        public void Defaults()
        {
            name = "";
            type = 0;
            IsAccrualLink = false;
            description = "";
            uid = Guid.NewGuid();
        }
        /// <summary>Disconnect this link form the network</summary>
        /// <returns> Ture if successful; false if from->RemoveOut or to->RemoveIn fail </returns>
        public bool Disconnect()
        {
            if (!from.RemoveOut(this) || !to.RemoveIn(this))
            {
                return false;
            }
            from = null;
            to = null;
            return true;
        }
        /// <summary>Check for if this link is a "Natural Flow" link</summary>
        /// <remarks> NOTE this definition is different than used in the model code Model code assumes any link to a demand node with a negative cost is a Natural Flow link </remarks>
        /// <returns> True if link is to a demand node, does not have an accrualLink, and has a waterRightsDate </returns>
        public bool IsNaturalFlowLink()
        {
            if (to.nodeType == NodeType.Demand && m.accrualLink == null && m.waterRightsDate != TimeManager.missingDate)
            {
                if (m.cost >= 0)
                {
                    Model.FireOnErrorGlobal("link has water right but not negative cost");
                }
                return true;
            }
            return false;
        }
        public bool IsOwnerLink()
        {
            if (!IsStorageLink())
            {
                return false;
            }
            for (int i = 0; i < m.rentLimit.Length; i++)
            {
                if (m.rentLimit[i] < 0)
                {
                    return false;
                }
            }
            return true;
        }
        public bool IsRentLink()
        {
            if (!IsStorageLink())
            {
                return false;
            }
            for (int i = 0; i < m.rentLimit.Length; i++)
            {
                if (m.rentLimit[i] < 0)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsLastFillLink()
        {
            if (this.to.m.lastFillLink == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, this.to.m.lastFillLink))
            {
                return true;
            }
            return false;
        }
        /// <summary>Check for if this link is a storage contract link</summary>
        /// <returns> True if this link has an accrualLink that ends at a reservoir node and this link ends at a demand node </returns>
        public bool IsStorageLink()
        {
            if (m.accrualLink != null)
            {
                if (m.accrualLink.to.nodeType == NodeType.Reservoir && this.to.nodeType == NodeType.Demand)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>Flag to recognize this link is an accural link before ownership links have been defined</summary>
        public bool IsAccrualLink; //Flag added to recognize an accrual link before some other link is pointing to it.

        /// <summary> Set this links' hydrologic state index based on the given table</summary>
        /// <remarks> Hydrologic State Table COULD be different than the links specified table. Don't know if this is intended </remarks>
        public void setHydStateIndex(HydrologicStateTable table)
        {
            if (mrlInfo != null)
            {
                mrlInfo.hydStateIndex = 0;
                if (this.m.hydTable != 0)
                {
                    mrlInfo.hydStateIndex = table.StateLevelIndex;
                }
            }
        }
        /// <summary>User-defined class to store data and processes in links outside the ModsimModel.</summary>
        public object Tag;
        /// <summary>Link narrative description.</summary>
        public string description;
        /// <summary>Link report prints info on a link; msg is a string to indicate where this was called from infoType is one of "ALL", "BASIC", "MLINK", "MLINFO", "MRLINFO" overloaded with just the first string to mean "ALL"</summary>
        public void ReportLink(string msg)
        {
            ReportLink(msg, LinkReportInfoType.ALL);
        }
        public void ReportLink(string msg, LinkReportInfoType infoType)
        {
            Model.FireOnErrorGlobal(string.Concat(msg, " ", name));
            if (IsAccrualLink)
            {
                Model.FireOnErrorGlobal(string.Concat(msg, "Accrual Link ", name, " Number ", Convert.ToString(number)));

                switch (infoType)
                {
                    case LinkReportInfoType.ALL:
                        {
                            PrintLinkBasic();
                            PrintAccrualMLink();
                            PrintAccrualMLInfo();
                            PrintAccrualMRLInfo();
                            return;
                        }
                    case LinkReportInfoType.MLINK:
                        {
                            PrintLinkBasic();
                            PrintAccrualMLink();
                            return;
                        }
                    case LinkReportInfoType.MLINFO:
                        {
                            PrintAccrualMLInfo();
                            return;
                        }
                    case LinkReportInfoType.MRLINFO:
                        {
                            PrintAccrualMRLInfo();
                            return;
                        }
                    default:
                        throw new Exception("LinkReportInfoType type is undefined");
                }
            }
            if (IsOwnerLink() || IsRentLink())
            {
                if (IsRentLink())
                {
                    Model.FireOnErrorGlobal(string.Concat(msg, "Rent Link ", name, " Number ", Convert.ToString(number)));
                }

                Model.FireOnErrorGlobal(string.Concat("   Owner Link ", " Number ", Convert.ToString(number)));
                switch (infoType)
                {
                    case LinkReportInfoType.ALL:
                        {
                            PrintLinkBasic();
                            PrintOwnerMLink();
                            PrintOwnerMLInfo();
                            PrintOwnerMRLInfo();
                            return;
                        }
                    case LinkReportInfoType.MLINK:
                        {
                            PrintLinkBasic();
                            PrintOwnerMLink();
                            return;
                        }
                    case LinkReportInfoType.MLINFO:
                        {
                            PrintOwnerMLInfo();
                            return;
                        }
                    case LinkReportInfoType.MRLINFO:
                        {
                            PrintOwnerMRLInfo();
                            return;
                        }
                    default:
                        throw new Exception("LinkReportInfoType type is undefined");
                }
            }
            Model.FireOnErrorGlobal(" ReportLink is only coded for accrual, owner, and rent links");
        }
        public void PrintLinkBasic()
        {
            Model.FireOnErrorGlobal(string.Concat(" Number ", Convert.ToString(number)));
            Model.FireOnErrorGlobal(string.Concat(" uid ", Convert.ToString(uid)));
        }
        public void PrintAccrualMLink()
        {
            if (m == null)
            {
                return;
            }
            Model.FireOnErrorGlobal(string.Concat(" Cost ", Convert.ToString(m.cost), " WaterRightDate ", Convert.ToString(m.waterRightsDate)));
        }
        public void PrintAccrualMLInfo()
        {
            if (mlInfo == null)
            {
                return;
            }
            if (mlInfo.cLinkL != null)
            {
                for (LinkList ll = mlInfo.cLinkL; ll != null; ll = ll.next)
                {
                    ll.link.ReportLink(" ", LinkReportInfoType.ALL);
                }
            }
            if (mlInfo.rLinkL != null)
            {
                for (LinkList ll = mlInfo.rLinkL; ll != null; ll = ll.next)
                {
                    ll.link.ReportLink(" ", LinkReportInfoType.ALL);
                }
            }
        }
        public void PrintAccrualMRLInfo()
        {
            Model.FireOnErrorGlobal(string.Concat(" SeasonCap ", Convert.ToString(mrlInfo.lnkSeasStorageCap)));
            Model.FireOnErrorGlobal(string.Concat(" stglft ", Convert.ToString(mrlInfo.stglft), " ownacrual ", Convert.ToString(mrlInfo.own_accrual)));
            Model.FireOnErrorGlobal(string.Concat(" prevstglft ", Convert.ToString(mrlInfo.prevstglft), " prevownacrul ", Convert.ToString(mrlInfo.prevownacrul)));
        }
        public void PrintOwnerMLink()
        {
            Model.FireOnErrorGlobal(string.Concat("  AccrualLink ", m.accrualLink.name, " AccrualLinkSeasonCap ", Convert.ToString(m.accrualLink.mrlInfo.lnkSeasStorageCap)));
            Model.FireOnErrorGlobal(string.Concat(" RelativeUseOrder ", Convert.ToString(m.relativeUseOrder), " hydTable ", Convert.ToString(m.hydTable), " lastFill ", Convert.ToString(m.lastFill), " groupNumber ", Convert.ToString(m.groupNumber)));
            if (m.linkConstraintUPS != null)
            {
                Model.FireOnErrorGlobal(string.Concat("  linkConstraintUPS ", Convert.ToString(m.linkConstraintUPS.number)));
            }
            if (m.linkConstraintDWS != null)
            {
                Model.FireOnErrorGlobal(string.Concat("  linkConstraintDWS ", Convert.ToString(m.linkConstraintDWS.number)));
            }
            for (int i = 0; i < m.rentLimit.Length; i++)
            {
                if (m.rentLimit[i] != 0)
                {
                    Model.FireOnErrorGlobal(" rentLimits ");
                    for (int j = 0; j < m.rentLimit.Length; j++)
                    {
                        Console.Write(" ");
                        Console.Write(Convert.ToString(m.rentLimit[j]));
                    }
                    Model.FireOnErrorGlobal(" ");
                    break;
                }
            }
        }
        public void PrintOwnerMLInfo()
        {
            Model.FireOnErrorGlobal(" Flow    Flow0   Hi    Lo");
            Model.FireOnErrorGlobal(string.Concat("  ", Convert.ToString(mlInfo.flow), " ", Convert.ToString(mlInfo.flow0), " ", Convert.ToString(mlInfo.hi), " ", Convert.ToString(mlInfo.lo)));
        }
        public void PrintOwnerMRLInfo()
        {
            Model.FireOnErrorGlobal(string.Concat(" capown ", Convert.ToString(mrlInfo.cap_own), " stglft ", Convert.ToString(mrlInfo.stglft), " ownacrual ", Convert.ToString(mrlInfo.own_accrual)));
            Model.FireOnErrorGlobal(string.Concat("   prevstglft ", Convert.ToString(mrlInfo.prevstglft), " prevownacrul ", Convert.ToString(mrlInfo.prevownacrul)));
        }
        /// <summary>Sums stglft and ownaccural for accrual links; this should be done only after mrlInfo is set for accural links, su;m stglft of ownership and rent links.</summary>
        public long SumStglft()
        {
            LinkList ll;
            Link lnk;
            if (!this.IsAccrualLink)
            {
                throw new Exception("SumStglft is for accrual links only");
            }
            if (this.mlInfo == null || this.mrlInfo == null)
            {
                throw new Exception("SumStglft needs mlInfo and mrlInfo");
            }
            long sumStglft = 0;
            for (ll = this.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                lnk = ll.link;
                if (lnk.m.groupNumber == 0 || lnk.mrlInfo.groupID > 0)
                {
                    sumStglft += lnk.mrlInfo.stglft;
                }
            }
            for (ll = this.mlInfo.rLinkL; ll != null; ll = ll.next)
            {
                lnk = ll.link;
                sumStglft += lnk.mrlInfo.stglft;
            }
            if (sumStglft > this.mrlInfo.lnkSeasStorageCap || sumStglft < 0)
            {
                Model.FireOnErrorGlobal(string.Concat(" AccrualLink ", Convert.ToString(this.name), " sum stglft ", Convert.ToString(sumStglft), " lnkSeasStorageCap ", Convert.ToString(this.mrlInfo.lnkSeasStorageCap)));
                if (sumStglft > this.mrlInfo.lnkSeasStorageCap)
                {
                    sumStglft = this.mrlInfo.lnkSeasStorageCap;
                }
                if (sumStglft < 0)
                {
                    sumStglft = 0;
                }
            }
            return sumStglft;
        }
        /// <summary>Sums ownership links accrual for accrual links</summary>
        public long SumOwnAccrual()
        {
            LinkList ll;
            Link lnk;
            if (!this.IsAccrualLink)
            {
                throw new Exception("SumOwnAccrual is for accrual links only");
            }
            if (this.mlInfo == null || this.mrlInfo == null)
            {
                throw new Exception("SumOwnAccrual needs mlInfo and mrlInfo");
            }
            long sumOwnAccrual = 0;
            for (ll = this.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                lnk = ll.link;
                if (lnk.m.groupNumber == 0 || lnk.mrlInfo.groupID > 0)
                {
                    sumOwnAccrual += lnk.mrlInfo.own_accrual;
                }
            }
            if (sumOwnAccrual > this.mrlInfo.lnkSeasStorageCap || sumOwnAccrual < 0)
            {
                Model.FireOnErrorGlobal(string.Concat(" AccrualLink ", Convert.ToString(this.name), " sum of OwnAccrual and contribLast ", Convert.ToString(sumOwnAccrual), " lnkSeasStorageCap ", Convert.ToString(this.mrlInfo.lnkSeasStorageCap)));
                if (sumOwnAccrual > this.mrlInfo.lnkSeasStorageCap)
                {
                    sumOwnAccrual = this.mrlInfo.lnkSeasStorageCap;
                }
                if (sumOwnAccrual < 0)
                {
                    sumOwnAccrual = 0;
                }
            }
            return sumOwnAccrual;
        }
        // For accrual links sum ownership links contribLast
        public long SumContribLast()
        {
            LinkList ll;
            Link lnk;
            if (!this.IsAccrualLink)
            {
                throw new Exception("SumOwnAccrual is for accrual links only");
            }
            if (this.mlInfo == null || this.mrlInfo == null)
            {
                throw new Exception("SumOwnAccrual needs mlInfo and mrlInfo");
            }
            long sumContribLast = 0;
            for (ll = this.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                lnk = ll.link;
                if (lnk.m.groupNumber == 0 || lnk.mrlInfo.groupID > 0)
                {
                    sumContribLast += lnk.mrlInfo.contribLast;
                }
            }
            return sumContribLast;
        }

        public int CompareTo(Object obj)
        {
            if (!obj.GetType().Equals(typeof(Link)))
            {
                throw new ArgumentException("obj is not the same type as this instance.");
            }

            //return this.number.CompareTo((obj as Link).number);
            return this.uid.CompareTo((obj as Link).uid);
        }

        /// <summary>
        /// Sets mlInfo.hi based on m.maxConstant or mlInfo.hiVariable constrained
        /// by optional minimum constraint.
        /// </summary>
        /// <param name="timestep">current model timestep</param>
        /// <param name="minConstraint">optional minimum constraint</param>
        internal void SetHI(int timestep, long minConstraint = long.MaxValue)
        {
            /*
             * BLounsbury: may need to consider hydrologic state for hiVariable,
             * but it was never used when I refactored to this function so I
             * ignored it.
             */
            if (m.maxConstant > 0)
            {
                mlInfo.hi = m.maxConstant;
            }
            else if (mlInfo.hiVariable != null && mlInfo.hiVariable.GetLength(0) > 0)
            {
                mlInfo.hi = mlInfo.hiVariable[timestep, 0];
            }
            else
            {
                mlInfo.hi = 0;
            }

            mlInfo.hi = Math.Min(mlInfo.hi, minConstraint);
        }

    }
}
