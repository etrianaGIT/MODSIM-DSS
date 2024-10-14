using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Initialize time step for <c>operate</c>.</summary>
    public static class OperateInit
    {
        public static int HasChannelLoss(Model mi)
        {
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                // should use a list of real links
                if (l.m.loss_coef > 0 && l.m.loss_coef < 1)
                {
                    return 1;
                }
            }
            return 0;
        }
        public static long SumMaxVolume(Model mi)
        {
            long itot = 0;
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                itot += n.m.max_volume;
            }
            return itot;
        }
        public static void Init_stend(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                n.mnInfo.stend = n.m.starting_volume;
            }
        }
        public static void Init_link_flow_lnktot(Model mi)
        {
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                if (!l.mlInfo.isArtificial)
                {
                    // should use a list of real links
                    for (int i = 0; i < 12; i++)
                    {
                        l.mrlInfo.link_flow[i] = 0;
                        l.mrlInfo.lnktot = 0;
                    }
                }
            }
        }
        public static void Set_gwrtnLink_cost(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Link l = mi.mInfo.realNodesList[i].mnInfo.gwrtnLink;
                if (l != null)
                {
                    l.mlInfo.cost = -50000 + 10 * l.to.m.pcost;
                }
            }
        }
        public static void Init_current_evap(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mrlInfo.current_evap = 0;
            }
        }
        public static void Reset_accrualLinkList_cost(Model mi)
        {
            // This should go away; we should not be changing the cost on these links
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                l.mlInfo.cost = l.m.cost;
            }
        }
        public static void Zero_accrualLink_flow(Model mi)
        {
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                l.mlInfo.flow = 0;
            }
        }
        public static void Zero_demLink_lo(Model mi)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Link l = mi.mInfo.demList[i].mnInfo.demLink;
                l.mlInfo.lo = 0;
            }
        }
        public static void Set_realLink_lo_hi(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                l.mrlInfo.accumsht = 0;
                l.mlInfo.flow0 = 0;
                l.mlInfo.lo = l.m.min;
                /* RKL should exclude owership links */
                if (!(l.mlInfo.isAccrualLink) && !(l.mlInfo.isLastFillLink))
                {
                    l.SetHI(mi.mInfo.CurrentModelTimeStepIndex);
                }
            }
        }
        public static void CheckSeasonCapDate(Model mi, Model mi1, DateTime currentDate)
        {
            if (currentDate.Month != mi1.seasCapDate.Month || currentDate.Day != mi1.seasCapDate.Day || currentDate.Millisecond != 0)
            {
                return;
            }

            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.lnkallow != 0)
                {
                    //we should not have to check for this stuff any more; accrual link lnkallow = 0
                    if ((mi.mInfo.ownerList.Length == 0) || !(l.mlInfo.isAccrualLink) || !(l.mlInfo.isLastFillLink))   // rkl 11/16/05
                    {
                        l.mrlInfo.lnktot = 0;
                        l.SetHI(mi.mInfo.CurrentModelTimeStepIndex, l.m.lnkallow);
                    }
                }
            }
        }
        public static void UpdateLnkTot(Model mi, int outputtimestepindex)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.lnkallow > 0)
                {
                    l.mrlInfo.lnktot += l.mrlInfo.link_flow[outputtimestepindex];
                }
            }
        }
        public static void SetAccrual2Stglft(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                // we don't do anything with rent links; this seems ok, but
                // what about groupNumber > 0 links?? do we wish to filter these out??
                if (l.mrlInfo.irent != -1)
                {
                    l.mrlInfo.own_accrual = l.mrlInfo.stglft;
                    l.mrlInfo.prevownacrul = l.mrlInfo.stglft;
                }
            }
        }
        public static void Check_hi_SeasonCap(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.lnkallow > 0)
                {
                    if (l.m.lnkallow - l.mrlInfo.lnktot < l.mlInfo.hi)
                    {
                        l.mlInfo.hi = l.m.lnkallow - l.mrlInfo.lnktot;
                        if (l.mlInfo.hi < 0)
                        {
                            l.mlInfo.hi = 0;
                        }
                        if (l.mlInfo.lo > l.mlInfo.hi)
                        {
                            l.mlInfo.lo = 0;
                        }
                    }
                }
            }
        }
        public static void ZeroNodeArrays(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                n.mnInfo.evpt = 0;
                n.mnInfo.start = 0;
                n.mnInfo.ithruSTG0 = 0;
                n.mnInfo.ithruSTG = 0;
                n.mnInfo.ithruNF0 = 0;
                n.mnInfo.ithruNF = 0;
                for (LinkList ll = n.OutflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.m.loss_coef == 1.0)   // routing link
                    {
                        Node n2 = l.m.returnNode;
                        n2.mnInfo.iroutreturn = l.mrlInfo.linkPrevflow[0];
                    }
                }
            }
        }
        public static void SetNode_start(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                n.mnInfo.start = n.mnInfo.stend;
            }
        }
        public static void Zero_demout_fldflow(Model mi)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                n.mnInfo.demout = 0;
                n.mnInfo.fldflow = 0;
            }
        }
        public static void Zero_ownerLinks_hi(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mlInfo.hi = 0;
                l.mlInfo.lo = 0;
            }
        }
        public static void ChannelLossConvgFlag(Model mi)
        {
            mi.mInfo.convg1 = true;
            for (int i = 0; (i < mi.mInfo.realLinkList.Length) && mi.mInfo.convg1; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.loss_coef >= 1.0)
                {
                    mi.mInfo.convg1 = false;
                    break;
                }
            }
        }
        public static void Zero_ideep(Model mi)
        {
            for (Node n2 = mi.firstNode; n2 != null; n2 = n2.next)
            {
                // should limit to real nodes
                n2.mnInfo.ideep0 = 0;
                n2.mnInfo.iseep0 = 0;
            }
        }
        public static void ShiftStend(Model mi)
        {
            // should limit to real reservoir nodes
            for (Node n2 = mi.firstNode; n2 != null; n2 = n2.next)
            {
                n2.mnInfo.stend0 = n2.mnInfo.stend;
            }
        }
        public static void ZeroCloss(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                l.mrlInfo.closs = 0;
            }
        }
        public static void ResetOwnerLinkCost(Model mi)
        {
            // should this set mlInfo->cost to new relative use order??
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mlInfo.cost = l.m.cost;
            }
        }

        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * CalculateOwnerAccumsht - Compare flows to requested flows in stg step
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * For each storage ownership link, we generate an accumulated difference
         * between requested flow and delivered flow in the storage step.  The
         * differences cause the storage owner to request less water from this source.
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * Note: What happens when the owner is pulling storage water (upstream ISF)?
        \* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - */
        public static void CalculateOwnerAccumsht(Model mi)
        {
            long iter = mi.mInfo.Iteration;

            if (iter % 2 == 0 && iter > 0)
            {
                if (iter > mi.mInfo.SMOOTHOPER - 4)
                {
                    for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
                    {
                        Link l = mi.mInfo.ownerList[i];
                        long d = l.mlInfo.hi - l.mlInfo.flow;
                        if (d > l.mrlInfo.accumsht && iter < mi.mInfo.ACCUMSHTLIMIT)
                        {
                            l.mrlInfo.accumsht = d;
                        }
                    }
                }
            }
        }

        /* Bypass Credit Links:
         *
         * Bypass Credit Links satisfy a portion of the demand in this demand node.
         *
         * This routine saves data in flow0 for allocate and others to watch.
         *
         *
         * optimization notes:
         *   This logic can be included in the ExchangeDems routine because
         *   we are looping over the same demands.
         */
        public static void BypassCreditLinksSaveData(Model mi)
        {
            int iter = mi.mInfo.Iteration;

            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                if (mi.mInfo.demList[i].m.pdstrm != null && mi.mInfo.demList[i].m.idstrmx[0] != null)
                {
                    Link l = mi.mInfo.demList[i].m.pdstrm;
                    if (iter > mi.mInfo.SMOOTHFLOTHRU + 20 && l.mlInfo.flow0 > l.mlInfo.flow)
                    {
                        // Do not change the flow0, that way we ramp the demand down.
                    }
                    else
                    {
                        l.mlInfo.flow0 = l.mlInfo.flow;
                    }
                }
            }
        }
        public static void ExchangeLimitLinksSaveData(Model mi, int nmown, int iodd)
        {
            if (iodd == 1 || nmown == 0)
            {
                for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
                {
                    Link l = mi.mInfo.realLinkList[i];
                    if (l.m.exchangeLimitLinks != null)
                    {
                        l.m.exchangeLimitLinks.mlInfo.flow0 = l.m.exchangeLimitLinks.mlInfo.flow;
                        l.mlInfo.flow0 = l.mlInfo.flow;
                    }
                }
                for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
                {
                    Node n = mi.mInfo.realNodesList[i];
                    for (int j = 0; j < 15; j++)
                    {
                        if (n.m.watchMaxLinks[j] != null)
                        {
                            n.m.watchMaxLinks[j].mlInfo.flow0 = n.m.watchMaxLinks[j].mlInfo.flow;
                        }
                        if (n.m.watchMinLinks[j] != null)
                        {
                            n.m.watchMinLinks[j].mlInfo.flow0 = n.m.watchMinLinks[j].mlInfo.flow;
                        }
                        if (n.m.watchLnLinks[j] != null)
                        {
                            n.m.watchLnLinks[j].mlInfo.flow0 = n.m.watchLnLinks[j].mlInfo.flow;
                        }
                        if (n.m.watchLogLinks[j] != null)
                        {
                            n.m.watchLogLinks[j].mlInfo.flow0 = n.m.watchLogLinks[j].mlInfo.flow;
                        }
                        if (n.m.watchExpLinks[j] != null)
                        {
                            n.m.watchExpLinks[j].mlInfo.flow0 = n.m.watchExpLinks[j].mlInfo.flow;
                        }
                        if (n.m.watchSqrLinks[j] != null)
                        {
                            n.m.watchSqrLinks[j].mlInfo.flow0 = n.m.watchSqrLinks[j].mlInfo.flow;
                        }
                        if (n.m.watchPowLinks[j] != null)
                        {
                            n.m.watchPowLinks[j].mlInfo.flow0 = n.m.watchPowLinks[j].mlInfo.flow;
                        }
                    }
                }
            }
        }

    }
}
