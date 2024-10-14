using System;
using System.Collections;
using System.Collections.Generic;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.NetworkUtils
{
    public class NetTopology
    {
        //nodeCalcOrder
        public static List<List<int>> m_netNodeNosUp;
        private static List<int> m_tempNetNodeNosUp;

        public static List<List<int>> findNetworkUpStream(Model mi)
        {
            Node sink = mi.firstNode;
            List<int> toDo = new List<int>();
            List<int> toDoDown = new List<int>();
            m_netNodeNosUp = new List<List<int>>();

            while (sink != null)
            {
                Node curNode = default(Node);
                Node curDown = default(Node);
                // Only demand nodes 'It doesn't do it if it's a flow thru demand.
                if ((sink.nodeType == NodeType.Demand || sink.nodeType == NodeType.Sink) & (sink.OutflowLinks == null))
                {
                    toDo.Add(sink.number);
                    toDoDown.Add(sink.number);
                    m_tempNetNodeNosUp = new List<int>();

                    while (toDo.Count > 0)
                    {
                        curNode = PopNode(mi, toDo);
                        curDown = PopNode(mi, toDoDown);
                        if (m_tempNetNodeNosUp.Count == 0)
                        {
                            m_tempNetNodeNosUp.Add(curDown.number);
                        }
                        //This step assigns values to tempNetNodeNosUp
                        if (!checkPreviousVisitToNode(curNode.number, curDown.number))
                        {
                            LinkList allLinkList = curNode.InflowLinks;
                            while (allLinkList != null)
                            {
                                Link curLink = allLinkList.link;
                                toDo.Insert(0, curLink.from.number);
                                toDoDown.Insert(0, curNode.number);
                                allLinkList = allLinkList.next;
                            }
                        }
                    }
                    //Add the last element found before todo doesn't have more elements to the last collection of NetNodeNosUp
                    m_netNodeNosUp.Add(m_tempNetNodeNosUp);
                }
                sink = sink.next;
            }
            return FinalProcessing(genNodeCalculationList());
        }

        private static List<List<int>> FinalProcessing(List<List<int>> results)
        {
            List<List<int>> final = new List<List<int>>();
            final.Add(results[0]);
            int activeFinalCollection = 1;

            for (int i = 1; i < results.Count; i++)
            {
                List<int> currRes = results[i];

                //Loop through all the already created Final Sequances and find the active and the collection of indexes
                List<int> indexes = CreateIndexes(final, currRes, activeFinalCollection);
                if (indexes.Count > 0)
                {
                    int Offset = 0;
                    int location = indexes[0];
                    for (int value = 0; value < currRes.Count; value++)
                    {
                        if (indexes[value] >= location)
                        {
                            location = indexes[value];
                            Offset = 0;
                            //This might be redundant
                            if (final[activeFinalCollection][location] == currRes[value])
                            {
                                continue;
                            }
                        }
                        if (indexes[value] == 0)
                        {
                            final[activeFinalCollection].Insert(location + Offset, currRes[value]);
                            Offset++;
                        }
                    }
                }
                else
                {
                    final.Add(results[i]);
                }
            }
            return final;
        }

        private static List<int> CreateIndexes(List<List<int>> final, List<int> activeResults, int activeFinalCollection)
        {
            List<int> indexColl = new List<int>();
            IEnumerator myEnumeratorResults = activeResults.GetEnumerator();
            bool found = false;
            int nodeNumber = -1;

            while (myEnumeratorResults.MoveNext())
            {
                int nodeNo = Convert.ToInt32(myEnumeratorResults.Current);
                for (int i = 0; i < final.Count; i++)
                {
                    IEnumerator myEnumerator = final[i].GetEnumerator();
                    while (myEnumerator.MoveNext())
                    {
                        if (Convert.ToInt32(myEnumerator.Current) == nodeNo)
                        {
                            found = true;
                            activeFinalCollection = i;
                            nodeNumber = i;
                            goto getIndex;
                        }
                    }
                }
            }
        getIndex:
            if (found)
            {
                myEnumeratorResults.Reset();
                //Loop through all the nodes in the result sequence
                while (myEnumeratorResults.MoveNext())
                {
                    int thisNode = Convert.ToInt32(myEnumeratorResults.Current);
                    IEnumerator myEnumerator = final[nodeNumber].GetEnumerator();
                    //loop through all the nodes in the final sequence where a current results node was found
                    int idx = 0;
                    while (myEnumerator.MoveNext())
                    {
                        idx++;
                        if (Convert.ToInt32(myEnumerator.Current) == thisNode)
                        {
                            break;
                        }
                    }
                    indexColl.Add(idx);
                }
            }
            return indexColl;
        }

        private static Node PopNode(Model mi, List<int> ListNumbers)
        {
            Node rval = mi.FindNode(ListNumbers[0]);
            ListNumbers.RemoveAt(0);
            return rval;
        }

        private static bool checkPreviousVisitToNode(int NodeNo, int NodeDwsNo)
        {
            int locPrevious = 0;
            //FIRST check for multiple links between nodes
            if (m_tempNetNodeNosUp.Count > 0)
            {
                for (locPrevious = 0; locPrevious < m_tempNetNodeNosUp.Count; locPrevious++)
                {
                    if (NodeNo == m_tempNetNodeNosUp[locPrevious])
                    {
                        return true;
                        // The node already exists therefore it's not necessary to add it again.
                    }
                }
            }
            //SECOND: Check if the upstream network for the current node has already been calculated.
            for (int i = 0; i < m_netNodeNosUp.Count; i++)
            {
                IEnumerator myEnumerator = m_netNodeNosUp[i].GetEnumerator();
                while (myEnumerator.MoveNext())
                {
                    if (Convert.ToInt32(myEnumerator.Current) == NodeNo)
                    {
                        m_tempNetNodeNosUp.Add(NodeNo);
                        return true;
                    }
                }
            }
            //THIRD check for dwnstream node in the list
            if (m_tempNetNodeNosUp.Count > 1)
            {
                for (locPrevious = 1; locPrevious < m_tempNetNodeNosUp.Count; locPrevious++)
                {
                    if (NodeDwsNo == m_tempNetNodeNosUp[locPrevious])
                    {
                        m_tempNetNodeNosUp.Insert(locPrevious, NodeNo);
                        return false;
                    }
                }
            }
            m_tempNetNodeNosUp.Add(NodeNo);
            return false;
        }

        public static void assignLinkNamesFromToNodes(Model mi)
        {
            Link curLink = mi.firstLink;
            while (curLink != null)
            {
                curLink.name = curLink.@from.number.ToString() + "-" + curLink.to.number.ToString();
                curLink = curLink.next;
            }
        }

        private static List<List<int>> genNodeCalculationList()
        {
            //Revert the order of the sequences to have the calculation order ascending.
            List<List<int>> nodeCalcOrder = new List<List<int>>();

            for (int i = 0; i < m_netNodeNosUp.Count; i++)
            {
                List<int> tempNetNode = new List<int>();
                for (int j = m_netNodeNosUp[i].Count; j >= 0; j--)
                {
                    if (tempNetNode.Count > 0)
                    {
                        IEnumerator nodesAdded = tempNetNode.GetEnumerator();
                        bool alreadyExist = false;
                        while (nodesAdded.MoveNext())
                        {
                            if (Convert.ToInt32(nodesAdded.Current) == m_netNodeNosUp[i][j])
                            {
                                alreadyExist = true;
                                break;
                            }
                        }
                        if (!alreadyExist)
                        {
                            tempNetNode.Add(m_netNodeNosUp[i][j]);
                        }
                    }
                    else
                    {
                        tempNetNode.Add(m_netNodeNosUp[i][j]);
                    }
                }
                nodeCalcOrder.Add(tempNetNode);
            }
            return nodeCalcOrder;
        }

        public static void MarkRoutingRegionNetworkUpStream(Model mi, Node startingNode, int regionNumber)
        {
            Node sink = startingNode;
            List<int> toDo = new List<int>();
            Node curNode = default(Node);
            toDo.Add(sink.number);
            sink.backRRegionID = regionNumber;

            while (toDo.Count > 0)
            {
                curNode = PopNode(mi, toDo);
                LinkList allLinkList = curNode.InflowLinks;
                while (allLinkList != null)
                {
                    Link curLink = allLinkList.link;
                    bool routingLink = (!curLink.mlInfo.isArtificial) && (curLink.m.loss_coef >= 1.0);
                    if ((!routingLink) && (!curLink.mlInfo.isArtificial) && (curLink.@from.backRRegionID == 0))
                    {
                        toDo.Insert(0, curLink.@from.number);
                        curLink.@from.backRRegionID = regionNumber;
                        MarkRoutingRegionNetworkDownStream(mi, curLink.@from, regionNumber);
                    }
                    allLinkList = allLinkList.next;
                }
            }
        }

        public static void MarkRoutingRegionNetworkDownStream(Model mi, Node startingNode, int regionNumber)
        {
            Node sink = startingNode;
            List<int> toDo = new List<int>();
            Node curNode = default(Node);
            toDo.Add(sink.number);
            sink.backRRegionID = regionNumber;

            while (toDo.Count > 0)
            {
                curNode = PopNode(mi, toDo);
                LinkList allLinkList = curNode.OutflowLinks;
                while (allLinkList != null)
                {
                    Link curLink = allLinkList.link;
                    bool routingLink = (!curLink.mlInfo.isArtificial) && (curLink.m.loss_coef >= 1.0);
                    if ((!routingLink) && (!curLink.mlInfo.isArtificial) && (curLink.to.backRRegionID == 0))
                    {
                        toDo.Insert(0, curLink.to.number);
                        curLink.to.backRRegionID = regionNumber;
                        MarkRoutingRegionNetworkUpStream(mi, curLink.to, regionNumber);
                    }
                    allLinkList = allLinkList.next;
                }

            }
        }

        public static int MostDownStreamRegionNo(Model mi, ref double[,] routCoef, int[] downStreamRegs, bool[] directionInverted, int[] regionDownstream)
        {
            int rval = 0;

            Link link = mi.firstLink;
            int maxregs = routCoef.GetLength(0);
            int maxcoefs = routCoef.GetLength(1);
            List<List<List<double>>> rCoef = new List<List<List<double>>>();
            List<List<int>> regionDownStreamColl = new List<List<int>>();
            List<List<int>> regionUpStreamColl = new List<List<int>>();

            for (int i = 0; i < maxregs; i++)
            {
                regionDownStreamColl.Add(new List<int>());
                regionUpStreamColl.Add(new List<int>());
                rCoef.Add(new List<List<double>>());

                for (int j = 0; j < maxregs; j++)
                {
                    rCoef[i].Add(new List<double>());
                }
            }
            for (int i = 0; i < downStreamRegs.Length; i++)
            {
                regionDownStreamColl[downStreamRegs[i]].Add(-1);
            }

            while (link != null)
            {
                if (!(link.mlInfo.isArtificial) && (link.m.loss_coef >= 1.0))
                {
                    int upStream = link.@from.backRRegionID;
                    int downStream = link.to.backRRegionID;
                    List<double> tempCoeff = new List<double>();
                    for (int i = 0; i < maxcoefs; i++)
                    {
                        tempCoeff.Add(link.m.lagfactors[i]);
                    }
                    rCoef[upStream][downStream] = tempCoeff;
                    regionDownStreamColl[upStream].Add(downStream);
                    regionUpStreamColl[downStream].Add(upStream);
                }
                link = link.next;
            }

            //Calculate the region routing coefficients to all the possible downstream points.
            double[][] minRegCoefs = new double[maxregs][];
            double[][] tempRegCoefs = new double[maxregs][];
            for (int k = 1; k < maxregs; k++)
            {
                double[] a = { 1 };
                minRegCoefs[k] = a;
            }

            List<int> toDo;
            int currRegion = 0;
            int activeRegion = 0;
            for (int i = 1; i < maxregs; i++)
            {
                if (regionDownStreamColl[i][0] != -1)
                {
                    toDo = new List<int>();
                    List<int> activeRegioncoll = new List<int>();
                    tempRegCoefs[i] = MultiplyCoefficients(tempRegCoefs, rCoef, i);
                    for (int k = 0; k < regionDownStreamColl[i].Count; k++)
                    {
                        toDo.Add(regionDownStreamColl[i][k]);
                        activeRegioncoll.Add(i);
                    }
                    while (toDo.Count > 0)
                    {
                        currRegion = PopRegion(toDo);
                        activeRegion = PopRegion(activeRegioncoll);
                        //Calculate the current Region coefficient
                        tempRegCoefs[currRegion] = MultiplyCoefficients(tempRegCoefs, rCoef, activeRegion, currRegion);
                        //Add downstream regions to todo
                        for (int j = 0; j < regionDownStreamColl[currRegion].Count; j++)
                        {
                            if (regionDownStreamColl[currRegion][0] == -1)
                            {
                                //Select the smallest coefficient for the sink regions
                                if (AreNewCoeffSlower(minRegCoefs[currRegion], tempRegCoefs[currRegion]))
                                {
                                    minRegCoefs[currRegion] = tempRegCoefs[currRegion];
                                }
                            }
                            else
                            {
                                //Add downstream regions to the todo list.
                                toDo.Add(regionDownStreamColl[currRegion][j]);
                                activeRegioncoll.Add(currRegion);
                            }
                        }
                    }
                }
            }

            //Calculate the most downstream region
            double[] slowerCoeff = { 1 };
            for (int i = 1; i < maxregs; i++)
            {
                if (regionDownStreamColl[i][0] == -1)
                {
                    if (AreNewCoeffSlower(slowerCoeff, minRegCoefs[i]))
                    {
                        slowerCoeff = minRegCoefs[i];
                        rval = i;
                    }
                }
            }

            //Calculate Max Number of significant coefficients
            int signifIndex = 0;
            bool foundIndex = false;
            for (int i = 0; i < minRegCoefs[rval].Length; i++)
            {
                if (minRegCoefs[rval][i] > 0)
                {
                    foundIndex = true;
                }
                if (minRegCoefs[rval][i] == 0 & foundIndex)
                {
                    break;
                }
                signifIndex++;
            }

            //Calculate ordered downsstream regions for back-routing calculation
            toDo = new List<int>();
            toDo.Add(rval);
            regionDownstream[rval] = -1;
            directionInverted[rval] = false;
            routCoef = new double[maxregs, signifIndex + 1];
            do
            {
                currRegion = PopRegion(toDo);
                int coefIndex = 0;
                //Add upstream regions to todo
                for (int j = 0; j < regionUpStreamColl[currRegion].Count; j++)
                {
                    //Add upstream regions to the todo list.
                    if (regionDownstream[regionUpStreamColl[currRegion][j]] == 0)
                    {
                        int UpSReg = regionUpStreamColl[currRegion][j];
                        toDo.Add(UpSReg);
                        regionDownstream[UpSReg] = currRegion;
                        directionInverted[UpSReg] = false;
                        //Trying this prevented an error with bounds here, but created an infeasible solution later on: Math.Min(rCoef(UpSReg, currRegion).count - 1, signifIndex)
                        for (coefIndex = 0; coefIndex < Math.Min(signifIndex, rCoef[UpSReg][currRegion].Count); coefIndex++)
                        {
                            routCoef[UpSReg, coefIndex] = rCoef[UpSReg][currRegion][coefIndex];
                        }
                    }
                }
                for (int j = 0; j < regionDownStreamColl[currRegion].Count; j++)
                {
                    if (regionDownStreamColl[currRegion][0] != -1)
                    {
                        if (regionDownstream[regionDownStreamColl[currRegion][j]] == 0)
                        {
                            int DwSReg = regionDownStreamColl[currRegion][j];
                            toDo.Add(DwSReg);
                            regionDownstream[DwSReg] = currRegion;
                            directionInverted[DwSReg] = true;
                            directionInverted[currRegion] = true;
                            for (coefIndex = 0; coefIndex < rCoef[currRegion][DwSReg].Count; coefIndex++)
                            {
                                routCoef[DwSReg, coefIndex] = rCoef[currRegion][DwSReg][coefIndex];
                            }
                        }
                    }
                }
            }
            while (toDo.Count > 0);
            return rval;
        }
        private static int PopRegion(List<int> ListNumbers)
        {
            int rval = ListNumbers[0];
            ListNumbers.RemoveAt(0);
            return rval;
        }
        private static double[] MultiplyCoefficients(double[][] CoeffMatrix, List<List<List<double>>> rcoef, int UpRegion, int DownRegion = -1)
        {
            int maxCoefNo = CoeffMatrix.Length + 1;
            double[] rval = new double[maxCoefNo];
            if (DownRegion == -1)
            {
                rval[0] = 1;
            }
            else
            {
                for (int i = 0; i < maxCoefNo; i++)
                {
                    for (int j = 0; j < maxCoefNo; j++)
                    {
                        if ((i + j < maxCoefNo) & (j < rcoef[UpRegion][DownRegion].Count))
                        {
                            rval[i + j] += CoeffMatrix[UpRegion][i] * rcoef[UpRegion][DownRegion][j];
                        }
                    }
                }
            }
            return rval;
        }
        private static bool AreNewCoeffSlower(double[] baseCoeff, double[] newCoeff)
        {
            bool rval = false;
            bool coefFound = false;
            int indexCount = 0;

            while (!coefFound)
            {
                if (baseCoeff[indexCount] > 0)
                {
                    if (baseCoeff[indexCount] > newCoeff[indexCount])
                    {
                        rval = true;
                    }
                    coefFound = true;
                }
                else
                {
                    if (newCoeff[indexCount] > 0)
                    {
                        coefFound = true;
                        rval = false;
                    }
                }
                indexCount++;
                if (indexCount > baseCoeff.Length)
                {
                    throw new Exception("No coefficient greater than 0 found in Back-routing branches search");
                }
            }
            return rval;
        }

    }
}