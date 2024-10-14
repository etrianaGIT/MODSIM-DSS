using System;

namespace Csu.Modsim.ModsimModel
{
    public class FlowThrough
    {

        /* -----------------------------------------------------------------------*
        During iodd = 1 we are in the storage step and we want to set the high
                      bound equal to the flow in the natural flow step, causing
                      the natural flow in the storage step to be the same or lower.
                      We would like to save the upper bounds in the hibnd slot.

        REMEMBER that natural flow links will also set their lower bound, enforcing
        the same operation between the natural flow and storage steps.  STG=NF
        links can be any link that does not have a parent link.

        During iodd = 0 we are in the natural flow step and we need to reset the
                      hi to the value from the previous NF step.  The only
                      exception is Exchange Limit Links - They receive their
                      bounds by watching other links STG step flows.
        \*----------------------------------------------------------------------- */
        public static void SetSTGStepHiToNFFlow(Model mi, int iodd)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];

                if (l.m.flagSTGeqNF != 0 && !l.mlInfo.isOwnerLink)
                {
                    if (iodd == 1)
                    {
                        l.mrlInfo.hibnd = l.mlInfo.hi;
                        l.mlInfo.hi = l.mlInfo.flow;
                    }
                    if (iodd == 0 && mi.mInfo.Iteration > 0 && l.m.exchangeLimitLinks == null)
                    {
                        l.mlInfo.hi = l.mrlInfo.hibnd;
                    }
                }
            }
        }

        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * ZeroFlowThrus - Clear flowthru variables
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * This routine clears the incoming STG step and NF step variables.
         * Both the current iteration variables and the last iteration variables (0)
         * are cleared to zero.  Each flowthru node's values and return location's
         * values are cleared.
        \* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - */
        public static void ZeroFlowThrus(Model mi)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                n.mnInfo.ithruSTG0 = 0;
                n.mnInfo.ithruSTG = 0;
                n.mnInfo.ithruNF0 = 0;
                n.mnInfo.ithruNF = 0;
                for (int j = 0; j < 10; j++)
                {
                    if (n.m.idstrmx[j] != null)
                    {
                        n.m.idstrmx[j].mnInfo.irtnflowthruSTG = 0;
                        n.m.idstrmx[j].mnInfo.irtnflowthruNF = 0;
                        n.m.idstrmx[j].mnInfo.artFlowthruSTG = 0;
                        n.m.idstrmx[j].mnInfo.artFlowthruNF = 0;
                    }
                }
            }
        }

        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * CalcFlowThroughsV2convg - Verify flowthru convergence
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * This routine walks through all the flow through demands and sets returns
         * based on diversions.
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
        \* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - */
        public static void CalcFlowThroughsV2convg(Model mi, int nmown, int iodd, out bool convg)
        {
            convg = true;

            if (iodd == 1 || nmown == 0)
            {
                // Calculate total diversion, push previous value to "0" var
                for (int i = 0; i < mi.mInfo.demList.Length; i++)
                {
                    Node n = mi.mInfo.demList[i];
                    long jthru = 0;
                    long itotal = 0; // This is per demand - not like gwater.
                    if (n.m.idstrmx[0] != null || n.m.idstrmx[1] != null || n.m.idstrmx[2] != null || n.m.idstrmx[3] != null || n.m.idstrmx[4] != null || n.m.idstrmx[5] != null || n.m.idstrmx[6] != null || n.m.idstrmx[7] != null || n.m.idstrmx[8] != null || n.m.idstrmx[9] != null)
                    {
                        Link l = n.mnInfo.demLink;
                        if (n.mnInfo.flowThruReturnLink != null && iodd != 0)
                        {
                            l = n.mnInfo.flowThruReturnLink;
                        }
                        jthru = l.mlInfo.flow;
                        if (nmown > 0 && iodd != 0)
                        {
                            itotal = n.mnInfo.ithruNF + n.mnInfo.ithruSTG;
                        }
                        else if (nmown == 0) // No owners always in natflow step.
                        {
                            itotal = n.mnInfo.ithruNF;
                        }

                        // Now we have the total of last iteration and this one.
                        if (!Utils.NearlyEqual((double)jthru, (double)itotal, mi.flowthru_cp))
                        {
                            convg = false;
                            if (mi.mInfo.Iteration > 50 || mi.mInfo.Iteration == mi.maxit || mi.mInfo.Iteration == mi.mInfo.SMOOTHFLOTHRU)
                            {
                                long curr = nmown != 0 ? n.mnInfo.ithruNF + n.mnInfo.ithruSTG : n.mnInfo.ithruNF;
                                long prev = nmown != 0 ? n.mnInfo.ithruNF0 + n.mnInfo.ithruSTG0 : n.mnInfo.ithruNF0;
                                DateTime dt = mi.TimeStepManager.Index2Date(mi.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);

                                mi.FireOnError("No convergence in flow through:");
                                mi.FireOnError("    " + dt.ToString(TimeManager.DateFormat));
                                mi.FireOnError(string.Format("    node: {0}, iter: {1}, prev: {2}, curr: {3}", n.number, mi.mInfo.Iteration, prev, curr));

                                Console.WriteLine("No convergence in flow through:");
                                Console.WriteLine("    " + dt.ToString(TimeManager.DateFormat));
                                Console.WriteLine(string.Format("    node: {0}, iter: {1}, prev: {2}, curr: {3}", n.number, mi.mInfo.Iteration, prev, curr));
                            }
                        }
                    }
                }
            }
        }

        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * CalcFlowThroughsV2iter - calculate diversion and return for flowthrus
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * Calculate traditional flow through demands
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * Notes:
         * - Sometimes users will set their flowthru information on the second
         *   flowthru data location.  Populating any of the data fields is valid.
         * - Splicing (averaging btwn iters) is possible.  Had problems with bypass
         *   credit and evap here.
         * - This code is not called for the storagesteponly flag
        \* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - */
        public static void CalcFlowThroughsV2iter(Model mi, int nmown, int iodd, Model mi1)
        {
            long iter = mi.mInfo.Iteration;

            /* must clear all possible return locations */
            /* Shift values from the main variables to the "zero" variables for flow
             * smoothing if we like */
            /* need to do shifting and zeroing for both ithru and irtnflowthru */
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                if (iodd == 1 && nmown > 0) // If we are in the STG step and have owners.
                {
                    n.mnInfo.ithruSTG0 = n.mnInfo.ithruSTG;
                    n.mnInfo.ithruSTG = 0;
                    if (iter == 2) // Clear so averaging works
                    {
                        n.mnInfo.ithruSTG0 = 0;
                    }
                }
                else if (nmown == 0) // If we are in the nf step -OR- not interleaved.
                {
                    n.mnInfo.ithruNF0 = n.mnInfo.ithruNF;
                    n.mnInfo.ithruNF = 0;
                    if (iter == 1) // Clear so averaging works
                    {
                        n.mnInfo.ithruNF0 = 0;
                    }
                }
                for (int j = 0; j < 10; j++)
                {
                    if (n.m.idstrmx[j] != null)
                    {
                        if (iodd != 0 && nmown > 0) // If we are in the STG step and have owners.
                        {
                            n.m.idstrmx[j].mnInfo.irtnflowthruSTG = 0;
                            n.m.idstrmx[j].mnInfo.irtnflowthruNF = 0;
                            n.m.idstrmx[j].mnInfo.artFlowthruSTG = 0;
                            n.m.idstrmx[j].mnInfo.artFlowthruNF = 0;
                        }
                        else // We are in the natural flow step.
                        {
                            n.m.idstrmx[j].mnInfo.irtnflowthruNF = 0;
                            n.m.idstrmx[j].mnInfo.artFlowthruNF = 0;
                        }
                    }
                }
            }

            // Calculate total diversion, push previous value to "0" var
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                long jthru = 0;
                if (n.m.idstrmx[0] != null || n.m.idstrmx[1] != null || n.m.idstrmx[2] != null || n.m.idstrmx[3] != null || n.m.idstrmx[4] != null || n.m.idstrmx[5] != null || n.m.idstrmx[6] != null || n.m.idstrmx[7] != null || n.m.idstrmx[8] != null || n.m.idstrmx[9] != null)
                {
                    bool hasFlowthruReturnLink = (n.mnInfo.flowThruReturnLink != null);

                    Link l = n.mnInfo.demLink;
                    if (hasFlowthruReturnLink && iodd == 1)
                    {
                        l = n.mnInfo.flowThruReturnLink;
                    }

                    jthru = l.mlInfo.flow;
                    // Storage accounts storage step.  Calculate flow thru and set
                    // return for natural flow for only those links with
                    // "flagStorageStepOnly" set to 1.
                    if (nmown > 0 && iodd == 1)
                    {
                        n.mnInfo.ithruNF = jthru;
                        n.mnInfo.ithruNF0 = Math.Max(jthru, n.mnInfo.ithruNF0);
                        // This is where we lower the ithruNF by the flow in links
                        // with the "flagStorageStepOnly" set to 1.
                        for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                        {
                            Link lin = ll.link;
                            if (lin.mlInfo.isArtificial)
                            {
                                continue;
                            }
                            if (lin.m.flagStorageStepOnly != 0)
                            {
                                n.mnInfo.ithruNF = Math.Max(0, n.mnInfo.ithruNF - lin.mlInfo.flow);
                                n.mnInfo.ithruNF0 = Math.Max(0, n.mnInfo.ithruNF0 - lin.mlInfo.flow);
                            }
                        }
                        n.mnInfo.ithruSTG = jthru - n.mnInfo.ithruNF;
                    }
                    else if (nmown > 0 && mi.timeStep.TSType != ModsimTimeStepType.Monthly) // == ModsimTimeStepType.Daily)
                    {
                        // Keep return flows from storage step active here.  Natural flow
                        // step has no meaning for return flow in the monthly context.
                        // Need to think about this for the daily routing context though.
                        //
                        // André, Mar. 12, 2012: What about other timesteps or user-defined timesteps (!?!)
                        n.mnInfo.ithruNF = jthru;
                    }
                    else if (nmown == 0) // No owners always in natflow step.
                    {
                        n.mnInfo.ithruNF = jthru;
                    }

                    // Now for this demand, we can add to all the return locations
                    // based on the value for this demand.  Note that for later
                    // iterations, we definitely kick in the flow smoothing.

                    // Storage step is done.  We need to set return flow that was
                    // not STORAGE-STEP-ONLY into the NF step.  We need to set a combined
                    // storage only flow + natural flow for the storage step.
                    if (nmown > 0 && iodd == 1)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            if (n.m.idstrmx[j] == null)
                            {
                                continue;
                            }
                            Node nn = n.m.idstrmx[j];

                            long thruNF = (long)(n.m.idstrmfraction[j] * n.mnInfo.ithruNF + DefineConstants.ROFF);
                            long thruSTG = (long)(n.m.idstrmfraction[j] * n.mnInfo.ithruSTG + DefineConstants.ROFF);
                            if (iter >= DefineConstants.SMOOTHITERATION)
                            {
                                thruNF = (long)(n.m.idstrmfraction[j] * .5 * (n.mnInfo.ithruNF + n.mnInfo.ithruNF0) + DefineConstants.ROFF);
                                thruSTG = (long)(n.m.idstrmfraction[j] * .5 * (n.mnInfo.ithruSTG + n.mnInfo.ithruSTG0) + DefineConstants.ROFF);
                            }

                            nn.mnInfo.irtnflowthruNF += thruNF;
                            nn.mnInfo.irtnflowthruSTG += thruSTG;

                            if (hasFlowthruReturnLink)
                            {
                                nn.mnInfo.artFlowthruNF += thruNF;
                                nn.mnInfo.artFlowthruSTG += thruSTG;
                            }
                        }
                    }
                    //else if (nmown > 0) // NF step done -- Put return flow for STstep
                    //{
                    //}
                    else if (nmown == 0 || iodd == 0) // No owners always in natflow step.
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            if (n.m.idstrmx[j] == null)
                            {
                                continue;
                            }
                            Node nn = n.m.idstrmx[j];

                            double RF;
                            if (mi == mi1)
                            {
                                RF = 1F;
                            }
                            else
                            {
                                Node n1 = mi1.FindNode(n.number);
                                Node n2 = mi1.FindNode(nn.number);
                                RF = Routing.RFactorForReturnFlow(n1.backRRegionID, n2.backRRegionID);
                            }

                            long thruNF = (long)(n.m.idstrmfraction[j] * n.mnInfo.ithruNF + DefineConstants.ROFF);
                            if (iter >= DefineConstants.SMOOTHITERATION)
                            {
                                thruNF = (long)(n.m.idstrmfraction[j] * .5 * (n.mnInfo.ithruNF + n.mnInfo.ithruNF0) * (RF) + DefineConstants.ROFF);
                            }

                            nn.mnInfo.irtnflowthruNF += thruNF;

                            if (hasFlowthruReturnLink)
                            {
                                nn.mnInfo.artFlowthruNF += thruNF;
                            }
                        }
                    }
                }
            }
        }

        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
         * ManageFlowThroughReturnLinks - Open and close flowthru return link
         * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - *
        \* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - */
        public static void ManageFlowThroughReturnLinks(Model mi, int nmown, int iodd)
        {
            // Manage the flowThroughReturnLink
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                if (n.mnInfo.flowThruReturnLink == null)
                {
                    continue;
                }

                /* RKL
                // flowThruReturnLink got defined for ANY flow through demand, not just those with ownership links
                // is this the intent??  IF we have a flow through with a low priority link coming in, this link
                // may have a large benefit from downstream accumulation; we may want to revisit this; at least have
                // the option of using the flowThruReturnLink or not
                RKL */
                if (nmown > 0)
                {
                    if (iodd == 1) // NF step solved, Preparing for STG step
                    {
                        // open the flow thru link, close demand link
                        n.mnInfo.flowThruReturnLink.mlInfo.hi = n.mnInfo.demLink.mlInfo.hi;
                        n.mnInfo.demLink.mlInfo.hi = 0;
                    }
                    else
                    {
                        // close the flow thru link for NF step, demLink
                        // already set from OperateIter.SetDemandLinkHi
                        n.mnInfo.flowThruReturnLink.mlInfo.hi = 0;
                    }
                }
            }
        }

        // Check for watch links in a node, return true if they exist.
        public static int HasWatchLogic(Node n)
        {
            int hasWatchLogic = 0;

            for (int i = 0; i < 15; i++)
            {
                if (n.m.watchMaxLinks[i] != null || n.m.watchMinLinks[i] != null || n.m.watchLnLinks[i] != null || n.m.watchLogLinks[i] != null || n.m.watchExpLinks[i] != null || n.m.watchSqrLinks[i] != null || n.m.watchPowLinks[i] != null)
                {
                    hasWatchLogic = 1;
                }
            }

            return hasWatchLogic;
        }

        public static void SecondSTGStepSetflagStorageStepOnlyLO(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.flagStorageStepOnly != 0)
                {
                    l.mlInfo.lo = l.mlInfo.flow;
                }
            }
        }

        public static void SecondSTGStepSetOwnerLinkLo(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mlInfo.lo = l.mlInfo.flow;
            }
        }

        public static void AfterSecondSTGStepResetflowThruSTGLink(Model mi)
        {
            for (int j = 0; j < mi.mInfo.childList.Length; j++)
            {
                Node n2 = mi.mInfo.childList[j];
                if (n2.mnInfo.flowThruSTGLink != null)
                {
                    Link l = n2.mnInfo.flowThruSTGLink;
                    l.mlInfo.hi = mi.defaultMaxCap; // 99999999;
                }
            }
        }

        public static void StorageFlowOnlyLinks2nditer(Model mi, bool finish2nd)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];

                if (l.m.flagSecondStgStepOnly != 0)
                {
                    if (!l.mlInfo.isOwnerLink)
                    {
                        if (mi.mInfo.Iteration == 0) // Zero on first iter
                        {
                            l.mlInfo.lo = 0;
                            l.mrlInfo.hibnd = l.mlInfo.hi;
                            l.mlInfo.hi = 0;
                            l.mlInfo.flow = 0;
                        }
                        else
                        {
                            if (finish2nd)
                            {
                                l.mlInfo.lo = 0;
                                l.mrlInfo.hibnd = l.mlInfo.hi;
                                l.mlInfo.hi = 0;
                            }
                            else // preparing to run 2nd stg iter
                            {
                                l.mlInfo.hi = l.mrlInfo.hibnd;
                            }
                        }
                    }
                }
            }
        }

        public static void Set2ndSTGstepNFlinks2flow(Model mi)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (!l.mlInfo.isArtificial)
                    {
                        if (l.mlInfo.cost < 0 && l.m.flagStorageStepOnly == 0)
                        {
                            l.mlInfo.lo = Math.Min(l.mlInfo.flow, l.mlInfo.hi);
                        }
                    }
                }
            }
        }

    }
}
