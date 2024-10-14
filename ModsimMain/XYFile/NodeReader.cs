using System;
using System.Collections;
using System.Collections.Generic;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    // node class handles reading the Nodes from the xy file
    public class NodeReader
    {
        // reads through xy file and creates all nodes
        // before calling CreateNodes be sure to acll ModelReader.ReadModelDetails
        public static void CreateNodes(Model mi, TextFile file)
        {
            XYFileReader.NodeArray = new Node[XYFileReader.NodeCount + 1];
            string[] MainCmds = null;
            MainCmds = XYCommands.MainCommands;
            int lineCounter = 0;
            int startIndex = 0;
            // line where string 'node' is found
            int endIndex = 0;
            // last line for this node
            Node node = new Node();
            try
            {
                while (lineCounter < file.Count)
                {
                    startIndex = file.Find("node", lineCounter, file.Count - 1);
                    endIndex = file.FindAny(MainCmds, startIndex + 1);
                    if (startIndex == -1)
                    {
                        return;
                    }
                    node = mi.AddNewNode(true);
                    node.name = XYFileReader.ReadString("name", "", file, startIndex, endIndex);
                    string nuidstring = XYFileReader.ReadString("nuid", "", file, startIndex, endIndex);
                    if (nuidstring != "")
                        node.uid = new Guid(nuidstring);
                    node.description = XYFileReader.ReadString("desc", "", file, startIndex, endIndex);
                    node.number = XYFileReader.ReadInteger("num", -1, file, startIndex, endIndex);
                    node.nodeType = (NodeType)XYFileReader.ReadInteger("ntype", 0, file, startIndex, endIndex);
                    XYFileReader.NodeArray[node.number] = node;
                    if (endIndex < 0)
                    {
                        return;
                    }
                    lineCounter = endIndex;
                }
            }
            catch (Exception ex)
            {
                string msg = string.Empty;
                if (node.name.Length > 0)
                {
                    msg = "Error reading node " + node.name + Environment.NewLine + ex.Message;
                }
                else if (node.number > 0)
                {
                    msg = "Error reading node number " + node.number + Environment.NewLine + ex.Message;
                }
                else
                {
                    msg = $"ERROR: [Reading nodes] (line {startIndex}) " + ex.Message;
                }

                mi.FireOnError(msg);
                throw new Exception(msg);
            }
        }
        // reads the details for nodes, gets a pointer to a node. and passes it to another routine.
        // you should have called CreateNodes before calling ReadNodes
        public static void ReadNodes(Model mi, TextFile file)
        {
            if (file.Count == 0)
            {
                return;
            }

            string[] MainCmds = XYCommands.MainCommands;
            int lineCounter = 0;
            int startIndex = 0;
            // line where string 'node' is found
            int endIndex = 0;
            // last line for this node
            int nodeNumber = 0;
            DateTime startTime = DateTime.Now;
            int timeCtr = 1;
            Node node = new Node();
            try
            {
                while (lineCounter < file.Count)
                {
                    startIndex = file.Find("node", lineCounter, file.Count - 1);
                    endIndex = file.FindAny(MainCmds, startIndex + 1);
                    if (startIndex == -1)
                    {
                        break;
                    }
                    if (endIndex == -1)
                    {
                        mi.FireOnMessage("WARNING: node appears to be last item in xy file (this is not standard)");
                        endIndex = file.Count - 1;
                    }
                    if (endIndex < 0)
                    {
                        return;
                    }
                    nodeNumber = XYFileReader.ReadInteger("num", -1, file, startIndex, endIndex);
                    node = XYFileReader.NodeArray[nodeNumber];
                    if (DateTime.Now.Subtract(startTime).Seconds > 10 * timeCtr)
                    {
                        double percentDone = Convert.ToDouble(100 * nodeNumber / XYFileReader.NodeArray.Length);
                        mi.FireOnMessage(percentDone.ToString("F0") + " percent read");
                        timeCtr += 1;
                    }
                    int[] tmpLinkNums = null;
                    tmpLinkNums = XYFileReader.ReadIntList("out", 0, file, startIndex, endIndex);
                    int i = 0;
                    Link link = null;
                    for (i = 0; i < tmpLinkNums.Length; i++)
                    {
                        link = XYFileReader.LinkArray[tmpLinkNums[i]];
                        if (link == null)
                        {
                            throw new Exception("Error: link number " + tmpLinkNums[i] + " does not exist");
                        }
                        if (node.OutflowLinks == null)
                        {
                            node.OutflowLinks = new LinkList();
                            node.OutflowLinks.link = link;
                        }
                        else
                        {
                            node.AddOut(link);
                        }
                    }
                    tmpLinkNums = XYFileReader.ReadIntList("in", 0, file, startIndex, endIndex);
                    for (i = 0; i < tmpLinkNums.Length; i++)
                    {
                        link = XYFileReader.LinkArray[tmpLinkNums[i]];
                        if (link == null)
                        {
                            throw new Exception("Error: link number " + tmpLinkNums[i] + " does not exist");
                        }
                        if (node.InflowLinks == null)
                        {
                            node.InflowLinks = new LinkList();
                            node.InflowLinks.link = link;
                        }
                        else
                        {
                            node.AddIn(link);
                        }
                    }
                    if (node.m == null)
                    {
                        node.m = new Mnode();
                    }
                    if (node.nodeType != NodeType.NonStorage)
                    {
                        node.m.hydTable = XYFileReader.ReadInteger("hydtable", 0, file, startIndex, endIndex);
                        if (mi.inputVersion.Type == InputVersionType.V056)
                        {
                            if (mi.runType == ModsimRunType.Explicit_Targets)
                            {
                                node.m.hydTable = 0;
                            }
                            else
                            {
                                if (node.m.hydTable == 0)
                                {
                                    node.m.hydTable = 1;
                                }
                            }
                        }
                        //*/ RKL
                        // OK, right here we know what table we are looking at if not 0
                        // so we should allocate the correct number of things like priority, adaDemandM etc
                        //RKL */
                        node.m.infLagi = LagInfo.ReadLagInfo("inflagi", mi, node, file, startIndex, endIndex);
                        int defaultlength = 1;
                        if (node.m.hydTable != 0)
                        {
                            if (mi.HydStateTables.Length < node.m.hydTable)
                            {
                                mi.FireOnError("Read HydStateTables.Length " + mi.HydStateTables.Length + " hydTable " + node.m.hydTable);
                                mi.FireOnError("I am going to change hydTable to zero for this Node number " + node.number);
                                node.m.hydTable = 0;
                            }
                            else
                            {
                                defaultlength = mi.HydStateTables[node.m.hydTable - 1].NumHydBounds + 1;
                            }
                        }
                        node.m.priority = XYFileReader.ReadIndexedIntegerList("priority", defaultlength, 100, file, startIndex, endIndex);
                    }
                    // For old xy files if the reservior has inflows, we should convert this to a forecast
                    if (node.nodeType != NodeType.Demand)
                    {
                        if (mi.inputVersion.Type == InputVersionType.V056)
                        {
                            node.m.adaInflowsM = XYFileReader.ReadTimeSeries("adainfm", TimeSeriesType.NonStorage, 0, file, startIndex, endIndex);
                            if (node.m.adaInflowsM.getSize() > 0)
                            {
                                node.m.adaInflowsM.VariesByYear = true;
                                if (mi.timeStep.TSType == ModsimTimeStepType.Daily)
                                {
                                    Ver7Upgrade.CorrectDailyTimeSeriesIndexes(mi.Nyears, node.m.adaInflowsM);
                                }
                                Ver7Upgrade.TruncateOldTimeSeries(mi, node.m.adaInflowsM);
                                XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaInflowsM);
                            }
                            if (node.m.adaInflowsM.getSize() > 0 & node.nodeType == NodeType.Reservoir)
                            {
                                mi.FireOnError("Inflows were found for node " + node.name + ", but inflows are not allowed for reservoirs. Inflows were moved to runoff forecasts. If you need these inflows for river gains, you will need to copy them to a nonstorage node");
                                ReservoirInflow2Forecast(node);
                            }
                        }
                        else
                        {
                            node.m.adaInflowsM = XYFileReader.ReadTimeSeries(mi, "tsinflow", file, startIndex, endIndex);
                            if (node.nodeType == NodeType.Reservoir & node.m.adaInflowsM.getSize() > 0)
                            {
                                mi.FireOnError("Inflows were found for node " + node.name + ", but inflows are not allowed for reservoirs. Inflows were moved to runoff forecasts. If you need these inflows for river gains, you will need to copy them to a nonstorage node");
                                ReservoirInflow2Forecast(node);
                            }

                        }
                    }
                    double[] nodeXY = null;
                    double[] nodeLabelXY = null;
                    nodeXY = XYFileReader.ReadIndexedFloatList("pos", 0, file, startIndex, endIndex);
                    nodeLabelXY = XYFileReader.ReadIndexedFloatList("labelpos", 0, file, startIndex, endIndex);
                    node.graphics.nodeLoc.X = Convert.ToSingle(nodeXY[0]);
                    node.graphics.nodeLoc.Y = Convert.ToSingle(nodeXY[1]);
                    if (nodeLabelXY.Length != 0)
                    {
                        node.graphics.nodeLabelLoc.X = Convert.ToSingle(nodeLabelXY[0]);
                        node.graphics.nodeLabelLoc.Y = Convert.ToSingle(nodeLabelXY[1]);
                    }
                    int tempbool = XYFileReader.ReadInteger("IsVisible", 1, file, startIndex, endIndex);
                    if (tempbool == 0)
                    {
                        node.graphics.visible = false;
                    }
                    tempbool = XYFileReader.ReadInteger("IsStorageRightRes", 0, file, startIndex, endIndex);
                    if (tempbool == 1)
                    {
                        node.graphics.storageRightReservoir = true;
                    }
                    tempbool = XYFileReader.ReadInteger("IsLabelVisible", 1, file, startIndex, endIndex);
                    if (tempbool == 0)
                    {
                        node.graphics.labelVisible = false;
                    }

                    node.m.selected = Convert.ToBoolean(XYFileReader.ReadInteger("select", 1, file, startIndex, endIndex));
                    if (node.nodeType == NodeType.Demand)
                    {
                        ReadDemandDetails(mi, node, file, startIndex, endIndex - 1);
                    }
                    else if (node.nodeType == NodeType.NonStorage)
                    {
                        ReadNonStorageDetails(mi, node, file, startIndex, endIndex - 1);
                    }
                    else if (node.nodeType == NodeType.Reservoir)
                    {
                        ReadReservoirDetails(mi, node, file, startIndex, endIndex - 1);
                    }
                    else if (node.nodeType == NodeType.Sink)
                    {
                        //No details needed to be provided for sink nodes
                    }
                    if (mi.inputVersion.Type == InputVersionType.V056)
                    {
                        if (node.nodeType == NodeType.Demand)
                        {
                            if (node.m.dem.Length > 0 & node.m.demd.Length > 0)
                            {
                                if (node.m.adaDemandsM.getSize() == 0)
                                {
                                    if (mi.timeStep.TSType != ModsimTimeStepType.Monthly)
                                    {
                                        node.m.adaDemandsM.VariesByYear = true;
                                    }
                                    XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaDemandsM);
                                }
                                DemandDist2adaDemandM(mi, node);
                            }
                            else
                            {
                                node.m.hydTable = 0;
                            }
                        }
                    }
                    node.m.GWStorageOnly = Convert.ToBoolean(XYFileReader.ReadInteger("GWStorOnly", 0, file, startIndex, endIndex));
                    lineCounter = endIndex;
                    nodeNumber = nodeNumber + 1;
                }
            }
            catch (Exception ex)
            {
                string msg = "Error reading node " + node.name + Environment.NewLine + ex.Message;
                mi.FireOnError(msg);
                throw new Exception(msg);
            }
        }
        private static void ReservoirInflow2Forecast(Node node)
        {
            if (node.m.adaInflowsM.getSize() == 0)
            {
                return;
            }
            node.m.adaForecastsM.VariesByYear = true;
            for (int i = 0; i < node.m.adaInflowsM.getSize(); i++)
            {
                node.m.adaForecastsM.setDate(i, node.m.adaInflowsM.getDate(i));
                node.m.adaForecastsM.setDataL(i, node.m.adaInflowsM.getDataL(i));
            }
            node.m.adaInflowsM = new TimeSeries(TimeSeriesType.NonStorage);
        }
        //ReadDemandDetails reads demand data from the xy file for the node specified.
        private static void ReadDemandDetails(Model mi, Node node, TextFile file, int startIndex, int endIndex)
        {
            long[] tmpLinkNums = null;
            try
            {
                node.m.pcap = XYFileReader.ReadLong("pcap", 0, file, startIndex, endIndex);
                node.m.pcapUnits = XYFileReader.ReadUnits(mi, "pcapUnits", mi.FlowUnits, file, startIndex, endIndex);
                node.m.pcost = XYFileReader.ReadLong("pcost", 0, file, startIndex, endIndex);
                node.m.spyld = XYFileReader.ReadFloat("spyld", 0, file, startIndex, endIndex);
                node.m.trans = XYFileReader.ReadFloat("trans", 0, file, startIndex, endIndex);
                node.m.Distance = XYFileReader.ReadFloat("ddist", 0, file, startIndex, endIndex);
                node.m.dem = XYFileReader.ReadIndexedIntegerList("dem", 0, 0, file, startIndex, endIndex);
                int defaultlength = 1;
                if (node.m.hydTable > 0)
                {
                    defaultlength = mi.HydStateTables[node.m.hydTable - 1].NumHydBounds + 1;
                }
                node.m.demr = XYFileReader.ReadIndexedIntegerList("demr", defaultlength, 100, file, startIndex, endIndex);
                int reservoirNumberForDirectDemand = XYFileReader.ReadInteger("demDirect", -1, file, startIndex, endIndex);
                if (reservoirNumberForDirectDemand != -1)
                {
                    node.m.demDirect = XYFileReader.NodeArray[reservoirNumberForDirectDemand];
                    // mi.FindNode(reservoirNumberForDirectDemand)
                    if (node.m.demDirect == null)
                    {
                        throw new Exception("Error: This node number " + reservoirNumberForDirectDemand + " does not exist");
                    }
                }
                long[] tmpNodeNumbers = null;
                tmpNodeNumbers = XYFileReader.ReadIndexedIntegerList("idstrmx", 0, file, startIndex, endIndex);
                for (int i = 0; i < tmpNodeNumbers.Length; i++)
                {
                    Node tmpNode = XYFileReader.NodeArray[Convert.ToInt32(tmpNodeNumbers[i])];
                    if (tmpNode == null)
                    {
                        throw new Exception("Error:  node number " + tmpNodeNumbers[i] + " not found ");
                    }
                    node.m.idstrmx[i] = tmpNode;
                }
                node.m.idstrmfraction = XYFileReader.ReadIndexedFloatList("idstrmfr", 10, 1, file, startIndex, endIndex);
                int tmpNodeNum = XYFileReader.ReadInteger("jdstrm", -1, file, startIndex, endIndex);
                if (tmpNodeNum != -1)
                {
                    Node tmpNode = XYFileReader.NodeArray[tmpNodeNum];
                    if (tmpNode == null)
                    {
                        throw new Exception("Error:  node number " + tmpNodeNum + " not found ");
                    }
                    node.m.jdstrm = tmpNode;
                    //exchange credit node
                }
                int tmpLinkNum = XYFileReader.ReadInteger("pdstrm", -1, file, startIndex, endIndex);
                if (tmpLinkNum != -1)
                {
                    Link tmpLink = XYFileReader.LinkArray[tmpLinkNum];
                    if (tmpLink == null)
                    {
                        throw new Exception("Error:  link number " + tmpLinkNum + " not found ");
                    }
                    node.m.pdstrm = tmpLink;
                }
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    node.m.infil = XYFileReader.ReadIndexedFloatList("infil", 12, 0, file, startIndex, endIndex);

                    int lper = mi.timeStep.NumOfTSsForV7Output;
                    bool ArrayNotEmpty = false;
                    for (int k = 0; k < lper; k++)
                    {
                        if (node.m.infil[k] != 0)
                        {
                            ArrayNotEmpty = true;
                        }
                    }
                    if (ArrayNotEmpty == true)
                    {
                        for (int k = 0; k < lper; k++)
                        {
                            node.m.adaInfiltrationsM.setDataF(k, node.m.infil[k]);
                            node.m.infil[k] = 0;
                        }
                        XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaInfiltrationsM);
                    }
                    else
                    {
                        //Implemented for the IID Network - It's not supported in Ver 7 Interface
                        node.m.adaInfiltrationsM = XYFileReader.ReadTimeSeries("adainfilts", TimeSeriesType.Infiltration, 0, file, startIndex, endIndex);
                        if (node.m.adaInfiltrationsM.getSize() > 0)
                        {
                            if (mi.timeStep.TSType == ModsimTimeStepType.Daily)
                            {
                                Ver7Upgrade.CorrectDailyTimeSeriesIndexes(mi.Nyears, node.m.adaInfiltrationsM);
                            }
                            Ver7Upgrade.TruncateOldTimeSeries(mi, node.m.adaInfiltrationsM);
                            node.m.adaDemandsM.VariesByYear = true;
                            XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaInfiltrationsM);
                        }
                    }
                }
                else
                {
                    node.m.adaInfiltrationsM = XYFileReader.ReadTimeSeries(mi, "tsinfiltration", file, startIndex, endIndex);
                }
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    node.m.adaDemandsM = XYFileReader.ReadTimeSeries("adademm", TimeSeriesType.Demand, 0, file, startIndex, endIndex);
                    if (node.m.adaDemandsM.getSize() > 0)
                    {
                        if (mi.timeStep.TSType == ModsimTimeStepType.Daily)
                        {
                            Ver7Upgrade.CorrectDailyTimeSeriesIndexes(mi.Nyears, node.m.adaDemandsM);
                        }
                        Ver7Upgrade.TruncateOldTimeSeries(mi, node.m.adaDemandsM);
                        node.m.adaDemandsM.VariesByYear = true;
                        XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaDemandsM);
                    }
                }
                else
                {
                    node.m.adaDemandsM = XYFileReader.ReadTimeSeries(mi, "tsdemand", file, startIndex, endIndex);
                }
                node.m.pumpLagi = LagInfo.ReadLagInfo("pumplagi", mi, node, file, startIndex, endIndex);
                node.m.watchFactors = XYFileReader.ReadIndexedFloatList("nwatchfact", 15, 1, file, startIndex, endIndex);
                tmpLinkNums = XYFileReader.ReadIndexedIntegerList("nwatchmax", 0, file, startIndex, endIndex);
                bool ReadWatchLogic = true;
                if (ReadWatchLogic)
                {
                    for (int i = 0; i < tmpLinkNums.Length; i++)
                    {
                        Link tmpLink = XYFileReader.LinkArray[Convert.ToInt32(tmpLinkNums[i])];
                        if (tmpLink == null)
                        {
                            throw new Exception("Error: nwatchmax reading link on xy file line " + startIndex);
                        }
                        node.m.watchMaxLinks[i] = tmpLink;
                    }
                    tmpLinkNums = XYFileReader.ReadIndexedIntegerList("nwatchmin", 0, file, startIndex, endIndex);

                    for (int i = 0; i < tmpLinkNums.Length; i++)
                    {
                        Link tmpLink = XYFileReader.LinkArray[Convert.ToInt32(tmpLinkNums[i])];
                        if (tmpLink == null)
                        {
                            throw new Exception("Error: nwatchmin reading link on xy file line " + startIndex);
                        }
                        node.m.watchMinLinks[i] = tmpLink;
                    }
                    tmpLinkNums = XYFileReader.ReadIndexedIntegerList("nwatchlog", 0, file, startIndex, endIndex);
                    for (int i = 0; i < tmpLinkNums.Length; i++)
                    {
                        Link tmpLink = XYFileReader.LinkArray[Convert.ToInt32(tmpLinkNums[i])];
                        if (tmpLink == null)
                        {
                            throw new Exception("Error: nwatchlog reading link on xy file line " + startIndex);
                        }
                        node.m.watchLogLinks[i] = tmpLink;
                    }
                    tmpLinkNums = XYFileReader.ReadIndexedIntegerList("nwatchln", 0, file, startIndex, endIndex);
                    for (int i = 0; i < tmpLinkNums.Length; i++)
                    {
                        Link tmpLink = XYFileReader.LinkArray[Convert.ToInt32(tmpLinkNums[i])];
                        if (tmpLink == null)
                        {
                            throw new Exception("Error: nwatchln reading link on xy file line " + startIndex);
                        }
                        node.m.watchLnLinks[i] = tmpLink;
                    }
                    tmpLinkNums = XYFileReader.ReadIndexedIntegerList("nwatchexp", 0, file, startIndex, endIndex);
                    for (int i = 0; i < tmpLinkNums.Length; i++)
                    {
                        Link tmpLink = XYFileReader.LinkArray[Convert.ToInt32(tmpLinkNums[i])];
                        if (tmpLink == null)
                        {
                            throw new Exception("Error: nwatchexp reading link on xy file line " + startIndex);
                        }
                        node.m.watchExpLinks[i] = tmpLink;
                    }
                    tmpLinkNums = XYFileReader.ReadIndexedIntegerList("nwatchxsq", 0, file, startIndex, endIndex);
                    for (int i = 0; i < tmpLinkNums.Length; i++)
                    {
                        Link tmpLink = XYFileReader.LinkArray[Convert.ToInt32(tmpLinkNums[i])];
                        if (tmpLink == null)
                        {
                            throw new Exception("Error: nwatchxsq reading link on xy file line " + startIndex);
                        }
                        node.m.watchSqrLinks[i] = tmpLink;
                    }
                    tmpLinkNums = XYFileReader.ReadIndexedIntegerList("nwatchpow", 0, file, startIndex, endIndex);
                    for (int i = 0; i < tmpLinkNums.Length; i++)
                    {
                        Link tmpLink = XYFileReader.LinkArray[Convert.ToInt32(tmpLinkNums[i])];
                        if (tmpLink == null)
                        {
                            throw new Exception("Error: nwatchpow reading link on xy file line " + startIndex);
                        }
                        node.m.watchPowLinks[i] = tmpLink;
                    }
                    node.m.powvalue = XYFileReader.ReadFloat("npowval", 0, file, startIndex, endIndex);
                }
                if (node.m.dem.Length > 0)
                {
                    double[] tmpFloatList = XYFileReader.ReadIndexedFloatList("demdist", 0, file, startIndex, endIndex);
                    if (tmpFloatList.Length > 0)
                    {
                        double[,] tmpDemandDist = new double[7, 12];
                        // xy file assumes size of node.m.demd(7,17)
                        Array.Resize(ref tmpFloatList, 7 * 12);
                        try
                        {
                            for (int row = 0; row <= 6; row++)
                            {
                                for (int col = 0; col <= 11; col++)
                                {
                                    tmpDemandDist[row, col] = tmpFloatList[row * 12 + col];
                                }
                            }
                        }
                        catch
                        {
                            mi.FireOnError("Error reading demand distribution (demd) Table");
                        }
                        node.m.demd = tmpDemandDist;
                    }
                    else
                    {
                        // demd is an OLD command where the indexing is transposed from that of demdist
                        tmpFloatList = XYFileReader.ReadIndexedFloatList("demd", 0, file, startIndex, endIndex);
                        double[,] tmpDemandDist = new double[7, 12];
                        Array.Resize(ref tmpFloatList, 7 * 12);
                        try
                        {
                            for (int row = 0; row <= 6; row++)
                            {
                                for (int col = 0; col <= 11; col++)
                                {
                                    tmpDemandDist[row, col] = tmpFloatList[col * 7 + row];
                                }
                            }
                        }
                        catch
                        {
                            mi.FireOnError("Error reading demand distribution (demd) Table");
                        }
                        node.m.demd = tmpDemandDist;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //DemandDist2adaDemandM gets rid of dem, demd and converts them to adaDemandM
        private static void DemandDist2adaDemandM(Model mi, Node node)
        {
            int i = 0;
            double value = 0;
            int lper = mi.timeStep.NumOfTSsForV7Output;
            int numhs = 0;
            bool convert = false;
            for (i = 0; i < node.m.dem.Length; i++)
            {
                if (node.m.dem[i] != 0)
                {
                    convert = true;
                }
            }
            if (convert == true)
            {
                convert = false;
                int j = 0;
                numhs = 1;
                if ((mi.HydStateTables != null) & node.m.hydTable != 0)
                {
                    numhs = mi.HydStateTables[node.m.hydTable - 1].NumHydBounds + 1;
                }
                for (i = 0; i < lper; i++)
                {
                    for (j = 0; j < numhs; j++)
                    {
                        if (node.m.demd[j, i] != 0)
                        {
                            convert = true;
                        }
                    }
                }
            }
            if (convert)
            {
                long intvalue = 0;
                int timeStepIndex = 0;
                int hydStateIndex = 0;
                int distIndex = 0;
                int numts = 0;
                if (mi.timeStep.TSType == ModsimTimeStepType.Daily)
                {
                    node.m.adaDemandsM.VariesByYear = true;
                    numts = Math.Max(mi.TimeStepManager.noModelTimeSteps, node.m.adaDemandsM.getSize());
                }
                else
                {
                    numts = Math.Max(lper, node.m.adaDemandsM.getSize());
                    if (numts > lper)
                    {
                        node.m.adaDemandsM.VariesByYear = true;
                    }
                }
                if (numhs > 1)
                {
                    node.m.adaDemandsM.MultiColumn = true;
                }
                if ((numhs + 1 > node.m.adaDemandsM.getNumCol() || numts > node.m.adaDemandsM.getSize()))
                {
                    node.m.adaDemandsM.setDataL(numts - 1, numhs - 1, 0);
                }
                for (timeStepIndex = 0; timeStepIndex < numts; timeStepIndex++)
                {
                    Math.DivRem(timeStepIndex, lper, out distIndex);
                    for (hydStateIndex = 0; hydStateIndex < numhs; hydStateIndex++)
                    {
                        if (hydStateIndex > node.m.dem.Length - 1)
                        {
                            value = 0;
                        }
                        else
                        {
                            value = node.m.adaDemandsM.getDataL(timeStepIndex, hydStateIndex) + node.m.dem[hydStateIndex] * node.m.demd[hydStateIndex, distIndex];
                        }
                        if (value > mi.defaultMaxCap)
                        {
                            value = mi.defaultMaxCap;
                        }
                        intvalue = Convert.ToInt64(value);
                        node.m.adaDemandsM.setDataL(timeStepIndex, hydStateIndex, intvalue);
                    }
                }
                node.m.dem = new long[0];
                node.m.demd = new double[0, 0];
            }
            else
            {
                node.m.adaDemandsM.MultiColumn = false;
                node.m.hydTable = 0;
                node.m.dem = new long[0];
                node.m.demd = new double[0, 0];
            }
        }
        //ReadReservoirDetails reads all reservoir information for the specified node
        private static void ReadReservoirDetails(Model mi, Node node, TextFile file, int startIndex, int endIndex)
        {
            int tmpLinkNum = 0;
            int tmpNodeNum = 0;
            try
            {
                node.m.max_volume = XYFileReader.ReadLong("max_vol", 0, file, startIndex, endIndex);
                node.m.min_volume = XYFileReader.ReadLong("min_vol", 0, file, startIndex, endIndex);
                node.m.starting_volume = XYFileReader.ReadLong("start_vol", 0, file, startIndex, endIndex);
                node.m.reservoir_units = XYFileReader.ReadUnits(mi, "reservoir_units", mi.StorageUnits, file, startIndex, endIndex);
                node.m.powmax = XYFileReader.ReadFloat("powmax", 0, file, startIndex, endIndex);
                node.m.elev = XYFileReader.ReadFloat("elev", 0, file, startIndex, endIndex);
                node.m.peakGeneration = Convert.ToBoolean(XYFileReader.ReadFloat("ipeak", 0, file, startIndex, endIndex));
                node.m.ResEffCurve = HydroReader.ReadEfficiency(mi, file, startIndex, endIndex, node);
                // power efficiency
                node.m.spyld = XYFileReader.ReadFloat("spyld", 0, file, startIndex, endIndex);
                node.m.trans = XYFileReader.ReadFloat("trans", 0, file, startIndex, endIndex);
                node.m.Distance = XYFileReader.ReadFloat("ddist", 0, file, startIndex, endIndex);
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    node.m.infil = XYFileReader.ReadIndexedFloatList("infil", 12, 0, file, startIndex, endIndex);
                    int lper = mi.timeStep.NumOfTSsForV7Output;
                    int k = 0;
                    bool ArrayNotEmpty = false;
                    for (k = 0; k < lper; k++)
                    {
                        if (node.m.infil[k] != 0)
                        {
                            ArrayNotEmpty = true;
                        }
                    }
                    if (ArrayNotEmpty)
                    {
                        for (k = 0; k < lper; k++)
                        {
                            node.m.adaInfiltrationsM.setDataF(k, node.m.infil[k]);
                            node.m.infil[k] = 0;
                        }
                        XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaInfiltrationsM);
                    }
                }
                else
                {
                    node.m.adaInfiltrationsM = XYFileReader.ReadTimeSeries(mi, "tsinfiltration", file, startIndex, endIndex);
                }
                node.m.seepg = XYFileReader.ReadFloat("seepg", 0, file, startIndex, endIndex);
                tmpLinkNum = XYFileReader.ReadInteger("rbypassl", -1, file, startIndex, endIndex);
                if (tmpLinkNum != -1)
                {
                    Link tmpLink = XYFileReader.LinkArray[tmpLinkNum];
                    if (tmpLink == null)
                    {
                        throw new Exception("Error:  link number " + tmpLinkNum + " not found ");
                    }
                    node.m.resBypassL = tmpLink;
                }
                node.parentFlag = Convert.ToBoolean(XYFileReader.ReadInteger("parent", 1, file, startIndex, endIndex));
                tmpNodeNum = XYFileReader.ReadInteger("mymom", -1, file, startIndex, endIndex);
                if (tmpNodeNum != -1)
                {
                    Node tmpNode = XYFileReader.NodeArray[tmpNodeNum];
                    if (tmpNode == null)
                    {
                        throw new Exception("Error:  parent node number " + tmpNodeNum + " not found ");
                    }
                    node.myMother = tmpNode;
                    // parent node
                }
                node.numChildren = XYFileReader.ReadInteger("numkids", 0, file, startIndex, endIndex);
                tmpNodeNum = XYFileReader.ReadInteger("resnext", -1, file, startIndex, endIndex);
                if (tmpNodeNum != -1)
                {
                    Node tmpNode = XYFileReader.NodeArray[tmpNodeNum];
                    if (tmpNode == null)
                    {
                        throw new Exception("Error:  resnext node number " + tmpNodeNum + " not found ");
                    }
                    node.RESnext = tmpNode;
                    // parent node
                }
                tmpNodeNum = XYFileReader.ReadInteger("resprev", -1, file, startIndex, endIndex);
                if (tmpNodeNum != -1)
                {
                    Node tmpNode = XYFileReader.NodeArray[tmpNodeNum];
                    if (tmpNode == null)
                    {
                        throw new Exception("Error:  resprev node number " + tmpNodeNum + " not found ");
                    }
                    node.RESprev = tmpNode;
                    // parent node
                }
                if ((mi.inputVersion.Type == InputVersionType.V056))
                {
                    node.m.adaEvaporationsM = XYFileReader.ReadTimeSeries("adaevam", TimeSeriesType.Evaporation, 0, file, startIndex, endIndex);
                    if (node.m.adaEvaporationsM.getSize() > 0)
                    {
                        if (mi.timeStep.TSType == ModsimTimeStepType.Daily)
                        {
                            Ver7Upgrade.CorrectDailyTimeSeriesIndexes(mi.Nyears, node.m.adaEvaporationsM);
                        }
                        Ver7Upgrade.TruncateOldTimeSeries(mi, node.m.adaEvaporationsM);
                        XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaEvaporationsM);
                    }
                    node.m.adaGeneratingHrsM = XYFileReader.ReadTimeSeries("adagenm", TimeSeriesType.Generating_Hours, 0, file, startIndex, endIndex);
                    if (node.m.adaGeneratingHrsM.getSize() > 0)
                    {
                        if (mi.timeStep.TSType == ModsimTimeStepType.Daily)
                        {
                            Ver7Upgrade.CorrectDailyTimeSeriesIndexes(mi.Nyears, node.m.adaGeneratingHrsM);
                        }
                        Ver7Upgrade.TruncateOldTimeSeries(mi, node.m.adaGeneratingHrsM);
                        XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaGeneratingHrsM);
                    }
                }
                else
                {
                    node.m.adaEvaporationsM = XYFileReader.ReadTimeSeries(mi, "tsevaprate", file, startIndex, endIndex);
                    node.m.adaGeneratingHrsM = XYFileReader.ReadTimeSeries(mi, "tsgeneratehrs", file, startIndex, endIndex);
                }
                tmpLinkNum = XYFileReader.ReadInteger("reslastfilllink", -1, file, startIndex, endIndex);
                if (tmpLinkNum != -1)
                {
                    Link tmpLink = XYFileReader.LinkArray[tmpLinkNum];
                    if (tmpLink == null)
                    {
                        throw new Exception("Error:  reslastfill link number " + tmpLinkNum + " not found ");
                    }
                    node.m.lastFillLink = tmpLink;
                }
                tmpLinkNum = XYFileReader.ReadInteger("resoutfl", -1, file, startIndex, endIndex);
                if (tmpLinkNum != -1)
                {
                    Link tmpLink = XYFileReader.LinkArray[tmpLinkNum];
                    if (tmpLink == null)
                    {
                        throw new Exception("Error:  resoutfl link number " + tmpLinkNum + " not found ");
                    }
                    node.m.resOutLink = tmpLink;
                }
                node.m.sysnum = XYFileReader.ReadInteger("sysnum", 0, file, startIndex, endIndex);
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    node.m.adaTargetsM.Interpolate = false;
                    int lper = mi.timeStep.NumOfTSsForV7Output;
                    if (mi.runType == ModsimRunType.Explicit_Targets)
                    {
                        node.m.adaTargetsM.MultiColumn = false;
                        node.m.adaTargetsM = XYFileReader.ReadTimeSeries("adatrgm", TimeSeriesType.Targets, 0, file, startIndex, endIndex);
                        if (node.m.adaTargetsM.getSize() > 0)
                        {
                            if (mi.timeStep.TSType == ModsimTimeStepType.Daily)
                            {
                                Ver7Upgrade.CorrectDailyTimeSeriesIndexes(mi.Nyears, node.m.adaTargetsM);
                            }
                            Ver7Upgrade.TruncateOldTimeSeries(mi, node.m.adaTargetsM);
                            node.m.adaTargetsM.VariesByYear = false;
                            if (node.m.adaTargetsM.getSize() > lper)
                            {
                                node.m.adaTargetsM.VariesByYear = true;
                            }
                            XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaTargetsM);
                        }
                    }
                    else
                    {
                        List<long> tsarray = XYFileReader.ReadIntTimeSeries("adatrgm", 0, file, startIndex, endIndex);
                        if (tsarray.Count > 0)
                        {
                            int numhs = 1;
                            if (mi.HydStateTables.Length > 0)
                            {
                                numhs = mi.HydStateTables[node.m.hydTable - 1].NumHydBounds + 1;
                                node.m.adaTargetsM.MultiColumn = true;
                            }

                            long intVal = 0;
                            for (int j = 0; j < numhs; j++)
                            {
                                for (int i = 0; i < lper; i++)
                                {
                                    intVal = 0;
                                    if (j * 12 + i < tsarray.Count)
                                    {
                                        intVal = Convert.ToInt64(tsarray[j * 12 + i]);
                                    }
                                    node.m.adaTargetsM.setDataL(i, j, intVal);
                                }
                            }
                            XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaTargetsM);
                        }
                    }
                }
                else
                {
                    node.m.adaTargetsM = XYFileReader.ReadTimeSeries(mi, "tstarget", file, startIndex, endIndex);
                    node.m.adaForecastsM = XYFileReader.ReadTimeSeries(mi, "tsforcast", file, startIndex, endIndex);
                }
                node.m.apoints = XYFileReader.ReadIndexedFloatList("apoints", 0, file, startIndex, endIndex);
                node.m.cpoints = XYFileReader.ReadIndexedIntegerList("cpoints", 0, file, startIndex, endIndex);
                //cpoints were converted to MODSIM long variables including precision in verison 8.6
                //  When reading previous version need to apply the ScaleFactor keep the previous value
                if (mi.inputVersion.Type < InputVersionType.V8_6)
                {
                    for (int i=0; i < node.m.cpoints.Length;i++ )
                        node.m.cpoints[i] = (long)Math.Round(node.m.cpoints[i] * mi.ScaleFactor, 0);
                }
                node.m.epoints = XYFileReader.ReadIndexedFloatList("epoints", 0, file, startIndex, endIndex);
                node.m.hpoints = XYFileReader.ReadIndexedIntegerList("hpoints", 0, file, startIndex, endIndex);
                node.m.area_units = XYFileReader.ReadUnits(mi, "area_units", mi.AreaUnits, file, startIndex, endIndex);
                node.m.capacity_units = XYFileReader.ReadUnits(mi, "capacity_units", mi.StorageUnits, file, startIndex, endIndex);
                node.m.hcapacity_units = XYFileReader.ReadUnits(mi, "hcapacity_units", mi.FlowUnits, file, startIndex, endIndex);
                int tmpNumPoints = node.m.cpoints.Length;
                if (tmpNumPoints != node.m.apoints.Length)
                {
                    mi.FireOnMessage("WARNING: reservoir node " + node.name + " area table has a different number of points than capacity table");
                    Array.Resize(ref node.m.apoints, tmpNumPoints);
                }
                if (tmpNumPoints != node.m.epoints.Length)
                {
                    mi.FireOnMessage("WARNING: reservoir node " + node.name + " elevation table has a different number of points than capacity table");
                    Array.Resize(ref node.m.epoints, tmpNumPoints);
                }
                if (tmpNumPoints != node.m.hpoints.Length)
                {
                    if (node.m.hpoints.Length != 0)
                    {
                        mi.FireOnMessage("WARNING: reservoir node " + node.name + " hydraulic capacity table has a different number of points than capacity table");
                    }
                    Array.Resize(ref node.m.hpoints, tmpNumPoints);
                }
                int idx = file.Find("resbali", startIndex, endIndex);
                if (idx >= startIndex & idx < endIndex)
                {
                    node.m.resBalance = new ResBalance();
                    long[] tmpPrioList = XYFileReader.ReadIndexedIntegerList("resbaliprio", 0, file, startIndex, endIndex);
                    double[] tmpTargetList = XYFileReader.ReadIndexedFloatList("resbalpcts", 0, file, startIndex, endIndex);
                    int maxSizeTarg = Math.Max(tmpTargetList.Length, tmpPrioList.Length);
                    Array.Resize(ref tmpPrioList, maxSizeTarg);
                    Array.Resize(ref tmpTargetList, maxSizeTarg);
                    node.m.resBalance.incrPriorities = tmpPrioList;
                    node.m.resBalance.targetPercentages = tmpTargetList;
                    node.m.resBalance.PercentBasedOnMaxCapacity = Convert.ToBoolean(XYFileReader.ReadInteger("resbalflag", 0, file, startIndex, endIndex));
                }
                long[] tmpFlowPts = XYFileReader.ReadIndexedIntegerList("flowpts", 0, file, startIndex, endIndex);
                double[] tmpTwelevPts = XYFileReader.ReadIndexedFloatList("twelevpts", 0, file, startIndex, endIndex);
                int maxSize = Math.Max(tmpFlowPts.Length, tmpTwelevPts.Length);
                Array.Resize(ref tmpFlowPts, maxSize);
                Array.Resize(ref tmpTwelevPts, maxSize);
                node.m.flowpts = tmpFlowPts;
                node.m.twelevpts = tmpTwelevPts;
                if (node.m.adaTargetsM.getSize() == 0)
                {
                    if (string.IsNullOrEmpty(node.name))
                    {
                        mi.FireOnMessage("WARNING: reservoir number " + node.number + " does not have targets defined");
                    }
                    else
                    {
                        mi.FireOnMessage("WARNING: reservoir node " + node.name + " does not have targets defined");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //ReadNonStorageDetails reads info for round nodes in xy file
        private static void ReadNonStorageDetails(Model mi, Node node, TextFile file, int startIndex, int endIndex)
        {
            try
            {
                if (mi.inputVersion.Type == InputVersionType.V056)
                {
                    node.m.import = XYFileReader.ReadLong("import", 0, file, startIndex, endIndex);
                    node.m.dist = XYFileReader.ReadIndexedFloatList("dist", 0, 0, file, startIndex, endIndex);
                    if (node.m.import > 0 & node.m.dist.Length > 0)
                    {
                        if (node.m.dist.Length < 12)
                        {
                            Array.Resize(ref node.m.dist, 12);
                        }
                        if (mi.timeStep.TSType != ModsimTimeStepType.Monthly)
                        {
                            node.m.adaInflowsM.VariesByYear = true;
                        }
                        XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaInflowsM);
                        Import2adaInflowsM(mi, node);
                    }
                    
                }
                node.m.inflowFactor = XYFileReader.ReadFloat("infFrac", 0, file, startIndex, endIndex);
                int tmpNodeNum = XYFileReader.ReadInteger("infFracNode", -1, file, startIndex, endIndex);
                if (tmpNodeNum != -1)
                {
                    Node tmpNode = XYFileReader.NodeArray[tmpNodeNum];
                    if (tmpNode == null)
                    {
                        throw new Exception("Error:  Inflow fraction node number " + tmpNodeNum + " not found ");
                    }
                    node.m.inflowFracNode = tmpNode;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //Import2adaInflowsM gets rid of imports converts to adaInflowsM
        private static void Import2adaInflowsM(Model mi, Node node)
        {
            int i = 0;
            int lper = mi.timeStep.NumOfTSsForV7Output;
            long value = 0;
            int distIndex = 0;
            int timeStepIndex = 0;
            bool convert = false;
            if (node.m.import != 0)
            {
                for (i = 0; i < lper; i++)
                {
                    if (node.m.dist[i] > 0)
                    {
                        convert = true;
                    }
                }
            }
            if (convert)
            {
                if (node.m.adaInflowsM.getSize() < lper)
                {
                    node.m.adaInflowsM.setDataL(lper - 1, 0);
                    XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaInflowsM);
                }
                if (node.m.adaInflowsM.getSize() > lper & node.m.adaInflowsM.getSize() < mi.TimeStepManager.noModelTimeSteps)
                {
                    node.m.adaInflowsM.setDataL(mi.TimeStepManager.noModelTimeSteps - 1, 0);
                    XYFileReader.FillDatesOldTimeSeries(mi, node.m.adaInflowsM);
                    node.m.adaInflowsM.VariesByYear = true;
                }
                for (timeStepIndex = 0; timeStepIndex < node.m.adaInflowsM.getSize(); timeStepIndex++)
                {
                    Math.DivRem(timeStepIndex, lper, out distIndex);
                    value = Convert.ToInt32(node.m.adaInflowsM.getDataL(timeStepIndex) + node.m.import * node.m.dist[distIndex]);
                    node.m.adaInflowsM.setDataL(timeStepIndex, value);
                }
                node.m.import = 0;
                for (i = 0; i < 12; i++)
                {
                    node.m.dist[i] = 0;
                }
            }
            node.m.import = 0;
            for (i = 0; i < 12; i++)
            {
                node.m.dist[i] = 0;
            }
        }
    }
}
