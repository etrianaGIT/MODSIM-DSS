using System;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersRent
    {

        // Rent Pool is called on specifed time steps to reassign storage water from
        //		contributors to subscribers; these were set in setnet by the rentlimits
        // Rent pool is called before the iteration sequence for the time step
        // Any ownership link that has positive rent limits is a potential contributor
        // A rental link is defined by having negative rent limits
        // It is not legal to have both positive and negative rent limits
        // Rental links must have an accrual link assigned
        // Each accrual link (reservoir priority) can have a rent pool (actually two; one for
        //       "normal" rent pool and one for "last to fill"
        // A contributor cannot contribute to both "normal" and "last to fill"
        //       If this is desired you must split the contract into two
        // Rent limits are annual amounts based on hydrologic state
        // Contributors can contribute up to their stglft; no borrowing based on forecast
        // Renters can never accrue storage; only ownerships can accrue
        // Renters' stglft is subject to evap and channel losses like ownerships' stglft
        // If a contributor rents subject to last to fill (contribLastThisSeason); this space gets a very junior
        //      priority until it fills (over a number of years if need be)
        // A reservoir that has a contributor to last to fill rental must have a lastfillLink
        //    this link's cost is user set (junior) and the bounds is set by code that keeps track
        //    of outstanding last fill space
        // For each accrual link the sum of contributions and subscriptions are derived
        //  If the sum of contributions is larger; the contributions are proportionately reduced
        //  If the sum of subscriptions is larger; the subscriptions are proportionately reduced
        // contributors stglft is reduced; renters srglft is increased
        // contributors get a positive contribRent or contribLastThisSeason
        // subscribers get a negative contribRent or contribLastThisSeason


        public static void RentPool(Model mi)
        {
            int i;
            diststr dptr;
            long dmax;
            Link l = null;
            Link l2 = null;
            Link ltmp = null;
            LinkList ll = null;
            diststr distList1 = null;
            diststr distList2 = null;
            diststr distList3 = null;
            diststr distList4 = null;
            long incrementalTransfer;
            /* For each priority - sum rental wants / needs & distribute */
            for (i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                l = mi.mInfo.accrualLinkList[i];
                if (l.mlInfo.rLinkL != null) // What about prev cond
                {
                    l.mrlInfo.rentaval = 0;
                    l.mrlInfo.lastaval = 0;
                    l.mrlInfo.rentdem = 0;
                    l.mrlInfo.rentlast = 0;
                    for (ll = l.mlInfo.cLinkL; ll != null; ll = ll.next)
                    {
                        l2 = ll.link;
                        /* sum available rental - for "normal" rent pool */
                        if (l2.mrlInfo.irent > 0 && l2.m.lastFill == 0)
                        {
                            int hs = (mi.runType == ModsimRunType.Conditional_Rules ? l2.mrlInfo.hydStateIndex : 0);
                            incrementalTransfer = Math.Max(0, l2.m.rentLimit[hs] - l2.mrlInfo.contribRent);
                            incrementalTransfer = Math.Min(incrementalTransfer, l2.mrlInfo.prevstglft);
                            dmax = Math.Max(0, incrementalTransfer);
                            l.mrlInfo.rentaval += dmax;
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList1, dmax, 0, dmax);
                            distList1.referencePtr = l2;
                            distList1.biasFrac = 0.0;
                            // distribution 1 is contributors to "normal" rent pool
                        }
                        /* sum available rental - for "last fill" rent pool */
                        if (l2.mrlInfo.irent > 0 && l2.m.lastFill > 0)
                        {
                            int hs = (mi.runType == ModsimRunType.Conditional_Rules ? l2.mrlInfo.hydStateIndex : 0);
                            dmax = Math.Max(0, Math.Min(Math.Abs(l2.m.rentLimit[hs]) - l2.mrlInfo.contribLastThisSeason, l2.mrlInfo.prevstglft));
                            l.mrlInfo.lastaval += dmax;
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList3, dmax, 0, dmax);
                            distList3.referencePtr = l2;
                            distList3.biasFrac = 0.0;
                            // distribution 3 is contributors to "last fill" rent pool
                        }
                    }

                    for (ll = l.mlInfo.rLinkL; ll != null; ll = ll.next)
                    {
                        l2 = ll.link;
                        /* sum desired rental - for "normal" rent pool */
                        if (l2.mrlInfo.irent < 0 && l2.m.lastFill == 0)
                        {
                            int hs = (mi.runType == ModsimRunType.Conditional_Rules ? l2.mrlInfo.hydStateIndex : 0);
                            // rentLimit and contribRent is negative for a rent link
                            dmax = Math.Max(0, Math.Abs(l2.m.rentLimit[hs]) + l2.mrlInfo.contribRent);
                            l.mrlInfo.rentdem += dmax;
                            //distList2 is "normal" rent pool subscribers
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList2, dmax, 0, dmax);
                            distList2.referencePtr = l2;
                            distList2.biasFrac = 0.0;
                        }
                        /* sum desired rental - for "last fill" rent pool */
                        if (l2.mrlInfo.irent < 0 && l2.m.lastFill > 0)
                        {
                            int hs = (mi.runType == ModsimRunType.Conditional_Rules ? l2.mrlInfo.hydStateIndex : 0);
                            // rentLimit and contribLast is negative for rent links
                            dmax = Math.Max(0, Math.Abs(l2.m.rentLimit[hs]) + l2.mrlInfo.contribLast);
                            l.mrlInfo.rentlast += dmax;
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList4, dmax, 0, dmax);
                            distList4.referencePtr = l2;
                            distList4.biasFrac = 0.0;
                            // distribution 4 is renters from "last fill" rent pool
                        }
                    }
                    /* Make choice.  2 operations:
                     *   1.  High demand, less water (or equal)
                     *      - distribute to renters the water that is available
                     *      - use up all contributor water
                     *   2.  Low demand, lotsa water
                     *      - distribute to contributors total rental amount
                     *      - satisfies all renters.
                     */
                    // if requests are > contributions we need to reduce requests proportionally
                    if (l.mrlInfo.rentdem >= l.mrlInfo.rentaval) // "normal" rent pool
                    {
                        /* distribute all of rentaval to renters */
                        if (distList2 != null)
                            GlobalMembersConstraint.DistributeProportional(l.mrlInfo.rentaval, 0, distList2, true);
                        //distList2 is "normal" pool subscribers
                        for (dptr = distList2; dptr != null; dptr = dptr.next)
                        {
                            ltmp = dptr.referencePtr;
                            ltmp.mrlInfo.prevstglft += dptr.returnValWhole;
                            ltmp.mrlInfo.stglft += dptr.returnValWhole;
                            ltmp.mrlInfo.contribRent -= dptr.returnValWhole;
                            ltmp.mrlInfo.cap_own = Math.Abs(ltmp.mrlInfo.contribRent);
                            ltmp.mrlInfo.own_accrual = Math.Abs(ltmp.mrlInfo.contribRent);
                            ltmp.mrlInfo.prevownacrul = ltmp.mrlInfo.own_accrual;
                        }
                        //distList1 is "normal" pool contributers
                        for (dptr = distList1; dptr != null; dptr = dptr.next)
                        {
                            ltmp = dptr.referencePtr;
                            ltmp.mrlInfo.prevstglft -= dptr.constraintHi;
                            ltmp.mrlInfo.stglft -= dptr.constraintHi;
                            ltmp.mrlInfo.contribRent += dptr.constraintHi;
                        }
                    }
                    else
                    {
                        /* distribute all of rentdem to contributors */
                        if (distList1 != null)
                            GlobalMembersConstraint.DistributeProportional(l.mrlInfo.rentdem, 0, distList1, true);
                        for (dptr = distList1; dptr != null; dptr = dptr.next)
                        {
                            ltmp = dptr.referencePtr;
                            ltmp.mrlInfo.prevstglft -= dptr.returnValWhole;
                            ltmp.mrlInfo.stglft -= dptr.returnValWhole;
                            ltmp.mrlInfo.contribRent += dptr.returnValWhole;
                        }
                        for (dptr = distList2; dptr != null; dptr = dptr.next)
                        {
                            ltmp = dptr.referencePtr;
                            incrementalTransfer = Math.Max(0, dptr.constraintHi);
                            ltmp.mrlInfo.stglft += incrementalTransfer;
                            ltmp.mrlInfo.prevstglft += incrementalTransfer;
                            ltmp.mrlInfo.contribRent -= incrementalTransfer;
                            ltmp.mrlInfo.cap_own = Math.Abs(ltmp.mrlInfo.contribRent);
                            ltmp.mrlInfo.own_accrual = ltmp.mrlInfo.cap_own;
                            ltmp.mrlInfo.prevownacrul = ltmp.mrlInfo.cap_own;
                        }
                    }
                    // if the request for last fill rental > supply
                    //   then we reduce the requests proportionally
                    if (l.mrlInfo.rentlast >= l.mrlInfo.lastaval)
                    {
                        /* distribute all of lastaval to last fill renters */
                        if (distList4 != null)
                            GlobalMembersConstraint.DistributeProportional(l.mrlInfo.lastaval, 0, distList4, true);
                        for (dptr = distList4; dptr != null; dptr = dptr.next)
                        {
                            ltmp = dptr.referencePtr;
                            ltmp.mrlInfo.prevstglft += dptr.returnValWhole;
                            ltmp.mrlInfo.stglft += dptr.returnValWhole;
                            ltmp.mrlInfo.contribLast -= dptr.returnValWhole;
                            ltmp.mrlInfo.cap_own = Math.Abs(ltmp.mrlInfo.contribLast);
                            ltmp.mrlInfo.own_accrual = ltmp.mrlInfo.cap_own;
                            ltmp.mrlInfo.prevownacrul = ltmp.mrlInfo.cap_own;
                        }
                        for (dptr = distList3; dptr != null; dptr = dptr.next)
                        {
                            ltmp = dptr.referencePtr;
                            ltmp.mrlInfo.prevstglft -= dptr.constraintHi;
                            ltmp.mrlInfo.stglft -= dptr.constraintHi;
                            ltmp.mrlInfo.contribLastThisSeason += dptr.constraintHi;
                        }
                    }
                    else
                    {
                        //the contribution is > requests so we reduce the contributions proportionally
                        /* distribute all of rentlast to last fill contributors */
                        // take the contributed storage from the contributor
                        if (distList3 != null)
                            GlobalMembersConstraint.DistributeProportional(l.mrlInfo.rentlast, 0, distList3, true);
                        for (dptr = distList3; dptr != null; dptr = dptr.next)
                        {
                            ltmp = dptr.referencePtr;
                            ltmp.mrlInfo.prevstglft -= dptr.returnValWhole;
                            ltmp.mrlInfo.stglft -= dptr.returnValWhole;
                            ltmp.mrlInfo.contribLastThisSeason += dptr.returnValWhole;
                        }
                        // give the contribution to the renters
                        for (dptr = distList4; dptr != null; dptr = dptr.next)
                        {
                            ltmp = dptr.referencePtr;
                            ltmp.mrlInfo.prevstglft += dptr.constraintHi;
                            ltmp.mrlInfo.stglft += dptr.constraintHi;
                            ltmp.mrlInfo.contribLast -= dptr.constraintHi;
                            ltmp.mrlInfo.cap_own = Math.Abs(ltmp.mrlInfo.contribLast);
                            ltmp.mrlInfo.own_accrual = ltmp.mrlInfo.cap_own;
                            ltmp.mrlInfo.prevownacrul = ltmp.mrlInfo.cap_own;
                        }
                    }
                    GlobalMembersConstraint.fake_free_diststr(ref distList1);
                    GlobalMembersConstraint.fake_free_diststr(ref distList2);
                    GlobalMembersConstraint.fake_free_diststr(ref distList3);
                    GlobalMembersConstraint.fake_free_diststr(ref distList4);
                }
            }
        }
        // This routine shifts contribLastThisSeason to contribLast.  
        // ContribLastThisSeason is set to zero.
        // Called by:  operate
        // Called on:  The month of accrual (monacrul)
        public static void ShiftContribLast(Model mi)
        {
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                for (LinkList ll = l.mlInfo.cLinkL; ll != null; ll = ll.next)
                {
                    Link l2 = ll.link;
                    if (l2.m.lastFill > 0)
                    {
                        if (l2.m.groupNumber == 0 || l2.mrlInfo.groupID > 0)
                        {
                            l2.mrlInfo.contribLast += l2.mrlInfo.contribLastThisSeason;
                            l2.mrlInfo.contribLastThisSeason = 0;
                        }
                    }
                }
            }
        }

        // This routine is called at each NF step in the iteration sequence
        // to give credit for Last Fill (refill) to each owner.  Only
        // owners with outstanding contribution to the last fill rent pool 
        // in the previous rental season are allowed to accrue this water.
        public static void CreditLastFillToOwners(Model mi)
        {
            diststr distList = null;

            for (int i = 0; i < mi.mInfo.lastFillLinkList.Length; i++)
            {
                Link l = mi.mInfo.lastFillLinkList[i];
                // Get all accrual links
                for (LinkList ll2 = l.to.InflowLinks; ll2 != null; ll2 = ll2.next)
                {
                    Link l2 = ll2.link;
                    if (l2.mlInfo.isAccrualLink)
                    {
                        // Owner links.  Check for lastFill.  Add to distribution.
                        for (LinkList ll3 = l2.mlInfo.cLinkL; ll3 != null; ll3 = ll3.next)
                        {
                            Link l3 = ll3.link;
                            if (l3.m.groupNumber == 0 || l3.mrlInfo.groupID > 0)
                            {
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList, Math.Abs(l3.mrlInfo.contribLast), 0, Math.Abs(l3.mrlInfo.contribLast));
                                distList.referencePtr = l3;
                            }
                        }
                    }
                }
                if (distList != null)
                {
                    GlobalMembersConstraint.DistributeProportional(l.mlInfo.flow, 0, distList, true);
                    for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                    {
                        Link l3 = dptr.referencePtr;
                        l3.mrlInfo.currentLastAccrual = dptr.returnValWhole;
                    }
                }
                GlobalMembersConstraint.fake_free_diststr(ref distList);
            }
        }
        public static void UpdateLastFillToOwners(Model mi, int newiter)
        {
            // Natural Flow step is complete.  Take credit for last fill.
            if (newiter % 2 == 1)
            {
                GlobalMembersRent.CreditLastFillToOwners(mi);
            }
        }
        // reduce contribLast with any last fill accrual
        public static void UpdateContribLast(Model mi)
        {
            for (int i = 0; i < mi.mInfo.lastFillLinkList.Length; i++)
            {
                Link l = mi.mInfo.lastFillLinkList[i];
                // Get all accrual links
                for (LinkList ll2 = l.to.InflowLinks; ll2 != null; ll2 = ll2.next)
                {
                    Link l2 = ll2.link;
                    if (l2.mlInfo.isAccrualLink)
                    {
                        // Owner links.  update the contribLast by the amount accrued through last fill
                        for (LinkList ll3 = l2.mlInfo.cLinkL; ll3 != null; ll3 = ll3.next)
                        {
                            Link l3 = ll3.link;
                            if (l3.m.groupNumber == 0 || l3.mrlInfo.groupID > 0)
                            {
                                l3.mrlInfo.contribLast -= l3.mrlInfo.currentLastAccrual;
                            }
                        }
                    }
                }
            }
        }

        // on accrual date we return any unused rent water to contributors in 
        // proportion to the contribution
        public static void PutBackUnusedRentStglft(Model mi)
        {
            diststr distList1 = null;
            diststr distList2 = null;

            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                long sumCRrenter = 0;
                if (l.mlInfo.rLinkL != null)
                {
                    for (LinkList ll = l.mlInfo.cLinkL; ll != null; ll = ll.next)
                    {
                        Link l2 = ll.link;
                        if (l2.m.lastFill == 0)
                        {
                            if (l2.mrlInfo.contribRent != 0)
                            {
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList1, Math.Abs(l2.mrlInfo.contribRent), 0, Math.Abs(l2.mrlInfo.contribRent));
                                distList1.referencePtr = l2;
                            }
                        }
                        l2.mrlInfo.contribRent = 0;
                    }
                    
                    for (LinkList ll = l.mlInfo.rLinkL; ll != null; ll = ll.next)
                    {
                        Link l2 = ll.link;
                        if (l2.m.lastFill == 0)
                        {
                            // sum of carryover storage for renters
                            sumCRrenter += Math.Abs(l2.mrlInfo.stglft);
                        }
                        l2.mrlInfo.own_accrual = 0;
                        l2.mrlInfo.stglft = 0;
                        l2.mrlInfo.prevownacrul = 0;
                        l2.mrlInfo.prevstglft = 0;
                        l2.mrlInfo.contribRent = 0;
                        l2.mrlInfo.contribLast = 0;
                    }
                    if (distList1 != null)
                    {
                        if (sumCRrenter > 0)
                            GlobalMembersConstraint.DistributeProportional(sumCRrenter, 0, distList1, true);
                        for (diststr dptr = distList1; dptr != null; dptr = dptr.next)
                        {
                            Link ltmp = dptr.referencePtr;
                            if (sumCRrenter > 0)
                                ltmp.mrlInfo.stglft += dptr.returnValWhole;
                            if (ltmp.mrlInfo.stglft > ltmp.mrlInfo.cap_own)
                                ltmp.mrlInfo.stglft = ltmp.mrlInfo.cap_own;
                            if (ltmp.mrlInfo.stglft > ltmp.mrlInfo.own_accrual)
                                ltmp.mrlInfo.own_accrual = ltmp.mrlInfo.stglft;
                            ltmp.mrlInfo.contribRent = 0;
                        }
                        GlobalMembersConstraint.fake_free_diststr(ref distList1);
                    }
                } // end of rent link if block
            } // end of accrual link loop - end of normal rent put back
            // give back unused last fill rent water to original contributors
            // - do this by reservoir
            //   distList2 - matches contributors to last fill rent pool
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.mnInfo.resLastFillLink != null)
                {
                    long sumCLrenter = 0;
                    for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                    {
                        Link l = ll.link;
                        if (l.mlInfo.isAccrualLink && l.mlInfo.rLinkL != null)
                        {
                            for (LinkList ll2 = l.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                            {
                                Link l2 = ll2.link;
                                if (l2.m.lastFill > 0)
                                {
                                    //sum of carryover for last fill renters
                                    sumCLrenter += Math.Abs(l2.mrlInfo.stglft);
                                    //Note we are zeroing out the rent links here; not the contributers
                                    l2.mrlInfo.own_accrual = 0;
                                    l2.mrlInfo.stglft = 0;
                                    l2.mrlInfo.prevownacrul = 0;
                                    l2.mrlInfo.prevstglft = 0;
                                    l2.mrlInfo.contribRent = 0;
                                    l2.mrlInfo.contribLast = 0;
                                    l2.mrlInfo.contribLastThisSeason = 0;
                                }
                            }
                            for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                            {
                                Link l2 = ll2.link;
                                if (l2.mrlInfo.contribLastThisSeason != 0)
                                {
                                    GlobalMembersConstraint.fake_alloc_diststr(ref distList2, System.Math.Abs(l2.mrlInfo.contribLastThisSeason), 0, System.Math.Abs(l2.mrlInfo.contribLastThisSeason));
                                    distList2.referencePtr = l2;
                                }
                            }
                        } // end of if block for accrual link with rent links
                    } // end of loop for inflow links to this reservoir
                    if (distList2 != null) // distList2 is last fill rent contributers
                    {
                        if (sumCLrenter > 0)
                            GlobalMembersConstraint.DistributeProportional(sumCLrenter, 0, distList2, true);
                        for (diststr dptr = distList2; dptr != null; dptr = dptr.next)
                        {
                            Link ltmp = dptr.referencePtr;
                            if (sumCLrenter > 0)
                            {
                                ltmp.mrlInfo.stglft += dptr.returnValWhole;
                                if (ltmp.mrlInfo.stglft > ltmp.mrlInfo.cap_own)
                                    ltmp.mrlInfo.stglft = ltmp.mrlInfo.cap_own;
                                if (ltmp.mrlInfo.stglft > ltmp.mrlInfo.own_accrual)
                                    ltmp.mrlInfo.own_accrual = ltmp.mrlInfo.stglft;
                                ltmp.mrlInfo.contribLastThisSeason -= dptr.returnValWhole;
                                if (ltmp.mrlInfo.contribLastThisSeason < 0)
                                    ltmp.mrlInfo.contribLastThisSeason = 0;
                            }
                            GlobalMembersConstraint.fake_free_diststr(ref distList2);
                        }
                    } // end of if block for last fill link in this reservoir
                } // end of reservoir loop for last fill rental put back
            }
        }
    }
}
