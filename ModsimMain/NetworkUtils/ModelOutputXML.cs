using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.NetworkUtils
{
    //public class ModelOutputXML
    //{
    //    private MODSIMOutputDS outDS;
    //    public enum LinkOutputType
    //    {
    //        Flow,
    //        Loss,
    //        NaturalFlow,
    //        StorLeft,
    //        Accrual,
    //        GroupStorLeft,
    //        GroupAccrual
    //    }
    //    public enum OutputTableType
    //    {
    //        Links,
    //        ReservoirFlows,
    //        ReservoirStorage,
    //        Demands,
    //        NonStorage
    //    }
    //    public ModelOutputXML(Model mi)
    //    {
    //        outDS = new MODSIMOutputDS(mi);
    //    }
    //    public void UpdateModelInfo(Model mi)
    //    {
    //        outDS = new MODSIMOutputDS(mi);
    //        DataTable LinkInfoTable = outDS.Tables["NodesInfo"];
    //        //add nodes
    //        Node myNode = mi.firstNode;
    //        while ((myNode != null))
    //        {
    //            DataRow infoRow = LinkInfoTable.NewRow();
    //            infoRow["NName"] = myNode.name;
    //            infoRow["NNumber"] = myNode.number;
    //            infoRow["NType"] = myNode.nodeType.ToString();
    //            LinkInfoTable.Rows.Add(infoRow);
    //            myNode = myNode.next;
    //        }
    //        //add links
    //        LinkInfoTable = outDS.Tables["linksInfo"];
    //        Link myLink = mi.firstLink;
    //        while ((myLink != null))
    //        {
    //            DataRow infoRow = LinkInfoTable.NewRow();
    //            infoRow["LName"] = myLink.name;
    //            infoRow["LNumber"] = myLink.number;
    //            infoRow["FromNode"] = myLink.@from.number;
    //            infoRow["toNode"] = myLink.to.number;
    //            LinkInfoTable.Rows.Add(infoRow);
    //            myLink = myLink.next;
    //        }
    //        LinkInfoTable = outDS.Tables["TimeSteps"];
    //        int i = 0;
    //        for (i = 0; i <= mi.TimeStepManager.noModelTimeSteps; i++)
    //        {
    //            DataRow infoRow = LinkInfoTable.NewRow();
    //            infoRow["TSDate"] = mi.TimeStepManager.Index2Date(i, TypeIndexes.ModelIndex);
    //            infoRow["EndDate"] = mi.TimeStepManager.Index2EndDate(i, TypeIndexes.ModelIndex);
    //            infoRow["TSIndex"] = i;
    //            LinkInfoTable.Rows.Add(infoRow);
    //        }
    //    }
    //    public void AddLinksOutput(Model mi)
    //    {
    //        DataTable LinkInfoTable = outDS.Tables["LinksOutput"];
    //        Link myLink = mi.firstLink;
    //        int TSIndex = mi.mInfo.CurrentModelTimeStepIndex;
    //        int monIndex = mi.TimeStepManager.GetMonthIndex(TSIndex, TypeIndexes.ModelIndex);
    //        while ((myLink != null))
    //        {
    //            if (!myLink.mlInfo.isArtificial)
    //            {
    //                DataRow infoRow = LinkInfoTable.NewRow();
    //                //infoRow("Date") = "#" & mi.mInfo.Month & "/" & mi.mInfo.Day & "/" & mi.mInfo.Year & " #"
    //                infoRow["TSIndex"] = TSIndex;
    //                infoRow["LNumber"] = myLink.number;
    //                if (OutputControlInfo.flo_flow)
    //                {
    //                    //if (myLink.mrlInfo.link_flow[monIndex] != mi.defaultMaxCap)
    //                        infoRow["Flow"] = myLink.mrlInfo.link_flow[monIndex] / mi.ScaleFactor;
    //                    //else
    //                    //    infoRow["Flow"] = mi.defaultMaxCap;
    //                }
    //                //If (OutputControlInfo.loss) Then infoRow("Loss") = myLink.mrlInfo.closs(monIndex)
    //                if (OutputControlInfo.loss)
    //                {
    //                    //if (myLink.mrlInfo.link_closs[monIndex] != mi.defaultMaxCap)
    //                        infoRow["Loss"] = myLink.mrlInfo.link_closs[monIndex] / mi.ScaleFactor;
    //                    //else
    //                    //    infoRow["Loss"] = mi.defaultMaxCap;
    //                }
    //                if (OutputControlInfo.natflow)
    //                {
    //                    //if (myLink.mrlInfo.natFlow[monIndex] != mi.defaultMaxCap)
    //                        infoRow["NaturalFlow"] = myLink.mrlInfo.natFlow[monIndex] / mi.ScaleFactor;
    //                    //else
    //                    //    infoRow["NaturalFlow"] = mi.defaultMaxCap;
    //                }
    //                //if (myLink.mlInfo.hi != mi.defaultMaxCap)
    //                    infoRow["LMax"] = myLink.mlInfo.hi / mi.ScaleFactor;
    //                //else
    //                //    infoRow["LMax"] = mi.defaultMaxCap;
    //                //if (myLink.mlInfo.lo != mi.defaultMaxCap)
    //                    infoRow["LMin"] = myLink.mlInfo.lo / mi.ScaleFactor;
    //                //else
    //                //    infoRow["LMin"] = mi.defaultMaxCap;
    //                if (mi.IsAccrualLink(myLink))
    //                {
    //                    int owners = 0;
    //                    double sumStorLeft = 0;
    //                    double sumAccr = 0;
    //                    for (owners = 0; owners < mi.mInfo.ownerList.Length; owners++)
    //                    {
    //                        Link curLink = mi.mInfo.ownerList[owners];
    //                        //Include the group links 
    //                        if ((object.ReferenceEquals(curLink.m.accrualLink, myLink)))
    //                        {
    //                            //When groups the owner links don't have storage left = (0)
    //                            sumStorLeft += curLink.mrlInfo.link_store[monIndex];
    //                            sumAccr += curLink.mrlInfo.link_accrual[monIndex];
    //                        }
    //                    }
    //                    if (OutputControlInfo.stgl)
    //                    {
    //                        //if (Convert.ToInt64(sumStorLeft) != mi.defaultMaxCap)
    //                        infoRow["StorLeft"] = sumStorLeft / mi.ScaleFactor;
    //                        //else
    //                        //    infoRow["StorLeft"] = mi.defaultMaxCap;
    //                    }
    //                    if (OutputControlInfo.acrl)
    //                    {
    //                        //if (Convert.ToInt64(sumAccr) != mi.defaultMaxCap)
    //                        infoRow["Accrual"] = sumAccr / mi.ScaleFactor;
    //                        //else
    //                        //    infoRow["Accrual"] = mi.defaultMaxCap;
    //                    }
    //                }
    //                else
    //                {
    //                    if (OutputControlInfo.stgl)
    //                    {
    //                        //if (myLink.mrlInfo.link_store[monIndex] != mi.defaultMaxCap)
    //                        infoRow["StorLeft"] = myLink.mrlInfo.link_store[monIndex] / mi.ScaleFactor;
    //                        //else
    //                        //    infoRow["StorLeft"] = mi.defaultMaxCap;
    //                    }
    //                    if (OutputControlInfo.acrl)
    //                    {
    //                        //if (myLink.mrlInfo.link_accrual[monIndex] != mi.defaultMaxCap)
    //                        infoRow["Accrual"] = myLink.mrlInfo.link_accrual[monIndex] / mi.ScaleFactor;
    //                        //else
    //                        //    infoRow["Accrual"] = mi.defaultMaxCap;
    //                    }
    //                }

    //                if ((OutputControlInfo.stgl & myLink.m.groupNumber > 0 & (myLink.m.accrualLink != null)))
    //                {
    //                    LinkList m_AccGroupLList = myLink.m.accrualLink.mlInfo.cLinkL;
    //                    while ((m_AccGroupLList != null))
    //                    {
    //                        Link curLink = m_AccGroupLList.link;
    //                        if ((curLink.mrlInfo.groupID == myLink.m.groupNumber))
    //                        {
    //                            infoRow["GroupLink"] = curLink.name;
    //                            //if (curLink.mrlInfo.link_store[monIndex] != mi.defaultMaxCap)
    //                            infoRow["GroupStorLeft"] = curLink.mrlInfo.link_store[monIndex] / mi.ScaleFactor;
    //                            //else
    //                            //    infoRow["GroupStorLeft"] = mi.defaultMaxCap;
    //                            //if (curLink.mrlInfo.link_accrual[monIndex] != mi.defaultMaxCap)
    //                            infoRow["GroupAccrual"] = curLink.mrlInfo.link_accrual[monIndex] / mi.ScaleFactor;
    //                            //else
    //                            //    infoRow["GroupAccrual"] = mi.defaultMaxCap;
    //                        }
    //                        m_AccGroupLList = m_AccGroupLList.next;
    //                    }
    //                }
    //                LinkInfoTable.Rows.Add(infoRow);
    //            }
    //            myLink = myLink.next;
    //        }
    //    }
    //    public void AddNodesOutput(Model mi)
    //    {
    //        Node myNode = mi.firstNode;
    //        while ((myNode != null))
    //        {
    //            if ((myNode.nodeType == NodeType.Demand) | (myNode.nodeType == NodeType.Sink))
    //            {
    //                AddDemandOutput(myNode, mi);
    //            }
    //            if ((myNode.nodeType == NodeType.NonStorage))
    //            {
    //                AddNonStorageOutput(myNode, mi);
    //            }
    //            if ((myNode.nodeType == NodeType.Reservoir))
    //            {
    //                AddReservoirOutput(myNode, mi);
    //            }
    //            myNode = myNode.next;
    //        }
    //    }
    //    private void AddDemandOutput(Node mynode, Model mi)
    //    {
    //        DataTable NodeOutTable = outDS.Tables["DEMOutput"];
    //        //Dim myNode As Node = mi.firstNode
    //        int TSIndex = mi.mInfo.CurrentModelTimeStepIndex;
    //        int monIndex = mi.TimeStepManager.GetMonthIndex(TSIndex, TypeIndexes.ModelIndex);
    //        //Do While Not (myNode Is Nothing)
    //        //    If (myNode.NodeType = NodeType.Demand) Or (myNode.NodeType = NodeType.Sink) Then
    //        DataRow NodeOutRow = NodeOutTable.NewRow();
    //        NodeOutRow["TSIndex"] = TSIndex;
    //        NodeOutRow["NNo"] = mynode.number;
    //        if (OutputControlInfo.demand)
    //        {
    //            //if (mynode.mnInfo.demand[TSIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Demand"] = mynode.mnInfo.demand[TSIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Demand"] = mi.defaultMaxCap;
    //        }
    //        //Calculate total avaliable surface water. 
    //        long iswat = mynode.mnInfo.upstrm_release[monIndex] + mynode.mnInfo.canal_in[monIndex] + mynode.mnInfo.irtnflowthruNF_OUT[monIndex] + mynode.mnInfo.unreg_inflow[monIndex];
    //        long t = mynode.mnInfo.demand[TSIndex] - mynode.mnInfo.demand_shortage[monIndex];
    //        t = System.Math.Max(0, t);
    //        iswat = System.Math.Max(0, System.Math.Min(iswat, t));
    //        if (OutputControlInfo.surf_in)
    //        {
    //            //if (iswat != mi.defaultMaxCap)
    //            NodeOutRow["Surf_In"] = iswat / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Surf_In"] = mi.defaultMaxCap;
    //        }

    //        ///* Estimate groundwater. */
    //        long igwat = t - iswat;
    //        igwat = System.Math.Max(0, igwat);
    //        if (OutputControlInfo.gw_in)
    //        {
    //            //if (System.Math.Min(igwat, mynode.mnInfo.gw_to_node[monIndex]) != mi.defaultMaxCap)
    //            NodeOutRow["Gw_In"] = System.Math.Min(igwat, mynode.mnInfo.gw_to_node[monIndex]) / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Gw_In"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.dem_sht)
    //        {
    //            //if (mynode.mnInfo.ishtm[TSIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Shortage"] = mynode.mnInfo.ishtm[TSIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Shortage"] = mi.defaultMaxCap;
    //        }
    //        NodeOutTable.Rows.Add(NodeOutRow);
    //        //    End If
    //        //    myNode = myNode.next
    //        //Loop
    //    }
    //    private void AddNonStorageOutput(Node mynode, Model mi)
    //    {
    //        DataTable NodeOutTable = outDS.Tables["NON_STOROutput"];
    //        int TSIndex = mi.mInfo.CurrentModelTimeStepIndex;
    //        if (mynode.mnInfo.inflow.Length > 0)
    //        {
    //            DataRow NodeOutRow = NodeOutTable.NewRow();
    //            NodeOutRow["TSIndex"] = TSIndex;
    //            NodeOutRow["NNo"] = mynode.number;
    //            //if (mynode.mnInfo.inflow[TSIndex, 0] != mi.defaultMaxCap)
    //            NodeOutRow["Inflow"] = mynode.mnInfo.inflow[TSIndex, 0] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Inflow"] = mi.defaultMaxCap;
    //            //Assumed no multicolumn
    //            NodeOutTable.Rows.Add(NodeOutRow);
    //        }
    //    }
    //    private void AddReservoirOutput(Node myNode, Model mi)
    //    {
    //        DataTable NodeOutTable = outDS.Tables["RESOutput"];
    //        DataTable NodeOutTable_STOR = outDS.Tables["RES_STOROutput"];
    //        //Dim myNode As Node = mi.firstNode
    //        int TSIndex = mi.mInfo.CurrentModelTimeStepIndex;
    //        System.DateTime TSDate = mi.TimeStepManager.Index2Date(TSIndex, TypeIndexes.ModelIndex);
    //        int monIndex = mi.TimeStepManager.GetMonthIndex(TSIndex, TypeIndexes.ModelIndex);
    //        //Do While Not (myNode Is Nothing)
    //        //If (myNode.NodeType = NodeType.Reservoir) Then
    //        DataRow NodeOutRow = NodeOutTable.NewRow();
    //        DataRow NodeOutRow_STOR = NodeOutTable_STOR.NewRow();
    //        NodeOutRow["TSIndex"] = TSIndex;
    //        NodeOutRow["NNo"] = myNode.number;
    //        NodeOutRow_STOR["TSIndex"] = TSIndex;
    //        NodeOutRow_STOR["NNo"] = myNode.number;
    //        if (OutputControlInfo.stor_beg)
    //        {
    //            //if (myNode.mnInfo.start_storage[mi.mInfo.CurrentModelTimeStepIndex] != mi.defaultMaxCap)
    //            NodeOutRow_STOR["Stor_Beg"] = myNode.mnInfo.start_storage[mi.mInfo.CurrentModelTimeStepIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow_STOR["Stor_Beg"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.stor_end)
    //        {
    //            //if (myNode.mnInfo.end_storage[mi.mInfo.CurrentModelTimeStepIndex] != mi.defaultMaxCap)
    //            NodeOutRow_STOR["Stor_End"] = myNode.mnInfo.end_storage[mi.mInfo.CurrentModelTimeStepIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow_STOR["Stor_End"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.stor_trg)
    //        {
    //            //if (myNode.mnInfo.trg_storage[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow_STOR["Stor_Trg"] = myNode.mnInfo.trg_storage[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow_STOR["Stor_Trg"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.spills)
    //        {
    //            //if (myNode.mnInfo.res_spill[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Spills"] = myNode.mnInfo.res_spill[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Spills"] = mi.defaultMaxCap;
    //        }
    //        //TODO: check this value in the new time series 
    //        //If (OutputControlInfo.evp_rate) Then NodeOutRow("Evap_Rate") = myNode.m.adaEvaporationsM.getDataF(TSIndex, 0)
    //        if (OutputControlInfo.evp_rate)
    //        {
    //            //if (myNode.m.adaEvaporationsM.getDataF(TSIndex, 0) != mi.defaultMaxCap)
    //            NodeOutRow["Evap_Rate"] = myNode.m.adaEvaporationsM.getDataF(TSIndex, 0) / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Evap_Rate"] = mi.defaultMaxCap;
    //        }
    //        // should be using array NOT TimeSeries
    //        if (OutputControlInfo.evp_loss)
    //        {
    //            //if (myNode.mnInfo.reservoir_evaporation[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Evap_Loss"] = myNode.mnInfo.reservoir_evaporation[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Evap_Loss"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.seepage)
    //        {
    //            // if (myNode.mnInfo.iseepr[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Seepage"] = myNode.mnInfo.iseepr[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Seepage"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.unreg_in)
    //        {
    //            //if (myNode.mnInfo.unreg_inflow[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Unreg_In"] = myNode.mnInfo.unreg_inflow[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Unreg_In"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.ups_rel)
    //        {
    //            //if (myNode.mnInfo.upstrm_release[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Ups_Rel"] = myNode.mnInfo.upstrm_release[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Ups_Rel"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.pump_in)
    //        {
    //            //if (myNode.mnInfo.canal_in[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Pump_In"] = myNode.mnInfo.canal_in[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Pump_In"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.gwater)
    //        {
    //            //if (myNode.mnInfo.gw_to_node[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["GWater"] = myNode.mnInfo.gw_to_node[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["GWater"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.dws_rel)
    //        {
    //            //if (myNode.mnInfo.downstrm_release[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Dws_Rel"] = myNode.mnInfo.downstrm_release[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Dws_Rel"] = mi.defaultMaxCap;
    //        }
    //        if (OutputControlInfo.pump_out)
    //        {
    //            //if (myNode.mnInfo.canal_out[monIndex] != mi.defaultMaxCap)
    //            NodeOutRow["Pump_Out"] = myNode.mnInfo.canal_out[monIndex] / mi.ScaleFactor;
    //            //else
    //            //    NodeOutRow["Pump_Out"] = mi.defaultMaxCap;
    //        }
    //        double rhead = myNode.mnInfo.avg_head[monIndex];
    //        if ((rhead < 0))
    //            rhead = 0;
    //        if (OutputControlInfo.head_avg)
    //            NodeOutRow["Head_Avg"] = rhead;
    //        if (OutputControlInfo.powr_avg)
    //            NodeOutRow["Powr_Avg"] = Convert.ToUInt32(myNode.mnInfo.avg_hydropower[monIndex] + DefineConstants.ROFF);
    //        // 0.4999999)
    //        double ghrs = Convert.ToDouble(myNode.m.adaGeneratingHrsM.getDataF(TSIndex));
    //        // should be using array NOT TimeSeries
    //        long ieng = Convert.ToInt64(Convert.ToDouble(myNode.mnInfo.avg_hydropower[monIndex]) * ghrs / 1000.0 + DefineConstants.ROFF);
    //        // 0.4999999)
    //        if ((OutputControlInfo.powr_pk))
    //            NodeOutRow["Powr_Pk"] = ieng;
    //        long i2nd = 0;
    //        if ((myNode.m.peakGeneration))
    //        {
    //            i2nd = 0;
    //        }
    //        else
    //        {
    //            ghrs = mi.timeStep.ToTimeSpan(TSDate).TotalHours - ghrs;
    //            if ((ghrs < 0.0))
    //                ghrs = 0.0;
    //            i2nd = Convert.ToInt64(Convert.ToDouble(myNode.mnInfo.avg_hydropower[monIndex]) * ghrs / 1000.0 + DefineConstants.ROFF);
    //            // 0.4999999)
    //        }
    //        if ((OutputControlInfo.pwr_2nd))
    //            NodeOutRow["Pwr_2nd"] = i2nd;
    //        //TODO: area cannot be called because the procedure is not referenced in this vb project.
    //        //Dim endingelev As Long
    //        //area(myNode.mnInfo.end_storage(monIndex), fsur, endingelev, q, myNode)
    //        //If (OutputControlInfo.elev_end) Then NodeOutRow("Elev_End") = endingelev

    //        NodeOutTable.Rows.Add(NodeOutRow);
    //        NodeOutTable_STOR.Rows.Add(NodeOutRow_STOR);
    //        //End If
    //        //myNode = myNode.next
    //        //Loop
    //    }
    //    public void SaveOutputToXML(string modelFileName)
    //    {
    //        string OutName = BaseNameString(modelFileName) + "OUTPUT.xml";
    //        System.IO.StreamWriter xmlSW = new System.IO.StreamWriter(OutName, true);


    //        //'XML inclusion during XML reading:

    //        //Dim reader As XIncludingReader = New XIncludingReader(BaseNameString(modelFileName) & "OUTPUT.xml")
    //        //'Dim doc As Xml.XPath.XPathDocument = New Xml.XPath.XPathDocument(reader)
    //        //Dim doc As Xml.XmlDocument = New Xml.XmlDocument
    //        //doc.Load(reader)
    //        //doc.
    //        //'Dim xslt As Xsl.XslTransform = New Xsl.XslTransform
    //        //'xslt.Load("stylesheet.xslt")
    //        //outDS.WriteXml(xmlSW)
    //        //xslt.Transform(doc, , xmlSW)
    //        //xmlSW.Close()
    //        //outDS = New MODSIMOutputDS
    //        //'        'XML inclusion while building XmlDocument:

    //        //'XmlReader reader = new XIncludingReader(new XmlTextReader("source.xml"));
    //        //'XmlDocument doc = new XmlDocument();			    
    //        //'doc.Load(reader);
    //        //'...        


    //        //'XML inclusion before an XSL Transformation:

    //        //'XslTransform xslt = new XslTransform();
    //        //'xslt.Load("stylesheet.xsl");
    //        //'XmlReader reader = new XIncludingReader("source.xml");
    //        //'XPathDocument xdoc = new XPathDocument(reader);
    //        //'xslt.Transform(xdoc, null, new StreamWriter("result.xml"));


    //    }
    //    public void LoadOutputFromXML(Model mi, string modelFileName)
    //    {
    //        string OutName = null;
    //        try
    //        {
    //            OutName = BaseNameString(modelFileName) + "OUTPUT.xml";
    //            outDS = new MODSIMOutputDS(mi);
    //            StreamReader xmlSW = new System.IO.StreamReader(OutName);
    //            outDS.ReadXml(xmlSW, XmlReadMode.IgnoreSchema);
    //            xmlSW.Close();
    //            CalculateMidDates();
    //        }
    //        catch
    //        {
    //            //MessageBox.Show("Output file " & OutName & " is not available")
    //        }
    //    }
    //    private string BaseNameString(string mName)
    //    {
    //        string functionReturnValue = null;
    //        string[] OutName = mName.Split('.');
    //        int i = 0;
    //        functionReturnValue = "";
    //        for (i = 0; i <= OutName.Length - 2; i++)
    //        {
    //            functionReturnValue += OutName[i];
    //            if (i < OutName.Length - 2)
    //                functionReturnValue += ".";
    //        }
    //        return functionReturnValue;
    //    }
    //    public long LinkOutputQuery(LinkOutputType OutputType, string linkName, int timeStepIndex)
    //    {
    //        int linkNo = GetLinkNo(linkName);
    //        DataTable LinkInfoTable = outDS.Tables["LinksOutput"];
    //        DataRow[] rows = LinkInfoTable.Select("Number = " + linkNo + " AND TSIndex = " + timeStepIndex);
    //        if (rows.Length != 1)
    //            return -1;
    //        return Convert.ToInt64(rows[0][OutputType.ToString()]);
    //    }
    //    public long LinkOutputQuery(LinkOutputType OutputType, int linkNo, int timeStepIndex)
    //    {
    //        DataTable LinkInfoTable = outDS.Tables["LinksOutput"];
    //        DataRow[] rows = LinkInfoTable.Select("Number = " + linkNo + " AND TSIndex = " + timeStepIndex);
    //        if (rows.Length != 1)
    //            return -1;
    //        return Convert.ToInt64(rows[0][OutputType.ToString()]);
    //    }
    //    public DataTable[] LinkOutput(string linkName, string AddLinkName = null)
    //    {
    //        int LinkNo = GetLinkNo(linkName);
    //        int AddLinkNo = 0;
    //        if (AddLinkName != null)
    //        {
    //            AddLinkNo = GetLinkNo(linkName);
    //            return LinkOutput(LinkNo, AddLinkNo);
    //        }
    //        else
    //        {
    //            return LinkOutput(LinkNo);
    //        }
    //    }
    //    private DataTable[] LinkOutput(int linkNo, int AddLinkNo = 0)
    //    {
    //        DataTable LinkInfoTable = outDS.Tables["LinksOutput"];
    //        DataTable TSInfoTable = outDS.Tables["TimeSteps"];
    //        // Sort descending by CompanyName column.
    //        DataRow[] rows = LinkInfoTable.Select("Number = " + linkNo);
    //        DataRow[] testStorRows = LinkInfoTable.Select("Number = " + linkNo + " AND (StorLeft > 0 or Accrual > 0 or GroupStorLeft > 0)");
    //        bool hasStorage = false;
    //        if (testStorRows.Length > 0)
    //            hasStorage = true;
    //        if (AddLinkNo > 0)
    //        {
    //            DataRow[] rowsAdd = LinkInfoTable.Select("Number = " + AddLinkNo);
    //        }
    //        int i = 0;
    //        DataTable outTable = new DataTable();
    //        outTable.Columns.Add("Mid Date", typeof(DateTime));
    //        outTable.Columns.Add("Start Date", typeof(DateTime));
    //        outTable.Columns.Add("End Date", typeof(DateTime));
    //        outTable.Columns.Add("Flow", typeof(long));
    //        outTable.Columns.Add("Loss", typeof(long));
    //        outTable.Columns.Add("NaturalFlow", typeof(long));
    //        outTable.Columns.Add("Min", typeof(long));
    //        outTable.Columns.Add("Max", typeof(long));
    //        for (i = 0; i < outTable.Columns.Count; i++)
    //        {
    //            outTable.Columns[i].ReadOnly = true;
    //        }
    //        string groupColStor = "";
    //        string groupColAccr = "";
    //        if (hasStorage)
    //        {
    //            outTable.Columns.Add("StorageLeft", typeof(long));
    //            outTable.Columns.Add("Accrual", typeof(long));
    //            if (!(rows[0]["GroupLink"] == DBNull.Value))
    //            {
    //                groupColStor = rows[0]["GroupLink"].ToString() + "_StorLeft";
    //                groupColAccr = rows[0]["GroupLink"].ToString() + "_Accrual";
    //                outTable.Columns.Add(groupColStor, typeof(long));
    //                outTable.Columns.Add(groupColAccr, typeof(long));
    //            }
    //        }
    //        CalculateMidDates();
    //        for (i = 0; i < rows.Length; i++)
    //        {
    //            DataRow[] m_daterow = TSInfoTable.Select("TSIndex = " + rows[i]["TSIndex"].ToString());
    //            DataRow m_row = outTable.NewRow();
    //            m_row["Mid Date"] = m_daterow[0]["MidDate"];
    //            m_row["Start Date"] = m_daterow[0]["Date"];
    //            m_row["End Date"] = Convert.ToDateTime(m_daterow[0]["EndDate"]);
    //            //.AddSeconds(-1)
    //            if (AddLinkNo > 0)
    //            {
    //                DataRow[] rowsAdd = LinkInfoTable.Select("Number = " + AddLinkNo + " AND TSIndex =" + rows[i]["TSIndex"].ToString());
    //                if (rowsAdd.Length == 1)
    //                {
    //                    m_row["Flow"] = Convert.ToDouble(rows[i]["Flow"]) + Convert.ToDouble(rowsAdd[0]["Flow"]);
    //                    m_row["Loss"] = Convert.ToDouble(rows[i]["Loss"]) + Convert.ToDouble(rowsAdd[0]["Loss"]);
    //                    m_row["NaturalFlow"] = Convert.ToDouble(rows[i]["NaturalFlow"]) + Convert.ToDouble(rowsAdd[0]["NaturalFlow"]);
    //                    m_row["Min"] = Convert.ToDouble(rows[i]["Min"]) + Convert.ToDouble(rowsAdd[0]["Min"]);
    //                    m_row["Max"] = Convert.ToDouble(rows[i]["Max"]) + Convert.ToDouble(rowsAdd[0]["Max"]);
    //                    if (hasStorage)
    //                    {
    //                        m_row["StorageLeft"] = Convert.ToDouble(rows[i]["StorLeft"]) + Convert.ToDouble(rowsAdd[0]["StorLeft"]);
    //                        m_row["Accrual"] = Convert.ToDouble(rows[i]["Accrual"]) + Convert.ToDouble(rowsAdd[0]["Accrual"]);
    //                        if (!(rows[i]["GroupLink"] == DBNull.Value))
    //                        {
    //                            m_row[groupColStor] = Convert.ToDouble(rows[i]["GroupStorLeft"]) + Convert.ToDouble(rowsAdd[0]["GroupStorLeft"]);
    //                            m_row[groupColAccr] = Convert.ToDouble(rows[i]["GroupAccrual"]) + Convert.ToDouble(rowsAdd[0]["GroupAccrual"]);
    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    MessageBox.Show("Problem finding the link Number = " + AddLinkNo + " AND TSIndex =" + rows[i]["TSIndex"].ToString());
    //                }
    //            }
    //            else
    //            {
    //                m_row["Flow"] = rows[i]["Flow"];
    //                m_row["Loss"] = rows[i]["Loss"];
    //                m_row["NaturalFlow"] = rows[i]["NaturalFlow"];
    //                m_row["Min"] = rows[i]["Min"];
    //                m_row["Max"] = rows[i]["Max"];
    //                if (hasStorage)
    //                {
    //                    m_row["StorageLeft"] = rows[i]["StorLeft"];
    //                    m_row["Accrual"] = rows[i]["Accrual"];
    //                    if (!(rows[i]["GroupLink"] == DBNull.Value))
    //                    {
    //                        m_row[groupColStor] = rows[i]["GroupStorLeft"];
    //                        m_row[groupColAccr] = rows[i]["GroupAccrual"];
    //                    }
    //                }
    //            }
    //            outTable.Rows.Add(m_row);
    //        }
    //        DataTable[] returnTables = new DataTable[2];
    //        returnTables[0] = TSInfoTable;
    //        returnTables[1] = outTable;
    //        DataTable NodeInfoTable = outDS.Tables["LinksInfo"];
    //        rows = NodeInfoTable.Select("Number = " + linkNo);
    //        if (rows.Length > 0)
    //        {
    //            if (!string.IsNullOrEmpty(rows[0]["Name"].ToString()))
    //            {
    //                returnTables[1].TableName = rows[0]["Name"].ToString();
    //            }
    //            else
    //            {
    //                returnTables[1].TableName = rows[0]["Number"].ToString();
    //            }
    //        }
    //        return returnTables;
    //    }
    //    //Public Function LinkFlow(ByVal fromNodeNo As Integer, ByVal toNodeNo As Integer) As DataTable
    //    //    Dim LinksInfoTable As DataTable = outDS.Tables("LinksInfo")
    //    //    Dim linksrows() As DataRow = LinksInfoTable.Select("FromNode = " & fromNodeNo & " AND ToNode = " & toNodeNo)
    //    //    Dim links As Integer
    //    //    If linksrows.Length > 0 Then
    //    //        Dim LinkInfoTable As DataTable = outDS.Tables("LinksOutput")
    //    //        Dim TSInfoTable As DataTable = outDS.Tables("TimeSteps")
    //    //        Dim outTable As New DataTable

    //    //        outTable.Columns.Add("Date", GetType(DateTime))
    //    //        outTable.Columns.Add("Flow", Type.GetType("System.Int32"))
    //    //        For links = 0 To linksrows.Length - 1
    //    //            Dim linkNo As Integer = linksrows(links)("Number")
    //    //            ' Sort descending by CompanyName column.
    //    //            Dim rows() As DataRow = LinkInfoTable.Select("Number = " & linkNo)
    //    //            Dim i As Integer
    //    //            For i = 0 To rows.Length - 1
    //    //                Dim m_daterow() As DataRow = TSInfoTable.Select("TSIndex = " & rows(i)("TSIndex"))
    //    //                Dim m_row As DataRow = outTable.NewRow
    //    //                m_row("Date") = m_daterow(0)("Date")
    //    //                m_row("Flow") = rows(i)("Flow")
    //    //                outTable.Rows.Add(m_row)
    //    //            Next
    //    //        Next links
    //    //        Return outTable
    //    //    Else
    //    //        Throw New System.Exception("Link not found to display from node " & fromNodeNo & " and to node " & toNodeNo)
    //    //    End If
    //    //End Function
    //    public DataTable[] NodeOutput(string nodeName)
    //    {
    //        int nodeNo = GetNodeNo(nodeName);
    //        if (nodeNo > 0)
    //        {
    //            return NodeOutput(nodeNo);
    //        }
    //        else
    //        {
    //            MessageBox.Show("Node " + nodeName + " not found in the output");
    //            return null;
    //        }
    //    }
    //    private DataTable[] NodeOutput(int nodeNo)
    //    {
    //        DataTable NodeInfoTable = outDS.Tables["NodesInfo"];
    //        DataRow[] Noderow = NodeInfoTable.Select("NNo = " + nodeNo);
    //        NodeType nType = default(NodeType);
    //        if (Noderow.Length > 0)
    //        {
    //            nType = Node.GetType(Noderow[0]["NodeType"].ToString());
    //        }
    //        else
    //        {
    //            return null;
    //        }
    //        DataTable m_DataTable = null;
    //        if (nType == NodeType.Reservoir)
    //        {
    //            m_DataTable = outDS.Tables["RESOutput"];
    //        }
    //        else if (nType == NodeType.Demand | nType == NodeType.Sink)
    //        {
    //            m_DataTable = outDS.Tables["DEMOutput"];
    //        }
    //        else if (nType == NodeType.NonStorage)
    //        {
    //            m_DataTable = outDS.Tables["NON_STOROutput"];
    //        }
    //        else
    //        {
    //            return null;
    //        }
    //        DataTable TSInfoTable = outDS.Tables["TimeSteps"];
    //        // Sort descending by CompanyName column.
    //        DataRow[] rows = m_DataTable.Select("NNo = " + nodeNo);
    //        int i = 0;
    //        int j = 0;
    //        DataTable outTable = new DataTable();
    //        outTable = m_DataTable.Clone();
    //        outTable.Columns.Remove("TSIndex");
    //        outTable.Columns.Remove("NNo");
    //        outTable.Columns.Add("Mid Date", typeof(DateTime));
    //        outTable.Columns.Add("Start Date", typeof(DateTime));
    //        outTable.Columns.Add("End Date", typeof(DateTime));
    //        for (i = 0; i < outTable.Columns.Count; i++)
    //        {
    //            outTable.Columns[i].ReadOnly = true;
    //        }
    //        CalculateMidDates();
    //        for (i = 0; i < rows.Length; i++)
    //        {
    //            DataRow[] m_daterow = TSInfoTable.Select("TSIndex = " + rows[i]["TSIndex"].ToString());
    //            DataRow m_row = outTable.NewRow();
    //            m_row["Mid Date"] = m_daterow[0]["MidDate"];
    //            m_row["Start Date"] = m_daterow[0]["Date"];
    //            m_row["End Date"] = m_daterow[0]["EndDate"];
    //            for (j = 2; j < m_DataTable.Columns.Count; j++)
    //            {
    //                m_row[m_DataTable.Columns[j].ColumnName] = rows[i][m_DataTable.Columns[j].ColumnName];
    //            }
    //            outTable.Rows.Add(m_row);
    //        }
    //        DataTable[] returnTables = new DataTable[2];
    //        returnTables[0] = TSInfoTable;
    //        returnTables[1] = outTable;
    //        if (!string.IsNullOrEmpty(Noderow[0]["Name"].ToString()))
    //        {
    //            returnTables[1].TableName = Noderow[0]["Name"].ToString();
    //        }
    //        else
    //        {
    //            returnTables[1].TableName = Noderow[0]["Number"].ToString();
    //        }

    //        //Build the storage datatable ( end of the month values
    //        if (nType == NodeType.Reservoir)
    //        {
    //            DataTable stortable = new DataTable();
    //            m_DataTable = outDS.Tables["RES_STOROutput"];
    //            stortable.Columns.Add("Date", typeof(DateTime));
    //            //stortable.Columns.Add("Min_Capacity", Type.GetType("System.Int32"))
    //            stortable.Columns.Add("Storage", typeof(long));
    //            stortable.Columns.Add("Stor_Target", typeof(long));
    //            //stortable.Columns.Add("Max_Capacity", Type.GetType("System.Int32"))
    //            for (i = 0; i < stortable.Columns.Count; i++)
    //            {
    //                stortable.Columns[i].ReadOnly = true;
    //            }
    //            rows = m_DataTable.Select("NNo = " + nodeNo);
    //            for (i = 0; i < rows.Length; i++)
    //            {
    //                DataRow[] m_daterow = TSInfoTable.Select("TSIndex = " + rows[i]["TSIndex"].ToString());
    //                if (Convert.ToInt32(rows[i]["TSIndex"]) == 0)
    //                {
    //                    DataRow m_row = stortable.NewRow();
    //                    m_row["Date"] = m_daterow[0]["Date"];
    //                    m_row["Storage"] = rows[i]["Stor_Beg"];
    //                    m_row["Stor_Target"] = rows[i]["Stor_Beg"];
    //                    stortable.Rows.Add(m_row);
    //                }
    //                if (m_daterow.Length > 0)
    //                {
    //                    DataRow m_row = stortable.NewRow();
    //                    m_row["Date"] = Convert.ToDateTime(m_daterow[0]["EndDate"]);
    //                    //.AddSeconds(-1)
    //                    m_row["Storage"] = rows[i]["Stor_End"];
    //                    m_row["Stor_Target"] = rows[i]["Stor_Trg"];
    //                    stortable.Rows.Add(m_row);
    //                }
    //            }
    //            Array.Resize(ref returnTables, 3);
    //            returnTables[2] = stortable;
    //            if (!string.IsNullOrEmpty(Noderow[0]["Name"].ToString()))
    //            {
    //                returnTables[2].TableName = Noderow[0]["Name"].ToString() + "_STOR";
    //            }
    //            else
    //            {
    //                returnTables[2].TableName = Noderow[0]["Number"].ToString() + "_STOR";
    //            }
    //        }
    //        return returnTables;
    //    }
    //    private void CalculateMidDates()
    //    {
    //        DataTable TSInfoTable = outDS.Tables["TimeSteps"];
    //        DataRow m_row = null;
    //        foreach (DataRow m_row_loopVariable in TSInfoTable.Select())
    //        {
    //            m_row = m_row_loopVariable;
    //            System.TimeSpan diff1 = Convert.ToDateTime(m_row["EndDate"]).Subtract(Convert.ToDateTime(m_row["Date"]));
    //            m_row["MidDate"] = Convert.ToDateTime(m_row["Date"]).AddSeconds(diff1.TotalSeconds / 2);
    //            m_row["Duration"] = diff1.TotalSeconds;
    //        }
    //    }
    //    public DataRow[] LinksInfoSelect(string fromNodeName, string toNodeName)
    //    {
    //        int fromNodeNo = GetNodeNo(fromNodeName);
    //        int toNodeNo = GetNodeNo(toNodeName);
    //        if (toNodeNo > 0 & fromNodeNo > 0)
    //        {
    //            DataTable LinksInfoTable = outDS.Tables["LinksInfo"];
    //            DataRow[] linksrows = LinksInfoTable.Select("FromNode = " + fromNodeNo + " AND ToNode = " + toNodeNo);
    //            return linksrows;
    //        }
    //        else
    //        {
    //            return null;
    //        }
    //    }
    //    public DataRow NodeInfoSelect(string nodeName)
    //    {
    //        int nodeNo = GetNodeNo(nodeName);
    //        DataTable LinksInfoTable = outDS.Tables["NodesInfo"];
    //        DataRow[] noderows = LinksInfoTable.Select("NNo = " + nodeNo);
    //        return (noderows[0]);
    //    }
    //    public void AddCalibrationLinks(string nodeName, DataTable nodeDataTable)
    //    {
    //        List<string> linksKeyWords = new List<string> { };
    //        List<string> colKeyWords = new List<string> { };
    //        linksKeyWords.Add("_CALIB_SINK");
    //        colKeyWords.Add("CALIB_TO_SINK");
    //        linksKeyWords.Add("_CALIB_RES");
    //        colKeyWords.Add("CALIB_FROM_SOURCE");
    //        linksKeyWords.Add("_CALIB_DS_SUPPLY");
    //        colKeyWords.Add("CALIB_DS_SUPPLY");
    //        linksKeyWords.Add("_SIM_BYPASS");
    //        colKeyWords.Add("BYPASS");
    //        linksKeyWords.Add("ANN_RETURN_TO_");
    //        colKeyWords.Add("ANN_GW_RETURN");
    //        linksKeyWords.Add("ANN_DEPLET_FROM_");
    //        colKeyWords.Add("ANN_GW_DEPLETION");

    //        //Create columns for all existing keywords
    //        List<int> linkNos = new List<int> { };
    //        List<string> colHeads = new List<string> { };
    //        DataRow[] linksrows = null;
    //        int i = 0;
    //        for (i = 1; i <= linksKeyWords.Count; i++)
    //        {
    //            DataTable LinksInfoTable = outDS.Tables["LinksInfo"];
    //            linksrows = LinksInfoTable.Select("Name = '" + nodeName + linksKeyWords[i].ToString() + "' OR Name = '" + linksKeyWords[i].ToString() + nodeName + "'");
    //            if (linksrows.Length > 0)
    //            {
    //                nodeDataTable.Columns.Add(colKeyWords[i].ToString(), typeof(long));
    //                object[] m_linkData = new object[3];
    //                linkNos.Add((int)linksrows[0]["Number"]);
    //                colHeads.Add(colKeyWords[i]);
    //            }
    //        }
    //        //populate the table for all existing linkNos
    //        if (linkNos.Count > 0)
    //        {
    //            DataRow aRow = null;
    //            CalculateMidDates();
    //            DataTable LinksOutputTable = outDS.Tables["LinksOutput"];
    //            DataTable TSInfoTable = outDS.Tables["TimeSteps"];
    //            DataRow[] m_rows = nodeDataTable.Select();
    //            if (m_rows.Length > 0)
    //            {
    //                foreach (DataRow aRow_loopVariable in m_rows)
    //                {
    //                    aRow = aRow_loopVariable;
    //                    System.DateTime currentDate = Convert.ToDateTime(aRow["Mid Date"]);
    //                    DataRow[] tSRow = null;
    //                    tSRow = TSInfoTable.Select("MidDate = #" + currentDate + "#");
    //                    for (i = 1; i <= linkNos.Count; i++)
    //                    {
    //                        linksrows = LinksOutputTable.Select("Number = " + linkNos[i].ToString() + " AND TSIndex = " + tSRow[0]["TSIndex"].ToString());
    //                        aRow[colHeads[i].ToString()] = linksrows[0]["Flow"];
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                m_rows = LinksOutputTable.Select("Number = " + linkNos[1].ToString());
    //                int j = 0;
    //                for (j = 0; j <= m_rows.Length - 1; j++)
    //                {
    //                    DataRow[] m_daterow = TSInfoTable.Select("TSIndex = " + m_rows[j]["TSIndex"].ToString());
    //                    DataRow m_row = nodeDataTable.NewRow();
    //                    m_row["Mid Date"] = m_daterow[0]["MidDate"];
    //                    m_row["Start Date"] = m_daterow[0]["Date"];
    //                    m_row["End Date"] = m_daterow[0]["EndDate"];
    //                    for (i = 1; i <= linkNos.Count; i++)
    //                    {
    //                        linksrows = LinksOutputTable.Select("Number = " + linkNos[i].ToString() + " AND TSIndex = " + m_rows[j]["TSIndex"].ToString());
    //                        m_row[colHeads[i].ToString()] = linksrows[0]["Flow"];
    //                    }
    //                    nodeDataTable.Rows.Add(m_row);
    //                }
    //            }
    //        }
    //    }
    //    public DataTable GetOutputTable(OutputTableType m_OutputTableType)
    //    {
    //        DataTable m_LinkTable = new DataTable();
    //        switch (m_OutputTableType)
    //        {
    //            case OutputTableType.Links:
    //                m_LinkTable = outDS.Tables["LinksOutput"];
    //                break;
    //            case OutputTableType.Demands:
    //                m_LinkTable = outDS.Tables["DEMOutput"];
    //                break;
    //            case OutputTableType.ReservoirFlows:
    //                m_LinkTable = outDS.Tables["RESOutput"];
    //                break;
    //            case OutputTableType.ReservoirStorage:
    //                m_LinkTable = outDS.Tables["RES_STOROutput"];
    //                break;
    //            case OutputTableType.NonStorage:
    //                m_LinkTable = outDS.Tables["NON_STOROutput"];
    //                break;
    //        }
    //        return m_LinkTable;
    //    }
    //    private int GetNodeNo(string nodeName)
    //    {
    //        DataTable nodeInfoTable = outDS.Tables["NodesInfo"];
    //        DataRow[] rows = nodeInfoTable.Select("Name = '" + nodeName + "'");
    //        if (rows.Length == 1)
    //        {
    //            int nodeNo = Convert.ToInt32(rows[0]["NNo"]);
    //            return nodeNo;
    //        }
    //        else
    //        {
    //            MessageBox.Show("Node named " + nodeName + " not found in the output");
    //            return 0;
    //        }
    //    }
    //    private int GetLinkNo(string linkName)
    //    {
    //        DataTable LinkInfoTable = outDS.Tables["LinksInfo"];
    //        DataRow[] rows = LinkInfoTable.Select("Name = '" + linkName + "'");
    //        if (rows.Length == 1)
    //        {
    //            int LinkNo = Convert.ToInt32(rows[0]["number"]);
    //            return LinkNo;
    //        }
    //        else
    //        {
    //            MessageBox.Show("Link named" + linkName + " not found in the output");
    //            return -1;
    //        }
    //    }

    //    public bool LinkOutputExist(string linkName)
    //    {
    //        bool functionReturnValue = false;
    //        DataTable LinkInfoTable = outDS.Tables["LinksInfo"];
    //        DataRow[] rows = LinkInfoTable.Select("Name = '" + linkName + "'");
    //        if (rows.Length == 1)
    //        {
    //            functionReturnValue = true;
    //        }
    //        else
    //        {
    //            functionReturnValue = false;
    //        }
    //        return functionReturnValue;
    //    }
    //    public bool NodeOutputExist(string nodeName)
    //    {
    //        bool functionReturnValue = false;
    //        DataTable nodeInfoTable = outDS.Tables["NodesInfo"];
    //        DataRow[] rows = nodeInfoTable.Select("Name = '" + nodeName + "'");
    //        if (rows.Length == 1)
    //        {
    //            functionReturnValue = true;
    //        }
    //        else
    //        {
    //            functionReturnValue = false;
    //        }
    //        return functionReturnValue;
    //    }

    //}
}
