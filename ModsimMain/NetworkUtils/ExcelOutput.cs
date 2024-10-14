using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using Csu.Modsim.ModsimModel;

namespace Csu.Modsim.NetworkUtils
{
    /// <summary>Generates an Excel database of output found in the xy_file_nameOUTPUT.mdb file.</summary>
    /// <remarks>If only a subset of data is desired to be exported, pass an instance of the ExcelOutputInfo class when instantiating this class.</remarks>
    public class ExcelOutput
    {
        private const int maxColumns = 250;
        private object misValue = System.Reflection.Missing.Value;
        private string xlPath;
        private string mdbPath;
        private ExcelOutputInclude include;
        private string msg = "";

        private int accuracy = 0;
        // Events
        /// <summary>Event that fires (nonerror) messages.</summary>
        /// <param name="Message">The message to fire.</param>
        public event FireMessageEventHandler FireMessage;
        public delegate void FireMessageEventHandler(string Message);
        /// <summary>Event that fires error messages.</summary>
        /// <param name="Message">The message to fire.</param>
        public event FireErrorMessageEventHandler FireErrorMessage;
        public delegate void FireErrorMessageEventHandler(string Message);

        // Properties
        /// <summary>Gets the previously fired message.</summary>
        /// <value>String value describing the previously fired message.</value>
        /// <returns>Returns the previously fired message.</returns>
        public string Message
        {
            get { return msg; }
        }

        // Constructor
        /// <summary>Constructs a new instance of ExcelOutput</summary>
        /// <param name="MDBOutputPath">The Access Database (*.mdb) file (previous output from MODSIM)</param>
        /// <param name="XLOutputPath">The desired Excel output file. </param>
        /// <param name="OutputToInclude">A class containing IO information to include or exclude when reading and writing.</param>
        public ExcelOutput(string MDBOutputPath, string XLOutputPath, int DecimalAccuracy, ExcelOutputInclude OutputToInclude = null)
        {
            if (!File.Exists(MDBOutputPath))
                throw new Exception("Cannot export results until results are successfully saved in output MDB file.");
            mdbPath = MDBOutputPath;
            xlPath = Path.GetFullPath(XLOutputPath);
            accuracy = DecimalAccuracy;
            if (OutputToInclude == null)
            {
                include = new ExcelOutputInclude();
                include.Import(Path.GetDirectoryName(xlPath) + "\\" + Path.GetFileNameWithoutExtension(xlPath) + ".txt");
            }
            else
            {
                include = OutputToInclude;
            }
        }

        // Messages 
        /// <summary>Pumps messages.</summary>
        /// <param name="Message">The message to pump.</param>
        public void MessagePumper(string Message)
        {
            if (FireMessage != null)
            {
                FireMessage(Message);
            }
        }
        /// <summary>Pumps error messages.</summary>
        /// <param name="Message">The message to pump.</param>
        public void ErrorMessagePumper(string Message)
        {
            if (FireErrorMessage != null)
            {
                FireErrorMessage(Message);
            }
        }

        // Create an excel output file
        /// <summary>Retrieves output from a previous MODSIM run (in an .mdb file), and summarizes the output in an Excel file.</summary>
        public void Write()
        {
            DataSet ds = ReadMDBOutput();
            WriteXLOutput(ds);
        }
        /// <summary>Write a DataSet to Excel.</summary>
        /// <param name="ds">The DataSet to write.</param>
        public void WriteXLOutput(DataSet ds)
        {
            try
            {
                if (ds == null)
                {
                    msg = "Can't write Excel output with an empty DataSet.";
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                    return;
                }

                msg = Environment.NewLine + "Writing output to " + Path.GetFileName(xlPath);
                if (FireMessage != null)
                {
                    FireMessage(msg);
                }

                DBUtil dbXL = new DBUtil(xlPath, true, ",", true);
                dbXL.FireMessage += MessagePumper;
                dbXL.FireErrorMessage += ErrorMessagePumper;

                if (File.Exists(xlPath))
                    File.Delete(xlPath);
                dbXL.Open();
                if (dbXL.ConnectionFailed)
                {
                    msg = "Connection to " + xlPath + " failed.";
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                    dbXL.Close();
                    return;
                }

                dbXL.ImportDataSet(ds, true, accuracy);
                dbXL.Close();
            }
            catch (Exception ex)
            {
                msg = "Error writing output to Excel." + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
            }
        }

        // Retrieving data from MODSIM output in .mdb file
        /// <summary>Retrieves previously run MODSIM output from a .mdb file.</summary>
        /// <returns>Returns a dataset containing all the MODSIM output tables in the .mdb file.</returns>
        public DataSet ReadMDBOutput()
        {
            try
            {
                msg = "Reading previous MODSIM output from " + Path.GetFileName(mdbPath);
                if (FireMessage != null)
                {
                    FireMessage(msg);
                }

                // Open the mdb database 
                DBUtil dbMDB = new DBUtil(mdbPath);
                dbMDB.FireMessage += MessagePumper;
                dbMDB.FireErrorMessage += ErrorMessagePumper;
                dbMDB.Open();
                if (dbMDB.ConnectionFailed)
                {
                    msg = "Connection to " + Path.GetFileName(mdbPath) + " failed.";
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                    return null;
                }

                // Retrieve the entire Access database.
                DataSet inDS = dbMDB.GetTables();
                if (inDS == null)
                {
                    msg = "Error retrieving tables from " + Path.GetFileName(mdbPath);
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                    return null;
                }

                // Get Timesteps, NodesInfo, LinksInfo and OutputTablesInfo
                DataSet outDS = null;
                DataTable TimestepDT = inDS.Tables["Timesteps"];
                DataTable NodesInfo = inDS.Tables["NodesInfo"];
                DataTable LinksInfo = inDS.Tables["LinksInfo"];
                DataTable HydroUnitsInfo = null;
                if (inDS.Tables.Contains("HydroUnitsInfo"))
                    HydroUnitsInfo = inDS.Tables["HydroUnitsInfo"];
                DataTable HydroTargetsInfo = null;
                if (inDS.Tables.Contains("HydroTargetsInfo"))
                    HydroTargetsInfo = inDS.Tables["HydroTargetsInfo"];
                DataTable OutputTablesInfo = inDS.Tables["OutputTablesInfo"];
                DataTable indexDT = null;
                DataTable outDT = null;
                DataTable tempDT = null;
                DataRow[] allRows = null;
                int tableInfoRow = 0;
                int objTypeCtr = 0;
                int tableNum = 0;
                int tableCount = 0;
                int indexDTRow = 0;
                string InTableName = null;
                string InColName = null;
                string OutColName = null;
                string OutTableName = null;
                string objNumColumn = null;
                string objNumColumnInfo = null;
                string objNameColumnInfo = null;
                string[] ObjectType = null;
                string PrevTableName = "";
                if ((TimestepDT != null) & (NodesInfo != null) & (LinksInfo != null) & (OutputTablesInfo != null))
                {
                    // Make sure tables are sorted.
                    TimestepDT.DefaultView.Sort = "TSIndex ASC";
                    NodesInfo.DefaultView.Sort = "NNumber ASC";
                    LinksInfo.DefaultView.Sort = "LNumber ASC";
                    OutputTablesInfo.DefaultView.Sort = "Object ASC";

                    // Loop through the info table to get all the column names from which to extract data.
                    for (tableInfoRow = 0; tableInfoRow < OutputTablesInfo.Rows.Count; tableInfoRow++)
                    {
                        bool skip = false;

                        // Make sure the input table exists and that it is output
                        InTableName = OutputTablesInfo.Rows[tableInfoRow]["Object"].ToString();
                        InColName = OutputTablesInfo.Rows[tableInfoRow]["OutputName"].ToString();
                        if (!string.IsNullOrEmpty(InTableName) && InTableName.EndsWith("Output") && (inDS.Tables[InTableName] != null))
                        {
                            // Determine which type of data is being used for extraction 
                            if (InTableName.StartsWith("Links"))
                            {
                                if (!include.Links)
                                    skip = true;
                                indexDT = LinksInfo;
                                objNumColumn = "LNumber";
                                objNumColumnInfo = "LNumber";
                                objNameColumnInfo = "LName";
                                ObjectType = new string[1] { "Link" };
                            }
                            else if (InTableName.StartsWith("HydroUnit"))
                            {
                                if (HydroUnitsInfo == null || !include.HydroUnits)
                                    skip = true;
                                indexDT = HydroUnitsInfo;
                                objNumColumn = "HydroUnitID";
                                objNumColumnInfo = "HydroUnitID";
                                objNameColumnInfo = "HydroUnitName";
                                ObjectType = new string[1] { "HydroUnit" };
                            }
                            else if (InTableName.StartsWith("HydroTarget"))
                            {
                                if (HydroTargetsInfo == null || !include.HydroTargets)
                                    skip = true;
                                indexDT = HydroTargetsInfo;
                                objNumColumn = "HydroTargetID";
                                objNumColumnInfo = "HydroTargetID";
                                objNameColumnInfo = "HydroTargetName";
                                ObjectType = new string[1] { "HydroTarget" };
                            }
                            else
                            {
                                indexDT = NodesInfo;
                                objNumColumn = "NNo";
                                objNumColumnInfo = "NNumber";
                                objNameColumnInfo = "NName";
                                if (InTableName.StartsWith("DEM"))
                                {
                                    if (!include.Demands)
                                        skip = true;
                                    ObjectType = new string[2] {
                                        NodeType.Demand.ToString(),
                                        NodeType.Sink.ToString()
                                    };
                                }
                                else if (InTableName.StartsWith("RES"))
                                {
                                    if (!include.Reservoirs)
                                        skip = true;
                                    ObjectType = new string[1] { NodeType.Reservoir.ToString() };
                                }
                                else if (InTableName.StartsWith("NON_STOR"))
                                {
                                    if (!include.NonstorageNodes)
                                        skip = true;
                                    ObjectType = new string[1] { NodeType.NonStorage.ToString() };
                                }
                                else
                                {
                                    throw new Exception("Unrecognized output in OutputTablesInfo table: " + InTableName);
                                }
                            }

                            // Skip the table if the user wants to.
                            if (skip)
                            {
                                if (PrevTableName != InTableName)
                                {
                                    msg = "Skipping table " + InTableName;
                                    if (FireMessage != null)
                                    {
                                        FireMessage(msg);
                                    }
                                    PrevTableName = InTableName;
                                }
                            }
                            else
                            {
                                // Inform the user when a new table is being worked on. 
                                if (PrevTableName != InTableName)
                                {
                                    msg = "Reading table " + InTableName;
                                    if (FireMessage != null)
                                    {
                                        FireMessage(msg);
                                    }
                                    PrevTableName = InTableName;
                                }

                                // Get the input data (store in a temporary DataTable)
                                tempDT = inDS.Tables[InTableName];
                                tempDT.DefaultView.Sort = objNumColumn + ", TSIndex";
                                tempDT = tempDT.DefaultView.ToTable();

                                // Loop through each object type (Link, Demand, Sink, Reservoir, or NonStorage)
                                for (objTypeCtr = 0; objTypeCtr < ObjectType.Length; objTypeCtr++)
                                {
                                    // When changing the object type, restart the tempDT row counter 
                                    int tempDTRow = 0;

                                    // Select the rows from the table to update
                                    if (InTableName.StartsWith("Links") || InTableName.StartsWith("Hydro"))
                                    {
                                        allRows = indexDT.Select();
                                    }
                                    else
                                    {
                                        allRows = indexDT.Select("NType = '" + ObjectType[objTypeCtr] + "'");
                                    }

                                    // Determine how many rows from the info table (indexDT) to read... Must be less than maxColumns
                                    tableCount = Convert.ToInt32(Math.Ceiling((double)allRows.Length / maxColumns));

                                    for (tableNum = 0; tableNum < tableCount; tableNum++)
                                    {
                                        // Define the output table name
                                        OutTableName = ObjectType[objTypeCtr] + "_" + InColName;
                                        if (tableNum > 0)
                                            OutTableName += "_" + tableNum.ToString();

                                        // Get StartDate and EndDates to start
                                        outDT = new DataTable(OutTableName);
                                        AddColumnToDT(outDT, TimestepDT, "StartDate", "TSDate");
                                        AddColumnToDT(outDT, TimestepDT, "EndDate", "EndDate");

                                        // Loop through all the rows in the info table querying each node/link number
                                        for (indexDTRow = tableNum * maxColumns; indexDTRow <= Math.Min(allRows.Length - tableNum * maxColumns, maxColumns) + tableNum * maxColumns - 1; indexDTRow++)
                                        {
                                            // Get the output column name.
                                            OutColName = allRows[indexDTRow][objNameColumnInfo].ToString();
                                            //.Replace(" "c, "_"c).Replace("-"c, "_"c).Replace("&"c, "_"c).Replace("|"c, "_"c).Replace("/"c, "_"c).Replace("\"c, "_"c).Replace("."c, "_"c).Replace(","c, "_"c).Replace(">"c","_"c)
                                            OutColName = Regex.Replace(OutColName, "[^0-9a-zA-Z$_]+", "_");
                                            //If DBUtil.ReservedWords.Contains(OutColName.ToUpper()) Then OutColName = OutColName & "_" & objNameColumnInfo
                                            //If IsNumeric(OutColName(0)) OrElse OutColName(0) = "_"c Then OutColName = "O" & OutColName
                                            if (Util.IsNumeric(OutColName))
                                                OutColName = "O" + OutColName;
                                            if (OutColName.Length >= 61)
                                                OutColName = OutColName.Substring(0, 60);
                                            if (string.IsNullOrEmpty(OutColName))
                                                OutColName = objNumColumnInfo + "_" + allRows[indexDTRow][objNumColumnInfo].ToString();

                                            // tempDT needs to be sorted before entering this function...
                                            if (tempDTRow != -1)
                                                tempDTRow = AddColumnToDT(outDT, tempDT, OutColName, InColName, objNumColumn, Convert.ToInt32(allRows[indexDTRow][objNumColumnInfo]), tempDTRow);
                                        }

                                        // Add the output table to the output DataSet
                                        if (outDS == null)
                                            outDS = new DataSet();
                                        outDS.Tables.Add(outDT);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    msg = "One of the required info tables is missing from the access database " + Path.GetFileName(mdbPath);
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                }

                dbMDB.Close();
                return outDS;
            }
            catch (Exception ex)
            {
                msg = "Error reading previous MODSIM output in " + mdbPath + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return null;

            }
        }

        // Helper Methods
        /// <summary>Copies a column from one table to the next, and allows the another column to be used as a reference (only records with a certain field value will be copied)</summary>
        /// <param name="DestinationTable">The DataTable to which a column is being added.</param>
        /// <param name="SourceTable">The DataTable from which the column is being extracted.</param>
        /// <param name="NewColumnName">The name of the column in the destination table to which the data will be copied.</param>
        /// <param name="SourceColumnName">The name of the column in the source table that from which the data comes.</param>
        /// <param name="WhereColumnName">The name of the column by which to group output values. Treated the column transfer like a query with this statement (retrieving records from SourceTable): WHERE WhereColumnName = WhereValue... So the table will need to be previously grouped by the column named WhereColumnName (Important !!!)</param>
        /// <param name="WhereValue">The value within the column named WhereColumnName by which filtering is performed.</param>
        /// <param name="StartIndex">RowIndex is returned -1 when this method terminates.</param>
        private int AddColumnToDT(DataTable DestinationTable, DataTable SourceTable, string NewColumnName, string SourceColumnName, string WhereColumnName = "", int WhereValue = 0, int StartIndex = 0)
        {
            // Create new table if DestinationTable is Nothing
            bool createNewRows = false;
            if (DestinationTable.Rows.Count == 0)
            {
                createNewRows = true;
            }

            // Can't copy data if SourceTable is Nothing
            if (SourceTable == null)
            {
                msg = "Source table is empty, and therefore cannot be copied to DestinationTable.";
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return -1;
            }

            // Get the column numbers for faster indexing
            int InColNum = SourceTable.Columns.IndexOf(SourceColumnName);
            string baseName = NewColumnName;
            int i = 0;
            while (DestinationTable.Columns.Contains(NewColumnName))
            {
                i += 1;
                NewColumnName = baseName + "_" + i.ToString();
            }
            DestinationTable.Columns.Add(NewColumnName, SourceTable.Columns[InColNum].DataType);
            int OutColNum = DestinationTable.Columns.IndexOf(NewColumnName);
            DataRow row = null;
            int DestinationRowCtr = -1;
            for (int SourceRowCtr = StartIndex; SourceRowCtr < SourceTable.Rows.Count; SourceRowCtr++)
            {
                if (string.IsNullOrEmpty(WhereColumnName) || Convert.ToInt32(SourceTable.Rows[SourceRowCtr][WhereColumnName]) == WhereValue)
                {
                    DestinationRowCtr += 1;
                    if (createNewRows || DestinationRowCtr > DestinationTable.Rows.Count - 1)
                        row = DestinationTable.NewRow();
                    else
                        row = DestinationTable.Rows[DestinationRowCtr];
                    row[OutColNum] = SourceTable.Rows[SourceRowCtr][InColNum];
                    if (createNewRows)
                        DestinationTable.Rows.Add(row);
                }
                else if (DestinationRowCtr > -1)
                {
                    StartIndex = SourceRowCtr;
                    return StartIndex;
                }
            }
            StartIndex = -1;
            return StartIndex;
        }

        // Slow, slow, slow, slow... don't use! 
        private DataSet ReadMDBOutput_Old()
        {
            try
            {
                msg = "Reading previous MODSIM output from " + Path.GetFileName(mdbPath);
                if (FireMessage != null)
                {
                    FireMessage(msg);
                }

                // Open the mdb database 
                DBUtil dbMDB = new DBUtil(mdbPath);
                dbMDB.FireMessage += MessagePumper;
                dbMDB.FireErrorMessage += ErrorMessagePumper;
                dbMDB.Open();
                if (dbMDB.ConnectionFailed)
                {
                    msg = "Connection to " + mdbPath + " failed.";
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                    return null;
                }

                // Get Timesteps, NodesInfo, LinksInfo and OutputTablesInfo
                DataSet outDS = null;
                DataTable TimestepDT = dbMDB.GetTable("SELECT * FROM Timesteps", "Timesteps");
                DataTable NodesInfo = dbMDB.GetTable("SELECT * FROM NodesInfo", "NodesInfo");
                DataTable LinksInfo = dbMDB.GetTable("SELECT * FROM LinksInfo", "LinksInfo");
                DataTable OutputTablesInfo = dbMDB.GetTable("SELECT * FROM OutputTablesInfo", "OutputTablesInfo");
                DataTable indexDT = null;
                DataTable outDT = null;
                DataTable tempDT = null;
                DataRow[] allRows = null;
                int tableInfoRow = 0;
                int objTypeCtr = 0;
                int tableNum = 0;
                int tableCount = 0;
                int indexDTRow = 0;
                int InColNum = 0;
                int OutColNum = 0;
                string InTableName = null;
                string InColName = null;
                string OutColName = null;
                string OutTableName = null;
                string objNumColumn = null;
                string objNumColumnInfo = null;
                string objNameColumnInfo = null;
                string[] ObjectType = null;
                string PrevTableName = "";
                if ((TimestepDT != null) & (NodesInfo != null) & (LinksInfo != null) & (OutputTablesInfo != null))
                {
                    // Loop through the info table to get all the column names from which to extract data.
                    for (tableInfoRow = 0; tableInfoRow < OutputTablesInfo.Rows.Count; tableInfoRow++)
                    {
                        bool skip = false;

                        // Make sure the input table exists and that it is output
                        InTableName = OutputTablesInfo.Rows[tableInfoRow]["Object"].ToString();
                        InColName = OutputTablesInfo.Rows[tableInfoRow]["OutputName"].ToString();
                        if (!string.IsNullOrEmpty(InTableName) && InTableName.EndsWith("Output") && dbMDB.TableExists(InTableName))
                        {
                            // Inform the user when a new table is being worked on. 
                            if (PrevTableName != InTableName)
                            {
                                msg = "Reading table " + InTableName;
                                if (FireMessage != null)
                                {
                                    FireMessage(msg);
                                }
                                PrevTableName = InTableName;
                            }

                            // Determine which type of data is being used for extraction 
                            if (InTableName.StartsWith("Links"))
                            {
                                if (!include.Links)
                                    skip = true;
                                indexDT = LinksInfo;
                                objNumColumn = "LNumber";
                                objNumColumnInfo = "LNumber";
                                objNameColumnInfo = "LName";
                                ObjectType = new string[1] { "Link" };
                            }
                            else
                            {
                                indexDT = NodesInfo;
                                objNumColumn = "NNo";
                                objNumColumnInfo = "NNumber";
                                objNameColumnInfo = "NName";
                                if (InTableName.StartsWith("DEM"))
                                {
                                    if (!include.Demands)
                                        skip = true;
                                    ObjectType = new string[2] {
                                        NodeType.Demand.ToString(),
                                        NodeType.Sink.ToString()
                                    };
                                }
                                else if (InTableName.StartsWith("RES"))
                                {
                                    if (!include.Reservoirs)
                                        skip = true;
                                    ObjectType = new string[1] { NodeType.Reservoir.ToString() };
                                }
                                else if (InTableName.StartsWith("NON_STOR"))
                                {
                                    if (!include.NonstorageNodes)
                                        skip = true;
                                    ObjectType = new string[1] { NodeType.NonStorage.ToString() };
                                }
                                else
                                {
                                    throw new Exception("Unrecognized output in OutputTablesInfo table.");
                                }
                            }

                            // Loop through each node type within each indexDT
                            if (!skip)
                            {

                                for (objTypeCtr = 0; objTypeCtr < ObjectType.Length; objTypeCtr++)
                                {
                                    // Select the rows from the table to update
                                    if (InTableName.StartsWith("Links"))
                                    {
                                        allRows = indexDT.Select();
                                    }
                                    else
                                    {
                                        allRows = indexDT.Select("NType = '" + ObjectType[objTypeCtr] + "'");
                                    }

                                    // Determine how many rows from the info table (indexDT) to read... Must be less than maxColumns
                                    tableCount = Convert.ToInt32(Math.Ceiling((double)allRows.Length / maxColumns));

                                    for (tableNum = 0; tableNum < tableCount; tableNum++)
                                    {
                                        // Define the output table name
                                        OutTableName = ObjectType[objTypeCtr] + "_" + InColName;
                                        if (tableNum > 0)
                                            OutTableName += "_" + tableNum.ToString();

                                        // Get StartDate and EndDates to start
                                        outDT = dbMDB.GetTable("SELECT TSDate, EndDate FROM Timesteps", OutTableName);

                                        // Loop through all the rows in the info table querying each node/link number
                                        for (indexDTRow = tableNum * maxColumns; indexDTRow <= Math.Min(allRows.Length - tableNum * maxColumns, maxColumns) + tableNum * maxColumns - 1; indexDTRow++)
                                        {
                                            // Get the output column name.
                                            OutColName = allRows[indexDTRow][objNameColumnInfo].ToString().Replace(' ', '_').Replace('-', '_');
                                            if (string.IsNullOrEmpty(OutColName))
                                                OutColName = objNumColumnInfo + "_" + allRows[indexDTRow][objNumColumnInfo].ToString();

                                            // Get the input data (store in a temporary DataTable)
                                            tempDT = dbMDB.GetTable("SELECT TSIndex, " + InColName + " AS " + OutColName + " FROM " + InTableName + " WHERE " + objNumColumn + " = " + allRows[indexDTRow][objNumColumnInfo].ToString() + " ORDER BY TSIndex ASC", OutTableName);
                                            if (tempDT == null)
                                            {
                                                msg = "Table " + InTableName + " could not be read." + Environment.NewLine + dbMDB.Message;
                                                if (FireErrorMessage != null)
                                                {
                                                    FireErrorMessage(msg);
                                                }
                                            }
                                            else if (outDT.Rows.Count == tempDT.Rows.Count)
                                            {
                                                InColNum = tempDT.Columns.IndexOf(OutColName);
                                                outDT.Columns.Add(tempDT.Columns[InColNum].ColumnName, tempDT.Columns[InColNum].DataType);
                                                OutColNum = outDT.Columns.IndexOf(OutColName);
                                                for (int i = 0; i < outDT.Rows.Count; i++)
                                                {
                                                    outDT.Rows[i][OutColNum] = tempDT.Rows[i][InColNum];
                                                }
                                            }
                                            else
                                            {
                                                msg = "Table " + InTableName + " does not contain the same amount of timesteps as the Timesteps table." + Environment.NewLine + dbMDB.Message;
                                                if (FireErrorMessage != null)
                                                {
                                                    FireErrorMessage(msg);
                                                }
                                            }
                                        }

                                        // Add the output table to the output DataSet
                                        if (outDS == null)
                                            outDS = new DataSet();
                                        outDS.Tables.Add(outDT);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    msg = "One of the required info tables is missing from the access database " + mdbPath;
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                }

                dbMDB.Close();
                return outDS;
            }
            catch (Exception ex)
            {
                msg = "Error reading previous MODSIM output in " + mdbPath + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return null;
            }
        }
    }
}
namespace Csu.Modsim.NetworkUtils
{

    /// <summary>Provides ExcelOutput with output information.</summary>
    /// <remarks>This is a small class containing boolean values to determine which output options should be exported to Excel.</remarks>
    public class ExcelOutputInclude
    {
        public bool Links = true;
        public bool Demands = true;
        public bool Reservoirs = true;
        public bool NonstorageNodes = true;
        public bool HydroUnits = true;
        public bool HydroTargets = true;
        public void Import(string FilePath)
        {
            if (File.Exists(FilePath))
            {
                Links = false;
                Demands = false;
                Reservoirs = false;
                NonstorageNodes = false;
                HydroUnits = false;
                HydroTargets = false;
                try
                {
                    StreamReader sr = new StreamReader(FilePath);
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine().Trim().ToLower();
                        if (line.StartsWith("link"))
                            Links = true;
                        if (line.StartsWith("demand"))
                            Demands = true;
                        if (line.StartsWith("reservoir"))
                            Reservoirs = true;
                        if (line.StartsWith("nonstorage"))
                            NonstorageNodes = true;
                        if (line.StartsWith("hydrounits"))
                            NonstorageNodes = true;
                        if (line.StartsWith("hydrodemands"))
                            NonstorageNodes = true;
                    }
                    sr.Close();
                }
                catch
                {
                }
            }
        }
    }
}
