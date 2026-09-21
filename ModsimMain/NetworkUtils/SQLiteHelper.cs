using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Data;
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

namespace Csu.Modsim.NetworkUtils
{
    public class SQLiteHelper : IDisposable
    {
        public string dbFile;
        private string ConnectionString { get; set; }
        //public MyDBSqlite _mDB;
        private SqliteConnection _sqlconnection { get; set; }
        private SqliteTransaction _sqltransaction { get; set; }
        public delegate void FireErrorMessageEventHandler(string Message);
        public event FireErrorMessageEventHandler FireErrorMessage;

        //public SQLiteHelper()
        //{
        //}
        public SQLiteHelper(string dbfilePath, bool journalOFF = true)
        {
            dbFile = dbfilePath; // Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), dbfile);
            ConnectionString = GetSqLiteConnectionString(dbFile);
            //_mDB = new MyDBSqlite(dbFile);
            if (journalOFF) ConnectionString += ";Journal Mode=Off";
        }

        private string GetSqLiteConnectionString(string dbFileName)
        {
            SqliteConnectionStringBuilder conn = new SqliteConnectionStringBuilder
            {
                DataSource = dbFileName,
                Mode=SqliteOpenMode.ReadWrite
            };
            conn.Add("Compress", true);

            return conn.ConnectionString;
        }

        public bool TestDatabaseStatus()
        {
            try
            {
                if (_sqlconnection == null || _sqlconnection.State != ConnectionState.Open)
                {
                    CheckDatabaseConnection();
                    CommitTransaction(omitCommit:true);
                }
            }
            catch (Exception ex)
            {
                FireErrorMessage("ERROR [DATABASE]: " + ex.Message);
                return false;
            }
            return true;
        }

        public void CreateDatabaseFile()
        {
            if (!File.Exists(dbFile))
            {
                using (var connection = new SqliteConnection($"Data Source={dbFile}"))
                {
                    connection.Open();
                    FireErrorMessage("Database file "+dbFile+" created.");
                }
            }
        }
        
        private void CheckDatabaseConnection()
        {
            if (_sqlconnection == null || _sqlconnection.State != ConnectionState.Open)
            {
                _sqlconnection = new SqliteConnection(ConnectionString);
                _sqlconnection.Open();
            }

            if (_sqltransaction == null)
            {
                _sqltransaction = _sqlconnection.BeginTransaction();
            }
            return;
        }

        public void Bgworker_ExecuteQuery(object sender, DoWorkEventArgs e)
        {
            string sql = e.Argument as string;
            ExecuteQuery(sql);
            CommitTransaction();
        }

        public void ExecuteQuery(string sql)
        {
            try
            {
                CheckDatabaseConnection();// (true);
                using (var cmd = _sqlconnection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Transaction= _sqltransaction;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                FireErrorMessage("ERROR [DATABASE]: " + ex.Message);
            }
        }

        /// <summary>
        /// Executes a non query command in the SQLite database
        /// </summary>
        /// /// <remarks>
        /// this function adds the query to the transactions.  User need to commit the transaction when finished.
        /// </remarks>
        /// <param name="sql">sql text to be executed.</param>
        private void ExecuteNonQuery(string sql)
        {
            //using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            //{
            //bool restarted = false;
            int triesCount = 0;
            int maxAttempts = 10;
        reStartCommitNQ:
            try
            {
                CheckDatabaseConnection();
                using (var cmd = _sqlconnection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Transaction= _sqltransaction;
                    cmd.ExecuteNonQuery();
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("locked"))
                {
                    if (triesCount == 0) 
                        FireErrorMessage(" Background output processing re-opening DB connection.");
                    else
                        FireErrorMessage(new String('-', triesCount));
                    System.Threading.Thread.Sleep(100*(triesCount+1));
                    triesCount++;
                    if (triesCount < maxAttempts) goto reStartCommitNQ;
                    FireErrorMessage("[ERROR Database]: " + Environment.NewLine + ex.Message);
                    FireErrorMessage("[ERROR Database]: Failed to commit changes in the database.");
                }
                else
                    FireErrorMessage("ERROR [DATABASE]" + ex.Message + Environment.NewLine + "   executing: " + sql);
            }
            finally
            {
                //c.Close();
            }
            //}
        }

        private int InsertRow(DataTable table, DataRow row)
        {
            List<string> columnNames = new List<string>();
            List<string> parameterNames = new List<string>();

            using (var cmd = _sqlconnection.CreateCommand())
            {
                cmd.Transaction = _sqltransaction;

                int parameterIndex = 0;

                foreach (DataColumn column in table.Columns)
                {
                    string parameterName = "$p" + parameterIndex;

                    columnNames.Add(QuoteIdentifier(column.ColumnName));
                    parameterNames.Add(parameterName);

                    object value = row[column, DataRowVersion.Current];

                    cmd.Parameters.AddWithValue(
                        parameterName,
                        value == null ? DBNull.Value : value);

                    parameterIndex++;
                }

                cmd.CommandText =
                    "INSERT INTO " + QuoteIdentifier(table.TableName) +
                    " (" + string.Join(", ", columnNames) + ")" +
                    " VALUES (" + string.Join(", ", parameterNames) + ");";

                return cmd.ExecuteNonQuery();
            }
        }


        private int DeleteRow(DataTable table, DataRow row)
        {
            if (table.PrimaryKey == null || table.PrimaryKey.Length == 0)
            {
                throw new InvalidOperationException(
                    "Cannot delete from table '" + table.TableName +
                    "' because the DataTable does not define a primary key.");
            }

            List<string> whereClauses = new List<string>();

            using (var cmd = _sqlconnection.CreateCommand())
            {
                cmd.Transaction = _sqltransaction;

                for (int i = 0; i < table.PrimaryKey.Length; i++)
                {
                    DataColumn keyColumn = table.PrimaryKey[i];
                    string parameterName = "$key" + i;

                    whereClauses.Add(
                        QuoteIdentifier(keyColumn.ColumnName) +
                        " = " +
                        parameterName);

                    object value = row[keyColumn, DataRowVersion.Original];

                    cmd.Parameters.AddWithValue(
                        parameterName,
                        value == null ? DBNull.Value : value);
                }

                cmd.CommandText =
                    "DELETE FROM " + QuoteIdentifier(table.TableName) +
                    " WHERE " + string.Join(" AND ", whereClauses) + ";";

                return cmd.ExecuteNonQuery();
            }
        }

        private bool IsDatabaseLocked(Exception ex)
        {
            SqliteException sqliteException = ex as SqliteException;

            if (sqliteException != null)
            {
                // SQLITE_BUSY = 5
                // SQLITE_LOCKED = 6
                return sqliteException.SqliteErrorCode == 5 ||
                       sqliteException.SqliteErrorCode == 6;
            }

            return ex.Message.IndexOf(
                "locked",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string QuoteIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        private int UpdateRow(DataTable table, DataRow row)
        {
            if (table.PrimaryKey == null || table.PrimaryKey.Length == 0)
            {
                throw new InvalidOperationException(
                    "Cannot update table '" + table.TableName +
                    "' because the DataTable does not define a primary key.");
            }

            List<string> setClauses = new List<string>();
            List<string> whereClauses = new List<string>();

            using (var cmd = _sqlconnection.CreateCommand())
            {
                cmd.Transaction = _sqltransaction;

                int parameterIndex = 0;

                foreach (DataColumn column in table.Columns)
                {
                    // This assumes primary-key values are not edited.
                    if (table.PrimaryKey.Contains(column))
                        continue;

                    string parameterName = "$value" + parameterIndex;

                    setClauses.Add(
                        QuoteIdentifier(column.ColumnName) +
                        " = " +
                        parameterName);

                    object value = row[column, DataRowVersion.Current];

                    cmd.Parameters.AddWithValue(
                        parameterName,
                        value == null ? DBNull.Value : value);

                    parameterIndex++;
                }

                for (int i = 0; i < table.PrimaryKey.Length; i++)
                {
                    DataColumn keyColumn = table.PrimaryKey[i];
                    string parameterName = "$key" + i;

                    whereClauses.Add(
                        QuoteIdentifier(keyColumn.ColumnName) +
                        " = " +
                        parameterName);

                    object value = row[keyColumn, DataRowVersion.Original];

                    cmd.Parameters.AddWithValue(
                        parameterName,
                        value == null ? DBNull.Value : value);
                }

                if (setClauses.Count == 0)
                    return 0;

                cmd.CommandText =
                    "UPDATE " + QuoteIdentifier(table.TableName) +
                    " SET " + string.Join(", ", setClauses) +
                    " WHERE " + string.Join(" AND ", whereClauses) + ";";

                return cmd.ExecuteNonQuery();
            }
        }



        public void PrepareSQLiteOutputFile(MODSIMOutputDS outDS)
        {
            // not needed using microsoft.data.sqlite, the file is created when the connection is opened.
            //if (!File.Exists(dbFile))
            //    SQLiteConnection.CreateFile(dbFile);
            CheckDatabaseConnection();

            FireErrorMessage("   Preparing db for output...");
            foreach (DataTable table in outDS.Tables)
            {
                if (!IsTableExist(table.TableName) || IsTableMissingColumns(table))
                {
                    string sql = "create table " + table.TableName + "(";
                    bool first = true, firstkey = true;
                    string primaryKeys = "";
                    foreach (DataColumn col in table.Columns)
                    {

                        if (!first) sql += ",";
                        sql += col.ColumnName + " " + GetSQLType(col.DataType.Name);
                        first = false;
                        if (col.Unique)
                        {
                            if (!firstkey) primaryKeys += ",";
                            primaryKeys += col.ColumnName;
                            firstkey = false;
                        }

                    }
                    if (table.PrimaryKey != null)
                    {

                        first = true;
                        if (table.PrimaryKey.Length == 0 && primaryKeys != "") sql += ", PRIMARY KEY(" + primaryKeys;
                        else
                        {
                            for (int i = 0; i < table.PrimaryKey.Length; i++)
                            {
                                if (first) sql += ", PRIMARY KEY(";
                                else sql += ",";
                                sql += table.PrimaryKey[i].ColumnName;
                                first = false;
                            }
                        }
                        if (sql.Contains("PRIMARY")) sql += ")";
                    }
                    sql += ")";
                    ExecuteNonQuery(sql);
                }
                else
                {
                    //Clear the data for existing tables.
                    ExecuteNonQuery("DELETE FROM [" + table.TableName + "]");
                }

            }
            //CommitTransaction();
            FireErrorMessage("   Output db ready.");
        }

        public void CreateSQLiteTSFile(DataSet modelTimeSeries, int activeScn)
        {
            try
            {
                // not needed using microsoft.data.sqlite, the file is created when the connection is opened.
                //if (!File.Exists(dbFile)) SQLiteConnection.CreateFile(dbFile);
                foreach (DataTable TSeries in modelTimeSeries.Tables)
                {
                    if (!IsTableExist(TSeries.TableName))
                    {
                        string sql = "create table [" + TSeries.TableName + "] (";
                        bool first = true, firstkey = true;
                        string primaryKeys = "";
                        foreach (DataColumn col in TSeries.Columns)
                        {

                            if (!first) sql += ",";
                            sql += col.ColumnName + " " + GetSQLType(col.DataType.Name);
                            first = false;
                            if (col.Unique)
                            {
                                if (!firstkey) primaryKeys += ",";
                                primaryKeys += col.ColumnName;
                                firstkey = false;
                            }

                        }
                        if (TSeries.PrimaryKey != null)
                        {

                            first = true;
                            if (TSeries.PrimaryKey.Length == 0 && primaryKeys != "") sql += ", PRIMARY KEY(" + primaryKeys;
                            else
                            {
                                for (int i = 0; i < TSeries.PrimaryKey.Length; i++)
                                {
                                    if (first) sql += ", PRIMARY KEY(";
                                    else sql += ",";
                                    sql += TSeries.PrimaryKey[i].ColumnName;
                                    first = false;
                                }
                            }
                            if (sql.Contains("PRIMARY")) sql += ")";
                        }
                        sql += ")";
                        ExecuteNonQuery(sql);
                        foreach (DataRow dr in TSeries.Rows)
                            if (dr.RowState == DataRowState.Unchanged) dr.SetAdded();
                    }
                    else
                    {
                        //If the table already has modified data, delete it for the table update.
                        //  TSTablesInfo needs to be treated differently, updating only the active scenario entries to preserve other scnario info.
                        if (TSeries.GetChanges() != null || TSeries.TableName == "TSTablesInfo")
                            if (TSeries.Rows.Count > 0 && (TSeries.GetChanges().Rows.Count > 0 || TSeries.TableName == "TSTablesInfo"))
                            {
                                String m_SQL = "DELETE FROM [" + TSeries.TableName + "]";
                                if (TSeries.TableName == "TSTablesInfo")
                                    m_SQL += " WHERE [ScnID] = " + activeScn;
                                ExecuteNonQuery(m_SQL);

                                //the TSTablesInfo has the relevant info in the rows (added) and might contain other entries that should be left
                                //  in the table
                                foreach (DataRow dr in TSeries.Rows)
                                {
                                    if (TSeries.TableName == "TSTablesInfo")
                                    {
                                        if (int.Parse(dr["ScnID"].ToString()) == activeScn)
                                        {
                                            dr.AcceptChanges();
                                            if (dr.RowState == DataRowState.Unchanged) dr.SetAdded();
                                        }
                                    }
                                    else
                                        if (dr.RowState == DataRowState.Unchanged) dr.SetAdded();
                                }
                            }
                    }
                }
            }
            catch { }
            finally
            {
                //This is required for the non-query transactions.
                //CommitTransaction();  //Commit is performed in the calling routine
            }
        }

        /// <summary>
        /// This function tests if there are differences in the columns between two tables, the one in memory and the one in the database.
        /// A reason for the tables to be different is having custom user variables defined in the output and the database created with the standard database schema.
        /// 
        /// </summary>
        /// <param name="table">In memory database table to be tested against the database table.</param>
        /// <returns>True/False if the columns have different rows. if different columns are identified, the database table is deleted. 
        /// These changes are done in the transaction, so a commit is required in the calling routine.  
        /// All calls to the database need to be called with transactions for consistency.</returns>
        private bool IsTableMissingColumns(DataTable table)
        {
            bool isMissing = false;
            try
            {
                //CheckDatabaseConnection(true);
                string sql = "SELECT * FROM " + table.TableName + " LIMIT 1";
                DataTable dbTbl = GetDBTable(sql, table.TableName);
                foreach (DataColumn dc in table.Columns)
                {
                    if (!dbTbl.Columns.Contains(dc.ColumnName))
                    {
                        isMissing = true;
                        ExecuteNonQuery("DROP TABLE " + table.TableName);
                        // CommitTransaction();  //Commit will be performed in the calling function.
                        break;
                    }
                }
            }
            catch
            {
                isMissing = true;
            }
            return isMissing;
        }

        private bool IsTableExist(string tablename)
        {
            bool isexist = false;
            try
            {
                CheckDatabaseConnection();
                string sql = "SELECT name FROM sqlite_master WHERE type = 'table'";
                using (var cmd = _sqlconnection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Transaction = _sqltransaction;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            if (r[0].ToString() == tablename)
                            {
                                isexist = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireErrorMessage("ERROR [DATABASE]" + ex.Message);
            }
            finally
            {
                //c.Close();
            }
            //using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            //{
            //    try
            //    {
            //        c.Open();
            //        string sql = "SELECT name FROM sqlite_master WHERE type = 'table'";
            //        using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
            //        {
            //            SQLiteDataReader r = cmd.ExecuteReader();
            //            while (r.Read())
            //            {
            //                if (r[0].ToString() == tablename)
            //                {
            //                    isexist = true;
            //                }
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("ERROR [DATABASE]: " + ex.Message);
            //    }
            //    finally
            //    {
            //        c.Close();
            //    }
            //}
            return isexist;
        }
        /// <summary>Retrieves all the table names within the database.</summary>
        /// <returns>Returns the tables names or Nothing if an error occurs.</returns>
        public string[] GetTableNames()
        {
            List<string> tables = new List<string>();
            try
            {
                CheckDatabaseConnection();
                string sql = "SELECT name FROM sqlite_master WHERE type = 'table'";
                using (var cmd = _sqlconnection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Transaction = _sqltransaction;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            tables.Add(r[0].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireErrorMessage("ERROR [DATABASE]" + ex.Message);
            }
            finally
            {
                //c.Close();
            }
            return tables.ToArray();
        }

        private string GetSQLType(string p)
        {
            switch (p)
            {
                case "Int32":
                case "Int64":
                    return "integer";
                case "String":
                    return "text";
                case "Double":
                    return "real";
                case "DateTime":
                    return "DATE";
                default:
                    return "none";
            }
        }

        public void CommitTransaction(bool printMsg = false, bool omitCommit = false)
        {
            int triesCount = 0;
            int maxAttempts = 10;
        reStartCommit:
            try
            {
                if (_sqltransaction != null && _sqltransaction.Connection != null)
                {
                    if (!omitCommit)
                    {
                        _sqltransaction.Commit();
                        System.Threading.Thread.Sleep(100);
                    }
                    _sqltransaction.Dispose();
                    _sqltransaction = null;
                }

                if (_sqlconnection != null && _sqlconnection.State == ConnectionState.Open)
                {
                    _sqlconnection.Close();
                    if (printMsg) FireErrorMessage(" DB connection closed.");
                    _sqlconnection.Dispose();
                    _sqlconnection = null;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("locked"))
                {
                    if (triesCount == 1) FireErrorMessage(" Background output processing re-opening DB connection.");
                    System.Threading.Thread.Sleep(100*triesCount);
                    triesCount++;
                    if (triesCount < maxAttempts) goto reStartCommit;
                    FireErrorMessage("ERROR [Database]: " + Environment.NewLine + ex.Message);
                    FireErrorMessage("ERROR [Database]: Failed to commit changes in the database.");
                    throw new Exception("ERROR [Database]: Failed to commit changes in the database.");
                }
                else
                    FireErrorMessage("ERROR [Database]: " + Environment.NewLine + ex.Message);
            }

        }

        public DataTable GetDBTable(string sql, string tableName)
        {
            DataTable rval = new DataTable();
            try
            {
                CheckDatabaseConnection();
                using (var cmd = _sqlconnection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Transaction = _sqltransaction;

                    using (var reader = cmd.ExecuteReader())
                    {
                        rval.Load(reader);
                    }
                }

                rval.TableName = tableName;

            }
            catch (Exception ex)
            {
                FireErrorMessage("ERROR [DATABASE]" + ex.Message);
            }
            finally
            {
                //CommitTransaction();
                //c.Close();
            }
            return rval;
        }

        public void Bgworker_UpdateTables(object sender, DoWorkEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            List<object> arguments = e.Argument as List<object>;
            DataSet m_DSet = arguments[0] as DataSet;
            bool commit = bool.Parse(arguments[1].ToString());
            double speed = UpdateTables(m_DSet);
            if (commit) CommitTransaction();
            if (speed > 0) FireErrorMessage(speed.ToString("   #.#") + " rows/sec.");
            m_DSet.Dispose();
        }

        public void Bgworker_UpdateTSTables(object sender, DoWorkEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            List<object> arguments = e.Argument as List<object>;
            Csu.Modsim.ModsimModel.Model m_model = arguments[0] as Csu.Modsim.ModsimModel.Model;
            CheckDatabaseConnection();
            foreach (string keyStr in m_model.m_TimeSeriesTbls.Keys)
            {
                DataSet m_TSDS = m_model.m_TimeSeriesTbls[keyStr];
                if (m_TSDS.Tables.Count > 0)
                {
                    Csu.Modsim.ModsimModel.Model.FireOnMessageGlobal("  processing " + keyStr + " - " + m_TSDS.Tables.Count + " time series.");
                    CreateSQLiteTSFile(m_TSDS, m_model.timeseriesInfo.activeScn);
                    double speed = UpdateTables(m_TSDS);
                    if (speed > 0) FireErrorMessage(speed.ToString("   #.#") + " rows/sec.");
                    //m_TSDS.Dispose();     
                }
            }
            CommitTransaction();
            m_model.FireOnMessage(" Done storing timeseries to database.");
            //BackgroundWorker a = sender as BackgroundWorker;
        }

        public bool workingOnUpdates = false;
        public double UpdateTables(DataSet m_DSet)//, bool commit)
        {
            int maxAttempts = 100;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            workingOnUpdates = true;
            //bool restarted = false;
            int totRows = 0;
            CheckDatabaseConnection();
            // DATABASE (Local): Formulate the SQL command.
            //String strSqlCommand = "SELECT * FROM tblTest ORDER BY IdPrimary ASC;";
            //SQLiteCommand oLocalCommand = null;
            //SQLiteDataAdapter oLocalAdapter = null;
            //SQLiteCommandBuilder oBuilder = null;
            //m_DSet.AcceptChanges();
            foreach (DataTable table in m_DSet.Tables)
            {
                int triesCount = 0;
                string currentTable = table.TableName;
                if (table.Rows.Count > 0)
                {
                reStartUpdate:
                    try
                    {
                        // create a separate array because AcceptChanges() can remove 
                        // deleted rows from the DataTable collection.
                        DataRow[] rows = table.Rows.Cast<DataRow>().ToArray();
                        foreach (DataRow row in rows)
                        {
                            if (row.RowState == DataRowState.Unchanged ||
                                row.RowState == DataRowState.Detached)
                            {
                                continue;
                            }
                            int updatedRows = 0;

                            if (row.RowState == DataRowState.Added)
                            {
                                updatedRows = InsertRow(table, row);
                            }
                            else if (row.RowState == DataRowState.Modified)
                            {
                                updatedRows = UpdateRow(table, row);
                            }
                            else if (row.RowState == DataRowState.Deleted)
                            {
                                updatedRows = DeleteRow(table, row);
                            }
                            totRows += updatedRows;
                            row.AcceptChanges();
                        }



                        //oLocalCommand = new SQLiteCommand(strSqlCommand, _sqlconnection, _sqltransaction);
                        //oLocalAdapter = new SQLiteDataAdapter(oLocalCommand);
                        ////oLocalAdapter.AcceptChangesDuringUpdate = true;
                        ////DataSet oLocalSet = new DataSet();
                        ////oLocalAdapter.Fill(oLocalSet, "tblTest");

                        //oBuilder = new SQLiteCommandBuilder(oLocalAdapter);
                        ////oLocalSet.AcceptChanges();
                        //oLocalAdapter.UpdateCommand = oBuilder.GetUpdateCommand();
                        ////oLocalAdapter.InsertCommand = oBuilder.GetInsertCommand();
                        ////oLocalAdapter.DeleteCommand = oBuilder.GetDeleteCommand();
                        //int upd = oLocalAdapter.Update(table);
                        //totRows += upd;// table.Rows.Count;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("locked"))
                        {
                            if (triesCount == 1) FireErrorMessage(" Background output processing re-opening DB connection.");
                            System.Threading.Thread.Sleep(400);
                            //CheckDatabaseConnection();
                            //restarted = true;
                            triesCount++;
                            if (triesCount < maxAttempts) goto reStartUpdate;
                            FireErrorMessage("[ERROR processing output]: " + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
                            FireErrorMessage("[ERROR processing output]: Failed to process table: " + currentTable);
                        }
                        else
                            FireErrorMessage(ex.Message);

                        //CommitTransaction();

                    }
                }
            }
            //if (restarted)
            //    CommitTransaction(restarted);

            // Clean up.
            //oLocalSet.Dispose();
            //if (oLocalAdapter != null) oLocalAdapter.Dispose();
            //if (oLocalCommand != null) oLocalCommand.Dispose();
            //oLocalCommand = null;
            workingOnUpdates = false;
            watch.Stop();
            return (totRows / watch.Elapsed.TotalSeconds);
        }

        /// <summary>Retrieves all the tables within a database and places them in a DataSet.</summary>
        /// <returns>Returns the output DataSet. Returns Nothing if an error occurs.</returns>
        public DataSet GetTables()
        {
            try
            {
                CheckDatabaseConnection();
                if (_sqlconnection==null)
                {
                    return null;
                }

                DataSet ds = new DataSet(dbFile.Replace('.', '_'));
                string[] tables = GetTableNames();
                for (int i = 0; i < tables.Length; i++)
                {
                    DataTable dt = GetDBTable("SELECT * FROM [" + tables[i] + "]", tables[i]);
                    if (dt != null)
                    {
                        ds.Tables.Add(dt.Copy());
                    }
                }
                CommitTransaction();
                return ds.Copy();
            }
            catch (Exception ex)
            {
                string msg = "Error retrieving tables from database " + dbFile + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                CommitTransaction();
                return null;
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (_sqltransaction != null)
                    {
                        _sqltransaction.Dispose();
                        _sqltransaction = null;
                    }
                    if (_sqlconnection != null)
                    {
                        _sqlconnection.Close();
                        _sqlconnection = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SqliteHelper1() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            //GC.SuppressFinalize(this);
        }
        #endregion
    }

}
