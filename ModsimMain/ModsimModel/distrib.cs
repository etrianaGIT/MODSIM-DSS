using System;
using System.Collections;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersDistrib
    {
        /* This one prepares for the storage iterations */
        public static void SetNFLinksStg(Model mi)
        {
            // Update accrual link costs, lower and upper bounds.
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                l.mrlInfo.losave = l.mlInfo.lo;
                l.mlInfo.lo = Math.Min(l.mlInfo.flow, l.mlInfo.hi);
                l.mrlInfo.csave = l.mlInfo.cost;
                if ((l.to.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES || (l.to.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && l.to.m.sysnum > 0)))
                {
                    l.mlInfo.cost = 0;
                    l.SetHI(mi.mInfo.CurrentModelTimeStepIndex, l.mrlInfo.lnkSeasStorageCap);
                }
                if (l.to.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES || (l.to.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && l.to.m.sysnum == 0))
                {
                    l.mlInfo.hi = l.mlInfo.lo;
                }
            }

            for (int i = 0; i < mi.mInfo.lastFillLinkList.Length; i++)
            {
                Link l = mi.mInfo.lastFillLinkList[i];
                l.mlInfo.hi = l.mrlInfo.contribLast;
                l.mlInfo.cost = 0;
            }

            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (!l.mlInfo.isArtificial)
                    {
                        if (l.mlInfo.isOwnerLink)
                        {
                            l.mlInfo.cost = -200000 - l.number + l.m.cost;
                        }
                        else if (l.mlInfo.cost < 0 && l.m.flagStorageStepOnly == 0 && l.m.flagSecondStgStepOnly == 0)
                        {
                            l.mlInfo.lo = Math.Min(l.mlInfo.flow, l.mlInfo.hi);
                        }
                    }
                }
            }

            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mlInfo.hi = 0;
                l.mlInfo.lo = 0;
            }

            // Reset the costs for the spill link on the child reservoir.
            for (int i = 0; i < mi.mInfo.childList.Length; i++)
            {
                Node n = mi.mInfo.childList[i];
                if (n.mnInfo.spillLink != null)
                {
                    n.mnInfo.spillLink.mlInfo.cost = DefineConstants.COST_MEDSMALL;    // 2999999;
                }
            }

            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];

                int cost = 50000;
                if (n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES || (n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum > 0))
                {
                    n.m.resOutLink.mlInfo.hi = mi.defaultMaxCap; // 99999999;
                    cost = 70000;
                }
                else if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum == 0) || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                {
                    n.m.resOutLink.mlInfo.hi = n.mnInfo.fldflow + n.mnInfo.demout;
                }

                n.mnInfo.targetLink.mlInfo.cost = -cost + 10 * n.m.priority[n.mnInfo.hydStateIndex];
            }
        }

        /* This one prepares for the natural flow iterations */
        public static void SetNFLinksNatFlow(Model mi)
        {
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                l.mlInfo.lo = l.mrlInfo.losave; // 0;
                l.mlInfo.cost = l.mrlInfo.csave;
            }
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.m.resOutLink != null)
                {
                    n.m.resOutLink.mlInfo.hi = 0;
                }
            }
            for (int i = 0; i < mi.mInfo.lastFillLinkList.Length; i++)
            {
                Link l = mi.mInfo.lastFillLinkList[i];
                l.mlInfo.hi = l.mrlInfo.contribLast;
                l.mlInfo.cost = l.m.cost;
            }

            // Match the costs for the spill link on the child reservoir to
            // the target link cost + 1.
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum == 0) || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                {
                    if (n.mnInfo.spillLink != null)
                    {
                        n.mnInfo.spillLink.mlInfo.cost = n.mnInfo.targetLink.mlInfo.cost + 1;
                    }
                }
            }
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (!l.mlInfo.isArtificial && l.m.flagStorageStepOnly == 0 && (l.mlInfo.isOwnerLink || l.mlInfo.cost < 0))
                    // what about l->m->flagSecondStgStepOnly
                    {
                        l.mlInfo.lo = 0;
                    }
                    if (l.mlInfo.isOwnerLink)
                    {
                        l.mlInfo.hi = 0;
                    }
                }
            }
        }
        public static void BuildOwnerDistList(ref diststr distList, Node n, long toLast)
        {
            for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
            {
                if (!ll.link.mlInfo.isAccrualLink)
                {
                    continue;
                }
                for (LinkList ll2 = ll.link.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                {
                    Link l = ll2.link;
                    if (l.m.groupNumber == 0 || l.mrlInfo.groupID > 0)
                    {
                        if (toLast == 0)
                        {
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList, (l.mrlInfo.cap_own - l.mrlInfo.contribLast), 0, Math.Max(0, (l.mrlInfo.cap_own - l.mrlInfo.prevstglft - l.mrlInfo.contribLast)));
                        }
                        else
                        {
                            if (l.mrlInfo.contribLast > 0)
                            {
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mrlInfo.contribLast, 0, l.mrlInfo.contribLast);
                            }
                        }
                        if (distList != null)
                        {
                            distList.referencePtr = l;
                            distList.biasFrac = 0;
                        }
                        else
                        {
                            /* Error, we have no distribution list */
                        }
                    }
                }
            }
        }
        public static void UpdateAccrualStglft(Model mi, int outputtimestepindex)
        {
            HashSet<Node> flowthrus = new HashSet<Node>();

            /* make the own_accrual variable current */
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];

                // BLounsbury: used to adjust stglft for flow thrus with ownerships
                if (l.to.mnInfo.flowThruReturnLink != null)
                {
                    flowthrus.Add(l.to);
                }

                if (l.m.accrualLink.m.numberOfGroups > 0 && l.m.groupNumber > 0)
                {
                    continue;
                }

                long flow = l.mlInfo.flow;
                long overLimit = 0;
                if (l.mrlInfo.irent >= 0) // Contributor or normal owner
                {
                    l.mrlInfo.own_accrual = l.mrlInfo.prevownacrul + l.mrlInfo.current_accrual + l.mrlInfo.currentLastAccrual - l.mrlInfo.current_evap;

                    if (l.mrlInfo.own_accrual > l.mrlInfo.cap_own)
                    {
                        overLimit = l.mrlInfo.own_accrual - l.mrlInfo.cap_own;
                        l.mrlInfo.own_accrual = l.mrlInfo.cap_own;
                    }
                    l.mrlInfo.stglft = Math.Max(0, Math.Min(l.mrlInfo.cap_own, (l.mrlInfo.current_accrual + l.mrlInfo.currentLastAccrual + l.mrlInfo.prevstglft - l.mrlInfo.current_evap - overLimit)));

                    // Override flow if dealing with group ownership
                    if (l.mrlInfo.groupID > 0)
                    {
                        flow = 0;
                        // Sum all group owner flows
                        for (LinkList ll = l.m.accrualLink.mlInfo.cLinkL; ll != null; ll = ll.next)
                        {
                            if (ll.link.m.groupNumber == l.mrlInfo.groupID)
                            {
                                flow += ll.link.mlInfo.flow;
                            }
                        }
                    }
                    if (l.m.linkChannelLoss != null)
                    {
                        double chanLoss = l.m.linkChannelLoss.m.loss_coef;
                        l.mrlInfo.stglft -= (long)(flow * (1.0 + chanLoss + chanLoss * chanLoss + chanLoss * chanLoss * chanLoss + chanLoss * chanLoss * chanLoss * chanLoss) + DefineConstants.ROFF);
                    }
                    else
                    {
                        l.mrlInfo.stglft -= flow;
                    }
                    l.mrlInfo.stglft = Math.Max(0, Math.Min(l.mrlInfo.cap_own, l.mrlInfo.stglft));
                }
                else // Rental Link
                {
                    if (l.mrlInfo.own_accrual > Math.Abs(l.mrlInfo.contribRent) + Math.Abs(l.mrlInfo.contribLast))
                    {
                        l.mrlInfo.own_accrual = Math.Abs(l.mrlInfo.contribRent) + Math.Abs(l.mrlInfo.contribLast);
                    }
                    l.mrlInfo.stglft = Math.Max(0, Math.Min(Math.Abs(l.mrlInfo.contribRent) + Math.Abs(l.mrlInfo.contribLast), (l.mrlInfo.current_accrual + l.mrlInfo.prevstglft - l.mlInfo.flow - l.mrlInfo.current_evap)));
                }
                l.mrlInfo.current_accrual = 0;
            }

            // Now do the accrual links with group ownerships.
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                for (int j = 1; j <= l.m.numberOfGroups; j++)
                {
                    // RKL setting all paper accounts for group owner links to zero
                    // when we deal with the paper accounts for a group owner link we must deal with
                    // the artificial groupID link;
                    for (LinkList ll = l.mlInfo.cLinkL; ll != null; ll = ll.next)
                    {
                        Link l3 = ll.link;
                        if (l3.m.groupNumber == j)
                        {
                            l3.mrlInfo.own_accrual = 0;
                            l3.mrlInfo.stglft = 0;
                            l3.mrlInfo.prevstglft = 0;
                            l3.mrlInfo.current_accrual = 0;
                            l3.mrlInfo.prevownacrul = 0;
                        }
                    }
                }
            }

            /* BLounsbury: Now we need to increase stglft by the amount that might
             * simply be passing through a flow thru node that is actually
             * natural flow delivery because I can't figure out how to force
             * natural flow water to not go through flow thru inflow links. */
            foreach (Node n in flowthrus)
            {
                // is there a better way to get natural flow?
                long solvernat = n.m.pdstrm.mrlInfo.natFlow[outputtimestepindex];
                long distribnat = n.m.pdstrm.mlInfo.flow;
                long nat = solvernat - distribnat;

                // if no natural flow delivery to distribute continue
                if (nat <= 0) continue;

                // count ownership links first to determine share in next loop
                int nlinks = 0;
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    if (ll.link.mlInfo.isOwnerLink)
                    {
                        //add group ownership link
                        if (ll.link.m.groupNumber > 0)
                        {
                            for (LinkList ll2 = ll.link.m.accrualLink.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                            {
                                if (ll2.link.mrlInfo.groupID == ll.link.m.groupNumber)
                                {
                                    nlinks++;
                                }
                            }
                        }
                        else
                        {
                            nlinks++;
                        }
                    }
                }


                // create distlist of links to increase stglft to
                long sumDist = 0;
                diststr distList = null;
                long share = nlinks > 0 ? nat / nlinks : 0;
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.mlInfo.isOwnerLink)
                    {
                        //add group ownership link
                        if (l.m.groupNumber > 0)
                        {
                            for (LinkList ll2 = ll.link.m.accrualLink.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                            {
                                Link l2 = ll2.link;
                                if (l2.mrlInfo.groupID == l.m.groupNumber)
                                {
                                    if (l.mlInfo.flow >= share)
                                    {
                                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, l2.mrlInfo.cap_own, 0, Math.Max(0, (l2.mrlInfo.cap_own - l2.mrlInfo.prevownacrul + l2.mrlInfo.current_evap)));
                                        sumDist += distList.constraintHi;
                                        distList.referencePtr = l2;
                                        distList.biasFrac = l2.mrlInfo.biasFrac;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (l.mlInfo.flow >= share)
                            {
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mrlInfo.cap_own, 0, Math.Max(0, (l.mrlInfo.cap_own - l.mrlInfo.prevownacrul + l.mrlInfo.current_evap)));
                                sumDist += distList.constraintHi;
                                distList.referencePtr = l;
                                distList.biasFrac = l.mrlInfo.biasFrac;
                            }
                        }
                    }
                }

                // equally distribute natural flow delivery to stglft for ownership links
                if (distList != null)
                {
                    GlobalMembersConstraint.DistributeEqualAmounts(Math.Min(nat, sumDist), 0, distList, true);
                    for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                    {
                        Link l = dptr.referencePtr;
                        l.mrlInfo.biasFrac = dptr.returnValFrac;
                        l.mrlInfo.stglft += dptr.returnValWhole;
                    }
                    GlobalMembersConstraint.fake_free_diststr(ref distList);
                }
            }
        }

        /* This routine might not be exactly right for those links without
         * owners.  Those links will end up with a zero return value in pLink.
         */
        /* Currently, this routine sums the paper water the accounts
         * holders think they have accrued.
         */
        public static void CalcSumStgAccrual(Model mi, Link pLink)
        {
            pLink.mrlInfo.sumPrevOwnAcrul = 0;
            if (pLink.mlInfo.cLinkL == null)
            {
                mi.FireOnError(string.Format("Might be a problem with CalcSumStgAccrual and link {0}", pLink.number.ToString()));
            }
            for (LinkList ll = pLink.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                Link l = ll.link;
                //Don't include owners that have groupNumber or rent links
                if (l.m.groupNumber == 0 || l.mrlInfo.groupID > 0 && l.mrlInfo.irent >= 0)
                {
                    pLink.mrlInfo.sumPrevOwnAcrul += l.mrlInfo.prevownacrul;
                }
            }
        }
        /* Calculate total of contribLast for this accrual link. */
        public static void CalcSumLastFill(Model mi, Link pLink)
        {
            pLink.mrlInfo.contribLast = 0;
            pLink.mrlInfo.contribLastThisSeason = 0;
            bool printAnnounce = false;
            if (pLink.mlInfo.cLinkL == null)
            {
                mi.FireOnError(string.Format("Might be a problem with CalcSumLastFill and link {0}", pLink.number.ToString()));
            }
            for (LinkList ll = pLink.mlInfo.cLinkL; ll != null; ll = ll.next)
            {
                Link l = ll.link;
                if (!(l.m.groupNumber == 0 || l.mrlInfo.groupID > 0))
                {
                    continue;
                }
                if (l.mrlInfo.contribLast < 0)
                {
                    if (!printAnnounce)
                    {
                        Console.WriteLine(string.Concat(pLink.name, " CalcSumLastFill"));
                        printAnnounce = true;
                    }
                    Console.WriteLine(string.Concat(l.name, " contribLast ", Convert.ToString(l.mrlInfo.contribLast)));
                    l.mrlInfo.contribLast = 0;
                }
                if (l.mrlInfo.contribLastThisSeason < 0)
                {
                    if (!printAnnounce)
                    {
                        Console.WriteLine(string.Concat(pLink.name, " CalcSumLastFill"));
                        printAnnounce = true;
                    }
                    Console.WriteLine(string.Concat(l.name, " contribLastThisSeason ", Convert.ToString(l.mrlInfo.contribLastThisSeason)));
                    l.mrlInfo.contribLastThisSeason = 0;
                }
                pLink.mrlInfo.contribLast += l.mrlInfo.contribLast;
                pLink.mrlInfo.contribLastThisSeason += l.mrlInfo.contribLastThisSeason;
            }
        }
        /* clear all the flows so we can start fresh. */
        // BAD name for this routine
        //RKL This routine is called before the time step loop and simply sets the rent pool
        // contribution values for the beginning of the study to input prevstglft
        public static void ClearDemandReleases(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mlInfo.flow = 0;
                l.mlInfo.flow0 = 0;
                l.mlInfo.hi = 0;
                if (l.mrlInfo.irent < 0 && l.mrlInfo.prevstglft > 0)
                {
                    if (l.m.lastFill > 0)
                    {
                        l.mrlInfo.contribLast = -(l.mrlInfo.prevstglft);
                    }
                    else
                    {
                        l.mrlInfo.contribRent = -(l.mrlInfo.prevstglft);
                    }
                }
            }
        }

        /* This is a rewriting of the functionality of the allocate
         * code provided by Roger Larson of the USBR.  Many thanks
         * for the expertise he provided in reservoir operations.
         */
        public static void DistribStorage(Model mi, int iodd)
        {
            /* RKL
            // we should have an mi member that indicates whether or not we have ownerships or not.
            RKL */
            if (mi.mInfo.Iteration < 2) // make sure to get to this
            {
                GlobalMembersDistrib.ResetAccumsht(mi);
            }

            if (iodd != 0) // in a storage iteration
            {
                GlobalMembersDistrib.SetNFLinksStg(mi);
                GlobalMembersDistrib.SetNonChildStgIter(mi);
                // Calculate all positive inflow water through accrual links.
                GlobalMembersDistrib.DistributePos(mi); // works for non-child.
                                                        // Calculate all negatives such as evaporation.
                GlobalMembersDistrib.DistributeNeg(mi);
            }
            else // in a natural flow iteration
            {
                GlobalMembersDistrib.SaveOwnerFlows(mi);
                GlobalMembersDistrib.CalculateAccumsht(mi); // check flows from stg iters
                GlobalMembersDistrib.SetNFLinksNatFlow(mi);
                GlobalMembersDistrib.SetNonChildNFIter(mi);
            }
        }

        /* This distributes all positive accrual based on accrual links
         *  Note: positive amounts are distributed by % ownership in the
         *        accrual link.
         */
        public static void DistributePos(Model mi)
        {
            diststr distList = null;

            /* need to loop through all accrual links for child reservoirs */
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                long sumDist = 0;

                /* need to distribute the flow to all owners - make a list and
                 * pass to distribution routine.
                 */
                for (LinkList ll = l.mlInfo.cLinkL; ll != null; ll = ll.next)
                {
                    Link l2 = ll.link;
                    if (l.m.numberOfGroups > 0 && l2.m.groupNumber > 0)
                    {
                        continue;
                    }
                    GlobalMembersConstraint.fake_alloc_diststr(ref distList, l2.mrlInfo.cap_own, 0, Math.Max(0, (l2.mrlInfo.cap_own - l2.mrlInfo.prevownacrul + l2.mrlInfo.current_evap)));
                    sumDist += distList.constraintHi;
                    distList.referencePtr = l2;
                    distList.biasFrac = l2.mrlInfo.biasFrac;
                }
                if (distList != null)
                {
                    GlobalMembersConstraint.DistributeProportional(Math.Min(l.mlInfo.flow, sumDist), 0, distList, (mi.mInfo.Iteration < 5));
                    for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                    {
                        Link l2 = dptr.referencePtr;
                        l2.mrlInfo.biasFrac = dptr.returnValFrac;
                        l2.mrlInfo.current_accrual = dptr.returnValWhole;
                    }
                    GlobalMembersConstraint.fake_free_diststr(ref distList);
                }
            }
        }
        public static void DistribNegEvap(Model mi)
        {
            long sumD = 0;
            diststr distList = null;

            if (mi.mInfo.Iteration % 2 == 0)
            {
                return;
            }

            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.mnInfo.parent && n.mnInfo.evpt < 0)
                {
                    long extra = -n.mnInfo.evpt;
                    long toLast = 0;
                    if (n.numChildren > 0)
                    {
                        for (int j = 0; j < mi.mInfo.resList.Length; j++)
                        {
                            if (mi.mInfo.resList[j].myMother == n)
                            {
                                Node nChild = mi.mInfo.resList[j];
                                GlobalMembersDistrib.BuildOwnerDistList(ref distList, nChild, toLast);
                            }
                        }
                    }
                    else
                    {
                        GlobalMembersDistrib.BuildOwnerDistList(ref distList, n, toLast);
                    }
                    if (distList != null)
                    {
                        GlobalMembersConstraint.DistributeProportional(extra, 0, distList, true);
                        sumD = 0;
                        /* distribute the storage left to all owners, keeping track
                        * of the sum of all distributed stglfts
                        */
                        for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                        {
                            sumD += dptr.returnValWhole;
                            Link l = dptr.referencePtr;
                            l.mrlInfo.current_evap += -dptr.returnValWhole;
                        }
                        GlobalMembersConstraint.fake_free_diststr(ref distList);
                    }
                    if (sumD < extra)
                    {
                        /* If we were unable to distribute to all normal owners and all
                         * normal rent pool contributors, we need to try to distribute
                         * the to Last Fill contributors */
                        toLast = 1;
                        if (n.numChildren > 0)
                        {
                            for (int j = 0; j < mi.mInfo.resList.Length; j++)
                            {
                                if (mi.mInfo.resList[j].myMother == n)
                                {
                                    Node nChild = mi.mInfo.resList[j];
                                    GlobalMembersDistrib.BuildOwnerDistList(ref distList, nChild, toLast);
                                }
                            }
                        }
                        else
                        {
                            GlobalMembersDistrib.BuildOwnerDistList(ref distList, n, toLast);
                        }
                        if (distList == null)
                        {
                            /* Error, we have excess water we could not balance */
                        }
                        else
                        {
                            GlobalMembersConstraint.DistributeProportional((extra - sumD), 0, distList, true);
                            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                            {
                                sumD += dptr.returnValWhole;
                                Link l = dptr.referencePtr;
                                l.mrlInfo.current_evap += -dptr.returnValWhole;
                            }
                        }
                        GlobalMembersConstraint.fake_free_diststr(ref distList);
                    }
                }
            }
        }
        // The first third of the SetDemandRelease routine is a distribution to all
        // demands without grouped links.  The second third is a distribution
        // to the grouped links.  The last third is a distribution to the
        // remaining links.
        // This routine is called in ONE place in operate when we have ownerships and on odd (storage iterations)
        public static void SetDemandReleases(Model mi)
        {
            diststr distList = null;
            DistConstraint dConList = null;

            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum == 0) || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                {
                    n.mnInfo.demout = 0;
                }
            }
            // Clear all distributions to zero for ownership links
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.mlInfo.isOwnerLink)
                    {
                        l.mlInfo.hi = 0;
                        l.touched = (short)0;
                    }
                }
            }

            for (int i = 0; i < mi.mInfo.ownerChanlossList.Length; i++)
            {
                Link l = mi.mInfo.ownerChanlossList[i];
                l.mrlInfo.attributeLossToStg = 0;
            }

            // For all demands:
            // Add constraints for each demand node as a summation constraint
            // Add any pipeline constraints
            // Add any group ownership constraints
            // Include channel losses

            // Pass to Summation constraint routine

            // For all demands:
            // - Find how much the demand gets.
            // - Add any bypass flow if necessary to the amount it got -- Nope - lowers dem
            //   - hi bound is lowered
            //   - if we have both bypass credit link and flow through must use MY_T("hi")
            //   - GW has no meaning for bypass credit link && flow through demands
            // - Determine any shortages by subtracting what it got from MY_T("use/hi") value
            // - For demands with shortages:
            //   - Hold onto the current distList head value
            //   - Touch all ownership links coming in
            //   - For those with stglft, put them on the distList
            //   - If you have succeeded in finding items to add to the distList
            //     you need to add a distribution constraint and a constraint list.
            //     - allocate a new DistConstraint
            //     - add each constraint member by pushing and walking down the
            //       distList until the held distList head is matching current marker.
            double totShortage = 0;
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                long demand = 0;
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    demand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                }
                // What about demands that have exchange credit nodes or watch logic??
                /* RKL
                // demGot should be just demLink flow if we add the new node between the real demand and artificial demand node
                // gwoutLink would parrallel new link from demand node to new node
                RKL */
                long demGot = (n.mnInfo.gwoutLink != null) ? n.mnInfo.demLink.mlInfo.flow + n.mnInfo.gwoutLink.mlInfo.flow : n.mnInfo.demLink.mlInfo.flow;

                if (demand > demGot) // Check for storage water.
                {
                    // Flow thrus without bypass credit use the demLink hi bound instead of use[]
                    long shortage = demand - demGot;
                    //pdstrm is bypass credit link - idstrmx is return node
                    if (mi.mInfo.demList[i].m.pdstrm == null && mi.mInfo.demList[i].m.idstrmx[0] != null && mi.mInfo.demList[i].mnInfo.flowThruReturnLink == null)
                    {
                        if (n.mnInfo.demLink.mlInfo.hi <= demGot) // Satisfied
                        {
                            continue;
                        }

                        // seems like this would be the preferred check for all cases; demLink hi + gwoutLink hi <= demGot
                        shortage = n.mnInfo.demLink.mlInfo.hi - demGot; // Add to shortage
                    }
                    // Now we have a shortage value -- use it - check for ownership
                    bool haveOwnership = false;
                    for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                    {
                        if (ll.link.mlInfo.isOwnerLink)
                        {
                            haveOwnership = true;
                        }
                    }
                    /* RKL
                    //  looks like we must deal with nodes with bypass links someplace else;  ok but we should have
                    // done some sanity checking; I don't believe we envision someone putting a bypass link on anything
                    // but a flowthru demand; we should check this and enforce it in the xyfile reader
                    RKL */
                    // pdstrm is bypass credit link - if we have ownership and a flow through with bypass credit then
                    //  we have the artificial link back to the river, so we don't worry about distributing any constraints
                    if (haveOwnership && n.m.pdstrm == null) // We must add this owner to the many distribution lists
                    {
                        diststr holdHead = distList;
                        for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                        {
                            Link l = ll.link;
                            if (l.mlInfo.isOwnerLink)
                            {
                                l.mlInfo.hi = l.mlInfo.lo = l.mlInfo.flow = 0; // OK?
                                                                               // PUSH onto distList
                                long hi;
                                if (l.m.groupNumber > 0)
                                {
                                    if (mi.mInfo.Iteration > 11) // something majic about 11 iterations?? should be user settable
                                    {
                                        hi = Math.Min(DefineConstants.NODATAVALUE, l.mrlInfo.accumshtMaxDeliv);
                                    }
                                    else
                                    {
                                        hi = DefineConstants.NODATAVALUE;
                                    }

                                }
                                else
                                {
                                    if (mi.mInfo.Iteration > 11) // something majic about 11 iterations?? should be user settable
                                    {
                                        hi = Math.Min(l.mrlInfo.accumshtMaxDeliv, Math.Max(0, (l.mrlInfo.prevstglft + l.mrlInfo.current_accrual + l.mrlInfo.currentLastAccrual - l.mrlInfo.current_evap - l.mrlInfo.accumsht)));
                                    }
                                    else
                                    {
                                        hi = Math.Max(0, l.mrlInfo.prevstglft + l.mrlInfo.current_accrual + l.mrlInfo.currentLastAccrual - l.mrlInfo.current_evap);
                                    }
                                }
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.m.cost, 0, hi);
                                distList.referencePtr = l;
                                distList.biasFrac = 0.0;
                                distList.constraintHi = Math.Max(0, Math.Min(distList.constraintHi, CheckGroupSeasonalCapacity(mi, n, l, shortage)));
                                // - For demands with shortages:
                                //   - Hold onto the current distList head value
                                //   - Touch all ownership links coming in
                                //   - For those with stglft, put them on the distList
                                //   - If you have succeeded in finding items to add to the distList
                            }
                        }
                        // Now we have added to distList, need to add constraints.
                        if (distList != holdHead) // Has ownerships & they passed criteria above
                        {
                            DistConstraint dConPtr = dConList;
                            dConList = new DistConstraint();
                            dConList.SetNext(dConPtr);
                            dConList.SetHi(shortage);
                            totShortage += shortage;
                            for (diststr dptr = distList; dptr != holdHead; dptr = dptr.next)
                            {
                                if ((dptr.referencePtr).m.linkChannelLoss != null)
                                {
                                    double chanLoss = (dptr.referencePtr).m.linkChannelLoss.m.loss_coef;
                                    chanLoss = 1 + chanLoss + chanLoss * chanLoss + chanLoss * chanLoss * chanLoss + chanLoss * chanLoss * chanLoss * chanLoss;
                                    dptr.lossFactorCredit = chanLoss;

                                    dConList.AddMemberCredit();
                                    constraintliststr holdCL = dConList.GetMemberListCredit();
                                    holdCL.distItem = dptr;
                                }
                                else
                                {
                                    dConList.AddMember();
                                    constraintliststr holdCL = dConList.GetMemberList();
                                    holdCL.distItem = dptr;
                                }
                            }
                        }
                    }
                }
            }

            // Now constraints for all group ownership links.  Go through each
            // group and walk across the distList to find a match.
            // For each distList item
            // -- Clear touched...
            // Clear currentGroupOwnAccrualLink
            // For each distList item
            // -- Check if the referencePtr->mrlInfo->groupOwn is true.
            //    -- If so, check to see if we have NOT touched this item yet &&
            //              check whether currentGroupOwnAccrualLink is NOT set
            //       -- TRUE: set currentGroupOwnAccrualLink to the accrual link
            //                referencePtr->m->accrualLink.  Initialize a new head for
            //                the summation constraints.  Add distItem to constraint
            //                member list.  Set touched to true.  Summation of group
            //                ownership link flows must be under the percieved stglft
            //                for the current month.  SetHi(accrual + stglft - evap ...)
            //       -- ELSEIF: check if item not marked && currentGroupOwnAccrualLink
            //                  matches my accrual link
            //          -- TRUE:  Add to the constraint member list.  Set touched to true.

            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                (dptr.referencePtr).touched = (short)0;
            }

            // We are in upstream flow units here, because we are talking about a reservoir pool
            Link l2 = null;
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link lAccrual = mi.mInfo.accrualLinkList[i];
                for (int j = 1; j <= lAccrual.m.numberOfGroups; j++)
                {
                    for (LinkList ll = lAccrual.mlInfo.cLinkL; ll != null; ll = ll.next)
                    {
                        if (ll.link.mrlInfo.groupID == j)
                        {
                            l2 = ll.link;
                        }
                    }

                    DistConstraint dConPtr = dConList;
                    dConList = new DistConstraint();
                    dConList.SetNext(dConPtr);
                    dConList.SetHi(Math.Max((l2.mrlInfo.prevstglft + l2.mrlInfo.currentLastAccrual + l2.mrlInfo.current_accrual - l2.mrlInfo.current_evap), 0));
                    for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                    {
                        Link l = dptr.referencePtr;
                        if (l.m.accrualLink == lAccrual && l.m.groupNumber == j)
                        {
                            dConList.AddMember();
                            constraintliststr holdCL = dConList.GetMemberList();
                            holdCL.distItem = dptr;
                        }
                    }
                }
            }

            // For each storage link, look at the storage limit link if it exists.
            // Clear touched!!!!
            // Go Through the distList that we have created:
            //  - Ignore any links we have already touched.
            //    - Check each link check to see if we have a storagelimitlink
            //    - If so & we haven't found one of these yet, set the global pointer.
            //      Set the summation constraint to match the hi bound - NF flow.
            //    - If our storage limit link matches the one that has been set:
            //      - Add this link to the distribution

            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                (dptr.referencePtr).touched = (short)0;
            }

            for (diststr dptr2 = distList; dptr2 != null; dptr2 = dptr2.next)
            {
                Link currentStorageLimitLink = null;
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    Link l = dptr.referencePtr;
                    if (l.m.linkConstraintUPS != null)
                    {
                        if (currentStorageLimitLink == null && l.touched == 0)
                        {
                            currentStorageLimitLink = l.m.linkConstraintUPS;
                            l.touched = 1;
                            DistConstraint dConPtr = dConList;
                            dConList = new DistConstraint();
                            dConList.SetNext(dConPtr);
                            dConList.SetHi(l.m.linkConstraintUPS.mlInfo.hi - l.m.linkConstraintUPS.mlInfo.flow);
                            dConList.AddMember();
                            constraintliststr holdCL = dConList.GetMemberList();
                            holdCL.distItem = dptr;
                        }
                        else if (currentStorageLimitLink == l.m.linkConstraintUPS && l.touched == 0)
                        {
                            l.touched = 1;
                            dConList.AddMember();
                            constraintliststr holdCL = dConList.GetMemberList();
                            holdCL.distItem = dptr;
                        }
                    }
                }
            }

            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                (dptr.referencePtr).touched = (short)0;
            }

            for (diststr dptr2 = distList; dptr2 != null; dptr2 = dptr2.next)
            {
                Link currentStorageLimitLink = null;
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    Link l = dptr.referencePtr;
                    if (l.m.linkConstraintDWS != null)
                    {
                        if (currentStorageLimitLink == null && l.touched == 0)
                        {
                            currentStorageLimitLink = l.m.linkConstraintDWS;
                            l.touched = 1;
                            DistConstraint dConPtr = dConList;
                            dConList = new DistConstraint();
                            dConList.SetNext(dConPtr);
                            dConList.SetHi(l.m.linkConstraintDWS.mlInfo.hi - l.m.linkConstraintDWS.mlInfo.flow);
                            dConList.AddMemberCredit();
                            constraintliststr holdCL = dConList.GetMemberListCredit();
                            holdCL.distItem = dptr;
                        }
                        else if (currentStorageLimitLink == l.m.linkConstraintDWS && l.touched == 0)
                        {
                            l.touched = 1;
                            dConList.AddMemberCredit();
                            constraintliststr holdCL = dConList.GetMemberListCredit();
                            holdCL.distItem = dptr;
                        }
                    }
                }
            }

            if (distList != null)
            {
                // Warning: symmetry - might want to do the non-childs in this routine
                // Non-childs can operate much like child reservoirs when relaxaccrual is off
                // Clear output variables for child reservoirs.
                for (int j = 0; j < mi.mInfo.resList.Length; j++)
                {
                    Node n = mi.mInfo.resList[j];
                    if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum == 0) || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                        if (n.m.resOutLink != null && n.m.sysnum == 0)
                        {
                            n.mnInfo.demout = 0;
                        }
                }

                // Do the distribution
                if (dConList != null)
                {
                    GlobalMembersConstraint.DistributeWithSummationConstraintsChloss(200000000 * mi.CalcScaleFactor(), 0, distList, dConList, true);
                }
                else
                {
                    GlobalMembersConstraint.DistributeWithSummationConstraintsChloss(200000000 * mi.CalcScaleFactor(), 0, distList, dConList, true);
                }
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    Link l = dptr.referencePtr;
                    l.mlInfo.hi = Math.Max(0, dptr.returnValWhole);
                    if (l.m.linkChannelLoss != null)
                    {
                        double chanLoss = l.m.linkChannelLoss.m.loss_coef;
                        l.mlInfo.hi = (long)((l.mlInfo.hi / (1.0 + chanLoss + chanLoss * chanLoss + chanLoss * chanLoss * chanLoss + chanLoss * chanLoss * chanLoss * chanLoss)) + DefineConstants.ROFF);
                        if (l.m.linkChannelLoss.from.mnInfo.chanLossLink != null)
                        {
                            l.m.linkChannelLoss.mrlInfo.attributeLossToStg += Math.Max(0, dptr.returnValWhole - l.mlInfo.hi);
                        }
                    }
                    if (l.m.accrualLink.to.m.resOutLink != null)
                    {
                        l.m.accrualLink.to.mnInfo.demout += Math.Max(0, dptr.returnValWhole);
                    }
                }
            }

            for (; dConList != null;)
            {
                DistConstraint dConPtr = dConList.GetNext();
                dConList.DeleteAllMembers();
                dConList = null;
                dConList = dConPtr;
            }

            if (dConList != null)
            {
                dConList.DeleteAllMembers();
                dConList = null;
            }

            GlobalMembersConstraint.fake_free_diststr(ref distList);

            for (int j = 0; j < mi.mInfo.resList.Length; j++)
            {
                Node n = mi.mInfo.resList[j];
                if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum == 0) || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                {
                    if (n.m.resOutLink != null && n.m.sysnum == 0)
                    {
                        Link l = n.m.resOutLink;
                        l.mlInfo.hi = n.mnInfo.demout + n.mnInfo.fldflow;
                    }
                }
            }
        }

        /// <summary>
        /// Constrain storage group contracts to the group seasonal capacity for
        /// zero system number reservoirs. This function distributes the available
        /// water by percent shortage, or each links shortage divided by the total
        /// group shortage. For example, if one link is short 400 and another is
        /// short 600, then they would get 40% and 60% of the available supply. If
        /// one link has 0 shortage, the other link would get 100% of the
        /// available supply.
        /// </summary>
        /// <param name="mi">Model</param>
        /// <param name="n">Node to evaluate</param>
        /// <param name="l">Link to constrain</param>
        /// <param name="shortage">Demand shortage</param>
        /// <returns>
        /// Link limit based on available group seasonal capacity. If link passed
        /// in is a rent link, does not have ownership in a zero system number
        /// reservoir, or is not a group ownership link, long.MaxValue is returned.
        /// </returns>
        private static long CheckGroupSeasonalCapacity(Model mi, Node n, Link l, long shortage)
        {
            /*
             * BLounsbury: Because I altered the code to balance sysnum=0
             * reservoirs at each timestep the storage accounts are credited to
             * match physical contents which opened up the possibility of owners
             * diverting above their seasonal capacity. This constrains these links
             * to their seasonal capacity but excludes rent links because I'm not
             * quite sure how they should be handled.
             */
            if (l.mrlInfo.irent < 0)
            {
                return long.MaxValue;
            }
            if (l.m.accrualLink.to.mnInfo.ownerType != DefineConstants.ZEROSYS_ACCOUNT_RES)
            {
                return long.MaxValue;
            }
            if (l.m.groupNumber <= 0)
            {
                // ETS - why lnktot is not checked here.  
                //       can single owners get more water than the seasonal limit?
                //       What is supposed to be controling the allocaiton to single owners.
                //       it seems like it would not include a check on channel losses either.
                return long.MaxValue;
            }

            long lnkorgrouptot = l.mrlInfo.lnktot;
            long lnkorgroupshort = shortage;
            for (LinkList ll2 = l.m.accrualLink.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
            {
                if (ll2.link.mlInfo.isOwnerLink)
                {
                    long dem = 0;
                    if (ll2.link.to.mnInfo.nodedemand.Length > 0)
                    {
                        dem = ll2.link.to.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                    }
                    long demIn = ll2.link.to.mnInfo.demLink.mlInfo.flow;
                    if (ll2.link.to.mnInfo.gwoutLink != null)
                    {
                        demIn += ll2.link.to.mnInfo.gwoutLink.mlInfo.flow;
                    }
                    long shorted = dem - demIn;
                    if (ll2.link != l && shorted > 0)
                    {
                        lnkorgroupshort += shorted;
                    }
                    if (ll2.link != l)
                    {
                        lnkorgrouptot += ll2.link.mrlInfo.lnktot;
                    }
                }
            }

            long limit = 0;
            if (lnkorgroupshort > 0)
            {
                 limit = (long)(((double)shortage / lnkorgroupshort) * (l.m.lnkallow - lnkorgrouptot));
            }

            long channelLoss = 0;
            if (l.m.linkChannelLoss != null)
            {
                channelLoss = l.m.linkChannelLoss.mrlInfo.closs;
            }

            return limit + channelLoss;
        }

        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * SetDemandReleasesFlowThrough - Set flow through storage releases
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * Routine handles the flowthrus with and without bypass credit links.
         * Because the pipeline constraint is important here, we have lotsa code
         * for it. This code is called in only ONE place when we have flow thru 
         * demands with ownership links;  it's purpose is to set things up for the 
         * "second" storage iteration
        \* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - */
        public static void SetDemandReleasesFlowThrough(Model mi)
        {
            diststr distList = null;
            DistConstraint dConList = null;

            // Clear all distributions to zero for ownership links
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    // filter for a demand node that is a flowthru demand with
                    // a ownership link AND a bypass link (pdstrm)
                    if (l.mlInfo.isOwnerLink && (l.to.m.idstrmx[0] != null || l.to.m.idstrmx[1] != null || l.to.m.idstrmx[2] != null || l.to.m.idstrmx[3] != null || l.to.m.idstrmx[4] != null || l.to.m.idstrmx[5] != null || l.to.m.idstrmx[6] != null || l.to.m.idstrmx[7] != null || l.to.m.idstrmx[8] != null || l.to.m.idstrmx[9] != null) && l.to.m.pdstrm != null)
                    {
                        l.mlInfo.hi = 0;
                        l.touched = (short)0;
                    }
                }
            }

            // For all demands:
            // - Find how much the demand gets.
            // - Add any bypass flow if necessary to the amount it got -- Nope - lowers dem
            //   - hi bound is lowered
            //   - if we have both bypass credit link and flow through must use MY_T("hi")
            //   - GW has no meaning for bypass credit link && flow through demands
            // - Determine any shortages by subtracting what it got from MY_T("use/hi") value
            // - For demands with shortages:
            //   - Hold onto the current distList head value
            //   - Touch all ownership links coming in
            //   - For those with stglft, put them on the distList
            //   - If you have succeeded in finding items to add to the distList
            //     you need to add a distribution constraint and a constraint list.
            //     - allocate a new DistConstraint
            //     - add each constraint member by pushing and walking down the
            //       distList until the held distList head is matching current marker.
            long totShortage = 0;
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];

                long demand = 0;
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    demand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                }

                long demGot = 0;
                if (n.mnInfo.flowThruReturnLink != null)
                {
                    demGot = n.mnInfo.flowThruReturnLink.mlInfo.flow;
                }
                else
                {
                    demGot = (n.mnInfo.gwoutLink != null) ? n.mnInfo.demLink.mlInfo.flow + n.mnInfo.gwoutLink.mlInfo.flow : n.mnInfo.demLink.mlInfo.flow;
                }

                // If BCL is set and we pass the test, credit BCL flow against use[]
                if (n.m.pdstrm != null)
                {
                    //demGot += n.m.pdstrm.mlInfo.flow; //BLounsbury: As per the comment above "-- Nope - lowers dem"
                }

                if (demand > demGot) // Check for storage water.
                {
                    // Flow thrus without bypass credit use the demLink hi bound instead of use[]
                    long shortage = demand - demGot;
                    if (mi.mInfo.demList[i].m.pdstrm == null && mi.mInfo.demList[i].m.idstrmx[0] != null && mi.mInfo.demList[i].mnInfo.flowThruReturnLink == null) // I don't have a return link -  my first return location is not null -  I have a bypass credit link
                    {
                        if (n.mnInfo.demLink.mlInfo.hi <= demGot)
                        {
                            continue;    // To the next demand in the system -- Correctly Satisfied
                        }
                        shortage = n.mnInfo.demLink.mlInfo.hi - demGot;
                    }
                    // Now we have a shortage value -- use it - check for ownership
                    bool haveOwnership = false;
                    double chanLoss = 0.0;
                    for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                    {
                        if (ll.link.mlInfo.isOwnerLink)
                        {
                            haveOwnership = true;
                        }
                        if (ll.link.m.linkChannelLoss != null)
                        {
                            if (chanLoss < ll.link.m.linkChannelLoss.m.loss_coef)
                            {
                                chanLoss = ll.link.m.linkChannelLoss.m.loss_coef;
                            }
                        }
                    }
                    if (haveOwnership) // We must add this owner to the many distribution lists
                    {
                        diststr holdHead = distList;
                        for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                        {
                            Link l = ll.link;
                            if (l.mlInfo.isOwnerLink && (l.to.m.idstrmx[0] != null || l.to.m.idstrmx[1] != null || l.to.m.idstrmx[2] != null || l.to.m.idstrmx[3] != null || l.to.m.idstrmx[4] != null || l.to.m.idstrmx[5] != null || l.to.m.idstrmx[6] != null || l.to.m.idstrmx[7] != null || l.to.m.idstrmx[8] != null || l.to.m.idstrmx[9] != null) && l.to.m.pdstrm != null)
                            {
                                l.mlInfo.hi = l.mlInfo.lo = l.mlInfo.flow = 0; // OK?
                                                                               // PUSH onto distList
                                long hi;
                                if (l.m.groupNumber > 0)
                                {
                                    hi = DefineConstants.NODATAVALUE;
                                }
                                else if (mi.mInfo.Iteration > 11)
                                {
                                    hi = Math.Min(l.mrlInfo.accumshtMaxDeliv, Math.Max(0, l.mrlInfo.prevstglft + l.mrlInfo.current_accrual + l.mrlInfo.currentLastAccrual - l.mrlInfo.current_evap - l.mrlInfo.accumsht));
                                }
                                else
                                {
                                    hi = Math.Max(0, l.mrlInfo.prevstglft + l.mrlInfo.current_accrual + l.mrlInfo.currentLastAccrual - l.mrlInfo.current_evap);
                                }
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.m.cost, 0, hi);
                                distList.referencePtr = l;
                                distList.biasFrac = 0.0;
                                distList.constraintHi = Math.Max(0, Math.Min(distList.constraintHi, CheckGroupSeasonalCapacity(mi, n, l, shortage)));
                                // - For demands with shortages:
                                //   - Hold onto the current distList head value
                                //   - Touch all ownership links coming in
                                //   - For those with stglft, put them on the distList
                                //   - If you have succeeded in finding items to add to the distList
                            }
                        }
                        // Now we have added to distList, need to add constraints.
                        if (distList != holdHead) // Has ownerships & they passed criteria above
                        {
                            DistConstraint dConPtr = dConList;
                            dConList = new DistConstraint();
                            dConList.SetNext(dConPtr);
                            dConList.SetHi((long)(shortage * (1.0 + chanLoss + chanLoss * chanLoss + chanLoss * chanLoss * chanLoss + chanLoss * chanLoss * chanLoss * chanLoss) + DefineConstants.ROFF));
                            totShortage += shortage;
                            for (diststr dptr = distList; dptr != holdHead; dptr = dptr.next) // Walk
                            {
                                dConList.AddMember();
                                constraintliststr holdCL = dConList.GetMemberList();
                                holdCL.distItem = dptr;
                            }
                        }
                    }
                }
            }

            // Now constraints for all group ownership links.  Go through each
            // group and walk across the distList to find a match.
            // For each distList item
            // -- Clear touched...
            // Clear currentGroupOwnAccrualLink
            // For each distList item
            // -- Check if the referencePtr->mrlInfo->groupOwn is true.
            //    -- If so, check to see if we have NOT touched this item yet &&
            //              check whether currentGroupOwnAccrualLink is NOT set
            //       -- TRUE: set currentGroupOwnAccrualLink to the accrual link
            //                referencePtr->m->accrualLink.  Initialize a new head for
            //                the summation constraints.  Add distItem to constraint
            //                member list.  Set touched to true.  Summation of group
            //                ownership link flows must be under the percieved stglft
            //                for the current month.  SetHi(accrual + stglft - evap ...)
            //       -- ELSEIF: check if item not marked && currentGroupOwnAccrualLink
            //                  matches my accrual link
            //          -- TRUE:  Add to the constraint member list.  Set touched to true.

            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                (dptr.referencePtr).touched = (short)0;
            }

            Link l2 = null;
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link lAccrual = mi.mInfo.accrualLinkList[i];
                for (int j = 1; j <= lAccrual.m.numberOfGroups; j++)
                {
                    for (LinkList ll = lAccrual.mlInfo.cLinkL; ll != null; ll = ll.next)
                    {
                        if (ll.link.mrlInfo.groupID == j)
                        {
                            l2 = ll.link;
                        }
                    }

                    DistConstraint dConPtr = dConList;
                    dConList = new DistConstraint();
                    dConList.SetNext(dConPtr);
                    dConList.SetHi(Math.Max((l2.mrlInfo.prevstglft + l2.mrlInfo.currentLastAccrual + l2.mrlInfo.current_accrual - l2.mrlInfo.current_evap), 0));
                    for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                    {
                        Link l = dptr.referencePtr;
                        if (l.m.accrualLink == lAccrual && l.m.groupNumber == j)
                        {
                            dConList.AddMember();
                            constraintliststr holdCL = dConList.GetMemberList();
                            holdCL.distItem = dptr;
                        }
                    }
                }
            }


            // For each storage link, look at the storage limit link if it exists.
            // Clear touched!!!!
            // Go Through the distList that we have created:
            //  - Ignore any links we have already touched.
            //    - Check each link check to see if we have a storagelimitlink
            //    - If so & we haven't found one of these yet, set the global pointer.
            //      Set the summation constraint to match the hi bound - NF flow.
            //    - If our storage limit link matches the one that has been set:
            //      - Add this link to the distribution

            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                (dptr.referencePtr).touched = (short)0;
            }

            for (diststr dptr2 = distList; dptr2 != null; dptr2 = dptr2.next)
            {
                Link currentStorageLimitLink = null;
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    Link l = dptr.referencePtr;
                    if (l.m.linkConstraintUPS != null)
                    {
                        if (currentStorageLimitLink == null && l.touched == 0)
                        {
                            currentStorageLimitLink = l.m.linkConstraintUPS;
                            l.touched = 1;
                            DistConstraint dConPtr = dConList;
                            dConList = new DistConstraint();
                            dConList.SetNext(dConPtr);
                            dConList.SetHi(l.m.linkConstraintUPS.mlInfo.hi - l.m.linkConstraintUPS.mlInfo.flow);
                            dConList.AddMember();
                            constraintliststr holdCL = dConList.GetMemberList();
                            holdCL.distItem = dptr;
                        }
                        else if (currentStorageLimitLink == l.m.linkConstraintUPS && l.touched == 0)
                        {
                            l.touched = 1;
                            dConList.AddMember();
                            constraintliststr holdCL = dConList.GetMemberList();
                            holdCL.distItem = dptr;
                        }
                    }
                }
            }

            if (distList != null)
            {
                // Warning: symmetry - might want to do the non-childs in this routine
                // Non-childs can operate much like child reservoirs when relaxaccrual is off
                // Clear output variables for child reservoirs.
                for (int j = 0; j < mi.mInfo.resList.Length; j++)
                {
                    Node n = mi.mInfo.resList[j];
                    if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum == 0) || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                    {
                        if (n.m.resOutLink != null && n.m.sysnum == 0)
                        {
                            n.mnInfo.demout = 0;
                        }
                    }
                }

                // Do the distribution
                if (dConList != null)
                {
                    GlobalMembersConstraint.DistributeWithSummationConstraintsChloss(200000000 * mi.CalcScaleFactor(), 0, distList, dConList, true);
                }
                else
                {
                    GlobalMembersConstraint.DistributeWithSummationConstraintsChloss(200000000 * mi.CalcScaleFactor(), 0, distList, dConList, true);
                }
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    Link l = dptr.referencePtr;
                    l.mlInfo.hi = Math.Max(0, dptr.returnValWhole);
                    if (l.m.linkChannelLoss != null)
                    {
                        double chanLoss = l.m.linkChannelLoss.m.loss_coef;
                        l.mlInfo.hi = (long)((l.mlInfo.hi / (1.0 + chanLoss + chanLoss * chanLoss + chanLoss * chanLoss * chanLoss + chanLoss * chanLoss * chanLoss * chanLoss)) + DefineConstants.ROFF);
                        if (l.m.linkChannelLoss.from.mnInfo.chanLossLink != null)
                        {
                            l.m.linkChannelLoss.mrlInfo.attributeLossToStg += Math.Max(0, dptr.returnValWhole - l.mlInfo.hi);
                        }
                    }
                    if (l.m.accrualLink.to.m.resOutLink != null)
                    {
                        l.m.accrualLink.to.mnInfo.demout += Math.Max(0, dptr.returnValWhole);
                    }
                }
            }

            for (; dConList != null;)
            {
                DistConstraint dConPtr = dConList.GetNext();
                dConList.DeleteAllMembers();
                dConList = null;
                dConList = dConPtr;
            }

            if (dConList != null)
            {
                dConList.DeleteAllMembers();
                dConList = null;
            }

            GlobalMembersConstraint.fake_free_diststr(ref distList);

            // Now we need to clean up any "over" distributions of flow to chanloss dems
            // We are almost guaranteed to have some of these.
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                long demand = 0;
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    demand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                }
                if (n.mnInfo.flowThruReturnLink == null)
                {
                    continue;
                }
                long demGot = n.mnInfo.flowThruReturnLink.mlInfo.flow;
                long sumDistributed = 0;
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    if (ll.link.mlInfo.isOwnerLink)
                    {
                        sumDistributed += ll.link.mlInfo.hi;
                        if (ll.link.m.linkChannelLoss != null)
                        {
                            sumDistributed -= (long)(ll.link.mlInfo.hi * ll.link.m.linkChannelLoss.m.loss_coef + DefineConstants.ROFF);
                        }
                    }
                }

                // We have allocated too much water to this demand.  We need to reduce
                // the water in this demand to match the exact amount of channel loss.
                // This loop has a release valve.  Just in case.  Not likely to use tho.
                Link l = null;
                for (int j = 0; sumDistributed + demGot > demand && j < 100; j++)
                {
                    long worstcost = -DefineConstants.COST_MED; // 9999999;
                                                                // Walk through all links to figure out which has the lowest
                                                                // ranking (worst cost).  Hold onto that link.  When we are positive
                                                                // this link has the worst cost, we reduce the link to zero if necessary.
                                                                // If we still haven't found enough, we need to continue with this
                                                                // demand until we have reduced the excess to zero.
                    for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                    {
                        if (ll.link.mlInfo.isOwnerLink && ll.link.mlInfo.hi > 0)
                        {
                            /* RKL
                            // should we be using relativeUseOrder instead of cost????
                            RKL */
                            if (ll.link.m.cost > worstcost && ll.link.m.accrualLink != null)
                            {
                                l = ll.link;
                                worstcost = ll.link.m.cost;
                            }
                        }
                    }

                    if (worstcost == -DefineConstants.COST_MED) //9999999)
                    {
                        continue;
                    }
                    // If we don't have a channel loss, the MY_T("undistribution") is very
                    // straightforward.  We just subtract the amount we got.  If
                    // we do have channel loss, we are actually reducing the amount
                    // distributed by some fraction of the amount we undistribute.
                    if (l.m.linkChannelLoss != null)
                    {
                        if (l.mlInfo.hi > (1 - l.m.linkChannelLoss.m.loss_coef) * (sumDistributed + demGot - demand))
                        {
                            l.mlInfo.hi -= (long)((1 - l.m.linkChannelLoss.m.loss_coef) * (sumDistributed + demGot - demand) + DefineConstants.ROFF);
                            sumDistributed -= (long)((1 - l.m.linkChannelLoss.m.loss_coef) * (sumDistributed + demGot - demand) + DefineConstants.ROFF);
                        }
                        else
                        {
                            sumDistributed -= (long)((1 - l.m.linkChannelLoss.m.loss_coef) * l.mlInfo.hi + DefineConstants.ROFF);
                            l.mlInfo.hi = 0;
                        }
                    }
                    else
                    {
                        long excess = sumDistributed + demGot - demand;
                        Node n2 = l.m.accrualLink.to;
                        if (l.mlInfo.hi < excess)
                        {
                            sumDistributed -= l.mlInfo.hi;
                            if (n2.m.resOutLink != null && n2.m.sysnum == 0)
                            {
                                n2.mnInfo.demout -= l.mlInfo.hi;
                            }
                            l.mlInfo.hi = 0;
                        }
                        else
                        {
                            l.mlInfo.hi -= excess;
                            sumDistributed -= excess;
                            if (n2.m.resOutLink != null && n2.m.sysnum == 0)
                            {
                                n2.mnInfo.demout -= excess;
                            }
                        }
                    }
                }
            }
            //WARNING This is not clear at all
            // We did an amazing amount of work to the ownership links to flow thru demands; this seems fine
            // Then we sum all ownership link hi to demout ; we want to constraint reservoir outflow
            // then now we set resOutLink->hi to demout + fldflow; this sees reasonable
            // the flowThruSTGLink hi is set the last iteration flow and so any "excess" has to
            //  come through the flowThruReleaseLink with a cost fo 10000; SO WHAT????!!!!!!!
            // I can't see a reason for the 10000 cost and bounds on the flowThruSTGLink
            //  The resOutLink is still going to constrain to demout + fldflow; sees like the
            // flowThtuReleaseLink, and flowThruSTGLink don't do much and should go away
            for (int j = 0; j < mi.mInfo.resList.Length; j++)
            {
                Node n = mi.mInfo.resList[j];
                if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum == 0) || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                {
                    Link l = n.m.resOutLink;
                    l.mlInfo.hi = n.mnInfo.demout + n.mnInfo.fldflow;
                }

                if (n.mnInfo.flowThruSTGLink != null)
                {
                    Link l = n.mnInfo.flowThruSTGLink;
                    l.mlInfo.hi = l.mlInfo.flow;
                }
            }
        }

        public static void DistributeNeg(Model mi)
        {
            diststr distList = null;
            diststr distList1 = null;
            diststr distList2 = null;

            /* need to loop through all accrual links for child reservoirs */
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.mnInfo.parent && n.mnInfo.ownerType != DefineConstants.NONCH_ACCOUNT_RES && n.mnInfo.ownerType != DefineConstants.ZEROSYS_ACCOUNT_RES)
                // make sure we don't get wierd situations
                {
                    continue;
                }
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    if (ll.link.mlInfo.isAccrualLink)
                    {
                        ll.link.mrlInfo.current_rent_evap = 0;
                        ll.link.mrlInfo.current_last_evap = 0;
                        /* need to distribute the evap to all owners - make a list and
                         * pass to distribution routine.
                         */
                        for (LinkList ll2 = ll.link.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l = ll2.link;
                            // should be accounting for channel loss?
                            long avgstglft = (long)((l.mrlInfo.prevstglft + Math.Max(0, (l.mrlInfo.prevstglft + l.mrlInfo.currentLastAccrual + l.mrlInfo.current_accrual - l.mrlInfo.current_evap - l.mlInfo.flow0))) / 2 + .5);

                            // We use average storage, just like the rest of modsim.
                            // This might be a point of contention.  An ownership that accrues
                            // and uses the water in the same month is not charged for evap.
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList, avgstglft, 0, avgstglft);
                            distList.biasFrac = l.mrlInfo.biasFrac;
                            distList.referencePtr = l;
                            l.mrlInfo.current_rent_evap = 0;
                        }
                        /* Distribute evap to the rental link animal -
                         * Remember that these links are charged with evap and the
                         * original owner is able to refill the water.
                         */
                        for (LinkList ll2 = ll.link.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            Link l = ll2.link;
                            long avgstglft = (long)((l.mrlInfo.prevstglft + Math.Max(0, (l.mrlInfo.prevstglft - l.mrlInfo.current_evap - l.mlInfo.flow0))) / 2 + .5);
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList, avgstglft, 0, avgstglft);
                            distList.biasFrac = l.mrlInfo.biasFrac;
                            distList.referencePtr = l;
                        }
                    }
                }
                if (distList != null)
                {
                    GlobalMembersConstraint.DistributeProportional(n.mnInfo.evpt, 0, distList, true);
                    for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                    {
                        Link l = dptr.referencePtr;
                        l.mrlInfo.biasFrac -= dptr.returnValFrac;
                        l.mrlInfo.current_evap = dptr.returnValWhole;
                        /* For rental links, we need to keep the amount per accrual
                         * link and distribute it as a return from rental.
                         * Note that last fill and normal rent pool users are kept
                         * separate with regard to evap.
                         */
                        if (l.mrlInfo.irent < 0)
                        {
                            if (l.m.lastFill > 0)
                            {
                                l.m.accrualLink.mrlInfo.current_last_evap += l.mrlInfo.current_evap;
                            }
                            else
                            {
                                l.m.accrualLink.mrlInfo.current_rent_evap += l.mrlInfo.current_evap;
                            }
                        }
                    }
                    GlobalMembersConstraint.fake_free_diststr(ref distList);
                }
                /*  It turns out that an accrual link w/o owners is ok.
                else
                  Fprintf(stderr,MY_T("serious error in DistributePos routine - caught \n"));
                */

                /* We have current_rent_evap and current_last_evap and can distribute
                 * these back to the contributing owners in each priority (aka by
                 * accrual link)
                 */
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    if (!ll.link.mlInfo.isArtificial && ll.link.mlInfo.isAccrualLink)
                    {
                        /* need to distribute the evap to all owners - make a list and
                         * pass to distribution routine.
                         */
                        if (ll.link.mrlInfo.current_rent_evap > 0 || ll.link.mrlInfo.current_last_evap > 0)
                        {
                            /* keep 2 distlists, one for normal rentals and the other
                             * for last fill rentals.
                             * Return evap water up to original contribution.
                             */
                            for (LinkList ll2 = ll.link.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                            {
                                Link l = ll2.link;
                                if (l.mrlInfo.contribRent > 0)
                                {
                                    GlobalMembersConstraint.fake_alloc_diststr(ref distList1, l.mrlInfo.contribRent, 0, l.mrlInfo.contribRent);
                                    distList1.biasFrac = 0.0;
                                    distList1.referencePtr = l;
                                }
                                if (l.mrlInfo.contribLast > 0)
                                {
                                    GlobalMembersConstraint.fake_alloc_diststr(ref distList2, l.mrlInfo.contribLast, 0, l.mrlInfo.contribLast);
                                    distList2.biasFrac = 0.0;
                                    distList2.referencePtr = l;
                                }
                            }
                            /* Normal rental evap */
                            if (distList1 != null && ll.link.mrlInfo.current_rent_evap > 0)
                            {
                                GlobalMembersConstraint.DistributeProportional(ll.link.mrlInfo.current_rent_evap, 0, distList1, true);
                                for (diststr dptr = distList1; dptr != null; dptr = dptr.next)
                                {
                                    Link l = dptr.referencePtr;
                                    l.mrlInfo.current_rent_evap = dptr.returnValWhole;
                                }
                            }
                            /* Last fill rental evap */
                            if (distList2 != null && ll.link.mrlInfo.current_last_evap > 0)
                            {
                                GlobalMembersConstraint.DistributeProportional(ll.link.mrlInfo.current_last_evap, 0, distList2, true);
                                for (diststr dptr = distList2; dptr != null; dptr = dptr.next)
                                {
                                    Link l = dptr.referencePtr;
                                    l.mrlInfo.current_last_evap = dptr.returnValWhole;
                                }
                            }
                        }
                    }
                }
            }
        }

        /****************************************************************************
        CalculateAccumsht - Calculate a strictly increasing list of undeliverable
                            storage water
        ------------------------------------------------------------------------------
        This routine calculates the difference between requested and delivered
        storage water.  Accumsht is the amount of water that is undeliverable
        for each iteration added to each previous iteration.

        When relaxAccrual is set, we are doing a physical storage step, and we
        don't have control of the outflow link.
        \****************************************************************************/
        public static void CalculateAccumsht(Model mi)
        {
            // Need to detect the presence of accumsht
            if (mi.mInfo.Iteration > 3 && mi.relaxAccrual == 0)
            {
                for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
                {
                    Link l = mi.mInfo.ownerList[i];
                    if (l.mlInfo.hi != l.mlInfo.flow) // We are being shorted
                    {
                        mi.mInfo.hasAccumsht = true;
                    }
                }
            }
            if (mi.mInfo.Iteration > 9 && mi.relaxAccrual == 0)
            {
                for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
                {
                    Link l = mi.mInfo.ownerList[i];
                    if (l.mlInfo.hi != l.mlInfo.flow) // We are being shorted
                    {
                        // In the effort to maintain network order, ramp down the upstream owners
                        // before ramping down the downstream owners.
                        //RKL is this an issue without child reservoirs?
                        long flowdiff = 0;
                        double chanLoss = (l.m.linkChannelLoss != null) ? l.m.linkChannelLoss.m.loss_coef : 0;
                        if (l.m.upsOwner)
                        {
                            flowdiff = Math.Max((l.mlInfo.hi - l.mlInfo.flow), 0);
                        }
                        else if (!l.m.upsOwner && mi.mInfo.Iteration > 11)
                        {
                            flowdiff = Math.Max((l.mlInfo.hi - l.mlInfo.flow), 0);
                        }
                        if (flowdiff > 0)
                        {
                            l.mrlInfo.accumsht = Math.Max(l.mrlInfo.accumsht, ((long)(Math.Max((l.mlInfo.hi - l.mlInfo.flow), 0) * (1.0 + chanLoss + chanLoss * chanLoss + chanLoss * chanLoss * chanLoss + chanLoss * chanLoss * chanLoss * chanLoss) + DefineConstants.ROFF)));
                            l.mrlInfo.accumshtMaxDeliv = Math.Min(l.mrlInfo.accumshtMaxDeliv, (long)(l.mlInfo.flow * (1.0 + chanLoss + chanLoss * chanLoss + chanLoss * chanLoss * chanLoss + chanLoss * chanLoss * chanLoss * chanLoss) + DefineConstants.ROFF));
                        }
                    }
                }
            }
        }
        public static void ResetAccumsht(Model mi)
        {
            mi.mInfo.hasAccumsht = false;

            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mrlInfo.accumsht = 0;
                l.mrlInfo.accumshtMaxDeliv = mi.defaultMaxCap; // 99999999;
            }
        }

        /* Save flows and hi bounds for all ownerships */

        public static void SaveOwnerFlows(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mlInfo.flow0 = l.mlInfo.flow;
                l.mrlInfo.hibnd = l.mlInfo.hi;
            }
        }
        public static void SetNonChildNFIter(Model mi)
        {
            // filter for non-childs and make appropriate bounds
            // for(i = 0; i < mi->mInfo->resListLen; i++)
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES)
                {
                    // Set spill bound to infinity and cost to match other priorities
                    if (n.mnInfo.spillLink != null)
                    {
                        n.mnInfo.spillLink.mlInfo.hi = mi.defaultMaxCap + 1;    // 100000000;
                    }
                    /* RKL
                    // This needs to be redone; we don't want to mess with the spillLink and targetLink costs
                    // The ONLY things that should happen here is the new nfStepStoLink should open up, close evap link
                    // and close resOutLink; we need to see if we have a "reasonable" value for fldflow and see if we
                    // pass this flow downstream for distribution
                    RKL */
                    if (mi.runType == ModsimRunType.Conditional_Rules) // using hydrologic state tables
                    {
                        if (n.mnInfo.spillLink != null)
                        {
                            n.mnInfo.spillLink.mlInfo.cost = -50000 + 10 * n.m.priority[n.mnInfo.hydStateIndex] + 1;
                        }
                        // Set target cost to match other priorities
                        n.mnInfo.targetLink.mlInfo.cost = -50000 + 10 * n.m.priority[n.mnInfo.hydStateIndex];
                    }
                    else // calibration (explicit targets)
                    {
                        if (n.mnInfo.spillLink != null)
                        {
                            n.mnInfo.spillLink.mlInfo.cost = -50000 + 10 * n.m.priority[0] + 1;
                        }
                        n.mnInfo.targetLink.mlInfo.cost = -50000 + 10 * n.m.priority[0];
                    }


                    // Set excess storage link cost to 1
                    n.mnInfo.excessStoLink.mlInfo.cost = 1;

                    if (n.m.resOutLink != null)
                    {
                        n.m.resOutLink.mlInfo.hi = n.mnInfo.fldflow;
                    }

                    // Set outflow bound based on relaxacrul in SetTarget
                    // Set accrual bound based on relaxacrul in SetTarget
                    /* RKL
                    // we should do this here or in another function; these reservoir control functions belong somewhere
                    // else; why are they here in distrib??
                    RKL */
                }
            }
        }
        public static void SetNonChildStgIter(Model mi)
        {
            // filter for non-childs and make appropriate bounds
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                /* RKL No need to mess with these costs; close the new nfStepStoLink; open the resOutLink and evap links. RKL */
                if (n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES)
                {
                    // Set target cost to zero
                    if (mi.runType == ModsimRunType.Conditional_Rules)
                    {
                        n.mnInfo.targetLink.mlInfo.cost = -70000 + 10 * n.m.priority[n.mnInfo.hydStateIndex];
                    }
                    else
                    {
                        n.mnInfo.targetLink.mlInfo.cost = -70000 + 10 * n.m.priority[0];
                    }

                    // Set excess storage link cost to 1
                    /* RKL ExcessStoLink cost should be just less than sinks; set this in setnet and leave it alone. RKL */
                    n.mnInfo.excessStoLink.mlInfo.cost = 1;

                    // Set outflow bound based on relaxAccrual in SetTarget
                    // Set accrual bound based on relaxAccrual in SetTarget
                    // Set accrual lower bound, assume always =0 when relaxAccrual = 0
                    if (n.m.resOutLink != null)
                    {
                        n.m.resOutLink.mlInfo.hi = mi.defaultMaxCap;    // 10000000;
                    }
                    if (mi.relaxAccrual == 0 && n.m.sysnum == 0)
                    {
                        for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                        {
                            Link l = ll.link;
                            if (l.mlInfo.isAccrualLink)
                            {
                                l.mlInfo.lo = 0;
                                /* RKL Why would we limit accrual links in the storage step?? Storage is put at a disadvantage with unmet NF links... RKL */
                                l.mlInfo.hi = l.mlInfo.flow;
                            }
                        }
                    }
                }
            }
        }

    }
}
