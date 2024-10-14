using System;
using System.Collections.Generic;
using Csu.Modsim.NetworkUtils;

namespace Csu.Modsim.ModsimModel
{
    public class Routing
    {
        public static int maxRegionID; //maxRegionID keeps track of the number of regions

        public static void resetDemandShortagemi2(Model mi2)
        {
            for (int i = 0; i < mi2.mInfo.realNodesList.Length; i++)
            {
                Node n2 = mi2.mInfo.realNodesList[i];
                if (n2.nodeType == NodeType.Demand || n2.nodeType == NodeType.Sink)
                {
                    for (long j = 0; j < n2.mnInfo.ishtm.Length; j++)
                    {
                        n2.mnInfo.demand[j] = 0;
                        n2.mnInfo.ishtm[j] = 0;
                    }
                }
            }
        }

        /// <summary>Extends the timeseries of mi2 with the information from mi1.</summary>
        public static void extendTimeSeries(Model mi1, Model mi2)
        {
            if (mi1.TimeStepManager.noBackRAdditionalTSteps > 0)
            {
                mi2.TimeStepManager.timeStepsList = mi1.TimeStepManager.timeStepsList;
                // Uses the last timestep value from the dates to be modeled...
                for (int i = 0; i < mi1.mInfo.realNodesList.Length; i++)
                {
                    Node n = mi1.mInfo.realNodesList[i];
                    int lastTimeStep = mi1.TimeStepManager.noModelTimeSteps;
                    //int lastTimeStep = mi1.TimeStepManager.Date2Index(mi1.TimeStepManager.endingDate, TypeIndexes.ModelIndex);
                    for (int j = lastTimeStep; j < (lastTimeStep + mi1.TimeStepManager.noBackRAdditionalTSteps); j++)
                    {
                        DateTime date = mi1.TimeStepManager.Index2Date(j, TypeIndexes.ModelIndex);
                        //Check if there is a valid date for the index.
                        //In the case of backrouting running beyond the available data, this date could not exist in the time step manager.
                        if (date == TimeManager.missingDate)
                        {
                            switch (n.nodeType)
                            {
                                case ModsimModel.NodeType.Reservoir:
                                    if (n.mnInfo.targetcontent.Length > 0)
                                    {
                                        for (int numhs = 0; numhs < n.mnInfo.targetcontent.GetLength(1); numhs++)
                                        {
                                            n.mnInfo.targetcontent[j, numhs] = n.mnInfo.targetcontent[lastTimeStep - 1, numhs];
                                        }
                                    }
                                    if (n.mnInfo.evaporationrate.Length > 0)
                                    {
                                        for (int numhs = 0; numhs < n.mnInfo.evaporationrate.GetLength(1); numhs++)
                                        {
                                            n.mnInfo.evaporationrate[j, numhs] = n.mnInfo.evaporationrate[lastTimeStep - 1, numhs];
                                        }
                                    }
                                    if (n.mnInfo.generatinghours.Length > 0)
                                    {
                                        for (int numhs = 0; numhs < n.mnInfo.generatinghours.GetLength(1); numhs++)
                                        {
                                            n.mnInfo.generatinghours[j, numhs] = n.mnInfo.generatinghours[lastTimeStep - 1, numhs];
                                        }
                                    }
                                    if (n.mnInfo.inflow.Length > 0)
                                    {
                                        for (int numhs = 0; numhs < n.mnInfo.inflow.GetLength(1); numhs++)
                                        {
                                            n.mnInfo.inflow[j, numhs] = n.mnInfo.inflow[lastTimeStep - 1, numhs];
                                        }
                                    }
                                    break;
                                case ModsimModel.NodeType.NonStorage:
                                    if (n.mnInfo.inflow.Length > 0)
                                    {
                                        for (int numhs = 0; numhs < n.mnInfo.inflow.GetLength(1); numhs++)
                                        {
                                            n.mnInfo.inflow[j, numhs] = n.mnInfo.inflow[lastTimeStep - 1, numhs];
                                        }
                                    }
                                    if (n.mnInfo.forecast.Length > 0)
                                    {
                                        for (int numhs = 0; numhs < n.mnInfo.forecast.GetLength(1); numhs++)
                                        {
                                            n.mnInfo.forecast[j, numhs] = n.mnInfo.forecast[lastTimeStep - 1, numhs];
                                        }
                                    }
                                    break;
                                case ModsimModel.NodeType.Demand:
                                    if (n.mnInfo.infiltrationrate.Length > 0)
                                    {
                                        for (int numhs = 0; numhs < n.mnInfo.infiltrationrate.GetLength(1); numhs++)
                                        {
                                            n.mnInfo.infiltrationrate[j, numhs] = n.mnInfo.infiltrationrate[lastTimeStep - 1, numhs];
                                        }
                                    }
                                    if (n.mnInfo.nodedemand.Length > 0)
                                    {
                                        for (int numhs = 0; numhs < n.mnInfo.nodedemand.GetLength(1); numhs++)
                                        {
                                            n.mnInfo.nodedemand[j, numhs] = n.mnInfo.nodedemand[lastTimeStep - 1, numhs];
                                        }
                                    }
                                    break;
                                case ModsimModel.NodeType.Sink:
                                    //SINK nodes arrays are populated with the arrays
                                    break;
                                default:
                                    throw new Exception("Node type is undefined");
                            }
                        }
                    }
                }
            }
        }

        // Split network at routing links
        public static void SplitNetwork(Model mi)
        {
            Link l;
            Node n;
            RegionHolder rh = null;
            int i;
            int j;
            long mostDownstreamRegionID;
            int[] regionDownstream; //[MAXREGIONS];  // Starting at 1
            double[] Raccum;
            double[] Rtmp;
            double[,] RDownstreamFactors; //[MAXREGIONS][MAXSIZERCOEFF];
            bool[] directionInverted;
            long MaxRCoeffNo = 0;

            // Clear all touched flags to zero - NODES
            for (n = mi.firstNode; n != null; n = n.next)
            {
                n.backRRegionID = 0;
            }

            // Find all routing links and mark upstream and downstream nodes
            Routing.maxRegionID = 1;
            List<int> downStreamRegions = new List<int>();

            for (l = mi.firstLink; l != null; l = l.next)
            {
                if (!l.mlInfo.isArtificial && l.m.loss_coef >= 1.0)
                {
                    if (l.from.backRRegionID == 0)
                    {
                        l.from.backRRegionID = Routing.maxRegionID;
                        Csu.Modsim.NetworkUtils.NetTopology.MarkRoutingRegionNetworkUpStream(mi, l.from, Routing.maxRegionID);
                        Csu.Modsim.NetworkUtils.NetTopology.MarkRoutingRegionNetworkDownStream(mi, l.from, Routing.maxRegionID);
                        Routing.maxRegionID++;
                    }
                }
            }

            //Search for most downstream region
            for (l = mi.firstLink; l != null; l = l.next)
            {
                if (!l.mlInfo.isArtificial && l.m.loss_coef >= 1.0)
                {
                    if (l.to.backRRegionID == 0)
                    {
                        l.to.backRRegionID = Routing.maxRegionID;
                        Csu.Modsim.NetworkUtils.NetTopology.MarkRoutingRegionNetworkDownStream(mi, l.to, Routing.maxRegionID);
                        downStreamRegions.Add(Routing.maxRegionID);
                        Routing.maxRegionID++;
                    }
                }
            }

            Routing.maxRegionID--; //There is one unassigned region
            regionDownstream = new int[Routing.maxRegionID + 1];

            // As of this point, we have regionID's and all nodes marked with region

            // Now we can tell which is upstream and which is downstream
            for (l = mi.firstLink; l != null; l = l.next)
            {
                if (!l.mlInfo.isArtificial && l.m.loss_coef >= 1.0)
                {
                    long count = 0;
                    for (i = 0; i < l.m.lagfactors.Length; i++)
                    {
                        if (l.m.lagfactors[i] > 0)
                        {
                            count = i + 1;
                        }
                    }
                    if (MaxRCoeffNo < count)
                    {
                        MaxRCoeffNo = count;
                    }
                }
            }
            //size the array including the most downstream region.
            RDownstreamFactors = new double[Routing.maxRegionID + 1, MaxRCoeffNo];
            directionInverted = new bool[Routing.maxRegionID + 1];
            int[] dSRegionsIDs = new int[downStreamRegions.Count];
            for (i = 0; i < downStreamRegions.Count; i++)
            {
                dSRegionsIDs[i] = downStreamRegions[i];
            }
            mostDownstreamRegionID = NetTopology.MostDownStreamRegionNo(mi, ref RDownstreamFactors, dSRegionsIDs, directionInverted, regionDownstream);

            // All regions can be played from downstream to upstream

            // Identity in downstream time
            RDownstreamFactors[mostDownstreamRegionID, 0] = 1.0;

            //This step is done if there are regions identified.

            if (Routing.maxRegionID > 0)
            {
                RegionHolder rhUpS = new RegionHolder();
                rhUpS.regionID = 1;
                rhUpS.nextRegionID = regionDownstream[rhUpS.regionID];
                for (i = 2; i <= Routing.maxRegionID; i++)
                {

                    rh = new RegionHolder();
                    rh.nextUpS = rhUpS;
                    rhUpS = rh;
                    if (i < regionDownstream.Length)
                    {
                        rh.nextRegionID = regionDownstream[i];
                    }

                    rh.regionID = i;

                }
                RegionHolder.firstRegion = rh; //This region is not pointed by anybody, needed to start regions navigation.
            }

            //Invert and replace region coefficients corresponding to bifurcations
            for (int regID = 1; regID <= Routing.maxRegionID; regID++) //for all regions
            {
                if (directionInverted[regID] && directionInverted[regionDownstream[regID]])
                {
                    double[] invertedRFactors = new double[RDownstreamFactors.GetLength(1)];
                    for (j = 0; j < RDownstreamFactors.GetLength(1); j++)
                    {
                        invertedRFactors[j] = RDownstreamFactors[regID, j];
                    }
                    invertedRFactors = Routing.CoefMatrixInverseLT(invertedRFactors);
                    for (j = 0; j < RDownstreamFactors.GetLength(1); j++)
                    {
                        RDownstreamFactors[regID, j] = invertedRFactors[j];
                    }
                }
            }

            int Lags;
            int vectorSize = RDownstreamFactors.GetLength(1);
            for (int regID = 1; regID <= Routing.maxRegionID; regID++) //for all regions
            {
                rh = RegionHolder.GetRegion(regID);

                // Start Raccum at identity.  Push downstream to farthest downstream time.
                Raccum = new double[vectorSize];
                Raccum[0] = 1.0;

                /* The routing coeficients are calculated from the region one where the
                region coef are equal to the first link, the second region coef are
                calculated based on the previous results assigned to Raccum.
                Rtmp = R0*f0
                Rtmp = R0*f1 + R1*f0
                Rtmp = R0*f2 + R1*f1 + R2f0
                Rtmp = R0*f3 + R1*f2 + R2f1 + R3f0
                .... up to 10 */

                long regnum = 0;

                do
                //for(long regnum = rh->regionID; regnum; regnum = regionDownstream[regnum]) //from currrent region to downstream
                //start calculation with original lags for the region.
                {
                    if (regnum == 0)
                    {
                        regnum = rh.regionID;
                    }
                    else
                    {
                        regnum = regionDownstream[regnum];
                    }
                    Rtmp = new double[vectorSize];

                    // Now go towards downstream and accumulate R
                    for (i = 0; i < vectorSize; i++)
                    {
                        for (j = 0; j < vectorSize; j++)
                        {
                            if ((i + j < vectorSize) && (i < Raccum.Length))
                            {
                                Rtmp[i + j] += Raccum[i] * RDownstreamFactors[regnum, j];
                            }
                        }
                    }
                    Array.Copy(Rtmp, Raccum, vectorSize);
                }
                while (regnum != mostDownstreamRegionID);

                rh.R = new double[Raccum.Length];
                Array.Copy(Raccum, rh.R, Raccum.Length);

                // Assign the Routing factors for each region equals to the routing links factors.
                bool allZeros = true;

                Lags = 0;
                for (i = 0; i < rh.R.Length; i++)
                {
                    //Store the maximum number of initial lags zeros
                    if (rh.R[i] == 0)
                    {
                        Lags++;
                    }
                    else
                    {
                        allZeros = false;
                        break;
                    }
                }

                if (allZeros)
                {
                    mi.FireOnError("all zero coefficient for a routing link");
                    throw new System.Exception(" all zero coefficient routing link");
                }
                rh.Lags = Lags;

                //Calculate the maximum number of positive lag factors.
                int Count = 1;
                for (i = Lags; i < rh.R.Length; i++)
                {
                    //CHECK: It assumes that there will be not zero coefficients before other positive coefficients
                    if (rh.R[i] == 0)
                    {
                        rh.posLastNoPositiveLag = Count + Lags - 2;
                        break;
                    }
                    Count++;
                }
            }

            //Save regional routing coefficients for scripting
            long arraySize = 0;

            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                rh = RegionHolder.GetRegion(regID);
                if (rh.R.Length > arraySize)
                {
                    arraySize = rh.R.Length;
                }
            }
            RegRoutingCoef = new double[Routing.maxRegionID + 1, arraySize];
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                rh = RegionHolder.GetRegion(regID);
                for (i = 0; i < rh.R.Length; i++)
                {
                    RegRoutingCoef[regID, i] = rh.R[i];
                }
            }


            /*Assigns high cost to the routing link to avoid flow throw the link and
            keeps track of the routing link for each region */
            for (long regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                for (l = mi.firstLink; l != null; l = l.next)
                {
                    if (l.m.loss_coef >= 1.0 && l.from.backRRegionID != l.to.backRRegionID && l.from.backRRegionID == regID)
                    {
                        int trueRegID = l.from.backRRegionID;
                        if (directionInverted[l.from.backRRegionID])
                        {
                            if (directionInverted[l.to.backRRegionID])
                            {
                                trueRegID = l.to.backRRegionID;
                            }
                        }
                        rh = RegionHolder.GetRegion(trueRegID);
                        rh.dwsLink = l;
                        if (regID != trueRegID)
                        {
                            rh.invertedDirection = true;
                        }
                    }
                }
            }
        }

        public static void CalculateDWSRouting(Model mi1, Model mi2, long NoNetsToCalculate)
        {
            Node n;
            Node n2;
            RegionHolder rh;

            double[] actualWaterDelivered = new double[DefineConstants.MAXLAG]; // This array could potentially be as large as the number of nets to calculate (Max. No. lags)

            for (int i = mi1.mInfo.realNodesList.Length - 1; i >= 0; i--)
            {
                n = mi1.mInfo.realNodesList[i];
                long demand1 = 0;
                long demand2 = 0;
                n2 = mi2.FindNode(n.number);
                if (n.mnInfo.nodedemand.Length > 0)
                {
                    demand1 = n.mnInfo.nodedemand[mi2.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex];
                    demand2 = n2.mnInfo.nodedemand[mi2.mInfo.CurrentModelTimeStepIndex, n2.mnInfo.hydStateIndex];
                }

                if (n.backRRegionID > 0) // make sure that node is conected to any of the routing links.
                {
                    rh = RegionHolder.GetRegion(n.backRRegionID);

                    if (n.nodeType == NodeType.Demand)
                    {
                        // RKL I think what we really mean is
                        if (mi2.mInfo.CurrentModelTimeStepIndex == mi1.mInfo.CurrentModelTimeStepIndex + 1)
                        {
                            // Reset the minimum flows in the links due to previous downstream flow delivered to this demand.
                            n2.mnInfo.demLink.mlInfo.minFlowBackRouting = 0;
                            n2.mnInfo.demLink.mlInfo.minGWFlowBackRouting = 0;
                        }

                        //This code handle when demand time series calculation crosses over years.

                        long revCount = NoNetsToCalculate;
                        double TempRoutDemand = 0;
                        double currentRoutDemand;
                        long CurrentMonth = mi1.mInfo.CurrentModelTimeStepIndex + 1;
                        for (long j = 0; j <= NoNetsToCalculate; j++)
                        {

                            //Calculate the corresponding month to lookup for shortages.  (when more than a week)
                            long lookupShortageMonth = CurrentMonth;
                            for (long lags = 0; lags < rh.Lags; lags++)
                            {
                                lookupShortageMonth += 1;
                            }
                            long shortage = 0;
                            if (lookupShortageMonth < n2.mnInfo.ishtm.Length)
                            {
                                shortage = n2.mnInfo.ishtm[lookupShortageMonth];
                            }
                            if ((j < NoNetsToCalculate) && (shortage > 0))
                            {
                                //Actual water delivered (in real time but it's calculated using the downstream time
                                //	diversion.  This calculation fail for region 1
                                currentRoutDemand = (n2.mnInfo.demand[lookupShortageMonth] - shortage);

                                double sumPrevDemands = 0;
                                long revCount2 = j + rh.Lags;
                                for (long calcNets = 0; calcNets < j; calcNets++)
                                {
                                    if (revCount2 < rh.R.Length)
                                    {
                                        sumPrevDemands += actualWaterDelivered[calcNets] * rh.R[revCount2];
                                    }
                                    revCount2--;
                                }
                                //This is the actual water delivered to this demand in time "j" in real time
                                if ((currentRoutDemand - sumPrevDemands) > 0)
                                {
                                    actualWaterDelivered[j] = (currentRoutDemand - sumPrevDemands) / rh.R[rh.Lags];
                                }
                                else
                                {
                                    actualWaterDelivered[j] = 0;
                                }
                            }
                            else
                            {
                                //***** Original demand calculations
                                if (n.mnInfo.nodedemand.Length > 0)
                                {
                                    long DemToRoute = n.mnInfo.nodedemand[mi1.mInfo.CurrentModelTimeStepIndex + 1 + j, n.mnInfo.hydStateIndex];
                                    actualWaterDelivered[j] = DemToRoute;
                                }
                                else
                                {
                                    actualWaterDelivered[j] = 0;
                                }
                            }
                            //Add to the current downstream demand values
                            if (revCount < rh.R.Length) // Assumes coeficients with zero values if array size is exceeded.
                            {
                                TempRoutDemand += actualWaterDelivered[j] * rh.R[revCount];
                            }
                            if (j < NoNetsToCalculate - rh.Lags)
                            {
                                //Calculated the part of the demand that in infiltrated and moved to the GWoutlink in the Gwater-damands
                                double routInfiltration;
                                if (revCount < rh.R.Length)
                                {
                                    double nodeinfiltrationrate = 0.0;
                                    if (n.mnInfo.infiltrationrate.Length > mi1.mInfo.CurrentModelTimeStepIndex + 1 + j)
                                    {
                                        nodeinfiltrationrate = n.mnInfo.infiltrationrate[mi1.mInfo.CurrentModelTimeStepIndex + 1 + j, 0];
                                    }
                                    routInfiltration = actualWaterDelivered[j] * rh.R[revCount] * nodeinfiltrationrate;
                                }
                                else
                                {
                                    routInfiltration = 0;
                                }
                                n2.mnInfo.demLink.mlInfo.minFlowBackRouting = (long)(TempRoutDemand - routInfiltration + DefineConstants.ROFF); //Purpose is to assign the min flow in the demand to previous steps calculated flows in the current downstream time.
                                n2.mnInfo.demLink.mlInfo.minGWFlowBackRouting = (long)(routInfiltration + DefineConstants.ROFF);
                            }
                            revCount--;
                            CurrentMonth += 1;
                        }
                        if (TempRoutDemand < 0)
                        {
                            throw new System.Exception("Error: Negative for demand time series in back-routing");
                        }
                        if (TempRoutDemand > 0 && n2.mnInfo.nodedemand.Length == 0)
                        {
                            throw new System.Exception(" Error: nodedemand length is zero - trying to set for back routing");
                        }
                        demand2 = (long)(TempRoutDemand + DefineConstants.ROFF);
                        if (n2.mnInfo.nodedemand.Length > 0)
                        {
                            n2.mnInfo.nodedemand[mi2.mInfo.CurrentModelTimeStepIndex, n.mnInfo.hydStateIndex] = (long)(TempRoutDemand + DefineConstants.ROFF);
                        }
                        n2.mnInfo.demLink.mlInfo.hi = demand2;
                        n2.mnInfo.demLink.mlInfo.lo = 0; //Sets the initial min bound of the mi2 to zero, if needed it will be adjusted in iter = 3.
                    }
                    if (n.mnInfo.inflow.Length > 0)
                    {
                        //Routes all the inflows for all nodes that have any in flows in the time series.
                        //Need to be calculated before the RESERVOIR NODE is processed (Reservoir bracket).
                        long revCount = NoNetsToCalculate;
                        double TempRoutValue = 0;
                        for (long j = 0; j <= NoNetsToCalculate; j++)
                        {
                            if (revCount < rh.R.Length) // Assumes coefficients with zero values if array size is exceeded.
                            {
                                TempRoutValue += n.mnInfo.inflow[mi1.mInfo.CurrentModelTimeStepIndex + 1 + j, 0] * rh.R[revCount];
                            }
                            revCount--;
                        }
                        n2.mnInfo.inflow[mi2.mInfo.CurrentModelTimeStepIndex, 0] = (long)(TempRoutValue + DefineConstants.ROFF);
                    }
                    if (n.nodeType == NodeType.Reservoir)
                    {
                        long TempRoutValue = 0;

                        if (NoNetsToCalculate <= rh.Lags)
                        {
                            if (n.mnInfo.inflow.Length > 0)
                            {
                                TempRoutValue = (long)(n.mnInfo.inflow[mi1.mInfo.CurrentModelTimeStepIndex + 1, 0] * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                                n2.mnInfo.inflow[mi2.mInfo.CurrentModelTimeStepIndex, 0] = (long)(TempRoutValue + DefineConstants.ROFF);
                            }
                            if (n.mnInfo.targetcontent.Length > 0)
                            {
                                TempRoutValue = (long)(n.mnInfo.targetcontent[mi1.mInfo.CurrentModelTimeStepIndex + 1, n.mnInfo.hydStateIndex] * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                                n2.mnInfo.targetcontent[mi2.mInfo.CurrentModelTimeStepIndex, n2.mnInfo.hydStateIndex] = TempRoutValue;
                                if (n2.mnInfo.targetcontent.GetLength(0) > mi2.mInfo.CurrentModelTimeStepIndex + 1)
                                {
                                    n2.mnInfo.targetcontent[mi2.mInfo.CurrentModelTimeStepIndex + 1, n2.mnInfo.hydStateIndex] = TempRoutValue;
                                }
                            }
                            // CHECK: The initial value for the reservoirs in large problems.
                            // CHECK: Including the MaxNoLags we move the initial res volume to the next time step -  Looks to be a good assumption because of the difference in values between water in reservoir and water diverted
                            n2.mnInfo.start = (long)(n.mnInfo.stend * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            n2.m.max_volume = (long)(n.m.max_volume * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            n2.m.min_volume = (long)(n.m.min_volume * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            //Reseting all links associated with reservoirs.
                            n2.mnInfo.targetLink.mlInfo.lo = (long)(n2.mnInfo.targetLink.mlInfo.lo * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            n2.mnInfo.targetLink.mlInfo.hi = TempRoutValue;

                            if (n2.mnInfo.spillLink != null)
                            {
                                n2.mnInfo.spillLink.mlInfo.lo = (long)(n2.mnInfo.spillLink.mlInfo.lo * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            }
                            if (n2.mnInfo.spillLink != null)
                            {
                                n2.mnInfo.spillLink.mlInfo.hi = (long)(n2.mnInfo.spillLink.mlInfo.hi * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            }

                            n2.mnInfo.excessStoLink.mlInfo.lo = (long)(n2.mnInfo.excessStoLink.mlInfo.lo * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            n2.mnInfo.excessStoLink.mlInfo.hi = (long)(n2.mnInfo.excessStoLink.mlInfo.hi * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);

                            n2.mnInfo.evapLink.mlInfo.lo = (long)(n2.mnInfo.evapLink.mlInfo.lo * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            n2.mnInfo.evapLink.mlInfo.hi = (long)(n2.mnInfo.evapLink.mlInfo.hi * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);

                            n2.mnInfo.infLink.mlInfo.lo = (long)(n2.mnInfo.infLink.mlInfo.lo * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                            n2.mnInfo.infLink.mlInfo.hi = (long)(n2.mnInfo.infLink.mlInfo.hi * rh.R[NoNetsToCalculate] + DefineConstants.ROFF);
                        }
                        else
                        {
                            // For values that don't change during the downstream simulation.

                            //Routed targets calculation due to their change for each time step.
                            long revCount = NoNetsToCalculate;
                            TempRoutValue = 0;

                            if (n.mnInfo.targetcontent.Length > 0)
                            {
                                TempRoutValue = (long)(n.mnInfo.targetcontent[mi1.mInfo.CurrentModelTimeStepIndex + 1 + NoNetsToCalculate - rh.Lags, n.mnInfo.hydStateIndex] * rh.R[rh.Lags] + DefineConstants.ROFF);
                                n2.mnInfo.targetcontent[mi2.mInfo.CurrentModelTimeStepIndex, n2.mnInfo.hydStateIndex] = (long)(TempRoutValue + DefineConstants.ROFF);
                                if (n2.mnInfo.targetcontent.GetLength(0) < mi2.mInfo.CurrentModelTimeStepIndex + 1)
                                {
                                    n2.mnInfo.targetcontent[mi2.mInfo.CurrentModelTimeStepIndex + 1, n2.mnInfo.hydStateIndex] = (long)(TempRoutValue + DefineConstants.ROFF);
                                }
                            }
                            n2.mnInfo.targetLink.mlInfo.lo = n2.m.min_volume;
                            n2.mnInfo.targetLink.mlInfo.hi = (long)(TempRoutValue + DefineConstants.ROFF);
                            double[] realTimeReserVolumeOut;
                            realTimeReserVolumeOut = new double[NoNetsToCalculate + 1];
                            double dwsNetVolReleased;
                            double tempRoutStartVol = 0.0;
                            long realTimeIndex = mi1.mInfo.CurrentModelTimeStepIndex + 1;
                            realTimeIndex += rh.Lags;

                            revCount = NoNetsToCalculate;

                            for (long j = rh.Lags; j <= NoNetsToCalculate; j++)
                            {
                                if (j < NoNetsToCalculate)
                                {
                                    //Actual water delivered (in real time but it's calculated using the downstream time
                                    //	diversion.  This calculation fail for region 1
                                    dwsNetVolReleased = (double)(n2.mnInfo.start_storage[realTimeIndex] - n2.mnInfo.end_storage[realTimeIndex]);
                                    //			start_storage and end_storage are OUTPUT arrays and are NOT SET until AFTER convergence
                                    //			ET -> This is not equivalent to the original code
                                    //			      This statement is called after a previous downstream (mi2) network has converged.
                                    //dwsNetVolReleased = (double)(n2->mnInfo->start - n2->mnInfo->stend);

                                    //This is the actual water delivered to this demand in time "j" in real time
                                    realTimeReserVolumeOut[j] = (dwsNetVolReleased) / rh.R[rh.Lags];

                                    //Add to the current downstream demand values
                                    if (revCount < rh.R.Length) // Assumes coeficients with value zero if array size is exceeded.
                                    {
                                        tempRoutStartVol += realTimeReserVolumeOut[j] * rh.R[revCount];
                                    }
                                    revCount--;
                                    realTimeIndex += 1;
                                }
                            }
                            if (tempRoutStartVol >= 0)
                            {
                                TempRoutValue = 0;
                                if (n2.mnInfo.inflow.Length <= 0)
                                {
                                    n2.mnInfo.inflow = new long[mi1.TimeStepManager.noModelTimeSteps + mi1.TimeStepManager.noBackRAdditionalTSteps, 1];
                                    n.mnInfo.inflow = new long[mi1.TimeStepManager.noModelTimeSteps + mi1.TimeStepManager.noBackRAdditionalTSteps, 1];
                                }
                                TempRoutValue = n.mnInfo.inflow[mi2.mInfo.CurrentModelTimeStepIndex, 0];
                                TempRoutValue += (long)tempRoutStartVol;
                                n2.mnInfo.inflow[mi2.mInfo.CurrentModelTimeStepIndex, 0] = (long)(TempRoutValue + DefineConstants.ROFF);
                            }
                            else
                            {
                                if (n2.mnInfo.spillLink != null)
                                {
                                    n2.mnInfo.spillLink.mlInfo.lo += (long)(System.Math.Abs(tempRoutStartVol) + DefineConstants.ROFF);
                                }
                            }
                        }
                        // CHECK: This is provisional - not sure if it even works
                        if (mi1.runType == ModsimRunType.Conditional_Rules)
                        {
                            n2.mnInfo.targetLink.mlInfo.cost = -(50000 - n.m.priority[n.mnInfo.hydStateIndex] * 10);
                        }
                        else
                        {
                            n2.mnInfo.targetLink.mlInfo.cost = -50000 + 10 * n.m.priority[0];
                        }
                    }
                    if (n.nodeType == NodeType.Sink)
                    {
                        long tempRoutDemand = 0;
                        if (n.mnInfo.nodedemand.Length > 0)
                        {
                            tempRoutDemand = n.mnInfo.nodedemand[mi1.mInfo.CurrentModelTimeStepIndex + 1, n.mnInfo.hydStateIndex];
                        }
                        n2.mnInfo.demLink.mlInfo.hi = tempRoutDemand;
                        n2.mnInfo.demLink.mlInfo.lo = 0;
                    }

                }
                else //For nodes that are not attached to the routing links
                {
                    if (n.nodeType == NodeType.Demand)
                    {
                        long TempRoutDemand = 0;
                        if (n2.mnInfo.nodedemand.Length > 0)
                        {
                            TempRoutDemand = n.mnInfo.nodedemand[mi1.mInfo.CurrentModelTimeStepIndex + 1, n.mnInfo.hydStateIndex];
                        }
                        n2.mnInfo.nodedemand[mi2.mInfo.CurrentModelTimeStepIndex, n2.mnInfo.hydStateIndex] = TempRoutDemand;
                        n2.mnInfo.demLink.mlInfo.hi = (long)(TempRoutDemand + DefineConstants.ROFF);
                    }
                    if (n.mnInfo.inflow.Length > 0)
                    {
                        n2.mnInfo.inflow[mi2.mInfo.CurrentModelTimeStepIndex, 0] = n.mnInfo.inflow[mi1.mInfo.CurrentModelTimeStepIndex + 1, 0];
                    }
                    if (n.nodeType == NodeType.Reservoir)
                    {
                        if (n2.mnInfo.inflow.Length > 0)
                        {
                            n2.mnInfo.inflow[mi2.mInfo.CurrentModelTimeStepIndex, 0] = n.mnInfo.inflow[mi1.mInfo.CurrentModelTimeStepIndex + 1, 0];
                        }
                        if (n2.mnInfo.targetcontent.Length > 0)
                        {
                            n2.mnInfo.targetcontent[mi2.mInfo.CurrentModelTimeStepIndex, n2.mnInfo.hydStateIndex] = n.mnInfo.targetcontent[mi1.mInfo.CurrentModelTimeStepIndex + 1, n.mnInfo.hydStateIndex];
                        }
                        n2.mnInfo.start = (long)n.mnInfo.stend;
                        n2.m.max_volume = (long)(n.m.max_volume);
                        n2.m.min_volume = (long)(n.m.min_volume);
                    }
                }
            }
        }

        /* This routine calculates the upper limit of all regions routing links
        for mi1-networks based on the flows calculated for routed links in networks that don't
        have routing effects(mi2-networks)*/
        public static void CopyAllResultstoUPSTime(Model mi)
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                Link l = rh.dwsLink;
                //This link will not be defined when it's the most downstream region. It's OK.
                if (l != null)
                {
                    double regionRoutingCoef;
                    if (rh.invertedDirection)
                    {
                        RegionHolder rhDwS = RegionHolder.GetRegion(l.from.backRRegionID);
                        regionRoutingCoef = rhDwS.R[rhDwS.Lags];
                    }
                    else
                    {
                        regionRoutingCoef = rh.R[rh.Lags];
                    }
                    double flows2 = rh.dwsLFlow[0 + rh.Lags] * (1 / regionRoutingCoef);
                    Link l1 = mi.FindLink(l.number);
                    /* This code sets up the upper limit of the routing link to the result
                    of processing mi2s networks results
                    It uses the first value of the results of inverting the matrix and multiplying times the mi2s flows */
                    if (l1.to.mnInfo.routingLink != null)
                    {
                        l1.to.mnInfo.routingLink.mlInfo.hi = (long)(flows2 + DefineConstants.ROFF);
                    }
                }
            }
        }

        public static int CalculateMaxLags(bool storageAccWithBackRouting)
        {
            RegionHolder rh;

            int maxLagsRegID = 1; //Default assumes that Lags = 0 and all regions will have same Lags (zero).
            int maxLags = 0;
            int storageMaxLags = 0;
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                rh = RegionHolder.GetRegion(regID);
                if (rh.Lags > maxLags)
                {
                    maxLagsRegID = regID;
                    maxLags = rh.Lags;
                }
                int index = rh.Lags;
                //TODO: Check that is guaranteed that we have zeros at the end of ->R ( important for storage accounts handling)
                while (rh.R.Length > index && rh.R[index] > 0)
                {
                    index++;
                }
                if (storageMaxLags < index)
                {
                    storageMaxLags = index;
                }
            }
            rh = RegionHolder.GetRegion(maxLagsRegID);
            //TODO: Check if this applies after making variables dynamic and fixing 12 size arrays.
            if (rh.Lags > 50)
            {
                Model.FireOnErrorGlobal(" the maximum number of lags has been exceeded ");
                throw new System.Exception(" the maximum number of lags has been exceeded ");
            }
            if (storageAccWithBackRouting)
            {
                //Calculates all lags that could be affected by the present storage releases.  Substract one because loop in operate is zero based.
                return storageMaxLags - 1;
            }
            else
            {
                //Calculates only the minimum number of lags need to distribute Natural flow.
                return rh.Lags;
            }
        }

        public static void SaveDWSTimeFlowsInRoutingLinks(Model mi2, long CurrentPosition, long iodd, Model mi1, long mon)
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                if (rh.dwsLink != null)
                {
                    Link l = rh.dwsLink;
                    Link l2 = mi2.FindLink(l.number);
                    rh.dwsLFlow[CurrentPosition] = l2.mlInfo.flow;
                    //Save the flow if natural flow iteration
                    if (mi1.storageAccountsWithBackRouting && iodd >= 0)
                    {
                        rh.dwsLFlowNFOnly[CurrentPosition] = l2.mrlInfo.natFlow[mon];
                    }
                    //Saves the real time flow in this is the position where the routing link flow is to be calculated->
                    Link l3 = mi2.FindLink(l.to.OutflowLinks.next.link.number); //Link where the min flow is set to
                    if (CurrentPosition >= rh.Lags)
                    {
                        rh.MinRTLinkFlow[CurrentPosition] = (double)(l2.mlInfo.flow - l3.mlInfo.lo) / rh.R[rh.Lags];
                        rh.MinRTLinkFlowNFOnly[CurrentPosition] = (double)(l2.mrlInfo.natFlow[mon] - l3.mlInfo.lo) / rh.R[rh.Lags];
                    }
                }
            }

            //Saves the flows through the storage owners links in realtime values.
            if (mi1.storageAccountsWithBackRouting)
            {
                for (int i = mi1.mInfo.realLinkList.Length - 1; i >= 0; i--)
                {
                    Link l = mi1.mInfo.realLinkList[i];
                    Link l2 = mi2.FindLink(l.number);
                    if (!l.mlInfo.isArtificial)
                    {
                        if (l.mlInfo.isOwnerLink)
                        {
                            if (iodd == 0)
                            {
                                RegionHolder rh = RegionHolder.GetRegion(l.to.backRRegionID);
                                //add the same logic that demand has to calculate actual flow
                                //  need additional variable .. could use the mi2 space in the link.
                                double sumPrevStorageVol = 0;
                                long revCount = CurrentPosition + rh.Lags;
                                for (long calcNets = 0; calcNets < CurrentPosition; calcNets++)
                                {
                                    if (revCount < rh.R.Length)
                                    {
                                        sumPrevStorageVol += l.mrlInfo.storageWaterCalcFutureTSteps[calcNets] * rh.R[revCount];
                                    }
                                    revCount--;
                                }
                                //This is the actual water delivered to this demand in time "CurrentPosition" in real time
                                if (l2.mlInfo.flow > sumPrevStorageVol)
                                {
                                    l.mrlInfo.storageWaterCalcFutureTSteps[CurrentPosition] = (l2.mlInfo.flow - sumPrevStorageVol) / rh.R[rh.Lags];
                                }
                                else
                                {
                                    l.mrlInfo.storageWaterCalcFutureTSteps[CurrentPosition] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ReturnFlowLinksRoute(Model mi1, Model mi2, long NoNetsToCalculate)
        {
            int j;
            //Route the flows returned from previous mi1 time step run
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                Link l = rh.dwsLink;
                if (l != null)
                {
                    Link l2 = mi2.FindLink(l.number);

                    int nextID = l.to.backRRegionID;
                    RegionHolder rhNext = RegionHolder.GetRegion(nextID);
                    double[] Rs = new double[rhNext.R.Length];
                    for (int i = 0; i < rhNext.R.Length; i++)
                    {
                        Rs[i] = rhNext.R[i];
                    }

                    long revCount = NoNetsToCalculate;
                    double TempRoutValue = 0.0;
                    for (j = 0; j <= NoNetsToCalculate; j++)
                    {
                        if (revCount < rhNext.R.Length)
                        {
                            TempRoutValue += rh.linkPrevflow[j] * Rs[revCount];
                        }
                        revCount--;
                    }

                    if (TempRoutValue >= 0)
                    {
                        //Only when no negative region routing coefficients
                        l2.mrlInfo.linkPrevflow[0] = (long)(TempRoutValue + DefineConstants.ROFF);
                    }
                    else
                    {
                        l2.mrlInfo.linkPrevflow[0] = (long)(0);
                        l2.mrlInfo.closs = (long)System.Math.Abs(TempRoutValue + DefineConstants.ROFF);
                    }
                    //clears routing link flows used to account for negative returns.
                    l2.to.mnInfo.routingLink.mlInfo.lo = (long)0;
                    l2.to.mnInfo.routingLink.mlInfo.hi = (long)0;
                    l2.m.returnNode.mnInfo.iroutreturn = l2.mrlInfo.linkPrevflow[0];
                }
            }

            //Route the GW Returns
            LagInfo lInfo;
            for (int i = mi1.mInfo.realNodesList.Length - 1; i >= 0; i--)
            {
                Node n = mi1.mInfo.realNodesList[i];

                if (n.nodeType == NodeType.Demand)
                {
                    for (j = 0, lInfo = n.m.infLagi; lInfo != null; lInfo = lInfo.next, j++)
                    {
                        if (lInfo.location != null) // make sure pointer is ok->.. (not guarenteed)
                        {
                            if (lInfo.location.backRRegionID > 0)
                            {
                                Node n2 = mi2.mInfo.realNodesList[i];
                                RegionHolder rh = RegionHolder.GetRegion(lInfo.location.backRRegionID);
                                long revCount = NoNetsToCalculate;
                                double TempRoutValue = 0;
                                for (long klag = 0; klag <= NoNetsToCalculate; klag++)
                                {
                                    if (revCount < rh.R.Length && klag < n.mnInfo.demPrevFlow.GetLength(1))
                                    {
                                        TempRoutValue += n.mnInfo.demPrevFlow[j, klag] * rh.R[revCount];
                                    }
                                    revCount--;
                                }
                                n2.mnInfo.demPrevFlow[j, 0] = (long)(TempRoutValue + DefineConstants.ROFF);
                            }
                        }
                    }
                }
            }
        }

        public static void RestoreRoutedReturnFlows(Model mi1)
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                Link l = rh.dwsLink;
                if (l != null)
                {
                    Link l1 = mi1.FindLink(l.number);
                    /*We need to use the next downstream region routing factors for the return flow
                    in the current region routing link*/
                    for (int i = 0; i < DefineConstants.MAXLAG; i++)
                    {
                        l1.mrlInfo.linkPrevflow[i] = rh.linkPrevflow[i];
                    }
                }

            }
        }

        public static void SaveRoutedReturnFlows(Model mi1)
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                Link l = rh.dwsLink;
                if (l != null)
                {
                    Link l1 = mi1.FindLink(l.number);
                    /*We need to use the next downstream region routing factors for the return flow
                    in the current region routing link*/
                    for (int i = 0; i < DefineConstants.MAXLAG; i++)
                    {
                        rh.linkPrevflow[i] = l1.mrlInfo.linkPrevflow[i];
                    }
                }

            }
        }

        public static double RFactorForReturnFlow(int RDemand, int RReturn)
        {
            double rval = 1;
            if (RDemand > 0 && RReturn > 0)
            {
                RegionHolder rh = RegionHolder.GetRegion(RDemand);
                RegionHolder rh2 = RegionHolder.GetRegion(RReturn);

                if (rh.R[0] == 0 || rh2.R[0] == 0)
                {
                    rval = 1F;
                }
                else
                {
                    rval = 1 / rh.R[0] * rh2.R[0];
                }
            }
            return rval;
        }

        public static void resetMINdwsLFlows()
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                for (long i = 0; i < DefineConstants.MAXLAG; i++)
                {
                    rh.MinRTLinkFlow[i] = 0.0;
                }
                for (long i = 0; i < DefineConstants.MAXLAG; i++)
                {
                    rh.MinRTLinkFlowNFOnly[i] = 0.0;
                }
            }
        }

        public static void setMinFlowDWSLinksBRouting(Model mi2, long currentPosition, Model mi1, long iodd)
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                Link l = rh.dwsLink;
                if (l != null)
                {
                    double minFlow = 0.0;
                    long revCount = currentPosition;
                    if (currentPosition >= rh.Lags)
                    {
                        for (long i = rh.Lags; i <= currentPosition; i++)
                        {
                            if (revCount < rh.R.Length) // Assumes coeficients with zero values if array size is exceeded.
                            {
                                if (mi1.storageAccountsWithBackRouting && iodd == 1)
                                {
                                    minFlow += rh.MinRTLinkFlow[i] * rh.R[revCount];
                                }
                                else
                                {
                                    minFlow += rh.MinRTLinkFlowNFOnly[i] * rh.R[revCount];
                                }
                            }
                            revCount--;
                        }
                        if (minFlow > 0 && currentPosition <= rh.posLastNoPositiveLag)
                        {
                            Link l2 = mi2.FindLink(l.to.OutflowLinks.next.link.number);
                            l2.mlInfo.lo = (long)(minFlow + DefineConstants.ROFF);
                        }
                    }
                }
            }
            for (int i = mi1.mInfo.realNodesList.Length - 1; i >= 0; i--)
            {
                Node n = mi1.mInfo.realNodesList[i];
                if (n.backRRegionID > 0) // make sure that node is conected to any of the routing regions.
                {
                    RegionHolder rh = RegionHolder.GetRegion(n.backRRegionID);
                    Node n2 = mi2.FindNode(n.number);
                    if (n.nodeType == NodeType.Demand)
                    {
                        if (currentPosition <= rh.posLastNoPositiveLag && n2.mnInfo.demLink.mlInfo.minFlowBackRouting != 0)
                        {
                            if (n2.mnInfo.demLink.mlInfo.minFlowBackRouting < 0)
                            {
                                throw new System.Exception("Error: Negative Min value for demand in back-routing");
                            }
                            //It adjusts the lower limit of the link to assure at least the fraction of the flows previously allocated to each demand.
                            n2.mnInfo.demLink.mlInfo.lo = n2.mnInfo.demLink.mlInfo.minFlowBackRouting;
                        }

                    }
                }
            }
        }

        public static void zeroFlowDWSRoutingLinks(Model mi2)
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                if (rh.dwsLink != null)
                {
                    Link l = rh.dwsLink;
                    Link l2 = mi2.FindLink(l.to.OutflowLinks.next.link.number);
                    l2.mlInfo.lo = 0;
                }
            }
        }

        //public static void CreateBackLinks(Model mi)
        //{

        //    Link l;
        //    Link rtnL;
        //    Node n = null;
        //    for (long i = 0; i < mi.mInfo.realLinkList.Length; i++)
        //    {
        //        l = mi.mInfo.realLinkList[i];
        //        if (!l.mlInfo.isArtificial && l.m.loss_coef >= 1) // routing link
        //        {
        //            n = l.to;
        //            rtnL = mi.AddNewLink(false);
        //            Utils.ConnectFromNode(rtnL, l.to);
        //            Utils.ConnectToNode(rtnL, l.from);
        //            rtnL.name = "ArtificialLink_" + l.from.name + "_" + l.to.name;
        //            rtnL.mlInfo = new MlInfo(); ;
        //            rtnL.mrlInfo = new MrlInfo();
        //            rtnL.mlInfo.isArtificial = true;
        //            rtnL.mlInfo.lo = 0;
        //            rtnL.mlInfo.hi = 5;
        //            rtnL.mlInfo.cost = DefineConstants.COST_SMALL - 1; // 999998;
        //            // Add to mi->mInfo->lList
        //            mi.mInfo.lList[rtnL.number] = rtnL;
        //            mi.mInfo.lListLen++;
        //        }
        //    }

        //}

        public static void RestoreStorageOwnShipLinkFlows(Model mi, long iodd)
        {
            for (int i = mi.mInfo.realLinkList.Length - 1; i >= 0; i--)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (!l.mlInfo.isArtificial)
                {
                    if (l.mlInfo.isOwnerLink)
                    {
                        //Replace the zero value used originally with the value calculated
                        //  when running the downstream time networks.  Captures water from storage in the natural flow step
                        //  freely available after routing.

                        if (iodd == 0 || iodd == 1)
                        {
                            l.mlInfo.hi = (long)(l.mrlInfo.storageWaterCalcFutureTSteps[0] + DefineConstants.ROFF);
                            l.mlInfo.lo = l.mlInfo.hi;
                        }
                        else
                        {
                            l.mlInfo.lo = 0;
                        }
                    }
                }
            }

        }
        public static void InitializeStorageWaterFromPreviousTSArrays(Model mi)
        {
            for (int i = mi.mInfo.realLinkList.Length - 1; i >= 0; i--)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (!l.mlInfo.isArtificial)
                {
                    if (l.mlInfo.isOwnerLink)
                    {
                        long maxLags = Routing.CalculateMaxLags(mi.storageAccountsWithBackRouting);
                        //add one to store current in location zero.
                        l.mrlInfo.storageWaterCalcFutureTSteps = new double[maxLags + 1];
                    }
                }
            }
        }
        // RKL which iter is being used? mi1 or mi2 or are they the same?
        public static void RouteStorageOwnShipLinkFlows(Model mi1, Model mi2, long noNetsToCalculate, long iter, long iodd)
        {
            long mon = mi2.mInfo.CurrentModelTimeStepIndex;
            long mymon = mi1.mInfo.CurrentModelTimeStepIndex + 1;

            //Route the previous calculated flows in the onwership links to set up minimum flows in downstream time
            for (int i = mi1.mInfo.realLinkList.Length - 1; i >= 0; i--)
            {
                Link l = mi1.mInfo.realLinkList[i];
                Link l2 = mi2.FindLink(l.number);
                if (!l.mlInfo.isArtificial)
                {
                    if (l.mlInfo.isOwnerLink)
                    {
                        //Roll back the values in the previous calculated link flows
                        if (mon == mymon && iter == 1)
                        {
                            long index;
                            for (index = 0; index < l.mrlInfo.storageWaterCalcFutureTSteps.Length - 1; index++)
                            {
                                l.mrlInfo.storageWaterCalcFutureTSteps[index] = l.mrlInfo.storageWaterCalcFutureTSteps[index + 1];
                            }
                            l.mrlInfo.storageWaterCalcFutureTSteps[index] = 0;
                        }

                        RegionHolder rh = RegionHolder.GetRegion(l.to.backRRegionID);
                        long revCount = noNetsToCalculate;
                        double TempRoutValue = 0;
                        for (long j = 0; j <= noNetsToCalculate; j++)
                        {
                            if (revCount < rh.R.Length) // Assumes coeficients with zero values if array size is exceeded.
                            {
                                TempRoutValue += l.mrlInfo.storageWaterCalcFutureTSteps[j] * rh.R[revCount];
                            }
                            revCount--;
                        }
                        if (iter == 0)
                        {
                            l2.mlInfo.lo = 0;
                            l2.mlInfo.hi = 0;
                        }
                        else
                        {
                            if (iodd == 0)
                            {
                                l2.mlInfo.lo = (long)(TempRoutValue + DefineConstants.ROFF);
                                l2.mlInfo.hi = l2.mlInfo.lo;
                            }
                            else
                            {
                                l2.mlInfo.lo += (long)(TempRoutValue + DefineConstants.ROFF);
                                l2.mlInfo.hi += (long)(TempRoutValue + DefineConstants.ROFF);
                            }
                        }
                    }
                }
            }
        }

        public static void CreateSinkNodesAtRoutLinks(Model mi1)
        {
            for (Link l = mi1.firstLink; l != null; l = l.next)
            {
                if (l.m.loss_coef >= 1.0)
                {
                    Node newSinkNode;
                    newSinkNode = new Node();
                    newSinkNode.nodeType = NodeType.Sink;
                    mi1.AddNode(newSinkNode, true);
                    string[] s = { "", "" };
                    s[0] = "SINK";
                    s[1] = System.Convert.ToString(newSinkNode.number);
                    newSinkNode.name = string.Concat(s);
                    newSinkNode.description = "";
                    Node fromNode = mi1.FindNode(l.m.returnNode.number);
                    mi1.AddNewRealLink(fromNode, newSinkNode);
                }
            }
        }

        public static void CreateListRoutingLinks(Model mi)
        {
            mi.routingLinks = new LinkList();
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                if (l.m.loss_coef == 1)
                {
                    mi.routingLinks.Add(l);
                }
            }
        }

        public static double[,] RegRoutingCoef;
        public static double[] CoefMatrixInverseLT(double[] routingCoef)
        {
            long firstNonZero = 0;
            for (long row = 0; row < routingCoef.Length; row++)
            {
                if (routingCoef[row] > 0)
                {
                    break;
                }
                firstNonZero += 1;
            }
            //Create Matrix using routing coefficients.
            long length = routingCoef.Length - firstNonZero;
            double[,] Matrix = new double[length, length];
            for (long row = firstNonZero; row < routingCoef.Length; row++)
            {
                for (long col = firstNonZero; col < routingCoef.Length; col++)
                {
                    if (row >= col)
                    {
                        Matrix[(col - firstNonZero), (row - firstNonZero)] = routingCoef[row - col + firstNonZero];
                    }
                }
            }

            double[,] ForwardMatrix = new double[length, length];
            double[,] MatrixInverse = new double[length, length];
            double divisor;

            MatrixCopy(ForwardMatrix, Matrix);
            MatrixSetIdentity(MatrixInverse);



            for (long row = 0; row < length; row++) // row by row
            {
                // First divide row by top left value
                divisor = ForwardMatrix[row, row];

                if (divisor == 0)
                {
                    throw new System.Exception("Division by zero - Error in Back-routing Code\n");
                }
                // Set to identity
                for (long col = 0; col < length; col++) // Across all columns
                {
                    ForwardMatrix[col, row] /= divisor;
                    MatrixInverse[col, row] /= divisor;
                }

                // Now use to clear all others below this row (know LT matrix)
                for (long rTmp = row + 1; rTmp < length; rTmp++)
                {
                    if (rTmp != row)
                    {

                        double beta = ForwardMatrix[row, rTmp];
                        for (long cTmp = 0; cTmp < length; cTmp++)
                        {
                            ForwardMatrix[cTmp, rTmp] -= beta * ForwardMatrix[cTmp, row];
                            MatrixInverse[cTmp, rTmp] -= beta * MatrixInverse[cTmp, row];
                        }
                    }
                }
            }
            double[] vectorInverted = new double[routingCoef.Length];
            for (long row = firstNonZero; row < routingCoef.Length; row++)
            {
                vectorInverted[row] = (double)MatrixInverse[0, (row - firstNonZero)];
            }
            return vectorInverted;
        }

        public static void MatrixCopy(double[,] dest, double[,] src)
        {
            dest = (double[,])src.Clone();
        }

        public static void MatrixSetIdentity(double[,] Matrix)
        {
            for (int i = 0; i < Matrix.Length; i++)
            {
                Matrix[i, i] = 1;
            }
        }

        public static void HandleNegativeReturns(Model mi)
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                Link l = rh.dwsLink;
                if (l != null)
                {
                    Link l1 = mi.FindLink(l.number);
                    if (l1.to.mnInfo.routingLink != null)
                    {
                        l1.to.mnInfo.routingLink.mlInfo.lo = l1.mrlInfo.closs;
                        l1.to.mnInfo.routingLink.mlInfo.hi = l1.mrlInfo.closs;
                    }
                }
            }
        }
        //Add a link between the end of the routing link and the following node
        //	This structure allow water returned downstream from the place where water is
        //	taken out by the routing link for the future.
        public static void AddRoutingLinksStructure(Model mi)
        {
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                if (l.m.loss_coef >= 1.0)
                {
                    Node newIntermNode = mi.AddNewNode(true);
                    newIntermNode.name = "EndRouting" + l.number.ToString();
                    newIntermNode.description = "";
                    Link zeroFlowLink = mi.AddNewLink(true);
                    Utils.ConnectFromNode(zeroFlowLink, newIntermNode);
                    Utils.ConnectToNode(zeroFlowLink, l.to);
                    zeroFlowLink.mlInfo = new MlInfo();
                    zeroFlowLink.name = "ZeroFlowLink" + l.number.ToString();
                    Utils.DisConnectToNode(l);
                    Utils.ConnectToNode(l, newIntermNode);
                }
            }
        }
        public static void BlockRoutingRegionFlow(Model mi)
        {
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                if (l.m.loss_coef >= 1.0)
                {
                    Link zeroFlowLink = mi.FindLink("ZeroFlowLink" + l.number.ToString());
                    zeroFlowLink.mlInfo.cost = DefineConstants.COST_LARGE; // 99999999;
                }
            }
        }
        public static void BackRoutingErrorCheck(Model mi1)
        {
            for (int regID = 1; regID <= Routing.maxRegionID; regID++)
            {
                RegionHolder rh = RegionHolder.GetRegion(regID);
                if (rh.dwsLink != null)
                {
                    Link l = rh.dwsLink;
                    Link l2 = mi1.FindLink("ZeroFlowLink" + l.number.ToString());
                    if (l2.mlInfo.flow > 0)
                    {
                        long errorPerc = (long)(l2.mlInfo.flow * 100 / l.mlInfo.flow);
                        mi1.FireOnMessage(string.Concat("BackRouting ERROR - in link ", System.Convert.ToString(l.number), " = ", System.Convert.ToString(errorPerc)));
                    }
                }
            }
        }
        public static void Set_realLink_lo_hi(Model mi1, Model mi2, long NoNetsToCalculate, ModelOutputSupport mi2MyMODSIMOutput)
        {
            for (int i = 0; i < mi1.mInfo.realLinkList.Length; i++)
            {
                Link lmi1 = mi1.mInfo.realLinkList[i];
                Link l = mi2.FindLink(lmi1.number);
                l.mrlInfo.accumsht = 0;
                l.mlInfo.flow0 = 0;

                Node n = mi1.FindNode(l.from.number);
                if (n.backRRegionID > 0)
                {
                    RegionHolder rh = RegionHolder.GetRegion(n.backRRegionID);
                    double[] actualWaterDelivered = new double[DefineConstants.MAXLAG];
                    long revCount = NoNetsToCalculate;
                    double TempRoutFlow = 0;
                    double currentDwsFlow;
                    double minFlow = 0;
                    double maxFlow = mi1.defaultMaxCap_Super; // 299999999;
                    int currIndex = mi1.mInfo.CurrentModelTimeStepIndex + 1;

                    for (long j = 0; j <= NoNetsToCalculate; j++)
                    {
                        if (j < NoNetsToCalculate)
                        {
                            //Actual water flowing throw the link
                            currentDwsFlow = (double)mi2MyMODSIMOutput.LinkOutputQuery(NetworkUtils.ModelOutputSupport.LinkOutputType.Flow, l.number, currIndex);
                            double sumPrevDemands = 0;
                            long revCount2 = j + rh.Lags;
                            for (long calcNets = 0; calcNets < j; calcNets++)
                            {
                                if (revCount2 < rh.R.Length)
                                {
                                    sumPrevDemands += actualWaterDelivered[calcNets] * rh.R[revCount2];
                                }
                                revCount2--;
                            }
                            //This is the actual water delivered to this demand in time "j" in real time
                            if ((currentDwsFlow - sumPrevDemands) > 0)
                            {
                                actualWaterDelivered[j] = (currentDwsFlow - sumPrevDemands) / rh.R[rh.Lags];
                            }
                            else
                            {
                                actualWaterDelivered[j] = 0;
                            }
                            if (revCount < rh.R.Length) // Assumes coeficients with zero values if array size is exceeded.
                            {
                                TempRoutFlow += actualWaterDelivered[j] * rh.R[revCount];
                            }
                        }
                        else
                        {
                            //***** current time step max flow in link
                            if (revCount < rh.R.Length) // Assumes coeficients with zero values if array size is exceeded.
                            {
                                //only calculates links that have min flow specified.
                                if (lmi1.m.min > 0)
                                {
                                    minFlow = TempRoutFlow + (lmi1.m.min * rh.R[revCount]);
                                }
                                else
                                {
                                    minFlow = 0;
                                }
                                if (rh.R[revCount] > 0)
                                {
                                    maxFlow = TempRoutFlow + (lmi1.m.maxConstant * rh.R[revCount]);
                                    //Exclusive options, this loop overwrites the maxflow
                                    for (int k = 0; k < mi1.mInfo.variableCapLinkList.Length; k++)
                                    {
                                        Link lvarCap = mi1.mInfo.variableCapLinkList[k];
                                        if (lvarCap == lmi1)
                                        {
                                            maxFlow = TempRoutFlow + (lmi1.mlInfo.hiVariable[mi2.mInfo.CurrentModelTimeStepIndex, 0] * rh.R[revCount]);
                                        }
                                    }
                                }
                            }

                        }
                        revCount--;
                        currIndex += 1;
                    }
                    l.mlInfo.lo = (long)(minFlow + DefineConstants.ROFF);
                    /* RKL
                    // should exclude varibleCapacity links and owership links
                    RKL */
                    if (!(l.mlInfo.isAccrualLink) && !(l.mlInfo.isLastFillLink))
                    {
                        if (maxFlow > mi1.defaultMaxCap_Super) // 299999999)
                        {
                            maxFlow = mi1.defaultMaxCap_Super;    // 299999999;
                        }
                        l.mlInfo.hi = (long)(maxFlow + DefineConstants.ROFF);
                    }
                }
            }
        }
        public static void SetMinOutflowReservoirs(Model mi1, Model mi2)
        {
            for (long i = mi1.mInfo.resList.Length - 1; i >= 0; i--)
            {
                Node n = mi1.mInfo.resList[i];
                Node n2 = mi2.FindNode(n.number);
                if (n.m.resOutLink != null)
                {
                    if (n2.mnInfo.inflow.Length > 0)
                    {
                        // substraction of the inflow in network 1 and 2 give the value added in CalculateDWSRouting to the reservoir
                        n2.m.resOutLink.mlInfo.lo = n2.mnInfo.inflow[mi2.mInfo.CurrentModelTimeStepIndex, 0] - n.mnInfo.inflow[mi2.mInfo.CurrentModelTimeStepIndex, 0];
                        if (n2.m.resOutLink.mlInfo.hi < n2.m.resOutLink.mlInfo.lo)
                        {
                            n2.m.resOutLink.mlInfo.hi = n2.m.resOutLink.mlInfo.lo;
                        }
                    }
                }
            }
        }
    }

    public class RegionHolder
    {
        public static RegionHolder firstRegion = null;

        public double[] R;
        public RegionHolder nextUpS; //next tracks the routing regions from downstream to upstream (p.e. Reg3->next = Reg2)
        public int regionID; //Identifies the region of the data contained in the Region Holder class.
        public Link dwsLink;
        public bool invertedDirection; //Indicates weather the upstream and downstream nodes must be inverted for back-routing branching calculation.
        public long[] dwsLFlow; //Total flow thru the routing link in downstream time. The array indicate different networks for equivalent future time steps.
        public long[] dwsLFlowNFOnly; //Flow thru the routing link in the natural flow step ( only for reservoir storage handling)
        public double[] MinRTLinkFlow;
        public double[] MinRTLinkFlowNFOnly; //Min flow in realtime for the routing link in downstream time networks after a flow has been placed in previous downstream time net calculation.
        public int posLastNoPositiveLag;
        public int Lags; //Minimum number of lags to be calculated to allocate natural flow.
        public long[] linkPrevflow;
        //long MaxRCoeffNo;				//It's the maximum number of places in the routing coefficient array with a value greater than zero.
        public int nextRegionID; // Stores the number ID of the next region for rotuing regions calculations.

        public RegionHolder()
        {
            //membernodes = 0;
            nextRegionID = 0;
            nextUpS = null;
            regionID = 0;
            dwsLink = null;
            invertedDirection = false;
            posLastNoPositiveLag = 0;
            Lags = 0;
            R = new double[0];
            dwsLFlow = new long[DefineConstants.MAXLAG];
            dwsLFlowNFOnly = new long[DefineConstants.MAXLAG];
            linkPrevflow = new long[DefineConstants.MAXLAG];
            MinRTLinkFlow = new double[DefineConstants.MAXLAG];
            MinRTLinkFlowNFOnly = new double[DefineConstants.MAXLAG];

            for (int i = 0; i < DefineConstants.MAXLAG; i++)
            {
                dwsLFlow[i] = 0;
                dwsLFlowNFOnly[i] = 0;
                MinRTLinkFlow[i] = 0.0;
                MinRTLinkFlowNFOnly[i] = 0.0;
                linkPrevflow[i] = 0;
            }
        }

        public static RegionHolder GetRegion(int ID)
        {
            RegionHolder head = RegionHolder.firstRegion;
            for (; head != null; head = head.nextUpS)
            {
                if (head.regionID == ID)
                {
                    return head;
                }
            }
            return null;
        }

    }
}
