using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.NetworkUtils
{
    public class ModelOutputMSDB
    {
        public enum InfoTableType
        {
            NodesInfo,
            LinksInfo,
            TimeSteps,
            Links,
            ReservoirFlows,
            ReservoirStorage,
            Demands,
            NonStorage
        }
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
            NonStorage,
            NodesInfo,
            LinksInfo,
            TableInfo,
            TimeSteps
        }
        public enum NodeColumnsNames
        {
            Surf_In,
            Gw_In,
            Demand,
            Shortage,
            Inflow,
            Flow_Thru,
            Stor_Beg,
            Stor_End,
            Stor_Trg,
            Elev_End,
            Spills,
            Evap_Loss,
            Seepage,
            Ups_Rel,
            Pump_In,
            Dws_Rel,
            Pump_Out,
            Hydra_Cap,
            Head_Avg,
            Powr_Avg,
            Energy,
            Pwr_2nd,
            Hydro_State
        }
        private OleDbConnection conn;
        public bool statusReady;
        public bool enableMTheading;
        private string DBFullPath;
        public string networkBaseName;
        MODSIMOutputDS outDS;
        DataTable outputVarInfo;
        private Model ModsimModel;
        public event OnMODSIMOutputMessageEventHandler OnMODSIMOutputMessage;
        public delegate void OnMODSIMOutputMessageEventHandler(string msg);

        //SQLiteHelper m_DBSQLite = null;
        DBUtil MS_DB = null;

        public static readonly string[] indices = new string[]
        {
            "NNo",
            "TSIndex",
            "LNumber",
            "HydroUnitID",
            "HydroTargetID"

        };
        public ModelOutputMSDB(Model mi, bool enableMThreading = false, bool enableSpecialLinks = false)
        {
            // Set the culture and UI culture before
            // the call to InitializeComponent.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            ModsimModel = mi;
            networkBaseName = ModelOutputSupport.BaseNameString(mi.fname);
            enableMTheading = enableMThreading;
            if (Csu.Modsim.ModsimModel.OutputControlInfo.ver8MSDBOutputFiles)
            {
                DBFullPath = ModelOutputSupport.BaseNameString(mi.fname) + "OUTPUT.mdb";
                //Get Current Output DB
                MS_DB = new DBUtil(DBFullPath);
                MS_DB.FireMessage += DatabaseMessagePumping;
                if (File.Exists(DBFullPath))
                {
                    OpenDBConnection();
                    CloseDBConnection();
                }
                statusReady = false;
            }
            if (OutputControlInfo.SQLiteOutputFiles)
            {
                DBFullPath = ModelOutputSupport.BaseNameString(mi.fname) + "OUTPUT.sqlite";
                using (SQLiteHelper m_DBSQLite = new SQLiteHelper(DBFullPath, true))
                {
                    m_DBSQLite.FireErrorMessage += DatabaseMessagePumping;
                    if (File.Exists(DBFullPath))
                    {
                        statusReady = m_DBSQLite.TestDatabaseStatus();
                    }
                }
            }
            if (File.Exists(DBFullPath)) GetVariableInfoTables(mi, enableSpecialLinks);
            outDS = new MODSIMOutputDS(mi);
        }
        private void GetVariableInfoTables(Model mi, bool addSpecialLinks = false)
        {
            string str = "SELECT * FROM OutputTablesInfo;";
            outputVarInfo = GetTableFromDB(str, "OutputTablesInfo");
            if (addSpecialLinks)
            {
                //Adds the type of output for the special links output. (at run time)
                List<List<string>> specialLinks = CreateSpecialLinkToNodesList();
                foreach (string link in specialLinks[1])
                {
                    DataRow m_row = outputVarInfo.NewRow();
                    m_row["Name"] = link;
                    m_row["VariableType"] = "Flow";
                    outputVarInfo.Rows.Add(m_row);
                }
                specialLinks = CreateSpecialLinkToLinksList();
                foreach (string link in specialLinks[1])
                {
                    DataRow m_row = outputVarInfo.NewRow();
                    m_row["Name"] = link;
                    m_row["VariableType"] = "Flow";
                    outputVarInfo.Rows.Add(m_row);
                }
            }
            mi.controlinfo.variableOutputNames = outputVarInfo;
            str = "SELECT OutputTablesInfo.VariableType FROM OutputTablesInfo GROUP BY OutputTablesInfo.VariableType;";
            DataTable m_OutputVarTypeInfo = GetTableFromDB(str, "OutputTablesInfo");
            mi.controlinfo.variableOutputTypes = m_OutputVarTypeInfo;
        }
        //This sub is needed to populate the mdb from the CSVs when the model is run or when after running the model with the flag create mdb file = FALSE the output is desired.
        public void InitializeOutput()
        {
            if (!statusReady)
            {
                CheckForAvailableMODSIMRun();
                //If statusReady And Not File.Exists(DBFullPath) Then
                if (statusReady)
                {
                    ModsimModel.FireOnMessage("Generating MDB Output...");
                    // Create the database.
                    try
                    {
                        if (File.Exists(DBFullPath))
                        {
                            File.Delete(DBFullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModsimModel.FireOnError("Unable to delete old database." + Environment.NewLine + Environment.NewLine + "Error Message: " + Environment.NewLine + ex.ToString());
                    }
                    DBUtil db = new DBUtil(DBFullPath, false, "", true, MSType.Access);
                    db.Create();

                    //CreateAccessDatabase()
                    if (enableMTheading)
                    {
                        Thread t = null;
                        t = new Thread(this.PopulateDBFromCSV);
                        statusReady = false;
                        t.Start();
                        statusReady = true;
                    }
                    else
                    {
                        statusReady = true;
                        this.PopulateDBFromCSV();
                    }
                }
                else
                {
                    //Check if there exist an previously created *MDB output file
                    if (File.Exists(DBFullPath))
                    {
                        statusReady = true;
                    }
                }
            }
        }
        public void DisableMSDBOutput()
        {
            statusReady = false;
        }
        private void CheckForAvailableMODSIMRun()
        {
            statusReady = true;
            try
            {
                foreach (DataTable table in outDS.Tables)
                {
                    string outFileName = networkBaseName + table.TableName + ".CSV";
                    if (!File.Exists(outFileName) || (File.Exists(DBFullPath) & File.GetLastWriteTime(DBFullPath) > File.GetLastWriteTime(outFileName)))
                    {
                        statusReady = false;
                        //Model.FireOnErrorGlobal("MODSIM CSV output files not found at this time.")
                        return;
                    }
                }
            }
            catch
            {
                ModsimModel.FireOnError("ERROR: looking for available MODSIM run");
            }
        }
        public event LoadMDBOutputfinishedEventHandler LoadMDBOutputfinished;
        public delegate void LoadMDBOutputfinishedEventHandler();
        private void PopulateDBFromCSV()
        {
            bool success = true;
            Stopwatch sw = Stopwatch.StartNew();

            // Create new oledb connection
            OpenDBConnection();
            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = conn;

            // Clear existing table info
            outDS.Clear();

            // Load each csv file into outDS
            for (int i = 0; i < outDS.Tables.Count; i++)
            {
                DataTable table = outDS.Tables[i];

                string outFileName = networkBaseName + table.TableName + ".csv";
                if (File.Exists(outFileName))
                {
                    // Check and match columns in the csv files
                    string[] csv_cols = GetCsvColumns(outFileName);
                    string[] tbl_cols = table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();
                    //Additional columns
                    if (csv_cols.Length > 0)
                    {
                        //Handles custom user output columns that would not be found in the template mbd
                        string[] add_cols = Array.FindAll(csv_cols, element => !tbl_cols.Contains(element));
                        foreach (string col in add_cols)
                        {
                            cmd.CommandText = "ALTER TABLE " + table.TableName + " ADD " + col + " DOUBLE";
                            cmd.ExecuteNonQuery();
                            table.Columns.Add(col,Type.GetType("System.Int32"));
                        }
                        tbl_cols = table.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray();
                    }
                    // Drop columns not in csv file
                    if (csv_cols.Length > 0 && csv_cols.Length != tbl_cols.Length)
                    {
                        table = table.DefaultView.ToTable(false, csv_cols);
                        string[] drop_cols = Array.FindAll(tbl_cols, element => !csv_cols.Contains(element));

                        string drop = string.Join(",", drop_cols);
                        cmd.CommandText = "ALTER TABLE " + table.TableName + " DROP COLUMN " + drop;
                        cmd.ExecuteNonQuery();
                    }

                    // Create schemi.ini to control data types in access columns
                    CreateSchema(cmd, table);

                    // Insert new table from the csv file
                    try
                    {
                        ConnectionBuilder builder = new ConnectionBuilder(outFileName, true, ",");
                        cmd.CommandText = "INSERT INTO " + table.TableName + " SELECT * FROM " + builder.TextConnStringInQuery();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        statusReady = false;
                        ModsimModel.FireOnError("FAILED TO LOAD CSV FILES FOR " + table.TableName + Environment.NewLine + Environment.NewLine + "ERROR: " + Environment.NewLine + ex.ToString());
                        success = false;
                    }
                }
                else
                {
                    ModsimModel.FireOnError(Path.GetFileName(outFileName) + " does not exist... Skipping.");
                }
            }
            if (success)
            {
                statusReady = true;
                sw.Stop();
                string msg = string.Format("Done Generating MDB Output (elapsed: {0:0.000 sec})", sw.Elapsed.TotalSeconds);
                ModsimModel.FireOnMessage(msg);
                CloseDBConnection();
                DeleteSchema();
            }
            else
            {
                CloseDBConnection();
                DeleteSchema();
            }

            if (LoadMDBOutputfinished != null)
            {
                LoadMDBOutputfinished();
            }
        }

        private string[] GetCsvColumns(string outFileName)
        {
            string[] rval = new string[0];
            if (File.Exists(outFileName))
            {
                using (StreamReader sr = new StreamReader(outFileName))
                {
                    if (!sr.EndOfStream)
                    {
                        rval = sr.ReadLine().Split(',');
                    }
                }
            }
            return rval;
        }

        private void DeleteSchema()
        {
            string inifile = Path.Combine(Path.GetDirectoryName(networkBaseName), "schema.ini");
            if (File.Exists(inifile))
            {
                File.Delete(inifile);
            }
        }

        private void CreateSchema(OleDbCommand cmd, DataTable table)
        {
            string csvfile = networkBaseName + table + ".csv";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[" + Path.GetFileName(csvfile) + "]");
            sb.AppendLine("ColNameHeader=True");
            sb.AppendLine("Format=CSVDelimited");
            sb.AppendLine("MaxScanRows=0");
            sb.AppendLine("CharacterSet=ANSI");
            sb.AppendLine("DateTimeFormat=" + TimeManager.DateFormat.Replace("mm", "nn"));

            for (int i = 0; i < table.Columns.Count; i++)
            {
                var col = table.Columns[i];
                sb.AppendLine(string.Format("Col{0}={1} {2}", i + 1, col.ColumnName, GetJetStringType(col.DataType.Name)));
            }

            string inifile = Path.Combine(Path.GetDirectoryName(csvfile), "schema.ini");
            using (StreamWriter sw = new StreamWriter(inifile))
            {
                sw.Write(sb);
            }
        }

        private string GetJetStringType(string datatype)
        {
            switch (datatype)
            {
                case "String":
                    return "Text";
                case "Int32":
                    return "Integer";
                case "Int64":
                    return "Long";
                case "Double":
                    return datatype;
                case "DateTime":
                    return datatype;
                case "Boolean":
                    return "Bit";
                case "Object":
                    return "Text";
                default:
                    throw new TypedDataSetGeneratorException("undefined DataType: " + datatype);
            }
        }

        private void CreateTable(OleDbCommand insertCMD, DataTable table)
        {
            try
            {
                string insertStr = "CREATE TABLE " + table.TableName + " (";
                bool first = true;
                foreach (DataColumn col in table.Columns)
                {
                    if (!first)
                    {
                        insertStr += ", ";
                    }
                    insertStr += "[" + col.ColumnName + "]";
                    insertStr += DBUtil.MapToSQLType(col.DataType.ToString());
                    first = false;
                }
                insertStr += ");";
                insertCMD.CommandText = insertStr;
                Int32 recordsAffected = insertCMD.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string CreateInsertString(DataTable table)
        {
            string rval = "";
            try
            {
                rval = "INSERT INTO " + table.TableName + " (";
                DataColumn m_col = null;
                bool first = true;
                foreach (DataColumn m_col_loopVariable in table.Columns)
                {
                    m_col = m_col_loopVariable;
                    if (!first)
                    {
                        rval += ", ";
                    }
                    rval += "[" + m_col.ColumnName + "]";
                    first = false;
                }
                rval += ") ";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return rval;
        }

        private void OpenDBConnection()
        {
            conn = new System.Data.OleDb.OleDbConnection();
            conn.ConnectionString = (new ConnectionBuilder(DBFullPath)).ConnectionString;

            try
            {
                conn.Open();
            }
            catch (System.Exception e)
            {
                ModsimModel.FireOnError(e.Message);
                statusReady = false;
            }
            finally
            {
            }
        }

        private void CloseDBConnection()
        {
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
        }

        /// <summary>Gets a datatable of output values for links.</summary>
        /// <param name="link">The link to retrieve output for.</param>
        /// <param name="addLink">Another link to retrieve output for.</param>
        /// <param name="scenariosActive">Specifies whether or not scenarios analysis is on.</param>
        public DataTable[] LinkOutput(Link link, Link addLink = null, bool scenariosActive = false)
        {
            InitializeOutput();
            if (statusReady)
            {
                try
                {
                    DataTable[] dts = new DataTable[1];
                    string str = "SELECT TimeSteps.TSDate AS [Start Date], TimeSteps.EndDate AS [End Date], TimeSteps.MidDate AS [Mid Date],";
                    if (addLink == null)
                    {
                        str += GetSELECTVariables("LinksOutput") + " FROM (LinksInfo INNER JOIN (LinksOutput INNER JOIN TimeSteps ON LinksOutput.TSIndex = TimeSteps.TSIndex) ON LinksInfo.LNumber = LinksOutput.LNumber )";
                        if(str.Contains("LinksMeasured")) str += " LEFT JOIN (LinksMeasured) ON (LinksOutput.TSIndex = LinksMeasured.TSIndex AND LinksOutput.LNumber = LinksMeasured.LNumber)";
                        str += " WHERE (((LinksInfo.LNumber)=" + link.number + ") AND ((LinksInfo.LName)='" + link.name + "'))";
                    }
                    else
                    {
                        //Measured flow output is not implemented when link flows are added 
                        str = "SELECT TimeSteps.TSDate AS [Start Date], TimeSteps.EndDate AS [End Date], TimeSteps.MidDate AS [Mid Date]," + " Sum(LinksOutput.Flow) AS [Flow], Sum(LinksOutput.Loss) AS [Channel loss or Routed Flow], Sum(LinksOutput.NaturalFlow) AS [NaturalFlow], Sum(LinksOutput.LMax) AS [LMax], Sum(LinksOutput.LMin) AS [LMin], " + "  Sum(LinksOutput.StorLeft) AS [Storage Left], Sum(LinksOutput.Accrual) AS [Accrual] ";
                        str += " FROM LinksInfo INNER JOIN (LinksOutput INNER JOIN TimeSteps ON LinksOutput.TSIndex = TimeSteps.TSIndex) ON LinksInfo.LNumber = LinksOutput.LNumber";
                        str += " WHERE ((((LinksInfo.LNumber)=" + link.number + ") AND ((LinksInfo.LName)='" + link.name + "')) OR (((LinksInfo.LNumber)=" + addLink.number + ") AND ((LinksInfo.LName)='" + addLink.name + "')))" + " GROUP BY TimeSteps.TSDate, TimeSteps.EndDate, TimeSteps.MidDate, LinksOutput.TSIndex ";
                    }
                    str += " ORDER BY TimeSteps.TSIndex;";
                    dts[0] = GetTableFromDB(str, "SummaryLinkOutput");
                    if (string.IsNullOrEmpty(link.name))
                    {
                        dts[0].TableName = "LinkNo" + link.number.ToString();
                    }
                    else
                    {
                        dts[0].TableName = link.name + "(" + link.number + ")";
                    }
                    if (scenariosActive)
                    {
                        dts[0].TableName = Path.GetFileNameWithoutExtension(DBFullPath).Split(new string[] { "OUTPUT" }, StringSplitOptions.None)[0] + "__" + dts[0].TableName;
                    }
                    return dts;
                }
                catch
                {
                    ModsimModel.FireOnError("Error generating link output from MDB file");
                    return null;
                }
            }
            return null;
        }

        /// <summary>Gets a datatable of output values for links.</summary>
        /// <param name="link">The link to retrieve output for.</param>
        /// <param name="addLink">Another link to retrieve output for.</param>
        /// <param name="scenariosActive">Specifies whether or not scenarios analysis is on.</param>
        public DataTable[] LinkOutput(Link link, out string str, Link addLink = null, bool scenariosActive = false)
        {
            InitializeOutput();
            str = "";
            if (statusReady)
            {
                try
                {
                    DataTable[] dts = new DataTable[1];
                    str = "SELECT TimeSteps.TSDate AS [Start Date], TimeSteps.EndDate AS [End Date], TimeSteps.MidDate AS [Mid Date],";
                    if (addLink == null)
                    {
                        str += GetSELECTVariables("LinksOutput") + " FROM (LinksInfo INNER JOIN (LinksOutput INNER JOIN TimeSteps ON LinksOutput.TSIndex = TimeSteps.TSIndex) ON LinksInfo.LNumber = LinksOutput.LNumber )";
                        if (str.Contains("LinksMeasured")) str += " LEFT JOIN (LinksMeasured) ON (LinksOutput.TSIndex = LinksMeasured.TSIndex AND LinksOutput.LNumber = LinksMeasured.LNumber)";
                        str += " WHERE (((LinksInfo.LNumber)=" + link.number + ") AND ((LinksInfo.LName)='" + link.name + "'))";
                    }
                    else
                    {
                        //Measured flow output is not implemented when link flows are added 
                        str = "SELECT TimeSteps.TSDate AS [Start Date], TimeSteps.EndDate AS [End Date], TimeSteps.MidDate AS [Mid Date]," + " Sum(LinksOutput.Flow) AS [Flow], Sum(LinksOutput.Loss) AS [Channel loss or Routed Flow], Sum(LinksOutput.NaturalFlow) AS [NaturalFlow], Sum(LinksOutput.LMax) AS [LMax], Sum(LinksOutput.LMin) AS [LMin], " + "  Sum(LinksOutput.StorLeft) AS [Storage Left], Sum(LinksOutput.Accrual) AS [Accrual] ";
                        str += " FROM LinksInfo INNER JOIN (LinksOutput INNER JOIN TimeSteps ON LinksOutput.TSIndex = TimeSteps.TSIndex) ON LinksInfo.LNumber = LinksOutput.LNumber";
                        str += " WHERE ((((LinksInfo.LNumber)=" + link.number + ") AND ((LinksInfo.LName)='" + link.name + "')) OR (((LinksInfo.LNumber)=" + addLink.number + ") AND ((LinksInfo.LName)='" + addLink.name + "')))" + " GROUP BY TimeSteps.TSDate, TimeSteps.EndDate, TimeSteps.MidDate, LinksOutput.TSIndex ";
                    }
                    str += " ORDER BY TimeSteps.TSIndex;";
                    dts[0] = GetTableFromDB(str, "SummaryLinkOutput");
                    if (string.IsNullOrEmpty(link.name))
                    {
                        dts[0].TableName = "LinkNo" + link.number.ToString();
                    }
                    else
                    {
                        dts[0].TableName = link.name + "(" + link.number + ")";
                    }
                    if (scenariosActive)
                    {
                        dts[0].TableName = Path.GetFileNameWithoutExtension(DBFullPath).Split(new string[] { "OUTPUT" }, StringSplitOptions.None)[0] + "__" + dts[0].TableName;
                    }
                    return dts;
                }
                catch
                {
                    ModsimModel.FireOnError("Error generating link output from MDB file");
                    return null;
                }
            }
            return null;
        }

        /// <summary>Gets a datatable of output values for hydropower units.</summary>
        /// <param name="hydroUnit">The hydropower unit for which to retrieve output</param>
        /// <param name="scenariosActive">Specifies whether or not scenarios analysis is on.</param>
        public DataTable[] HydroOutput(HydropowerUnit hydroUnit, bool scenariosActive = false)
        {
            InitializeOutput();
            if (statusReady)
            {
                try
                {
                    MODSIMOutputDS.TimeStepsDataTable tsDT = this.outDS.TimeSteps;
                    HydropowerControllerDataSet.HydroUnitsInfoDataTable hydroInfoDT = this.ModsimModel.hydro.HydroUnitsTable;
                    MODSIMOutputDS.HydroUnitOutputDataTable hydroOutDT = this.outDS.HydroUnitOutput;
                    string hydroInfoIDCol = hydroInfoDT.TableName + "." + hydroInfoDT.HydroUnitIDColumn.ColumnName;
                    string hydroInfoNameCol = hydroInfoDT.TableName + "." + hydroInfoDT.HydroUnitNameColumn.ColumnName;
                    string hydroOutIDCol = hydroOutDT.TableName + "." + hydroOutDT.HydroUnitIDColumn.ColumnName;
                    string hydroOutTSCol = hydroOutDT.TableName + "." + hydroOutDT.TSIndexColumn.ColumnName;
                    string timestepsTSCol = tsDT.TableName + "." + tsDT.TSIndexColumn.ColumnName;
                    string startDateCol = tsDT.TableName + "." + tsDT.TSDateColumn.ColumnName;
                    string endDateCol = tsDT.TableName + "." + tsDT.EndDateColumn.ColumnName;
                    string midDateCol = tsDT.TableName + "." + tsDT.MidDateColumn.ColumnName;
                    DataTable[] dts = new DataTable[1];
                    string m_str = "SELECT " + startDateCol + " AS [Start Date], " + endDateCol + " AS [End Date], " + midDateCol + " AS [Mid Date]," + GetSELECTVariables(hydroOutDT.TableName) + " FROM (" + hydroOutDT.TableName + " INNER JOIN " + hydroInfoDT.TableName + " ON " + hydroOutIDCol + " = " + hydroInfoIDCol + ") INNER JOIN " + this.outDS.TimeSteps.TableName + " ON " + hydroOutTSCol + " = " + timestepsTSCol + " WHERE " + hydroInfoIDCol + " = " + hydroUnit.ID.ToString() + " AND " + hydroInfoNameCol + " = '" + hydroUnit.Name + "'" + " ORDER BY " + timestepsTSCol + ";";
                    dts[0] = GetTableFromDB(m_str, "HydroOutput");
                    if (string.IsNullOrEmpty(hydroUnit.Name))
                    {
                        dts[0].TableName = "HydroUnitID" + hydroUnit.ID.ToString();
                    }
                    else
                    {
                        dts[0].TableName = hydroUnit.Name + "(" + hydroUnit.ID + ")";
                    }
                    if (scenariosActive)
                    {
                        dts[0].TableName = Path.GetFileNameWithoutExtension(DBFullPath).Split(new string[] { "OUTPUT" }, StringSplitOptions.None)[0] + "__" + dts[0].TableName;
                    }
                    return dts;
                }
                catch (Exception ex)
                {
                    ModsimModel.FireOnError("Error generating link output from MDB file." + Environment.NewLine + Environment.NewLine + "Error Message: " + Environment.NewLine + ex.ToString());
                    return null;
                }
            }
            return null;
        }
        /// <summary>Gets a datatable of output values for hydropower targets.</summary>
        /// <param name="hydroTarget">The hydropower target for which to retrieve output</param>
        /// <param name="scenariosActive">Specifies whether or not scenarios analysis is on.</param>
        public DataTable[] HydroOutput(HydropowerTarget hydroTarget, bool scenariosActive = false)
        {
            InitializeOutput();
            if (statusReady)
            {
                try
                {
                    MODSIMOutputDS.TimeStepsDataTable tsDT = this.outDS.TimeSteps;
                    HydropowerControllerDataSet.HydroTargetsInfoDataTable hydroInfoDT = this.ModsimModel.hydro.HydroTargetsTable;
                    MODSIMOutputDS.HydroTargetOutputDataTable hydroOutDT = this.outDS.HydroTargetOutput;
                    string hydroInfoIDCol = hydroInfoDT.TableName + "." + hydroInfoDT.HydroTargetIDColumn.ColumnName;
                    string hydroInfoNameCol = hydroInfoDT.TableName + "." + hydroInfoDT.HydroTargetNameColumn.ColumnName;
                    string hydroOutIDCol = hydroOutDT.TableName + "." + hydroOutDT.HydroTargetIDColumn.ColumnName;
                    string hydroOutTSCol = hydroOutDT.TableName + "." + hydroOutDT.TSIndexColumn.ColumnName;
                    string timestepsTSCol = tsDT.TableName + "." + tsDT.TSIndexColumn.ColumnName;
                    string startDateCol = tsDT.TableName + "." + tsDT.TSDateColumn.ColumnName;
                    string endDateCol = tsDT.TableName + "." + tsDT.EndDateColumn.ColumnName;
                    string midDateCol = tsDT.TableName + "." + tsDT.MidDateColumn.ColumnName;
                    DataTable[] dts = new DataTable[1];
                    string m_str = "SELECT " + startDateCol + " AS [Start Date], " + endDateCol + " AS [End Date], " + midDateCol + " AS [Mid Date]," + GetSELECTVariables(hydroOutDT.TableName) + " FROM (" + hydroOutDT.TableName + " INNER JOIN " + hydroInfoDT.TableName + " ON " + hydroOutIDCol + " = " + hydroInfoIDCol + ") INNER JOIN " + this.outDS.TimeSteps.TableName + " ON " + hydroOutTSCol + " = " + timestepsTSCol + " WHERE " + hydroInfoIDCol + " = " + hydroTarget.ID.ToString() + " AND " + hydroInfoNameCol + " = '" + hydroTarget.Name + "'" + " ORDER BY " + timestepsTSCol + ";";
                    dts[0] = GetTableFromDB(m_str, "HydroOutput");
                    if (string.IsNullOrEmpty(hydroTarget.Name))
                    {
                        dts[0].TableName = "HydroUnitID" + hydroTarget.ID.ToString();
                    }
                    else
                    {
                        dts[0].TableName = hydroTarget.Name + "(" + hydroTarget.ID + ")";
                    }
                    if (scenariosActive)
                    {
                        dts[0].TableName = Path.GetFileNameWithoutExtension(DBFullPath).Split(new string[] { "OUTPUT" }, StringSplitOptions.None)[0] + "__" + dts[0].TableName;
                    }
                    return dts;
                }
                catch (Exception ex)
                {
                    ModsimModel.FireOnError("Error generating link output from MDB file." + Environment.NewLine + Environment.NewLine + "Error Message: " + Environment.NewLine + ex.ToString());
                    return null;
                }
            }
            return null;
        }
        /// <summary>Gets a datatable of output values for nodes.</summary>
        /// <param name="node">The node to get output for.</param>
        /// <param name="scenariosActive">Specifies whether or not scenarios analysis is on.</param>
        public DataTable[] NodeOutput(Node node, bool scenariosActive = false)
        {
            InitializeOutput();
            if (statusReady)
            {
                try
                {
                    DataTable[] dts = new DataTable[1];
                    //Add Node output
                    string str = GetNodeTypeQueryTable(node);
                    dts[0] = GetTableFromDB(str, "SummaryNodeOutput");
                    //Table name
                    if (string.IsNullOrEmpty(node.name))
                    {
                        dts[0].TableName = "NodeNo" + node.number.ToString();
                    }
                    else
                    {
                        dts[0].TableName = node.name;
                    }
                    if (scenariosActive)
                    {
                        dts[0].TableName = Path.GetFileNameWithoutExtension(DBFullPath).Split(new string[] { "OUTPUT" }, StringSplitOptions.None)[0] + "__" + dts[0].TableName;
                    }
                    if (node.nodeType == NodeType.Reservoir)
                    {
                        str = "SELECT TimeSteps.TSDate AS [Start Date], TimeSteps.EndDate AS [End Date],";
                        str += GetSELECTVariables("RES_STOROutput") + " FROM NodesInfo INNER JOIN (RES_STOROutput INNER JOIN TimeSteps ON RES_STOROutput.TSIndex = TimeSteps.TSIndex) ON NodesInfo.NNumber = RES_STOROutput.NNo" + " WHERE (((NodesInfo.NNumber)=" + node.number + ") AND ((NodesInfo.NName)='" + node.name + "'))" + " ORDER BY TimeSteps.TSIndex;";
                        //Get datatable
                        Array.Resize(ref dts, 2);
                        dts[1] = GetTableFromDB(str, "SummaryResSTOROutput");

                        //Table name
                        if (node.name == null | string.IsNullOrEmpty(node.name))
                        {
                            dts[1].TableName = "NodeNo" + node.number.ToString() + "_STOR";
                        }
                        else
                        {
                            dts[1].TableName = node.name + "_STOR";
                        }
                        if (scenariosActive)
                        {
                            dts[1].TableName = Path.GetFileNameWithoutExtension(DBFullPath).Split(new string[] { "OUTPUT" }, StringSplitOptions.None)[0] + "__" + dts[1].TableName;
                        }
                    }
                    return dts;
                }
                catch (Exception ex)
                {
                    ModsimModel.FireOnError("Error generating Node Output from MDB file");
                    ModsimModel.FireOnError(ex.Message);
                    return null;
                }
            }
            return null;
        }
        private string GetNodeTypeQueryTable(Node node)
        {
            string rval = null;
            rval = "SELECT TimeSteps.TSDate AS [Start Date], TimeSteps.MidDate AS [Mid Date], TimeSteps.EndDate AS [End Date], ";
            if (node.nodeType == NodeType.Reservoir)
            {
                rval += GetSELECTVariables("RESOutput") + " FROM NodesInfo INNER JOIN (RESOutput INNER JOIN TimeSteps ON RESOutput.TSIndex = TimeSteps.TSIndex) ON NodesInfo.NNumber = RESOutput.NNo";
            }
            else if (node.nodeType == NodeType.Demand | node.nodeType == NodeType.Sink)
            {
                rval += GetSELECTVariables("DEMOutput") + " FROM NodesInfo INNER JOIN (DEMOutput INNER JOIN TimeSteps ON DEMOutput.TSIndex = TimeSteps.TSIndex) ON NodesInfo.NNumber = DEMOutput.NNo";
            }
            else if (node.nodeType == NodeType.NonStorage)
            {
                rval += GetSELECTVariables("NON_STOROutput") + " FROM NodesInfo INNER JOIN (NON_STOROutput INNER JOIN TimeSteps ON NON_STOROutput.TSIndex = TimeSteps.TSIndex) ON NodesInfo.NNumber = NON_STOROutput.NNo";
            }
            rval += " WHERE (((NodesInfo.NNumber)=" + node.number + ") AND ((NodesInfo.NName)='" + node.name + "'))" + " ORDER BY TimeSteps.TSIndex;";
            return rval;
        }
        private string GetSELECTVariables(string tableName, string prefix = "")
        {
            bool first = true;
            string rval = "";
            if (outputVarInfo == null)
            {
                //Handle old output version
                DataTable table = outDS.Tables[tableName];
                //DataRow[] vars = ModsimModel.controlinfo.variableOutputNames.Select("Object = '" + tableName + "'");
                List<string> activeCols = ModsimModel.controlinfo.variableOutputNames.AsEnumerable().Where(x => x["Object"].ToString() == tableName).Select(x => x["OutputName"].ToString()).ToList();
                //List<string> activeCols = from p in ModsimModel.controlinfo.variableOutputNames.AsEnumerable() where p.Field<string>("Object") == tableName select (p.Field<string>("OuputName").ToString()).ToList(); 
                foreach (DataColumn col in table.Columns)
                {
                    if (!ModelOutputMSDB.indices.Contains(col.ColumnName) && activeCols.Contains(col.ColumnName))
                    {
                        if (!first)
                        {
                            rval += ",";
                        }
                        if (!string.IsNullOrEmpty(prefix))
                        {
                            rval += prefix + "(";
                        }
                        rval += tableName + "." + col.ColumnName;
                        if (!string.IsNullOrEmpty(prefix))
                        {
                            rval += ")";
                        }
                        first = false;
                    }
                }
            }
            else
            {
                string sel = "Object = '" + tableName + "'";
                if (tableName == "LinksOutput") sel += " OR Object = 'LinksMeasured'";
                DataRow[] rows = outputVarInfo.Select(sel);
                foreach (DataRow row in rows)
                {
                    if (!first)
                    {
                        rval += ",";
                    }
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        rval += prefix + "(";
                    }
                    rval += Convert.ToString(row["Object"]);// tableName;
                    if (Convert.ToString(row["OutputName"]).LastIndexOf(" ") > 0)
                    {
                        rval += ".[" + row["OutputName"].ToString() + "]";
                    }
                    else
                    {
                        rval += "." + row["OutputName"].ToString();
                    }

                    if (!string.IsNullOrEmpty(prefix))
                    {
                        rval += ")";
                    }
                    if (!(row["Name"] == DBNull.Value))
                    {
                        if (!string.IsNullOrEmpty(row["Name"].ToString()))
                        {
                            rval += " AS [" + row["Name"].ToString() + "]";
                        }
                    }
                    first = false;
                }
            }
            return rval;
        }
        private DataTable GetTableFromDB(string queryString, string m_TableName)
        {
            DataTable rval = null;
            try
            {
                if (OutputControlInfo.ver8MSDBOutputFiles)
                {
                    try
                    {
                    OpenDBConnection();
                    OleDbCommand SelectCmd = null;
                    SelectCmd = new OleDbCommand(queryString, conn);
                    OleDbDataAdapter dAdapt = new OleDbDataAdapter();
                    dAdapt.SelectCommand = SelectCmd;
                    OleDbCommandBuilder cb = new OleDbCommandBuilder(dAdapt);
                    DataSet m_DS = new DataSet();
                    dAdapt.Fill(m_DS, "QueryTable");
                    rval = m_DS.Tables["QueryTable"];
                    rval.TableName = m_TableName;
                    }
                    catch
                    {
                        return null;
                    }
                    finally
                    {
                        CloseDBConnection();
                    }
                }
                if (OutputControlInfo.SQLiteOutputFiles)
                {
                    using (SQLiteHelper m_DBSQLite = new SQLiteHelper(DBFullPath, true))
                    {
                        m_DBSQLite.FireErrorMessage += DatabaseMessagePumping;
                        rval = m_DBSQLite.GetDBTable(queryString, m_TableName);
                        //m_DBSQLite.CommitTransaction(omitCommit:true);
                    }
                }
  
            }
            catch
            {
                return null;
            }
            return rval;
        }
        /// <summary>This function returns false if a combination of number and name on the link doesn't match any link in the output. The output file might be out of date.</summary>
        /// <param name="link">The link to check</param>
        public bool LinkOutputExist(Link link)
        {
            bool rval = false;
            InitializeOutput();
            if (statusReady)
            {
                try
                {
                    string str = "SELECT LinksInfo.LName FROM LinksInfo ";
                    str += " WHERE (((LinksInfo.LName)='" + link.name + "') AND ((LinksInfo.LNumber)=" + link.number + "));";
                    DataTable LinkInfoTable = GetTableFromDB(str, "LinkReturn");
                    if (LinkInfoTable.Rows.Count == 1)
                    {
                        rval = true;
                    }
                    else
                    {
                        rval = false;
                    }
                }
                catch
                {
                    rval = false;
                }
            }
            else
            {
                rval = false;
            }
            return rval;
        }
        /// <summary>This function returns false if a combination of number and name on the node doesn't match any node in the output. The output file might be out of date.</summary>
        /// <param name="node">The node to check</param>
        public bool NodeOutputExist(Node node)
        {
            bool rval = false;
            InitializeOutput();
            if (statusReady)
            {
                try
                {
                    string m_str = "SELECT NodesInfo.NName FROM NodesInfo ";
                    m_str += " WHERE (((NodesInfo.NName)='" + node.name + "') AND ((NodesInfo.NNumber)=" + node.number + "));";
                    DataTable nodeInfoTable = null;
                    nodeInfoTable = GetTableFromDB(m_str, "NodeReturn");
                    if (nodeInfoTable.Rows.Count == 1)
                    {
                        rval = true;
                    }
                    else
                    {
                        rval = false;
                    }
                }
                catch
                {
                    rval = false;
                }
            }
            return rval;
        }
        /// <summary>This function returns false if a combination of number and name on the hydropower unit doesn't match any node in the output. The output file might be out of date.</summary>
        /// <param name="hydroUnit">The hydropower unit to check</param>
        public bool HydroUnitOutputExists(HydropowerUnit hydroUnit)
        {
            InitializeOutput();
            if (statusReady)
            {
                try
                {
                    Csu.Modsim.ModsimModel.HydropowerControllerDataSet.HydroUnitsInfoDataTable dt = this.ModsimModel.hydro.HydroUnitsTable;
                    string tableName = dt.TableName;
                    string NameColName = tableName + "." + dt.HydroUnitNameColumn.ColumnName;
                    string IDColName = tableName + "." + dt.HydroUnitIDColumn.ColumnName;
                    string m_str = "SELECT " + NameColName + " FROM " + tableName + " WHERE (" + NameColName + " = '" + hydroUnit.Name + "'" + " AND " + IDColName + " = " + hydroUnit.ID.ToString() + ");";
                    DataTable queriedDT = GetTableFromDB(m_str, "queriedHydroUnit");
                    if (queriedDT.Rows.Count == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        /// <summary>This function returns false if a combination of number and name on the hydropower target doesn't match any node in the output. The output file might be out of date.</summary>
        /// <param name="hydroTarget">The hydropower target to check</param>
        public bool HydroTargetOutputExists(HydropowerTarget hydroTarget)
        {
            InitializeOutput();
            if (statusReady)
            {
                try
                {
                    Csu.Modsim.ModsimModel.HydropowerControllerDataSet.HydroTargetsInfoDataTable dt = this.ModsimModel.hydro.HydroTargetsTable;
                    string tableName = dt.TableName;
                    string NameColName = tableName + "." + dt.HydroTargetNameColumn.ColumnName;
                    string IDColName = tableName + "." + dt.HydroTargetIDColumn.ColumnName;
                    string str = "SELECT " + NameColName + " FROM " + tableName + " WHERE (" + NameColName + " = '" + hydroTarget.Name + "'" + " AND " + IDColName + " = " + hydroTarget.ID.ToString() + ");";
                    DataTable queriedDT = GetTableFromDB(str, "queriedHydroTarget");
                    if (queriedDT.Rows.Count == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        private List<List<string>> CreateSpecialLinkToNodesList()
        {
            List<List<string>> rval = new List<List<string>>();
            List<string> linksKeyWords = new List<string>();
            List<string> colKeyWords = new List<string>();
            linksKeyWords.Add("_CALIB_SINK");
            colKeyWords.Add("CALIB_TO_SINK");
            //---
            linksKeyWords.Add("_CALIB_SOURCE");
            colKeyWords.Add("CALIB_FROM_SOURCE");
            //---
            linksKeyWords.Add("_CALIB_RES");
            colKeyWords.Add("CALIB_FROM_SOURCE");
            //---
            linksKeyWords.Add("_CALIB_DS_SUPPLY");
            colKeyWords.Add("CALIB_DS_SUPPLY");
            //---
            linksKeyWords.Add("_SIM_BYPASS");
            colKeyWords.Add("BYPASS");
            //---
            linksKeyWords.Add("ANN_RETURN_TO_");
            colKeyWords.Add("ANN_GW_RETURN");
            //---
            linksKeyWords.Add("ANN_DEPLET_FROM_");
            colKeyWords.Add("ANN_GW_DEPLETION");
            //---
            linksKeyWords.Add("SINK_ESC_VALVE_");
            colKeyWords.Add("SINK_ESC_VALVE_");

            rval.Add(linksKeyWords);
            rval.Add(colKeyWords);
            return rval;
        }
        private List<List<string>> CreateSpecialLinkToLinksList()
        {
            List<List<string>> rval = new List<List<string>>();
            List<string> linksKeyWords = new List<string>();
            List<string> colKeyWords = new List<string>();
            linksKeyWords.Add("ZeroFlowLink");
            colKeyWords.Add("BackRoutingError");

            rval.Add(linksKeyWords);
            rval.Add(colKeyWords);
            return rval;
        }
        public void AddSpecialLinks(string objectNameKey, DataTable[] outDataTable, bool scenariosActive = false)
        {
            List<string> linksKeyWords = null;
            List<string> colKeyWords = null;
            List<List<string>> specialLinks = CreateSpecialLinkToNodesList();
            linksKeyWords = specialLinks[0];
            colKeyWords = specialLinks[1];
            //Create columns for all existing keywords
            for (int i = 0; i < linksKeyWords.Count; i++)
            {
                string str = " SELECT * FROM LinksInfo WHERE LName = '" + objectNameKey + linksKeyWords[i].ToString() + "' OR LName = '" + linksKeyWords[i].ToString() + objectNameKey + "' ;";
                DataTable linksInfoTable = GetTableFromDB(str, "LinkReturn");
                DataRow[] linksrows = linksInfoTable.Select();
                if (linksrows.Length > 0)
                {
                    Array.Resize(ref outDataTable, outDataTable.Length + 1);
                    Link link = new Link();
                    link.name = linksrows[0]["LName"].ToString();
                    link.number = Convert.ToInt32(linksrows[0]["LNumber"]);
                    DataTable[] tmpTbls = LinkOutput(link, null, scenariosActive);
                    outDataTable[outDataTable.Length - 1] = tmpTbls[1];
                }
            }
        }
        public long LinkOutputQuery(LinkOutputType outputType, int linkNo, int timeStepIndex)
        {
            InitializeOutput();
            if (statusReady)
            {
                string str = "SELECT LinksOutput.*" + " FROM LinksInfo INNER JOIN LinksOutput ON LinksInfo.LNumber = LinksOutput.LNumber" + " WHERE (((LinksInfo.LNumber)=" + linkNo + ") AND ((LinksOutput.TSIndex)=" + timeStepIndex + "));";
                DataTable LinkInfoTable = GetTableFromDB(str, "LinksQueryOutput");
                DataRow[] rows = LinkInfoTable.Select();
                if (rows.Length != 1)
                {
                    return -1;
                }
                return Convert.ToInt64(rows[0][outputType.ToString()]);
            }
            else
            {
                return -1;
            }
        }
        public long LinkOutputQuery(LinkOutputType outputType, string linkName, int timeStepIndex)
        {
            InitializeOutput();
            if (statusReady)
            {
                string str = "SELECT LinksOutput.*" + " FROM LinksInfo INNER JOIN LinksOutput ON LinksInfo.LNumber = LinksOutput.LNumber" + " WHERE (((LinksInfo.LName)='" + linkName + "') AND ((LinksOutput.TSIndex)=" + timeStepIndex + "));";
                DataTable LinkInfoTable = GetTableFromDB(str, "LinksQueryOutput");
                DataRow[] rows = LinkInfoTable.Select();
                if (rows.Length != 1)
                {
                    return -1;
                }
                return Convert.ToInt32(rows[0][outputType.ToString()]);
            }
            else
            {
                return -1;
            }
        }
        public DataTable LinkOutputTSQuery(LinkOutputType outputType, Link link, double factor = 1, bool isInteger = true)
        {
            InitializeOutput();
            if (statusReady)
            {
                string str = null;
                if (isInteger)
                {
                    str = "SELECT TimeSteps.TSDate, INT([LinksOutput].[" + outputType.ToString() + "]*" + factor + ")";
                }
                else
                {
                    str = "SELECT TimeSteps.TSDate, ([LinksOutput].[" + outputType.ToString() + "]*" + factor + ")";
                }

                str += "FROM (TimeSteps INNER JOIN LinksOutput ON TimeSteps.TSIndex = LinksOutput.TSIndex) INNER JOIN LinksInfo ON LinksOutput.LNumber = LinksInfo.LNumber";
                str += "            WHERE(((LinksInfo.LName) = '" + link.name + "'))";
                str += "ORDER BY TimeSteps.TSIndex;";
                DataTable linkInfoTable = null;
                try
                {
                    linkInfoTable = GetTableFromDB(str, link.name + "_" + outputType.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                return linkInfoTable;
            }
            else
            {
                return null;
            }
        }
        public DataTable NodeOutputTSQuery(NodeColumnsNames colName, Node node, double factor = 1, bool isInteger = true, Link addLink = null, LinkOutputType outputType = LinkOutputType.Flow)
        {
            InitializeOutput();
            string tableName = "";
            if (statusReady)
            {
                string str = null;
                switch (node.nodeType)
                {
                    case NodeType.Demand:
                        tableName = "DEMOutput";
                        break;
                    case NodeType.Reservoir:
                        switch (colName)
                        {
                            case NodeColumnsNames.Elev_End:
                            case NodeColumnsNames.Stor_Beg:
                            case NodeColumnsNames.Stor_End:
                            case NodeColumnsNames.Stor_Trg:
                                tableName = "RES_STOROutput";
                                break;
                            default:
                                tableName = "RESOutput";
                                break;
                        }
                        break;
                    case NodeType.NonStorage:
                        tableName = "NON_STOROutput";
                        break;
                }
                if (isInteger)
                {
                    str = "SELECT TimeSteps.TSDate, INT([" + tableName + "].[" + colName.ToString() + "]*" + factor + ")";
                }
                else
                {
                    str = "SELECT TimeSteps.TSDate, [" + tableName + "].[" + colName.ToString() + "]*" + factor;
                }
                str += "FROM NodesInfo INNER JOIN (TimeSteps INNER JOIN " + tableName + " ON TimeSteps.TSIndex = " + tableName + ".TSIndex) ON NodesInfo.NNumber = " + tableName + ".NNo";
                str += "            WHERE(((NodesInfo.NName) = '" + node.name + "'))";
                str += "ORDER BY TimeSteps.TSIndex;";
                DataTable nodeTSTable = null;
                try
                {
                    nodeTSTable = GetTableFromDB(str, node.name + "_" + colName.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                return nodeTSTable;
            }
            else
            {
                return null;
            }
        }
        public DataTable NodePlusLinkOutputTSQuery(NodeColumnsNames colName, Node node, Link addLink, LinkOutputType outputType, double factor = 1, bool isInteger = true)
        {
            InitializeOutput();
            string tableName = "";
            if (statusReady)
            {
                string str = null;
                switch (node.nodeType)
                {
                    case NodeType.Demand:
                        tableName = "DEMOutput";
                        break;
                    case NodeType.Reservoir:
                        switch (colName)
                        {
                            case NodeColumnsNames.Elev_End:
                            case NodeColumnsNames.Stor_Beg:
                            case NodeColumnsNames.Stor_End:
                            case NodeColumnsNames.Stor_Trg:
                                tableName = "RES_STOROutput";
                                break;
                            default:
                                tableName = "RESOutput";
                                break;
                        }
                        break;
                    case NodeType.NonStorage:
                        tableName = "NON_STOROutput";
                        break;
                }
                if (isInteger)
                {
                    str = "SELECT TimeSteps.TSDate, Int(([" + tableName + "].[" + colName.ToString() + "]+[LinksOutput].[" + outputType.ToString() + "])*" + factor + ") AS [Value]";
                }
                else
                {
                    str = "SELECT TimeSteps.TSDate, (([" + tableName + "].[" + colName.ToString() + "]+[LinksOutput].[" + outputType.ToString() + "])*" + factor + ") AS [Value]";
                }
                str += " FROM (LinksInfo INNER JOIN LinksOutput ON LinksInfo.LNumber = LinksOutput.LNumber) INNER JOIN (NodesInfo INNER JOIN (TimeSteps INNER JOIN " + tableName + " ON TimeSteps.TSIndex = " + tableName + ".TSIndex) ON NodesInfo.NNumber = " + tableName + ".NNo) ON LinksOutput.TSIndex = TimeSteps.TSIndex";
                str += " WHERE(((NodesInfo.NName) = '" + node.name + "') And ((LinksInfo.LName) = '" + addLink.name + "'))";
                str += " ORDER BY TimeSteps.TSIndex;";
                DataTable NodeTSTable = null;
                try
                {
                    NodeTSTable = GetTableFromDB(str, node.name + "_" + colName.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                return NodeTSTable;
            }
            else
            {
                return null;
            }
        }
        public DataTable GetOutputTable(OutputTableType outputType, string whereQueryStr = "")
        {
            InitializeOutput();
            if (statusReady)
            {
                DataTable linkTable = null;
                string str = null;
                string tableName = "";
                switch (outputType)
                {
                    case OutputTableType.Links:
                        tableName = "LinksOutput";
                        break;
                    case OutputTableType.Demands:
                        tableName = "DEMOutput";
                        break;
                    case OutputTableType.ReservoirFlows:
                        tableName = "RESOutput";
                        break;
                    case OutputTableType.ReservoirStorage:
                        tableName = "RES_STOROutput";
                        break;
                    case OutputTableType.NonStorage:
                        tableName = "NON_STOROutput";
                        break;
                    case OutputTableType.NodesInfo:
                        tableName = "NodesInfo";
                        break;
                    case OutputTableType.LinksInfo:
                        tableName = "LinksInfo";
                        break;
                    case OutputTableType.TableInfo:
                        tableName = "OutputTablesInfo";
                        break;
                    case OutputTableType.TimeSteps:
                        tableName = "TimeSteps";
                        break;
                }
                str = "SELECT " + tableName + ".* FROM " + tableName;
                if (!string.IsNullOrEmpty(whereQueryStr))
                {
                    str += " WHERE " + whereQueryStr;
                }
                str += " ;";
                linkTable = GetTableFromDB(str, tableName);
                return linkTable;
            }
            else
            {
                return null;
            }
        }
        public DataRow[] LinksInfoSelect(string fromNodeName, string toNodeName)
        {
            InitializeOutput();
            if (statusReady)
            {
                int fromNodeNo = GetNodeNo(fromNodeName);
                int toNodeNo = GetNodeNo(toNodeName);
                string str = "SELECT LinksInfo.*" + " FROM LinksInfo " + " WHERE (((LinksInfo.FromNode)=" + fromNodeNo + ") AND ((LinksInfo.ToNode)=" + toNodeNo + "));";

                if (toNodeNo > 0 & fromNodeNo > 0)
                {
                    DataTable linksInfoTable = GetTableFromDB(str, "LinksQueryOutput");
                    DataRow[] linksrows = linksInfoTable.Select();
                    return linksrows;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public DataRow NodeInfoSelect(string nodeName)
        {
            InitializeOutput();
            if (statusReady)
            {
                DataTable linksInfoTable = GetOutputTable(OutputTableType.NodesInfo);
                DataRow[] noderows = linksInfoTable.Select("NName = '" + nodeName + "'");
                return (noderows[0]);
            }
            else
            {
                return null;
            }
        }
        private int GetNodeNo(string nodeName)
        {
            InitializeOutput();
            if (statusReady)
            {
                DataTable nodeInfoTable = GetOutputTable(OutputTableType.NodesInfo);
                DataRow[] rows = nodeInfoTable.Select("NName = '" + nodeName + "'");
                if (rows.Length == 1)
                {
                    int nodeNo = Convert.ToInt32(rows[0]["NNumber"]);
                    return nodeNo;
                }
                else
                {
                    MessageBox.Show("Node named " + nodeName + " not found in the output");
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
        private int GetLinkNo(string linkName)
        {
            InitializeOutput();
            if (statusReady)
            {
                DataTable linkInfoTable = GetOutputTable(OutputTableType.LinksInfo);
                DataRow[] rows = linkInfoTable.Select("LName = '" + linkName + "'");
                if (rows.Length == 1)
                {
                    int linkNo = Convert.ToInt32(rows[0]["LNumber"]);
                    return linkNo;
                }
                else
                {
                    MessageBox.Show("Link named" + linkName + " not found in the output");
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }
        //public void AppendOutput(string sourceOutput, bool silent = false)
        //{
        //    DBUtil DB = null;
        //    //Append Time Step Table
        //    try
        //    {
        //        if (OnMODSIMOutputMessage != null)
        //        {
        //            OnMODSIMOutputMessage("Loading New Time Steps");
        //        }
        //        OpenDBConnection();
        //        OleDbCommand SelectCmd = null;
        //        SelectCmd = new OleDbCommand("SELECT TimeSteps.* FROM(TimeSteps)ORDER BY TimeSteps.TSDate; ", conn);
        //        OleDbDataAdapter dAdapt = new OleDbDataAdapter();
        //        dAdapt.SelectCommand = SelectCmd;
        //        OleDbCommandBuilder cb = new OleDbCommandBuilder(dAdapt);
        //        DataSet m_DS = new DataSet();
        //        dAdapt.Fill(m_DS, "QueryTable");
        //        DataTable timeStepTbl = m_DS.Tables["QueryTable"];

        //        // Get a datatable
        //        DB = new DBUtil(sourceOutput);
        //        DB.FireMessage += DatabaseMessagePumping;
        //        DB.Open();
        //        DataTable newTimeSteps = DB.GetTable("SELECT * FROM TimeSteps", "NewTimeSteps");
        //        DB.Close();

        //        int tsCounter = Convert.ToInt32(timeStepTbl.Rows[timeStepTbl.Rows.Count - 1]["TSIndex"]);
        //        System.DateTime TSLastDate = Convert.ToDateTime(timeStepTbl.Rows[timeStepTbl.Rows.Count - 1]["TSDate"]);
        //        for (int i = 0; i < newTimeSteps.Rows.Count; i++)
        //        {
        //            tsCounter += 1;
        //            if (Convert.ToDateTime(newTimeSteps.Rows[i]["TSDate"]) > TSLastDate)
        //            {
        //                int col = 0;
        //                DataRow newrow = timeStepTbl.NewRow();
        //                for (col = 0; col < newTimeSteps.Columns.Count; col++)
        //                {
        //                    string colName = newTimeSteps.Columns[col].ColumnName;
        //                    if (colName == "TSIndex")
        //                    {
        //                        newrow[colName] = tsCounter;
        //                    }
        //                    else
        //                    {
        //                        newrow[colName] = newTimeSteps.Rows[i][colName];
        //                    }
        //                }
        //                timeStepTbl.Rows.Add(newrow);
        //            }
        //            else
        //            {
        //                MessageBox.Show("Dates to append contain previous dates to the last date in the base Output");
        //                return;
        //            }
        //        }
        //        dAdapt.Update(timeStepTbl);
        //        CloseDBConnection();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error Appending Time Steps table" + Environment.NewLine + ex.Message);
        //    }
        //    //Get Current Output DB
        //    DB = new DBUtil(DBFullPath);
        //    DB.FireMessage += DatabaseMessagePumping;
        //    DB.Open();

        //    //Append the Links Output
        //    if (OnMODSIMOutputMessage != null)
        //    {
        //        OnMODSIMOutputMessage("Appending Links Output ...");
        //    }
        //    string query = "INSERT INTO LinksOutput ( LNumber, TSIndex, " + GetColumsListFromOutput(DB, "LinksOutput") + " )";
        //    query += " SELECT LinksOutput1.LNumber, TimeSteps.TSIndex, " + GetColumsListFromOutput(DB, "LinksOutput", "LinksOutput1.");
        //    query += " FROM (SELECT * FROM LinksOutput IN '" + sourceOutput + "') AS LinksOutput1 ";
        //    query += " INNER JOIN (TimeSteps INNER JOIN ";
        //    query += "  (SELECT * FROM TimeSteps IN '" + sourceOutput + "') AS";
        //    query += " TimeSteps1 ON TimeSteps.TSDate = TimeSteps1.TSDate) ON LinksOutput1.TSIndex = TimeSteps1.TSIndex;";
        //    DB.ExecuteNonQuery(query);
        //    //Append the Demands Output
        //    AppendNodeOutputTable(DB, "DEMOutput", sourceOutput, silent);
        //    //Append the Reservoir Storage Output
        //    AppendNodeOutputTable(DB, "RES_STOROutput", sourceOutput, silent);
        //    //Append the Reservoir Output
        //    AppendNodeOutputTable(DB, "RESOutput", sourceOutput, silent);
        //    //Append the Non_storage Output
        //    AppendNodeOutputTable(DB, "NON_STOROutput", sourceOutput, silent);
        //    DB.Close();
        //}
        //private void AppendNodeOutputTable(DBUtil DB, string tablename, string sourceoutput, bool silent)
        //{
        //    try
        //    {
        //        if (OnMODSIMOutputMessage != null)
        //        {
        //            OnMODSIMOutputMessage("Appending table: " + tablename + " ...");
        //        }
        //        string query = "INSERT INTO " + tablename + " ( NNo, TSIndex, " + GetColumsListFromOutput(DB, tablename) + " )";
        //        query += " SELECT NodeOutput1.NNo, TimeSteps.TSIndex, " + GetColumsListFromOutput(DB, tablename, "NodeOutput1.");
        //        query += " FROM (SELECT * FROM " + tablename + " IN '" + sourceoutput + "') AS NodeOutput1 ";
        //        query += " INNER JOIN (TimeSteps INNER JOIN ";
        //        query += "  (SELECT * FROM TimeSteps IN '" + sourceoutput + "') AS";
        //        query += " TimeSteps1 ON TimeSteps.TSDate = TimeSteps1.TSDate) ON NodeOutput1.TSIndex = TimeSteps1.TSIndex;";
        //        int m_return = DB.ExecuteNonQuery(query);
        //        if (m_return == 0)
        //        {
        //            if (!silent)
        //            {
        //                MessageBox.Show("Appending table:" + tablename + " returned 0 rows added.");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Appending table:" + tablename + " failed." + Environment.NewLine + ex.Message);
        //    }
        //}
        //private string GetColumsListFromOutput(DBUtil DB, string tablename, string prefix = "")
        //{
        //    string rval = "";

        //    string query = "SELECT OutputTablesInfo.Object, OutputTablesInfo.OutputName";
        //    query += " FROM(OutputTablesInfo)";
        //    query += " WHERE (((OutputTablesInfo.Object)='" + tablename + "'));";

        //    DataTable m_columns = DB.GetTable(query, "ColNames");
        //    for (int i = 0; i < m_columns.Rows.Count; i++)
        //    {
        //        if (i > 0)
        //        {
        //            rval += ", ";
        //        }
        //        if (prefix != null)
        //        {
        //            rval += prefix;
        //        }
        //        rval += m_columns.Rows[i]["OutputName"].ToString();
        //    }
        //    return rval;
        //}
        private void DatabaseMessagePumping(string message)
        {
            if (OnMODSIMOutputMessage != null)
            {
                OnMODSIMOutputMessage(message);
            }
        }
    }
}
