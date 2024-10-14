using System.Threading;
using System.Globalization;

namespace Csu.Modsim.ModsimModel
{

    public static class GlobalMembersModsim
    {
        public static void ModelStatusMsg(string str)
        {
            Model.FireOnMessageGlobal(str);
        }
        private delegate void fnDelegate(string str, bool msgType);
    }

    public static class Modsim
    {
        /*     ******************************************************************
         *
         *     PROGRAM MODSIM
         *
         *     ******************************************************************
         *
         *         PROGRAM  MODSIM    --  RIVER BASIN SIMULATION PACKAGE
         *         ORIGINAL DEVELOPMENT --  CARLOS PUENTES
         *         SYSTEMS ENGINEERING DIVISION, TEXAS WATER DEVELOPMENT BOARD
         *         MARCH 1972
         *         MODIFICATIONS   --  JOHN M. SHAFER
         *         COLORADO STATE UNIVERSITY
         *         1978-1980
         *         MICROCOMPUTER VERSION: N-F Chou and J.W. Labadie
         *         MODSIM  V 4.10      IBM PC XT/AT   cSu       Mar. 1988
         *         Modified JWF: Nov 1990 - Relax option added
         *                       May 1991 - Output xxx.PRN file added
         *                       Jun 1991 - Groundwater option modified
         *                                     inc User-defined GW reservoir
         *                       Aug 1991 - Max link accumulation option added
         *                       Sep 1991 - Optional command entry thru
         *                                     batch file
         *                       Nov 1991 - Routing option added
         *                                - Graphics Removed
         *                                - Plot removed
         *                                - Clear Screen subroutine added
         *                       Dec 1991 - Variable dimensioning thru
         *                                  Parameter file
         *                                - Common Reservoir for account
         *                                  purposes
         *                                - non-sequential reservoir numbering
         *                       Jan 1992 - Interactive calculation of Glover
         *                                  groundwater and Muskingum channel
         *                                  routing coefficients w metric and
         *                                  variable time step
         *                                - selective node,link,& GW output
         *
         *
         *                      1. Stream-Aquifer Interactions
         *                      2. Conjunctive Use of SW & GW
         *                      3. Hydropower--Variable Efficiency
         *                      4. Flow-Through Demand For Instream Uses
         *                      5. Channel Losses Calculated
         *                      6. Time increments: Monthly, Weekly, Daily
         *
         *      modsim5.0        1994     - storage allocation added (Roger Larson)
         *                                - watch links / demands (Marc Baldo, JWL)
         *               Jan 10-14, 1995  - model translation to C, using interface
         *                                  .xy library, Marc Baldo, Marc Seter
         *               Feb-Oct, 1995    - recoding storage distribution routines,
         *                                  cleaning up, redesigning, Marc Baldo
                          2004            - .NET
         *
         *     *****************************************************************
         */
        public static Model mi1
        {
            get
            {
                return GlobalMembersOperate.mi1;
            }
        }
        public static Model mi2 = null;
        public static int RunSolver(Model mi, bool backRouting)
        {
            mi.backRouting = backRouting;
            return RunSolver(mi);
        }
        public static int RunSolver(Model mi)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            ModsimTimer runDur = new ModsimTimer();
            int haveRouting;
            bool errorRunning = false;
            string msg;
            mi2 = null;
            mi.FireOnMessage("PROGRAM MODSIM -- RIVER BASIN SIMULATION PACKAGE");
            mi.FireOnMessage("Initializing model structures...");

            // Reading partial flows
            if (OutputControlInfo.partial_flows)
            {
                GlobalMembersGwater.ReadPartialFlowsADA(mi);
            }

            // Convert units and fill all the TimeSeries first
            mi.FirePreConvertUnits();
            if (mi.ConvertUnitsAndFillTimeSeries)
            {
                mi.ConvertAndFill(false);
            }
            else if ((msg = mi.ConvertAndFill(true)) != "")
            {
                mi.FireOnError("\nMODSIM cannot solve the network until the data is filled and converted... Exiting.\n" + msg);
                return 1;
            }
            mi.FirePostConvertUnits();

            mi.FireOnMessage("Backrouting flag: " + mi.backRouting.ToString());
            if (OutputControlInfo.ver7OutputFiles)
            {
                GlobalMembersOutput.outputInit(mi);
            }
            GlobalMembersConstraint.sortstrInit();
            GlobalMembersConstraint.diststrInit();
            // Check for routing links
            haveRouting = 0;
            Routing.CreateListRoutingLinks(mi);
            if (mi.routingLinks.Count() > 0)
            {
                haveRouting = 1;
            }
            // Make duplicate downstream networks if necessary
            if (haveRouting != 0 && mi.backRouting)
            {
                Routing.AddRoutingLinksStructure(mi);
                mi.mInfo = new MinfoStr();
                //Duplicate the network
                //GlobalMembersSetnet.DupNetwork(mi, mi2);
                mi2 = mi.Clone();
                mi2.name = "Back-Routing Network";
                mi2.mInfo = new MinfoStr();
                mi2.fname = NetworkUtils.ModelOutputSupport.BaseNameString(mi.fname) + "BR.xy";
                /* RKL We should allow to script before and after setnet RKL */
                GlobalMembersSetnet.setnet(mi2);
                mi2.NetworkIsSetForRuntime = true;
            }
            else
            {
                if (haveRouting != 0) //Physical routing only
                {
                    Routing.AddRoutingLinksStructure(mi);
                }
                mi.mInfo = new MinfoStr();
            }
            /* set up the network artificial links */
            if (GlobalMembersSetnet.setnet(mi))
            {
                GlobalMembersModsim.ModelStatusMsg("Encountered error - exiting...");
                return 1;
            }
            mi.NetworkIsSetForRuntime = true;

            // Call glover - Note that this can create lag arrays.
            if (mi.useLags == 0)
            {
                GlobalMembersGlover.glover(mi);
                //ET: Is this valid for groundwater using model generated lags?
                //TODO: Does back-routing work with muskingum?
                if (haveRouting != 0 && mi.backRouting)
                {
                    GlobalMembersGlover.glover(mi2);
                }
            }
            /* prepare the network for running through the model. */
            GlobalMembersSetnet.prepnet(mi);
            if (haveRouting != 0 && mi.backRouting)
            {
                GlobalMembersSetnet.prepnet(mi2);
            }
            //if(haveRouting) prepnet(mi3);

            /* RKL allow script before and after operate; what is appropriate and not appropriate? RKL */
            //Set the flag to calculate storage water in future times if using back-routing and there are storage owners.
            if (mi.backRouting && mi.mInfo.ownerList.Length > 0)
            {
                mi.storageAccountsWithBackRouting = true;
            }
            int MaxLags;
            if (haveRouting != 0 && mi.backRouting)
            {
                //Split the network into regions based on the routing links.  All nodes in these regions will have the same calculation time
                Routing.SplitNetwork(mi);
                //Block the flow downstrea of the routing link to avoid bypassing flow - uses high possitive cost.
                Routing.BlockRoutingRegionFlow(mi);
                //Inizialize dynamic arrays to store future storage flow in the ownership links
                if (mi.storageAccountsWithBackRouting)
                {
                    Routing.InitializeStorageWaterFromPreviousTSArrays(mi);
                }
                MaxLags = Routing.CalculateMaxLags(mi.storageAccountsWithBackRouting); //gets the maximum number of lags required
                                                                                       //This call populate "noBackRAdditionalTSteps" and expand the time series tables
                mi.TimeStepManager.ExtendTSTable(mi.timeStep, MaxLags);
            }

            // Load the TimeSeries data
            ModsimTimer lts = new ModsimTimer();
            mi.FirePreTimeSeriesLoad();
            mi.LoadArraysFromTimeSeries();
            if (mi.hydro.IsActive)
            {
                mi.hydro.LoadTimeseriesData();
                mi.hydro.FitReservoirPolynomials();
            }
            mi.FirePostTimeSeriesLoad();
            lts.Report("Done loading TimeSeries arrays");

            // Solve the network
            GlobalMembersModsim.ModelStatusMsg("Solving network...");
            if (haveRouting != 0 && mi.backRouting)
            {
                // extend timeseries and initialize the back-routing timeseries.
                mi2.FireBackRoutPreTimeSeriesLoad(mi);
                Routing.extendTimeSeries(mi, mi2);
                mi2.InitBackRoutArraysFromTimeSeries(mi);
                if (mi2.hydro.IsActive)
                {
                    mi2.hydro.extendTimeSeries();
                    mi2.hydro.InitBackRoutTimeseriesData(mi);
                }
                mi2.FireBackRoutPostTimeSeriesLoad(mi);

                if (GlobalMembersOperate.operate(mi, mi2) != 0)
                {
                    errorRunning = true;
                }
            }
            else
            {
                if (GlobalMembersOperate.operate(mi, mi) != 0)
                {
                    errorRunning = true;
                }
            }
            GlobalMembersArcdump.closeDumpFile();
            GlobalMembersArcdump.closeArcDump();
            GlobalMembersModsim.ModelStatusMsg("Releasing model resources...");
            GlobalMembersOutput.outputFree();
            mi.FireOnMessage(runDur.GetReport("Done"));
            GlobalMembersModsim.ModelStatusMsg("Done");
            if (errorRunning)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

    }
}
