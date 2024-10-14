using System;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>takes old blocks of iteration sequence code out of operate</summary>
    public class OperateIter
    {
        public static void SaveNFStepFlow(Model mi, long outputtimestep)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                l.mrlInfo.natFlow[outputtimestep] = l.mlInfo.flow;
            }
        }

        public static void SetResOutLink_fldflow(Model mi)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.m.resOutLink != null)
                {
                    n.m.resOutLink.mlInfo.hi = n.mnInfo.fldflow;
                }
            }
        }

        public static void SetMassBalBoundsCost(Model mi)
        {
            mi.mInfo.massBalToInf.mlInfo.hi = mi.defaultMaxCap_Super; // 299999999;
            mi.mInfo.massBalToInf.mlInfo.cost = 0;
            mi.mInfo.massBalToInf.mlInfo.lo = 0;

            mi.mInfo.infToGwater.mlInfo.hi = mi.defaultMaxCap_Super; // 299999999;
            mi.mInfo.infToGwater.mlInfo.cost = 0;
            mi.mInfo.infToGwater.mlInfo.lo = 0;

            mi.mInfo.gwaterToInf.mlInfo.hi = mi.defaultMaxCap_Super; // 299999999;
            mi.mInfo.gwaterToInf.mlInfo.cost = 0;
            mi.mInfo.gwaterToInf.mlInfo.lo = 0;

            mi.mInfo.stoToMassBal.mlInfo.lo = 0;
            mi.mInfo.stoToMassBal.mlInfo.hi = mi.defaultMaxCap_Super; // 299999999;
            mi.mInfo.stoToMassBal.mlInfo.cost = 0;

            mi.mInfo.spillToMassBal.mlInfo.cost = 0;
        }

        public static void IterZeroSet_gwoutLink(Model mi)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Link l = mi.mInfo.demList[i].mnInfo.gwoutLink;
                if (l != null)
                {
                    l.mlInfo.hi = 0;
                    l.mlInfo.lo = 0;
                }
            }
        }

        public static void Set_chanLossLink_hiNF(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerChanlossList.Length; i++)
            {
                Link l = mi.mInfo.ownerChanlossList[i];
                if (l.from.mnInfo.chanLossLink != null)
                {
                    l.mrlInfo.attributeLossToStg = Math.Max(0, l.mrlInfo.attributeLossToStg);
                    l.from.mnInfo.chanLossLink.mlInfo.hi -= l.mrlInfo.attributeLossToStg;
                    if (l.from.mnInfo.chanLossLink.mlInfo.hi < 0)
                    {
                        l.mrlInfo.attributeLossToStg += l.from.mnInfo.chanLossLink.mlInfo.hi;
                    }
                    l.from.mnInfo.chanLossLink.mlInfo.hi = Math.Max(0, l.from.mnInfo.chanLossLink.mlInfo.hi);
                }
            }
        }

        public static void Set_chanLossLink_hiSTG(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerChanlossList.Length; i++)
            {
                Link l = mi.mInfo.ownerChanlossList[i];
                if (l.from.mnInfo.chanLossLink != null)
                {
                    l.from.mnInfo.chanLossLink.mlInfo.hi += Math.Max(0, l.mrlInfo.attributeLossToStg);
                }
            }
        }

        public static void Set_gwoutLink_cost(Model mi, ref long maxd1)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                if (mi.runType == ModsimRunType.Conditional_Rules)
                {
                    /* RKL
                    // get rid of this; we will set the cost to the priority read in; no translation
                    // cost on gwoutLink should be very negative; turn it off in NF step and on in storage step
                    RKL */
                    n.mnInfo.demLink.mlInfo.cost = -50000 + 10 * n.m.demr[n.mnInfo.hydStateIndex];
                    if (n.mnInfo.gwoutLink != null)
                    {
                        n.mnInfo.gwoutLink.mlInfo.cost = n.mnInfo.demLink.mlInfo.cost - 1;
                    }
                }
                else
                {
                    n.mnInfo.demLink.mlInfo.cost = -50000 + 10 * n.m.demr[0];
                    if (n.mnInfo.gwoutLink != null)
                    {
                        n.mnInfo.gwoutLink.mlInfo.cost = n.mnInfo.demLink.mlInfo.cost - 1;
                    }
                }
                maxd1 += n.mnInfo.demLink.mlInfo.hi;

                if (n.mnInfo.flowThruReturnLink != null)
                {
                    maxd1 += n.mnInfo.flowThruReturnLink.mlInfo.hi;
                }
            }
        }

        public static void InitArtificalLinks_lo_cost(Model mi, ref long maxd)
        {
            mi.mInfo.demToMassBal.mlInfo.hi = maxd;
            mi.mInfo.stoToMassBal.mlInfo.lo = 0;
            mi.mInfo.stoToMassBal.mlInfo.cost = 0;
            mi.mInfo.demToMassBal.mlInfo.lo = 0;
            mi.mInfo.demToMassBal.mlInfo.cost = 0;
            mi.mInfo.spillToMassBal.mlInfo.lo = 0;
            mi.mInfo.spillToMassBal.mlInfo.cost = 0;
            mi.mInfo.massBalToInf.mlInfo.lo = 0;
            mi.mInfo.massBalToInf.mlInfo.cost = 0;
            mi.mInfo.infToGwater.mlInfo.lo = 0;
            mi.mInfo.infToGwater.mlInfo.cost = 0;
            mi.mInfo.gwaterToInf.mlInfo.lo = 0;
            mi.mInfo.gwaterToInf.mlInfo.cost = 0;
        }

        public static void Set_gwrtnLink_hi_lo_cost(Model mi)
        {
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                Link l = n.mnInfo.gwrtnLink;
                if (l != null)
                {
                    l.mlInfo.cost = 0;
                    if (n.m.pcap > 0)
                    {
                        l.mlInfo.lo = 0;
                        l.mlInfo.hi = n.m.pcap;
                        l.mlInfo.cost = -50000 + 10 * n.m.pcost;
                    }
                }
            }
        }

        public static void SetRes_costNFstep(Model mi)
        {
            for (long i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                n.mnInfo.targetLink.mlInfo.cost = -50000 + 10 * n.m.priority[n.mnInfo.hydStateIndex];
                n.mnInfo.spillLink.mlInfo.cost = DefineConstants.COST_MEDSMALL;
            }
        }

        public static void SetResAccrualNFstep(Model mi)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES && n.m.sysnum > 0) || n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                {
                    for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                    {
                        if (ll.link.mlInfo.isAccrualLink) // Close accrual links - settarg
                        {
                            //WARNING CalcSumStgAccrual should be called outside all this only when
                            // iter = 0 since we are dealing with prevownaccrual
                            GlobalMembersDistrib.CalcSumStgAccrual(mi, ll.link);
                            GlobalMembersDistrib.CalcSumLastFill(mi, ll.link);
                            long space2fill = ll.link.mrlInfo.lnkSeasStorageCap - ll.link.mrlInfo.sumPrevOwnAcrul + ll.link.mrlInfo.current_rent_evap + ll.link.mrlInfo.current_evap - ll.link.mrlInfo.contribLast;
                            if (space2fill < 0)
                            {
                                mi.FireOnError(string.Format("{0} lnkSeasonStorageCap {1} sumPrevOwnAcrul {2} evap {3} contribLast {4}", ll.link.name, ll.link.mrlInfo.lnkSeasStorageCap, ll.link.mrlInfo.sumPrevOwnAcrul, ll.link.mrlInfo.current_evap + ll.link.mrlInfo.current_rent_evap, ll.link.mrlInfo.contribLast));
                            }
                            ll.link.mlInfo.hi = Math.Max(0, Math.Min(space2fill, ll.link.mlInfo.hi));

                            ll.link.SetHI(mi.mInfo.CurrentModelTimeStepIndex, ll.link.mlInfo.hi);

                            if (n.mnInfo.resLastFillLink != null)
                            {
                                n.mnInfo.resLastFillLink.mrlInfo.contribLast += ll.link.mrlInfo.contribLast;
                            }

                        }
                    }
                    //Sets last fill accrual amount
                    if (n.mnInfo.resLastFillLink != null)
                    {
                        n.mnInfo.resLastFillLink.mlInfo.hi = n.mnInfo.resLastFillLink.mrlInfo.contribLast + n.mnInfo.resLastFillLink.mrlInfo.current_last_evap;
                    }
                }
            }
        }

        public static void SetResCostSysnumRelaxAccrualSTGstep(Model mi)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                /*
                 * RKL: get rid of this cost setting on artificial target storage
                 * link; also don't mess with cost on spillLink; need a new link
                 * parallel to excess storage link: cost =0, open in NF step closed
                 * in storage step, artificial target storage link is closed in NF
                 * step open in storage step; this assures correct NF distribution,
                 * and NO changes in priorities between iterations
                 */
                Node n = mi.mInfo.resList[i];
                if ((n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES) && n.m.sysnum > 0)
                {
                    if (mi.runType == ModsimRunType.Conditional_Rules)
                    {
                        n.mnInfo.targetLink.mlInfo.cost = -70000 + 10 * n.m.priority[n.mnInfo.hydStateIndex];
                    }
                    else
                    {
                        n.mnInfo.targetLink.mlInfo.cost = -70000 + 10 * n.m.priority[0];
                    }
                    if (n.mnInfo.spillLink != null)
                    {
                        n.mnInfo.spillLink.mlInfo.cost = DefineConstants.COST_MED;    //9999999;
                    }
                    for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                    {
                        // maybe should worry about the last fill link somehow
                        if (ll.link.mlInfo.isAccrualLink) // Open up accrual links
                        {
                            ll.link.SetHI(mi.mInfo.CurrentModelTimeStepIndex, ll.link.mrlInfo.lnkSeasStorageCap);
                        }
                    }
                }
            }
        }

        public static void CheckTargetLinkHIltLO(Model mi)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.m.min_volume > 0)
                {
                    if (n.mnInfo.targetLink != null && n.mnInfo.targetLink.mlInfo.lo > n.mnInfo.targetLink.mlInfo.hi)
                    {
                        n.mnInfo.targetLink.mlInfo.hi = n.mnInfo.targetLink.mlInfo.lo;
                    }
                }
            }
        }

        public static void CheckLinksLOandHIbounds(Model mi)
        {
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                if (l.mlInfo.hi < 0 || l.mlInfo.hi > mi.defaultMaxCap_Super)
                {
                    if (!l.mlInfo.isArtificial)
                    {
                        if (string.IsNullOrEmpty(l.name))
                        {
                            mi.FireOnMessage(string.Format("Warning on link {0:D}: upper bound =  {1:D} > default upper bound = {2:D}\n", l.number, l.mlInfo.hi, mi.defaultMaxCap));
                        }
                        else
                        {
                            mi.FireOnMessage(string.Format("Warning on link {0}: upper bound =  {1:D} > default upper bound = {2:D}\n", l.name, l.mlInfo.hi, mi.defaultMaxCap));
                        }
                    }
                    l.mlInfo.hi = mi.defaultMaxCap_Super;
                }

                l.mlInfo.lo = Math.Max(0, Math.Min(l.mlInfo.lo, l.mlInfo.hi));
                l.mlInfo.hi = Math.Max(0, l.mlInfo.hi);
                l.mlInfo.flow = Math.Min(l.mlInfo.hi, Math.Max(l.mlInfo.lo, l.mlInfo.flow));
            }
        }

        public static void SetResCostNoSysnumNoRelaxAccrualSTGstep(Model mi)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.mnInfo.spillLink != null)
                {
                    n.mnInfo.spillLink.mlInfo.cost = DefineConstants.COST_MEDSMALL;    // 2999999;
                }
                if (n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES && n.m.sysnum == 0)
                {
                    if (mi.runType == ModsimRunType.Conditional_Rules) // using hydrologic state tables.
                    {
                        n.mnInfo.targetLink.mlInfo.cost = -50000 + 10 * n.m.priority[n.mnInfo.hydStateIndex];
                    }
                    else
                    {
                        n.mnInfo.targetLink.mlInfo.cost = -50000 + 10 * n.m.priority[0];
                    }
                }
            }
        }

        public static void CheckFlowThruSMOOTHBOUND(Model mi, int nmown)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Link l = mi.mInfo.demList[i].mnInfo.demLink;
                Node n = l.from;
                if (n.m.idstrmx[0] != null)
                {
                    long inow = (nmown > 0) ? n.mnInfo.ithruSTG + n.mnInfo.ithruNF : n.mnInfo.ithruNF;
                    long ipre = (nmown > 0) ? n.mnInfo.ithruSTG0 + n.mnInfo.ithruNF0 : n.mnInfo.ithruNF0;
                    if (inow >= ipre)
                    {
                        l.mlInfo.hi = ipre;
                        l.mlInfo.lo = 0;
                    }
                }
            }
        }

        public static void WatchLinksUpdateConvg(Model mi, int nmown, int iodd, int iter, out bool convgWatch)
        // BEWARE! iter is really newiter from operate
        {
            double linkSumLn;
            double linkSumLog;
            double linkSumExp;
            double linkSumSqr;
            double linkSumPow;
            double linkMax;
            double linkMin;
            double calcHi;
            int linkMinValid;
            int hasWatchLogic;
            int linkExpValid;
            int linkPowValid;

            convgWatch = true;

            if (iodd == 1 || nmown == 0)
            {
                // Handle watch nodes
                DateTime dt = mi.TimeStepManager.Index2Date(mi.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);
                for (int j = 0; j < mi.mInfo.demList.Length; j++)
                {
                    Node n = mi.mInfo.demList[j];
                    // nodedemand[ts,hs] is set in ExchangeLimitLinksSetDemands to watchNew so we can use
                    // demand for convergence test below
                    long demand = 0;
                    if (n.mnInfo.nodedemand.Length > 0)
                    {
                        demand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                    }
                    linkSumLn = 0.0;
                    linkSumLog = 0.0;
                    linkSumExp = 0.0;
                    linkSumSqr = 0.0;
                    linkSumPow = 0.0;
                    linkMax = 0.0;
                    linkMin = (double)mi.defaultMaxCap; //9999999.999;
                    linkMinValid = 0;
                    linkExpValid = 0;
                    linkPowValid = 0;
                    hasWatchLogic = 0;
                    calcHi = 0.0;

                    for (int i = 0; i < 15; i++)
                    {
                        if (n.m.watchMaxLinks[i] != null)
                        {
                            hasWatchLogic = 1;
                            if (n.m.watchMaxLinks[i].mlInfo.flow0 > linkMax)
                            {
                                linkMax = (double)n.m.watchMaxLinks[i].mlInfo.flow;
                            }
                        }
                        if (n.m.watchMinLinks[i] != null)
                        {
                            hasWatchLogic = 1;
                            if (n.m.watchMinLinks[i].mlInfo.flow0 < linkMin)
                            {
                                linkMin = (double)n.m.watchMinLinks[i].mlInfo.flow;
                                linkMinValid = 1;
                            }
                        }
                        if (n.m.watchLnLinks[i] != null)
                        {
                            hasWatchLogic = 1;
                            linkSumLn += (double)n.m.watchLnLinks[i].mlInfo.flow;
                        }
                        if (n.m.watchLogLinks[i] != null)
                        {
                            hasWatchLogic = 1;
                            linkSumLog += (double)n.m.watchLogLinks[i].mlInfo.flow;
                        }
                        if (n.m.watchExpLinks[i] != null)
                        {
                            hasWatchLogic = 1;
                            linkSumExp += (double)n.m.watchExpLinks[i].mlInfo.flow;
                            linkExpValid = 1;
                        }
                        if (n.m.watchSqrLinks[i] != null)
                        {
                            hasWatchLogic = 1;
                            linkSumSqr += (double)n.m.watchSqrLinks[i].mlInfo.flow;
                        }
                        if (n.m.watchPowLinks[i] != null)
                        {
                            hasWatchLogic = 1;
                            linkSumPow += (double)n.m.watchPowLinks[i].mlInfo.flow;
                            linkPowValid = 1;
                        }
                    }
                    // We now have values and can calculate:
                    // a0*max(1-15) + a1*min(1-15) + a2*ln(sum(1-15)) + a3*log(sum(1-15)) +
                    // a4*exp(sum(1-15)) + a5*sqr(sum(1-15)) + a6*pow(sum(1-15),powval)
                    if (hasWatchLogic != 0)
                    {
                        //if ((long)linkSumPow != mi.defaultMaxCap)
                        //{
                            linkSumPow /= mi.ScaleFactor;
                        //}
                        calcHi = linkPowValid * 100 * n.m.watchFactors[6] * (double)Math.Pow((double)linkSumPow, (double)n.m.powvalue);
                        calcHi = calcHi + n.mnInfo.nuse + n.m.watchFactors[0] * (double)linkMax + n.m.watchFactors[1] * (double)linkMin * (double)linkMinValid + n.m.watchFactors[2] * (double)((linkSumLn > 0) ? (Math.Log((double)linkSumLn)) : 0) + n.m.watchFactors[3] * (double)((linkSumLog > 0) ? (Math.Log10(linkSumLog)) : 0) + n.m.watchFactors[4] * (double)Math.Exp(linkSumExp) * linkExpValid + n.m.watchFactors[5] * (double)Math.Pow(linkSumSqr, 2.0);
                        //TODO: ET: Is the use of nuse supposed to be or it introduces a bug for demand customization.
                        // NOTE we increment any time series data from nuse

                        if (calcHi < 0)
                        {
                            calcHi = 0.0;
                        }

                        if (iter > mi.mInfo.SMOOTHOPER)
                        {
                            calcHi += (demand - calcHi) / 2;
                            if (demand - calcHi != 0)
                            {
                                string msg = " Smooting convergence watchlink (" + n.name + "): " + calcHi;
                                Console.WriteLine(msg);
                                mi.FireOnMessage(msg);
                            }
                        }
                        n.mnInfo.watchNew = (long)(calcHi + DefineConstants.ROFF);

                        //don't test convergence until iter 11
                        if (iter < 10)
                        {
                            convgWatch = false;
                            continue;
                        }

                        // nodedemand[ts,hs] is set in ExchangeLimitLinksSetDemands to watchNew so we can use
                        // demand for convergence test below
                        if (n.mnInfo.watchNew == demand)
                        {
                            continue;
                        }
                        if (!Utils.NearlyEqual((double)n.mnInfo.watchNew, (double)demand, mi.flowthru_cp))
                        {
                            convgWatch = false;

                            if (iter > 50)
                            {
                                long diff = Math.Abs(n.mnInfo.watchNew - demand);

                                mi.FireOnError("No convergence in demand watch:");
                                mi.FireOnError("    " + dt.ToString(TimeManager.DateFormat));
                                mi.FireOnError(string.Format("    node: {0}, iter: {1}, diff: {2}, curr: {3}", n.number, iter, diff, n.mnInfo.watchNew));

                                Console.WriteLine("No convergence in demand watch:");
                                Console.WriteLine("    " + dt.ToString(TimeManager.DateFormat));
                                Console.WriteLine(string.Format("    node: {0}, iter: {1}, diff: {2}, curr: {3}", n.number, iter, diff, n.mnInfo.watchNew));
                            }
                        }
                    } // has watch logic
                } // end of demList
            } // storage iteration OR no ownerships in data set
        }

        public static void ExchangeLimitLinksUpdateConvg(Model mi, int nmown, int iodd, int iter, ref bool convgWatch)
        // BEWARE ! iter is really newiter from operate
        {
            //ET: needs to keep the value from previous convergence check
            //convgWatch = true;

            if (iodd == 1 || nmown == 0)
            {
                DateTime dt = mi.TimeStepManager.Index2Date(mi.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);
                for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
                {
                    Link l = mi.mInfo.realLinkList[i];
                    if (l.m.exchangeLimitLinks != null)
                    {
                        l.mrlInfo.watchNew = l.m.exchangeLimitLinks.mlInfo.flow;

                        //don't test convergence until iter 11
                        if (iter < 10)
                        {
                            convgWatch = false;
                            continue;
                        }

                        //if flow is the same, continue
                        if (l.mlInfo.flow == l.mlInfo.flow0)
                        {
                            continue;
                        }

                        //if flow is greater than last iter exchangelimitlink flow,
                        //something bad is going on, else check convergence
                        if (l.mlInfo.flow > l.m.exchangeLimitLinks.mlInfo.flow0)
                        {
                            //this should never occur!!! because the hi bound on
                            //this link is set to the exchange limit link flow0
                            mi.FireOnError("ERROR: link flow greater than exchange limit link flow");
                            mi.FireOnError("    " + dt.ToString(TimeManager.DateFormat));
                            mi.FireOnError(string.Format("    link: {0}, iter: {1}, flow: {2}, flow limit: {3}", l.number, iter, l.mlInfo.flow, l.mrlInfo.watchNew));
                        }
                        else if (!Utils.NearlyEqual((double)l.mlInfo.flow, (double)l.mlInfo.flow0, mi.flowthru_cp))
                        {
                            convgWatch = false;

                            if (iter > 50)
                            {
                                mi.FireOnError("No convergence in exchange limit link:");
                                mi.FireOnError("    " + dt.ToString(TimeManager.DateFormat));
                                mi.FireOnError(string.Format("    link: {0}, iter: {1}, curr: {2}, prev: {3}", l.number, iter, l.mlInfo.flow, l.mlInfo.flow0));

                                Console.WriteLine("No convergence in exchange limit link:");
                                Console.WriteLine("    " + dt.ToString(TimeManager.DateFormat));
                                Console.WriteLine(string.Format("    link: {0}, iter: {1}, curr: {2}, prev: {3}", l.number, iter, l.mlInfo.flow, l.mlInfo.flow0));
                            }
                        }
                    } // has exchange limit link
                } // end of realLinkList
            } // storage iteration OR no ownerships in data set
        }

        /* -----------------------------------------------------------------------*
        During iodd = 1 we are in the storage step and we want to turn on these links.
                      Major differences will be noticed between various types
                      of links.  If this is an ownership link already, we
                      do not need to modify the upper bound.  If it is a variable
                      or seasonal capacity link, our upper bound comes from a
                      different place.
        During iodd = 0 we are in the natural flow step and we want to set
                      these links to zero max capacity.
        REMEMBER that these links coupled with flow through demands act
        to keep the water only in the storage step.
        \*----------------------------------------------------------------------- */
        public static void StorageFlowOnlyLinks(Model mi, int iodd)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];

                if (l.m.flagStorageStepOnly != 0)
                {
                    if (!l.mlInfo.isOwnerLink)
                    {
                        if (iodd == 0)
                        {
                            l.mlInfo.lo = 0;
                            l.mrlInfo.hibnd = l.mlInfo.hi;
                            l.mlInfo.hi = 0;
                            l.mlInfo.flow = 0;
                        }
                        if (iodd == 1 && mi.mInfo.Iteration > 0)
                        {
                            l.mlInfo.hi = l.mrlInfo.hibnd;
                        }
                    }
                }
            }
        }

        /* ExchangeDems:
         *
         * Exchange Credit Demand nodes watch the amount of water recieved by
         * another demand in the system and set demand equal to that flow.
         *
         * Warning:
         *  currently exchange demands do not understand water that is going
         *  to satisfy a demand that is not 100% consumptive or a flow through.
         */
        /* variables:
         *  n  -  This demand node
         *  l  -  Watched demand node demLink
         *  l2 -  Watched demand node demExcessLink
         *  i  -  counter variable
         */
        public static void ExchangeDems(Model mi)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                if (mi.mInfo.demList[i].m.jdstrm != null)
                {
                    Node n = mi.mInfo.demList[i];
                    Link l = n.m.jdstrm.mnInfo.demLink;
                    if (l != null)
                    {
                        n.mnInfo.demLink.mlInfo.hi = l.mlInfo.flow;

                        if (n.mnInfo.nodedemand.Length == 0)
                        {
                            int hs = 1;
                            if (mi.HydStateTables.Length > 0 && n.m.hydTable > 0)
                            {
                                hs = mi.HydStateTables[n.m.hydTable - 1].NumHydBounds + 1;
                            }
                            n.mnInfo.nodedemand = new long[mi.TimeStepManager.noModelTimeSteps, hs];
                        }
                        n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] = l.mlInfo.flow;
                    }
                }
            }
        }

        public static void ExchangeLimitLinksSetLinks(Model mi, int nmown, int iodd, int iter)
        {
            if ((iodd == 0 || nmown == 0) && iter > 0)
            {
                for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
                {
                    Link l = mi.mInfo.realLinkList[i];
                    if (l.m.exchangeLimitLinks != null)
                    {
                        l.mlInfo.hi = l.mrlInfo.watchNew;
                    }
                }
            }
        }

        public static void ExchangeLimitLinksSetDemands(Model mi, int nmown, int iodd, int iter)
        {
            if ((iodd == 0 || nmown == 0) && iter > 0)
            {
                for (int i = 0; i < mi.mInfo.demList.Length; i++)
                {
                    Node n = mi.mInfo.demList[i];
                    if (FlowThrough.HasWatchLogic(n) != 0)
                    {
                        n.mnInfo.demLink.mlInfo.hi = n.mnInfo.watchNew;

                        int hs = 1;
                        if (mi.HydStateTables.Length > 0 && n.m.hydTable > 0)
                        {
                            hs = mi.HydStateTables[n.m.hydTable - 1].NumHydBounds + 1;
                        }
                        if (n.mnInfo.nodedemand.Length == 0)
                        {
                            n.mnInfo.nodedemand = new long[mi.TimeStepManager.noModelTimeSteps, hs];
                        }
                        n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] = n.mnInfo.watchNew;
                        if (n.mnInfo.gwoutLink != null)
                        {
                            GlobalMembersGwater.GWCalculateDemandLinks(mi, n);
                        }
                    }
                }
            }
        }

        public static void SetDemandLinkHi(Model mi, int iter, bool realTimeIteration)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Link l = mi.mInfo.demList[i].mnInfo.demLink;
                Node n = l.from;
                l.mlInfo.hi = 0;
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    long demand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                    if (iter > 0 && n.m.pdstrm != null)
                    {
                        demand = Math.Max(0, demand - n.m.pdstrm.mlInfo.flow0);
                    }
                    l.mlInfo.hi = demand;
                }
                if (realTimeIteration) //In mi2 nets the minimum flow in the demand link is set to assure previous routed flow.
                {
                    l.mlInfo.lo = 0;
                }
            }
        }

    }
}
