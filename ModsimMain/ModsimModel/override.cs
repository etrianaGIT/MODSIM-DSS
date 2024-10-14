using System;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersOverride
    {

        // We are truncating the child targets based on the parent target.
        // There is a problem here.  We really need to do something like this;
        //  1.  Look at the outflow out of all child reservoirs.
        //  2.  Look at the inflow of all child reservoirs.
        //  3.  Calculate the final ending amount for all the child reservoirs.
        //  4.  Calculate the difference between the actual ending amount and
        //      the parent target.  We have already satisfied all child targets.
        //  5.  Adjust the operation to fix the problems with the distribution.
        //  6.  Make sure the child targets are set to the right amounts.
        /* SetNonChildRel0 is responsible for:
         * NF iters        Spill           infinity         0
         *                 Target          Normal           0
         *                 Excess          Normal           1
         *  Relaxacrul=off
         *                 Accrual Links   zero if evacuation needed
         *                                     or
         *                                  minimum of
         *                                ((Water Right     -cost (user defined)
         *                                 - accrual)
         *                                     and
         *                                 space available)
         *
         *                 Main Outflow    flood release    usercost
         *                                  (routing)
         *
         *                 Main Outflow    0                usercost
         *                                  no routing
         *
         * ST iters        Spill           Normal           Big Penalty (Normal)
         *                 Target          Normal           -11000 + 10*prio
         *                 Excess          Normal           1
         *                 Accrual Links   NF flow          -cost (user defined)
         *                                 LO = HI
         *                 Main Outflow    infinity         usercost
         */
        public static void SetNonChildRel0(Model mi)
        {
            Link l;
            Link l3;
            Node n = null;
            LinkList ll;
            LinkList ll3;
            diststr distList = null;
            diststr dptr;
            long hiVariable;
            long constraintHi = 0;
            long constraintLo = 0;
            long plusAmount = 0;
            long minusAmount = 0;
            long sumEvap = 0;
            long outFlow = 0;
            long negEvap = 0;
            // loop through all non-child ownership reservoirs
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                n = mi.mInfo.resList[i];
                if (n.mnInfo.targetcontent.Length > 0) // can't do this operation if target is not set for this reservoir
                {
                    long target = Math.Min(n.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex], n.m.max_volume);
                    sumEvap = 0;
                    negEvap = 0;
                    if (n.mnInfo.evpt < 0)
                    {
                        negEvap = Math.Abs(n.mnInfo.evpt);
                    }
                    // if starting contents plus negative evap (inflow) is less than the target
                    //   then we need to distribute the allowed accrual to the accrual links by priority
                    // this is IF we wish to restrict accrual by space available (what about outflow?)
                    // if relaxaccrual is on, we should allow the accrual links (including last fill) to open up to lnkallow - lnktot
                    if ((n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES) && n.mnInfo.start + negEvap < target)
                    // we should be allowing some fldflow to go downstream !!! RKL
                    // should we have outflow and evap subtracted from start??
                    {
                        // Clear summation variables
                        if (n.mnInfo.resLastFillLink != null)
                        {
                            n.mnInfo.resLastFillLink.mrlInfo.current_last_evap = 0;
                            n.mnInfo.resLastFillLink.mrlInfo.contribLast = 0;
                            n.mnInfo.resLastFillLink.mrlInfo.contribLastThisSeason = 0;
                        }

                        // should sum up all outflows for this res.  I won't do that
                        // right now...  Should be easy though.
                        if (n.m.resOutLink != null)
                        {
                            outFlow = 0;
                        }
                        for (ll = n.InflowLinks; ll != null; ll = ll.next)
                        {
                            l = ll.link;
                            if (l.mlInfo.isAccrualLink)
                            {
                                l.mrlInfo.current_evap = 0;
                                l.mrlInfo.current_rent_evap = 0;
                                l.mrlInfo.contribLast = 0;
                                l.mrlInfo.contribLastThisSeason = 0;
                                // go through all ownership links - calculating totals
                                for (ll3 = l.mlInfo.cLinkL; ll3 != null; ll3 = ll3.next)
                                {
                                    l3 = ll3.link; // l3 is an ownership link under accrual link l
                                    if (l3.m.groupNumber > 0)
                                    {
                                        continue;
                                    }
                                    l.mrlInfo.current_evap += l3.mrlInfo.current_evap;
                                    l.mrlInfo.current_rent_evap += l3.mrlInfo.current_rent_evap;
                                    sumEvap += l3.mrlInfo.current_evap;
                                    sumEvap += l3.mrlInfo.current_rent_evap; // RKL
                                    l.mrlInfo.contribLast += l3.mrlInfo.contribLast;
                                    l.mrlInfo.contribLastThisSeason += l3.mrlInfo.contribLastThisSeason;
                                    if (n.mnInfo.resLastFillLink != null)
                                    {
                                        n.mnInfo.resLastFillLink.mrlInfo.contribLast += l3.mrlInfo.contribLast;
                                        n.mnInfo.resLastFillLink.mrlInfo.contribLastThisSeason += l3.mrlInfo.contribLastThisSeason;
                                        n.mnInfo.resLastFillLink.mrlInfo.current_last_evap += l3.mrlInfo.current_last_evap;
                                    }
                                }
                                //WARNING This routine sums prevownacrual for each cLinkL so it makes sense to only
                                // do this on iter = 0, prevownacrual does not change in the iteration sequence
                                GlobalMembersDistrib.CalcSumStgAccrual(mi, l); // might move elsewhere
                                GlobalMembersDistrib.CalcSumLastFill(mi, l); // get a fresh version of contribLast

                                /* RKL
                                // so what we are really trying to do here is see which accrual links can open up and to what
                                // extent based on how much space we have to fill (Target + evap - start)
                                // This should not even be called if RelaxAcrul is on
                                RKL */
                                if (l.mlInfo.hiVariable != null && l.mlInfo.hiVariable.GetLength(0) > 0)
                                {
                                    hiVariable = l.mlInfo.hiVariable[mi.mInfo.CurrentModelTimeStepIndex, 0];
                                }
                                else
                                {
                                    hiVariable = l.m.maxConstant;
                                }
                                constraintHi = Math.Min(hiVariable, (l.mrlInfo.lnkSeasStorageCap - l.mrlInfo.sumPrevOwnAcrul - l.mrlInfo.contribLast + l.mrlInfo.current_rent_evap + l.mrlInfo.current_evap));
                                if (constraintHi < 0)
                                {
                                    Console.WriteLine(string.Concat("Override SetNonChildRel0 150 constraintHi ", Convert.ToString(constraintHi)));
                                    Console.WriteLine(string.Concat(" Link", Convert.ToString(l.number)));
                                    Console.WriteLine(string.Concat(" Link lnkSeasStorageCap", Convert.ToString(l.mrlInfo.lnkSeasStorageCap)));
                                    Console.WriteLine(string.Concat(" Link sumPrevOwnAcrul", Convert.ToString(l.mrlInfo.sumPrevOwnAcrul)));
                                    Console.WriteLine(string.Concat(" Link contribLast", Convert.ToString(l.mrlInfo.contribLast)));
                                    Console.WriteLine(string.Concat(" Link current_rent_evap", Convert.ToString(l.mrlInfo.current_rent_evap)));
                                    Console.WriteLine(string.Concat(" Link current_evap", Convert.ToString(l.mrlInfo.current_evap)));
                                    Console.WriteLine("constraintHi is set to zero");
                                    constraintHi = 0;
                                }
                                constraintLo = 0;
                                GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mlInfo.cost, constraintLo, constraintHi);
                                distList.referencePtr = l;
                                distList.biasFrac = 0.0;
                            }
                        }
                        if (n.mnInfo.resLastFillLink != null)
                        {
                            l = n.mnInfo.resLastFillLink;
                            sumEvap += l.mrlInfo.current_last_evap;
                            constraintHi = l.mrlInfo.contribLast + l.mrlInfo.current_last_evap;
                            constraintLo = 0;
                            if (constraintHi < 0)
                            {
                                Console.WriteLine(string.Concat("Override SetNonChildRel0 181 constraintHi ", Convert.ToString(constraintHi)));
                                Console.WriteLine(string.Concat(" Link", Convert.ToString(l.number)));
                                Console.WriteLine(string.Concat(" Link contribLast", Convert.ToString(l.mrlInfo.contribLast)));
                                Console.WriteLine(string.Concat(" Link current_last_evap", Convert.ToString(l.mrlInfo.current_last_evap)));
                                Console.WriteLine("constraintHi is set to zero");
                                constraintHi = 0;
                            }
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mlInfo.cost, constraintLo, constraintHi);
                            distList.referencePtr = l;
                            distList.biasFrac = 0.0;
                        }
                        // note that this does not take into account the inflow into
                        // this reservoir from non-approved sources. Only accrual
                        // should work at this time.
                        // Note that we don't calculate evap as well.  This means that
                        // we need to do something about that as well.
                        plusAmount = target + sumEvap + outFlow;
                        minusAmount = n.mnInfo.start;
                        GlobalMembersConstraint.DistributeByPriority(plusAmount, minusAmount, distList, true);
                        // have distribution, make it work.
                        for (dptr = distList; dptr != null; dptr = dptr.next)
                        {
                            l = dptr.referencePtr;
                            if (dptr.returnValWhole < 0)
                            {
                                Console.WriteLine(string.Concat("SetNonChildRel0 214 link ", Convert.ToString(l.number)));
                                Console.WriteLine(string.Concat(" dptr->returnValWhole ", Convert.ToString(dptr.returnValWhole)));
                                Console.WriteLine("link hi is set to zero");
                            }
                            l.mlInfo.hi = Math.Max(0, dptr.returnValWhole);
                            l.mlInfo.lo = 0;
                        }
                        GlobalMembersConstraint.fake_free_diststr(ref distList);

                        // set outflow link to zero bound at initialization for each mon
                        // should this be here or just in operate?
                        if (n.m.resOutLink != null)
                        {
                            n.m.resOutLink.mlInfo.hi = 0;
                        }
                    }
                    else if ((n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES) & n.mnInfo.start + negEvap >= target)
                    /* RKL
                    // what we are saying here is that we cannot store anything this time step; but this ignores the
                    // possibility we might release water to meet demands; so this should be cleaned up
                    RKL */
                    {
                        // start - outflow - evap ??? RKL
                        // we should be allowing some fldflow to go downstream !!! RKL
                        for (ll = n.InflowLinks; ll != null; ll = ll.next)
                        {
                            if (ll.link.mlInfo.isAccrualLink)
                            {
                                ll.link.mlInfo.hi = 0;
                                ll.link.mlInfo.lo = 0;
                            }
                        }
                        if (n.mnInfo.resLastFillLink != null)
                        {
                            n.mnInfo.resLastFillLink.mlInfo.hi = 0;
                            n.mnInfo.resLastFillLink.mlInfo.lo = 0;
                        }
                    }
                }
            }
        }
    }
}
