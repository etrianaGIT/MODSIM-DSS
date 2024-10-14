using System;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersEvap
    {
        /*****************************************************************************
        EstimateEvap: Update the current calculation of the evaporation bounds.
        -------------------------------------------------------------------------------
        -- Call UpdateInflow routine after calling this routine.
        \*****************************************************************************/
        public static void EstimateIterEvap(Model mi, int iodd, int nmown, out bool convg)
        {
            convg = true;

            if (iodd == 0 && nmown > 0)
            {
                return;
            }

            // Go through all reservoirs
            // Calculate stend values (flow through target + excess link - evap estimate)
            // Call evap calculation routine
            // Loop back if necessary up to 3 times.
            // check convergence calculation
            if (iodd == 1 && nmown > 0)
            {
                for (int i = 0; i < mi.mInfo.resList.Length; i++)
                {
                    Node n = mi.mInfo.resList[i];
                    n.mnInfo.evapLink.mlInfo.hi = 0;
                }
            }

            for (int loopback = 0; loopback < 10; loopback++)
            {
                // Add the child reservoir stends to get the parent stends & calc evap
                for (int i = 0; i < mi.mInfo.resList.Length; i++)
                {
                    Node n = mi.mInfo.resList[i];

                    if (n.RESnext != n && n.mnInfo.parent)
                    {
                        // Find all children of this node
                        long sumstend = 0;
                        for (int j = 0; j < mi.mInfo.resList.Length; j++)
                        {
                            Node n2 = mi.mInfo.resList[j];
                            if (n2.myMother == n && n2.mnInfo.child)
                            {
                                sumstend += n2.mnInfo.targetLink.mlInfo.flow + n2.mnInfo.excessStoLink.mlInfo.flow;
                                if (n.mnInfo.evpt > 0)
                                {
                                    n.mnInfo.stend += n.mnInfo.evapLink.mlInfo.flow - n.mnInfo.evpt;
                                }
                            }
                        }
                        n.mnInfo.stend = Math.Max(0, sumstend);
                        GlobalMembersEvap.EstimateStartingEvap(mi, 0);
                    }
                }

                // For all other reservoirs, do the stend
                for (int i = 0; i < mi.mInfo.resList.Length; i++)
                {
                    Node n = mi.mInfo.resList[i];
                    // if this is a "nonchild parent" or parent reservoir with children
                    if (n.mnInfo.parent && n.RESnext == n || n.mnInfo.child)
                    {
                        // Calculate stend for this reservoir
                        n.mnInfo.stend = n.mnInfo.targetLink.mlInfo.flow + n.mnInfo.excessStoLink.mlInfo.flow;
                        if (n.mnInfo.evpt > 0)
                        {
                            n.mnInfo.stend += n.mnInfo.evapLink.mlInfo.flow - n.mnInfo.evpt;
                        }
                        n.mnInfo.stend = Math.Max(0, Math.Min(n.mnInfo.stend, n.m.max_volume));
                    }
                }
                // If this is a parent reservoir, calculate evap
                GlobalMembersEvap.EstimateStartingEvap(mi, 0);
            }

            // Now check for convergence on all nodes.  (stend && stend0)
            int iter = mi.mInfo.Iteration;
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];

                if (!Utils.NearlyEqual((double)n.mnInfo.stend, (double)n.mnInfo.stend0, mi.evap_cp))
                {
                    convg = false;

                    if (iter > 50)
                    {
                        DateTime dt = mi.TimeStepManager.Index2Date(mi.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);

                        mi.FireOnError("No convergence in ending storage:");
                        mi.FireOnError("    " + dt.ToString(TimeManager.DateFormat));
                        mi.FireOnError(string.Format("    node: {0}, iter: {1}, curr: {2}, prev: {3}", n.number, mi.mInfo.Iteration, n.mnInfo.stend, n.mnInfo.stend0));

                        Console.WriteLine("No convergence in ending storage:");
                        Console.WriteLine("    " + dt.ToString(TimeManager.DateFormat));
                        Console.WriteLine(string.Format("    node: {0}, iter: {1}, curr: {2}, prev: {3}", n.number, mi.mInfo.Iteration, n.mnInfo.stend, n.mnInfo.stend0));
                    }
                }
                n.mnInfo.stend0 = n.mnInfo.stend;
            }
        }

        /*****************************************************************************
        CalculateNodeEvapAndHydCap:  Update current estimates of evap and hydcap
        -------------------------------------------------------------------------------
        \*****************************************************************************/
        public static void CalculateNodeEvapAndHydCap(Node n, Model mi, long startContents, long endContents, out long evap, out long hydCap)
        {
            long outFlowCap0;
            long outFlowCap1;
            double fsur0;
            double fsur1;
            double fhead0;
            double fhead1;

            // Find the type of reservoir
            // child account - do nothing
            // Do the appropriate calculations

            HydropowerElevDef.GetData(n, (long)(startContents), out fsur0, out fhead0, out outFlowCap0);
            HydropowerElevDef.GetData(n, (long)(endContents), out fsur1, out fhead1, out outFlowCap1);
            double nodeevaprate = 0.0;
            long iter = mi.mInfo.Iteration;
            long index = mi.mInfo.CurrentModelTimeStepIndex;
            if (n.mnInfo.evaporationrate.Length > 0)
            {
                nodeevaprate = n.mnInfo.evaporationrate[index, 0];
            }

            // Evaporation
            double evaptemp = 0.5 * (fsur0 + fsur1) * nodeevaprate;
            if (evaptemp > n.mnInfo.targetLink.mlInfo.flow + n.mnInfo.excessStoLink.mlInfo.flow + n.mnInfo.evapLink.mlInfo.flow)
            {
                evaptemp = n.mnInfo.targetLink.mlInfo.flow + n.mnInfo.excessStoLink.mlInfo.flow + n.mnInfo.evapLink.mlInfo.flow;
            }
            evaptemp *= mi.ScaleFactor;
            evap = (long)(evaptemp + DefineConstants.ROFF);

            // Hydraulic Capacity
            if (n.mnInfo.hydraulicCapNode != null)
            {
                if (iter > mi.mInfo.SMOOTHHYDCAP)
                {
                    hydCap = Math.Min(n.mnInfo.hydCap, (long)(0.5 * (outFlowCap0 + outFlowCap1) + DefineConstants.ROFF));
                }
                else
                {
                    double aveContent = Math.Round((startContents + endContents) * 0.5,0);
                    HydropowerElevDef.GetData(n, (long)(aveContent), out fsur0, out fhead0, out outFlowCap0);
                    //hydCap = (long)(0.5 * (outFlowCap0 + outFlowCap1) + ROFF);
                    hydCap = outFlowCap0;
                }
            }
            else
            {
                hydCap = 0;
            }

            n.mnInfo.area = (double)((fsur0 + fsur1) * .5);
            n.mnInfo.avg_elevation = (double)0.5 * (fhead0 + fhead1);
            n.mnInfo.starting_elevation = fhead0;
            n.mnInfo.endElevation = fhead1;
        }

        /*****************************************************************************
        UpdateInflow: Calculate the initial evap estimate at iteration zero.
        -------------------------------------------------------------------------------
        -- Currently calculates the total inflow link flow.
        -- This routine takes into account negative evaporation estimates
        --  Called after EstimateStartingEvap
        \*****************************************************************************/
        public static void UpdateInflow(Model mi, int iodd, long nmown)
        {
            // I think we are trying to cover WAY too many cases here;
            //  this needs to be broken up into separate functions
            long negEvap;
            for (int i = 0; i < mi.mInfo.inflowNodes.Length; i++)
            {
                Link l = mi.mInfo.inflowNodes[i].mnInfo.infLink;
                Node n = l.to;

                long nodeinflow = 0;
                if (n.mnInfo.inflow.Length > 0)
                {
                    nodeinflow = n.mnInfo.inflow[mi.mInfo.CurrentModelTimeStepIndex, 0];
                }
                if (n.mnInfo.evpt < 0)
                {
                    negEvap = -(n.mnInfo.evpt);
                }
                else
                {
                    negEvap = 0;
                }
                if (mi.mInfo.Iteration > 0) // NOW we have values for irtnflowthru and iroutreturn
                {
                    l.mlInfo.lo = n.mnInfo.start + nodeinflow + n.mnInfo.iroutreturn + negEvap;

                    /* BLounsbury: irtnflowthruSTG and irtnflowthruNF include flowThruReturnLink
                     * flows and flowThruReturnLink is an artifical link that adds
                     * to the streamflow automatically. Previously, this double counted
                     * water, this infLink needs to be reduced by the flowThruReturnLink
                     * flow to not double count water. So, artFlowthruNF and artFlowthruSTG
                     * were created to keep track of flowThruReturnLink flows. */
                    if (iodd == 1 || nmown == 0) // use NF value
                    {
                        l.mlInfo.lo += n.mnInfo.irtnflowthruNF;
                        l.mlInfo.lo -= n.mnInfo.artFlowthruNF;
                    }
                    else // use storage flow through value - take care of storage only links
                    {
                        l.mlInfo.lo += n.mnInfo.irtnflowthruNF + n.mnInfo.irtnflowthruSTG;
                        l.mlInfo.lo -= n.mnInfo.artFlowthruNF + n.mnInfo.artFlowthruSTG;
                    }
                }
                else // iter is zero - if we PROPERLY initialize things this if block should not be needed
                {
                    l.mlInfo.lo = n.mnInfo.start + nodeinflow + negEvap;
                }
                l.mlInfo.hi = l.mlInfo.lo;
            }
        }

        /*****************************************************************************
        EstimateStartingEvap: Calculate the initial evap estimate at iteration zero.
        -------------------------------------------------------------------------------
        // At iteration zero:
        // For old modsim child reservoirs, sum all the targets and keep that
        // value around.  For ownership child reservoirs, get the target from
        // the parent target.
        // Use initial storage and target for parent reserovoir.
        // Calculate initial evap estimate and hydraulic capacity estimate
        // based on these values.

        -- Call UpdateInflow routine after calling this routine.
        \*****************************************************************************/
        public static void EstimateStartingEvap(Model mi, long InitialGuess)
        {
            long evap;
            long hydCap;
            long avgstg;
            long target;
            long index = mi.mInfo.CurrentModelTimeStepIndex;
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];

                // Make sure we are in a reservoir with evaporation.
                if (n.mnInfo.parent && n.mnInfo.targetcontent.Length > 0)
                {
                    if (InitialGuess != 0)
                    {
                        if (n.mnInfo.targetcontent.Length > index)
                        {
                            GlobalMembersEvap.CalculateNodeEvapAndHydCap(n, mi, n.mnInfo.start, n.mnInfo.targetcontent[index, n.mnInfo.hydStateIndex], out evap, out hydCap);
                        }
                        else
                        {
                            GlobalMembersEvap.CalculateNodeEvapAndHydCap(n, mi, n.mnInfo.start, n.mnInfo.start, out evap, out hydCap);
                        }
                    }
                    else
                    {
                        GlobalMembersEvap.CalculateNodeEvapAndHydCap(n, mi, n.mnInfo.start, n.mnInfo.stend, out evap, out hydCap);
                    }

                    // If negative evap, set a variable to add to the inflow arc
                    // otherwise, set the new bounds for the target storage link
                    target = n.mnInfo.targetcontent[index, n.mnInfo.hydStateIndex];
                    if (evap > 0)
                    {
                        n.mnInfo.targetLink.mlInfo.hi = target;
                        n.mnInfo.evapLink.mlInfo.hi = evap;
                        n.mnInfo.excessStoLink.mlInfo.hi = Math.Max(0, n.m.max_volume - target);
                    }
                    else
                    {
                        n.mnInfo.targetLink.mlInfo.hi = target;
                        n.mnInfo.evapLink.mlInfo.hi = 0;
                        n.mnInfo.excessStoLink.mlInfo.hi = n.m.max_volume - target;
                    }
                    n.mnInfo.evpt = evap;
                    if (n.mnInfo.hydraulicCapNode != null)
                    {
                        n.mnInfo.hydCap = hydCap;
                        n.mnInfo.hydraulicCapLink.mlInfo.hi = n.mnInfo.hydCap;
                    }
                }
            }
            // Distribute evap to all child reservoirs.
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                // Make sure we are in a reservoir with evaporation & children.
                evap = Math.Abs(n.mnInfo.evpt);
                diststr distList1 = null;
                if (n.mnInfo.parent && n.RESnext != n)
                {
                    // Walk through children and build a distribution list.
                    for (int j = 0; j < mi.mInfo.resList.Length; j++)
                    {
                        Node n2 = mi.mInfo.resList[j];
                        if (n2.myMother == n)
                        {
                            if (InitialGuess != 0)
                            {
                                avgstg = n2.mnInfo.start;
                                if (n2.mnInfo.targetcontent.Length > 0)
                                {
                                    avgstg = (n2.mnInfo.start + n2.mnInfo.targetcontent[index, n.mnInfo.hydStateIndex]) / 2;
                                }
                            }
                            else
                            {
                                avgstg = (n2.mnInfo.start + n2.mnInfo.stend) / 2;
                            }
                            GlobalMembersConstraint.fake_alloc_diststr(ref distList1, avgstg, 0, mi.defaultMaxCap - 1);
                            distList1.referencePtrN = n2;
                        }
                    }

                    if (distList1 != null)
                    {
                        GlobalMembersConstraint.DistributeProportional(evap, 0, distList1, true);

                        // Walk through the distlist distribute evaporation.
                        for (diststr dptr = distList1; dptr != null; dptr = dptr.next)
                        {
                            Node n2 = dptr.referencePtrN;
                            if (InitialGuess != 0)
                            {
                                target = n2.mnInfo.start;
                            }
                            else
                            {
                                target = n2.mnInfo.targetcontent[index, n.mnInfo.hydStateIndex];
                            }
                            if (evap == n.mnInfo.evpt) // Positive evap
                            {
                                n2.mnInfo.targetLink.mlInfo.hi = target;
                                n2.mnInfo.excessStoLink.mlInfo.hi = Math.Max(0, n2.m.max_volume - target);
                                n2.mnInfo.evpt = dptr.returnValWhole;
                                n2.mnInfo.evapLink.mlInfo.hi = dptr.returnValWhole;
                            }
                            else // Negative evap UpdateInflow is called to add to inflow link
                            {
                                n2.mnInfo.evpt = -(dptr.returnValWhole);
                                n2.mnInfo.targetLink.mlInfo.hi = target;
                                n2.mnInfo.evapLink.mlInfo.hi = 0;
                                n2.mnInfo.excessStoLink.mlInfo.hi = Math.Max(0, n2.m.max_volume - target);
                            }
                        }
                        GlobalMembersConstraint.fake_free_diststr(ref distList1);
                    }
                }
            }
        }

    }
}
