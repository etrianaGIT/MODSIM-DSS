using System;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersSetnet
    {
        public static void addSpillTrackingLinks(Model mi)
        {
            /* spill links in and out for each node */
            /* Note that this code is costly... */
            List<Link> tmpLinks = new List<Link>(mi.mInfo.lList);
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                Link rtnL = mi.AddNewLink(false);
                Utils.ConnectFromNode(rtnL, n);
                Utils.ConnectToNode(rtnL, mi.mInfo.artMassN);
                rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artMassN.name;
                rtnL.mlInfo = new MlInfo();
                rtnL.mlInfo.isArtificial = true;
                rtnL.mlInfo.lo = 0;
                rtnL.mlInfo.hi = mi.defaultMaxCap; //999999;
                rtnL.mlInfo.cost = DefineConstants.COST_LARGER; // 288888888;
                tmpLinks.Add(rtnL);

                n = mi.mInfo.realNodesList[i];
                rtnL = mi.AddNewLink(false);
                Utils.ConnectFromNode(rtnL, mi.mInfo.artMassN);
                Utils.ConnectToNode(rtnL, n);
                rtnL.name = "ArtificialLink_" + mi.mInfo.artMassN.name + "_" + n.name;
                rtnL.mlInfo = new MlInfo(); ;
                rtnL.mlInfo.isArtificial = true;
                rtnL.mlInfo.lo = 0;
                rtnL.mlInfo.hi = mi.defaultMaxCap; //999999;
                rtnL.mlInfo.cost = DefineConstants.COST_LARGER; // 288888888;
                tmpLinks.Add(rtnL);
            }
            mi.mInfo.lList = tmpLinks.ToArray();
            Array.Sort(mi.mInfo.lList);
        }

        /* This function prep's the data right after it's been read by rwModsim,
         * initializing certain values specific for the model.
         */
        public static void prepnet(Model mi)
        {
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.mnInfo.targetcontent.Length > 0)
                {
                    for (int j = 0; j < mi.TimeStepManager.noModelTimeSteps; j++)
                    {
                        int numhs = 1;
                        if (mi.HydStateTables.Length > 0 && n.m.hydTable > 0 && n.mnInfo.targetcontent.Length > mi.TimeStepManager.noModelTimeSteps)
                        {
                            numhs = mi.HydStateTables[n.m.hydTable - 1].NumHydBounds + 1;
                        }
                        for (int h = 0; h < numhs; h++)
                        {
                            n.mnInfo.targetcontent[j, h] = Math.Min(n.mnInfo.targetcontent[j, h], n.m.max_volume);
                        }
                    }
                }
            }
            // TODO: These values need to change dynamically according to the user-selected values.
            //	[ET] If the cost is greater than this value will conflict with zero cost links in reservoirs
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                if (n.nodeType == NodeType.Sink)
                {
                    n.m.demr[0] = DefineConstants.COST_SINK;
                }
            }
        }

        private static void AddHydroTargetArtificialLinks(Model mi)
        {
            if (!mi.hydro.IsActive)
            {
                return;
            }

            // get full list of hydropower targets
            HydropowerTarget[] targets = mi.hydro.HydroTargets;
            if (targets == null || targets.Length == 0)
            {
                return;
            }

            // add links without repitition
            HashSet<Link> links = new HashSet<Link>();
            foreach (HydropowerTarget target in targets)
            {
                foreach (Link link in target.FlowLinks)
                {
                    links.Add(link);
                }
            }

            if (mi.hydro.IterativeTechnique == IterativeSolutionTechnique.Nothing)
            {
                foreach (Link link in links)
                {
                    link.mlInfo.hydroSpillLinks = mi.FindParallelLinks(link, false);
                }
                return;
            }

            // append artificial link to the hydro discharge links...
            //  These will be the new FlowLinks in HydropowerUnits
            foreach (Link link in links)
            {
                Node midNode = mi.AddNewNode(false);
                Link hadd = mi.AddNewLink(false);
                Link hcontrol = mi.AddNewLink(false);
                Link hinflow = mi.AddNewLink(false);
                Link hdirect = mi.AddNewLink(false);
                List<Link> hspillLinks = new List<Link>(mi.FindParallelLinks(link));

                // Link structure
                Utils.ConnectFromNode(hadd, midNode);
                Utils.ConnectToNode(hadd, mi.mInfo.artDemandN);
                Utils.ConnectFromNode(hcontrol, midNode);
                Utils.ConnectToNode(hcontrol, mi.mInfo.artDemandN);
                Utils.ConnectFromNode(hinflow, mi.mInfo.artInflowN);
                Utils.ConnectToNode(hinflow, link.to);
                Utils.ConnectFromNode(hdirect, midNode);
                Utils.ConnectToNode(hdirect, link.to);

                Utils.DisConnectToNode(link);
                Utils.ConnectToNode(link, midNode);
                
                hspillLinks.Remove(link);
                foreach (Link spillLink in hspillLinks)
                {
                    Utils.DisConnectToNode(spillLink);
                    Utils.ConnectToNode(spillLink, mi.mInfo.artDemandN);
                }

                // set info
                midNode.name = "hydroTargetNode" + link.number.ToString();
                midNode.description = "Downstream of a link that has hydropower units";
                midNode.mnInfo = new MnInfo();
                hadd.name = "hydroAdditionalLink" + link.number.ToString();
                hadd.mlInfo = new MlInfo();
                hadd.mrlInfo = new MrlInfo();
                hcontrol.name = "hydroControlLink" + link.number.ToString();
                hcontrol.mlInfo = new MlInfo();
                hcontrol.mrlInfo = new MrlInfo();
                hinflow.name = "hydroInflowLink" + link.number.ToString();
                hinflow.mlInfo = new MlInfo();
                hinflow.mrlInfo = new MrlInfo();
                hdirect.name = "hydroDirect" + link.number.ToString();
                hdirect.mlInfo = new MlInfo();
                hdirect.mrlInfo = new MrlInfo();

                // set costs of new links
                hadd.mlInfo.cost = Math.Abs(link.m.cost);
                hcontrol.mlInfo.cost = link.m.cost;
                hinflow.mlInfo.cost = 0;
                link.mlInfo.cost = -1;
                hadd.m.maxConstant = mi.defaultMaxCap;
                hcontrol.m.maxConstant = 0;
                hinflow.m.maxConstant = 0;
                hadd.mlInfo.hi = hadd.m.maxConstant;
                hcontrol.mlInfo.hi = 0;
                hinflow.mlInfo.hi = 0;
                hdirect.m.maxConstant = mi.defaultMaxCap;
                hdirect.mlInfo.hi= mi.defaultMaxCap;

                // add new links to link.mlInfo
                link.mlInfo.hydroAdditional = hadd;
                link.mlInfo.hydroControl = hcontrol;
                link.mlInfo.hydroInflow = hinflow;
                link.mlInfo.hydroSpillLinks = hspillLinks.ToArray();
                Array.Sort(link.mlInfo.hydroSpillLinks);
            }
        }

        /// <summary>Adds all the artificial links and nodes to the network. Note that it also creates the mass balance nodes associated with each storage link.</summary>
        /// <param name="mi">The model to set artificial links and nodes...</param>
        /// <returns>Returns true if an error occurs; otherwise, false.</returns>
        /// <remarks>
        /// Variables:
        ///   mi->mInfo->artInflowN - A pointer to the artificial inflow node data structure.
        ///   mi->mInfo->artDemandN - A pointer to the artificial demand node data structure.
        ///   mi->mInfo->artStorageN - A pointer to the artificial storage node data structure.
        ///   mi->mInfo->artSpillN - A pointer to the artificial spill node data structure.
        ///   mi->mInfo->artMassN - A pointer to the artificial mass balance node data structure.
        ///   mi->mInfo->artGroundWatN - A pointer to the artificial gwater node data structure.
        ///
        /// Internal optimization notes:
        ///   1. We can avoid making inflow arcs for nodes that are not return flow locations or are inflow locations.  Expect #nodes-#nodes*.90 fewer links.
        ///   2. We can avoid making groundwater links for nodes that have no groundwater needs.  (Both ways...)
        /// </remarks>
        public static bool setnet(Model mi)
        {
            Link rtnL;
            int nperGlover = Math.Max(4, Math.Min(mi.nlags, mi.TimeStepManager.noModelTimeSteps));
            nperGlover = (mi.useLags != 0) ? 0 : (nperGlover + 1);

            // handle the max lags
            mi.maxLags = Math.Min(mi.nlags, DefineConstants.MAXLAG);

            //tmp lists to add nodes/links then set lists to arrays
            List<Node> tmpNodelist = new List<Node>();
            List<Link> tmpLinklist = new List<Link>();
            List<Node> allNodes = new List<Node>();
            List<Link> allLinks = new List<Link>();
            List<Link> ownerLinks = new List<Link>();

            //Initialize max variable before any operation with it is done.
            /* make list of all variable capacity links - unsorted */
            /* This list has all links with non-zero, positive variable capacities */
            tmpLinklist.Clear();
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                if (l.m.maxVariable.getSize() > 0)
                {
                    if (l.m.maxVariable.getSize() > 1)
                    {
                        tmpLinklist.Add(l);
                        l.m.maxConstant = 0;
                    }
                    else
                    {
                        l.m.maxConstant = l.m.maxVariable.getDataL(0, 0);
                    }
                }
                else
                {
                    //This is the default for link capacity.
                    l.m.maxConstant = mi.defaultMaxCap; // 99999999;
                }
            }
            mi.mInfo.variableCapLinkList = tmpLinklist.ToArray();
            Array.Sort(mi.mInfo.variableCapLinkList);

            /* allocate model node information */
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                n.mnInfo = new MnInfo();
            }
            /* allocate model link information */
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                l.mlInfo = new MlInfo();
                l.mrlInfo = new MrlInfo();
            }

            /* We should have an idea of nmown by here? */
            /* copy link cost information for non-accrual links */
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                l.mlInfo.cost = l.m.cost;
                l.mlInfo.lo = l.m.min;
                l.mlInfo.hi = l.m.maxConstant;
            }

            tmpNodelist.Clear();
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                tmpNodelist.Add(n);
            }
            mi.mInfo.realNodesList = tmpNodelist.ToArray();
            Array.Sort(mi.mInfo.realNodesList);

            tmpLinklist.Clear();
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                tmpLinklist.Add(l);
                /* Warning: This makes the assumption that all real links are reaches... */
                l.mlInfo.isReach = true;
            }
            mi.mInfo.realLinkList = tmpLinklist.ToArray();
            Array.Sort(mi.mInfo.realLinkList);

            /* add/connect artificial inflow links */
            mi.mInfo.Initialize(mi);

            /* first add all reservoirs */
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                // RKL consider a new type of link called a storage carryover link;
                // do NOT allow inflows to a reservoir node; need to consider negative evap
                if (n.nodeType == NodeType.Reservoir)
                {
                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, mi.mInfo.artInflowN);
                    Utils.ConnectToNode(rtnL, n);
                    rtnL.name = "ArtificialLink_" + mi.mInfo.artInflowN.name + "_" + n.name;
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mlInfo.isArtificial = true;
                    n.mnInfo.infLink = rtnL;
                }
            }

            // hydropower target artificial links
            AddHydroTargetArtificialLinks(mi);

            /* flow through return locations */
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                // Again consider a different label/name/type of link; keep infLink distict
                if (n.m.idstrmx[0] != null)
                {
                    for (int j = 0; j < 10 && n.m.idstrmx[j] != null; j++)
                    {
                        if (n.m.idstrmx[j].mnInfo.infLink == null)
                        {
                            rtnL = mi.AddNewLink(false);
                            Utils.ConnectFromNode(rtnL, mi.mInfo.artInflowN);
                            Utils.ConnectToNode(rtnL, n.m.idstrmx[j]);
                            rtnL.name = "ArtificialLink_" + mi.mInfo.artInflowN.name + "_" + n.m.idstrmx[j].name;
                            rtnL.mlInfo = new MlInfo();
                            rtnL.mlInfo.isArtificial = true;
                            n.m.idstrmx[j].mnInfo.infLink = rtnL;
                        }
                    }
                }
            }

            /* all real inflow locations create inflow link for each node in network.   */
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                //RKL we should NOT allow inflows to demand and sink nodes
                /* BLounsbury: added constraint per RKL comment above */
                if (n.nodeType != NodeType.Demand & n.nodeType != NodeType.Sink)
                {
                    if (n.mnInfo.infLink == null)
                    {
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, mi.mInfo.artInflowN);
                        Utils.ConnectToNode(rtnL, n);
                        rtnL.name = "ArtificialLink_" + mi.mInfo.artInflowN.name + "_" + n.name;
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        n.mnInfo.infLink = rtnL;
                    }
                }
            }

            /* add/connect artificial target storage links & final storage links */
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                if (n.nodeType == NodeType.Reservoir)
                {
                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, n);
                    Utils.ConnectToNode(rtnL, mi.mInfo.artStorageN);
                    rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artStorageN.name + "_Target";
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mlInfo.isArtificial = true;
                    n.mnInfo.targetLink = rtnL;

                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, n);
                    Utils.ConnectToNode(rtnL, mi.mInfo.artStorageN);
                    rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artStorageN.name + "_Evap";
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mlInfo.isArtificial = true;
                    rtnL.mlInfo.cost = -DefineConstants.COST_LARGE; // 99999999;
                    n.mnInfo.evapLink = rtnL;

                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, n);
                    Utils.ConnectToNode(rtnL, mi.mInfo.artStorageN);
                    rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artStorageN.name + "_ExcessStorage";
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mlInfo.isArtificial = true;
                    n.mnInfo.excessStoLink = rtnL;
                    /* RKL
                    //  add new link parallel to excessStoLink for networks with ownerships
                    // this link would have zero priority and is open in NF step, closed in storage step
                    rtnL = mi->AddNewLink(FALSE);
                    Utils::ConnectFromNode(rtnL, n);
                    Utils::ConnectToNode(rtnL, mi->mInfo->artStorageN);
                    rtnL->mlInfo = new (MlInfo);
                    rtnL->mlInfo->isArtificial = TRUE;
                    n->mnInfo->nfStepStoLink = rtnL;
                    RKL */
                }
            }

            /* add/connect artificial demand links and artificial excess demand links*/
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                if (n.nodeType == NodeType.Demand || n.nodeType == NodeType.Sink)
                {
                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, n);
                    Utils.ConnectToNode(rtnL, mi.mInfo.artDemandN);
                    rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artDemandN.name;
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mlInfo.isArtificial = true;
                    n.mnInfo.demLink = rtnL;
                    if (mi.backRouting)
                    {
                        //backflow link to avoid solver crashes due to combination of
                        //	min flow in demLink and integer calculation numerical errors
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, mi.mInfo.artDemandN);
                        Utils.ConnectToNode(rtnL, n);
                        rtnL.name = "ArtificialLink_" + mi.mInfo.artDemandN.name + "_" + n.name + "_BackFlow";
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        rtnL.mlInfo.cost = DefineConstants.COST_LARGE2; // 98888888;
                        rtnL.mlInfo.hi = 1000;
                        rtnL.mlInfo.minFlowBackRouting = 1; //Temporal Flag for further setnet code changing hi bound of artificial links
                    }
                }
            }

            /* add/connect reservoir spill links */
            if (mi.IncludeSpillLinks)
            {
                for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
                {
                    Node n = mi.mInfo.realNodesList[i];
                    if (n.nodeType == NodeType.Reservoir)
                    {
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, n);
                        Utils.ConnectToNode(rtnL, mi.mInfo.artSpillN);
                        rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artSpillN.name;
                        rtnL.mlInfo = new MlInfo(); ;
                        rtnL.mlInfo.isArtificial = true;
                        n.mnInfo.spillLink = rtnL;
                    }
                }
            }

            /* add/connect artificial groundwater arcs */

            /* gwrtn - pumping (pcap > 0)
            *         return location - node return location or link return location
            */
            /* RKL
            // so whatis the "cost"; does it matter, is the lo set for all these links?
            //  should we have a very hi priority and lo =0?
            RKL */
            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.returnNode != null && l.m.loss_coef > 0 && l.m.loss_coef < 1 && l.m.returnNode.mnInfo.gwrtnLink == null)
                {
                    Node n = l.m.returnNode;
                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, mi.mInfo.artGroundWatN);
                    Utils.ConnectToNode(rtnL, n);
                    rtnL.name = "ArtificialLink_" + mi.mInfo.artGroundWatN.name + "_" + n.name;
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mlInfo.isArtificial = true;
                    rtnL.mlInfo.lo = 0;
                    n.mnInfo.gwrtnLink = rtnL;
                }
            }

            /* return flow location or pumping > 0 */
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                if (n.mnInfo.gwrtnLink == null) //n->m->pcap &&
                {
                    /* RKL
                    // so, this looks kinda bad; any node that is NOT a channel loss link gets a gwrtnLink with
                    // a wired -200000 cost??
                    RKL */
                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, mi.mInfo.artGroundWatN);
                    Utils.ConnectToNode(rtnL, n);
                    rtnL.name = "ArtificialLink_" + mi.mInfo.artGroundWatN.name + "_" + n.name;
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mlInfo.isArtificial = true;
                    /* RKL
                    // may or may not be appropriate cost; we may want to put together a routine at the end of
                    // setnet to get the highest priority for any storage ownership link and set all the evap,
                    // gwrtnLink, etc links at a lower unique cost
                    RKL */
                    rtnL.mlInfo.cost = -DefineConstants.COST_MEDSMALL2; // 2000000;
                    n.mnInfo.gwrtnLink = rtnL;
                }
                int biggestLagForNode = 1; // min value

                int jj;
                LagInfo lInfo;
                for (jj = 0, lInfo = n.m.pumpLagi; lInfo != null; lInfo = lInfo.next, jj++)
                {
                    if (biggestLagForNode < lInfo.numLags)
                    {
                        biggestLagForNode = lInfo.numLags;
                    }
                    if (lInfo.location != null && lInfo.location.mnInfo.gwoutLink == null)
                    {
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, lInfo.location);
                        Utils.ConnectToNode(rtnL, mi.mInfo.artGroundWatN);
                        rtnL.name = "ArtificialLink_" + lInfo.location.name + "_" + mi.mInfo.artGroundWatN.name;
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        /* RKL
                        // so, no indication of a cost here, is lo set or does the cost get set later; should
                        // be defined (or at least commented) what happens here; prefer lo =0 and very high priority
                        RKL */
                        lInfo.location.mnInfo.gwoutLink = rtnL;
                    }
                }

                //WARNING jj == 1 even if we have just one depletion location; this would be more
                // clear if we used if(n->m->pumpLagi)
                if (jj > 0)
                {
                    int lenDim2 = Math.Max(biggestLagForNode, nperGlover);
                    n.mnInfo.demPrevDepn = new long[jj, lenDim2];
                    for (int junki = 0; junki < jj; junki++)
                    {
                        for (int junkj = 0; junkj < lenDim2; junkj++)
                        {
                            n.mnInfo.demPrevDepn[junki, junkj] = 0;
                        }
                    }
                }
                if (n.m.adaInfiltrationsM.getSize() > 0 || n.m.seepg != 0.0)
                {
                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, n);
                    Utils.ConnectToNode(rtnL, mi.mInfo.artGroundWatN);
                    rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artGroundWatN.name;
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mlInfo.isArtificial = true;
                    rtnL.mlInfo.lo = 0;
                    n.mnInfo.gwoutLink = rtnL;
                }
                if ((n.m.adaInfiltrationsM.getSize() > 0 || n.m.seepg != 0.0) && n.m.infLagi != null)
                {
                    // count number of lInfo objects and build arrays
                    biggestLagForNode = 1; // min value
                    for (jj = 0, lInfo = n.m.infLagi; lInfo != null; lInfo = lInfo.next, jj++)
                    {
                        if (biggestLagForNode < lInfo.numLags)
                        {
                            biggestLagForNode = lInfo.numLags;
                        }
                        if (lInfo.location != null && lInfo.location.mnInfo.gwrtnLink == null)
                        {
                            rtnL = mi.AddNewLink(false);
                            Utils.ConnectFromNode(rtnL, mi.mInfo.artGroundWatN);
                            Utils.ConnectToNode(rtnL, lInfo.location);
                            rtnL.name = "ArtificialLink_" + mi.mInfo.artGroundWatN.name + "_" + lInfo.location.name;
                            rtnL.mlInfo = new MlInfo();
                            rtnL.mlInfo.isArtificial = true;
                            /* RKL
                            // cost??? , again betcha lo get's set; I HATE this
                            RKL */
                            lInfo.location.mnInfo.gwrtnLink = rtnL;
                        }
                    }
                    if (jj > 0)
                    {
                        int lenDim2 = Math.Max(biggestLagForNode, nperGlover);

                        n.mnInfo.demPrevFlow = new long[jj, lenDim2];
                        for (int junki = 0; junki < jj; junki++)
                        {
                            for (int junkj = 0; junkj < lenDim2; junkj++)
                            {
                                n.mnInfo.demPrevFlow[junki, junkj] = 0;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                if (n.m.PartialFlows != null)
                {
                    // Check for presence of positive and negative partials
                    int posflag = 0;
                    int negflag = 0;
                    for (int j = 0; j < n.m.PartialFlows.Length; j++)
                    {
                        if (n.m.PartialFlows[j] > 0)
                        {
                            posflag = 1;
                        }
                        if (n.m.PartialFlows[j] < 0)
                        {
                            negflag = 1;
                        }
                    }

                    if (posflag != 0 && n.mnInfo.gwrtnLink == null) // Adds the gw rtn link for partials
                    {
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, mi.mInfo.artGroundWatN);
                        Utils.ConnectToNode(rtnL, n);
                        rtnL.name = "ArtificialLink_" + mi.mInfo.artGroundWatN.name + "_" + n.name;
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        n.mnInfo.gwrtnLink = rtnL;
                    }

                    if (negflag != 0 && n.mnInfo.gwoutLink == null) // Adds the gw out link for partials
                    {
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, n);
                        Utils.ConnectToNode(rtnL, mi.mInfo.artGroundWatN);
                        rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artGroundWatN.name;
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        rtnL.mlInfo.lo = 0;
                        /* RKL
                        // cost may not be appropriate
                        RKL */
                        rtnL.mlInfo.cost = -DefineConstants.COST_MEDSMALL2; // 2000000;
                        n.mnInfo.gwoutLink = rtnL;
                    }
                }
            }

            for (int i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                Link l = mi.mInfo.realLinkList[i];
                if (l.m.loss_coef > 0 && l.m.loss_coef < 1) // gw chanloss link
                {
                    if (l.from.mnInfo.chanLossLink == null) // not created yet
                    {
                        Node n = l.from;
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, n);
                        Utils.ConnectToNode(rtnL, mi.mInfo.artGroundWatN);
                        rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artGroundWatN.name + "_ChannelLoss";
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        rtnL.mlInfo.lo = 0;
                        /* RKL
                        // cost ????????
                        RKL */
                        rtnL.mlInfo.cost = l.m.cost - DefineConstants.COST_LARGE; // -99999999 + l.m.cost;
                        n.mnInfo.chanLossLink = rtnL;
                    }
                }
                else if (l.m.loss_coef >= 1) // routing link
                {
                    if (l.to.mnInfo.routingLink == null) // not created yet
                    {
                        Node n = l.to;
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, n);
                        Utils.ConnectToNode(rtnL, mi.mInfo.artGroundWatN);
                        rtnL.name = "ArtificialLink_" + n.name + "_" + mi.mInfo.artGroundWatN.name + "_Routing";
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        rtnL.mlInfo.lo = 0;
                        // RKL cost
                        rtnL.mlInfo.cost = -DefineConstants.COST_LARGE + 1; // 99999998;
                        n.mnInfo.routingLink = rtnL;
                    }
                    if (l.m.returnNode.mnInfo.infLink == null) // add inflow return if needed
                    {
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, mi.mInfo.artInflowN);
                        Utils.ConnectToNode(rtnL, l.m.returnNode);
                        rtnL.name = "ArtificialLink_" + mi.mInfo.artInflowN.name + "_" + l.m.returnNode.name;
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        l.m.returnNode.mnInfo.infLink = rtnL;
                    }
                }
            }

            /* add/connect artificial mass balance links */
            rtnL = mi.AddNewLink(false);
            Utils.ConnectFromNode(rtnL, mi.mInfo.artStorageN);
            Utils.ConnectToNode(rtnL, mi.mInfo.artMassN);
            rtnL.name = "ArtificialLink_" + mi.mInfo.artStorageN.name + "_" + mi.mInfo.artMassN.name;
            rtnL.mlInfo = new MlInfo();
            rtnL.mlInfo.isArtificial = true;
            mi.mInfo.stoToMassBal = rtnL;

            rtnL = mi.AddNewLink(false);
            Utils.ConnectFromNode(rtnL, mi.mInfo.artSpillN);
            Utils.ConnectToNode(rtnL, mi.mInfo.artMassN);
            rtnL.name = "ArtificialLink_" + mi.mInfo.artSpillN.name + "_" + mi.mInfo.artMassN.name;
            rtnL.mlInfo = new MlInfo();
            rtnL.mlInfo.isArtificial = true;
            mi.mInfo.spillToMassBal = rtnL;

            rtnL = mi.AddNewLink(false);
            Utils.ConnectFromNode(rtnL, mi.mInfo.artDemandN);
            Utils.ConnectToNode(rtnL, mi.mInfo.artMassN);
            rtnL.name = "ArtificialLink_" + mi.mInfo.artDemandN.name + "_" + mi.mInfo.artMassN.name;
            rtnL.mlInfo = new MlInfo();
            rtnL.mlInfo.isArtificial = true;
            mi.mInfo.demToMassBal = rtnL;

            rtnL = mi.AddNewLink(false);
            Utils.ConnectFromNode(rtnL, mi.mInfo.artMassN);
            Utils.ConnectToNode(rtnL, mi.mInfo.artInflowN);
            rtnL.name = "ArtificialLink_" + mi.mInfo.artMassN.name + "_" + mi.mInfo.artInflowN.name;
            rtnL.mlInfo = new MlInfo();
            rtnL.mlInfo.isArtificial = true;
            mi.mInfo.massBalToInf = rtnL;

            rtnL = mi.AddNewLink(false);
            Utils.ConnectFromNode(rtnL, mi.mInfo.artInflowN);
            Utils.ConnectToNode(rtnL, mi.mInfo.artGroundWatN);
            rtnL.name = "ArtificialLink_" + mi.mInfo.artInflowN.name + "_" + mi.mInfo.artGroundWatN.name;
            rtnL.mlInfo = new MlInfo();
            rtnL.mlInfo.isArtificial = true;
            mi.mInfo.infToGwater = rtnL;

            rtnL = mi.AddNewLink(false);
            Utils.ConnectFromNode(rtnL, mi.mInfo.artGroundWatN);
            Utils.ConnectToNode(rtnL, mi.mInfo.artStorageN);
            rtnL.name = "ArtificialLink_" + mi.mInfo.artGroundWatN.name + "_" + mi.mInfo.artStorageN.name;
            rtnL.mlInfo = new MlInfo();
            rtnL.mlInfo.isArtificial = true;
            mi.mInfo.gwaterToInf = rtnL;

            /* make list of all demand nodes - unsorted */
            tmpNodelist.Clear();
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                if (n.nodeType == NodeType.Demand || n.nodeType == NodeType.Sink)
                {
                    tmpNodelist.Add(n);
                }
            }
            mi.mInfo.demList = tmpNodelist.ToArray();
            Array.Sort(mi.mInfo.demList);

            // sanity checking for nodedemand array size and watch logic
            //  we should probably NOT allow the user to have BOTH exchange credit node and watch links
            for (int i = 0; i < mi.mInfo.demList.Length; i++)
            {
                Node n = mi.mInfo.demList[i];
                int hs = 0;
                if (mi.HydStateTables.Length > 0 && n.m.hydTable > 0)
                {
                    hs = mi.HydStateTables[n.m.hydTable - 1].NumHydBounds + 1;
                }
                // jdstrm is exchange credit node
                if (n.m.jdstrm != null && n.mnInfo.nodedemand.Length <= 0)
                {
                    if (n.m.watchMaxLinks != null || n.m.watchMinLinks != null || n.m.watchLnLinks != null || n.m.watchLogLinks != null || n.m.watchExpLinks != null || n.m.watchSqrLinks != null || n.m.watchPowLinks != null)
                    {
                        throw new Exception(" You can't have BOTH exchange credit node AND watch links");
                    }
                    // RKL should we assume one column for demands that have watch logic?
                    //  we CAN have a time series demand that is incremented by watch logic,
                    //  do we want to continue to support this?
                    n.mnInfo.nodedemand = new long[mi.TimeStepManager.noModelTimeSteps, hs];
                    continue;
                }
                // watch links
                if (n.m.watchMaxLinks != null || n.m.watchMinLinks != null || n.m.watchLnLinks != null || n.m.watchLogLinks != null || n.m.watchExpLinks != null || n.m.watchSqrLinks != null || n.m.watchPowLinks != null)
                {
                    n.mnInfo.nodedemand = new long[mi.TimeStepManager.noModelTimeSteps, hs];
                }
            }

            /* make list of all reservoir nodes - unsorted */
            tmpNodelist.Clear();
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                if (n.nodeType == NodeType.Reservoir)
                {
                    tmpNodelist.Add(n);
                }
            }
            mi.mInfo.resList = tmpNodelist.ToArray();
            Array.Sort(mi.mInfo.resList);

            /* make list of all child reservoirs - unsorted */
            /* RKL
            // child reservoir stuff
            RKL */
            tmpNodelist.Clear();
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                if ((n.nodeType == NodeType.Reservoir) && !(n.mnInfo.parent) && n.myMother != null)
                {
                    tmpNodelist.Add(n);
                }
            }
            mi.mInfo.childList = tmpNodelist.ToArray();
            Array.Sort(mi.mInfo.childList);

            /* make list of all parent reservoirs - unsorted */
            tmpNodelist.Clear();
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                if ((n.nodeType == NodeType.Reservoir) && (n.myMother == null) && n.RESnext != n && n.RESnext != null)
                {
                    tmpNodelist.Add(n);
                }
            }
            mi.mInfo.parentList = tmpNodelist.ToArray();
            Array.Sort(mi.mInfo.parentList);

            /* make list of all import nodes - unsorted */
            /* An import node has a non-zero constant import amount */
            /* RKL
            // should not be needed in future since we will be converting all imports to time series
            RKL */
            tmpNodelist.Clear();
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                if (n.m.import > 0)
                {
                    tmpNodelist.Add(n);
                }
            }
            mi.mInfo.importNodes = tmpNodelist.ToArray();
            Array.Sort(mi.mInfo.importNodes);

            /* make list of all inflow nodes - unsorted  - this is equivalent to l1,l2 */
            /* currently all nodes are inflow nodes */
            /* this list can be knocked down significantly... */
            /* remember things such as routing returning as inflows */
            /* RKL
            // I would like ONLY nonstorage nodes to have inflow; and only those with inflow would
            require infLink;  this would mean we MAY need to be able to add this link in scripts??
            RKL */
            tmpNodelist.Clear();
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                if (mi.mInfo.realNodesList[i].mnInfo.infLink != null)
                {
                    tmpNodelist.Add(mi.mInfo.realNodesList[i]);
                }
            }
            mi.mInfo.inflowNodes = tmpNodelist.ToArray();
            Array.Sort(mi.mInfo.inflowNodes);

            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                //if I have an accrual link defined and the accrual link is not myself and I am not artificial
                //  this discludes the groupID links for now
                if (l.m.accrualLink != null && l.m.accrualLink != l && !l.mlInfo.isArtificial)
                {
                    if (l.m.accrualLink.to.myMother != null) // if i am a child reservoir
                    {
                        l.m.accrualLink.to.mnInfo.ownerType = DefineConstants.CHILD_ACCOUNT_RES;
                        l.m.accrualLink.to.myMother.mnInfo.ownerType = DefineConstants.PARENT_ACCOUNT_RES;
                    }
                    else if (l.m.accrualLink.to.m.sysnum == 0)
                    {
                        l.m.accrualLink.to.mnInfo.ownerType = DefineConstants.ZEROSYS_ACCOUNT_RES;
                    }
                    else
                    {
                        l.m.accrualLink.to.mnInfo.ownerType = DefineConstants.NONCH_ACCOUNT_RES;
                    }
                    // so, ownerList includes rent links
                    ownerLinks.Add(l);
                    l.mlInfo.isOwnerLink = true;
                    int tableNumber = l.m.hydTable;
                    int numstates;
                    if (mi.HydStateTables.Length == 0 || tableNumber == 0)
                    {
                        numstates = 1;
                    }
                    else
                    {
                        numstates = mi.HydStateTables[tableNumber - 1].NumHydBounds + 1;
                    }
                    for (int j = 0; j < numstates; j++)
                    {
                        if (l.m.rentLimit[j] < 0) // this is a rental
                        {
                            l.mrlInfo.irent = -1;
                            l.mrlInfo.contribRent = 0;
                        }
                        else if (l.m.rentLimit[j] > 0) // this is a contributor
                        {
                            l.mrlInfo.irent = 1;
                            l.mrlInfo.contribRent = 0;
                            l.mrlInfo.contribLast = 0;
                        }
                    }
                }
            }

            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.PARENT_ACCOUNT_RES)
                {
                    if (n.m.resBypassL == null && n.mnInfo.ownerType != DefineConstants.CHILD_ACCOUNT_RES)
                    {
                        mi.FireOnError(string.Concat("Need a bypass link for node ", n.name));
                        return true; //throw new Exception("Account reservoir must have a bypass link");
                    }
                    if (n.m.resOutLink == null && n.mnInfo.ownerType != DefineConstants.PARENT_ACCOUNT_RES)
                    {
                        mi.FireOnError(string.Concat("Need an outflow link for node ", n.name));
                        return true; //throw new Exception("Account reservoir must have an outflow link");
                    }
                }
            }

            /* flow through bypass credit links under storage ownership logic */
            if (ownerLinks.Count > 0)
            {
                for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
                {
                    Node n = mi.mInfo.realNodesList[i];

                    // Add the flow through direct return link if we have a bypass credit
                    /* RKL
                    // idstrmx is a return node location, pdstrm is the bypass credit link
                    // In a network with ownership links defined, ANY flow through demand with a bypass credit link
                    //  gets the flowThruReturnLink defined (not just flow through demands with ownership links)
                    RKL */
                    int cnt = 0;
                    for (int k = 0; k < 10; k++)
                    {
                        if (n.m.idstrmx[k] != null)
                        {
                            cnt++;
                        }
                    }

                    if (cnt > 0 && n.m.pdstrm != null)
                    {
                        // Check to see if more than one return location exists
                        if (cnt > 1)
                        {
                            string str = string.Format("Error: Node '{0}' ({1:D}) contains bypass link and more than one return location\n", n.name, n.number);
                            GlobalMembersModsim.ModelStatusMsg(str);
                            return true;
                        }

                        for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                        {
                            if (ll.link.m.accrualLink != null)
                            {
                                // Set the storage flow only flag
                                // This seems odd, for ownerships to flow thru demands, should this be
                                //   the flag2ndStorageStepOnly?
                                ll.link.m.flagStorageStepOnly = 1;
                            }
                        }
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, n);
                        Utils.ConnectToNode(rtnL, n.m.idstrmx[0]);
                        rtnL.name = "ArtificialLink_" + n.name + "_" + n.m.idstrmx[0].name;
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        rtnL.mlInfo.hi = 0;
                        rtnL.mlInfo.lo = 0;
                        rtnL.mlInfo.cost = 0;
                        n.mnInfo.flowThruReturnLink = rtnL;
                    }
                }
            }

            /* We can verify that we have ownerships before we set the operation
            * of last fill links or resout links.
            */
            tmpLinklist.Clear();
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                n.mnInfo.resLastFillLink = null;
                if (n.mnInfo.ownerType == DefineConstants.CHILD_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.NONCH_ACCOUNT_RES || n.mnInfo.ownerType == DefineConstants.ZEROSYS_ACCOUNT_RES)
                {
                    for (LinkList holdLL = n.InflowLinks; holdLL != null; holdLL = holdLL.next)
                    {
                        Link l = holdLL.link;
                        if (n.m.lastFillLink == l)
                        {
                            n.mnInfo.resLastFillLink = l;
                            l.mlInfo.isLastFillLink = true;
                            l.mlInfo.hi = 0;
                            l.mlInfo.lo = 0;
                            l.mlInfo.flow = 0;
                            tmpLinklist.Add(l);
                        }
                    }
                }
            }
            mi.mInfo.lastFillLinkList = tmpLinklist.ToArray();
            Array.Sort(mi.mInfo.lastFillLinkList);

            /* make connections from parent links to all their children */
            /* Note that accrual link designation requires that parents be
            * connected to a reservoir on their outflow.
            */
            /* Note that this code sets child connections for all links that
            * match the ownership criteria above.  Possibly need to make
            * sure that the "to" end of the link needs to connect to a res.
            */
            for (int i = 0; i < ownerLinks.Count; i++)
            {
                Link l = ownerLinks[i];
                Link l2 = l.m.accrualLink; // interface "parent" link field
                l2.mlInfo.isAccrualLink = (l2.to.nodeType == NodeType.Reservoir); // This sets the accrual link model data structure flag to true
                /* make accrual separate of seasonal capacity links */
                if (l2.mlInfo.isAccrualLink && l2.m.lnkallow != 0)
                {
                    l2.mrlInfo.lnkSeasStorageCap = l2.m.lnkallow;
                    l2.m.lnkallow = 0;
                }
                if (l.mrlInfo.irent < 0)
                {
                    LinkList holdLL = new LinkList();
                    holdLL.next = l.m.accrualLink.mlInfo.rLinkL;
                    holdLL.link = l;
                    l.m.accrualLink.mlInfo.rLinkL = holdLL;
                }
                else
                {
                    LinkList holdLL = new LinkList();
                    holdLL.next = l2.mlInfo.cLinkL;
                    holdLL.link = l;
                    l2.mlInfo.cLinkL = holdLL;
                    long groupCapOwned = (l.m.groupNumber == 0) ? 0 : l.m.accrualLink.m.stgAmount[l.m.groupNumber - 1];
                    l.m.lnkallow = Math.Max(l.m.capacityOwned, groupCapOwned);
                }
            }

            /* make list of all accrual links - unsorted */
            /* an accrual link is an inflow to an account reservoir with a negative
            * cost.  There should be links referring to this link as their parent.
            * We use the child link pointers to determine if this link is a
            * parent link.
            */
            tmpLinklist.Clear();
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                /* RKL
                // do we really want to be limited to negative costs???
                RKL */
                if (l.to.nodeType == NodeType.Reservoir && l.m.cost < 0 && l.mlInfo.cLinkL != null)
                {
                    tmpLinklist.Add(l);
                }
            }
            mi.mInfo.accrualLinkList = tmpLinklist.ToArray();
            Array.Sort(mi.mInfo.accrualLinkList);

            /* This list starts from one */
            allNodes.Add(null); //BLounsbury: hate to do this but relax4 is setup this way
            for (Node n = mi.firstNode; n != null; n = n.next)
            {
                allNodes.Add(n);
            }

            /* This list starts from one */
            allLinks.Add(null); //BLounsbury: hate to do this but relax4 is setup this way
            for (Link l = mi.firstLink; l != null; l = l.next)
            {
                allLinks.Add(l);
                if (l.mlInfo.isArtificial)
                {
                    l.mlInfo.lo = 0;
                    //Backrouting flag to avoid changing upper limit in the back flow link
                    if (l.mlInfo.minFlowBackRouting == 0)
                    {
                        l.mlInfo.hi = 0;
                    }
                    else
                    {
                        l.mlInfo.minFlowBackRouting = 0;
                    }
                }
                l.mlInfo.flow = l.mlInfo.lo;
            }

            /* copy parent flag over to mnInfo structure */
            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                n.mnInfo.parent = n.parentFlag;
                n.mnInfo.child = !n.parentFlag;
            }

            GlobalMembersSetnet.CopyCapown(ownerLinks);

            // Group Ownership Links
            Node rtnN = mi.AddNewNode(false);
            rtnN.mnInfo = new MnInfo();
            rtnN.name = "ArtificialNode_Group";
            mi.mInfo.artGroupN = rtnN;
            allNodes.Add(rtnN);
            /* RKL
            // I don't understand why accrualLinkList->Length is different than accuralLinkListLen
            // This should be cleaned up so we don't have two incompatable array lengths
            RKL */
            for (int i = 0; i < mi.mInfo.accrualLinkList.Length; i++)
            {
                Link l = mi.mInfo.accrualLinkList[i];
                // If it has a group, process
                for (int j = 1; j <= l.m.numberOfGroups; j++)
                {
                    // Make a fake ownerships
                    rtnL = mi.AddNewLink(false);
                    Utils.ConnectFromNode(rtnL, mi.mInfo.artGroupN);
                    Utils.ConnectToNode(rtnL, mi.mInfo.artGroupN);
                    rtnL.name = l.name + ".Group" + j.ToString();
                    rtnL.mlInfo = new MlInfo();
                    rtnL.mrlInfo = new MrlInfo();
                    rtnL.mrlInfo.cap_own = l.m.stgAmount[j - 1];
                    rtnL.mrlInfo.prevownacrul = l.m.initStglft[j - 1];
                    rtnL.mrlInfo.own_accrual = l.m.initStglft[j - 1];
                    rtnL.mrlInfo.prevstglft = l.m.initStglft[j - 1];
                    rtnL.mrlInfo.stglft = l.m.initStglft[j - 1];
                    rtnL.m.accrualLink = l;
                    rtnL.mrlInfo.groupID = j;
                    // Add to cLinkL
                    LinkList ll = new LinkList();
                    ll.next = l.mlInfo.cLinkL;
                    ll.link = rtnL;
                    l.mlInfo.cLinkL = ll;
                    // NOTE no prevision for rental group ownership links
                    ownerLinks.Add(rtnL);
                    allLinks.Add(rtnL);
                }
            }
            // For child reservoirs with children with targets, we need to check
            // the topology.  If the inflow is disconnected, we need to connnect
            // the target links to a node so the summation constraint of the
            // parent target is possible.
            DateTime now;
            DateTime firstDate = mi.TimeStepManager.dataStartDate;
            for (int i = 0; i < mi.mInfo.parentList.Length; i++)
            {
                Node n = mi.mInfo.parentList[i];
                int parHasAllChildTargets = 1;
                bool foundChildWOtargets = false;
                Node connectN = null;
                int connFlag = 1;
                for (int j = 0; j < mi.mInfo.childList.Length; j++)
                {
                    Node nChild = mi.mInfo.childList[j];
                    bool childHasTargets = false;
                    int numhs = 1;
                    if (mi.HydStateTables.Length > 0 && n.m.hydTable > 0)
                    {
                        numhs = mi.HydStateTables[n.m.hydTable - 1].NumHydBounds + 1;
                    }
                    if (nChild.myMother == n)
                    {
                        if (nChild.m.adaTargetsM.getSize() > 0)
                        {
                            now = mi.TimeStepManager.startingDate;
                            for (int ti = 0; ti < mi.TimeStepManager.noModelTimeSteps; ti++)
                            {
                                for (int hi = 0; hi < numhs; hi++)
                                {
                                    nChild.mnInfo.hydStateIndex = hi;
                                    int tsi = nChild.m.adaTargetsM.GetTsIndex(now);
                                    if (nChild.m.adaTargetsM.getDataL(tsi, hi) > 0)
                                    {
                                        childHasTargets = true;
                                        nChild.mnInfo.targetExists = true;
                                        break;
                                    }
                                }
                                if (childHasTargets)
                                {
                                    break;
                                }
                                now = mi.TimeStepManager.GetNextIniDate(now);
                            }
                            if (!childHasTargets)
                            {
                                foundChildWOtargets = true;
                            }
                        }
                        else
                        {
                            foundChildWOtargets = true;

                        }
                        for (LinkList ll = nChild.InflowLinks; ll != null; ll = ll.next)
                        {
                            if (ll.link.mlInfo.isAccrualLink)
                            {
                                if (connectN == null)
                                {
                                    connectN = ll.link.from;
                                    break;
                                }
                                else if (connectN != ll.link.from)
                                {
                                    connFlag = 0;
                                    break;
                                }
                            }
                        }
                    }
                    // If we have all child targ's & we are not connected to the same inflow,
                    // we need to connect the target links for the correct target/accrual
                    // operation.
                    if (foundChildWOtargets)
                    {
                        parHasAllChildTargets = 0;
                    }
                    if (parHasAllChildTargets != 0 && connFlag == 0)
                    {
                        // add Summation node
                        rtnN = mi.AddNewNode(false);
                        rtnN.mnInfo = new MnInfo();
                        rtnN.name = "ArtificialNode_Summation";
                        allNodes.Add(rtnN);
                        // Add link to artificial storage node
                        // Real link added first to handle relax bug
                        rtnL = mi.AddNewLink(false);
                        Utils.ConnectFromNode(rtnL, rtnN);
                        Utils.ConnectToNode(rtnL, mi.mInfo.artStorageN);
                        rtnL.name = "ArtificialLink_" + rtnN.name + "_" + mi.mInfo.artStorageN.name;
                        rtnL.mlInfo = new MlInfo();
                        rtnL.mlInfo.isArtificial = true;
                        rtnL.mlInfo.lo = 0;
                        rtnL.mlInfo.hi = mi.defaultMaxCap; // 99999999;
                        rtnL.mlInfo.cost = -1; // /* RKL cost = -1 ??????????
                        allLinks.Add(rtnL);
                        for (int k = 0; k < mi.mInfo.childList.Length; k++)
                        {
                            nChild = mi.mInfo.childList[k];
                            if (nChild.myMother == n)
                            {
                                Utils.DisConnectToNode(nChild.mnInfo.targetLink);
                                Utils.ConnectToNode(nChild.mnInfo.targetLink, rtnN);
                                nChild.mnInfo.targetLink.name = "ArtificialLink_" + nChild.mnInfo.targetLink.from.name + "_" + rtnN.name;
                                Utils.DisConnectToNode(nChild.mnInfo.excessStoLink);
                                Utils.ConnectToNode(nChild.mnInfo.excessStoLink, rtnN);
                                nChild.mnInfo.excessStoLink.name = "ArtificialLink_" + nChild.mnInfo.excessStoLink.from.name + "_" + rtnN.name;
                            }
                        }
                    }
                }
            }

            // Add links to handle reservoir balancing construct
            // Check all nodes to see if they fall within the categories of
            // nodes that can represent this construct.
            // We are let off the calculation if we don't have a resBalance.
            /* RKL
            // we should enforce that we MUST have at least ONE reservoir layer and so n->m->resBalance is always true for a reservoir
            //  n->m->resBalance->incrPriorities->Length >= 1
            RKL */
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                /* RKL
                // so we don't need to worry about the double subscript of incrPriorities
                RKL */
                if (n.m.min_volume > 0 && n.m.resBalance == null)
                {
                    /* AQD
                        This "if" statement needed to be placed before the following "if" statement because
                        the following if statement always assumes that resBalance is already instantiated
                    AQD */
                    //Create a reservoir balance layer for minimum volume
                    n.m.resBalance = new ResBalance();
                    n.m.resBalance.PercentBasedOnMaxCapacity = true;
                    n.m.resBalance.targetPercentages = new double[1];
                    n.m.resBalance.targetPercentages[0] = 100F;
                    n.m.resBalance.incrPriorities = new long[1];
                    n.m.resBalance.incrPriorities[0] = 0;
                }
                if ((n.m.min_volume > 0 || n.m.resBalance != null) && n.m.resBalance.targetPercentages.Length > 0)
                {
                    //Add minimum reservoir volume layer and sort the existing layer to assure correct functioning of the algorithm.
                    int index;
                    int currSize = n.m.resBalance.targetPercentages.Length;
                    bool first;
                    double[] tempPercentages;
                    long[] tempincPriorities;

                    bool backwards = false;
                    if (n.m.resBalance.targetPercentages[currSize - 1] < n.m.resBalance.targetPercentages[0])
                    {
                        backwards = true;
                    }
                    if (n.m.min_volume > 0)
                    {
                        //Initialize varaibles to accomodate an additional layer for the min volume
                        first = true;
                        index = -1;
                        tempPercentages = new double[currSize + 1];
                        tempincPriorities = new long[currSize + 1];
                        if (backwards)
                        {
                            index = currSize;
                        }
                    }
                    else
                    {
                        //Initialize variable for backwards arrays sorting.
                        first = false;
                        index = 0;
                        tempPercentages = new double[currSize];
                        tempincPriorities = new long[currSize];
                        if (backwards)
                        {
                            index = currSize - 1;
                        }
                    }
                    //This could be skipped if no minimum and backwards = False.  It's not big overhead at this point.
                    for (int perc = 0; perc < tempPercentages.Length; perc++)
                    {
                        if (first)
                        {
                            tempPercentages[0] = 0.0; //It will be calculated when setting link bounds.
                            tempincPriorities[0] = -DefineConstants.COST_LARGE3; // 98999999;
                        }
                        else
                        {
                            tempPercentages[perc] = n.m.resBalance.targetPercentages[index];
                            tempincPriorities[perc] = n.m.resBalance.incrPriorities[index];
                        }
                        first = false;
                        if (backwards)
                        {
                            index--;
                        }
                        else
                        {
                            index++;
                        }
                    }
                    n.m.resBalance.targetPercentages = tempPercentages;
                    n.m.resBalance.incrPriorities = tempincPriorities;
                }
                if (n.m.resBalance != null && n.m.resBalance.targetPercentages.Length > 0)
                {
                    bool hasOwnerLinks = false;
                    for (Link l = mi.firstLink; l != null; l = l.next)
                    {
                        if (l.m.accrualLink != null && l.m.accrualLink.to == n)
                        {
                            hasOwnerLinks = true;
                        }
                    }
                    /* RKL
                    //  This looks bad, looks like we only create the resBalanceNode and balanceLinks if we have
                    //   hasOwnerLinks > 0; we need to remove this dependency
                    RKL */
                    if (n.nodeType == NodeType.Reservoir && !((n.numChildren > 0) || (n.myMother != null && hasOwnerLinks && n.m.sysnum == 0)))
                    {
                        rtnN = mi.AddNewNode(false);
                        rtnN.mnInfo = new MnInfo();
                        rtnN.name = "ArtificialNode_ReservoirBalance";
                        n.mnInfo.resBalanceNode = rtnN;
                        allNodes.Add(rtnN);
                        // Create the balance links - manipulated in operate.
                        /* RKL
                        // for(j = 0; j < n->m->resBalance->targetPercentages->Length; j++)
                        // so we don't need to worry about the double subscript of incrPriorities
                        //  when we wish to have incrPriorities for each hydrologic state instead of various
                        //  priorities (costs) on the artificial target storage link
                        // We will have to enforce having at least one "reservoir level"
                        RKL */
                        for (int j = 0; j < n.m.resBalance.incrPriorities.Length; j++)
                        {
                            LinkList newLL = new LinkList(); // (LinkList *) mcalloc(1,sizeof(LinkList));
                            newLL.next = n.mnInfo.balanceLinks;
                            n.mnInfo.balanceLinks = newLL;
                            newLL.link = mi.AddNewLink(false);
                            newLL.link.mlInfo = new MlInfo();
                            newLL.link.mlInfo.isArtificial = true;
                            newLL.link.mlInfo.cost = 0;
                            newLL.link.mlInfo.hi = mi.defaultMaxCap; //99999999;
                            Utils.ConnectFromNode(newLL.link, n);
                            Utils.ConnectToNode(newLL.link, rtnN);
                            newLL.link.name = "ArtificialLink_" + n.name + "_" + rtnN.name + "_" + (n.m.resBalance.incrPriorities.Length>1 && j== n.m.resBalance.incrPriorities.Length -1? "Min":j.ToString());
                            allLinks.Add(newLL.link);
                        }
                        /* RKL
                        // This link should not be needed; "min" res capacity should be handeled with level with very high priority
                        RKL */
                        n.mnInfo.outLetL = mi.AddNewLink(false);
                        n.mnInfo.outLetL.mlInfo = new MlInfo();
                        n.mnInfo.outLetL.mlInfo.isArtificial = true;
                        Utils.ConnectFromNode(n.mnInfo.outLetL, n);
                        Utils.ConnectToNode(n.mnInfo.outLetL, rtnN);
                        n.mnInfo.outLetL.name = "ArtificialLink_" + n.name + "_" + rtnN.name;
                        n.mnInfo.outLetL.mlInfo.hi = mi.defaultMaxCap; //999999999;
                        // RKL set this cost to zero; better yet get rid of this link
                        n.mnInfo.outLetL.mlInfo.cost = DefineConstants.COST_SMALLER; // 399999; // Needs to balance Ex Link
                        allLinks.Add(n.mnInfo.outLetL);
                        // After all that, disconnect and reconnect the target link
                        Utils.DisConnectFromNode(n.mnInfo.targetLink);
                        Utils.ConnectFromNode(n.mnInfo.targetLink, rtnN);
                    }
                }
            }

            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                n.mnInfo.targetLink.mlInfo.lo = 0; // This should NEVER be anything but zero; put a hi priority on a layer
            }

            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                if (n.mnInfo.child)
                {
                    continue;
                }
                // Check for Hydraulic Capacity non zero value in table.
                int foundHC = 0;
                for (int j = 0; j < n.m.hpoints.Length; j++)
                {
                    if (n.m.hpoints[j] > 0)
                    {
                        foundHC++;
                        break;
                    }
                }

                if (foundHC != 0)
                {
                    Node outputNodeParent = null;

                    // If we have Hydraulic Capacity, decide on reservoir type and add
                    // the artificial node and move the links.
                    /* RKL
                    //  child Reservoir stuff
                    RKL */
                    if (n.mnInfo.parent && n.RESnext != n) // Parent Reservoirs with Children
                    {
                        // look for bypass link
                        if (n.m.resBypassL != null)
                        {
                            outputNodeParent = n.m.resBypassL.to;
                        }

                        // look for all children
                        for (int j = 0; j < mi.mInfo.childList.Length; j++)
                        {
                            Node n2 = mi.mInfo.childList[j];
                            if (n2.myMother == n)
                            {
                                // Find all outflow links that connect to the downstream node
                                Node outputNode = null;
                                if (n2.m.resOutLink != null)
                                {
                                    outputNode = n2.m.resOutLink.to;
                                }
                                else
                                {
                                    for (LinkList ll = n2.OutflowLinks; ll != null; ll = ll.next)
                                    {
                                        if (ll.link.mlInfo.isArtificial)
                                        {
                                            continue;
                                        }
                                        if (outputNode != null)
                                        {
                                            if (outputNode != ll.link.to)
                                            {
                                                // Error to user
                                                outputNode = null;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            outputNode = ll.link.to;
                                        }
                                    }
                                }
                                if ((outputNodeParent != outputNode) && outputNodeParent != null)
                                {
                                    // Error
                                    outputNodeParent = null;
                                    break;
                                }
                                else
                                {
                                    outputNodeParent = outputNode;
                                }
                            }
                        }

                        if (outputNodeParent != null)
                        {
                            // create the hydraulic capacity node
                            n.mnInfo.hydraulicCapNode = mi.AddNewNode(false);
                            n.mnInfo.hydraulicCapNode.mnInfo = new MnInfo();
                            n.mnInfo.hydraulicCapNode.name = "ArtificialNode_HydraulicCap";
                            allNodes.Add(n.mnInfo.hydraulicCapNode);

                            // move the links
                            for (LinkList ll = outputNodeParent.InflowLinks; ll != null; ll = ll.next)
                            {
                                if (ll.link.mlInfo.isArtificial || (n.m.resBypassL != ll.link && (ll.link.from.nodeType != NodeType.Reservoir || ll.link.from.myMother != n)))
                                {
                                    continue;
                                }
                                rtnL = ll.link;
                                Utils.DisConnectToNode(rtnL);
                                Utils.ConnectToNode(rtnL, n.mnInfo.hydraulicCapNode);
                                ll = outputNodeParent.InflowLinks;
                            }
                            // Connect the HC link from HC node to the original node
                            n.mnInfo.hydraulicCapLink = mi.AddNewLink(false);
                            n.mnInfo.hydraulicCapLink.mlInfo = new MlInfo();
                            n.mnInfo.hydraulicCapLink.mlInfo.isArtificial = true;
                            Utils.ConnectFromNode(n.mnInfo.hydraulicCapLink, n.mnInfo.hydraulicCapNode);
                            Utils.ConnectToNode(n.mnInfo.hydraulicCapLink, outputNodeParent);
                            n.mnInfo.hydraulicCapLink.name = "ArtificialLink_" + n.mnInfo.hydraulicCapNode.name + "_" + outputNodeParent.name;
                            allLinks.Add(n.mnInfo.hydraulicCapLink);
                        }
                    }
                    /* RKL
                    //  end of child reservoir stuff for hydraulic capacity
                    RKL */
                    else // Non-Child Reservoirs
                    {
                        // look for bypass link
                        if (n.m.resOutLink != null)
                        {
                            outputNodeParent = n.m.resOutLink.to;
                        }

                        if (outputNodeParent != null)
                        {
                            // create the hydraulic capacity node
                            n.mnInfo.hydraulicCapNode = mi.AddNewNode(false);
                            n.mnInfo.hydraulicCapNode.mnInfo = new MnInfo();
                            n.mnInfo.hydraulicCapNode.name = "ArtificialNode_HydraulicCapOutflow";
                            allNodes.Add(n.mnInfo.hydraulicCapNode);

                            // move the links (outflow and bypass links only)
                            for (LinkList ll = outputNodeParent.InflowLinks; ll != null; ll = ll.next)
                            {
                                if ((ll.link == n.m.resBypassL) || (ll.link == n.m.resOutLink))
                                {
                                    rtnL = ll.link;
                                    Utils.DisConnectToNode(rtnL);
                                    Utils.ConnectToNode(rtnL, n.mnInfo.hydraulicCapNode);
                                    ll = outputNodeParent.InflowLinks;
                                }
                            }
                            // Connect the HC link from HC node to the original node
                            n.mnInfo.hydraulicCapLink = mi.AddNewLink(false);
                            n.mnInfo.hydraulicCapLink.mlInfo = new MlInfo();
                            n.mnInfo.hydraulicCapLink.mlInfo.isArtificial = false;
                            n.mnInfo.hydraulicCapLink.mrlInfo = new MrlInfo();
                            Utils.ConnectFromNode(n.mnInfo.hydraulicCapLink, n.mnInfo.hydraulicCapNode);
                            Utils.ConnectToNode(n.mnInfo.hydraulicCapLink, outputNodeParent);
                            n.mnInfo.hydraulicCapLink.name = "ArtificialLink_" + n.mnInfo.hydraulicCapNode.name + "_" + outputNodeParent.name;
                            allLinks.Add(n.mnInfo.hydraulicCapLink);
                        }
                    }
                    // walk through the demand nodes
                    // if the demand node is linked from the ouputNodeParent then
                    //    if the demand node m->demDirect is this reservoir node then
                    //        for each real link to the demand DisConnectFromNode and
                    //                 ConnectFromNode to the hydraulicCapNode
                    if (n.mnInfo.hydraulicCapNode != null)
                    {
                        for (int k = 0; k < mi.mInfo.demList.Length; k++)
                        {
                            Node dem = mi.mInfo.demList[k];
                            if (dem.m.demDirect != null && dem.m.demDirect == n)
                            {
                                for (LinkList ll = dem.InflowLinks; ll != null; ll = ll.next)
                                {
                                    Link l = ll.link;
                                    if (!l.mlInfo.isArtificial && l.from == outputNodeParent)
                                    {
                                        Utils.DisConnectFromNode(l);
                                        Utils.ConnectFromNode(l, n.mnInfo.hydraulicCapNode);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // RKL: What are these 5 links for and the cost of 10???
            // Put fake link in.
            for (int fake = 0; fake < 5; fake++)
            {
                rtnL = mi.AddNewLink(false);
                rtnL.mlInfo = new MlInfo();
                Utils.ConnectFromNode(rtnL, mi.mInfo.artStorageN);
                Utils.ConnectToNode(rtnL, mi.mInfo.artStorageN);
                rtnL.name = "ArtificialLink_" + mi.mInfo.artStorageN.name + "_" + mi.mInfo.artStorageN.name + "_Fake" + (fake + 1).ToString();
                rtnL.mlInfo.isArtificial = true;
                rtnL.mlInfo.lo = 0;
                rtnL.mlInfo.hi = mi.defaultMaxCap; //9999999;
                rtnL.mlInfo.cost = 10;
                allLinks.Add(rtnL);
            }

            for (int i = 0; i < mi.mInfo.realNodesList.Length; i++)
            {
                Node n = mi.mInfo.realNodesList[i];
                n.mnInfo.irtnflowthruSTG = 0;
                n.mnInfo.irtnflowthruNF = 0;
                n.mnInfo.iroutreturn = 0;
                n.mnInfo.artFlowthruSTG = 0;
                n.mnInfo.artFlowthruNF = 0;
            }

            GlobalMembersSetnet.NetworkTraceFTDemands(mi, allNodes, allLinks);

            // Build final list of all nodes/links/ownerlinks
            mi.mInfo.nList = allNodes.ToArray();
            mi.mInfo.lList = allLinks.ToArray();
            mi.mInfo.ownerList = ownerLinks.ToArray();
            Array.Sort(mi.mInfo.nList);
            Array.Sort(mi.mInfo.lList);
            Array.Sort(mi.mInfo.ownerList);

            // Build output control list for links
            tmpLinklist.Clear();
            for (int i = 1; i < mi.mInfo.lList.Length; i++)
            {
                Link l = mi.mInfo.lList[i];
                if ((!l.mlInfo.isArtificial) && (l.m.selected || (mi.iplot == 0)))
                {
                    tmpLinklist.Add(l);
                }
            }
            mi.mInfo.outputLinkList = tmpLinklist.ToArray();
            Array.Sort(mi.mInfo.outputLinkList);

            // Build output control list for nodes
            tmpNodelist.Clear();
            for (int i = 1; i < mi.mInfo.nList.Length; i++)
            {
                Node n = mi.mInfo.nList[i];
                if ((n.nodeType > NodeType.Undefined) && (n.m.selected || (mi.iplot == 0)))
                {
                    tmpNodelist.Add(n);
                }
            }
            mi.mInfo.outputNodeList = tmpNodelist.ToArray();
            Array.Sort(mi.mInfo.outputNodeList);

            // Detect flowthrus with ownership links.  Set mInfo FT status flag.
            mi.mInfo.hasFTOwners = false;
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                if (l.to.nodeType == NodeType.Demand)
                {
                    // Found an owner to a demand.  Check for FT status
                    Node n = l.to;
                    if ((n.m.idstrmx[0] != null || n.m.idstrmx[1] != null || n.m.idstrmx[2] != null || n.m.idstrmx[3] != null || n.m.idstrmx[4] != null || n.m.idstrmx[5] != null || n.m.idstrmx[6] != null || n.m.idstrmx[7] != null || n.m.idstrmx[8] != null || n.m.idstrmx[9] != null) && n.m.pdstrm != null)
                    {
                        // Ok.
                        mi.mInfo.hasFTOwners = true;
                    }
                }
            }

            // Generate list of channel loss links attributable to storage owner links
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                if (l.m.linkChannelLoss != null)
                {
                    l.m.linkChannelLoss.m.touchedSorted = 0;
                }
            }

            tmpLinklist.Clear();
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                if (l.m.linkChannelLoss != null && l.m.linkChannelLoss.m.touchedSorted == 0)
                {
                    tmpLinklist.Add(l.m.linkChannelLoss);
                    l.m.linkChannelLoss.m.touchedSorted = 1;
                }
            }
            mi.mInfo.ownerChanlossList = tmpLinklist.ToArray();
            Array.Sort(mi.mInfo.ownerChanlossList);

            // Sanity check for last fill link
            short iquit = 0;
            for (int i = 0; i < mi.mInfo.resList.Length; i++)
            {
                Node n = mi.mInfo.resList[i];
                for (LinkList ll = n.InflowLinks; ll != null; ll = ll.next)
                {
                    Link l = ll.link;
                    if (l.mlInfo.isAccrualLink)
                    {
                        for (LinkList ll2 = l.mlInfo.cLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            if (ll2.link.m.lastFill != 0 && n.mnInfo.resLastFillLink == null)
                            {
                                string str = "Reservoir " + n.name + " has last fill contributor " + ll2.link.name + " but no last fill link\n";
                                GlobalMembersModsim.ModelStatusMsg(str);
                                iquit = 1;
                            }
                        } // end of for loop on child links
                        for (LinkList ll2 = l.mlInfo.rLinkL; ll2 != null; ll2 = ll2.next)
                        {
                            if (ll2.link.m.lastFill != 0 && n.mnInfo.resLastFillLink == null)
                            {
                                GlobalMembersModsim.ModelStatusMsg("Reservoir " + n.name + " has last fill renter " + ll2.link.name + " but no last fill link\n");
                                iquit = 1;
                            }
                        }
                    }
                }
            }

            if (iquit != 0)
            {
                return true;
            }

            return false;
        }

        public static void CopyCapown(List<Link> ownerList)
        {
            for (int i = 0; i < ownerList.Count; i++)
            {
                Link l = ownerList[i];
                l.mrlInfo.cap_own = l.m.capacityOwned;
                // This was all done for the artificial group ownership links when they were created
                if (l.mrlInfo.groupID == 0 && l.m.groupNumber == 0)
                {
                    l.mrlInfo.prevstglft = l.m.initialStglft;
                    l.mrlInfo.prevownacrul = l.m.initialStglft;
                    l.mrlInfo.own_accrual = l.m.initialStglft;
                    l.mrlInfo.stglft = l.m.initialStglft;
                    l.mlInfo.hi = 0;
                    l.mlInfo.lo = 0;
                }
            }
        }

        public static void NetworkTraceFTDemands(Model mi, List<Node> allNodes, List<Link> allLinks)
        {
            for (int i = 0; i < mi.mInfo.ownerList.Length; i++)
            {
                Link l = mi.mInfo.ownerList[i];
                Node n = l.to;
                if (n.nodeType == NodeType.Demand)
                {
                    // Found an owner to a demand.  Check for FT status
                    if ((n.m.idstrmx[0] != null || n.m.idstrmx[1] != null || n.m.idstrmx[2] != null || n.m.idstrmx[3] != null || n.m.idstrmx[4] != null || n.m.idstrmx[5] != null || n.m.idstrmx[6] != null || n.m.idstrmx[7] != null || n.m.idstrmx[8] != null || n.m.idstrmx[9] != null) && n.m.pdstrm != null)
                    {
                        // now check reservoir type
                        Node n2 = l.m.accrualLink.to;
                        // RKL: Child reservoir stuff; don't know why these two links and the extra node are needed
                        if (n2.mnInfo.child) // Must be a child reservoir
                        {
                            // Check for construct
                            if (n2.mnInfo.flowThruAllocNode == null)
                            {
                                if (n2.m.resOutLink != null)
                                {
                                    Link l2 = n2.m.resOutLink;

                                    // New flowThruAllocNode
                                    Node rtnN = mi.AddNewNode(false);
                                    rtnN.mnInfo = new MnInfo();
                                    rtnN.name = "ArtificialNode_FlowThruAlloc";
                                    n2.mnInfo.flowThruAllocNode = rtnN;
                                    allNodes.Add(rtnN);

                                    // Add two links between flowThruAllocNode & output node
                                    Link rtnL = mi.AddNewLink(false);
                                    rtnL.mlInfo = new MlInfo(); ;
                                    rtnL.mlInfo.isArtificial = true;
                                    rtnL.mlInfo.lo = 0;
                                    rtnL.mlInfo.hi = mi.defaultMaxCap; // 99999999;
                                    rtnL.mlInfo.cost = 0;
                                    Utils.ConnectFromNode(rtnL, n2.mnInfo.flowThruAllocNode);
                                    Utils.ConnectToNode(rtnL, l2.to);
                                    rtnL.name = "ArtificialLink_" + n2.mnInfo.flowThruAllocNode.name + "_" + l2.to.name + "_FlowThruStorage";
                                    allLinks.Add(rtnL);
                                    n2.mnInfo.flowThruSTGLink = rtnL;

                                    rtnL = mi.AddNewLink(false);
                                    rtnL.mlInfo = new MlInfo(); ;
                                    rtnL.mlInfo.isArtificial = true;
                                    rtnL.mlInfo.lo = 0;
                                    rtnL.mlInfo.hi = mi.defaultMaxCap; // 99999999;
                                    rtnL.mlInfo.cost = 10000;
                                    Utils.ConnectFromNode(rtnL, n2.mnInfo.flowThruAllocNode);
                                    Utils.ConnectToNode(rtnL, l2.to);
                                    rtnL.name = "ArtificialLink_" + n2.mnInfo.flowThruAllocNode.name + "_" + l2.to.name + "_FlowThruRelease";
                                    allLinks.Add(rtnL);
                                    n2.mnInfo.flowThruReleaseLink = rtnL;
                                    // Move the current outflow link to flow through our contstruct
                                    Utils.DisConnectToNode(l2);
                                    Utils.ConnectToNode(l2, n2.mnInfo.flowThruAllocNode);
                                    Console.WriteLine("ftan = {0:D}\n", n2.mnInfo.flowThruAllocNode.number);
                                }
                            }
                        }
                    }
                }
            }
        }
        public static double[,] RDownstreamFactors = new double[DefineConstants.MAXREGIONS, DefineConstants.MATRIXSIZE]; // outside splitnetwork for RFactorForReturnFlow

    }
}
