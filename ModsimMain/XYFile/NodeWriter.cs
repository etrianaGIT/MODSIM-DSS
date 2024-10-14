using System;
using System.IO;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.ModsimIO
{
    // node class handles writing the Nodes to the xy file
    // Example:::
    //====
    //node
    //name gainBearUS
    //num 147
    //ntype 2
    //num_out 2
    //out 176
    //out 163
    //adainfm
    //0 781
    //1 2842
    //2
    //resnext 147
    //resprev 147
    //pos
    //0 1860
    //1 2320
    //epos
    //0-1 -842150451
    //
    public class NodeWriter
    {
        // writes the details for nodes to xy file
        public static void WriteNodes(Model mi, StreamWriter xyOutStream)
        {
            int sz = mi.NodeCount;
            int i = 0;
            int nodeNumber = 0;
            Node node = null;
            for (nodeNumber = 1; nodeNumber <= sz; nodeNumber++)
            {
                node = mi.FindNode(nodeNumber);
                xyOutStream.WriteLine("node");
                xyOutStream.WriteLine("name " + node.name);
                xyOutStream.WriteLine("nuid " + node.uid.ToString());
                xyOutStream.WriteLine("desc " + node.description);
                xyOutStream.WriteLine("num " + node.number);
                xyOutStream.WriteLine("ntype " + (int)node.nodeType);
                if (node.InflowLinks != null)
                {
                    for (i = 0; i < node.InflowLinks.Count(); i++)
                    {
                        xyOutStream.WriteLine("in " + node.InflowLinks.Item(node.InflowLinks.Count() - 1 - i).number);
                    }
                }
                if (node.OutflowLinks != null)
                {
                    for (i = 0; i < node.OutflowLinks.Count(); i++)
                    {
                        xyOutStream.WriteLine("out " + node.OutflowLinks.Item(node.OutflowLinks.Count() - 1 - i).number);
                    }
                }
                if (node.nodeType != NodeType.NonStorage)
                {
                    if (node.nodeType == NodeType.Reservoir)
                    {
                        XYFileWriter.WriteIndexedIntList("priority", node.m.priority, 100, xyOutStream);
                    }
                    XYFileWriter.WriteInteger("hydtable", xyOutStream, node.m.hydTable, 0);
                    LagInfo.WriteLagInfo("inflagi", node.m.infLagi, mi, node, xyOutStream);
                }
                if (node.nodeType == NodeType.NonStorage)
                {
                    if (node.m.adaInflowsM.getSize() > 0)
                    {
                        if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("tsinflow", node.m.adaInflowsM, xyOutStream);
                    }
                }
                xyOutStream.WriteLine("pos");
                xyOutStream.WriteLine("0 " + node.graphics.nodeLoc.X.ToString("F4"));
                xyOutStream.WriteLine("1 " + node.graphics.nodeLoc.Y.ToString("F4"));
                xyOutStream.WriteLine("labelpos");
                xyOutStream.WriteLine("0 " + node.graphics.nodeLabelLoc.X.ToString("F4"));
                xyOutStream.WriteLine("1 " + node.graphics.nodeLabelLoc.Y.ToString("F4"));
                if (!node.graphics.visible)
                {
                    xyOutStream.WriteLine("IsVisible " + Convert.ToInt32(node.graphics.visible));
                }
                if (node.graphics.storageRightReservoir)
                {
                    xyOutStream.WriteLine("IsStorageRightRes " + Convert.ToInt32(node.graphics.storageRightReservoir));
                }
                if (node.graphics.labelVisible == false)
                {
                    xyOutStream.WriteLine("IsLabelVisible " + Convert.ToInt32(node.graphics.labelVisible));
                }
                if (!node.m.selected)
                {
                    XYFileWriter.WriteInteger("select", xyOutStream, 0, 1);
                }
                if (node.nodeType == NodeType.Demand)
                {
                    WriteDemandDetails(mi, node, xyOutStream);
                }
                else if (node.nodeType == NodeType.Reservoir)
                {
                    WriteReservoirDetails(mi, node, xyOutStream);
                }
                else if (node.nodeType == NodeType.NonStorage)
                {
                    WriteNonStorageDetails(mi, node, xyOutStream);
                }
                else if (!(node.nodeType == NodeType.Sink))
                {
                    //Throw New Exception("Error: Error in node type. type is " & node.nodeType)
                    mi.FireOnError("Error in node type. Type is " + node.nodeType);
                }
                if (node.m.GWStorageOnly)
                {
                    XYFileWriter.WriteInteger("GWStorOnly", xyOutStream, 1, 0);
                }
            }
        }
        //WriteDemandDetails writes demand data for the node specified.
        private static void WriteDemandDetails(Model mi, Node node, StreamWriter xyOutStream)
        {
            try
            {
                XYFileWriter.WriteInteger("pcap", xyOutStream, node.m.pcap, 0);
                if (node.m.pcapUnits != null)
                {
                    xyOutStream.WriteLine("pcapUnits " + node.m.pcapUnits.Label);
                }
                XYFileWriter.WriteInteger("pcost", xyOutStream, node.m.pcost, 0);
                XYFileWriter.WriteFloat("spyld", xyOutStream, node.m.spyld, 0);
                XYFileWriter.WriteFloat("trans", xyOutStream, node.m.trans, 0);
                XYFileWriter.WriteFloat("ddist", xyOutStream, node.m.Distance, 0);
                XYFileWriter.WriteIndexedIntList("demr", node.m.demr, 100, xyOutStream); //Priority of demand
                XYFileWriter.WriteNodeNumber("demDirect", node.m.demDirect, xyOutStream);
                XYFileWriter.WriteNodeNumberList("idstrmx", node.m.idstrmx, xyOutStream);
                XYFileWriter.WriteIndexedFloatList("idstrmfr", node.m.idstrmfraction, 1, xyOutStream);
                XYFileWriter.WriteNodeNumber("jdstrm", node.m.jdstrm, xyOutStream); //exchange credit node
                XYFileWriter.WriteLinkNumber("pdstrm", node.m.pdstrm, xyOutStream); //bypass credit link
                if (node.m.adaDemandsM.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("tsdemand", node.m.adaDemandsM, xyOutStream);
                }
                LagInfo.WriteLagInfo("pumplagi", node.m.pumpLagi, mi, node, xyOutStream);
                XYFileWriter.WriteIndexedFloatList("nwatchfact", node.m.watchFactors, 1, xyOutStream);
                XYFileWriter.WriteLinkNumberList("nwatchmax", node.m.watchMaxLinks, xyOutStream);
                XYFileWriter.WriteLinkNumberList("nwatchmin", node.m.watchMinLinks, xyOutStream);
                XYFileWriter.WriteLinkNumberList("nwatchlog", node.m.watchLogLinks, xyOutStream);
                XYFileWriter.WriteLinkNumberList("nwatchln", node.m.watchLnLinks, xyOutStream);
                XYFileWriter.WriteLinkNumberList("nwatchexp", node.m.watchExpLinks, xyOutStream);
                XYFileWriter.WriteLinkNumberList("nwatchxsq", node.m.watchSqrLinks, xyOutStream);
                XYFileWriter.WriteLinkNumberList("nwatchpow", node.m.watchPowLinks, xyOutStream);
                XYFileWriter.WriteFloat("npowval", xyOutStream, node.m.powvalue, 0); //exponent for nwatchpow
                if (node.m.adaInfiltrationsM.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("tsinfiltration", node.m.adaInfiltrationsM, xyOutStream);
                }
            }
            catch (Exception ex)
            {
                mi.FireOnError("Exception in WriteDemandDetails " + ex.Message);
            }
        }
        //WriteReservoirDetails writes all reservoir information for the specified node
        private static void WriteReservoirDetails(Model mi, Node node, StreamWriter xyOutStream)
        {
            try
            {
                XYFileWriter.WriteInteger("max_vol", xyOutStream, node.m.max_volume, 0);
                XYFileWriter.WriteInteger("min_vol", xyOutStream, node.m.min_volume, 0);
                XYFileWriter.WriteInteger("start_vol", xyOutStream, node.m.starting_volume, 0);
                if (node.m.reservoir_units != null)
                {
                    xyOutStream.WriteLine("reservoir_units " + node.m.reservoir_units.Label);
                }
                XYFileWriter.WriteFloat("powmax", xyOutStream, node.m.powmax, 0);
                XYFileWriter.WriteFloat("elev", xyOutStream, node.m.elev, 0);
                if (node.m.peakGeneration)
                {
                    XYFileWriter.WriteInteger("ipeak", xyOutStream, 1, 0);
                }
                HydroWriter.WriteEfficiency(mi, node.m.ResEffCurve, xyOutStream, true);
                if (node.m.spyld != 0.0)
                {
                    XYFileWriter.WriteFloat("spyld", xyOutStream, node.m.spyld, 0);
                }
                if (node.m.trans != 0.0)
                {
                    XYFileWriter.WriteFloat("trans", xyOutStream, node.m.trans, 0);
                }
                if (node.m.Distance != 0.0)
                {
                    XYFileWriter.WriteFloat("ddist", xyOutStream, node.m.Distance, 0);
                }
                if (node.m.adaInfiltrationsM.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("tsinfiltration", node.m.adaInfiltrationsM, xyOutStream);
                }
                if (node.m.seepg != 0.0)
                {
                    XYFileWriter.WriteFloat("seepg", xyOutStream, node.m.seepg, 0);
                }
                if (node.m.resBypassL != null)
                {
                    XYFileWriter.WriteLinkNumber("rbypassl", node.m.resBypassL, xyOutStream);
                }
                // we HAVE to write parent flag of zero if it is a child reservoir
                if (node.parentFlag == false)
                {
                    XYFileWriter.WriteInteger("parent", xyOutStream, 0, 1);
                }
                XYFileWriter.WriteNodeNumber("mymom", node.myMother, xyOutStream);
                XYFileWriter.WriteInteger("numkids", xyOutStream, node.numChildren, 0);
                //ResNext and ResPrev need to exist in order to work properly
                //( They can go away when child reservoir code is gone)
                if (node.RESnext == null)
                {
                    node.RESnext = node;
                }
                XYFileWriter.WriteNodeNumber("resnext", node.RESnext, xyOutStream);
                if (node.RESprev == null)
                {
                    node.RESprev = node;
                }
                XYFileWriter.WriteNodeNumber("resprev", node.RESprev, xyOutStream);
                if (node.m.adaEvaporationsM.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("tsevaprate", node.m.adaEvaporationsM, xyOutStream);
                }
                if (node.m.adaGeneratingHrsM.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("tsgeneratehrs", node.m.adaGeneratingHrsM, xyOutStream);
                }
                if (node.m.lastFillLink != null)
                {
                    XYFileWriter.WriteLinkNumber("reslastfilllink", node.m.lastFillLink, xyOutStream);
                }
                if (node.m.resOutLink != null)
                {
                    XYFileWriter.WriteLinkNumber("resoutfl", node.m.resOutLink, xyOutStream);
                }
                XYFileWriter.WriteInteger("sysnum", xyOutStream, node.m.sysnum, 0);
                if (node.m.adaTargetsM.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("tstarget", node.m.adaTargetsM, xyOutStream);
                }
                if (node.m.adaForecastsM.getSize() > 0)
                {
                    if (mi.timeseriesInfo.xyFileTimeSeries) XYFileWriter.WriteTimeSeries("tsforcast", node.m.adaForecastsM, xyOutStream);
                }
                XYFileWriter.WriteIndexedFloatList("apoints", node.m.apoints, 0, xyOutStream);
                XYFileWriter.WriteIndexedIntList("cpoints", node.m.cpoints, 0, xyOutStream);
                XYFileWriter.WriteIndexedFloatList("epoints", node.m.epoints, 0, xyOutStream);
                XYFileWriter.WriteIndexedIntList("hpoints", node.m.hpoints, 0, xyOutStream);
                if (node.m.area_units != null)
                {
                    xyOutStream.WriteLine("area_units " + node.m.area_units.Label);
                }
                if (node.m.capacity_units != null)
                {
                    xyOutStream.WriteLine("capacity_units " + node.m.capacity_units.Label);
                }
                if (node.m.hcapacity_units != null)
                {
                    xyOutStream.WriteLine("hcapacity_units " + node.m.hcapacity_units.Label);
                }
                if (node.m.resBalance != null)
                {
                    xyOutStream.WriteLine("resbali");
                    XYFileWriter.WriteIndexedIntList("resbaliprio", node.m.resBalance.incrPriorities, 0, xyOutStream);
                    XYFileWriter.WriteIndexedFloatList("resbalpcts", node.m.resBalance.targetPercentages, 0, xyOutStream);
                    if (node.m.resBalance.PercentBasedOnMaxCapacity)
                    {
                        XYFileWriter.WriteInteger("resbalflag", xyOutStream, 1, 0);
                    }
                }
                XYFileWriter.WriteIndexedIntList("flowpts", node.m.flowpts, 0, xyOutStream);
                XYFileWriter.WriteIndexedFloatList("twelevpts", node.m.twelevpts, 0, xyOutStream);
            }
            catch (Exception ex)
            {
                mi.FireOnError("Error in WriteReservoirDetails " + ex.Message);
            }
        }
        //WriteNonStorageDetails reads info for round nodes in xy file
        private static void WriteNonStorageDetails(Model mi, Node node, StreamWriter xyOutStream)
        {
            try
            {
                //XYFileWriter.WriteInteger("import", xyOutStream, node.m.import, 0);
                //XYFileWriter.WriteIndexedFloatList("dist", node.m.dist, 0, xyOutStream);
                XYFileWriter.WriteNodeNumber("infFracNode", node.m.inflowFracNode, xyOutStream);
                XYFileWriter.WriteFloat("infFrac", xyOutStream, node.m.inflowFactor, 0);
            }
            catch (Exception ex)
            {
                mi.FireOnError("Error in WriteNonStorageDetails " + ex.Message);
            }
        }
    }
}
