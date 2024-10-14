using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Csu.Modsim.ModsimModel;
using System.Text;
using System.ComponentModel;

namespace Csu.Modsim.NetworkUtils
{
    public class ModelOutputSupport
    {
        public static bool SuppressDeletingCSVFiles = false;

        public MODSIMOutputDS outDS;
        //Allows to add a user defined value to the link/node output table.
        //The event will be raised for each link/node, the user should use the provided link/node object to set
        // the value in the corresponding column in the provided row.
        public event AddCurrentUserLinkOutputEventHandler AddCurrentUserLinkOutput;
        public delegate void AddCurrentUserLinkOutputEventHandler(Link link, DataRow row);
        public event AddCurrentUserNonStorageOutputEventHandler AddCurrentUserNonStorageOutput;
        public delegate void AddCurrentUserNonStorageOutputEventHandler(Node node, DataRow row);
        public event AddCurrentUserDemandOutputEventHandler AddCurrentUserDemandOutput;
        public delegate void AddCurrentUserDemandOutputEventHandler(Node node, DataRow row);
        public event AddCurrentUserReservoirOutputEventHandler AddCurrentUserReservoirOutput;
        public delegate void AddCurrentUserReservoirOutputEventHandler(Node node, DataRow row);
        public event AddCurrentUserReservoir_STOROutputEventHandler AddCurrentUserReservoir_STOROutput;
        public delegate void AddCurrentUserReservoir_STOROutputEventHandler(Node node, DataRow row);
        private List<string[]> userDefinedOutVarsLinks;
        private List<string[]> userDefinedOutVarsNodes;
        StreamWriter m_LinksOutputCSV;
        StreamWriter m_LinksMeasuredCSV;
        StreamWriter m_DEMOutputCSV;
        StreamWriter m_RESOutputCSV;
        StreamWriter m_RES_STOROutputCSV;
        StreamWriter m_NON_STOROutputCSV;
        StreamWriter m_OutputTablesInfoCSV;
        StreamWriter m_HydroUnitOutputCSV;
        StreamWriter m_HydroTargetOutputCSV;
        public bool outputReady = false;
        private double uFactor;
        //public event System.ComponentModel.RunWorkerCompletedEventHandler RunWorkerCompleted;

        public enum LinkOutputType
        {
            Flow,
            Loss,
            NaturalFlow,
            StorLeft,
            Accrual,
            GroupStorLeft,
            GroupAccrual
        }
        public enum OutputTableType
        {
            Links,
            ReservoirFlows,
            ReservoirStorage,
            Demands,
            NonStorage
        }

        public ModelOutputSupport(Model mi, bool inMemoryOnly,bool outputInitialize = true)
        {
            // Set the culture and UI culture before
            // the call to InitializeComponent.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            if (outputInitialize)
            {
                if (inMemoryOnly)
                {
                    UpdateModelInfo(mi);
                    outputReady = true;
                }
                else
                {
                    outputReady = InitializeForOutput(mi);
                }
            }
            uFactor = Math.Pow(10, mi.accuracy);
        }

        public void UpdateModelInfo(Model mi)
        {
            outDS = new MODSIMOutputDS(mi);
            outDS.CleanColumns();
            AddUserDefinedVars();
            //add nodes
            for (int i = 0; i < mi.mInfo.outputNodeList.Length; i++)
            {
                Node n = mi.mInfo.outputNodeList[i];
                outDS.NodesInfo.AddNodesInfoRow(n.number, n.name, n.nodeType.ToString(),n.uid.ToString());
            }
            //add links
            for (int i = 0; i < mi.mInfo.outputLinkList.Length; i++)
            {
                Link l = mi.mInfo.outputLinkList[i];
                outDS.LinksInfo.AddLinksInfoRow(l.name, l.number, l.@from.number, l.to.number,l.uid.ToString());
            }
            //add time steps to the output
            for (int i = 0; i < mi.TimeStepManager.noModelTimeSteps; i++)
            {
                DateTime initialDate = mi.TimeStepManager.Index2Date(i, TypeIndexes.ModelIndex);
                DateTime endDate = mi.TimeStepManager.Index2EndDate(i, TypeIndexes.ModelIndex);
                TimeSpan diff = endDate.Subtract(initialDate);
                DateTime midDate = initialDate.AddMilliseconds(diff.TotalMilliseconds / 2);
                string altDate = initialDate.ToString(TimeManager.DateFormat_DB);
                outDS.TimeSteps.AddTimeStepsRow(i, initialDate, endDate, midDate, diff.TotalSeconds,altDate);
            }
            //Add Output Table info
            foreach (DataTable table in outDS.Tables)
            {
                foreach (DataColumn col in table.Columns)
                {
                    if (!ModelOutputMSDB.indices.Contains(col.ColumnName))
                    {
                        string[] defaults = FindDisplayOutName(col.ColumnName);
                        outDS.OutputTablesInfo.AddOutputTablesInfoRow(table.TableName, col.ColumnName, defaults[1], defaults[0]);
                    }
                }
            }
            if (mi.backRouting)
            {
                outDS.OutputTablesInfo.AddOutputTablesInfoRow("", "BackRoutingError", "Flow", "Back-Routing Error");
            }
            //Add hydro tables
            if (mi.hydro.IsActive)
            {
                // Add the hydropower unit table
                outDS.Tables.Add(mi.hydro.HydroUnitsTable);

                // Add the hydropower target table
                outDS.Tables.Add(mi.hydro.HydroTargetsTable);
            }
        }
        public void AddLinksOutput(Model mi)
        {
            DataTable linkOutputTable = outDS.LinksOutput;
            int tsIndex = mi.mInfo.CurrentModelTimeStepIndex;
            int monIndex = mi.TimeStepManager.GetMonthIndex(tsIndex, TypeIndexes.ModelIndex);

            for (int i = 0; i < mi.mInfo.outputLinkList.Length; i++)
            {
                Link l = mi.mInfo.outputLinkList[i];
                DataRow infoRow = linkOutputTable.NewRow();
                infoRow["TSIndex"] = tsIndex;
                infoRow["LNumber"] = l.number;
                if (OutputControlInfo.flo_flow)
                {
                    infoRow["Flow"] = l.mrlInfo.link_flow[monIndex] / mi.ScaleFactor;
                }
                if (OutputControlInfo.loss)
                {
                    infoRow["Loss"] = l.mrlInfo.link_closs[monIndex] / mi.ScaleFactor;
                }
                if (OutputControlInfo.natflow)
                {
                    infoRow["NaturalFlow"] = l.mrlInfo.natFlow[monIndex] / mi.ScaleFactor;
                }
                double linkMax = Convert.ToDouble(l.mlInfo.hi);
                if (l.m.loss_coef > 0 & l.m.loss_coef < 1)
                {
                    //Add channel loss to the link capacity (removed in gwater)
                    linkMax += l.mrlInfo.link_closs[monIndex];
                }
                if (OutputControlInfo.l_Max)
                {
                    infoRow["LMax"] = linkMax / mi.ScaleFactor;
                }
                if (OutputControlInfo.l_Min)
                {
                    infoRow["LMin"] = l.mlInfo.lo / mi.ScaleFactor;
                }
                if (OutputControlInfo.acc_output)
                {
                    if (mi.IsAccrualLink(l))
                    {
                        if (OutputControlInfo.stgl)
                        {
                            infoRow["StorLeft"] = l.mrlInfo.link_store[monIndex] / mi.ScaleFactor;
                        }
                        if (OutputControlInfo.acrl)
                        {
                            infoRow["Accrual"] = l.mrlInfo.link_accrual[monIndex] / mi.ScaleFactor;
                        }
                    }
                    else
                    {
                        if (OutputControlInfo.stgl)
                        {
                            infoRow["StorLeft"] = l.mrlInfo.link_store[monIndex] / mi.ScaleFactor;
                        }
                        if (OutputControlInfo.acrl)
                        {
                            infoRow["Accrual"] = l.mrlInfo.link_accrual[monIndex] / mi.ScaleFactor;
                        }
                    }

                    if (OutputControlInfo.stgl && l.m.groupNumber > 0 && (l.m.accrualLink != null))
                    {
                        LinkList m_AccGroupLList = l.m.accrualLink.mlInfo.cLinkL;
                        while (m_AccGroupLList != null)
                        {
                            Link curLink = m_AccGroupLList.link;
                            if (curLink.mrlInfo.groupID == l.m.groupNumber)
                            {
                                infoRow["GroupLink"] = curLink.name;
                                infoRow["GroupStorLeft"] = curLink.mrlInfo.link_store[monIndex] / mi.ScaleFactor;
                                infoRow["GroupAccrual"] = curLink.mrlInfo.link_accrual[monIndex] / mi.ScaleFactor;
                            }
                            m_AccGroupLList = m_AccGroupLList.next;
                        }
                    }
                }
                if (OutputControlInfo.hydroState)
                {
                    infoRow["Hydro_State"] = l.mrlInfo.hydStateIndex;
                }
                if (AddCurrentUserLinkOutput != null)
                {
                    AddCurrentUserLinkOutput(l, infoRow);
                }
                linkOutputTable.Rows.Add(infoRow);
            }
        }

        public void AddLinksMeasured(Model mi)
        {
            DataTable linkMeasuredTable = outDS.LinksMeasured;
            int tsIndex = mi.mInfo.CurrentModelTimeStepIndex;
            //int monIndex = mi.TimeStepManager.GetMonthIndex(tsIndex, TypeIndexes.ModelIndex);
            DateTime date = mi.TimeStepManager.Index2Date(tsIndex, TypeIndexes.ModelIndex);

            for (int i = 0; i < mi.mInfo.outputLinkList.Length; i++)
            {
                Link l = mi.mInfo.outputLinkList[i];
                if (l.m.adaMeasured.getSize() > 0)
                {
                    DataRow infoRow = linkMeasuredTable.NewRow();
                    infoRow["TSIndex"] = tsIndex;
                    infoRow["LNumber"] = l.number;
                    //Assumes there is no hs on measured timeseries
                    infoRow["Meas_Flow"] = l.m.adaMeasured.getDataL(l.m.adaMeasured.GetTsIndex(date), 0) / mi.ScaleFactor;
                    linkMeasuredTable.Rows.Add(infoRow);
                }
            }
        }

        public void AddNodesOutput(Model mi)
        {
            for (int i = 0; i < mi.mInfo.outputNodeList.Length; i++)
            {
                Node node = mi.mInfo.outputNodeList[i];
                if (node.nodeType == NodeType.Demand || node.nodeType == NodeType.Sink)
                {
                    if (OutputControlInfo.dem_output)
                    {
                        AddDemandOutput(mi, node);
                    }
                }
                if (node.nodeType == NodeType.NonStorage)
                {
                    if (OutputControlInfo.nonStorage_output)
                    {
                        AddNonStorageOutput(mi, node);
                    }
                }
                if (node.nodeType == NodeType.Reservoir)
                {
                    if (OutputControlInfo.res_output)
                    {
                        AddReservoirOutput(mi, node);
                    }
                }
            }
        }
        private void AddDemandOutput(Model mi, Node node)
        {
            DataTable NodeOutTable = outDS.DEMOutput;
            int tsIndex = mi.mInfo.CurrentModelTimeStepIndex;
            int monIndex = mi.TimeStepManager.GetMonthIndex(tsIndex, TypeIndexes.ModelIndex);
            DataRow NodeOutRow = NodeOutTable.NewRow();
            NodeOutRow["TSIndex"] = tsIndex;
            NodeOutRow["NNo"] = node.number;
            if (OutputControlInfo.demand)
            {
                NodeOutRow["Demand"] = node.mnInfo.demand[tsIndex] / mi.ScaleFactor;
            }
            //Calculate total avaliable surface water.
            long iswat = node.mnInfo.upstrm_release[monIndex] + node.mnInfo.canal_in[monIndex] + node.mnInfo.irtnflowthruNF_OUT[monIndex] + node.mnInfo.unreg_inflow[monIndex];
            long t = Math.Max(0, node.mnInfo.demand[tsIndex] - node.mnInfo.demand_shortage[monIndex]);
            iswat = Math.Max(0, Math.Min(iswat, t));
            if (OutputControlInfo.surf_in)
            {
                NodeOutRow["Surf_In"] = iswat / mi.ScaleFactor;
            }

            ///* Estimate groundwater. */
            long igwat = Math.Max(0, t - iswat);
            if (OutputControlInfo.gw_in)
            {
                NodeOutRow["Gw_In"] = Math.Min(igwat, node.mnInfo.gw_to_node[monIndex]) / mi.ScaleFactor;
            }
            if (OutputControlInfo.dem_sht)
            {
                NodeOutRow["Shortage"] = node.mnInfo.ishtm[tsIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.hydroState)
            {
                NodeOutRow["Hydro_State"] = node.mnInfo.hydStateIndex;
            }
            if (AddCurrentUserDemandOutput != null)
            {
                AddCurrentUserDemandOutput(node, NodeOutRow);
            }
            NodeOutTable.Rows.Add(NodeOutRow);
        }
        private void AddNonStorageOutput(Model mi, Node node)
        {
            DataTable NodeOutTable = outDS.NON_STOROutput;
            int tsIndex = mi.mInfo.CurrentModelTimeStepIndex;
            int monIndex = mi.TimeStepManager.GetMonthIndex(tsIndex, TypeIndexes.ModelIndex);
            bool localInflow = node.mnInfo.inflow.Length > 0;
            DataRow NodeOutRow = NodeOutTable.NewRow();
            NodeOutRow["TSIndex"] = tsIndex;
            NodeOutRow["NNo"] = node.number;
            if (OutputControlInfo.Inflow)
            {
                if (localInflow)
                {
                    NodeOutRow["Inflow"] = node.mnInfo.inflow[tsIndex, 0] / mi.ScaleFactor;
                }
                else
                {
                    NodeOutRow["Inflow"] = 0;
                }
            }
            if (OutputControlInfo.gw_output)
            {
                NodeOutRow["Gw_In"] = node.mnInfo.gw_to_node[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.Flow_Thru)
            {
                NodeOutRow["Flow_Thru"] = (node.mnInfo.irtnflowthruNF + node.mnInfo.irtnflowthruSTG) / mi.ScaleFactor;
            }
            if (OutputControlInfo.Rout_Ret)
            {
                NodeOutRow["Rout_Ret"] = node.mnInfo.iroutreturn / mi.ScaleFactor;
            }
            if (AddCurrentUserNonStorageOutput != null)
            {
                AddCurrentUserNonStorageOutput(node, NodeOutRow);
            }
            NodeOutTable.Rows.Add(NodeOutRow);
        }
        private void AddReservoirOutput(Model mi, Node node)
        {
            DataTable NodeOutTable = outDS.RESOutput;
            DataTable NodeOutTable_STOR = outDS.Tables[outDS.RES_STOROutput.TableName];
            int tsIndex = mi.mInfo.CurrentModelTimeStepIndex;
            int monIndex = mi.TimeStepManager.GetMonthIndex(tsIndex, TypeIndexes.ModelIndex);
            DataRow NodeOutRow = NodeOutTable.NewRow();
            DataRow NodeOutRow_STOR = NodeOutTable_STOR.NewRow();
            NodeOutRow["TSIndex"] = tsIndex;
            NodeOutRow["NNo"] = node.number;
            NodeOutRow_STOR["TSIndex"] = tsIndex;
            NodeOutRow_STOR["NNo"] = node.number;
            if (OutputControlInfo.stor_beg)
            {
                NodeOutRow_STOR["Stor_Beg"] = node.mnInfo.start_storage[mi.mInfo.CurrentModelTimeStepIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.stor_end)
            {
                NodeOutRow_STOR["Stor_End"] = node.mnInfo.end_storage[mi.mInfo.CurrentModelTimeStepIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.stor_trg)
            {
                NodeOutRow_STOR["Stor_Trg"] = node.mnInfo.trg_storage[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.elev_end)
            {
                NodeOutRow_STOR["Elev_End"] = node.mnInfo.endElevation;
            }
            if (OutputControlInfo.spills)
            {
                NodeOutRow["Spills"] = node.mnInfo.res_spill[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.evp_loss)
            {
                NodeOutRow["Evap_Loss"] = node.mnInfo.reservoir_evaporation[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.seepage)
            {
                NodeOutRow["Seepage"] = node.mnInfo.iseepr[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.ups_rel)
            {
                NodeOutRow["Ups_Rel"] = node.mnInfo.upstrm_release[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.pump_in)
            {
                NodeOutRow["Pump_In"] = node.mnInfo.canal_in[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.gwater)
            {
                NodeOutRow["Gw_In"] = node.mnInfo.gw_to_node[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.dws_rel)
            {
                NodeOutRow["Dws_Rel"] = node.mnInfo.downstrm_release[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.pump_out)
            {
                NodeOutRow["Pump_Out"] = node.mnInfo.canal_out[monIndex] / mi.ScaleFactor;
            }
            if (OutputControlInfo.hydra_Cap)
            {
                NodeOutRow["Hydra_Cap"] = node.mnInfo.hydCap / mi.ScaleFactor;
            }
            if (OutputControlInfo.hydroState)
            {
                NodeOutRow["Hydro_State"] = node.mnInfo.hydStateIndex;
            }
            double rhead = node.mnInfo.avg_head[monIndex];
            if (rhead < 0)
            {
                rhead = 0;
            }
            if (OutputControlInfo.head_avg)
            {
                NodeOutRow["Head_Avg"] = rhead;
            }
            if (OutputControlInfo.powr_avg)
            {
                NodeOutRow["Powr_Avg"] = node.mnInfo.avg_hydropower[monIndex];
            }
            double ghrs = 0;
            if (node.mnInfo.generatinghours.Length > 0)
            {
                ghrs = node.mnInfo.generatinghours[tsIndex, 0];
            }
            double ieng = node.mnInfo.avg_hydropower[monIndex] * ghrs / 1000.0;
            if (OutputControlInfo.powr_pk)
            {
                NodeOutRow["Energy"] = ieng;
            }
            long i2nd = 0;
            if (node.m.peakGeneration)
            {
                i2nd = 0;
            }
            else
            {
                DateTime startDate = mi.mInfo.CurrentBegOfPeriodDate;
                DateTime endDate = mi.mInfo.CurrentEndOfPeriodDate;
                double thr = endDate.Subtract(startDate).TotalHours;
                ghrs = thr - ghrs;
                if (ghrs < 0.0)
                {
                    ghrs = 0.0;
                }
                i2nd = Convert.ToInt64(node.mnInfo.avg_hydropower[monIndex] * ghrs / 1000.0);
            }
            if (OutputControlInfo.pwr_2nd)
            {
                NodeOutRow["Pwr_2nd"] = i2nd;
            }
            if (AddCurrentUserReservoirOutput != null)
            {
                AddCurrentUserReservoirOutput(node, NodeOutRow);
            }
            NodeOutTable.Rows.Add(NodeOutRow);
            if (AddCurrentUserReservoir_STOROutput != null)
            {
                AddCurrentUserReservoir_STOROutput(node, NodeOutRow_STOR);
            }
            NodeOutTable_STOR.Rows.Add(NodeOutRow_STOR);
        }
        public void AddHydropowerOutput(Model mi)
        {
            // Add hydropower unit output
            MODSIMOutputDS.HydroUnitOutputDataTable hydroUnitDT = this.outDS.HydroUnitOutput;
            foreach (HydropowerUnit hydroUnit in mi.hydro.HydroUnits)
            {
                double flow = mi.FlowUnits.ConvertFrom(hydroUnit.Flow, HydropowerUnit.DefaultFlowUnits, mi.mInfo.CurrentBegOfPeriodDate);
                hydroUnitDT.AddHydroUnitOutputRow(hydroUnit.ID, mi.mInfo.CurrentModelTimeStepIndex, flow, hydroUnit.Head, hydroUnit.Efficiency, hydroUnit.Power, hydroUnit.Energy, hydroUnit.DowntimeFactor, hydroUnit.GeneratingHours);
            }

            // Add hydropower target output
            MODSIMOutputDS.HydroTargetOutputDataTable hydroTargetDT = this.outDS.HydroTargetOutput;
            foreach (HydropowerTarget hydroTarget in mi.hydro.HydroTargets)
            {
                hydroTargetDT.AddHydroTargetOutputRow(hydroTarget.ID, mi.mInfo.CurrentModelTimeStepIndex, hydroTarget.EnergyTarget, hydroTarget.Energy, hydroTarget.EnergyDiff);
            }
        }
        public void FlushOutputToCSV(Model mi, bool finalizeOutput)
        {
            string modelFileName = mi.fname;
            string networkName = BaseNameString(modelFileName);

            foreach (DataTable table in outDS.Tables)
            {
                string outFileName = networkName + table.TableName + ".CSV";
                StreamWriter currentStream = null;

                if (table.TableName == outDS.LinksOutput.TableName)
                {
                    currentStream = m_LinksOutputCSV;
                }
                else if (table.TableName == outDS.LinksMeasured.TableName)
                {
                    currentStream = m_LinksMeasuredCSV;
                }
                else if (table.TableName == outDS.DEMOutput.TableName)
                {
                    currentStream = m_DEMOutputCSV;
                }
                else if (table.TableName == outDS.RESOutput.TableName)
                {
                    currentStream = m_RESOutputCSV;
                }
                else if (table.TableName == outDS.RES_STOROutput.TableName)
                {
                    currentStream = m_RES_STOROutputCSV;
                }
                else if (table.TableName == outDS.NON_STOROutput.TableName)
                {
                    currentStream = m_NON_STOROutputCSV;
                }
                else if (table.TableName == outDS.OutputTablesInfo.TableName)
                {
                    currentStream = m_OutputTablesInfoCSV;
                }
                else if (table.TableName == outDS.HydroUnitOutput.TableName)
                {
                    currentStream = m_HydroUnitOutputCSV;
                }
                else if (table.TableName == outDS.HydroTargetOutput.TableName)
                {
                    currentStream = m_HydroTargetOutputCSV;
                }

                if (currentStream != null)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        string outLine = "";
                        bool initial = true;
                        for (int col = 0; col < table.Columns.Count; col++)
                        {
                            if (!initial)
                            {
                                outLine += ",";
                            }
                            if (object.ReferenceEquals(table.Columns[col].DataType, typeof(string)))
                            {
                                if (row[col] != DBNull.Value)
                                {
                                    outLine += "\"" + row[col].ToString() + "\"";
                                }
                                else
                                {
                                    outLine += "\"\"";
                                }
                            }
                            else
                            {
                                if (row[col] != DBNull.Value)
                                {
                                    outLine += string.Format("{0:G17}", row[col]);
                                }
                                else
                                {
                                    outLine += "0";
                                }
                            }
                            initial = false;
                        }
                        currentStream.WriteLine(outLine);
                    }
                }
            }
            outDS = new MODSIMOutputDS(mi);
            outDS.CleanColumns();
            AddUserDefinedVars();
            if (finalizeOutput)
            {
                FinilizeOutput(mi);
                ModelOutputMSDB m_MSDBOutput = new ModelOutputMSDB(mi);
                if (OutputControlInfo.ver8MSDBOutputFiles)
                {
                    m_MSDBOutput.InitializeOutput();
                    //Deletes the csv if user selected and not problems in the mdb file creation.
                    if (OutputControlInfo.DeleteTempVer8OutputFiles && m_MSDBOutput.statusReady)
                    {
                        DeleteCSVFiles(modelFileName);
                    }
                }
                else
                {
                    m_MSDBOutput.DisableMSDBOutput();
                }
            }
        }

        private SQLiteHelper m_DB;
        //public void FlushOutputToSQLiteOLD(Model mi, bool commit = false)
        //{
        //    string modelFileName = mi.fname;
        //    string networkName = BaseNameString(modelFileName);

        //    string outFileName =  networkName + "OUTPUT.SQLite";
        //    if(m_DB == null) m_DB = new SQLiteHelper(outFileName);

        //    if (!outputReady) m_DB.PrepareSQLiteOutputFile(outDS);

        //    //string outLine = "";
        //    StringBuilder outLine = new StringBuilder();
        //    foreach (DataTable table in outDS.Tables)
        //    {
                
        //        String currentTable = table.TableName;
        //        if (table.Rows.Count > 0 ) outLine.Append(" INSERT INTO " + currentTable + " VALUES ");
        //        bool firstValue = true;
        //        foreach (DataRow row in table.Rows)
        //        {
        //            if (!firstValue) outLine.Append(",");
        //            outLine.Append(" (");
        //            bool initial = true;
        //            for (int col = 0; col < table.Columns.Count; col++)
        //            {
        //                if (!initial)
        //                {
        //                    outLine.Append(",");
        //                }
        //                if (object.ReferenceEquals(table.Columns[col].DataType, typeof(string)))
        //                {
        //                    if (row[col] != DBNull.Value)
        //                    {
        //                        outLine.Append("\"" + row[col].ToString() + "\"");
        //                    }
        //                    else
        //                    {
        //                        outLine.Append("\"\"");
        //                    }
        //                }
        //                else if (object.ReferenceEquals(table.Columns[col].DataType, typeof(DateTime)))
        //                {
        //                    if (row[col] != DBNull.Value)
        //                    {
        //                        DateTime m_date = DateTime.Parse(row[col].ToString());
        //                        outLine.Append("'" + m_date.ToString("yyyy-MM-dd HH:mm:ss") + "'");
        //                    }
        //                    else
        //                    {
        //                        outLine.Append("null");
        //                    }
        //                }
        //                else
        //                {
        //                    if (row[col] != DBNull.Value)
        //                    {
        //                        outLine.Append(string.Format("{0:G17}", row[col]));
        //                    }
        //                    else
        //                    {
        //                        outLine.Append("0");
        //                    }
        //                }
        //                initial = false;
        //            }
        //            outLine.Append(") ");
        //            firstValue = false;
        //        }
        //        if (table.Rows.Count > 0) outLine.Append("; ");
                
        //    }
        //    // Execute the command to import data into the database.
        //    //using (BackgroundWorker _bgworker = new BackgroundWorker())
        //    //    {
        //    //        _bgworker.DoWork += m_DB.Bgworker_ExecuteQuery;
        //    //        _bgworker.WorkerReportsProgress = false;
        //    //        _bgworker.RunWorkerAsync(outLine.ToString());
        //    //    }
        //    m_DB.ExecuteQuery(outLine.ToString());
        //    if(commit) m_DB.CommitTransaction();

        //    //Clear the output tables.
        //    outDS = new MODSIMOutputDS(mi);
        //    outDS.CleanColumns();
        //    AddUserDefinedVars();
        //    //if (finalizeOutput)
        //    //{
        //    //    //FinilizeOutput(mi);
        //    //    ModelOutputMSDB m_MSDBOutput = new ModelOutputMSDB(mi);
        //    //    if (OutputControlInfo.ver8MSDBOutputFiles)
        //    //    {
        //    //        m_MSDBOutput.InitializeOutput();
        //    //        //Deletes the csv if user selected and not problems in the mdb file creation.
        //    //        if (OutputControlInfo.DeleteTempVer8OutputFiles && m_MSDBOutput.statusReady)
        //    //        {
        //    //            DeleteCSVFiles(modelFileName);
        //    //        }
        //    //    }
        //    //    else
        //    //    {
        //    //        m_MSDBOutput.DisableMSDBOutput();
        //    //    }
        //    //}
        //}

        public bool FlushOutputToSQLite(Model mi, bool commit = false)
        {
            if (!outputReady || m_DB == null)
                if (!InitializeForOutput(mi))
                {
                    mi.FireOnError("Failed to initialize output database");
                    return false;
                }

            string modelFileName = mi.fname;
            string networkName = BaseNameString(modelFileName);

            //string outFileName = networkName + "OUTPUT.SQLite";
            //if (m_DB == null)
            //{
            //    m_DB = new SQLiteHelper(outFileName, true);
            //    m_DB.FireErrorMessage += DatabaseMessagePumping;
            //}
            bool enableMTheading = !commit;
            if ((!m_DB.workingOnUpdates || commit))
            {
                mi.FireOnMessage("writing SQLite output");

                //if (!outputReady) m_DB.PrepareSQLiteOutputFile(outDS);
                //string outLine = "";
                // Execute the command to import data into the database.
                Model.FireOnMessageGlobal("  writting " + outDS.LinksOutput.Rows.Count + " rows.");
                if (enableMTheading) //Multithread
                {
                    using (BackgroundWorker _bgworker = new BackgroundWorker())
                    {
                        _bgworker.DoWork += m_DB.Bgworker_UpdateTables;
                        _bgworker.WorkerReportsProgress = false;
                        List<object> arguments = new List<object>();
                        arguments.Add(outDS.Copy());
                        arguments.Add(commit); //this seems to be always false - it could be removed in the future.
                        m_DB.workingOnUpdates = true; //set in advance of UpdateTables for very small networks.
                        _bgworker.RunWorkerAsync(arguments);
                    }
                }
                else
                {
                    //The last output processing (when commit = true) is not done in background threads to avoid issues with the output class initialization.
                    while (m_DB.workingOnUpdates) Thread.Sleep(100);
                    m_DB.UpdateTables(outDS);
                    m_DB.CommitTransaction(true);
                    m_DB = null;
                }

                //Clear the output tables.
                outDS = new MODSIMOutputDS(mi);
                outDS.CleanColumns();
                AddUserDefinedVars();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DatabaseMessagePumping(string message)
        {
            Model.FireOnMessageGlobal(message);
        }

        private void AddUserDefinedVars()
        {
            if (userDefinedOutVarsLinks != null)
            {
                foreach (string[] vars in userDefinedOutVarsLinks)
                {
                    AddUserOutVarToTables(vars[0], true, false);
                }
            }
            if (userDefinedOutVarsNodes != null)
            {
                foreach (string[] vars in userDefinedOutVarsNodes)
                {
                    AddUserOutVarToTables(vars[0], false, true);
                }
            }
        }
        public static string BaseNameString(string mName)
        {
            string m_Fpath = Path.GetFullPath(mName);
            string m_path = Path.GetDirectoryName(m_Fpath);
            string m_fileName = Path.GetFileNameWithoutExtension(mName);
            return Path.Combine(m_path, m_fileName);// m_path + "\\" + m_fileName;
        }
        private void OpenOutputCVSFiles(string fName)
        {
            string networkName = BaseNameString(fName);
            foreach (DataTable table in outDS.Tables)
            {
                string outFileName = networkName + table.TableName + ".CSV";
                StreamWriter currentStream = null;

                if (table.TableName == outDS.LinksOutput.TableName)
                {
                    currentStream = CreateNewCSV(ref m_LinksOutputCSV, outFileName);
                }
                else if (table.TableName == outDS.LinksMeasured.TableName)
                {
                    currentStream = CreateNewCSV(ref m_LinksMeasuredCSV, outFileName);
                }
                else if (table.TableName == outDS.DEMOutput.TableName)
                {
                    currentStream = CreateNewCSV(ref m_DEMOutputCSV, outFileName);
                }
                else if (table.TableName == outDS.RESOutput.TableName)
                {
                    currentStream = CreateNewCSV(ref m_RESOutputCSV, outFileName);
                }
                else if (table.TableName == outDS.RES_STOROutput.TableName)
                {
                    currentStream = CreateNewCSV(ref m_RES_STOROutputCSV, outFileName);
                }
                else if (table.TableName == outDS.NON_STOROutput.TableName)
                {
                    currentStream = CreateNewCSV(ref m_NON_STOROutputCSV, outFileName);
                }
                else if (table.TableName == outDS.OutputTablesInfo.TableName)
                {
                    currentStream = CreateNewCSV(ref m_OutputTablesInfoCSV, outFileName);
                }
                else if (table.TableName == outDS.HydroUnitOutput.TableName)
                {
                    currentStream = CreateNewCSV(ref m_HydroUnitOutputCSV, outFileName);
                }
                else if (table.TableName == outDS.HydroTargetOutput.TableName)
                {
                    currentStream = CreateNewCSV(ref m_HydroTargetOutputCSV, outFileName);
                }

                if (currentStream != null)
                {
                    string outLine = "";
                    bool initial = true;
                    foreach (DataColumn col in table.Columns)
                    {
                        if (!initial)
                        {
                            outLine += ",";
                        }
                        outLine += col.ColumnName;
                        initial = false;
                    }
                    currentStream.WriteLine(outLine);
                }
            }
        }

        private StreamWriter CreateNewCSV(ref StreamWriter currStream, string filename)
        {
            if (File.Exists(filename))
            {
                if (currStream != null)
                {
                    currStream.Close();
                }
                try
                {
                    File.Delete(filename);
                }
                catch (Exception ex)
                {
                    Model.FireOnErrorGlobal("Unable to obtain file lock for: " + filename + Environment.NewLine + "    " + ex.Message);
                    return null;
                }
            }
            currStream = new StreamWriter(filename, true);
            return currStream;
        }
        private void DeleteCSVFiles(string fName)
        {
            if (SuppressDeletingCSVFiles)
            {
                return;
            }
            string networkName = BaseNameString(fName);
            foreach (DataTable table in outDS.Tables)
            {
                string outFileName = networkName + table.TableName + ".CSV";
                if (File.Exists(outFileName))
                {
                    if (table.TableName == outDS.LinksOutput.TableName && m_LinksOutputCSV != null)
                    {
                        m_LinksOutputCSV.Close();
                    }
                    else if (table.TableName == outDS.LinksMeasured.TableName && m_LinksMeasuredCSV != null)
                    {
                        m_LinksMeasuredCSV.Close();
                    }
                    else if (table.TableName == outDS.DEMOutput.TableName && m_DEMOutputCSV != null)
                    {
                        m_DEMOutputCSV.Close();
                    }
                    else if (table.TableName == outDS.RESOutput.TableName && m_RESOutputCSV != null)
                    {
                        m_RESOutputCSV.Close();
                    }
                    else if (table.TableName == outDS.RES_STOROutput.TableName && m_RES_STOROutputCSV != null)
                    {
                        m_RES_STOROutputCSV.Close();
                    }
                    else if (table.TableName == outDS.NON_STOROutput.TableName && m_NON_STOROutputCSV != null)
                    {
                        m_NON_STOROutputCSV.Close();
                    }
                    else if (table.TableName == outDS.OutputTablesInfo.TableName && m_OutputTablesInfoCSV != null)
                    {
                        m_OutputTablesInfoCSV.Close();
                    }
                    else if (table.TableName == outDS.HydroUnitOutput.TableName && m_HydroUnitOutputCSV != null)
                    {
                        m_HydroUnitOutputCSV.Close();
                    }
                    else if (table.TableName == outDS.HydroTargetOutput.TableName && m_HydroTargetOutputCSV != null)
                    {
                        m_HydroTargetOutputCSV.Close();
                    }

                    try
                    {
                        File.Delete(outFileName);
                    }
                    catch (Exception ex)
                    {
                        Model.FireOnErrorGlobal("Unable to obtain file lock for: " + outFileName + Environment.NewLine + "    " + ex.Message);
                        continue;
                    }
                }
            }
        }
        public bool InitializeForOutput(Model mi)
        {
            //outDS = new MODSIMOutputDS(mi);
            //outDS.CleanColumns();
            //AddUserDefinedVars();
            UpdateModelInfo(mi);
            string networkName = BaseNameString(mi.fname);

//retryDeletingDB:
//            //If a previous output file exist it will be deleted. 
            string outFileName ;//= networkName + "OUTPUT.SQLite";
//            try
//            {
//                if (File.Exists(outFileName)) File.Delete(outFileName);
//            }
//            catch(Exception ex)
//            {
//                if (OutputControlInfo.SQLiteOutputFiles && ex.Message.Contains("it is being used by another process"))
//                {
//                    if (MessageBox.Show("Output SQLite Database is locked.  Please close the file before continuing. \n Do you want to retry?", "Output file locked", MessageBoxButtons.YesNo) == DialogResult.Yes)
//                        goto retryDeletingDB;
//                    Model.FireOnErrorGlobal("Ouptut file ERROR: Simulation aborted.");
//                    return false;
//                }
//            }

            outFileName = networkName + "OUTPUT.mdb";
            if (File.Exists(outFileName)) File.Delete(outFileName);

            if (OutputControlInfo.ver8MSDBOutputFiles)
            {
                foreach (DataTable table in outDS.Tables)
                {
                    outFileName = networkName + table.TableName + ".CSV";
                    StreamWriter currentStream = null;
                    if (!SuppressDeletingCSVFiles && File.Exists(outFileName))
                    {
                        try
                        {
                            File.Delete(outFileName);
                        }
                        catch (Exception ex)
                        {
                            Model.FireOnErrorGlobal("Unable to obtain file lock for: " + outFileName + Environment.NewLine + "    " + ex.Message);
                            return false;
                        }
                    }


                    if (table.TableName == outDS.LinksInfo.TableName ||
                            table.TableName == outDS.NodesInfo.TableName ||
                            table.TableName == outDS.TimeSteps.TableName ||
                            table.TableName == mi.hydro.HydroUnitsTable.TableName ||
                            table.TableName == mi.hydro.HydroTargetsTable.TableName)
                    {
                        currentStream = new StreamWriter(outFileName, true);
                        string outLine = "";
                        bool initial = true;
                        foreach (DataColumn col in table.Columns)
                        {
                            if (!initial)
                            {
                                outLine += ",";
                            }
                            outLine += col.ColumnName;
                            initial = false;
                        }
                        currentStream.WriteLine(outLine);
                    }
                    if (currentStream != null)
                    {
                        foreach (DataRow row in table.Rows)
                        {
                            string m_outLine = "";
                            bool initial = true;
                            int m_col = 0;
                            for (m_col = 0; m_col < table.Columns.Count; m_col++)
                            {
                                if (!initial)
                                {
                                    m_outLine += ",";
                                }
                                if (row[m_col] != DBNull.Value)
                                {
                                    if (object.ReferenceEquals(table.Columns[m_col].DataType, typeof(DateTime)))
                                    {
                                        m_outLine += Convert.ToDateTime(row[m_col]).ToString(TimeManager.DateFormat);
                                    }
                                    else
                                    {
                                        m_outLine += row[m_col].ToString();
                                    }
                                }
                                else
                                {
                                    if (object.ReferenceEquals(table.Columns[m_col].DataType, typeof(string)))
                                    {
                                        m_outLine += "''";
                                    }
                                }
                                initial = false;
                            }
                            currentStream.WriteLine(m_outLine);
                        }
                        currentStream.Close();
                    }
                }
                //Delete existing MDB output file
                ModelOutputMSDB m_MSDBOutput = new ModelOutputMSDB(mi);
                m_MSDBOutput.DisableMSDBOutput();
                OpenOutputCVSFiles(mi.fname);
            }
            else {
                outFileName = networkName + "OUTPUT.sqlite";
                try
                {
                    if (m_DB == null)
                    {
                        m_DB = new SQLiteHelper(outFileName, true);
                        m_DB.FireErrorMessage += DatabaseMessagePumping;
                    }
                    m_DB.PrepareSQLiteOutputFile(outDS);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            outputReady = true;
            return true;
        }
        public void FinilizeOutput(Model mi)
        {
            if (outputReady)
            {
                foreach (DataTable table in outDS.Tables)
                {
                    if (table.TableName == outDS.LinksOutput.TableName && m_LinksOutputCSV != null)
                    {
                        m_LinksOutputCSV.Close();
                    }
                    else if (table.TableName == outDS.LinksMeasured.TableName && m_LinksMeasuredCSV != null)
                    {
                        m_LinksMeasuredCSV.Close();
                    }
                    else if (table.TableName == outDS.DEMOutput.TableName && m_DEMOutputCSV != null)
                    {
                        m_DEMOutputCSV.Close();
                    }
                    else if (table.TableName == outDS.RESOutput.TableName && m_RESOutputCSV != null)
                    {
                        m_RESOutputCSV.Close();
                    }
                    else if (table.TableName == outDS.RES_STOROutput.TableName && m_RES_STOROutputCSV != null)
                    {
                        m_RES_STOROutputCSV.Close();
                    }
                    else if (table.TableName == outDS.NON_STOROutput.TableName && m_NON_STOROutputCSV != null)
                    {
                        m_NON_STOROutputCSV.Close();
                    }
                    else if (table.TableName == outDS.OutputTablesInfo.TableName && m_OutputTablesInfoCSV != null)
                    {
                        m_OutputTablesInfoCSV.Close();
                    }
                    else if (table.TableName == outDS.HydroUnitOutput.TableName && m_HydroUnitOutputCSV != null)
                    {
                        m_HydroUnitOutputCSV.Close();
                    }
                    else if (table.TableName == outDS.HydroTargetOutput.TableName && m_HydroTargetOutputCSV != null)
                    {
                        m_HydroTargetOutputCSV.Close();
                    }
                }
            }
        }
        public long LinkOutputQuery(LinkOutputType OutputType, string linkName, int timeStepIndex)
        {
            int linkNo = GetLinkNo(linkName);
            DataRow[] rows = outDS.LinksOutput.Select("LNumber = " + linkNo + " AND TSIndex = " + timeStepIndex);
            if (rows.Length != 1)
            {
                return -1;
            }
            return Convert.ToInt64(rows[0][OutputType.ToString()]);
        }
        public long LinkOutputQuery(LinkOutputType OutputType, int linkNo, int timeStepIndex)
        {
            DataRow[] rows = outDS.LinksOutput.Select("LNumber = " + linkNo + " AND TSIndex = " + timeStepIndex);
            if (rows.Length != 1)
            {
                return -1;
            }
            return Convert.ToInt64(rows[0][OutputType.ToString()]);
        }
        private int GetLinkNo(string linkName)
        {
            DataTable LinkInfoTable = outDS.Tables[outDS.LinksInfo.TableName];
            DataRow[] rows = LinkInfoTable.Select("Name = '" + linkName + "'");
            if (rows.Length == 1)
            {
                int LinkNo = Convert.ToInt32(rows[0]["LNumber"]);
                return LinkNo;
            }
            else
            {
                MessageBox.Show("Link named" + linkName + " not found in the output");
                return -1;
            }
        }
        public void AddUserDefinedOutputVariable(Model mi, string VarName, bool linkOutputVar, bool nodeOutputVar, string varType)
        {
            //DataTable OutInfoTable = outDS.Tables[outDS.OutputTablesInfo.TableName];
            mi.FireOnMessage("  Adding user defined output variable:" + VarName);
            string[] varInfo =
            {
            VarName,
            varType
            };
            if (linkOutputVar)
            {
                if (userDefinedOutVarsLinks == null)
                {
                    userDefinedOutVarsLinks = new List<string[]> { };
                }
                userDefinedOutVarsLinks.Add(varInfo);
            }
            if (nodeOutputVar)
            {
                if (userDefinedOutVarsNodes == null)
                {
                    userDefinedOutVarsNodes = new List<string[]> { };
                }
                userDefinedOutVarsNodes.Add(varInfo);
            }
            //AddUserOutVarToTables(VarName, linkOutputVar, nodeOutputVar);
            FinilizeOutput(mi);
            //InitializeForOutput(mi);
        }
        private void AddUserOutVarToTables(string VarName, bool linkOutputVar, bool nodeOutputVar)
        {
            foreach (DataTable m_table_loopVariable in outDS.Tables)
            {
                DataTable m_table = m_table_loopVariable;
                bool addColumn = false;
                if (m_table.TableName == outDS.LinksOutput.TableName)
                {
                    if (linkOutputVar)
                    {
                        addColumn = true;
                    }
                }
                if (m_table.TableName == outDS.NON_STOROutput.TableName)
                {
                    if (nodeOutputVar)
                    {
                        addColumn = true;
                    }
                }
                //Added variable to other nodes output tables.
                if (m_table.TableName == outDS.RESOutput.TableName)
                {
                    if (nodeOutputVar)
                    {
                        addColumn = true;
                    }
                }
                if (m_table.TableName == outDS.RES_STOROutput.TableName)
                {
                    if (nodeOutputVar)
                    {
                        addColumn = true;
                    }
                }
                if (m_table.TableName == outDS.DEMOutput.TableName)
                {
                    if (nodeOutputVar)
                    {
                        addColumn = true;
                    }
                }
                if (addColumn)
                {
                    if (!m_table.Columns.Contains(VarName))
                    {
                        DataColumn m_NewColumn = new DataColumn();
                        m_NewColumn.ColumnName = VarName;
                        m_NewColumn.DataType = typeof(double);
                        m_NewColumn.AllowDBNull = true;
                        m_NewColumn.Caption = VarName;
                        m_table.Columns.Add(m_NewColumn);
                    }
                }
            }
        }
        private string[] FindDisplayOutName(string columnName)
        {
            string[] Found =
            {
            columnName,
            "Flow"
        };
            switch (columnName)
            {
                //Links
                case "Loss":
                    Found[0] = "Channel loss or Routed Flow";
                    break;
                case "LMax":
                    Found[0] = "Capacity";
                    break;
                case "LMin":
                    Found[0] = "Minimum";
                    break;
                case "NaturalFlow":
                    Found[0] = "Last Natural Flow";
                    break;
                case "Accrual":
                    Found[0] = "Storage Accrual";
                    Found[1] = "Volume";
                    break;
                case "StorLeft":
                    Found[0] = "Storage Left";
                    Found[1] = "Volume";
                    break;
                case "GroupStorLeft":
                    Found[0] = "Group Storage Left";
                    Found[1] = "Volume";
                    break;
                case "GroupAccrual":
                    Found[0] = "Group Accrual";
                    Found[1] = "Volume";
                    break;
                //Demands
                case "Surf_In":
                    Found[0] = "Surface Inflow";
                    break;
                case "Gw_In":
                    Found[0] = "Groundwater Inflow";
                    break;
                //Reservoirs
                case "Stor_Beg":
                    Found[0] = "Beginning Storage";
                    Found[1] = "Volume";
                    break;
                case "Stor_End":
                    Found[0] = "Ending Storage";
                    Found[1] = "Volume";
                    break;
                case "Stor_Trg":
                    Found[0] = "Target Storage";
                    Found[1] = "Volume";
                    break;
                case "Evap_Loss":
                    Found[0] = "Evaporation Loss";
                    break;
                case "Ups_Rel":
                    Found[0] = "Upstream Inflow";
                    break;
                case "Pump_In":
                    Found[0] = "ByPass Flow";
                    break;
                case "Dws_Rel":
                    Found[0] = "Storage Release";
                    break;
                case "Pump_Out":
                    Found[0] = "Downstream Flow";
                    break;
                case "Powr_Avg":
                case "Power":
                    Found[0] = "Average Power";
                    Found[1] = "Power";
                    break;
                case "Pwr_2nd":
                    Found[0] = "Secondary Energy";
                    Found[1] = "Energy";
                    break;
                case "Energy":
                    Found[0] = "Energy";
                    Found[1] = "Energy";
                    break;
                case "Elev_End":
                    Found[0] = "Ending Elevation";
                    Found[1] = "Length";
                    break;
                case "Head_Avg":
                case "Head":
                    Found[0] = "Average Head";
                    Found[1] = "Length";
                    break;
                case "Hydra_Cap":
                    Found[0] = "Hydraulic Capacity";
                    break;
                case "Inflow":
                    Found[0] = "Local Inflow";
                    break;
                case "Flow_Thru":
                    Found[0] = "Flow-Thru Inflow";
                    break;
                case "Rout_Ret":
                    Found[0] = "Routed Inflow";
                    break;
                case "Hydro_State":
                    Found[0] = "Hydrologic State Index";
                    Found[1] = "Hydrologic State";
                    break;
                // Hydropower units
                case "Discharge":
                    Found[0] = "Hydro Unit Discharge";
                    Found[1] = "Flow";
                    break;
                case "Efficiency":
                    Found[0] = "Efficiency";
                    Found[1] = "Dimensionless";
                    break;
                case "DowntimeFactor":
                    Found[0] = "Downtime Factor";
                    Found[1] = "Dimensionless";
                    break;
                case "GeneratingHours":
                    Found[0] = "Generating Hours";
                    Found[1] = "Time Rate";
                    break;
                // Hydropower targets
                case "EnergyTarget":
                case "EnergyProduction":
                case "EnergyDifference":
                    Found[0] = "Energy " + columnName.Replace("Energy", "");
                    Found[1] = "Energy";
                    break;
                case "Meas_Flow":
                    Found[0] = "Measured Flow";
                    Found[1] = "Flow";
                    break;
                default:
                    string[] m_var = new string[2];
                    if (userDefinedOutVarsLinks != null)
                    {
                        foreach (string[] m_var_loopVariable in userDefinedOutVarsLinks)
                        {
                            m_var = m_var_loopVariable;
                            if (m_var[0] == columnName)
                            {
                                Found[0] = columnName;
                                Found[1] = m_var[1];
                            }
                        }
                    }
                    if (userDefinedOutVarsNodes != null)
                    {
                        foreach (string[] m_var_loopVariable in userDefinedOutVarsNodes)
                        {
                            m_var = m_var_loopVariable;
                            if (m_var[0] == columnName)
                            {
                                Found[0] = columnName;
                                Found[1] = m_var[1];
                            }
                        }
                    }
                    break;
            }
            return Found;
        }

        /// <summary>
        /// Used when the nodes and link time series are stored in a database.
        /// It writes all the model time series tables to a database.  These tables are associated with a particular scenario.
        /// The processing also creates a table "Info" that contains the metadata for the time sereis. 
        /// </summary>
        /// <param name="mi">active model</param>
        /// <param name="background">flag to run process time series in the background</param>
        public void TimeseriesToSQLite(Model mi, bool background = false, bool checkDBCopy = false)
        {
            string modelFileName = mi.fname;
            string networkName = BaseNameString(modelFileName);

            //Default timeseries database uses the same name than the XY File.
            string outFileName = networkName + ".sqlite";
            if (checkDBCopy)
            {
                if (mi.timeseriesInfo.dbPath == "" && outFileName != mi.timeseriesInfo.dbFullPath)
                {
                    //skip saving logic for the interface run file.
                    if (modelFileName != mi.timeseriesInfo.dbFullPath.Replace(".sqlite", "RUN.xy"))
                    {
                        if (MessageBox.Show("Do you want to copy the current timeseries database file?" + Environment.NewLine + "if [No] is selected, only the active time series will be saved.", "Saving timeseries database", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            if (File.Exists(outFileName))
                            {
                                if (MessageBox.Show("Do you want to overwrite the existing file?", "File already exists.", MessageBoxButtons.YesNo) == DialogResult.No)
                                {
                                    mi.FireOnError(" Error - writing timeseries to database aborted by the user. File was not saved correctly.");
                                    return;
                                }
                            }
                            File.Copy(mi.timeseriesInfo.dbFullPath, outFileName, true);
                        }
                    }
                }
            }
            if (mi.timeseriesInfo.dbPath != "")
            {
                outFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(modelFileName), mi.timeseriesInfo.dbPath));
                if (outFileName != mi.timeseriesInfo.dbFullPath)
                {
                    //The XY file has moved.
                    Uri basePath = new Uri(mi.fname);
                    Uri destinationUri = new Uri(mi.timeseriesInfo.dbFullPath);
                    mi.timeseriesInfo.dbPath = Uri.UnescapeDataString(basePath.MakeRelativeUri(destinationUri).ToString());
                    outFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(modelFileName), mi.timeseriesInfo.dbPath));
                    mi.timeseriesInfo.dbFullPath = outFileName;
                }
            }
            
            bool dbFileExist = File.Exists(outFileName);
            if (m_DB == null || m_DB.dbFile!=outFileName)
            {
                m_DB = new SQLiteHelper(outFileName);
                m_DB.FireErrorMessage += DatabaseMessagePumping;
                if (!dbFileExist) m_DB.CreateDatabaseFile();
            }
            //Process object timeseries to Dataset
            //DataSet m_TSDS = PrepareTSDaset(mi);
            PrepareLinkTSDict(mi,dbFileExist);
            PrepareNodeTSDict(mi,dbFileExist);
            // Update Data in the database
            mi.FireOnMessage("writing timeseries to database ...");
            mi.FireOnMessage("  Active Scenario: " + mi.timeseriesInfo.activeScn);
            List<object> arguments = new List<object>();
            arguments.Add(mi);
            if (background)
            {
                using (BackgroundWorker _bgworker = new BackgroundWorker())
                {
                    _bgworker.DoWork += m_DB.Bgworker_UpdateTSTables;
                    _bgworker.WorkerReportsProgress = false;
                    //_bgworker.RunWorkerCompleted += RunWorkerCompleted;
                    
                    _bgworker.RunWorkerAsync(arguments);
                }
            }
            else
            {

                try
                {
                    m_DB.Bgworker_UpdateTSTables(this, new DoWorkEventArgs(arguments));
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message); }

                //foreach (string keyStr in mi.m_TimeSeriesTbls.Keys)
                //{
                //    DataSet m_TSDS = mi.m_TimeSeriesTbls[keyStr];
                //    Model.FireOnMessageGlobal("  writting " + keyStr + " - " + m_TSDS.Tables.Count + " time series.");
                //    m_DB.CreateSQLiteTSFile(m_TSDS);
                //    double speed = m_DB.UpdateTables(m_TSDS, false);
                //    if (speed > 0) mi.FireOnMessage(speed.ToString("   #.#") + " rows/sec.");
                //    //m_TSDS.Dispose();     
                //}
                //m_DB.CommitTransaction();
                //mi.FireOnMessage(" Done writing timeseries to database.");
            }
            //string outLine = "";
            // Execute the command to import data into the database.
            //foreach (string keyStr in mi.m_TimeSeriesTbls.Keys)
            //{
            //    DataSet m_TSDS = mi.m_TimeSeriesTbls[keyStr];
            //    Model.FireOnMessageGlobal("  writting "+ keyStr + " - " + m_TSDS.Tables.Count + " time series.");
            //    m_DB.CreateSQLiteTSFile(m_TSDS);
            //    if (background) //Multithread
            //    {
            //        using (BackgroundWorker _bgworker = new BackgroundWorker())
            //        {
            //            _bgworker.DoWork += m_DB.Bgworker_UpdateTSTables;
            //            _bgworker.WorkerReportsProgress = false;
            //            List<object> arguments = new List<object>();
            //            arguments.Add(mi);
            //            arguments.Add(true);//commit
            //            m_DB.workingOnUpdates = true; //set in advance of UpdateTables for very small networks.
            //            _bgworker.RunWorkerAsync(arguments);
            //        }
            //    }
            //    else
            //    {
            //        //The last output processing (when commit = true) is not done in background threads to avoid issues with the output class initialization.
            //        m_DB.UpdateTables(mi);
            //    }
            //}
            //        return true;
            //}
            //    else
            //    {
            //        return false;
            //    }

        }

        public void TimeseriesFromSQLite(ref Model mi, bool background = false)
        {
            // Update Data from the database

            mi.FireOnMessage("retriving timeseries from database ...");
            
            List<object> arguments = new List<object>();
            arguments.Add(mi);
            if (background)
            {
                using (BackgroundWorker _bgworker = new BackgroundWorker())
                {
                    _bgworker.DoWork += PopulateTimeSeriedTbls;
                    _bgworker.WorkerReportsProgress = false;
                    //m_DB.workingOnUpdates = true; //set in advance of UpdateTables for very small networks.
                    _bgworker.RunWorkerAsync(arguments);
                }
            }
            else
            {
                try
                {
                    PopulateTimeSeriedTbls(this, new DoWorkEventArgs(arguments));
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message); }
            }
        }

        private void PopulateTimeSeriedTbls(object sender, DoWorkEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            List<object> arguments = e.Argument as List<object>;
            Csu.Modsim.ModsimModel.Model m_model = arguments[0] as Csu.Modsim.ModsimModel.Model;
           
            string modelFileName = m_model.fname;
            string networkName = BaseNameString(modelFileName);

            string outFileName = networkName + ".sqlite";
            if (m_model.timeseriesInfo.dbPath != "")
                outFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(modelFileName), m_model.timeseriesInfo.dbPath));

            m_model.FireOnMessage("  Using file: "+ outFileName +".");
            using (m_DB = new SQLiteHelper(outFileName))
            {
                m_DB.FireErrorMessage += DatabaseMessagePumping;

                string scenarioID = m_model.timeseriesInfo.activeScn.ToString();
                m_model.FireOnMessage("  Active scenario : " + scenarioID + ".");
                DataTable m_InfoTable = m_DB.GetDBTable("SELECT * FROM TSTablesInfo WHERE ScnID = " + scenarioID, "TSTablesInfo");
                //mi.m_TimeSeriesTbls["Info"].Tables
                foreach (DataRow dr in m_InfoTable.Rows)
                {
                    string tableName = dr["TableName"].ToString();
                    string sql = "SELECT * FROM [" + tableName + "] ORDER BY [Date]";
                    DataTable m_TSTbl = m_DB.GetDBTable(sql, tableName);
                    m_TSTbl.PrimaryKey = new DataColumn[] { m_TSTbl.Columns[0] };
                    Link l = null;
                    Node n = null;
                    if (dr["Type"].ToString() == "Link")
                    {
                        l = m_model.FindLink(new Guid(dr["uid"].ToString()));

                    }
                    else
                    {
                        n = m_model.FindNode(new Guid(dr["uid"].ToString()));
                    }
                    TimeSeries ts = null;
                    switch (dr["TSType"].ToString())
                    {
                        case "adaMeasured":
                            ts = l.m.adaMeasured;
                            break;
                        case "maxVariable":
                            ts = l.m.maxVariable;
                            break;
                        case "adaDemandsM":
                            ts = n.m.adaDemandsM;
                            break;
                        case "adaInflowsM":
                            ts = n.m.adaInflowsM;
                            break;
                        case "adaTargetsM":
                            ts = n.m.adaTargetsM;
                            break;
                        case "adaForecastsM":
                            ts = n.m.adaForecastsM;
                            break;
                        case "adaEvaporationsM":
                            ts = n.m.adaEvaporationsM;
                            break;
                        case "adaGeneratingHrsM":
                            ts = n.m.adaGeneratingHrsM;
                            break;
                        case "adaInfiltrationsM":
                            ts = n.m.adaInfiltrationsM;
                            break;
                    }
                    ts.dataTable = m_TSTbl;
                    ts.VariesByYear = dr["VariesByYear"].ToString() == "1";
                    ts.Interpolate = dr["Interpolate"].ToString() == "1";
                    ts.MultiColumn = dr["MultiColumn"].ToString() == "1";
                    ts.units = dr["units"].ToString();
                }
                //This is done here since loading ts could be done in the background!
                m_model.CheckUnits();
                m_DB.CommitTransaction();
            }
            //m_DB.Dispose();
            //m_DB = null;
            m_model.FireOnMessage("Completed loading time series from the database.");
        }

        private void PrepareLinkTSDict(Model mi, bool dbFileExist)
        {
            DataSet m_DS;
            if (mi.m_TimeSeriesTbls==null) mi.m_TimeSeriesTbls = new Dictionary<string, DataSet>();
            if (mi.m_TimeSeriesTbls.ContainsKey("Info") && !dbFileExist)
                mi.m_TimeSeriesTbls.Remove("Info");
            
            if (!mi.m_TimeSeriesTbls.ContainsKey("Info"))
            {
                //This assumes that links are processed before nodes.
                m_DS = new DataSet("Info");
                mi.m_TimeSeriesTbls.Add("Info", m_DS);
                DataTable m_InfoTable = new DataTable("TSTablesInfo");
                m_InfoTable.Columns.Add("ScnID", typeof(int));
                m_InfoTable.Columns.Add("TableName", typeof(string));
                m_InfoTable.Columns.Add("Type", typeof(string));
                m_InfoTable.Columns.Add("ObjName", typeof(string));
                m_InfoTable.Columns.Add("TSType", typeof(string));
                m_InfoTable.Columns.Add("units", typeof(string));
                m_InfoTable.Columns.Add("VariesByYear", typeof(bool));
                m_InfoTable.Columns.Add("MultiColumn", typeof(bool));
                m_InfoTable.Columns.Add("Interpolate", typeof(bool));
                m_InfoTable.Columns.Add("uid", typeof(string));
                m_InfoTable.PrimaryKey = new DataColumn[] { m_InfoTable.Columns["uid"], m_InfoTable.Columns["ScnID"], m_InfoTable.Columns["TSType"] };
                mi.m_TimeSeriesTbls["Info"].Tables.Add(m_InfoTable);
            }
            
            string scenarioID = mi.timeseriesInfo.activeScn.ToString();
            string[] lTSList = new string[] { "adaMeasured", "maxVariable" };
            foreach (string m_ts in lTSList)
            {
                
                if (!mi.m_TimeSeriesTbls.ContainsKey(m_ts))
                {
                    m_DS = new DataSet("_" + m_ts);
                    mi.m_TimeSeriesTbls.Add(m_ts, m_DS);
                }
                    else
                {
                    m_DS = mi.m_TimeSeriesTbls[m_ts];
                }
                //m_DS = new DataSet("_maxVariable");
                //mi.m_TimeSeriesTbls.Add("maxVariable", m_DS);

                try
                {
                    for (Link l = mi.firstLink; l != null; l = l.next)
                    {
                        TimeSeries thisTS = null;

                        switch (m_ts)
                        {
                            case "adaMeasured":
                                thisTS = l.m.adaMeasured;
                                break;
                            case "maxVariable":
                                thisTS = l.m.maxVariable;
                                break;
                        }
                        if (thisTS != null && thisTS.getSize() > 0)
                        {
                            thisTS.dataTable.TableName = scenarioID + "_" + l.uid + "_" + m_ts;
                            if (mi.m_TimeSeriesTbls[m_ts].Tables.Contains(thisTS.dataTable.TableName))
                                mi.m_TimeSeriesTbls[m_ts].Tables.Remove(thisTS.dataTable.TableName);  //Is this going to mess up the db update?
                            mi.m_TimeSeriesTbls[m_ts].Tables.Add(thisTS.dataTable.Copy());
                            mi.m_TimeSeriesTbls[m_ts].Tables[thisTS.dataTable.TableName].PrimaryKey = new DataColumn[] { mi.m_TimeSeriesTbls[m_ts].Tables[thisTS.dataTable.TableName].Columns[0] };
                            //mi.m_TimeSeriesTbls[m_ts].Tables[thisTS.dataTable.TableName].AcceptChanges();
                            ProcessInfoDRow(ref mi, new object[] { scenarioID, thisTS.dataTable.TableName, "Link", l.name, m_ts, thisTS.units.Label, thisTS.VariesByYear, thisTS.MultiColumn, thisTS.Interpolate, l.uid.ToString() });
                        }
                    }
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message); }
            }
        }

        private void PrepareNodeTSDict(Model mi, bool dbFileExist)
        {
            if (mi.m_TimeSeriesTbls ==null) mi.m_TimeSeriesTbls = new Dictionary<string, DataSet>();
            string scenarioID = mi.timeseriesInfo.activeScn.ToString();
            string[] nTSList = new string[] { "adaDemandsM", "adaInflowsM", "adaTargetsM","adaForecastsM","adaEvaporationsM","adaGeneratingHrsM","adaInfiltrationsM" };
            DataSet m_DS;
            foreach (string m_ts in nTSList)
            {
                if (!mi.m_TimeSeriesTbls.ContainsKey(m_ts))
                {
                    m_DS = new DataSet("_" + m_ts);
                    mi.m_TimeSeriesTbls.Add(m_ts, m_DS);
                }
                else
                {
                    m_DS = mi.m_TimeSeriesTbls[m_ts];
                }
                                
                try
                {
                    for (Node n = mi.firstNode; n != null; n = n.next)
                    {
                        TimeSeries thisTS = null;

                        switch (m_ts)
                        {
                            case "adaDemandsM":
                                thisTS = n.m.adaDemandsM;
                                break;
                            case "adaInflowsM":
                                thisTS = n.m.adaInflowsM;
                                break;
                            case "adaTargetsM":
                                thisTS = n.m.adaTargetsM;
                                break;
                            case "adaForecastsM":
                                thisTS = n.m.adaForecastsM;
                                break;
                            case "adaEvaporationsM":
                                thisTS = n.m.adaEvaporationsM;
                                break;
                            case "adaGeneratingHrsM":
                                thisTS = n.m.adaGeneratingHrsM;
                                break;
                            case "adaInfiltrationsM":
                                thisTS = n.m.adaInfiltrationsM;
                                break;
                        }
                        if (thisTS != null && thisTS.getSize() > 0)
                        {
                            thisTS.dataTable.TableName = scenarioID + "_" + n.uid + "_" + m_ts;
                            if (mi.m_TimeSeriesTbls[m_ts].Tables.Contains(thisTS.dataTable.TableName))
                                mi.m_TimeSeriesTbls[m_ts].Tables.Remove(thisTS.dataTable.TableName);  //Is this going to mess up the db update?
                            mi.m_TimeSeriesTbls[m_ts].Tables.Add(thisTS.dataTable.Copy());
                            mi.m_TimeSeriesTbls[m_ts].Tables[thisTS.dataTable.TableName].PrimaryKey = new DataColumn[] { mi.m_TimeSeriesTbls[m_ts].Tables[thisTS.dataTable.TableName].Columns[0] };
                            ProcessInfoDRow(ref mi, new object[] { scenarioID, thisTS.dataTable.TableName, "Node", n.name, m_ts, thisTS.units!=null?thisTS.units.Label:"", thisTS.VariesByYear, thisTS.MultiColumn, thisTS.Interpolate, n.uid.ToString() });
                        }
                    }
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message); }
            }
        }

        private void ProcessInfoDRow(ref Model mi, object[] p)
        {
            DataTable dt = mi.m_TimeSeriesTbls["Info"].Tables["TSTablesInfo"];
            DataRow[] drs = dt.Select("TableName = '" + p[1] + "'");
            if (drs.Length == 1)
                drs[0].ItemArray = p;
            else
                mi.m_TimeSeriesTbls["Info"].Tables["TSTablesInfo"].Rows.Add(p);
        }

        private DataSet PrepareTSDaset(Model mi)
        {
            DataSet m_DS = new DataSet("Observations");
            for (Link l = mi.firstLink; l != null; l = l.next) 
            {
                if (l.m.adaMeasured != null)
                {
                    l.m.adaMeasured.dataTable.TableName = l.name + "_adaMeasured";
                    m_DS.Tables.Add(l.m.adaMeasured.dataTable);
                }
            }
            return m_DS;
        }

        public DataTable GetRunOutputTSTbl(string OutputDB, Link l, bool returnInternalTbl = false, string variable = "Flow")
        {
            DataTable result = null;
            try
            {
                if (File.Exists(OutputDB))
                {
                    double factor = 1;
                    if (returnInternalTbl) factor = uFactor;
                    string sql = "SELECT TSDate AS[Date]," + variable + "*" + factor + " AS HS0 " +
                            " FROM LinksOutput JOIN TimeSteps ON TimeSteps.TSIndex = LinksOutput.TSIndex" +
                            " JOIN LinksInfo ON LinksInfo.LNumber = LinksOutput.LNumber" +
                            " WHERE LinksInfo.LName = '" + l.name + "'  ORDER BY TSDate1; ";
                    result = GetOutputDBTbl(OutputDB, sql, l.name);
                    
                }
            } 
            catch {}
            return result;
        }

        private DataTable GetOutputDBTbl(string OutputDB, string sql, string name)
        {
            if (OutputDB.EndsWith(".sqlite"))
            {
                using (SQLiteHelper m_OutputDB = new SQLiteHelper(OutputDB))
                {
                    m_OutputDB.FireErrorMessage += DatabaseMessagePumping;
                    return m_OutputDB.GetDBTable(sql, name + "OUTPUT");
                }
            }
            else 
            {
                //Get Current Output DB                      
                using (DBUtil m_OutputDB = new DBUtil(OutputDB))
                {
                    m_OutputDB.FireMessage += DatabaseMessagePumping;
                    return m_OutputDB.GetTable(sql, name + "OUTPUT");
                }
            }
        }

        public DataTable GetRunOutputTSTbl(string OutputDB, Node n, bool returnInternalTbl, string variable = "Surf_In", string outputTblName = "DEMOutput")
        {
            DataTable result = null;
            try
            {
                if (File.Exists(OutputDB))
                {
                    double factor = 1;
                    if (returnInternalTbl) factor = uFactor;
                    string sql = "SELECT TSDate AS[Date]," + variable + "*" + factor+ " AS HS0 " +
                            " FROM " + outputTblName + " JOIN TimeSteps ON TimeSteps.TSIndex = " + outputTblName + ".TSIndex" +
                            " JOIN NodesInfo ON NodesInfo.NNumber = " + outputTblName + ".NNo" +
                            " WHERE NodesInfo.NName = '" + n.name + "'  ORDER BY TSDate1; ";
                    result = GetOutputDBTbl(OutputDB, sql, n.name);
                }
            }
            catch { }
            return result;
        }

    }
}
