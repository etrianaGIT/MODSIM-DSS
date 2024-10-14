using System;

namespace Csu.Modsim.ModsimModel
{
    public static class ResCalculator
    {
        /// <summary>Calculates the mean hydropower production from a reservoir, and sets the output variables.</summary>
        /// <param name="mi">The model for which to calculate mean hydropower.</param>
        /// <param name="n">The reservoir node for which to calculate mean hydropower.</param>
        /// <param name="mon">The month index.</param>
        public static void CalcMeanHydroPower(Model mi, Node n, int mon)
        {
            Node n_dem;
            long itrel1;
            double release;
            LinkList ll;
            double nethead;
            int i;
            bool usingOutflow;

            // Reservoir outflow links can be defined within the Storage Rights extension
            if (n.m.resOutLink != null)
            {
                //If outflow link defined use it for hydropower calculation.
                //release = (double)n.m.resOutLink.mlInfo.flow / ((n.m.resOutLink.mlInfo.flow != mi.defaultMaxCap) ? mi.ScaleFactor : 1);
                release = (double)n.m.resOutLink.mlInfo.flow / mi.ScaleFactor;
                usingOutflow = true;
            }
            else
            {
                //release = (double)n.mnInfo.downstrm_release[mon] / ((n.mnInfo.downstrm_release[mon] != mi.defaultMaxCap) ? mi.ScaleFactor : 1);
                release = (double)n.mnInfo.downstrm_release[mon] /  mi.ScaleFactor ;
                usingOutflow = false;
            }

            //This if is called in regular (non-Child) reservoirs.
            if (n.mnInfo.parent)
            {
                // Add any flow going directly through the reservoir not coming out of a child.
                if (n.m.resBypassL != null)
                {
                    release += (double)n.m.resBypassL.mlInfo.flow / mi.ScaleFactor;
                    n.mnInfo.ipinf = n.m.resBypassL.mlInfo.flow;
                }

                // Subtract any diversions specified as directly from the reservoir. 
                double directOut = 0.0;
                for (i = 0; i < mi.mInfo.demList.Length; i++)
                {
                    n_dem = mi.mInfo.demList[i];
                    if (release > 0 && n_dem.m.demDirect == n)
                        for (ll = n_dem.InflowLinks; ll != null; ll = ll.next)
                            if (!(ll.link.mlInfo.isArtificial) && (ll.link.mlInfo.flow > 0))
                                directOut += (double)ll.link.mlInfo.flow;
                }
                n.mnInfo.iprel = (long)(n.mnInfo.downstrm_release[mon] - directOut + DefineConstants.ROFF);
                if (!usingOutflow)
                {
                    //if (directOut != mi.defaultMaxCap)
                    release -= directOut / mi.ScaleFactor;
                    if (release < 0)
                        release = 0.0;
                }
            }

            if (n.mnInfo.generatinghours.Length == 0 || n.mnInfo.generatinghours[mi.mInfo.CurrentModelTimeStepIndex, 0] < 0.0001)
                n.mnInfo.avg_hydropower[mon] = 0.0;
            else
            {
                itrel1 = (long)(release + DefineConstants.ROFF);

                // Get the powerplant or tailwater elevation.
                double tailElev = n.m.elev; 
                for (i = 0; i < n.m.twelevpts.Length; i++)
                    if (n.m.twelevpts[i] != 0)
                    {
                        n.mnInfo.tail_elevation = HydropowerElevDef.GetElev_TailWater(n, itrel1);
                        tailElev = n.mnInfo.tail_elevation;
                        break;
                    }

                n.mnInfo.head = n.mnInfo.avg_elevation - tailElev;
                nethead = n.mnInfo.head;
                if (nethead < 0.0) nethead = 0.0;
                if (n.HasOldHydropowerDefined)
                {
                    double eff = 1;
                    if (n.m.ResEffCurve != null)
                        eff = n.m.ResEffCurve.GetEfficiency(itrel1, mi.FlowUnits, nethead, mi.LengthUnits, true);

                    // Calculate length of the time step
                    DateTime date = mi.TimeStepManager.Index2Date(mi.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);

                    /* Compute power in KW */
                    n.mnInfo.avg_hydropower[mon] = HydropowerUnit.GetPower(release, mi.FlowUnits, nethead, mi.LengthUnits, eff, n.m.powmax, mi.PowerUnits, HydroUnitType.Turbine, date, n.mnInfo.generatinghours[mi.mInfo.CurrentModelTimeStepIndex, 0], n.m.peakGeneration, mi.timeStep);
                }
                else
                    n.mnInfo.avg_hydropower[mon] = 0;
            }
        }
        public static void SetAccrualLnkTotals(Model mi)
        {
            // this function sums the seasonal accrual for all ownership links for each accrual link
            // each own_accrual for the month needs to be set before this routine is called
            Link l;
            int i;
            for (i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                l = mi.mInfo.accrualLinkList[i];
                l.mrlInfo.own_accrual = l.SumOwnAccrual();
                l.mrlInfo.lnktot = l.mrlInfo.own_accrual + l.SumContribLast();
                l.mrlInfo.stglft = l.SumStglft();
            }
        }
        public static void SetupSpillLinks(Model mi)
        {
            Link l;
            int i;
            for (i = 0; i < mi.mInfo.resList.Length; i++)
            {
                l = mi.mInfo.resList[i].mnInfo.spillLink;
                if (l != null)
                {
                    l.mlInfo.hi = mi.defaultMaxCap; // 99999999;
                    l.mlInfo.cost = i + DefineConstants.COST_MEDSMALL; // 2999999 + i; // make them slightly unique
                }
            }
            mi.mInfo.spillToMassBal.mlInfo.hi = mi.defaultMaxCap_Super; // 299999999;
        }

        /// <summary>
        /// The operation of the reservoirs in the deadpool needs handling because the cost set to the 
        /// links between deadpools could generate movement of water between reservoir when phisically is 
        /// not possible to move water. 
        /// </summary>
        /// <param name="mi">active MODSIM model</param>
        public static void DeadpoolOperation(Model mi)
        {
            Node n;
            int i;
            for (i = 0; i < mi.mInfo.resList.Length; i++)
            {
                n = mi.mInfo.resList[i];


                //if (n.m.min_volume > 0 && n.mnInfo.balanceLinks != null)
                //{

                LinkList llout;
                int outl;
                if (n.m.min_volume >= n.mnInfo.stend && n.m.min_volume>0) //(l.mlInfo.flow < l.mlInfo.hi) ||
                {
                    //Storage in the deadpool  
                    //First layer is the minimum volume in the reservoir.
                    Link l = n.mnInfo.balanceLinks.link;
                    long delta = l.mlInfo.hi - l.mlInfo.flow;
                    if (n.m.resOutLink != null)
                    {

                        n.m.resBypassL.mlInfo.hi = Math.Max(0, n.m.resBypassL.mlInfo.flow - delta);
                        delta = Math.Max(0, delta - n.m.resBypassL.mlInfo.flow);
                        mi.FireOnMessage("  [" + mi.mInfo.Iteration + "] Reservoir " + n.name + " in deadpool, adjusting bypass (" + n.m.resBypassL.name + ") to: " + n.m.resBypassL.mlInfo.hi);

                        n.m.resOutLink.mlInfo.hi = Math.Max(0, n.m.resOutLink.mlInfo.flow - delta);
                        //delta = Math.Max(0, delta - n.m.resOutLink.mlInfo.flow);
                        mi.FireOnMessage("  [" + mi.mInfo.Iteration + "] Reservoir " + n.name + " in deadpool, adjusting outflow (" + n.m.resOutLink.name + ") to: " + n.m.resOutLink.mlInfo.hi);

                    }
                    else
                    {
                        double totRelease = 0;
                        for (outl = 0, llout = n.OutflowLinks; llout != null; outl++, llout = llout.next)
                            if (!llout.link.mlInfo.isArtificial)
                            {
                                totRelease += llout.link.mlInfo.flow;
                            }
                        for (outl = 0, llout = n.OutflowLinks; llout != null; outl++, llout = llout.next)
                        {
                            if (!llout.link.mlInfo.isArtificial)
                            {
                                if (delta > 0)
                                {
                                    if (delta >= llout.link.mlInfo.flow)
                                    {
                                        llout.link.mlInfo.hi = llout.link.mlInfo.flow;
                                        delta = Math.Max(0, delta - llout.link.mlInfo.hi);
                                    }
                                    else
                                    {
                                        llout.link.mlInfo.hi = Math.Max(0, delta);
                                        delta = 0;
                                    }
                                    mi.FireOnMessage("  Reservoir " + n.name + " in deadpool, adjusting outflow (" + llout.link.name + ") to: " + llout.link.mlInfo.hi);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //Storage above the deadpool  
                    //   if need to open the outflow links if they were prevously closed by the deadpool operation.
                    
                    //for (outl = 0, llout = n.OutflowLinks; llout != null; outl++, llout = llout.next)
                    //{
                    //    if (!llout.link.mlInfo.isArtificial)
                    //    {
                    //        if (llout.link.mlInfo.hi < mi.defaultMaxCap)
                    //        {
                    //            llout.link.mlInfo.hi = llout.link.mlInfo.hiVariable[mi.mInfo.CurrentModelTimeStepIndex, 0]; //mi.defaultMaxCap;
                    //        }
                    //    }
                    //}
                }
            }
        }

        // RKL We should separate this routine's functionality; first part deals with nodes with an ouflow link with channel loss rest of the function deals with reservoir bypass link flow and other inflow/outflow
        public static void SetupStreamFlows(Node n, int mon)
        {
            long idn = 0;
            long iup = 0;
            // long      ipi = 0;
            //  long      ipo = 0;
            //    long     *ip;
            LinkList ll;

            /* WARNING: reaches may not work. */

            for (ll = n.OutflowLinks; ll != null; ll = ll.next)
            {
                if (!(ll.link.mlInfo.isArtificial))
                {
                    //if(ll->link->mlInfo->isReach)  ip = &idn;
                    ////else                         ip = &ipo;

                    //*ip += ll->link->mlInfo->flow;
                    //if(ll->link->m->loss_coef < 1.0)
                    //  *ip += (long)(ll->link->mrlInfo->closs + ROFF);
                    if (ll.link.mlInfo.isReach)
                        idn += ll.link.mlInfo.flow;
                    if (ll.link.m.loss_coef < 1.0)
                        idn += (long)(ll.link.mrlInfo.closs + DefineConstants.ROFF);
                }
            }

            for (ll = n.InflowLinks; ll != null; ll = ll.next)
            {
                if (!(ll.link.mlInfo.isArtificial))
                {
                    //if(ll->link->mlInfo->isReach)  ip = &iup;
                    ////else                         ip = &ipi;

                    //*ip += ll->link->mlInfo->flow;
                    if (ll.link.mlInfo.isReach)
                        iup += ll.link.mlInfo.flow;
                }
            }
            n.mnInfo.upstrm_release[mon] = iup;
            n.mnInfo.downstrm_release[mon] = idn;
            n.mnInfo.canal_in[mon] = n.mnInfo.ipinf;
            n.mnInfo.canal_out[mon] = n.mnInfo.iprel;
        }
        /* RKL this routine should go away; don't have any priority on targetLink; should always be zero have at least one "balanceLink" with the priority RKL */
        public static void SetResTargetCosts(Model mi)
        {
            int i;
            Node n = null;

            //for(i = 0; i < mi->mInfo->resListLen; i++)
            for (i = 0; i < mi.mInfo.resList.Length; i++)
            {
                n = mi.mInfo.resList[i];
                if (n.mnInfo.ownerType == DefineConstants.OLD_MODSIM_RES)
                {
                    if (mi.runType == ModsimRunType.Explicit_Targets)
                    {
                        n.mnInfo.targetLink.mlInfo.cost = -50000 + 10 * n.m.priority[0];
                    }
                    else
                    {
                        n.mnInfo.targetLink.mlInfo.cost = -50000 + 10 * n.m.priority[n.mnInfo.hydStateIndex];
                    }
                }
            }
        }

    }
}
