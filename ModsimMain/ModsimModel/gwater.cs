//#define DEBUG_Infil

using System;
using System.IO;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersGwater
    {
        public static bool chanloss(Model mi)
        {
            bool convg = true;
            long iter = mi.mInfo.Iteration;

            // Zero channel loss link's hi bound.
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                if (n.mnInfo.chanLossLink != null)
                {
                    n.mnInfo.chanLossLink.mlInfo.hi = 0;
                    n.mnInfo.chanLossLink.mlInfo.lo = 0;
                }
            }

            // Zero out output values for return
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.loss_coef > 0 && l.m.loss_coef < 1 && l.m.returnNode != null)
                {
                    l.m.returnNode.mnInfo.clossreturn = 0;
                }
            }

            /* calculate channel losses */
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.loss_coef > 0 && l.m.loss_coef < 1)
                {
                    /* channel loss groundwater link
                     * This change allows to have more that one channel loss 
                     * link (with different loss coefficients) leaving a node
                     *  it relies on having enough water to fullfill the 
                     *  channel loss link water requirement. In other words it
                     *  assumes that the calculated channel losses for each 
                     *  link will happen in the next solution (this is very
                     *  likely to happen because the very small cost these 
                     *  links have (-99999999)
                     *  Prev Calculation: totalFlow = l.mlInfo.flow + 
                     *  l.from.mnInfo.chanLossLink.mlInfo.flow; */
                    
                    Link chanLossL = l.from.mnInfo.chanLossLink;
                    long totalFlow = l.mlInfo.flow + l.mrlInfo.closs;
                    long depletion = (long)(l.m.loss_coef * totalFlow + .5);
                    chanLossL.mlInfo.hi += depletion;
                    l.mlInfo.hi += l.mrlInfo.closs - depletion;
                    l.mrlInfo.closs = depletion;
                }
            }

            /* calculate return flows to downstream nodes */
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.loss_coef > 0 && l.m.loss_coef < 1)
                {
                    long chanLossFlow = l.mrlInfo.closs;
                    if (l.m.returnNode != null)
                    {
                        Link rtnLink = l.m.returnNode.mnInfo.gwrtnLink;
                        rtnLink.mlInfo.lo += (long)(((double)l.m.lagfactors[0] * (double)chanLossFlow) + (double)l.mrlInfo.linkPrevflow[0] + DefineConstants.ROFF);
                        rtnLink.mlInfo.hi += (long)(((double)l.m.lagfactors[0] * (double)chanLossFlow) + (double)l.mrlInfo.linkPrevflow[0] + DefineConstants.ROFF);
                        l.m.returnNode.mnInfo.clossreturn += (long)(((double)l.m.lagfactors[0] * (double)chanLossFlow) + (double)l.mrlInfo.linkPrevflow[0] + DefineConstants.ROFF);
                    }
                }
            }

            // if in STG step or no owners, check convergence
            if (mi.mInfo.Iteration % 2 == 1 || mi.mInfo.ownerList.Length == 0)
            {
                /* convergence criteria */
                for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
                {
                    Link l = mi.mInfo.realLinkList[i];
                    if (l.m.loss_coef > 0 && l.m.loss_coef < 1)
                    {
                        if (!Utils.NearlyEqual((double)l.mrlInfo.closs0, (double)l.mrlInfo.closs, mi.gw_cp))
                        {
                            convg = false;

                            DateTime dt = mi.TimeStepManager.Index2Date(mi.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);

                            if (iter > mi.maxit - 10)
                            {
                                Console.WriteLine("No convergence in channel loss:");
                                Console.WriteLine("    " + dt.ToString(TimeManager.DateFormat));
                                Console.WriteLine(string.Format("link: {0}, flow: {1}, loss: {2}, loss0: {3}", l.number, l.mlInfo.flow, l.mrlInfo.closs, l.mrlInfo.closs0));
                            }
                            if (iter > 50)
                            {
                                mi.FireOnError("No convergence in channel loss:");
                                mi.FireOnError("    " + dt.ToString(TimeManager.DateFormat));
                                mi.FireOnMessage(string.Format("link: {0}, flow: {1}, loss: {2}, loss0: {3}", l.number, l.mlInfo.flow, l.mrlInfo.closs, l.mrlInfo.closs0));
                            }
                        }
                    }
                }

                /* save an old version of closs related to convergence */
                for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
                {
                    Link l = mi.mInfo.realLinkList[i];
                    if (l.m.loss_coef > 0 && l.m.loss_coef < 1)
                    {
                        l.mrlInfo.closs0 = l.mrlInfo.closs;
                    }
                }
            }
            return convg;
        }

        /*
         * sumflow = total flow satisfying demand + flowing to gwater
         * ideep = calculated deep percolation for this node
         * iseep = calculated return flow for this time period that
         *         originated at this node.
         * This routine calculates deep percolation for the current month.
         * It also makes return flow calculations for all demands with return
         * flow.
         */
        // This routine is primarily for demand infiltration
        public static bool demands(Model mi, ref long totdep, Model mi1)
        {
            bool convg = true;
            int jj;
            LagInfo lInfo;
            long predepit = 0;
            long predepit2 = 0;
            long iter = mi.mInfo.Iteration;

            if (iter == 0)
            {
                for (int i = 0; i < mi.mInfo.demList.Length; i++)
                {
                    Node n = mi.mInfo.demList[i];
                    Link l = n.mnInfo.gwoutLink;
                    if (l != null)
                    {
                        l.mlInfo.hi = l.mlInfo.lo = l.mlInfo.flow = 0;
                    }
                    n.mnInfo.ideep0 = 0;
                }
            }

            // Partial flow returns
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                Link l = n.mnInfo.gwrtnLink;
                if (l != null && n.m.PartialFlows != null && (n.m.PartialFlows[0] > 0))
                {
                    l.mlInfo.hi += n.m.PartialFlows[0];
                    l.mlInfo.lo += n.m.PartialFlows[0];
                }
                l = n.mnInfo.gwoutLink;
                if (l != null && n.m.PartialFlows != null && (n.m.PartialFlows[0] < 0))
                {
                    l.mlInfo.hi += Math.Abs(n.m.PartialFlows[0]);
                    l.mlInfo.lo += Math.Abs(n.m.PartialFlows[0]);
                }
            }

            // Demand infiltration calculations
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                long demand = 0;
                long incRtn = 0;
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    demand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                }
                Node n1 = mi1.mInfo.demList[i];

                if (n.mnInfo.gwoutLink != null)
                {
                    long ideep = 0;
                    long sumflow = n.mnInfo.demLink.mlInfo.flow + n.mnInfo.gwoutLink.mlInfo.flow;
                    if (sumflow > 0)
                    {
                        ideep = (long)(n.mnInfo.infiltrationrate[mi.mInfo.CurrentModelTimeStepIndex, 0] * (double)sumflow + .51);

                        //RKL This may be causing more problems than it is solving in some cases
                        // We may wish to allow the user to tweak things on their own
                        if (iter > mi.mInfo.GWSMOOTH && ideep > n.mnInfo.ideep0 + 1)
                        {
                            ideep = (long)(0.8 * (ideep - n.mnInfo.ideep0) + n.mnInfo.ideep0 + 0.51);
                        }
                        n.mnInfo.demLink.mlInfo.hi = Math.Max(demand - ideep, 0);
                        // RKL
                        // I would like to change this; I would like to have a node between the demand and the artificial demand node
                        // then hook up the artificial demand link between the demand and this intermeadiate node
                        //  the upper bound of the demand link DOES NOT change
                        // we have the groundwater infiltration link from the intermeadiate node to the gw node with a very high benefit
                        //  the rest of the flow goes to the artificial demand node at zero cost
                        // RKL
                        if (demand == 0) // just for safety?
                        {
                            ideep = 0;
                        }
                    }
                    else
                    {
                        n.mnInfo.demLink.mlInfo.hi = demand;
                    }
                    n.mnInfo.gwoutLink.mlInfo.hi = ideep;

                    if (mi.backRouting && iter > 3 && mi != mi1)
                    {
                        //Set the minimum bound in the groundwater link to the infiltration of the corresponding previous downstream time demand flows.
                        if (n.mnInfo.gwoutLink.mlInfo.hi >= n.mnInfo.gwoutLink.mlInfo.minGWFlowBackRouting)
                        {
                            n.mnInfo.gwoutLink.mlInfo.lo = n.mnInfo.demLink.mlInfo.minGWFlowBackRouting;
                        }
                        else
                        {
                            mi.FireOnError(string.Format("Min flow in Groundwater Link greater than max bound, node: {0}", n.number));
                        }
                    }
                    else
                    {
                        n.mnInfo.gwoutLink.mlInfo.lo = 0;
                    }
                    if (iter > 0)
                    {
                        totdep += n.mnInfo.gwoutLink.mlInfo.flow;
                    }

                    bool jflagmlb = false;
                    for (int j = 0; j < mi.TimeStepManager.noModelTimeSteps; j++)
                    {
                        if (n.mnInfo.infiltrationrate[j, 0] > 0)
                        {
                            jflagmlb = true;
                            break;
                        }
                    }

                    // if this node has infiltration, calculate return flows
                    if (jflagmlb && n.m.infLagi != null)
                    {
                        if (iter == 0)
                        {
                            ideep = 0;
                        }
#if DEBUG_Infil
                    bool written = false;
#endif
                        for (jj = 0, lInfo = n.m.infLagi; lInfo != null; lInfo = lInfo.next, jj++)
                        {
                            if (lInfo.location != null) // not guaranteed to exist
                            {
                                Node n2 = mi1.FindNode(lInfo.location.number);
                                double RF;
                                if (mi == mi1)
                                {
                                    RF = 1F;
                                }
                                else
                                {
                                    RF = Routing.RFactorForReturnFlow(n1.backRRegionID, n2.backRRegionID);
                                }
                                Link larc = lInfo.location.mnInfo.gwrtnLink;
                                incRtn = (long)(lInfo.percent * (double)lInfo.lagInfoData[0] * (double)ideep * (RF) + (double)n.mnInfo.demPrevFlow[jj, 0] + DefineConstants.ROFF);
                                larc.mlInfo.lo += incRtn;
                                larc.mlInfo.hi += incRtn;
                            }
                        }
                    }
                    // shift values to hold 1 iter back
                    predepit += n.mnInfo.ideep0;
                    n.mnInfo.ideep0 = ideep;
                    predepit2 += ideep;
                }
            }


            if (iter == 0)
            {
                return false;
            }

            if (totdep + predepit > 0)
            {
                double rdiff = Utils.RelativeDiff((double)predepit, (double)totdep);
                if (!Utils.NearlyEqual((double)predepit, (double)totdep, mi.gw_cp))
                {
                    convg = false;

                    DateTime dt = mi.TimeStepManager.Index2Date(mi.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);

                    if (iter > mi.maxit - 10)
                    {
                        Console.WriteLine("No convergence percolation:");
                        Console.WriteLine("    " + dt.ToString(TimeManager.DateFormat));
                        Console.WriteLine(string.Format("    iter: {0}, predepit: {1}, totaldep: {2}, rdiff: {3:0.00%}", iter, predepit, totdep, rdiff));
                    }
                    if (iter > 50)
                    {
                        mi.FireOnError("No convergence percolation:");
                        mi.FireOnError("    " + dt.ToString(TimeManager.DateFormat));
                        mi.FireOnError(string.Format("    iter: {0}, predepit: {1}, totaldep: {2}, rdiff: {3:0.00%}", iter, predepit, totdep, rdiff));
                    }
                }
                //Second convergence criteria added to assure convergence to a solution
                //	This checks for the returns from the current solution and the previous solution to be
                //	between the aceptable convergence percentage.
                if (!Utils.NearlyEqual((double)predepit, (double)predepit2, mi.gw_cp)) // we are not converged
                {
                    convg = false;
                }
            }
            return convg;
        }

        /*****************************************************************************
        gwater - Calculate all groundwater effects and return convergence
        -------------------------------------------------------------------------------
        Gwater is currently called every other iteration for networks with storage
        allocation logic.  It calls routines to calculate reservoir seepage, channel loss,
        deep percolation, groundwater pumping, routing and river depletion as a result
        of groundwater pumping.  Convergence is calculated in each subroutine and the
        global gwater convergence flag is updated.
        \*****************************************************************************/
        private static long predep;
        private static long presep;
        private static long totdep;
        private static long totsep;
        public static void gwater(Model mi, ref long maxd2, ref bool convg, Model mi1)
        {
            bool demandsConv;
            bool seepageConv;
            bool chanLossConv = true;
            bool pumpConv = true;
            bool routConv;

            // Step 01d - Set bounds on arcs from groundwater node at zero or pumping capacity
            totdep = 0;
            for (LinkList llPtr = mi.mInfo.artGroundWatN.OutflowLinks; llPtr != null; llPtr = llPtr.next)
            {
                Link l = llPtr.link;
                if (l != mi.mInfo.gwaterToInf)
                {
                    l.mlInfo.lo = 0;
                    l.mlInfo.hi = l.to.m.pcap;
                }
            }

            // Step 01e - SET BOUNDS ON ARCS TO GROUNDWATER NODE AT ZERO
            for (LinkList llPtr = mi.mInfo.artGroundWatN.InflowLinks; llPtr != null; llPtr = llPtr.next)
            {
                Link l = llPtr.link; // get a hold of the link
                if (l != mi.mInfo.infToGwater)
                {
                    l.mlInfo.lo = 0;
                    l.mlInfo.hi = 0;
                }
            }

            if (mi.mInfo.Iteration == 0)
            {
                seepageConv = GlobalMembersGwater.seepage(mi, ref totsep);
                demandsConv = GlobalMembersGwater.demands(mi, ref totdep, mi1);
                pumpConv = GlobalMembersGwater.pump(mi, ref totdep);
                routConv = false;
                presep = totsep;
                predep = 0;
            }
            else
            {
                seepageConv = GlobalMembersGwater.seepage(mi, ref totsep);
                if (mi == mi1)
                {
                    routConv = GlobalMembersGwater.rout(mi, ref maxd2);
                }
                else
                {
                    routConv = true;
                }
                demandsConv = GlobalMembersGwater.demands(mi, ref totdep, mi1);
                pumpConv = GlobalMembersGwater.pump(mi, ref totdep);
                chanLossConv = GlobalMembersGwater.chanloss(mi);
                predep = totdep;
            }

            // Step 7 - Calculate the sum of upper bounds on surface water depletion arcs

            convg = seepageConv && demandsConv && pumpConv && chanLossConv && routConv;
            if (mi.maxit - mi.mInfo.Iteration < 10 && !convg)
            {
                Console.WriteLine("seepageConv " + seepageConv);
                Console.WriteLine("demandsConv " + demandsConv);
                Console.WriteLine("pumpConv " + pumpConv);
                Console.WriteLine("chanLossConv " + chanLossConv);
                Console.WriteLine("routConv " + routConv);
            }
        }

        /*****************************************************************************
        rout - Calculate routed flow, adjust artificial links, and calculate convergence
            This is for stream flow routing (and back routing)
        -------------------------------------------------------------------------------
        \*****************************************************************************/
        private static long rout_LastMonthPrinted = -1;
        public static bool rout(Model mi, ref long maxd2)
        {
            bool routConvg = true;
            maxd2 = 0;

            // Calculate how much flow and put as hi bound for art routing link
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.loss_coef >= 1) // routing link
                {
                    // Calculate how much flow and put as hi bound for routing link
                    if (l.to.mnInfo.routingLink == null)
                    {
                        mi.FireOnError("Problem with routing - missing rout link");
                        mi.FireOnError(string.Format("node: {0}", l.to.number.ToString()));
                    }
                    else
                    {
                        // Make artificial routing link want the flow from this
                        // routing link that shows up at least a day after day 0.
                        long rLossToLater = (long)(l.mlInfo.flow * (1.0 - l.m.lagfactors[0]) + DefineConstants.ROFF);

                        //Commented to avoid overwrite it in the routing routine
                        l.to.mnInfo.routingLink.mlInfo.hi = rLossToLater;
                        //Set Convergence for routing - for Physical Routing
                        if (l.mrlInfo.closs != rLossToLater)
                        {
                            routConvg = false;
                        }
                        l.mrlInfo.closs = rLossToLater;

                        // Now copy the flow in this link through the routing lag factors into its return location
                        if (l.m.returnNode != null) // Not Legal - but we should not crash...
                        {
                            //iroutreturn is only for the present time step calculation.
                            if (mi.backRouting)
                            {
                                //In backrouting case, the link downstream of the routing link has a very high cost, forcing the flow to
                                //	not go through there.  The current flow need to be returned downstream.
                                l.m.returnNode.mnInfo.iroutreturn = l.mrlInfo.linkPrevflow[0] + (long)(((double)l.mlInfo.flow * l.m.lagfactors[0]) + DefineConstants.ROFF);
                            }
                            else // physical routing only
                            {
                                //Only previously routed flows need to be returned.
                                l.m.returnNode.mnInfo.iroutreturn = l.mrlInfo.linkPrevflow[0];
                            }
                        }
                        else
                        {
                            if (rout_LastMonthPrinted != mi.mInfo.CurrentModelTimeStepIndex)
                            {
                                rout_LastMonthPrinted = mi.mInfo.CurrentModelTimeStepIndex;
                                mi.FireOnError(string.Format("Link {0} is missing its return node.", l.number.ToString()));
                            }
                        }
                    }
                }
            }

            return routConvg;
        }

        // Take care of groundwater depletion
        // WARNING no convergence check here except for if iter == 0
        public static bool pump(Model mi, ref long totdep)
        {
            bool convg = true;
            long iter = mi.mInfo.Iteration;

            if (iter == 0)
            {
                for (int i = 0; i < mi.mInfo.demList.Length; i++)
                {
                    Node n = mi.mInfo.demList[i];
                    n.mnInfo.pump0 = 0;
                    for (LagInfo lInfo = n.m.pumpLagi; lInfo != null; lInfo = lInfo.next)
                    {
                        if (lInfo.location != null)
                        {
                            lInfo.location.mnInfo.gwoutLink.mlInfo.hi = 0;
                        }
                    }
                }
            }

            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                if (n.m.pcap > 0)
                {
                    long sumflow = n.mnInfo.gwrtnLink.mlInfo.flow;
                    long ipump = 0;
                    if (sumflow > 0)
                    {
                        ipump = sumflow;
                    }
                    if (iter > 0)
                    {
                        totdep += n.mnInfo.gwrtnLink.mlInfo.flow;
                    }
                    /* check for infiltration rate and set jflagmlb for it */
                    // not yet needed for demands
                    /* if this node has pumping, calculate pumping */
                    if (n.m.pcap > 0)
                    {
                        long iseep = n.mnInfo.gwrtnLink.mlInfo.flow;
                        // k is the index of the depletion location laginfo data
                        int k;
                        LagInfo lInfo;
                        for (lInfo = n.m.pumpLagi, k = 0; lInfo != null; lInfo = lInfo.next, k++)
                        {
                            if (lInfo.location != null)
                            {
                                Link larc = lInfo.location.mnInfo.gwoutLink;
                                larc.mlInfo.cost = -2000000;
                                larc.mlInfo.lo = 0;
                                larc.mlInfo.hi += (long)(lInfo.lagInfoData[0] * (double)iseep * (double)lInfo.percent + (double)n.mnInfo.demPrevDepn[k, 0] + DefineConstants.ROFF);
                            }
                        }
                    }
                    /* shift values to hold 1 iter back */
                    n.mnInfo.pump0 = ipump;
                }
            }

            if (iter == 0)
            {
                return false;
            }

            return convg;
        }

        /*****************************************************************************
        RouteTimeSeries - Shift previous days of routed flows
        -------------------------------------------------------------------------------
        Rename to ShiftRoutePrevFlow.
        \*****************************************************************************/

        public static void RouteTimeSeries(Model mi)
        {
            // Calculate how much flow and put as hi bound for art routing link
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.loss_coef >= 1) // routing link
                {
                    // Calculate how much flow and put as hi bound for routing link
                    if (l.to.mnInfo.routingLink == null)
                    {
                        mi.FireOnError(string.Format("Problem with routing - missing rout link Node: ", l.to.number.ToString()));
                    }
                    else
                    {
                        for (int k = 1; k < mi.maxLags; k++)
                        {
                            l.mrlInfo.linkPrevflow[k] += (long)((double)l.mlInfo.flow * l.m.lagfactors[k] + 0.5);
                        }
                    }
                }
            }
        }

        /*****************************************************************************
        seepage - Calculate reservoir seepage
        -------------------------------------------------------------------------------
        \*****************************************************************************/

        public static bool seepage(Model mi, ref long totsep)
        {
            bool convg = true;
            long iter = mi.mInfo.Iteration;

            totsep = 0;
            if (iter == 0)
            {
                for (int i = 0; i < mi.mInfo.resList.Length; i++)
                {
                    Node n = mi.mInfo.resList[i];
                    n.mnInfo.iseep0 = 0;
                }
            }

            long predepit = 0;
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if ((n.m.max_volume > 0) && (n.m.seepg > 0) && n.mnInfo.gwoutLink != null)
                {
                    Link l = n.mnInfo.gwoutLink;

                    // allow groundwater to leave
                    l.mlInfo.hi = (iter == 0) ? ((long)(n.m.seepg * (double)n.mnInfo.start + DefineConstants.ROFF)) : ((long)(n.m.seepg * (((double)n.mnInfo.start + (double)n.mnInfo.stend) / 2) + DefineConstants.ROFF));
                    long ideep = l.mlInfo.flow;
                    l.mlInfo.cost = -DefineConstants.COST_LARGE + 1; // 99999998;

                    // flow smooth in later iterations
                    if (iter > 0)
                    {
                        totsep += ideep;
                    }
                    if (iter > mi.mInfo.GWSMOOTH && ideep > n.mnInfo.iseep0 + 1)
                    {
                        ideep = (long)(0.8 * (ideep - n.mnInfo.iseep0) + n.mnInfo.iseep0 + 0.51);
                    }

                    // calculate return flow from this and previous months
                    int jj;
                    LagInfo lInfo;
                    for (jj = 0, lInfo = n.m.infLagi; lInfo != null; lInfo = lInfo.next, jj++)
                    {
                        if (lInfo.location != null)
                        {
                            Link larc = lInfo.location.mnInfo.gwrtnLink;

                            larc.mlInfo.hi += (long)(lInfo.lagInfoData[0] * (double)ideep * (double)lInfo.percent + (double)n.mnInfo.demPrevFlow[jj, 0] + DefineConstants.ROFF);

                            larc.mlInfo.lo += (long)(lInfo.lagInfoData[0] * (double)ideep * (double)lInfo.percent + (double)n.mnInfo.demPrevFlow[jj, 0] + DefineConstants.ROFF);
                        }
                    }
                    predepit += n.mnInfo.iseep0;
                    n.mnInfo.iseep0 = ideep;
                }
            }

            if (iter == 0)
            {
                return false;
            }

            if (totsep + predepit > 0)
            {
                if (!Utils.NearlyEqual((double)predepit, (double)totsep, mi.gw_cp))
                {
                    convg = false;
                }
            }
            return convg;
        }

        /*
         * Use ideep0 and infil and set of lag factors to generate a set of
         * previous flows for lagging.
         */
        public static void setDemandPrevflow(Model mi)
        {
            int j;
            LagInfo lInfo;

            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                if (n.m.pcap != 0)
                {
                    for (j = 0, lInfo = n.m.pumpLagi; lInfo != null; lInfo = lInfo.next, j++)
                    {
                        if (lInfo.location != null) // make sure pointer is ok... (not guaranteed)
                        {
                            for (int k = 1; k < lInfo.numLags; k++)
                            {
                                if (k < lInfo.lagInfoData.Length)
                                {
                                    n.mnInfo.demPrevDepn[j, k] += (long)((double)n.mnInfo.pump0 * lInfo.percent * lInfo.lagInfoData[k] + .5);
                                }
                                else
                                {
                                    n.mnInfo.demPrevDepn[j, k] += 0;
                                }
                            }
                        }
                    }
                }
                if (n.mnInfo.ideep0 > 0)
                {
                    for (j = 0, lInfo = n.m.infLagi; lInfo != null; lInfo = lInfo.next, j++)
                    {
                        if (lInfo.location != null) // make sure pointer is ok... (not guarenteed)
                        {
                            for (int k = 1; k < lInfo.numLags; k++)
                            {
                                double lagcoeff = 0.0;
                                if (k < lInfo.lagInfoData.Length)
                                {
                                    lagcoeff = lInfo.lagInfoData[k];
                                }
                                n.mnInfo.demPrevFlow[j, k] += (long)((double)n.mnInfo.ideep0 * lInfo.percent * lagcoeff + .5);
                            }
                        }
                    }
                }
            }
        }

        /*
         * Move previous flows over by one month when we change months.
         * Should call this routine in operate after convergence.
         */

        public static void shiftDemandPrevflow(Model mi)
        {
            int j;
            LagInfo lInfo;

            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];

                // infiltration
                for (j = 0, lInfo = n.m.infLagi; lInfo != null; lInfo = lInfo.next, j++)
                {
                    if (n.mnInfo.demPrevFlow != null)
                    {
                        for (int k = 0; k < lInfo.numLags - 1; k++)
                        {
                            n.mnInfo.demPrevFlow[j, k] = n.mnInfo.demPrevFlow[j, k + 1];
                        }
                        n.mnInfo.demPrevFlow[j, lInfo.numLags - 1] = 0;
                    }
                }

                // pumping
                for (j = 0, lInfo = n.m.pumpLagi; lInfo != null; lInfo = lInfo.next, j++)
                {
                    if (n.mnInfo.demPrevDepn != null)
                    {
                        for (int k = 0; k < lInfo.numLags - 1; k++)
                        {
                            n.mnInfo.demPrevDepn[j, k] = n.mnInfo.demPrevDepn[j, k + 1];
                        }
                        n.mnInfo.demPrevDepn[j, lInfo.numLags - 1] = 0;
                    }
                }
            }

            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];

                // seepage
                for (j = 0, lInfo = n.m.infLagi; lInfo != null; lInfo = lInfo.next, j++)
                {
                    if (n.mnInfo.demPrevFlow != null)
                    {
                        for (int k = 0; k < lInfo.numLags - 1; k++)
                        {
                            n.mnInfo.demPrevFlow[j, k] = n.mnInfo.demPrevFlow[j, k + 1];
                        }
                        n.mnInfo.demPrevFlow[j, lInfo.numLags - 1] = 0;
                    }
                }
            }
        }

        /*
         * Move previous flows over by one month when we change months.
         * Should call this routine in operate after convergence.
         */
        public static void shiftLinkPrevflow(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                for (int j = 0; j < mi.maxLags + 1; j++)
                {
                    if (j < l.mrlInfo.linkPrevflow.Length - 1)
                    {
                        l.mrlInfo.linkPrevflow[j] = l.mrlInfo.linkPrevflow[j + 1];
                    }
                }
            }
        }

        /*
         * Lagged channel loss
         * Use closs0 and set of lag factors to generate a set of
         * previous flows for lagging.
         */
        public static void setLinkPrevflow(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.mrlInfo.closs0 > 0)
                {
                    // start with one (next time step) but shouldn't we stop at maxLags - 1?
                    for (int j = 1; j < mi.maxLags + 1; j++)
                    {
                        l.mrlInfo.linkPrevflow[j] += (long)((double)l.mrlInfo.closs0 * l.m.lagfactors[j] + .5);
                    }
                }
            }
        }

        // read GW partial flows based on Return location
        public static void ReadPartialFlowsADA(Model mi)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(mi.fname)) + @"\";
            string pattern = "*.prt";
            string[] files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                Node n = mi.FindNode(name);
                if (n != null)
                {
                    // read contents
                    StreamReader sr = new StreamReader(file);
                    string contents = sr.ReadToEnd();
                    sr.Close();

                    // get partial flows for the return node.
                    if (!string.IsNullOrEmpty(contents))
                    {
                        string[] lines = contents.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        n.m.PartialFlows = new long[lines.Length];
                        for (int i = 0; i < lines.Length; i++)
                        {
                            long value;
                            if (long.TryParse(lines[i], out value))
                            {
                                n.m.PartialFlows[i] = value;
                            }
                        }
                    }
                }
            }
        }

        // write GW partial flows based on Return location
        public static void WritePartialFlowsADA(Model mi)
        {
            int j;
            LagInfo lInfo;

            long MaxLagFound = 1;
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];

                for (int i2 = 0; i2 < mi.mInfo.demList.Length; i2++)
                {
                    Node nDem = mi.mInfo.demList[i2];

                    for (j = 0, lInfo = nDem.m.infLagi; lInfo != null; lInfo = lInfo.next, j++)
                    {
                        if (nDem.mnInfo.demPrevFlow != null && lInfo.location == n)
                        {
                            if (lInfo.numLags > MaxLagFound)
                            {
                                MaxLagFound = lInfo.numLags;
                            }
                        }
                    }

                    for (j = 0, lInfo = nDem.m.pumpLagi; lInfo != null; lInfo = lInfo.next, j++)
                    {
                        if (nDem.mnInfo.demPrevDepn != null && lInfo.location == n)
                        {
                            if (lInfo.numLags > MaxLagFound)
                            {
                                MaxLagFound = lInfo.numLags;
                            }
                        }
                    }
                }
            }

            long[] TempPrevFlow = new long[MaxLagFound + 1];
            long TPFsize = MaxLagFound;

            // find all nodes in network
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];

                for (int pos = 0; pos < MaxLagFound; pos++)
                {
                    TempPrevFlow[pos] = 0;
                }

                // sum the prevflows for that node
                for (j = 0; j < mi.mInfo.realLinkList.Length; j++)
                {
                    Link l = mi.mInfo.realLinkList[j];

                    // MAXLAG is associated with link lag factors
                    if (l.m.returnNode == n)
                    {
                        for (int pos = 0; pos < DefineConstants.MAXLAG && pos < TempPrevFlow.Length; pos++)
                        {
                            TempPrevFlow[pos] += l.mrlInfo.linkPrevflow[pos];
                        }
                    }
                }

                for (int i2 = 0; i2 < mi.mInfo.demList.Length; i2++)
                {
                    Node nDem = mi.mInfo.demList[i2];

                    // infiltration
                    for (j = 0, lInfo = nDem.m.infLagi; lInfo != null; lInfo = lInfo.next, j++)
                    {
                        if (nDem.mnInfo.demPrevFlow != null && lInfo.location == n)
                        {
                            for (int k = 0; k < lInfo.numLags - 1; k++)
                            {
                                TempPrevFlow[k] += nDem.mnInfo.demPrevFlow[j, k];
                            }
                        }
                    }

                    // pumping
                    for (j = 0, lInfo = nDem.m.pumpLagi; lInfo != null; lInfo = lInfo.next, j++)
                    {
                        if (nDem.mnInfo.demPrevDepn != null && lInfo.location == n)
                        {
                            for (int k = 0; k < lInfo.numLags - 1; k++)
                            {
                                TempPrevFlow[k] -= nDem.mnInfo.demPrevDepn[j, k];
                            }
                        }
                    }
                }

                bool foundNonZero = false;
                for (int pos = 0; pos < MaxLagFound; pos++)
                {
                    if (TempPrevFlow[pos] > 0)
                    {
                        foundNonZero = true;
                        break;
                    }
                }

                if (foundNonZero && n.name.Length == 0)
                {
                    Console.Write("node number {0:D} has no name, but does have partial flows\n", n.number);
                }

                if (foundNonZero && n.name.Length > 0)
                {
                    string dir = Path.GetDirectoryName(Path.GetFullPath(mi.fname)) + @"\";
                    FileStream fs = new FileStream(dir + n.name + ".prt", FileMode.Create);
                    if (fs.CanWrite)
                    {
                        StreamWriter sw = new StreamWriter(fs);
                        for (int pos = 0; pos < MaxLagFound; pos++)
                        {
                            sw.WriteLine(TempPrevFlow[pos]);
                        }
                        sw.Close();
                    }
                }
            }
        }

        public static void ShiftPartialFlows(Model mi)
        {
            // find all nodes in network
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];

                if (n.m.PartialFlows != null)
                {
                    for (int pos = 0; pos < n.m.PartialFlows.Length - 1; pos++)
                    {
                        n.m.PartialFlows[pos] = n.m.PartialFlows[pos + 1];
                    }
                    n.m.PartialFlows[n.m.PartialFlows.Length - 1] = 0;
                }
            }
        }

        /*****************************************************************************
        GWCalculateDemandLinks - Calculate an updated split between GW and
        consumptive demand.
        -------------------------------------------------------------------------------
        \*****************************************************************************/
        public static void GWCalculateDemandLinks(Model mi, Node n)
        {
            long demand = 0;
            if (n.mnInfo.nodedemand.Length > 0)
            {
                demand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
            }
            double nodeinfiltrationrate = 0.0;
            if (n.mnInfo.infiltrationrate.Length > 0)
            {
                nodeinfiltrationrate = n.mnInfo.infiltrationrate[mi.mInfo.CurrentModelTimeStepIndex, 0];
            }
            if (nodeinfiltrationrate > 0.5)
            {
                n.mnInfo.gwoutLink.mlInfo.hi = (long)(demand * nodeinfiltrationrate + DefineConstants.ROFF);
                n.mnInfo.demLink.mlInfo.hi = (long)(demand * (1.0 - nodeinfiltrationrate) + DefineConstants.ROFF);

                // We only retain flow for recalculating GW & other intra-iteration procedures.
                // This flow is fictitious between iters.  Spill from one link goes to the other
                if (n.mnInfo.demLink.mlInfo.flow > n.mnInfo.demLink.mlInfo.hi)
                {
                    n.mnInfo.gwoutLink.mlInfo.flow += n.mnInfo.demLink.mlInfo.flow - n.mnInfo.demLink.mlInfo.hi;
                    n.mnInfo.demLink.mlInfo.flow = n.mnInfo.demLink.mlInfo.hi;
                    if (n.mnInfo.gwoutLink.mlInfo.flow > n.mnInfo.gwoutLink.mlInfo.hi)
                    {
                        n.mnInfo.gwoutLink.mlInfo.flow = n.mnInfo.gwoutLink.mlInfo.hi;
                    }
                }

                // We only retain flow for recalculating GW & other intra-iteration procedures.
                // This flow is fictitious between iters.  Spill from one link goes to the other
                if (n.mnInfo.gwoutLink.mlInfo.flow > n.mnInfo.gwoutLink.mlInfo.hi)
                {
                    n.mnInfo.demLink.mlInfo.flow += n.mnInfo.gwoutLink.mlInfo.flow - n.mnInfo.gwoutLink.mlInfo.hi;
                    n.mnInfo.gwoutLink.mlInfo.flow = n.mnInfo.gwoutLink.mlInfo.hi;
                    if (n.mnInfo.demLink.mlInfo.flow > n.mnInfo.demLink.mlInfo.hi)
                    {
                        n.mnInfo.demLink.mlInfo.flow = n.mnInfo.demLink.mlInfo.hi;
                    }
                }
            }
            else
            {
                // We only retain flow for recalculating GW & other intra-iteration procedures.
                // Leading with the Dem link starts GW at zero.
                n.mnInfo.gwoutLink.mlInfo.hi = 0;
                n.mnInfo.gwoutLink.mlInfo.flow = 0;
            }
        }

        public static void GWStorageStepOnly(Model mi, long iodd)
        {
            for (long i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                Link l = n.mnInfo.gwrtnLink;

                if (l != null && n.m.GWStorageOnly)
                {
                    if (iodd == 0)
                    {
                        //  Set the max bounds of the link to zero - Pumping is inactive during the Natural Flow Step
                        l.mlInfo.hi = l.mlInfo.lo = 0;
                    }
                    else
                    {
                        //Storage Step
                        //   Set the max bounds of the link to the max pumping capacity
                        //   - The default cost of the link is -50000, making it junior to the ownership links
                        //    that are the order of -200000
                        l.mlInfo.hi = n.m.pcap;
                    }
                }
            }
        }

    }
}
