using System;
using System.Text;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersOperate
    {
        public static Model mi1 = null;
        private static int NumOfFeasibilityFailures = 0;
        private static int MaxNumOfFeasibilityFailures = 10;
        private static int iodd;
        private static int nmown;
        private static int outputtimestepindex;

        /// <summary>The main subroutine in MODSIM that solves the system timestep by timestep using the relax algorithm.</summary>
        /// <param name="mi">The MODSIM model on which to iterate.</param>
        /// <param name="mi2">A copied MODSIM model used for backrouting</param>
        /// <returns>Returns 1 if an error occured; otherwise, returns 0.</returns>
        /// <remarks>The relax algorithm is solved during Step 31. Modsim.RunSolver preprocesses data for this routine and controls message pumping to clients.</remarks>
        public static int operate(Model mi, Model mi2)
        {
            int i;
            int MaxLags = 0;
            int[] hydstateindex = new int[mi.numHydTables * 12];
            int iter;
            int numoutputtimesteps;
            int newiter = 0;
            nmown = mi.mInfo.ownerList.Length;
            mi1 = mi;
            DateTime currentDate;
            DateTime fDate; // there are two dates? one for output?
            int retVal = 0;
            long maxd;
            long maxd1;
            long maxd2;
            long maxit; // Must be odd...
            long itot;
            Link l;
            long haveChannelLoss = 0;
            long haveRouting = 0;
            bool AfterRoutingNets = false; //flag to start calculating mi2 nets
            long RouteIterLeft = 1;
            bool exitRouting = true;
            bool realTimeIteration = true; //This flag indicates if the current iteration is a real time iteration.  When it's false indicates the downstream time values are being calculated.  Downstream time is used only for back-routing.
            long totalInfeasibleSum = 0;
            mi.mInfo.CurrentArrayIndex = 1;
            ResSystemList rsList = null;

            //MDB Ver 8 Output
            NetworkUtils.ModelOutputSupport myMODSIMOutput;
            NetworkUtils.ModelOutputSupport mi2MyMODSIMOutput = null;
            //Create a new instance of the ver8 output class
            myMODSIMOutput = new NetworkUtils.ModelOutputSupport(mi, false,false);
            //if (!myMODSIMOutput.outputReady) return retVal;
            mi1.OutputSupportClass = myMODSIMOutput;

            iter = 0;
            maxit = mi.maxit;
            if (maxit % 2 != 1 && nmown > 0)
            {
                maxit++;
            }
            mi.mInfo.convg = true;
            mi.mInfo.convgWatch = true;
            mi.mInfo.convgSTEND = true;
            mi.mInfo.convgFTHRU = true;
            mi.mInfo.convgHydro = true;
            haveChannelLoss = OperateInit.HasChannelLoss(mi);
            haveRouting = 0;
            if (mi.routingLinks.Count() > 0)
            {
                haveRouting = 1;
            }
            // If there is not routing in the network, ignore the user flag
            if (haveRouting == 0 && mi.backRouting)
            {
                mi.backRouting = false;
            }
            if (mi.backRouting)
            {
                mi.FireOnMessage("-> Triggering Back-Routing Logic");
                MaxLags = Routing.CalculateMaxLags(mi.storageAccountsWithBackRouting); //gets the maximum number of lags required
                RouteIterLeft = MaxLags;
            }
            outputtimestepindex = 0;
        L_126:
            itot = OperateInit.SumMaxVolume(mi);
            OperateInit.Init_stend(mi);
            OperateInit.Init_link_flow_lnktot(mi);
            if (nmown > 0)
            {
                //Build reservoir system list to pooling stglft and physical water for balance
                rsList = new ResSystemList(mi);
                GlobalMembersDistrib.ClearDemandReleases(mi); // sets flow,flow0,hi to zero for ownership links
                // also sets contribRent and contribLast for rental links (irent<0)
                // assumes contribLast is -prefstglft from input
            }
            // step 05 - set costs on groundwater pumping arcs according to user input
            /* RKL
            // I would like to suggest we create a new node between the demand and the artificial demand node
            //  The artificial demand link would have this new node as the from node; priority =0;
            //  The artificial demnd link would have upper bounds set at use[mon] and not change
            // new link between the demand and new node would have upper bounds of 99999999 and priority user set
            // for demands with infilitration (gwrtnLink !null) the cost should be = most negative evap link - link #
            // bounds for gwrtnLink is set in iteration sequence; note bounds on artificial link remains use[mon]
            // gwrtnLink would be shut off in NF step, open to last STG step flow of demand link * infiltration rate in STG step
            RKL */
            OperateInit.Set_gwrtnLink_cost(mi);
            if (mi.backRouting && mi == mi1)
            {
                //Initialize the back-routing network and leave it active in "mi"
                mi = mi2;
                goto L_126;
            }
            numoutputtimesteps = mi.timeStep.NumOfTSsForV7Output;
            currentDate = mi1.TimeStepManager.startingDate; // date user wants to start the simulation.
            mi1.mInfo.CurrentModelTimeStepIndex = 0;
            mi1.mInfo.Iteration = iter;
            mi1.mInfo.MonthIndex = outputtimestepindex;
            //Both mi1 and mi2 networks can be initialized in scripting
            mi1.FireInit();
            if (mi.hydro.IsActive)
            {
                mi.hydro.Updating += UpdateOutputs;
            }

            (mi1.OutputSupportClass as NetworkUtils.ModelOutputSupport).InitializeForOutput(mi);
            if (!myMODSIMOutput.outputReady)
            {
                mi1.FireOnError(string.Format("ERROR [OUTPUT]: Failed initializing output. Run canceled.", iter));
                return retVal;
            }

            if (mi.backRouting)
            {
                //This code needed for the first time that backrouting runs.
                mi2.mInfo.CurrentModelTimeStepIndex = 0;
                mi1.mInfo.CurrentModelTimeStepIndex = -1; //Initialization for first iteration. mi2 net is always run first
                //Initializes output for downstream time nets
                mi2MyMODSIMOutput = new NetworkUtils.ModelOutputSupport(mi2, true);
                mi2.mInfo.Iteration = iter;
                mi2.mInfo.MonthIndex = outputtimestepindex;
                mi1.FireBackRoutInit(mi2, Routing.RegRoutingCoef);
            }
            fDate = currentDate;

            // Set up cost summary information...
            CostSummary costs = new CostSummary(mi1, "costs.csv");
            CostSummary costs2 = new CostSummary(mi2, "costs_backrouting.csv");

            //
            //	TOP OF TIMESTEP LOOP
            //
            int numTimesteps = mi1.TimeStepManager.noModelTimeSteps;
            int pct = 0;
            for (int timestepindex = 0; timestepindex < numTimesteps; timestepindex++)
            {
                /* RKL we may need scripting at beginning of time step RKL */
                mi1.FireOnMessage(currentDate.ToString(TimeManager.DateFormat));

                /* To inform progress bar */
                int pctDone = ((timestepindex + 1) * 200 + numTimesteps) / (numTimesteps * 2);
                if (pctDone > pct)
                {
                    mi1.FireOnMessage("percent done " + pctDone);
                    pct = pctDone;
                }

                OperateInit.Init_current_evap(mi);
                /* RKL we should not be changing costs on these links RKL */
                //Set accrual links back to the user defined cost if needed
                OperateInit.Reset_accrualLinkList_cost(mi);

                //  ETS - Moved here to include the accrual month in the calculation.
                //          E.g., if accrual month is the first simulation timestep we don't want to do this
                //              at the end of the loop.
                // monacrul -- accrualMonth accrualmon monthaccrual monthAccrual
                /* reset accrual variable to paper storage left. */
                if (nmown > 0)
                {
                    if (GlobalMembersOperate.IsAccrualDate(mi, currentDate))
                    {
                        //Put back unused rent water and clear contribRent
                        GlobalMembersRent.PutBackUnusedRentStglft(mi);
                        // Shift any ContribLastThisSeason to ContribLast
                        GlobalMembersRent.ShiftContribLast(mi);
                        if (GlobalMembersOperate.IsBalanceDate(mi, currentDate))
                        {
                            mi.FirePreBalance();
                            rsList.BalanceAccrualDate();
                            mi.FirePostBalance();
                        }
                        // begin the next accrual season with accural = stglft
                        OperateInit.SetAccrual2Stglft(mi);
                    }
                }

            L_650:
                /* Executed only the first time that a new mon is going to be calculated */
                if (mi.backRouting)
                {
                    //Make sure update values in mi1 when backrouting is active
                    mi.mInfo.MonthIndex = outputtimestepindex;
                    if (realTimeIteration && !AfterRoutingNets) // Copy to future times
                    {
                        mi = mi2;
                        realTimeIteration = false;
                    }
                }
                OperateInit.ZeroNodeArrays(mi);
                //* ETS - is this needed for all nodes rather that only reservoirs?
                OperateInit.SetNode_start(mi);
                mi.mInfo.CurrentModelTimeStepIndex = timestepindex;
                mi.mInfo.CurrentBegOfPeriodDate = mi.TimeStepManager.Index2Date(timestepindex, TypeIndexes.ModelIndex);
                mi.mInfo.CurrentEndOfPeriodDate = mi.TimeStepManager.Index2EndDate(timestepindex, TypeIndexes.ModelIndex);
                mi.mInfo.MonthIndex = outputtimestepindex;
                iter = 0;
                mi.mInfo.Iteration = iter;
                newiter = 0;
                iodd = 0;
                /* RKL
                // TimeStepInit computes the hydrologic state index for all tables; then for each demand node
                // sets use and infiltration rate, for reservoirs set target, evap rate, and generating hours,
                RKL */
                GlobalMembersOperate.TimeStepInit(mi);
                FlowThrough.ZeroFlowThrus(mi); // zero natural flow and storage step flowthrus
                /* RKL this looks like it needs some work; we may be excluding non account reservoirs from having balance tables RKL */
                GlobalMembersRestargbalance.PrepReservoirTargetBalancingConstruct(mi); // set bounds on balanceLinks
                /* clear the flow values for the natural flow links */
                OperateInit.Zero_accrualLink_flow(mi);
                // Handle old modsim reservoir target costs
                /* RKL make SetResTargetCosts go away; don't set targetLink costs RKL */
                ResCalculator.SetResTargetCosts(mi);
                if (nmown > 0 && outputtimestepindex > 0)
                {
                    /* RKL reset undelivered storage water variables to zero RKL */
                    // looks like we should split DistribStorage into SetUpNFStep and SetUpSTGStep or something
                    //  At this location, DistribStorage is just doing some initialization stuff
                    GlobalMembersDistrib.DistribStorage(mi, iodd);
                    //Set minimum reservoir releases from previous downstream time networks.
                    if (mi1.storageAccountsWithBackRouting && !realTimeIteration && iodd != 0)
                    {
                        Routing.SetMinOutflowReservoirs(mi1, mi2);
                    }
                }
                /* RKL The demLink->lo should ALWAYS be zero; why is this needed RKL */
                OperateInit.Zero_demLink_lo(mi);
                // STEP 03 - set hi and lo bounds on links
                OperateInit.Set_realLink_lo_hi(mi);
                // step 14a - initialize total accumulated link (currently set for end of
                OperateInit.CheckSeasonCapDate(mi, mi1, currentDate);
                // step 15a - check total accumulated link flow versus total allowable accumulated link flow
                OperateInit.Check_hi_SeasonCap(mi);
                GlobalMembersEvap.EstimateStartingEvap(mi, 1);
                // We call the evap to increment inflow for negative evap??
                // we should maybe separate this functionality if this is correct
                GlobalMembersEvap.UpdateInflow(mi, iodd, nmown); // adjusts for negEvap, flowthru return and routreturn
                GlobalMembersOperate.SetDemandNodeDemand(mi);
                // step 21 - set bounds on initial storage arcs set bounds + flows on storage arcs
                mi.mInfo.stoToMassBal.mlInfo.lo = 0;
                if (nmown > 0)
                {
                    OperateInit.Zero_demout_fldflow(mi);
                    // shut storage ownership outflow links off - rkl
                    OperateInit.Zero_ownerLinks_hi(mi);
                    //RENT POOL
                    /* RKL need to be able to script before and after rentPool RKL */
                    if (GlobalMembersOperate.IsRentDate(mi, currentDate))
                    {
                        GlobalMembersRent.RentPool(mi);
                    }
                }
                maxd1 = 0;
                // step 26 - set up spill arcs and price according to order set limits on spill arcs
                ResCalculator.SetupSpillLinks(mi);
                // step 26a - initialize convergence flag to false used to cycle program one additional
                //            iteration each time step for use with channel routing option
                OperateInit.ChannelLossConvgFlag(mi);
                // step 27 - set bounds on mass balance arcs
                OperateInit.Zero_ideep(mi);
                OperateInit.ShiftStend(mi);
                // step 28 - initialize channel loss to zero at beginning of each iteration
                OperateInit.ZeroCloss(mi);
                // One would think this code is not needed but it is.
                OperateInit.ResetOwnerLinkCost(mi);
                FlowThrough.StorageFlowOnlyLinks2nditer(mi, false);
                
            L_750:
                /* RKL do we ever need scripting before this backRouting stuff move this block to a function IterationBackRoutTop RKL */
                // Allow scripting at the top of the iteration loop
                if (realTimeIteration)
                {
                    mi.FireIterTop();
                }
                else
                {
                    mi1.FireBackRoutIterTop(mi, Routing.RegRoutingCoef);
                }

                // Backrouting
                if (mi.backRouting)
                {
                    if (!realTimeIteration)
                    {
                        //Change time series and open reservoirs during the mi2 iteration depending
                        // on the max number of lags.
                        /*Calculate year and month of the network to be calculated
                        the year is taken as it is because the formulas inside
                        calculateDWSRouting were changed to iy-1 */
                        if (RouteIterLeft + 1 != 0) //mymon+MaxLags is redundant
                        {
                            if (iter == 0)
                            {
                                Routing.CalculateDWSRouting(mi1, mi2, (MaxLags - RouteIterLeft));
                                Routing.RestoreRoutedReturnFlows(mi1);
                                Routing.ReturnFlowLinksRoute(mi1, mi2, (MaxLags - RouteIterLeft));
                                Routing.zeroFlowDWSRoutingLinks(mi2);
                                //Updates Dws Time Inflows
                                GlobalMembersEvap.UpdateInflow(mi, iodd, nmown);
                                GlobalMembersRestargbalance.PrepReservoirTargetBalancingConstruct(mi); // set bounds on balanceLinks
                            }
                            if (iter == 3)
                            {
                                Routing.Set_realLink_lo_hi(mi1, mi2, (MaxLags - RouteIterLeft), mi2MyMODSIMOutput);
                            }
                        }
                        if (iter == 3)
                        {
                            Routing.setMinFlowDWSLinksBRouting(mi2, (MaxLags - RouteIterLeft), mi1, iodd);
                        }
                        else if (mi1.storageAccountsWithBackRouting && iter > 3)
                        {
                            Routing.setMinFlowDWSLinksBRouting(mi2, (MaxLags - RouteIterLeft), mi1, iodd);
                        }
                    }
                }

                // Set nodedemand[,] to meet specified power demands
                // Also, set hi for demand links ***we may want to make this routine more intelligent to skip flow thru, exchange, and watch demands.
                //  For now we just make sure we do all demands, then we reset the special ones
                //  another option is to force the scripts to set the demLink hi
                // If no ownerships in the network OR this is a NF iteration (including iter ==0),
                //  set all demand node demLink hi to zero (we should explicitly do this in a separate function)
                //  then set to nodedemand for current model timestepIndex and node hydstateIndex
                if (iodd == 0 || nmown == 0)
                {
                    OperateIter.SetDemandLinkHi(mi, iter, realTimeIteration);
                }
                if (iter > 0)
                {
                    if (nmown > 0)
                    {
                        // DistribStorage should be two functions and depending on whether we are in a NF or STG
                        //  iteration call the correct one; we should not have to pass iodd or nmown
                        GlobalMembersDistrib.DistribStorage(mi, iodd);
                        GlobalMembersDistrib.DistribNegEvap(mi);
                        //Set minimum reservoir releases from previous downstream time networks.
                        if (mi1.storageAccountsWithBackRouting && !realTimeIteration && iodd > 0)
                        {
                            Routing.SetMinOutflowReservoirs(mi1, mi2);
                        }
                        if (iodd == 1)
                        {
                            GlobalMembersDistrib.SetDemandReleases(mi);
                        }
                    }
                    if (iodd == 1 || nmown == 0)
                    {
                        OperateIter.SaveNFStepFlow(mi, outputtimestepindex);
                    }
                }
                //Route available info in the storage owner links for downstream time networks.
                //RKL what is iter here? mi or mi2 or does it matter?
                if (!realTimeIteration)
                {
                    if (nmown > 0 && mi1.storageAccountsWithBackRouting)
                    {
                        Routing.RouteStorageOwnShipLinkFlows(mi1, mi2, (MaxLags - RouteIterLeft), iter, iodd);
                    }
                }
                FlowThrough.ManageFlowThroughReturnLinks(mi, nmown, newiter % 2);
                GlobalMembersRestargbalance.UpdateReservoirTargetBalancingConstruct(mi, newiter);
                GlobalMembersRent.UpdateLastFillToOwners(mi, newiter);
                OperateInit.CalculateOwnerAccumsht(mi);
                OperateIter.ExchangeLimitLinksSetLinks(mi, nmown, newiter % 2, newiter);
                OperateIter.ExchangeLimitLinksSetDemands(mi, nmown, newiter % 2, newiter);
                mi.mInfo.convg = true;
                mi.mInfo.convgWatch = true;
                mi.mInfo.convgSTEND = true;
                mi.mInfo.convgFTHRU = true;
                mi.mInfo.convgHydro = true;
                if (!((nmown > 0 && iodd == 0) || iter == 0))
                {
                    ResCalculator.SetupSpillLinks(mi);
                }
                // Call before iter 0 solved, and on even iters  (between STG && NF step)
                // RKL this will overwrite (not increment) any demand set above with watch links so we should NOT ALLOW both to be set in the XY file code / interface
                if ((iodd == 0 || nmown == 0) && iter <= mi.mInfo.SMOOTHOPER)
                {
                    OperateIter.ExchangeDems(mi);
                }
                // If we have ownerships, this iteration is a STG step (or iter ==0)
                // SetTarget sets reservoir accrual, storage, outflow link bounds
                //* ETS this seems to be only for parent-child reservoirs - Is it used in other cases?
                if (nmown > 0 && (iodd == 1 || iter == 0))
                {
                    GlobalMembersSettarg.SetTarget(mi);
                }
                if (iter == 0 && nmown > 0)
                {
                    OperateIter.SetResOutLink_fldflow(mi);
                }
                /* RKL
                // Have Owerships, this iteration is a NF iteration; we are going to try to set accrual link bounds
                // based on cost and how much space there is; this should depend on relaxaccrual
                // we should once and for all consolidate code dealing with setting bounds and costs on accrual,
                // ownership / rental, reservoir outflow, target, excess, (all the links dealing with storage allocation)
                RKL */
                if (mi.relaxAccrual == 0 && nmown > 0 && (iodd == 0 || iter == 0))
                {
                    GlobalMembersOverride.SetNonChildRel0(mi);
                }
                maxd = 0;
                maxd1 = 0;
                maxd2 = 0;
                OperateIter.SetMassBalBoundsCost(mi);
                // step 29 - call groundwater subroutine to determine groundwater accruals
                //           and depletions for each iteration
                if (iter == 0)
                {
                    OperateIter.IterZeroSet_gwoutLink(mi);
                }
                //Groundwater calculation: done in both mi1 and mi2 after the mi2 infiltration rates and lag factors are transformed.
                if (nmown == 0 || iodd == 0)
                {
                    GlobalMembersGwater.gwater(mi, ref maxd2, ref mi.mInfo.convgw, mi1);
                }
                /* RKL again this is a NF iteration RKL */
                if (realTimeIteration && ((nmown == 0) || (iodd == 0)))
                {
                    OperateIter.Set_chanLossLink_hiNF(mi);
                }
                if (mi.backRouting)
                {
                    if (realTimeIteration && ((nmown == 0) || (iodd == 1)))
                    {
                        Routing.CopyAllResultstoUPSTime(mi);
                    }
                    if (!realTimeIteration && ((nmown == 0) || (iodd == 1)) && iter > 0)
                    {
                        Routing.HandleNegativeReturns(mi);
                    }
                }
                if (!mi.mInfo.convgw && (iter > mi.maxit - 10 || iter > mi.mInfo.GWSMOOTH - 10))
                {
                    mi1.FireOnError(string.Format("No convergence GW iter: {0}", iter));
                }
                if (nmown > 0 && iodd > 0)
                {
                    OperateIter.Set_chanLossLink_hiSTG(mi);
                }
                // step 30 - set upper bound on storage to mass balance arc according to
                //           upper bound on groundwater accrual arcs

                // step 31 - call network flow algorithm link with relax.for (relaxcall)
                // Assign costs to deep percolation arcs - give 1 unit benefit above demands
                OperateIter.Set_gwoutLink_cost(mi, ref maxd1);
                maxd = maxd1 + maxd2;
                mi.mInfo.demToMassBal.mlInfo.hi = maxd;
                mi.mInfo.demToMassBal.mlInfo.lo = 0;
                mi.mInfo.demToMassBal.mlInfo.cost = 0;
                OperateIter.Set_gwrtnLink_hi_lo_cost(mi);
                // Does this stuff get called if there are no ownerships in the network? Should it?
                if (iodd == 0)
                {
                    OperateIter.SetRes_costNFstep(mi);
                }
                //Set accrual limit on accrual links and last fill link
                if (iodd == 0)
                {
                    OperateIter.SetResAccrualNFstep(mi);
                }
                //Set cost on target storage link and open all accrual links to season capacity
                if (iodd == 1 && mi.relaxAccrual == 1)
                {
                    OperateIter.SetResCostSysnumRelaxAccrualSTGstep(mi);
                }
                /* RKL
                // we need something to replace this mess;  I shudder when I see lower bounds being set
                // we should have the model user set a reservoir level with a very high priority
                //  and consider getting rid of min_volume
                RKL */
                OperateIter.CheckTargetLinkHIltLO(mi);
                OperateIter.CheckLinksLOandHIbounds(mi);
                // Does this stuff get called if we have no ownerships in the network?
                //if (iodd == 1 && mi.relaxAccrual == 0)
                //{
                //    OperateIter.SetResCostNoSysnumNoRelaxAccrualSTGstep(mi);
                //}
                OperateIter.StorageFlowOnlyLinks(mi, iodd);
                //FlowThrough.SetSTGStepHiToNFFlow(mi, iodd);

                if (iodd == 0 && mi1.storageAccountsWithBackRouting && !realTimeIteration)
                {
                    if (iter > 0 && nmown > 0)
                    {
                        Routing.SetMinOutflowReservoirs(mi1, mi2);
                    }
                }
                //Reset the flows in ownership links with previous routing calculations calculations.
                if (realTimeIteration)
                {
                    if (nmown > 0 && mi1.storageAccountsWithBackRouting)
                    {
                        if (iter >= 4)
                        {
                            Routing.RestoreStorageOwnShipLinkFlows(mi, iodd);
                        }
                    }
                }
                //Handle pumping with storage accounts
                if (nmown > 0)
                {
                    GlobalMembersGwater.GWStorageStepOnly(mi, iodd);
                }

                // Close reservoir outflow if volume is in deadpool
                if (iter > 4)
                {
                    ResCalculator.DeadpoolOperation(mi);
                }

                //Trigger user define onIterBottom.
                if (realTimeIteration)
                {
                    mi.FireIterBottom();
                }
                else
                {
                    mi1.FireBackRoutIterBottom(mi, Routing.RegRoutingCoef);
                }

                // Call the Solver
                GlobalMembersRelax4.silent = false;
                if (GlobalMembersRelax4.relaxcallfortran(mi) != 0)
                {
                    retVal = 1;
                    if (!mi.backRouting)
                    {
                        mi.mInfo.ada_feasible = false;
                        //goto ErrRunningSolver;
                    }
                }

                if (realTimeIteration)
                {
                    costs.Write();
                    GlobalMembersArcdump.performDump("After relax, mi1", mi);
                    GlobalMembersArcdump.performArcDump("After relax, mi1", mi);
                }
                if (!realTimeIteration && mi1 != mi2)
                {
                    costs2.Write();
                    GlobalMembersArcdump.performDump("After relax, mi2", mi);
                    GlobalMembersArcdump.performArcDump("After relax, mi2", mi);
                }

                // With backrouting, there will be infeasiblity generally from iter 0 to 3 when solving mi2... After that infeasibility is fixed.
                totalInfeasibleSum = GlobalMembersArcdump.DumpInfeasNodes(mi);
                if (totalInfeasibleSum != 0 && iter == 0)
                {
                    mi1.FireOnError("A total infeasible sum of " + totalInfeasibleSum.ToString() + " occurred during the first iteration. Create arcdump.txt in the same directory as the model to see what is happening with each node.");
                }

                // Check feasibility that requires exiting.
                if (!mi.mInfo.ada_feasible)
                {

                    mi1.FireOnError("No feasible solution in the current period. Attempting fix.");
                    goto L_bye_bye;
                }

                // Hydropower controller update and convergence check
                if (mi.hydro.IterativeTechnique != IterativeSolutionTechnique.Nothing && realTimeIteration)
                {
                    mi.hydro.Update();
                }

                // Allow scripting before convergence and iteration increment
                mi.FirePreConvergenceCheck();

                // step 32 - increment iter
                iter++;
                mi.mInfo.Iteration = iter;

                /* Special Convergence - rerun solver */
                // This call handles flow through 3rd iteration if needed.
                if (mi.mInfo.hasFTOwners && (newiter > 0) && (newiter % 2 == 1))
                {
                    FlowThrough.StorageFlowOnlyLinks2nditer(mi, false);
                    // we are setting lo for any link that is flagStorageStepOnly; pretty dangerous
                    FlowThrough.SecondSTGStepSetflagStorageStepOnlyLO(mi);
                    FlowThrough.SecondSTGStepSetOwnerLinkLo(mi);
                    //SetDemandReleasesFlowThrough - child reservoirs
                    //set upperbounds of the flowThruSTGLink to last iteration flow
                    //WOW this is the only place this is called; it does a lot of work dealing
                    // with constraining the CHILD reservoirs if we have flow thru demands with ownerships
                    //Hopefully we can get rid of most of this if we get rid of supporting child reservoirs
                    GlobalMembersDistrib.SetDemandReleasesFlowThrough(mi);
                    FlowThrough.Set2ndSTGstepNFlinks2flow(mi);
                    GlobalMembersRelax4.relaxcallfortranincremental(mi); //Call Solver for second storage step
                    GlobalMembersArcdump.performDump("second stg step", mi);
                    GlobalMembersArcdump.performArcDump("second stg step", mi);
                    FlowThrough.StorageFlowOnlyLinks2nditer(mi, true);
                    FlowThrough.AfterSecondSTGStepResetflowThruSTGLink(mi);
                }
                /* Handle all flow through demands */
                FlowThrough.CalcFlowThroughsV2convg(mi, nmown, iodd, out mi.mInfo.convgFTHRU);
                FlowThrough.CalcFlowThroughsV2iter(mi, nmown, iodd, mi1);
                //FlowThrough.CalcFlowThroughsV2convg(mi, nmown, iodd, out mi.mInfo.convgFTHRU);
                // It would be VERY desirable to get these calls at the top of the iteration loop
                //   but we need to have the convergence check here
                // newiter%2 == 1 here means the last solver call was a STG step and we need to store a bunch of
                //  stuff that is used for watch logic
                //  sets watchNew which is later set to nodedemand
                OperateIter.WatchLinksUpdateConvg(mi, nmown, newiter % 2, newiter, out mi.mInfo.convgWatch);
                // Set l->mrlInfo->watchNew which later gets set to hi
                // Exchange link convergence flag operates on the results from watchlinks
                // TODO: We might want to implement a unique convergence flag for exchange limit links
                OperateIter.ExchangeLimitLinksUpdateConvg(mi, nmown, newiter % 2, newiter, ref  mi.mInfo.convgWatch);
                // set flow0 to flow - exchange limit and watch links
                OperateInit.ExchangeLimitLinksSaveData(mi, nmown, newiter % 2);
                // step 33a - readjust evaporation limits for account reservoirs
                GlobalMembersEvap.EstimateIterEvap(mi, iodd, nmown, out mi.mInfo.convgSTEND);
                if ((iodd == 1 || nmown == 0) && iter <= mi.mInfo.SMOOTHOPER)
                {
                    OperateInit.BypassCreditLinksSaveData(mi);
                }
                // For negative evap??
                // iodd here is still 1 if the last solver call was a STG step
                GlobalMembersEvap.UpdateInflow(mi, iodd, nmown);

                // step 34 - test for maximum no. of iterations
                // smooth any oscillations
                iodd = iter % 2;
                // NEW iodd
                // iter was incremneted right after the solver; so if the LAST iteration was a STG step,
                //   now iodd == 0 means the LAST solver call WAS a STG step and the NEXT iteration is NF
                // Stg step or no ownerships - do smoothing on smoothbound (constrain to min of this or last solution)
                if ((nmown > 0 && iodd == 0) || nmown == 0)
                {
                    if (iter > mi.mInfo.SMOOTHOPER)
                    {
                        OperateIter.CheckFlowThruSMOOTHBOUND(mi, nmown);
                    }
                }
                //increment newiter;
                if (realTimeIteration)
                {
                    newiter++;
                }

                // step 35 - test for convergence of groundwater and flow-thru demand and hydropower target component of modsim
                if (mi.hydro.IsActive && mi.hydro.IterativeTechnique != IterativeSolutionTechnique.Nothing)
                {
                    mi.mInfo.convgHydro = mi.hydro.IsConverged();
                }
                if (!mi.mInfo.convgw)
                {
                    mi.mInfo.convg = false;
                }
                if (mi.mInfo.convg)
                {
                    mi.mInfo.convg1 = true;
                }
                if (iter <= 6 && (!mi.mInfo.hasAccumsht || nmown != 0 || haveChannelLoss == 0 || haveRouting == 0))
                {
                    mi.mInfo.convg1 = false;
                }
                if (iter < 2)
                {
                    mi.mInfo.convg1 = false;
                }
                if (iter <= 14 && (haveChannelLoss != 0 || haveRouting != 0 || mi.mInfo.hasAccumsht) && nmown > 0)
                {
                    mi.mInfo.convg1 = false;
                }
                if ((haveChannelLoss != 0 || haveRouting != 0) && iter < 6)
                {
                    mi.mInfo.convg1 = false;
                }
                if ((nmown > 0) && (iter % 2 == 1))
                {
                    mi.mInfo.convg1 = false;
                }

                StringBuilder iterMSG = new StringBuilder();
                iterMSG.AppendLine("    " + currentDate.ToString(TimeManager.DateFormat));
                iterMSG.AppendLine("    convgw = " + mi.mInfo.convgw.ToString());
                iterMSG.AppendLine("    convgWatch = " + mi.mInfo.convgWatch.ToString());
                iterMSG.AppendLine("    convgFTHRU = " + mi.mInfo.convgFTHRU.ToString());
                iterMSG.AppendLine("    convgSTEND = " + mi.mInfo.convgSTEND.ToString());
                iterMSG.Append("    convgHydro = " + mi.mInfo.convgHydro.ToString());


                if (iter > maxit)
                {
                    if (mi.nomaxitmessage == 0)
                    {
                        iterMSG.Insert(0, "Ran into maximum iterations:" + Environment.NewLine);
                        mi1.FireOnError(iterMSG.ToString());
                        Console.WriteLine(iterMSG);
                    }
                    mi.mInfo.convg = mi.mInfo.convg1 = mi.mInfo.convgWatch = mi.mInfo.convgFTHRU = mi.mInfo.convgSTEND = mi.mInfo.convgw = mi.mInfo.convgHydro = true;
                }

                // Check convergence
                if (mi.mInfo.convgw && mi.mInfo.convg1 && mi.mInfo.convgWatch && mi.mInfo.convgFTHRU && mi.mInfo.convgSTEND && mi.mInfo.convgHydro)
                {
                    goto L_850;
                }
                /* RKL It would be nice to clean up the code so that iter++ is here and the bool IsNaturalFlowStep is set here then get rid of all the iodd and iter%2 confusion RKL */
                if (mi.maxit - mi.mInfo.Iteration < 10)
                {
                    Console.WriteLine(iterMSG);
                }
                goto L_750; // next iteration
            L_850: // converged
                if (!realTimeIteration) // when routing if the current routing month count converges then we store the results for the downstream links of all regions
                {
                    Routing.SaveDWSTimeFlowsInRoutingLinks(mi2, (MaxLags - RouteIterLeft), iodd, mi1, outputtimestepindex);
                }

                // Hydropower controller update and convergence check
                if (!mi.hydro.WillUpdateOutputs)
                {
                    GlobalMembersOperate.UpdateOutputs(mi);
                }
                if (realTimeIteration)
                {
                    mi.hydro.Update();
                }

                if (realTimeIteration)
                {
                    mi.FireConverged();    // scripting after convergence
                    iter = mi.mInfo.Iteration;  //update iter (internal) if iteration changed/reset in custom code.
                }
                else
                {
                    mi1.FireBackRoutConverged(mi, Routing.RegRoutingCoef);
                }
                if (!mi.mInfo.convg || !mi.mInfo.convg1 || !mi.mInfo.convgWatch || !mi.mInfo.convgHydro)
                {
                    goto L_750;
                }
                mi1.FireOnMessage(string.Format("             Last Iter: {0}", iter));

                // update return flow discharge file
                if (realTimeIteration)
                {
                    // derives the future lagged (routed) flows AFTER this time step
                    GlobalMembersGwater.RouteTimeSeries(mi);
                    // shift all routing flows for the next time step
                    GlobalMembersGwater.shiftLinkPrevflow(mi);
                }
                GlobalMembersGwater.shiftDemandPrevflow(mi);
                if (nmown > 0)
                {
                    // credit contribLast for any accrual
                    GlobalMembersRent.UpdateContribLast(mi);
                    // update own_accrual & stglft for current accrual, use, evap, channelloss
                    //  for all owner and rent links
                    GlobalMembersDistrib.UpdateAccrualStglft(mi, outputtimestepindex);
                    /* RKL
                    // it may be desirable to call a script in place of the built in balance routines
                    // be able to script before and after balance
                    RKL */
                    //if (GlobalMembersOperate.IsBalanceDate(mi, currentDate))
                    //{
                    // We have two balance routines: one for accrual date and not accrual date
                    //  this is because of rent pool complications
                    if (!GlobalMembersOperate.IsAccrualDate(mi, currentDate))
                    {
                        mi.FirePreBalance();
                        rsList.BalanceNonAccrualDate(currentDate);
                        mi.FirePostBalance();
                    }
                    //}
                    //// monacrul -- accrualMonth accrualmon monthaccrual monthAccrual
                    ///* reset accrual variable to paper storage left. */
                    //if (GlobalMembersOperate.IsAccrualDate(mi, currentDate))
                    //{
                    //    //Put back unused rent water and clear contribRent
                    //    GlobalMembersRent.PutBackUnusedRentStglft(mi);
                    //    // Shift any ContribLastThisSeason to ContribLast
                    //    GlobalMembersRent.ShiftContribLast(mi);
                    //    if (GlobalMembersOperate.IsBalanceDate(mi, currentDate))
                    //    {
                    //        mi.FirePreBalance();
                    //        rsList.BalanceAccrualDate();
                    //        mi.FirePostBalance();
                    //    }
                    //    // begin the next accrual season with accural = stglft
                    //    OperateInit.SetAccrual2Stglft(mi);
                    //}
                    // update accrual links lnktot, sum own_accrual and stglft for accural links
                    ResCalculator.SetAccrualLnkTotals(mi);
                    // set prevstglft, prevownacrual
                    GlobalMembersOperate.UpdatePrevstglft(mi);
                    // set link_store, link_accrual for owner and accrual links for output
                    GlobalMembersOperate.PrepareOwnerOutput(mi, outputtimestepindex);
                }
                // for non accrual links with lnkallow
                OperateInit.UpdateLnkTot(mi, outputtimestepindex);
                // This one is specific to initial conditions partials read in from flat files for each node
                GlobalMembersGwater.ShiftPartialFlows(mi);
                // Save monthly hydrologic state index for output routines.
                /* RKL another wired monthly variable that needs fixing; output each time step RKL */
                if (mi.HydStateTables.Length > 0)
                {
                    for (i = 0; i < mi.numHydTables; i++)
                    {
                        hydstateindex[i * 12 + outputtimestepindex] = mi.HydStateTables[i].StateLevelIndex + 1;
                    }
                }
                else
                {
                    hydstateindex[outputtimestepindex] = 0;
                }
                /* RKL scripting after accrual date checked; before "final routing" stuff RKL */
                if (mi.backRouting)
                {
                    /*If in the middle of the pre-routing calculation
                    don't increase the mon needs to go back to routing
                    internal month count*/
                    if (!realTimeIteration)
                    {
                        if (exitRouting) //Only the first time that is doing mi2s
                        {
                            RouteIterLeft = MaxLags;
                        }
                        AfterRoutingNets = false;
                        exitRouting = true;
                        //only solve routing networks for the next time step needed to
                        //provide the first flow in the first region.
                        //Output for backrouting calculation
                        mi2MyMODSIMOutput.AddLinksOutput(mi);
                        mi2MyMODSIMOutput.AddNodesOutput(mi);
                        mi2MyMODSIMOutput.AddHydropowerOutput(mi);
                        mi2MyMODSIMOutput.AddLinksMeasured(mi);

                        if (RouteIterLeft != 0) // if(mymon+MaxLags > mon)
                        {
                            RouteIterLeft--;
                            exitRouting = false;
                            if ((timestepindex % numoutputtimesteps + 1) == numoutputtimesteps)
                            {
                                outputtimestepindex = 0;
                            }
                            else
                            {
                                outputtimestepindex++;
                            }
                            timestepindex++;
                            iter = 0;
                        }
                        if (exitRouting)
                        {
                            //Clear current results for overwriting in next downstreamtime calculation.
                            mi2MyMODSIMOutput.outDS.ClearOutputTables();
                            mi = mi1;
                            realTimeIteration = true;
                            mi1.FireOnMessage("->End Routing Iteration");
                            //Return back to the main loop the values of month and year
                            outputtimestepindex = mi1.mInfo.MonthIndex;
                            timestepindex = mi1.mInfo.CurrentModelTimeStepIndex + 1;
                            //flag to calculate the final solution for mi1
                            AfterRoutingNets = true;
                            //Prepare demand and shortage arrays for next routed demand calculation
                            Routing.resetDemandShortagemi2(mi2);
                            Routing.resetMINdwsLFlows();
                            //Sets the flow in the storage owners to the value
                            //   that are entitled from previous time steps.
                            RouteIterLeft = MaxLags;
                        }
                        goto L_650;
                    }
                    Routing.SaveRoutedReturnFlows(mi1);
                    Routing.BackRoutingErrorCheck(mi1);
                    if (currentDate < mi.TimeStepManager.endingDate)
                    {
                        mi1.FireOnMessage("->Begin Routing Iteration");
                    }
                    AfterRoutingNets = false;
                } //#endif backrouting

                if (realTimeIteration)
                {
                    mi.FireConvergedFinal();
                }

                currentDate = mi.TimeStepManager.GetNextIniDate(currentDate);
                mi.mInfo.Iteration = iter;

                myMODSIMOutput.AddLinksOutput(mi);
                myMODSIMOutput.AddNodesOutput(mi);
                myMODSIMOutput.AddHydropowerOutput(mi);
                myMODSIMOutput.AddLinksMeasured(mi);

                // scripting after "final routing" stuff
                if ((timestepindex % numoutputtimesteps + 1) == numoutputtimesteps || timestepindex >= mi.TimeStepManager.noModelTimeSteps - 1)
                {
                    // end of "major" time step (week, quarter, year) output version 7 output and
                    // reset outputtimestepindex
                    if (OutputControlInfo.ver7OutputFiles)
                    {
                        int dataindex = mi.TimeStepManager.Date2Index(fDate, TypeIndexes.DataIndex);
                        int iy = (int)(dataindex / numoutputtimesteps) + 1;
                        GlobalMembersOutput.writeOUT2year(mi, iy, fDate, hydstateindex); // write hydrologic state indices
                        if (OutputControlInfo.acc_output)
                        {
                            GlobalMembersOutput.writeACC2year(mi, iy, fDate);    // write account information
                        }
                        if (OutputControlInfo.dem_output)
                        {
                            GlobalMembersOutput.writeDEM2year(mi, iy, fDate);    // write demand information
                        }
                        if (OutputControlInfo.gw_output)
                        {
                            GlobalMembersOutput.writeGW2year(mi, iy, fDate);    // write GW infiltration & return/pump
                        }
                        if (OutputControlInfo.flo_output)
                        {
                            GlobalMembersOutput.writeLNK2year(mi, iy, fDate);    // write link flows
                        }
                        if (OutputControlInfo.res_output)
                        {
                            GlobalMembersOutput.writeRES2year(mi, iy, fDate);    // write reservoir values
                        }
                    }
                    outputtimestepindex = 0; // reset for next "major" time step (week, quarter, year)
                    fDate = currentDate; // reset "first" date for output routine
                }
                else
                {
                    outputtimestepindex++;    //  not at the end of "major" time step yet
                }
                if (OutputControlInfo.ver8MSDBOutputFiles)
                {
                    if (mi.mInfo.CurrentArrayIndex == OutputControlInfo.noTimeStepsInMemory)
                    {
                        if (OutputControlInfo.ver8MSDBOutputFiles)
                        {
                            mi.FireOnMessage("writing mdb output");
                            myMODSIMOutput.FlushOutputToCSV(mi1, false);
                        }
                        //if (OutputControlInfo.SQLiteOutputFiles)
                        //{
                        //    myMODSIMOutput.FlushOutputToSQLite(mi1);
                        //}
                        mi.mInfo.CurrentArrayIndex = 0;
                    }
                }

                if (OutputControlInfo.SQLiteOutputFiles)
                {
                    if (myMODSIMOutput.FlushOutputToSQLite(mi1))
                        mi.mInfo.CurrentArrayIndex = 0;
                }

                mi.mInfo.CurrentArrayIndex++;

            } // end of new timestep loop
            // **********TIMESTEPS ENDED*************
            // **************************************

            /* RKL script at end of all time steps; run is done RKL */
            //Writes output to the XML file
            if (OutputControlInfo.ver8MSDBOutputFiles) myMODSIMOutput.FlushOutputToCSV(mi1, true);
            if (OutputControlInfo.SQLiteOutputFiles) myMODSIMOutput.FlushOutputToSQLite(mi1, true);

            costs.Close();
            costs2.Close();

            if (realTimeIteration)
            {
                mi.FireEnd();
            }
            else
            {
                mi1.FireBackRoutEnd(mi, Routing.RegRoutingCoef);
            }
            //  option to write out partial flows; return flows that are in memory for time steps after
            // the end of the model run
            if (OutputControlInfo.partial_flows)
            {
                GlobalMembersGwater.WritePartialFlowsADA(mi);
            }

            mi.FireOnMessage("Successful completion of program MODSIM.");
            if (!OutputControlInfo.ver8MSDBOutputFiles && !OutputControlInfo.SQLiteOutputFiles) mi.FireOnMessage("No output format is selected. Output not written.");
            return retVal;
        L_bye_bye:
            if (realTimeIteration)
            {
                Console.Write("mi1 is active\n");
            }
            else
            {
                Console.Write("mi2 is active\n");
            }
            GlobalMembersArcdump.performDump("Infeasible solution - Before spill added", mi);
            GlobalMembersArcdump.performArcDump("Infeasible solution - Before spill added", mi);
            totalInfeasibleSum = GlobalMembersArcdump.DumpInfeasNodes(mi, inMemory: true);
            if (totalInfeasibleSum != 0)
            {
                mi1.FireOnError("A total infeasibility of " + totalInfeasibleSum.ToString() + " was found before the spill was added. Create arcdump.txt in the same directory as the model to see what is happening with each node.");
            }
            GlobalMembersSetnet.addSpillTrackingLinks(mi);
            GlobalMembersRelax4.relaxcallfortran(mi);
            totalInfeasibleSum = GlobalMembersArcdump.DumpInfeasNodes(mi, inMemory: true);
            if (totalInfeasibleSum != 0)
            {
                mi1.FireOnError("A total infeasibility of " + totalInfeasibleSum.ToString() + " was found after the spill was added. Create arcdump.txt in the same directory as the model to see what is happening with each node.");
            }
            if (mi.mInfo.ada_feasible)
            {
                GlobalMembersArcdump.performDump("Fixed infeasibility - Look at the spill links and arcdump file", mi);
                GlobalMembersArcdump.performArcDump("Fixed infeasibility - Look at the spill links added to each node", mi);
                mi1.FireOnError("Fixed infeasibility - Look at the spill links added to each node");
                for (l = mi.firstLink; l != null; l = l.next)
                {
                    if (l.to != null && l.to == mi.mInfo.artMassN && l.mlInfo.cost == DefineConstants.COST_LARGER && l.mlInfo != null && l.mlInfo.flow > 0) // 288888888
                    {
                        mi1.FireOnError(string.Format("Excess water entering node {0}, {1} units", l.from.name, l.mlInfo.flow));
                    }
                    if (l.from != null && l.from == mi.mInfo.artMassN && l.mlInfo.cost == DefineConstants.COST_LARGER && l.mlInfo != null && l.mlInfo.flow > 0) // 288888888
                    {
                        mi1.FireOnError(string.Format("Excess water leaving node {0}, {1} units", l.to.name, l.mlInfo.flow));
                    }
                }
                if (mi1.infeasibleRestart > 0)
                {
                    mi1.FireOnError("WARNING: Water will likely be removed from the network to fix infeasibilities.");
                    if (NumOfFeasibilityFailures < MaxNumOfFeasibilityFailures)
                    {
                        NumOfFeasibilityFailures++; // toggles the infeasibility fixing mechanism so that this routine doesn't get caught in an infinite loop
                        mi1.FireOnMessage("\nRestarting solver with added spill links to improve feasibility.\n");
                        if (OutputControlInfo.ver8MSDBOutputFiles) myMODSIMOutput.FlushOutputToCSV(mi1, true);
                        if (OutputControlInfo.SQLiteOutputFiles) myMODSIMOutput.FlushOutputToSQLite(mi1, true);
                        return GlobalMembersOperate.operate(mi1, mi2);
                    }
                    else
                    {
                        NumOfFeasibilityFailures = 0; // toggles the infeasibility fixing mechanism so that this routine doesn't get caught in an infinite loop
                        mi1.FireOnMessage("\nThe maximum number of infeasibility failures (" + MaxNumOfFeasibilityFailures.ToString() + ") occurred. Restart the solver, and it will attempt to automatically fix the infeasibility... But it will likely fail.\n");
                    }
                }
                else
                {
                    mi1.FireOnError("No feasible solution in the current period. Exiting.");
                    goto ErrRunningSolver;
                }

            }
            else
            {
                GlobalMembersArcdump.performDump("Unfixed infeasibility - Unknown error", mi);
                GlobalMembersArcdump.performArcDump("Unfixed infeasibility - Unknown error", mi);
                mi1.FireOnError("Internal Modsim Error.  Too many Nodes or Links");
            }

        ErrRunningSolver:
            costs.Close();
            costs2.Close();

            //Write the last set of time steps beyond the last data flush
            if (OutputControlInfo.ver8MSDBOutputFiles) myMODSIMOutput.FlushOutputToCSV(mi1, true);
            if (OutputControlInfo.SQLiteOutputFiles) myMODSIMOutput.FlushOutputToSQLite(mi1, true);

            return retVal;
        }
        public static void SetHydTableIndex(Model mi)
        {
            double fracAmount;
            DateTime currentDate = mi.TimeStepManager.Index2Date(mi.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);
            if (mi.HydStateTables.Length == 0)
            {
                return;
            }
            for (int i = 0; i < mi.HydStateTables.Length; i++)
            {
                long tsubmx = 0;
                long wtrsys = 0;
                int numstates = mi.HydStateTables[i].NumHydBounds + 1;
                long hydDate = 0;
                if (mi.HydStateTables[i].hydDates.Count > 1)
                {
                    hydDate = mi.HydStateTables[i].HydTableDateIndex(currentDate);
                }
                for (int j = 0; j < mi.HydStateTables[i].NumReservoirs; j++)
                {
                    Node n = mi.HydStateTables[i].Reservoirs[j];
                    tsubmx += n.m.max_volume;
                    long nodeinflow = 0;
                    if (n.mnInfo.forecast.Length > 0)
                    {
                        nodeinflow = n.mnInfo.forecast[mi.mInfo.CurrentModelTimeStepIndex, 0];
                    }
                    wtrsys += n.mnInfo.start + nodeinflow;
                }
                if (tsubmx != 0)
                {
                    fracAmount = (double)wtrsys / (double)tsubmx;
                }
                else
                {
                    fracAmount = 0.0;
                    mi.HydStateTables[i].StateLevelIndex = 0;
                }
                if (fracAmount < mi.HydStateTables[i].hydBounds[0, hydDate]) // lowest state
                {
                    mi.HydStateTables[i].StateLevelIndex = 0;
                }
                else if (fracAmount > mi.HydStateTables[i].hydBounds[numstates - 2, hydDate]) // highest state
                {
                    mi.HydStateTables[i].StateLevelIndex = numstates - 1;
                }
                else
                {
                    for (int j = 0; j < numstates - 2; j++)
                    {
                        if (mi.HydStateTables[i].hydBounds[j, hydDate] < 0)
                        {
                            mi.HydStateTables[i].StateLevelIndex = 0;
                            break;
                        }
                        if (fracAmount > mi.HydStateTables[i].hydBounds[j, hydDate] && fracAmount <= mi.HydStateTables[i].hydBounds[j + 1, hydDate])
                        {
                            mi.HydStateTables[i].StateLevelIndex = j + 1;
                            break;
                        }
                    }
                }
            }
        }
        public static void SetOwnerLinksHydStateIndex(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                if (l.m.hydTable == 0)
                {
                    l.mrlInfo.hydStateIndex = 0;
                }
                else
                {
                    if (l.mrlInfo != null)
                    {
                        l.mrlInfo.hydStateIndex = 0;
                        if (mi.HydStateTables.Length >= l.m.hydTable)
                        {
                            l.mrlInfo.hydStateIndex = mi.HydStateTables[l.m.hydTable - 1].StateLevelIndex;
                        }
                    }
                }
            }
        }
        public static void SetDemandNodeHydStateIndex(Model mi)
        {
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                GlobalMembersOperate.SetNodeHydStateIndex(mi, n);
            }
        }
        public static void SetResNodeHydStateIndex(Model mi)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                GlobalMembersOperate.SetNodeHydStateIndex(mi, n);
            }
        }
        // called at the beginning of each time step
        public static void TimeStepInit(Model mi)
        {
            GlobalMembersOperate.SetHydTableIndex(mi); // foreach hydrologic state table, derive the StateLevelIndex
            GlobalMembersOperate.SetOwnerLinksHydStateIndex(mi); // foreach ownership link, set hydStateIndex
            GlobalMembersOperate.SetDemandNodeHydStateIndex(mi); // foreach demand node set hydStateIndex
            GlobalMembersOperate.SetResNodeHydStateIndex(mi); // foreach reservoir node set hydStateIndex
        }
        public static void SetNodeHydStateIndex(Model mi, Node n)
        {
            n.mnInfo.hydStateIndex = 0;
            if (n.m.hydTable > 0 && mi.HydStateTables.Length >= n.m.hydTable)
            {
                n.mnInfo.hydStateIndex = mi.HydStateTables[n.m.hydTable - 1].StateLevelIndex;
            }
        }
        /// <summary>Sets <c>node.mnInfo.nuse</c> to the original node demand at the specific timestep (before any code changes it).</summary>
        public static void SetDemandNodeDemand(Model mi)
        {
            int tsindex = mi.mInfo.CurrentModelTimeStepIndex;
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    n.mnInfo.nuse = n.mnInfo.nodedemand[tsindex, n.mnInfo.hydStateIndex];
                }
                else
                {
                    n.mnInfo.nuse = 0;
                }
            }
        }
        public static void UpdateOutputs(object sender, EventArgs e)
        {
            UpdateOutputs(mi1);
        }
        /// <summary>Updates hydropower related information. Should be updated before hydropower calculations take place.</summary>
        public static void UpdateOutputs(Model mi)
        {
            // derive the future lagged infiltration and/or depletions
            GlobalMembersGwater.setDemandPrevflow(mi);
            // derive the future lagged routing/channel loss return flows
            GlobalMembersGwater.setLinkPrevflow(mi);
            // step 36 - build shortage array - shortage is defined as demand minus delivery
            GlobalMembersOutput.BuildShortageArray(mi);
            // step 38 - calculate final res storage and set monthly evap estimate
            // step 39 - calculate monthly evap. and determine res. ending storage
            GlobalMembersEvap.EstimateIterEvap(mi, iodd, nmown, out mi.mInfo.convgSTEND);
            // simply zero link_store and link_accrual before preparing output
            GlobalMembersOperate.ZeroLinkStoreAccrualArrays(mi, outputtimestepindex);
            // setup link output-sets link_flow to flow
            //  then add channel loss and flothru if "online"
            GlobalMembersMss.CalcLinkFlows(mi, outputtimestepindex);
            GlobalMembersOperate.SummarizeReservoirOutput(mi, outputtimestepindex);
            GlobalMembersOperate.SummarizeNodeOutputArrays(mi, outputtimestepindex);
        }
        public static void PrepareOwnerOutput(Model mi, int outputtimestepindex)
        {
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                l.mrlInfo.link_store[outputtimestepindex] = l.SumStglft();
                l.mrlInfo.link_accrual[outputtimestepindex] = l.SumOwnAccrual();
                if (l.mrlInfo.link_store[outputtimestepindex] > l.mrlInfo.lnkSeasStorageCap)
                {
                    mi.FireOnMessage(string.Concat(" AccrualLink ", Convert.ToString(l.name), " link_store ", Convert.ToString(l.mrlInfo.link_store[outputtimestepindex]), " link_accrual ", Convert.ToString(l.mrlInfo.link_accrual[outputtimestepindex]), " lnkSeasStorageCap ", Convert.ToString(l.mrlInfo.lnkSeasStorageCap)));
                }
            }
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                l.mrlInfo.link_store[outputtimestepindex] = l.mrlInfo.stglft;
                l.mrlInfo.link_accrual[outputtimestepindex] = l.mrlInfo.own_accrual;
            }
        }
        public static bool IsBalanceDate(Model mi, DateTime currentDate)
        {
            for (int i = 0; i < mi.accBalanceDates.Count; i++)
            {
                if (mi.timeStep.TSType == ModsimTimeStepType.Monthly && mi.accBalanceDates.Item(i).Month == currentDate.Month)
                {
                    return true;
                }
                else if (mi.timeStep.TSType != ModsimTimeStepType.Monthly && mi.accBalanceDates.Item(i).Month == currentDate.Month && mi.accBalanceDates.Item(i).Day == currentDate.Day && currentDate.Millisecond == 0) // the interface only allows months.. why is this here? kt.
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsAccrualDate(Model mi, DateTime currentDate)
        {
            if (mi.accrualDate != TimeManager.missingDate //accrualDate is set
                //Monthly time step
                    && ((mi.timeStep.TSType == ModsimTimeStepType.Monthly && mi.accrualDate.Month == currentDate.Month) // Only the Month counts
                // Not monthly time step
                        || ((mi.timeStep.TSType != ModsimTimeStepType.Monthly) && mi.accrualDate.Month == currentDate.Month && mi.accrualDate.Day == currentDate.Day && currentDate.Millisecond == 0))) // Day and Month count
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void ZeroLinkStoreAccrualArrays(Model mi, int outputtimestepindex)
        {
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                //WARNING this does not initalize the group ownership links; they are artificial
                Link l = mi.mInfo.realLinkList[i];
                l.mrlInfo.link_store[outputtimestepindex] = 0;
                l.mrlInfo.link_accrual[outputtimestepindex] = 0;
            }
        }
        public static bool IsRentDate(Model mi, DateTime currentDate)
        {
            for (int i = 0; i < mi.rentPoolDates.Count; i++)
            {
                if (mi.timeStep.TSType == ModsimTimeStepType.Monthly && mi.rentPoolDates.Item(i).Month == currentDate.Month)
                {
                    return true;
                }
                else if (mi.timeStep.TSType != ModsimTimeStepType.Monthly && mi.rentPoolDates.Item(i).Month == currentDate.Month && mi.rentPoolDates.Item(i).Day == currentDate.Day && currentDate.Millisecond == 0) //Does rent pool work in daily?
                {
                    return true;
                }
            }
            return false;
        }
        public static void SummarizeReservoirOutput(Model mi, int outputtimestepindex)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                n.mnInfo.iseepr[outputtimestepindex] = n.mnInfo.iseep0;
                Link l = n.mnInfo.spillLink;
                if (l != null)
                {
                    l.from.mnInfo.res_spill[outputtimestepindex] = l.mlInfo.flow;
                }
                long nodeinflow = 0;
                if (n.mnInfo.inflow.Length > 0)
                {
                    nodeinflow = n.mnInfo.inflow[mi.mInfo.CurrentModelTimeStepIndex, 0];
                }
                n.mnInfo.start_storage[mi.mInfo.CurrentModelTimeStepIndex] = n.mnInfo.start;
                n.mnInfo.unreg_inflow[outputtimestepindex] = nodeinflow;
                n.mnInfo.reservoir_evaporation[outputtimestepindex] = n.mnInfo.evpt;
                n.mnInfo.end_storage[mi.mInfo.CurrentModelTimeStepIndex] = n.mnInfo.stend;
                long target = 0;
                if (n.mnInfo.targetcontent.Length > 0)
                {
                    target = n.mnInfo.targetcontent[mi.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                }
                n.mnInfo.trg_storage[outputtimestepindex] = target;
                // step 41 - determine groundwater depletions and accruals
                // we do all this stuff with groundwater below as well on all real nodes
                // do we even want to do this with reservoirs???
                //  maybe we should have a new link for seepage to distinguish it from demands
                l = n.mnInfo.gwrtnLink;
                if (l != null)
                {
                    n.mnInfo.gw_to_node[outputtimestepindex] = l.mlInfo.flow;
                }
                else
                {
                    n.mnInfo.gw_to_node[outputtimestepindex] = 0;
                }

                l = n.mnInfo.gwoutLink;
                if (l != null)
                {
                    n.mnInfo.node_to_gw[outputtimestepindex] = l.mlInfo.flow;
                }
                else
                {
                    n.mnInfo.node_to_gw[outputtimestepindex] = 0;
                }
                // step 42 - set up upstream and downstream flows
                ResCalculator.SetupStreamFlows(n, outputtimestepindex);

                // step 43 - compute mean hydropower (kw) at each site where eff > zero
                ResCalculator.CalcMeanHydroPower(mi, n, outputtimestepindex);
                n.mnInfo.avg_head[outputtimestepindex] = n.mnInfo.head;
            }
        }
        public static void SummarizeNodeOutputArrays(Model mi, int outputtimestepindex)
        {
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                // step 42b - set up upstream and downstream flows
                //Why is SetupStreamFlows being fired for non reservoir nodes; it already WAS fired ten lines up for reservoirs
                ResCalculator.SetupStreamFlows(n, outputtimestepindex);
                n.mnInfo.irtnflowthruNF_OUT[outputtimestepindex] = n.mnInfo.irtnflowthruNF; // used in output
                long nodeinflow = 0;
                if (n.mnInfo.inflow.Length > 0)
                {
                    nodeinflow = n.mnInfo.inflow[mi.mInfo.CurrentModelTimeStepIndex, 0];
                }
                // write out unregulated inflow + flow through demand bypass credit flow
                // This seems flakey --- Marc
                if (n.m.pdstrm != null)
                {
                    n.mnInfo.unreg_inflow[outputtimestepindex] = nodeinflow + n.m.pdstrm.mlInfo.flow;
                }
                else
                {
                    n.mnInfo.unreg_inflow[outputtimestepindex] = nodeinflow;
                }
                long nodedemand = 0;
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    int hydstate = n.mnInfo.hydStateIndex;
                    nodedemand = n.mnInfo.nodedemand[mi.mInfo.CurrentModelTimeStepIndex, hydstate];

                    n.mnInfo.demand[mi.mInfo.CurrentModelTimeStepIndex] = nodedemand; // output variable - this is used in routing code as well
                    n.mnInfo.demand_shortage[outputtimestepindex] = n.mnInfo.ishtm[mi.mInfo.CurrentModelTimeStepIndex];
                }
                // step 45 - determine groundwater depletions and accruals
                if (n.mnInfo.gwrtnLink != null)
                {
                    n.mnInfo.gw_to_node[outputtimestepindex] = n.mnInfo.gwrtnLink.mlInfo.flow;
                }
                else
                {
                    n.mnInfo.gw_to_node[outputtimestepindex] = 0;
                }

                if (n.mnInfo.gwoutLink != null)
                {
                    n.mnInfo.node_to_gw[outputtimestepindex] = n.mnInfo.gwoutLink.mlInfo.flow;
                }
                else
                {
                    n.mnInfo.node_to_gw[outputtimestepindex] = 0;
                }
            }
        }
        public static void UpdatePrevstglft(Model mi)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                if (l.m.groupNumber == 0)
                {
                    l.mrlInfo.prevstglft = l.mrlInfo.stglft;
                    l.mrlInfo.prevownacrul = l.mrlInfo.own_accrual;
                }
            }
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                l.mrlInfo.prevstglft = l.mrlInfo.stglft;
                l.mrlInfo.prevownacrul = l.mrlInfo.own_accrual;
            }
        }
    }
}
