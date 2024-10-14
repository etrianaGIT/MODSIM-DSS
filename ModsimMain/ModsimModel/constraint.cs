using System;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersConstraint
    {

        // ---------------------------------------------------------------------------
        // Distribution routines fit better here.  Should be objectized.
        // ---------------------------------------------------------------------------

        /* Distribute by proportion passed inside diststr */
        public static void DistributeProportional(long plusAmount, long minusAmount, diststr distList, bool distErrorIgnore)
        {
            double theAmount = (double)(plusAmount - minusAmount);
            
            if (distList == null)
                throw new Exception("Bad news!  distList is null in constraint.cs at method DistributeProportional(..)");

            // check if bad bounds were passed
            if (theAmount > 0)
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                    if (dptr.constraintHi < dptr.constraintLo)
                        Model.FireOnErrorGlobal("Bad bounds in DistributeProportional routine");

            /* sum up distribution proportions */
            long sumDistProps = 0;
            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                sumDistProps += dptr.extra;

            /* Need to protect from devide by zero, looks like proportions are
             * meaningless in this case...
             */
            if (sumDistProps == 0)
            {
                GlobalMembersConstraint.DistributeEqualAmounts(plusAmount, minusAmount, distList, distErrorIgnore);
                return;
            }

            if (theAmount > 0)
            {
                for (int kount = 0; kount < 50; kount++)
                {
                    if (sumDistProps == 0 && theAmount > ((double)(plusAmount - minusAmount) / 1000) && !distErrorIgnore)
                    {
                        Model.FireOnErrorGlobal(string.Concat("Unable to distribute water: amount of excess = ", theAmount.ToString()));
                        Model.FireOnErrorGlobal(string.Format("Distribution = {0} - {1}", plusAmount.ToString(), minusAmount.ToString()));

                        if (theAmount > 5000)
                        {
                            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                                Model.FireOnErrorGlobal(string.Format("Link {0}, constraintL {1}, constraintH {2}", dptr.referencePtr.number.ToString(), dptr.constraintLo.ToString(), dptr.constraintHi.ToString()));
                            throw new Exception("Unable to distribute water");
                        }
                        break;
                    }
                    if (sumDistProps == 0)
                        break;
                    double fractionalAmount = theAmount / sumDistProps;
                    if (fractionalAmount < ((double)(plusAmount - minusAmount) / 1000) && (theAmount < 3.0))
                        break;
                    /* theAmount is an amount to be distributed - The only time
                     * we need it is for when some links reach their bounds and
                     * force a redistribution of the water.
                     *
                     * sumDistProps is allowed to double based on the distributions
                     * of links still in play.  Any links already at their bounds
                     * are given an extra value of zero and are taken out of play.
                     */
                    theAmount = 0.0;
                    sumDistProps = 0;

                    for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                    {
                        if (dptr.extra > 0)
                        {
                            double distSum = dptr.extra * fractionalAmount;
                            /* hit constraint */
                            if (distSum + dptr.returnValFrac > dptr.constraintHi)
                            {
                                theAmount += distSum + dptr.returnValFrac - dptr.constraintHi;
                                dptr.returnValFrac = (double)dptr.constraintHi;
                                dptr.extra = 0; // remove from distribution
                            }
                            else // successful distribution
                            {
                                dptr.returnValFrac += distSum;
                                sumDistProps += dptr.extra;
                            }
                        }
                    }
                }
                /* We now have a doubleing point answer, but we still need
                 * do some work to achieve an integer solution.
                 */
                /* need to walk through priorities, truncating doubleing point answer,
                 * keeping track of the remainder
                 */
                double sumFractions = 0.0;
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    dptr.returnValWhole = (long)(dptr.returnValFrac);
                    if (dptr.extra > 0)
                    {
                        dptr.returnValFrac -= (double)dptr.returnValWhole;
                        sumFractions += dptr.returnValFrac;
                    }
                    else
                    {
                        dptr.returnValFrac = 0.0;
                    }
                }
                /* Now loop through, doing a sort to find the top (long) sumFractions
                 * values and keep them around.  Remember that we keep the memory
                 * around until later.
                 */
                long numOfSorted = 0;
                sortstr headSorted = null;
                double worstValue = (double)DefineConstants.NODATAVALUE;
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    if (numOfSorted < (long)sumFractions)
                    {
                        GlobalMembersConstraint.push_alloc_sorted(ref headSorted, dptr.returnValFrac + dptr.biasFrac);
                        headSorted.ptr = dptr;
                        if (headSorted.value < worstValue)
                            worstValue = headSorted.value;
                        numOfSorted++;
                    }
                    else if (worstValue < dptr.returnValFrac + dptr.biasFrac)
                    {
                        double newWorstValue = (double)DefineConstants.NODATAVALUE; //999999.0;
                        for (sortstr sortptr = headSorted; sortptr != null; sortptr = sortptr.next)
                        {
                            if (sortptr.value == worstValue)
                            {
                                sortptr.value = dptr.returnValFrac + dptr.biasFrac;
                                sortptr.ptr = dptr;
                            }
                            if (newWorstValue > sortptr.value)
                                newWorstValue = sortptr.value;
                        }
                        worstValue = newWorstValue;
                    }
                }
                /* Now give each one of these guys a single unit of water.
                 * Set their residuals accordingly, and clear residuals for
                 * any owners that fill.
                 */
                for (sortstr sortptr = headSorted; sortptr != null; sortptr = sortptr.next)
                {
                    sortptr.ptr.returnValFrac += sortptr.ptr.biasFrac - 1;
                    sortptr.ptr.returnValWhole++;
                    if (sortptr.ptr.returnValWhole == sortptr.ptr.constraintHi)
                        sortptr.ptr.returnValFrac = (double)0.0;
                }
                /* get rid of the current sorted list if needed. */
                for (; GlobalMembersConstraint.pop_alloc_sorted(ref headSorted) != 0; )
                    ;
            }
            else if (theAmount < 0)
            {
                /* This is just an inverted form of the problem */
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    dptr.biasFrac = -dptr.biasFrac;
                    long holdInt = dptr.constraintLo;
                    dptr.constraintLo = -dptr.constraintHi;
                    dptr.constraintHi = -holdInt;
                }
                GlobalMembersConstraint.DistributeProportional(minusAmount, plusAmount, distList, distErrorIgnore);
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    dptr.returnValWhole = -dptr.returnValWhole;
                    dptr.returnValFrac = -dptr.returnValFrac;
                    long holdInt = dptr.constraintLo;
                    dptr.constraintLo = -dptr.constraintHi;
                    dptr.constraintHi = -holdInt;
                }
            }
            else // the amount is zero
            {
                for (diststr dptr = distList; dptr != null; dptr = dptr.next)
                {
                    dptr.returnValWhole = 0;
                }
            }
        }

        /* Give everyone equal amounts - based on DistributeProportional */
        public static void DistributeEqualAmounts(long plusAmount, long minusAmount, diststr distList, bool distErrorIgnore)
        {
            for (diststr dptr = distList; dptr != null; dptr = dptr.next)
            {
                dptr.extra = 5;
            }
            GlobalMembersConstraint.DistributeProportional(plusAmount, minusAmount, distList, distErrorIgnore);
        }

        /* Distribute by priority passed inside diststr */
        public static void DistributeByPriority(long plusAmount, long minusAmount, diststr distList, bool distErrorIgnore)
        {
            long best;
            short inside = 0;
            long distAmount;
            diststr hold = null;
            diststr holdBest = null;
            diststr dptr = null;

            // temporary protection
            if (plusAmount < minusAmount)
            {
                string msg = "Internal Modsim error, bad numbers in DistributeByPriority";
                Model.FireOnErrorGlobal(msg);
                throw new Exception(msg);
            }

            // check if bad bounds were passed
            if (plusAmount > minusAmount) // Not required yet, but will be...
            {
                for (dptr = distList; dptr != null; dptr = dptr.next)
                {
                    if (dptr.constraintHi < dptr.constraintLo)
                    {
                        Model.FireOnErrorGlobal("Bad bounds in DistributeByPriority routine");
                        Console.WriteLine(string.Concat("constraintHi ", Convert.ToString(dptr.constraintHi)));
                        Console.WriteLine(string.Concat("constraintLo ", Convert.ToString(dptr.constraintLo)));
                        if (dptr.referencePtr != null)
                        {
                            Console.WriteLine(string.Concat("link ", Convert.ToString(dptr.referencePtr.number)));
                            Console.WriteLine(string.Concat("TimeStepIndex ", Convert.ToString(Model.RefModel.mInfo.CurrentModelTimeStepIndex)));
                            Console.WriteLine(string.Concat("Iteration ", Convert.ToString(Model.RefModel.mInfo.Iteration)));
                        }
                        else
                        {
                            Console.WriteLine(string.Concat("node ", Convert.ToString(dptr.referencePtrN.number)));
                        }
                    }
                }
            }
            distAmount = plusAmount - minusAmount;
            if (distAmount == 0)
            {
                for (dptr = distList; dptr != null; dptr = dptr.next)
                {
                    dptr.returnValWhole = 0;
                }
                return;
            }

            for (distAmount = plusAmount - minusAmount; distAmount != 0; inside = 0)
            {
                best = DefineConstants.NODATAVALUE; //9999999;
                holdBest = null;
                for (hold = distList; hold != null; hold = hold.next)
                {
                    /* look for a place to put it. */
                    if (hold.extra < best && hold.returnValWhole == 0 && hold.constraintHi != 0)
                    {
                        best = hold.extra;
                        holdBest = hold;
                        inside++;
                    }
                }
                if (inside != 0 && holdBest != null)
                {
                    distAmount -= holdBest.constraintHi;
                    holdBest.returnValWhole = holdBest.constraintHi;
                    if (distAmount < 0)
                    {
                        holdBest.returnValWhole += distAmount;
                        distAmount = 0;
                        break;
                    }
                }
                else
                {
                    if (!distErrorIgnore)
                    {
                        Model.FireOnErrorGlobal(string.Format("unable to distribute by priority: Excess = {0}", distAmount.ToString()));
                    }
                    distAmount = 0;
                }
            }
        }
        public static void DistributeWithSummationConstraints(long plusAmount, long minusAmount, diststr distList, DistConstraint sumConstraints, bool distErrorIgnore)
        {
            DistConstraint cptr;
            long maximum_constraint;
            long found_maximum_constraint;
            long sum_members;
            long found_hold_best;
            long hold_constraint_hi;
            constraintliststr dsptr;
            long best;
            short inside = 0;
            long distAmount;
            diststr hold = null;
            diststr holdBest = null;
            diststr dptr = null;

            // temporary protection
            if (plusAmount < minusAmount)
            {
                string msg = "error - bad numbers in DistributeWithSummationConstraints";
                if (dptr.referencePtr != null)
                    Console.WriteLine(string.Concat("link ", Convert.ToString(dptr.referencePtr.number)));
                else
                    Console.WriteLine(string.Concat("node ", Convert.ToString(dptr.referencePtrN.number)));
                Model.FireOnErrorGlobal(msg);
                throw new Exception(msg);

            }

            // A little bit of stolen code from DistributeByPriority.
            // check if bad bounds were passed
            if (plusAmount > minusAmount) // Not required yet, but will be...
            {
                for (dptr = distList; dptr != null; dptr = dptr.next)
                {
                    if (dptr.constraintHi < dptr.constraintLo)
                    {
                        Model.FireOnErrorGlobal("Bad bounds in DistributeWithSummationConstraints routine");
                        if (dptr.referencePtr != null)
                            Console.WriteLine(string.Concat("link ", Convert.ToString(dptr.referencePtr.number)));
                        else
                            Console.WriteLine(string.Concat("node ", Convert.ToString(dptr.referencePtrN.number)));
                    }
                }
            }

            distAmount = plusAmount - minusAmount;
            for (dptr = distList; dptr != null; dptr = dptr.next)
                dptr.returnValWhole = 0;

            for (distAmount = plusAmount - minusAmount; distAmount != 0; inside = 0)
            {
                best = DefineConstants.NODATAVALUE; //9999999;
                holdBest = null;
                for (hold = distList; hold != null; hold = hold.next)
                {
                    /* look for a place to put it. */
                    if (hold.extra < best && hold.returnValWhole == 0 && hold.constraintHi != 0 && hold.remove_from_consideration == 0)
                    {
                        best = hold.extra;
                        holdBest = hold;
                        inside++;
                    }
                }
                if (inside != 0 && holdBest != null)
                {
                    // Calculate the most we can give this distlist item
                    maximum_constraint = DefineConstants.NODATAVALUE; //999999999;
                    found_maximum_constraint = 0;
                    for (cptr = sumConstraints; cptr != null; cptr = cptr.GetNext())
                    {
                        sum_members = 0;
                        found_hold_best = 0;
                        for (dsptr = cptr.GetMemberList(); dsptr != null; dsptr = dsptr.next)
                        {
                            sum_members += dsptr.distItem.returnValWhole;
                            if (dsptr.distItem == holdBest)
                            {
                                found_hold_best = 1;
                            }
                        }
                        // Set maximum constraint because we found hold_best.
                        if (found_hold_best != 0)
                        {
                            // Check constraint to see if it will be in play
                            if (cptr.GetHi() - sum_members < holdBest.constraintHi)
                            {
                                if ((found_maximum_constraint != 0 && maximum_constraint > cptr.GetHi() - sum_members) || found_maximum_constraint == 0)
                                {
                                    found_maximum_constraint = 1;
                                    maximum_constraint = cptr.GetHi() - sum_members;
                                }
                            }
                        }
                    }

                    // Distribute to this MY_T("best") holder, within the constraints of
                    // all external summations.
                    hold_constraint_hi = holdBest.constraintHi; // Save Hi con
                    // Bcse of possible 0 distribution to this distitem, we must have:
                    holdBest.remove_from_consideration = 1;
                    if (found_maximum_constraint != 0)
                        holdBest.constraintHi = Math.Min(maximum_constraint, holdBest.constraintHi);
                    distAmount -= holdBest.constraintHi;
                    holdBest.returnValWhole = holdBest.constraintHi;
                    holdBest.constraintHi = hold_constraint_hi; // Restore Hi con
                    if (distAmount < 0)
                    {
                        holdBest.returnValWhole += distAmount;
                        distAmount = 0;
                        break;
                    }
                }
                else
                {
                    if (!distErrorIgnore)
                        Model.FireOnErrorGlobal(string.Format("unable to distribute by priority with summation constraints: Excess = {0}", distAmount.ToString()));
                    distAmount = 0;
                }
            }
        }

        // All values are in upstream storage time unless there is a lossfactor
        // associated with the storage.
        public static void DistributeWithSummationConstraintsChloss(long plusAmount, long minusAmount, diststr distList, DistConstraint sumConstraints, bool distErrorIgnore)
        {
            DistConstraint cptr;
            long maximum_constraint;
            long found_maximum_constraint;
            long sum_members;
            long found_hold_best;
            long hold_constraint_hi;
            constraintliststr dsptr;
            long best;
            short inside = 0;
            long distAmount;
            diststr hold = null;
            diststr holdBest = null;
            diststr dptr = null;
            long charge = 0;
            long credit = 0;
            double lossfactor = 0.0;
            double temporary_lossfactor;
            long temporary_constraint;

            // temporary protection
            if (plusAmount < minusAmount)
            {
                string msg = "Internal Modsim error, bad numbers in DistributeWithSummationConstraints";
                Model.FireOnErrorGlobal(msg);
                throw new Exception(msg);
            }

            // A little bit of stolen code from DistributeByPriority.
            // check if bad bounds were passed
            if (plusAmount > minusAmount) // Not required yet, but will be...
            {
                for (dptr = distList; dptr != null; dptr = dptr.next)
                {
                    if (dptr.constraintHi < dptr.constraintLo)
                    {
                        Model.FireOnErrorGlobal("Bad bounds in DistributeWithSummationConstraints routine");
                        if (dptr.referencePtr != null)
                            Console.WriteLine(string.Concat("link ", Convert.ToString(dptr.referencePtr.number)));
                        else
                            Console.WriteLine(string.Concat("node ", Convert.ToString(dptr.referencePtrN.number)));
                    }
                }
            }

            distAmount = plusAmount - minusAmount;
            for (dptr = distList; dptr != null; dptr = dptr.next)
            {
                dptr.returnValWhole = 0;
            }

            for (distAmount = plusAmount - minusAmount; distAmount != 0; inside = 0)
            {
                best = DefineConstants.NODATAVALUE; //9999999;
                holdBest = null;
                for (hold = distList; hold != null; hold = hold.next)
                {
                    /* look for a place to put it. */
                    if (hold.extra < best && hold.returnValWhole == 0 && hold.constraintHi != 0 && hold.remove_from_consideration == 0)
                    {
                        best = hold.extra;
                        holdBest = hold;
                        inside++;
                    }
                }
                if (inside != 0 && holdBest != null)
                {
                    // Calculate the most we can give this distlist item
                    maximum_constraint = DefineConstants.NODATAVALUE; //999999999;
                    found_maximum_constraint = 0;
                    int maxcnt = 50000; 
                    for (cptr = sumConstraints; cptr != null; cptr = cptr.GetNext())
                    {
                        sum_members = 0;
                        found_hold_best = 0;
                        int ctr = 0; 
                        for (dsptr = cptr.GetMemberList(); dsptr != null; dsptr = dsptr.next)
                        {
                            if (dsptr.distItem == null)
                                Model.FireOnErrorGlobal("A variable of type constraintliststr does not have its distItem defined... ");
                            else
                            {
                                sum_members += dsptr.distItem.returnValWhole;
                                if (dsptr.distItem == holdBest)
                                {
                                    found_hold_best = 1;
                                    charge = 0;
                                    credit = 0;
                                }

                                // Check for circular reference
                                ctr++;
                                if (ctr > maxcnt)
                                {
                                    Model.FireOnErrorGlobal("A circular distribution list (a linked list class 'constraintliststr') was detected (more than " + maxcnt.ToString() + " iterations). This happened while looking for");
                                    if (dsptr.distItem.referencePtr != null)
                                        Model.FireOnErrorGlobal("Link " + dsptr.distItem.referencePtr.name + " (" + dsptr.distItem.referencePtr.number.ToString() + ")");
                                    else
                                        Model.FireOnErrorGlobal("Node " + dsptr.distItem.referencePtrN.name + " (" + dsptr.distItem.referencePtrN.number.ToString() + ")");
                                    break;
                                }
                            }
                        }
                        ctr = 0; 
                        for (dsptr = cptr.GetMemberListCharge(); dsptr != null; dsptr = dsptr.next)
                        {
                            if (dsptr.distItem == null)
                                Model.FireOnErrorGlobal("A variable of type constraintliststr does not have its distItem defined... ");
                            else
                            {
                                sum_members += (long)(dsptr.distItem.returnValWhole / dsptr.distItem.lossFactorCharge + DefineConstants.ROFF);
                                if (dsptr.distItem == holdBest)
                                {
                                    found_hold_best = 1;
                                    charge = 1;
                                    credit = 0;
                                }

                                // Check for circular reference
                                ctr++;
                                if (ctr > maxcnt)
                                {
                                    Model.FireOnErrorGlobal("A circular distribution list (a linked list class 'constraintliststr')  was detected (more than " + maxcnt.ToString() + " iterations). This happened while looking for");
                                    if (dsptr.distItem.referencePtr != null)
                                        Model.FireOnErrorGlobal("Link " + dsptr.distItem.referencePtr.name + " (" + dsptr.distItem.referencePtr.number.ToString() + ")");
                                    else
                                        Model.FireOnErrorGlobal("Node " + dsptr.distItem.referencePtrN.name + " (" + dsptr.distItem.referencePtrN.number.ToString() + ")");
                                    break;
                                }
                            }
                        }
                        ctr = 0;
                        for (dsptr = cptr.GetMemberListCredit(); dsptr != null; dsptr = dsptr.next)
                        {
                            if (dsptr.distItem == null)
                                Model.FireOnErrorGlobal("A variable of type constraintliststr does not have its distItem defined... ");
                            else
                            {
                                sum_members += (long)(dsptr.distItem.returnValWhole / dsptr.distItem.lossFactorCredit + DefineConstants.ROFF);
                                if (dsptr.distItem == holdBest)
                                {
                                    found_hold_best = 1;
                                    charge = 0;
                                    credit = 1;
                                }

                                // Check for circular reference
                                ctr++;
                                if (ctr > maxcnt)
                                {
                                    Model.FireOnErrorGlobal("A circular distribution list (a linked list class 'constraintliststr')  was detected (more than " + maxcnt.ToString() + " iterations). This happened while looking for");
                                    if (dsptr.distItem.referencePtr != null)
                                        Model.FireOnErrorGlobal("Link " + dsptr.distItem.referencePtr.name + " (" + dsptr.distItem.referencePtr.number.ToString() + ")");
                                    else
                                        Model.FireOnErrorGlobal("Node " + dsptr.distItem.referencePtrN.name + " (" + dsptr.distItem.referencePtrN.number.ToString() + ")");
                                    break;
                                }
                            }
                        }

                        // Summation constraint is either in reservoir time or in downstream time
                        // lossfactorcredit will be less than one, increasing the distribution to meet
                        // this summation constraint.

                        // Set maximum constraint because we found hold_best.
                        if (found_hold_best != 0)
                        {
                            if (charge != 0)
                                temporary_lossfactor = holdBest.lossFactorCharge;
                            else if (credit != 0)
                                temporary_lossfactor = holdBest.lossFactorCredit;
                            else
                                temporary_lossfactor = 1.0;

                            // Move into upstream time based on lossfactor
                            temporary_constraint = (long)((cptr.GetHi() - sum_members) + DefineConstants.ROFF);
                            // If constraint in play, limit maximum_constraint by it

                            //if(temporary_constraint*temporary_lossfactor < holdBest->constraintHi)
                            if ((temporary_constraint * temporary_lossfactor < holdBest.constraintHi && found_maximum_constraint == 0) || (found_maximum_constraint != 0 && temporary_constraint < maximum_constraint))
                            {
                                if (found_maximum_constraint == 0)
                                {
                                    found_maximum_constraint = 1;
                                    //maximum_constraint = temporary_constraint * lossfactor;
                                    maximum_constraint = temporary_constraint;
                                    lossfactor = temporary_lossfactor;
                                }
                                else if (temporary_constraint < maximum_constraint)
                                {
                                    //maximum_constraint = temporary_constraint * lossfactor;
                                    maximum_constraint = temporary_constraint;
                                    lossfactor = temporary_lossfactor;
                                }
                            }
                        }
                    }

                    // Distribute to this "best" holder, within the constraints of
                    // all external summations.
                    hold_constraint_hi = holdBest.constraintHi; // Save Hi con
                    // Bcse of possible 0 distribution to this distitem, we must have:
                    holdBest.remove_from_consideration = 1;
                    if (found_maximum_constraint != 0)
                        holdBest.constraintHi = Math.Min((long)(maximum_constraint * lossfactor + DefineConstants.ROFF), holdBest.constraintHi);
                    distAmount -= holdBest.constraintHi;
                    holdBest.returnValWhole = holdBest.constraintHi;
                    holdBest.constraintHi = hold_constraint_hi; // Restore Hi con
                    if (distAmount < 0)
                    {
                        holdBest.returnValWhole += distAmount;
                        distAmount = 0;
                        break;
                    }
                }
                else
                {
                    if (!distErrorIgnore)
                        Model.FireOnErrorGlobal(string.Format("unable to distribute by priority with summation constraints: Excess = {0}", distAmount.ToString()));
                    distAmount = 0;
                }
            }
        }

        /* 
         * pops the head from the sortstr passed and returns 1 if it 
         * succeeds, and zero if it was NULL
         */
        public static int pop_alloc_sorted(ref sortstr head)
        {
            sortstr hold = (head != null) ? (head).next : null;
            if (head != null)
                GlobalMembersConstraint.sortstrPush(head); // this rtn makes head->next dangerous to use

            head = hold;
            if (hold != null)
                return 1;
            else
                return 0;
        }

        /* 
         * need to build a cleanup routine for when it all goes sour...
         * takes an element from the free list, copies the appropriate value,
         * and updates head 
         */
        public static void push_alloc_sorted(ref sortstr head, double value)
        {
            sortstr hold = GlobalMembersConstraint.sortstrPop();

            if (hold == null)
            {
                string msg = "failed memory chunk allocation in push_alloc_sorted";
                Model.FireOnErrorGlobal(msg);
                throw new Exception(msg);
            }

            hold.value = value;
            hold.next = (head);
            head = hold;
        }

        /* take item passed and put it into the free list */
        public static void sortstrPush(sortstr head)
        {
            head.next = DistConstraint.freeListSortStr;
            DistConstraint.freeListSortStr = head;
        }
        public static sortstr sortstrPop()
        {
            sortstr hold = DistConstraint.freeListSortStr;
            if (DistConstraint.freeListSortStr != null)
            {
                DistConstraint.freeListSortStr = DistConstraint.freeListSortStr.next;
            }
            else
            {
                hold = new sortstr();
                hold.next = null;
            }
            return (hold);
        }
        public static void sortstrFree()
        {
            if (DistConstraint.freeListSortStr != null)
            {
                sortstr hold = DistConstraint.freeListSortStr;
                for (sortstr hold2 = DistConstraint.freeListSortStr.next; hold2 != null; hold2 = hold2.next)
                {
                    hold = hold2;
                }
            }
        }

        /* Allocate the initial free list - make a little overshoot */
        public static void sortstrInit()
        {
            DistConstraint.freeListSortStr = null;
            for (int i = 0; i < 75; i++)
            {
                sortstr hold = new sortstr();
                hold.next = DistConstraint.freeListSortStr;
                DistConstraint.freeListSortStr = hold;
            }
        }
        public static void fake_alloc_diststr(ref diststr distList, long proportion, long lo, long hi)
        {
            diststr hold= GlobalMembersConstraint.diststrPop();
            hold.extra = proportion;
            hold.constraintLo = lo;
            hold.constraintHi = hi;
            hold.remove_from_consideration = 0;
            hold.next = distList;
            distList = hold;
        }
        public static void fake_free_diststr(ref diststr distList)
        {
            for (; distList != null; )
            {
                diststr hold = distList;
                distList = (distList).next;
                hold.biasFrac = 0.0;
                hold.returnValWhole = 0;
                hold.returnValFrac = 0.0;
                GlobalMembersConstraint.diststrPush(hold);
            }
        }

        /* Allocate the initial free list - make a little overshoot */
        public static void diststrInit()
        {
            DistConstraint.freeListDistStr = null;
            for (int i = 0; i < 75; i++)
            {
                diststr hold = new diststr();
                hold.next = DistConstraint.freeListDistStr;
                DistConstraint.freeListDistStr = hold;
            }
        }
        public static void diststrFree()
        {
            if (DistConstraint.freeListDistStr != null)
            {
                diststr hold = DistConstraint.freeListDistStr;
                for (diststr hold2 = DistConstraint.freeListDistStr.next; hold2 != null; hold2 = hold2.next)
                {
                    hold = hold2;
                }
            }
        }
        // move to next diststr in list
        public static diststr diststrPop()
        {
            diststr hold = DistConstraint.freeListDistStr;

            if (hold == null) // back to the allocation wagon, leave the free list at NULL...
            {
                hold = new diststr(); //(diststr *) mcalloc(1, sizeof(diststr));
            }
            if (DistConstraint.freeListDistStr != null)
            {
                DistConstraint.freeListDistStr = DistConstraint.freeListDistStr.next;
            }
            return (hold);
        }

        /* take item passed and put it into the free list */
        public static void diststrPush(diststr head)
        {
            head.next = DistConstraint.freeListDistStr;
            DistConstraint.freeListDistStr = head;
        }

    }


}
