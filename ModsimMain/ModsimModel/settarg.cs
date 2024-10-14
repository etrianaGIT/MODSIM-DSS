namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersSettarg
    {

        /*****************************************************************************
        SetTarget: Calculates the targets for "child" reservoirs of each "parent"
        -------------------------------------------------------------------------------
        - Inputs: (* changing, + static)
          * Evaporation calculation from the previous storage step.
          * Current demand requests
          + Parent Targets
          + Child Targets

        Set Target is called during the space between the NF & STG steps as
        well as for the initial iteration's calculation of target.  

        Parent accrual side:
          - summation constraints:
            - Child reservoirs with targets
              => target - start + evap + childDemRequestOutflow - FOutflow + ForcedIn
            - Parent
              => target - sumchildtargets - 
                 start                     (only for nontargeted children) + 
                 evap                      (only for nontargeted children) + 
                 sumChildDemRequestOutflow (only for nontargeted children) - 
                 FOutflow + ForcedIn
            - Child reservoir
              => size - start + evap + DemRequestOutflow - FOutflow + ForcedIn
          - per accrual link constraint:
            - Accrual capability - If relaxaccrual off.  (sysnum has no effect)
            - Accrual seascap    - If relaxaccrual on.   (sysnum has no effect)

        Sysnum makes child reservoirs act like non-child reservoirs.
          - open up all links in and out to let the target link work - stg step.
          - In the storage step, we are interested in a different targeting
            operation that will actually ignore the lnktot.  This
            means that we have a different operation, that applies the accrual
            capability based on whether we are in the STG step or we
            are in the NF step.
        Relaxaccrual opens up the accrual side during the NF step.
          - ignore physical target of reservoir during the NF step.
          - we don't care about the target operation here because of spill links.
        Sysnum OFF
          - Accrual constrained between NF & STG step.
          - Limitation to physical accrual capability.

        Forced inflow / outflow
          - Want to effect the target based on what we see happening to the res.
          - If water "stolen" from one child res and placed into another, change
            the targeting operation appropriately to match.
          - Forced water is always the first thing settarg takes into account.

        Trickiness - NO
          - We might like to treat the targets on the child reservoirs as 
            a not so sure target.  If the child needs to accrue, but the
            accrual capability is not possible, it is time to let another
            child have a crack at it.
            - Case - pure child reservoir - below target, accrual is limited by lnktot.
              - Deal that space back to the parent
              - Check operation to verify that we are indeed within parent target.
            - Case - pure child reservoir - above target.  No effect on parent oper.
            - Case - forced water from pure child - accrual capability gone
              - Same as case 1 above

        -=-=-=-=-=-=-=- -=-=-=-=-=-=-=- -=-=-=-=-=-=-=- -=-=-=-=-=-=-=- -=-=-=-=-=-=-=-
        Debugging (with gcc):
        bre in DoTargetingOperation

        define stprint
        printf "sumChildEvap:            %d\n", sumChildEvap
        printf "sumChildTargets:         %d\n", sumChildTargets
        printf "sumChildStart:           %d\n", sumChildStart
        printf "sumChildFInflow:         %d\n", sumChildFInflow
        printf "sumChildFOutflow:        %d\n", sumChildFOutflow
        printf "sumChildDemandRelease:   %d\n", sumChildDemandRelease
        end 

        printf "sumEstimateChildRelease: %d\n", sumEstimateChildRelease
        -=-=-=-=-=-=-=- -=-=-=-=-=-=-=- -=-=-=-=-=-=-=- -=-=-=-=-=-=-=- -=-=-=-=-=-=-=-


        \*****************************************************************************/

        /*****************************************************************************
        -------------------------------------------------------------------------------
        Testing modes:
          - Accrual test
            - 2 res's, single accrual links, match reservoir size normal accrual.
          - Release test
          - Perfect test
          - Child reservoir targets with:
            - accrual
            - release
            - perfect

        All child reservoirs must have accrual capability?  No.

        Parts of the scheme:
          - no accrual capability                 A
          - multiple accrual                      B
          - child target                          C
          - Second Fill                           D
          - Links with Seascap - no ownership     E
          - Forced inflow / outflow               F
          - System number                         G
          - Relaxaccrual on & System number       H
          - Evap exclusion                        I
          - Evap                                  J
          - Negative evap                         K

        Roger operation:

        2 child reservoirs, multiple accrual, one with target, one without. 
        System number on one without target.  Relaxaccrual on.

        BCJ, BHJ

        Todd operation:

        2 child reservoirs, both with target, parent target enforced on children

        BCJ, BCJ

        Paul operation:

        1 child reservoir, target on parent multiple fills.

        BDJ

        Old Gunnison operation:

        2 child reservoirs.  Target on parent controls the operation of the
        children.  Pure child reservoirs.  Steal water from one and credit
        it to the other at the end of the year.  Extra accrual capability 
        with no explicit owner (balance handles it).

        BFJ, BEFJ

        \***************************************************************************/

        /* Notes:
           - Nobody is setting l->mrlInfo->demandOutflow.
           - Who is setting the evap values for the links?
           - The cookie algorithm is still broken.
           - 
           - 
           - 
         */

        //void SetTarget(Model *mi, long mon)
        public static void SetTarget(Model mi)
        {
            long i;
            long j;
            Node n;
            Node nChild;
            long sumChildEvap;
            long sumChildTargets;
            long sumChildStart;
            long sumChildFInflow;
            long sumChildFOutflow;
            long sumChildDemandRelease;

            /* loop through all parent reservoirs setting targets */
            for (i = 0; i < mi.mInfo.parentList.Length; i++)
            {
                n = mi.mInfo.parentList[i];

                sumChildStart = sumChildTargets = sumChildFOutflow = sumChildFInflow = sumChildEvap = sumChildDemandRelease = 0;

                // Gather up sumChildStart, sumChildTargets, sumChildFOutflow, sumChildFInflow
                // Also calculate total accrual capability
                GlobalMembersSettarg.CalcChildrenReservoirStateWithAccrual(mi, n, ref sumChildStart, ref sumChildTargets, ref sumChildFOutflow, ref sumChildFInflow, ref sumChildDemandRelease, ref sumChildEvap);

                // Do all child reservoirs with targets
                for (j = 0; j < mi.mInfo.childList.Length; j++)
                {
                    nChild = mi.mInfo.childList[j];
                    if (nChild.myMother == n && nChild.mnInfo.targetExists)
                    {
                        GlobalMembersSettarg.CalcChildrenReservoirStateWithAccrual(mi, nChild, ref sumChildStart, ref sumChildTargets, ref sumChildFOutflow, ref sumChildFInflow, ref sumChildDemandRelease, ref sumChildEvap);
                        GlobalMembersSettarg.DoTargetingOperation(mi, nChild, ref sumChildStart, ref sumChildTargets, ref sumChildFOutflow, ref sumChildFInflow, ref sumChildDemandRelease, ref sumChildEvap);
                    }
                }
                // Do parent reservoir
                GlobalMembersSettarg.CalcChildrenReservoirStateWithAccrual(mi, n, ref sumChildStart, ref sumChildTargets, ref sumChildFOutflow, ref sumChildFInflow, ref sumChildDemandRelease, ref sumChildEvap);
                GlobalMembersSettarg.DoTargetingOperation(mi, n, ref sumChildStart, ref sumChildTargets, ref sumChildFOutflow, ref sumChildFInflow, ref sumChildDemandRelease, ref sumChildEvap);
            }
        }

        // Again we have this problem with system number
        //   RAS operation -  (don't need settarget for this reservoir)
        //    Accrual links NF (all OPEN!) [ALLOW - ACCRUED] 
        //                 STG (BIGTIME OPEN) [ALLOW]

        //   S   operation -  (SetTarget is needed)
        //    Accrual links NF (Physical Accrual) [ALLOW - ACCRUED] limited by TARGET 
        //                 STG (BIGTIME OPEN) [ALLOW]

        // RA operation - Exactly like old child reservoir

        // For child reservoirs under RAS, take the summation of the
        // other targets and subtract that from the parent to get their target.  This
        // is the only valid operation for the target.  We might also be able to
        // slave from non-targeted children w/o SYSNUM.  Let's explore that one
        // when it actually happens as a valid mode.


        /*****************************************************************************
        CalcChildrenReservoirStateWithAccrual() - Calc for all Child reservoirs of n.
        -------------------------------------------------------------------------------
        Gather up values across all child reservoirs for sumChildStart, 
        sumChildTargets, sumChildFOutflow, and sumChildFInflow.

        This routine has 2 modes:
          Mode 1: Pass a parent reservoir, all values applicable to parent's target
                  operation are returned
          Mode 2: Pass a child with a target, all values applicable to a child
                  targeting itself are returned.
        \*****************************************************************************/

        public static void CalcChildrenReservoirStateWithAccrual(Model mi, Node n, ref long sumChildStart, ref long sumChildTargets, ref long sumChildFOutflow, ref long sumChildFInflow, ref long sumChildDemandRelease, ref long sumChildEvap)
        {
            long i;
            LinkList ll;
            LinkList ll3;
            Link l;
            Link l3;
            Node nChild;
            long cStart;
            long cFOutflow;
            long cFInflow;
            long cEvap;
            long cDemandRelease;

            sumChildStart = sumChildTargets = sumChildFOutflow = sumChildFInflow = sumChildEvap = sumChildDemandRelease = 0;
            for (i = 0; i < mi.mInfo.childList.Length; i++)
            {
                cStart = cFOutflow = cFInflow = cEvap = cDemandRelease = 0;
                nChild = mi.mInfo.childList[i];
                if (nChild.myMother == n || nChild == n) // Mode 2 valid -  Mode 1 valid
                {
                    if (nChild.mnInfo.targetExists && nChild != n) // In Mode2, calculate targeting parms -  In Mode1, calc SumChildTargets
                    {
                        sumChildTargets += nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, nChild.mnInfo.hydStateIndex];
                    }
                    else // AccCap for non-targeted children(M1) or a self targeting child(M2)
                    {
                        cStart = nChild.mnInfo.start;
                        for (ll = nChild.InflowLinks; ll != null; ll = ll.next)
                        {
                            l = ll.link;
                            if (l.mlInfo.isAccrualLink)
                            {
                                GlobalMembersDistrib.CalcSumStgAccrual(mi, l); // Lnktot calculation
                                GlobalMembersDistrib.CalcSumLastFill(mi, l); // contribLast & contribLastThisSeason

                                l.mrlInfo.current_evap = 0;
                                for (ll3 = l.mlInfo.cLinkL; ll3 != null; ll3 = ll3.next)
                                {
                                    l3 = ll3.link;
                                    l.mrlInfo.current_evap += l3.mrlInfo.current_evap;
                                }
                            }
                        }

                        // Get forced inflow.
                        cFInflow = GlobalMembersSettarg.CalcChildForcedInflow(nChild);

                        // Gather forced outflow.
                        cFOutflow = GlobalMembersSettarg.CalcChildForcedOutflow(nChild);

                        // Calculate total demand release from this child.
                        cDemandRelease = nChild.mnInfo.demout;

                        // Calculate total evap for this child reservoir
                        cEvap = nChild.mnInfo.evpt;

                        // Prepare a target for accrual side operation (EST of STEND)
                        if (nChild != n)
                        {
                            if (nChild.mnInfo.targetcontent.Length == 0)
                            {
                                int numhs = 1;
                                if (mi.HydStateTables.Length > 0 && n.m.hydTable > 0)
                                    numhs = mi.HydStateTables[n.m.hydTable - 1].NumHydBounds + 1;
                                nChild.mnInfo.targetcontent = new long[mi.TimeStepManager.noModelTimeSteps, numhs];
                            }
                            nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] = (cStart + cFInflow - cFOutflow - cDemandRelease - cEvap);
                        }
                    }
                    // Gather parent sums based on child values (target handled above)
                    sumChildStart += cStart;
                    sumChildFInflow += cFInflow;
                    sumChildFOutflow += cFOutflow;
                    sumChildDemandRelease += cDemandRelease;
                    sumChildEvap += cEvap;
                }
            }
        }

        /*****************************************************************************
        DoTargetingOperation() - operate parent or child with target to match target
        -------------------------------------------------------------------------------
        Mode1:  Passed a parent reservoir
          - Set children targets that aren't set
        Mode2:  Passed a child reservoir with a target
          - Don't set the target for the child reservoir (already set)

        State 1: Below Target (BT)
          - get all accrual links applicable to mode => DistList
          - Make Summation Constraint out of all applicable children reservoirs
            - M1 - Each child supplies a set of accrual links & a summation
            - M2 - The only child (itself) is the summation
          - Call dist w/ Sum Constraints <= difference between Est Stg & Target
          - Pass out the dist to the accrual links

        State 2: Above Target (AT)
          - Close all accrual links
          - Get a list of all reservoirs
          - Distribute space requirement between the reservoirs without targets
          - Distribute based on maximum capacity of each child reservoir

        State 3: Perfect
          - Close off inflow
          - Set outflow to DR

        If sysnum is on, the target on the child reservoirs is only based
        on the physical target of the reservoir.  

        Relaxaccrual (RA) operation 
        Relaxaccrual off, sysnum reservoirs - Target is indeterminate in the
          storage step if we have more than one reservoir.  Assume we do not.
        Relaxaccrual on,  sysnum reservoirs - Target is much more straightforward.
          Accrual capability is not as big a deal as the priority number on 
          the reservoir.  Use the seasonal capacity - lnktot for the accrual
          capability in the NF step.  Open the links to full capacity for
          the storage step.
        Sysnum off - Strict accrual, Strict release

        STG step - sysnum reservoir - 
          - Open Accrual to seascap
          - Open Outflow to huge
        NF  step - sysnum res - RA on
          - Open Accrual to seascap - lnktot
        NF  step - sysnum res - RA off
          - Open Accrual to seascap - lnktot or to match parent physical target
          - Target is based on Accrual to seascap
        STG step sysnum off
          - Accrual matches stg step accrual
          - Outflow matches DR or floodflow
        \*****************************************************************************/

        public static void DoTargetingOperation(Model mi, Node n, ref long sumChildStart, ref long sumChildTargets, ref long sumChildFOutflow, ref long sumChildFInflow, ref long sumChildDemandRelease, ref long sumChildEvap)
        {
            long i;
            long j;
            long leftToAccrue;
            Node nChild;
            Link l = null;
            //LinklastFillLink = NULL;
            LinkList ll = null;
            DistConstraint dConList = null;
            DistConstraint dConPtr = null;
            long pTarget;
            long sumChildEstimatedContents;
            long TotalLastFillRefill;
            diststr distList = null;
            diststr dptr;
            constraintliststr holdCL;
            long targetnumber;
            pTarget = sumChildTargets;
            if (n.mnInfo.targetcontent.Length > 0)
                pTarget = n.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
            sumChildEstimatedContents = sumChildStart - sumChildEvap - sumChildDemandRelease + sumChildFOutflow - sumChildFInflow;
            if (pTarget - sumChildTargets - sumChildEstimatedContents > 0)
            { // We can fill
                // Go across all children without targets & target them for accrual.
                for (i = 0; i < mi.mInfo.childList.Length; i++)
                {
                    nChild = mi.mInfo.childList[i];
                    if ((nChild.myMother == n && !nChild.mnInfo.targetExists) || nChild == n) // Mode 2 valid -  Mode 1 valid
                    {
                        nChild.mnInfo.fldflow = 0;
                        // Add this child reservoir to a list of summation constraints
                        dConPtr = dConList;
                        dConList = new DistConstraint();
                        dConList.SetNext(dConPtr);
                        dConList.SetHi(nChild.m.max_volume);

                        // Build a DistList of all accrual links on incoming side.
                        // Include things like seascap links w/o owners & make
                        // sure to include last fill links.
                        // Build a second distlist for the child reservoirs w/o
                        // targets.  Use it to calc target for SYSNUM res's.
                        TotalLastFillRefill = 0;
                        for (ll = nChild.InflowLinks; ll != null; ll = ll.next)
                        {
                            l = ll.link;
                            if (!l.mlInfo.isArtificial)
                            {
                                if (l.mlInfo.isAccrualLink)
                                {
                                    leftToAccrue = l.mrlInfo.lnkSeasStorageCap - l.mrlInfo.sumPrevOwnAcrul + l.mrlInfo.current_evap - l.mrlInfo.contribLast;
                                    if (leftToAccrue < 0)
                                        leftToAccrue = 0;
                                    GlobalMembersConstraint.fake_alloc_diststr(ref distList, l.mlInfo.cost, 0, leftToAccrue);
                                    distList.referencePtr = l;
                                    distList.biasFrac = (double)0.0;
                                    TotalLastFillRefill += l.mrlInfo.contribLast;

                                    // Add this link to the distribution Summation constraint.
                                    dConList.AddMember();
                                    holdCL = dConList.GetMemberList();
                                    holdCL.distItem = distList;
                                }
                                else if (l.m.lnkallow != 0) // Seascap only
                                {
                                }
                            }
                        }
                        // Handle last fill link value calc'd above
                        // is this code being called to many times for parent reservoirs with children?
                        if (distList != null && n.m.lastFillLink != null)
                        {
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList, n.m.lastFillLink.mlInfo.cost, 0, TotalLastFillRefill);
                            distList.referencePtr = n.m.lastFillLink;
                            distList.biasFrac = (double)0.0;
                        }
                    }
                }
                // Call the distribute routine with parent target as the
                // final summation constraint, using the child reservoir size
                // as summation constraints (dConList).

                GlobalMembersConstraint.DistributeWithSummationConstraints(pTarget - sumChildTargets - sumChildEstimatedContents, 0, distList, dConList, true);

                // Walk through the distList, assigning distributions to links
                // Add to the estimated ending contents.  Accrual capability
                // offsets any losses from evap, and even allows for backfill.
                for (dptr = distList; dptr != null; dptr = dptr.next)
                {
                    l = dptr.referencePtr;
                    l.mlInfo.hi = dptr.returnValWhole;
                    if (l.to.mnInfo.spillLink != null)
                        l.to.mnInfo.spillLink.mlInfo.hi = l.to.mnInfo.demout;
                    if (!l.to.mnInfo.targetExists)
                        l.to.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] += dptr.returnValWhole;
                    l.to.mnInfo.targetLink.mlInfo.hi = l.to.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                    l.to.mnInfo.evapLink.mlInfo.hi = System.Math.Max(l.to.mnInfo.evpt, 0);
                }

                // If for some reason the target is negative, truncate to zero.
                // Note that there is most likely an error elsewhere to make this happen.
                for (dptr = distList; dptr != null; dptr = dptr.next)
                {
                    l = dptr.referencePtr;
                    if (!l.to.mnInfo.targetExists && l.to.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] < 0)
                    {
                        l.to.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] = 0;
                        l.to.mnInfo.targetLink.mlInfo.hi = 0;
                    }
                }

                // If relaxaccrual on, we need to set the child reservoir target
                // based on physical capacity.  Note that this means there is only
                // one child reservoir with a system number without a target.
                // RaS operation: find child reservoirs w/ no targ & w/ sysnums
                // Arbitrarily set the target to match the parent - sumChildTargets + evap.
                if (mi.relaxAccrual != 0)
                {
                    for (j = 0; j < mi.mInfo.childList.Length; j++)
                    {
                        nChild = mi.mInfo.childList[j];
                        targetnumber = pTarget - sumChildTargets;
                        if (nChild.myMother == n && !nChild.mnInfo.targetExists && nChild.m.sysnum > 0 && targetnumber > 0)
                        {
                            if (targetnumber > 0)
                            {
                                if (targetnumber > nChild.m.max_volume)
                                {
                                    nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] = nChild.m.max_volume;
                                    targetnumber -= nChild.m.max_volume;
                                    // This looks buggy- I think we mean to set the target to max_volume if targetnumber > max_volume
                                    //nChild->m->adaTargetsM->increment(timestep, hydstateindex,
                                    //           -nChild->m->max_volume);
                                }
                                else
                                {
                                    nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] = targetnumber;
                                    targetnumber = 0;
                                }
                                nChild.mnInfo.targetLink.mlInfo.hi = nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                                nChild.mnInfo.evapLink.mlInfo.hi = System.Math.Max(l.to.mnInfo.evpt, 0);
                            }
                            else
                            {
                                nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] = 0;
                                nChild.mnInfo.evapLink.mlInfo.hi = System.Math.Max(nChild.mnInfo.evpt, 0);
                                nChild.mnInfo.targetLink.mlInfo.hi = 0;
                            }
                        }
                    }
                }

                // loop & delete head of list
                for (; dConList != null; )
                {
                    dConPtr = dConList.GetNext();
                    dConList.DeleteAllMembers();
                    dConList = null;
                    dConList = dConPtr;
                }

                // delete head of list ??again?? -- Distrib.cc same way ??why??
                if (dConList != null)
                {
                    dConList.DeleteAllMembers();
                    dConList = null;
                }

                GlobalMembersConstraint.fake_free_diststr(ref distList);
            }
            else if (pTarget - sumChildTargets - sumChildEstimatedContents < 0)
            { // We must release
                // Release to meet parent target.  Close off accrual capability.
                // This code not necessary for the RaS reservoirs.
                for (i = 0; i < mi.mInfo.childList.Length; i++)
                {
                    nChild = mi.mInfo.childList[i];
                    if ((nChild.myMother == n && !nChild.mnInfo.targetExists) || nChild == n)
                    {
                        nChild.mnInfo.fldflow = 0;
                        for (ll = nChild.InflowLinks; ll != null; ll = ll.next)
                        {
                            l = ll.link;
                            if (l.mlInfo.isAccrualLink)
                            {
                                l.mlInfo.hi = 0;
                            }
                        }
                    }
                    // Get a distList of all child reservoirs w/o targs
                    if (nChild.myMother == n && !nChild.mnInfo.targetExists)
                    {
                        // Add nChild to distlist -- Use max capacity as the dist proportion
                        // Use the estimated ending storage as the available water for the dist.
                        GlobalMembersConstraint.fake_alloc_diststr(ref distList, nChild.m.max_volume, 0, System.Math.Max(0, nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex]));
                        //nChild->m->adaTargetsM->getDataL(timestep, hydstateindex)));
                        distList.referencePtrN = nChild;
                        distList.biasFrac = (double)0.0;
                    }
                }

                if (distList != null)
                {
                    GlobalMembersConstraint.DistributeProportional(0, pTarget - sumChildTargets - sumChildEstimatedContents, distList, false);
                    for (dptr = distList; dptr != null; dptr = dptr.next)
                    {
                        nChild = dptr.referencePtrN;
                        nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] -= dptr.returnValWhole;
                        //	   nChild->m->adaTargetsM->increment(timestep, hydstateindex, -dptr->returnValWhole);
                        nChild.mnInfo.targetLink.mlInfo.hi = nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                        //		   nChild->m->adaTargetsM->getDataL(timestep, hydstateindex);
                        // We have already taken into account the demand release in the estimated
                        // target for each child reservoir.  The returned Dist value is the extra
                        // flood release for this child.
                        nChild.mnInfo.fldflow = dptr.returnValWhole;
                    }
                }

                GlobalMembersConstraint.fake_free_diststr(ref distList);
            }
            else
            { // Current storage is perfect to meet requested outflow (DR)
                // Close off inflow
                // Close off outflow except for demandrelease
                // Estimated target is perfect for the reservoir

                for (i = 0; i < mi.mInfo.childList.Length; i++)
                {
                    nChild = mi.mInfo.childList[i];
                    if ((nChild.myMother == n && !nChild.mnInfo.targetExists) || nChild == n)
                    {
                        nChild.mnInfo.fldflow = 0;
                        for (ll = nChild.InflowLinks; ll != null; ll = ll.next)
                        {
                            l = ll.link;
                            if (l.mlInfo.isAccrualLink)
                                l.mlInfo.hi = 0;
                        }
                        nChild.mnInfo.targetLink.mlInfo.hi = nChild.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                        //         nChild->m->adaTargetsM->getDataL(timestep, hydstateindex);
                    }
                }
            }
        }

        /*****************************************************************************
        CalcChildForcedInflow - Gather forced inflow for named child
        -------------------------------------------------------------------------------
        Forced inflow is inflow that does not pass through an accrual link,
        a last fill link, or a seascap link w/o owners.
        \*****************************************************************************/


        public static long CalcChildForcedInflow(Node nChild)
        {
            long retval = 0;
            LinkList ll;
            Link l = null;
            for (ll = nChild.InflowLinks; ll != null; ll = ll.next)
            {
                l = ll.link;
                // Nothing special about this link - just forced flow
                if (!l.mlInfo.isArtificial && !l.mlInfo.isAccrualLink && !l.mlInfo.isLastFillLink)
                    retval += l.mlInfo.flow0; // STG step flow
            }
            return retval;
        }
        /*****************************************************************************
        CalcChildForcedOutflow - Gather forced outflow for named child
        -------------------------------------------------------------------------------
        Forced outflow is outflow not passing through a reservoir outflow
        link (parent = itself).
        \*****************************************************************************/

        public static long CalcChildForcedOutflow(Node nChild)
        {
            long retval = 0;
            LinkList ll;
            Link l = null;
            for (ll = nChild.OutflowLinks; ll != null; ll = ll.next)
            {
                l = ll.link;
                // Nothing special about this link - just forced flow
                if (!l.mlInfo.isArtificial && l.m.accrualLink != l)
                    retval += l.mlInfo.flow0; // STG step flow
            }
            return retval;
        }
    }
}
