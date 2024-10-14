using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace Csu.Modsim.NetworkUtils
{
    public enum MSType
    {
        Undefined = -1,
        Access = 0,
        Excel = 1,
        Word = 2,
        PowerPoint = 3,
        Text = 4
    }
}
namespace Csu.Modsim.NetworkUtils
{
    public class DBUtil : IDisposable
    {
        private OleDbConnection conn = null;
        private string dbPath;
        private string dbName;
        private string dbDir;
        private bool hasHead;
        private string delim;
        private string connStr;
        private bool connFailed = true;
        private MSType dbType;
        private bool overWrite;
        private string msg = "";
        private OleDbDataAdapter dAdapt = null;
        public event FireMessageEventHandler FireMessage;
        public delegate void FireMessageEventHandler(string Message);
        public event FireErrorMessageEventHandler FireErrorMessage;
        public delegate void FireErrorMessageEventHandler(string Message);
        public static string[] ReservedWords = new string[]
        {
            "ACCESSIBLE",
            "ALTER",
            "AS",
            "BEFORE",
            "BINARY",
            "BY",
            "CASE",
            "CHARACTER",
            "COLUMN",
            "CONTINUE",
            "CROSS",
            "CURRENT_TIMESTAMP",
            "DATABASE",
            "DAY_MICROSECOND",
            "DEC",
            "DEFAULT",
            "DESC",
            "DISTINCT",
            "DOUBLE",
            "EACH",
            "ENCLOSED",
            "EXIT",
            "FETCH",
            "FLOAT8",
            "FOREIGN",
            "GRANT",
            "HIGH_PRIORITY",
            "HOUR_SECOND",
            "IN",
            "INNER",
            "INSERT",
            "INT2",
            "INT8",
            "INTO",
            "JOIN",
            "KILL",
            "LEFT",
            "LINEAR",
            "LOCALTIME",
            "LONG",
            "LOOP",
            "MATCH",
            "MEDIUMINT",
            "MINUTE_MICROSECOND",
            "MODIFIES",
            "NO_WRITE_TO_BINLOG",
            "ON",
            "OPTIONALLY",
            "OUT",
            "PRECISION",
            "PURGE",
            "READS",
            "REFERENCES",
            "RENAME",
            "REQUIRE",
            "RETURN",
            "RLIKE",
            "SECOND_MICROSECOND",
            "SEPARATOR",
            "SIGNAL",
            "SPECIFIC",
            "SQLSTATE",
            "SQL_CALC_FOUND_ROWS",
            "STARTING",
            "TERMINATED",
            "TINYINT",
            "TRAILING",
            "UNDO",
            "UNLOCK",
            "USAGE",
            "UTC_DATE",
            "VALUES",
            "VARCHARACTER",
            "WHERE",
            "WRITE",
            "ZEROFILL",
            "ADD",
            "ANALYZE",
            "ASC",
            "BETWEEN",
            "BLOB",
            "CALL",
            "CHANGE",
            "CHECK",
            "CONDITION",
            "CONVERT",
            "CURRENT_DATE",
            "CURRENT_USER",
            "DATABASES",
            "DAY_MINUTE",
            "DECIMAL",
            "DELAYED",
            "DESCRIBE",
            "DISTINCTROW",
            "DROP",
            "ELSE",
            "ESCAPED",
            "EXPLAIN",
            "FLOAT",
            "FOR",
            "FROM",
            "GROUP",
            "HOUR_MICROSECOND",
            "IF",
            "INDEX",
            "INOUT",
            "INT",
            "INT3",
            "INTEGER",
            "IS",
            "KEY",
            "LEADING",
            "LIKE",
            "LINES",
            "LOCALTIMESTAMP",
            "LONGBLOB",
            "LOW_PRIORITY",
            "MAXVALUE",
            "MEDIUMTEXT",
            "MINUTE_SECOND",
            "NATURAL",
            "NULL",
            "OPTIMIZE",
            "OR",
            "OUTER",
            "PRIMARY",
            "RANGE",
            "READ_WRITE",
            "REGEXP",
            "REPEAT",
            "RESIGNAL",
            "REVOKE",
            "SCHEMA",
            "SELECT",
            "SET",
            "SMALLINT",
            "SQL",
            "SQLWARNING",
            "SQL_SMALL_RESULT",
            "STRAIGHT_JOIN",
            "THEN",
            "TINYTEXT",
            "TRIGGER",
            "UNION",
            "UNSIGNED",
            "USE",
            "UTC_TIME",
            "VARBINARY",
            "VARYING",
            "WHILE",
            "XOR",
            "ALL",
            "AND",
            "ASENSITIVE",
            "BIGINT",
            "BOTH",
            "CASCADE",
            "CHAR",
            "COLLATE",
            "CONSTRAINT",
            "CREATE",
            "CURRENT_TIME",
            "CURSOR",
            "DAY_HOUR",
            "DAY_SECOND",
            "DECLARE",
            "DELETE",
            "DETERMINISTIC",
            "DIV",
            "DUAL",
            "ELSEIF",
            "EXISTS",
            "FALSE",
            "FLOAT4",
            "FORCE",
            "FULLTEXT",
            "HAVING",
            "HOUR_MINUTE",
            "IGNORE",
            "INFILE",
            "INSENSITIVE",
            "INT1",
            "INT4",
            "INTERVAL",
            "ITERATE",
            "KEYS",
            "LEAVE",
            "LIMIT",
            "LOAD",
            "LOCK",
            "LONGTEXT",
            "MASTER_SSL_VERIFY_SERVER_CERT",
            "MEDIUMBLOB",
            "MIDDLEINT",
            "MOD",
            "NOT",
            "NUMERIC",
            "OPTION",
            "ORDER",
            "OUTFILE",
            "PROCEDURE",
            "READ",
            "REAL",
            "RELEASE",
            "REPLACE",
            "RESTRICT",
            "RIGHT",
            "SCHEMAS",
            "SENSITIVE",
            "SHOW",
            "SPATIAL",
            "SQLEXCEPTION",
            "SQL_BIG_RESULT",
            "SSL",
            "TABLE",
            "TINYBLOB",
            "TO",
            "TRUE",
            "UNIQUE",
            "UPDATE",
            "USING",
            "UTC_TIMESTAMP",
            "VARCHAR",
            "WHEN",
            "WITH",
            "YEAR_MONTHACCES",
            "MIN",
            "MAX",
            "SUM"

        };
        // Properties
        /// <summary>Gets the connection string for this instance.</summary>
        /// <value>The connection string for the instance.</value>
        /// <returns>Returns the connection string for the instance.</returns>
        public string ConnectionString
        {
            get
            {
                return connStr;
            }
        }
        /// <summary>Gets whether the connection to the database failed.</summary>
        /// <value>A boolean value: True if the connection failed. False if not. </value>
        /// <returns>Returns whether the connection to the database failed.</returns>
        public bool ConnectionFailed
        {
            get
            {
                return connFailed;
            }
        }
        /// <summary>Gets the previously fired message.</summary>
        /// <value>String value describing the previously fired message.</value>
        /// <returns>Returns the previously fired message.</returns>
        public string Message
        {
            get
            {
                return msg;
            }
        }
        /// <summary>Gets the OleDbConnection object.</summary>
        public OleDbConnection Connection
        {
            get
            {
                return this.conn;
            }
        }
        /// <summary>Gets the current data adapter used in retrieving tables.</summary>
        public OleDbDataAdapter DataAdapter
        {
            get
            {
                return this.dAdapt;
            }
        }

        // Constructor / destructor
        /// <summary>Constructs a DBUtil instance.</summary>
        /// <param name="DatabasePath">Specifies the database file path to which to connect.</param>
        /// <param name="HasHeader">Specifies whether a text file or Excel file has a header or not.</param>
        /// <param name="SpecifiedDelimiter">Specifies the delimiter to be used when reading and writing text files.</param>
        /// <param name="Overwrite">Specifies whether to overwrite a database.</param>
        /// <param name="type">Specifies the type of database being accessed. If not specified, the database type is inferred based on file extension.</param>
        /// <remarks>This method opens a connection to the database immediately. The connection stays open until the Close() method is called. Methods within the class pump messages using FireMessage(msg) and errors using FireErrorMessage(msg).</remarks>
        public DBUtil(string DatabasePath, bool HasHeader = false, string SpecifiedDelimiter = "", bool Overwrite = false, MSType type = MSType.Undefined)
            : base()
        {
            dbPath = DatabasePath;
            dbName = Path.GetFileName(dbPath);
            dbDir = Path.GetDirectoryName(dbDir) + "\\";
            if (dbDir == "\\")
            {
                dbDir = "";
            }
            hasHead = HasHeader;
            delim = SpecifiedDelimiter;
            ConnectionBuilder cb = new ConnectionBuilder(dbPath, hasHead, delim, type);
            connStr = cb.ConnectionString;
            dbType = cb.GetMSType;
            this.overWrite = Overwrite;
        }
        /// <summary>Opens the connection to the database. Creates the database if it does not exist.</summary>
        /// <remarks>If the database does not exist, this method creates one.</remarks>
        public void Open()
        {
            OpenDBConnection();
        }
        /// <summary>Creates the database if it does not exist.</summary>
        /// <param name="Overwrite">Specifies whether to overwrite the database if it already exists (this would basically be creating a new database).</param>
        public void Create(bool Overwrite = false, bool LeaveOpen = false)
        {
            // Don't create the database if it is already connected.
            if (connFailed || Overwrite || !File.Exists(dbPath))
            {
                bool oldVal = this.overWrite;
                this.overWrite = Overwrite;
                OpenDBConnection();
                if (!LeaveOpen)
                {
                    CloseDBConnection();
                }
                this.overWrite = oldVal;
            }
            else
            {
                msg = "Database " + dbName + " was not created, because it is already connected, or it already exists and Overwrite = False.";
                if (FireMessage != null)
                {
                    FireMessage(msg);
                }
            }
        }
        /// <summary>Closes the connection to the database.</summary>
        public void Close()
        {
            CloseDBConnection();
        }

        // Opening and closing
        private void OpenDBConnection()
        {
            if (conn == null)
            {
                conn = new OleDbConnection();
                conn.ConnectionString = connStr;
                try
                {
                    if (!File.Exists(dbPath))
                    {
                        CreateDB();
                    }
                    conn.Open();
                    connFailed = false;
                    msg = "Database " + dbName + " successfully opened.";
                    //RaiseEvent FireMessage(msg)
                }
                catch (System.Exception ex)
                {
                    conn = null;
                    connFailed = true;
                    msg = "Error opening database " + dbName + Environment.NewLine + "ERROR MESSAGE: " + ex.Message + Environment.NewLine + Environment.NewLine + "Do you have the correct database engine installed? Download and install the latest version of Microsoft's Access Database Engine (Make sure to use the 64-bit version of the Access Database Engine for computers with 64-bit CPU; otherwise, use the 32-bit version).";
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                }
            }
        }
        private void CreateDB()
        {
            // Create the database
            try
            {
                if (dbType == MSType.Text)
                {
                    if (!Directory.Exists(dbDir))
                    {
                        Directory.CreateDirectory(dbDir);
                    }
                }
                else
                {
                    if (!overWrite && File.Exists(dbPath))
                    {
                        return;
                    }
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }

                    Stream stream = null;
                    if (dbType == MSType.Access)
                    {
                        stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Csu.Modsim.NetworkUtils.mdb_template.mdb");
                    }
                    else if (dbType == MSType.Excel)
                    {
                        stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Csu.Modsim.NetworkUtils.blank.xls");
                    }
                    else
                    {
                        throw new Exception("Type not supported by create database.");
                    }

                    // If we have a stream, create it
                    if (stream != null)
                    {
                        using (stream)
                        {
                            byte[] buffer = new byte[Convert.ToInt32(stream.Length) + 1];
                            stream.Read(buffer, 0, buffer.Length);

                            using (FileStream fs = new FileStream(dbPath, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                using (BinaryWriter bw = new BinaryWriter(fs))
                                {
                                    bw.Write(buffer);
                                }
                            }

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                msg = "Error when attempting to create database " + dbPath + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
            }
        }
        private void CloseDBConnection()
        {
            if (conn != null && conn.State == ConnectionState.Open)
            {
                try
                {
                    conn.Dispose();
                    conn.Close();
                    connFailed = true;
                    msg = "Database " + dbName + " successfully closed.";
                    //RaiseEvent FireMessage(msg)
                }
                catch (Exception ex)
                {
                    connFailed = true;
                    msg = "Error closing database " + dbPath + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                }
                finally
                {
                    conn = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        // Retrieving data from data base
        /// <summary>Retrieves a table from a database and puts it in a DataTable.</summary>
        /// <param name="queryString">The string querying the database.</param>
        /// <param name="TableName">The output name for the table.</param>
        /// <returns>Returns a DataTable filled with the data from the query or Nothing if an error occurs.</returns>
        public DataTable GetTable(string queryString, string TableName)
        {
            DataTable functionReturnValue = null;
            try
            {
                if (connFailed)
                {
                    return null;
                }

                OleDbCommand SelectCmd = new OleDbCommand(queryString, conn);
                dAdapt = new OleDbDataAdapter();
                dAdapt.SelectCommand = SelectCmd;
                OleDbCommandBuilder cb = new OleDbCommandBuilder(dAdapt);
                DataSet m_DS = new DataSet();
                dAdapt.Fill(m_DS, "QueryTable");
                functionReturnValue = m_DS.Tables["QueryTable"].Copy();
                functionReturnValue.TableName = TableName;
            }
            catch (Exception ex)
            {
                msg = "Error getting table " + TableName + " from the database." + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return null;
            }
            return functionReturnValue;
        }
        /// <summary>Retrieves all the table names within the database.</summary>
        /// <returns>Returns the tables names or Nothing if an error occurs.</returns>
        public string[] GetTableNames()
        {
            try
            {
                if (connFailed)
                {
                    return null;
                }

                DataTable schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[]
                {
                    null,
                    null,
                    null,
                    "TABLE"
                });
                if (schemaTable.Rows.Count > 0)
                {
                    // Get the names of all the tables.
                    string[] tables = new string[schemaTable.Rows.Count];
                    for (int i = 0; i < schemaTable.Rows.Count; i++)
                    {
                        tables[i] = schemaTable.Rows[i][2].ToString();
                    }

                    // Ensure that none of the tables in the list have the same name (minus '$' off the end)
                    List<string> aList = new List<string>();
                    Array.Sort(tables);
                    if (tables.Length > 0)
                    {
                        char[] c = new char[] { '$' };
                        tables[0] = tables[0].TrimEnd(c);
                        aList.Add(tables[0]);
                        for (int i = 1; i < tables.Length; i++)
                        {
                            tables[i] = tables[i].TrimEnd(c);
                            if (tables[i] != tables[i - 1])
                            {
                                aList.Add(tables[i]);
                            }
                        }
                        tables = new string[aList.Count];
                        aList.CopyTo(tables);
                    }
                    return tables;
                }
                return null;
            }
            catch (Exception ex)
            {
                msg = "Error retrieving table names from database " + dbName + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return null;
            }
        }
        /// <summary>Retrieves all the tables within a database and places them in a DataSet.</summary>
        /// <returns>Returns the output DataSet. Returns Nothing if an error occurs.</returns>
        public DataSet GetTables()
        {
            try
            {
                if (connFailed)
                {
                    return null;
                }

                DataSet ds = new DataSet(dbName.Replace('.', '_'));
                string[] tables = GetTableNames();
                for (int i = 0; i < tables.Length; i++)
                {
                    DataTable dt = GetTable("SELECT * FROM [" + tables[i] + "]", tables[i]);
                    if (dt != null)
                    {
                        ds.Tables.Add(dt);
                    }
                }
                return ds;
            }
            catch (Exception ex)
            {
                msg = "Error retrieving tables from database " + dbName + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return null;
            }
        }

        // Writing data to data base
        /// <summary>Imports a single DataTable into the database.</summary>
        /// <param name="table">The DataTable to import.</param>
        /// <param name="Overwrite">Specifies whether to overwrite a table with the same name, if it exists.</param>
        /// <param name="DecimalAccuracy">Decimal accuracy with which to store data.</param>
        /// <returns>Returns an instance of TimeSpan specifying how long it took to import the new table.</returns>
        public TimeSpan ImportTable(DataTable table, bool Overwrite = false, int DecimalAccuracy = 0)
        {
            try
            {
                if (connFailed)
                {
                    return TimeSpan.Zero;
                }

                Stopwatch sw = Stopwatch.StartNew();

                // Define the column names
                string[] ColNames = GetColNames(table);
                string Cols = "[" + string.Join("], [", ColNames) + "]";

                // Make sure the directory exists
                string txtFlDir = Path.GetDirectoryName(dbPath) + "\\";
                if (!Directory.Exists(txtFlDir))
                {
                    Directory.CreateDirectory(txtFlDir);
                }

                // Write temporary file (much quicker than inserting row by row)
                string txtFlName = "temporary_" + table.TableName + ".csv";
                DTtoText(table, txtFlDir + txtFlName, true, false, ',', null, null, DecimalAccuracy);

                // Insert into the database
                CreateTable(table, Overwrite);
                OleDbCommand cmd = new OleDbCommand("INSERT INTO [" + table.TableName + "] (" + Cols + ") SELECT " + Cols + " FROM " + (new ConnectionBuilder(txtFlDir + txtFlName, true, ",")).TextConnStringInQuery(), conn);
                cmd.ExecuteNonQuery();

                // Delete temporary file
                File.Delete(txtFlDir + txtFlName);

                // Return the elapsed time
                sw.Stop();
                return sw.Elapsed;
            }
            catch (Exception ex)
            {
                msg = "Error importing DataTable " + table.TableName + " to database " + dbName + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return TimeSpan.Zero;
            }
        }
        /// <summary>Imports all the tables within a DataSet into the database.</summary>
        /// <param name="ds">The DataSet to import.</param>
        /// <param name="Overwrite">Specifies whether to overwrite a table with the same name, if it exists.</param>
        /// <returns>Returns an instance of TimeSpan specifying how long it took to import the DataSet.</returns>
        public TimeSpan ImportDataSet(DataSet ds, bool Overwrite = false, int DecimalAccuracy = 0)
        {
            try
            {
                if (connFailed)
                {
                    return TimeSpan.Zero;
                }

                Stopwatch sw = Stopwatch.StartNew();

                // Loop through the tables and only import the ones that don't exist or can be overwritten.
                for (int i = 0; i < ds.Tables.Count; i++)
                {
                    msg = "Importing table " + ds.Tables[i].TableName;
                    if (FireMessage != null)
                    {
                        FireMessage(msg);
                    }
                    ImportTable(ds.Tables[i], Overwrite, DecimalAccuracy);
                }

                sw.Stop();
                return sw.Elapsed;
            }
            catch (Exception ex)
            {
                msg = "Error importing DataSet " + ds.DataSetName + " to database " + dbName + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return TimeSpan.Zero;
            }
        }
        /// <summary>Exports a DataTable to a text file.</summary>
        /// <param name="dt">The DataTable to export.</param>
        /// <param name="FlPath">The file path of the text file to export.</param>
        /// <param name="IncludeHeaders">Specifies whether to write the headers or not.</param>
        /// <param name="AppendToTextFile">Specifies whether to append to the text file. If false, this method will overwrite the old text file.</param>
        /// <param name="delim">Specifies the delimiter character to use between columns.</param>
        /// <param name="formats">An array of strings specifying the formats for each column. If not specified, default number formatting is used.</param>
        /// <param name="FixedWidths">An array of integers specifying the width of each column, if fixed. If not specified, only one delimiter is used between each column.</param>
        public static void DTtoText(DataTable dt, string FlPath, bool IncludeHeaders = false, bool AppendToTextFile = false, char delim = ' ', string[] formats = null, int[] FixedWidths = null, int DecimalAccuracy = 0)
        {
            try
            {
                // Create the output directory if it does not exist
                if (!Directory.Exists(Path.GetDirectoryName(FlPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FlPath));
                }

                // Start writing to the file
                StreamWriter sw = new StreamWriter(FlPath, AppendToTextFile);
                sw.Write(DTToString(dt, IncludeHeaders, AppendToTextFile, delim, formats, FixedWidths, DecimalAccuracy));
                sw.Close();
            }
            catch
            {
                throw new Exception("Error exporting DataTable " + dt.TableName + " to text " + Path.GetFileName(FlPath));
            }
        }
        /// <summary>Creates a string representing a datatable.</summary>
        /// <param name="dt">The DataTable to export.</param>
        /// <param name="IncludeHeaders">Specifies whether to write the headers or not.</param>
        /// <param name="AppendToTextFile">Specifies whether to append to the text file. If false, this method will overwrite the old text file.</param>
        /// <param name="delim">Specifies the delimiter character to use between columns.</param>
        /// <param name="formats">An array of strings specifying the formats for each column. If not specified, default number formatting is used.</param>
        /// <param name="FixedWidths">An array of integers specifying the width of each column, if fixed. If not specified, only one delimiter is used between each column.</param>
        /// <returns>Returns a string that has the whole datatable in it.</returns>
        public static string DTToString(DataTable dt, bool IncludeHeaders = false, bool AppendToTextFile = false, char delim = ' ', string[] formats = null, int[] FixedWidths = null, int DecimalAccuracy = 0)
        {
            StringBuilder s = new StringBuilder();

            if (IncludeHeaders)
            {
                s.Append(OutputLine(GetColNames(dt), delim, formats, FixedWidths) + Environment.NewLine);
            }

            // Make sure the formats are specified as 'R' (round-trip) for Double and Single columns at least
            Type[] types = GetColTypes(dt);
            if (formats == null || formats.Length != dt.Columns.Count)
            {
                Array.Resize(ref formats, dt.Columns.Count);
            }
            if (DecimalAccuracy != 0)
            {
                for (int j = 0; j < types.Length; j++)
                {
                    if ((object.ReferenceEquals(types[j], typeof(double)) || object.ReferenceEquals(types[j], typeof(float))) && (formats[j] == null || string.IsNullOrEmpty(formats[j])))
                    {
                        formats[j] = "F" + DecimalAccuracy;
                    }
                }
            }

            // Write each row to the text file.
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                s.Append(OutputLine(dt.Rows[i].ItemArray, delim, formats, FixedWidths) + Environment.NewLine);
            }

            return s.ToString();
        }

        // DataTable / Database reader and writer helper methods
        private static string[] GetColNames(DataTable dt)
        {
            string[] colNames = new string[dt.Columns.Count];
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                colNames[i] = dt.Columns[i].ColumnName;
            }
            return colNames;
        }
        private static Type[] GetColTypes(DataTable dt, string[] colNames = null)
        {
            if (colNames == null)
            {
                colNames = GetColNames(dt);
            }
            Type[] colTypes = new Type[colNames.Length + 1];
            for (int i = 0; i < colNames.Length; i++)
            {
                colTypes[i] = dt.Columns[i].DataType;
            }
            return colTypes;
        }
        private int CreateTable(DataTable m_table, bool Overwrite = false)
        {
            try
            {
                // Make sure not to overwrite the current table in the database
                if (connFailed || (!Overwrite && TableExists(m_table.TableName)))
                {
                    return 0;
                }
                if (Overwrite && TableExists(m_table.TableName))
                {
                    DeleteExistingTables(m_table.TableName);
                }

                // Build the query string
                OleDbCommand insertCMD = new OleDbCommand();
                insertCMD.Connection = conn;
                string insertStr = "CREATE TABLE [" + m_table.TableName + "] (";
                string[] ColNames = GetColNames(m_table);
                System.Type[] ColTypes = GetColTypes(m_table, ColNames);
                for (int col = 0; col < ColNames.Length; col++)
                {
                    if (col > 0)
                    {
                        insertStr += ", ";
                    }
                    insertStr += " [" + ColNames[col] + "] " + MapToSQLType(ColTypes[col].FullName) + " NULL ";
                }
                insertStr += " );";
                insertCMD.CommandText = insertStr;
                return insertCMD.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = "Error creating the table " + m_table.TableName + "." + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return 0;
            }
        }
        /// <summary>Determines whether the specified table exists within the database.</summary>
        /// <param name="tablename">The name of the table to search for.</param>
        /// <returns>Returns true if the table exists. Returns false if it does not, or if an error occurs when attempting to retrieve database information.</returns>
        public bool TableExists(string tablename)
        {
            try
            {
                DataTable schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[]
                {
                    null,
                    null,
                    tablename,
                    "TABLE"
                });
                if (schemaTable.Rows.Count == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        /// <summary>Deletes existing tables within the database... Be careful when calling!</summary>
        /// <param name="SpecificTable">Specifies a single table to delete. If Nothing, this method will delete all tables within the database.</param>
        /// <returns>Returns the number of records affected.</returns>
        public int DeleteExistingTables(string SpecificTable = null)
        {
            try
            {
                if (connFailed)
                {
                    return 0;
                }
                int recordsAffected = 0;

                // Return immediately if the table does not exist.
                if (SpecificTable != null && !TableExists(SpecificTable))
                {
                    return 0;
                }

                // Loop through the schema and delete all the tables (unless a SpecificTable is specified)
                string[] tables = GetTableNames();
                if ((tables != null) && tables.Length > 0)
                {
                    OleDbCommand insertCMD = new OleDbCommand();
                    insertCMD.Connection = conn;
                    for (int i = 0; i < tables.Length; i++)
                    {
                        if (SpecificTable == null || (SpecificTable == tables[i]))
                        {
                            insertCMD.CommandText = "DROP TABLE " + tables[i];
                            recordsAffected += insertCMD.ExecuteNonQuery();
                        }
                    }
                }
                return recordsAffected;
            }
            catch (Exception ex)
            {
                if (SpecificTable == null)
                {
                    msg = "Error deleting tables within the database." + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                }
                else
                {
                    msg = "Error attempting to delete table " + SpecificTable + "." + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                    if (FireErrorMessage != null)
                    {
                        FireErrorMessage(msg);
                    }
                }
                return 0;
            }
        }
        /// <summary>Maps a CLR type (System.Integer, System.Long, etc.) to an SQL type (INT, BIGINT, etc.).</summary>
        /// <param name="systemType">The CLR or System datatype.</param>
        /// <returns>Returns a string of the SQL type.</returns>
        public static string MapToSQLType(string systemType)
        {
            switch (systemType)
            {
                case "System.String":
                    return " TEXT ";
                case "System.DateTime":
                    return " DATETIME ";
                case "System.Double":
                    return " FLOAT ";
                case "System.Decimal":
                    return " FLOAT ";
                case "System.Single":
                    return " REAL ";
                case "System.Int32":
                case "System.Int16":
                    return " INT ";
                case "System.Long":
                case "System.Int64":
                    return " BIGINT ";
                case "System.Boolean":
                    return "YESNO";
                default:
                    throw new Exception("Cannot convert " + systemType + " to SQL type.");
            }
        }
        /// <summary>Queries a DataTable using the Select method, and returns a DataTable instead of a DataRow array.</summary>
        /// <param name="dTable">The DataTable to query.</param>
        /// <param name="filter">The filter expression used to query the DataTable.</param>
        /// <returns>Returns a DataTable with rows queried using the filter expression.</returns>
        public static DataTable QueryDataTable(DataTable dTable, string filter)
        {
            DataTable dt = dTable.Clone();
            DataRow[] drs = dTable.Select(filter);
            foreach (DataRow row in drs)
            {
                dt.ImportRow(row);
            }
            return dt;
        }

        // Helper methods for writing text output
        private static string OutputLine(object[] theVals, char delim = ' ', string[] formats = null, int[] FixedWidths = null)
        {
            StringBuilder strBuilder = new StringBuilder();
            int j = 0;

            // Make sure all arrays are the correct size and fill data if need be.
            if (theVals.Length == 0)
            {
                return "";
            }
            Array.Resize(ref formats, theVals.Length);
            if (FixedWidths != null)
            {
                int valLen = FixedWidths.Length;
                Array.Resize(ref FixedWidths, theVals.Length);
                // Fill in the rest of the fixed widths with the last fixed width number (only if FixedWidths.Length < theVals.Length)
                for (j = valLen; j < theVals.Length; j++)
                {
                    FixedWidths[j] = FixedWidths[valLen - 1];
                }
            }

            // Build the string line
            for (j = 0; j < theVals.Length - 1; j++)
            {
                string tmp = ValueToString(theVals[j], formats[j]);
                if (FixedWidths == null)
                {
                    strBuilder.Append(tmp.PadRight(tmp.Length + 1, delim));
                }
                else
                {
                    strBuilder.Append(tmp.PadRight(FixedWidths[j], delim));
                }
            }
            strBuilder.Append(ValueToString(theVals[j], formats[j]));
            return strBuilder.ToString();
        }
        private static string ValueToString(object theVal, string format)
        {
            if (object.ReferenceEquals(theVal.GetType(), typeof(double)))
            {
                return Convert.ToDouble(theVal).ToString(format);
            }
            else if (object.ReferenceEquals(theVal.GetType(), typeof(long)))
            {
                return Convert.ToInt64(theVal).ToString(format);
            }
            else if (object.ReferenceEquals(theVal.GetType(), typeof(int)))
            {
                return Convert.ToInt32(theVal).ToString(format);
            }
            else if (object.ReferenceEquals(theVal.GetType(), typeof(float)))
            {
                return Convert.ToSingle(theVal).ToString(format);
            }
            else
            {
                return theVal.ToString();
            }
        }

        // Any NonQuery type executions
        /// <summary>Executes a NonQuery type database read and/or write.</summary>
        /// <param name="expression">Specifies the sql statements.</param>
        /// <returns>Returns the number of rows affected by the execution. Returns 0 when an error occurs.</returns>
        public int ExecuteNonQuery(string expression)
        {
            if (this.connFailed)
            {
                return 0;
            }
            int m_rowsaffected = 0;
            OleDbCommand m_command = new OleDbCommand();
            OleDbTransaction m_trans = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                m_command.Connection = conn;
                m_command.Transaction = m_trans;
                m_command.CommandText = expression;
                m_rowsaffected = m_command.ExecuteNonQuery();
                m_trans.Commit();
            }
            catch (Exception ex)
            {
                m_trans.Rollback();
                msg = "Executing NonQuery expression failed." + Environment.NewLine + "ERROR MESSAGE: " + ex.Message;
                if (FireErrorMessage != null)
                {
                    FireErrorMessage(msg);
                }
                return 0;
            }
            return m_rowsaffected;
        }

        // To detect redundant calls
        private bool disposedValue = false;

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: free other state (managed objects).
                }

                // TODO: free your own state (unmanaged objects).
                // TODO: set large fields to null.
                this.Close();
            }
            this.disposedValue = true;
        }

        #region " IDisposable Support "
        // This code added by Visual Basic to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}

namespace Csu.Modsim.NetworkUtils
{

    public class ConnectionBuilder
    {

        private MSType instType = MSType.Undefined;
        private string dbpath = "";
        private bool HasHeader = false;
        private string Extension = "";
        private string delim = "";
        private string connStr = "";
        public static readonly string[] AccessExtensions = new string[]
        {
            ".mdb",
            ".accdb"
        };
        public static readonly string[] ExcelExtensions = new string[]
        {
            ".xlsx",
            ".xls",
            ".xlsm"
            //, ".xlsb", ".xlsm", ".xlam", ".xltx", ".xltm", ".xla", ".xlt", ".xlm", ".xlw"}
        };
        public static readonly string[] WordExtensions = new string[]
        {
            ".doc",
            ".docx",
            ".docm"
        };
        public static readonly string[] PowerPointExtensions = new string[]
        {
            ".ppt",
            ".pptx",
            ".pptm"
        };
        public static readonly string[] TextExtensions = new string[]
        {
            ".csv",
            ".txt",
            ".asc",
            ".tab"

        };
        // Constructor
        /// <summary>Constructor for the ConnectionBuilder Class.</summary>
        /// <param name="DatabasePath">The full path to the database.</param>
        /// <param name="HasHeader">Value specifying whether the database contains headers (this is only used for Excel and text files).</param>
        /// <param name="SpecifiedDelimiter">A user-specified delimiter for text file databases.</param>
        /// <param name="SpecifiedType">A user-specified MSType of the database. If this is specified, the type of the file is not determined based on the extension of the database file.</param>
        public ConnectionBuilder(string DatabasePath, bool HasHeader = false, string SpecifiedDelimiter = "", MSType SpecifiedType = MSType.Undefined)
        {
            this.dbpath = DatabasePath;
            this.Extension = Path.GetExtension(dbpath).ToLower();
            this.HasHeader = HasHeader;
            if (!string.IsNullOrEmpty(SpecifiedDelimiter))
            {
                this.delim = SpecifiedDelimiter;
            }
            else
            {
                this.delim = GetDelimiter;
            }
            if (SpecifiedType != MSType.Undefined)
            {
                this.instType = SpecifiedType;
            }
            else
            {
                this.instType = GetMSType;
            }
            connStr = ConnectionString;
        }

        // Properties
        /// <summary>Determines the MSType of the current instance based on the specified file path unless an MSType variable was passed to the constructor of the current ConnectionBuilder class.</summary>
        /// <returns>Returns the MSType of the current instance.</returns>
        public MSType GetMSType
        {
            get
            {
                if (instType == MSType.Undefined)
                {
                    if (Array.Find(AccessExtensions, element => element.Equals(Extension)) != null)
                    {
                        return MSType.Access;
                    }
                    else if (Array.Find(ExcelExtensions, element => element.Equals(Extension)) != null)
                    {
                        return MSType.Excel;
                    }
                    else if (Array.Find(WordExtensions, element => element.Equals(Extension)) != null)
                    {
                        return MSType.Word;
                    }
                    else if (Array.Find(PowerPointExtensions, element => element.Equals(Extension)) != null)
                    {
                        return MSType.PowerPoint;
                    }
                    else if (Array.Find(TextExtensions, element => element.Equals(Extension)) != null)
                    {
                        return MSType.Text;
                    }
                    else
                    {
                        return MSType.Undefined;
                    }
                }
                else
                {
                    return instType;
                }
            }
        }
        /// <summary>Retrieves the version of Microsoft Access or Excel (or other) used on the host computer.</summary>
        /// <returns>Returns the integer of the Microsoft Access version installed on the host computer.</returns>
        /// <remarks>This procedure looks within HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\App Paths\ in order to determine where the executable of the office component is located, from which the version of the file is extracted.
        /// 	MS Access versions 'number(year)': 7(1997), 8(1998), 9(2000), 10(2002), 11(2003), 12(2007), 14(2010)
        /// 	MS Excel versions 'number(year)': 2(1987), 3(1990), 4(1992), 5(1993), 7(1995), 8(1997), 9(2000), 10(2002), 11(2003), 12(2007), 14(2010)
        /// </remarks>
        public int MajorVersion
        {
            get
            {
                string path = MSOfficeExePath();
                if (string.IsNullOrEmpty(path))
                {
                    return -1;
                }
                return FileVersionInfo.GetVersionInfo(path).FileMajorPart;
            }
        }
        /// <summary>Gets the delimiter of the current instance of the ConnectionBuilder class.</summary>
        /// <returns>Returns the delimiter of the current instance of the ConnectionBuilder class.</returns>
        public string GetDelimiter
        {
            get
            {
                if (instType == MSType.Text && string.IsNullOrEmpty(delim))
                {
                    switch (Extension)
                    {
                        case ".csv":
                            this.delim = ",";
                            break;
                        case ".txt":
                            this.delim = " ";
                            break;
                        case ".asc":
                            this.delim = " ";
                            break;
                        case ".tab":
                            this.delim = "\t";
                            break;
                    }
                }
                return this.delim;
            }
        }
        /// <summary>Gets the connection string to the Microsoft Access or Excel database based on the version of Office on the host computer.</summary>
        /// <returns>Returns the connection string.</returns>
        /// <remarks>The version of the Access database is determined by the procedure: MajorVersion()</remarks>
        public string ConnectionString
        {
            get
            {
                // Exit immediately if connStr is built already...
                if (!string.IsNullOrEmpty(connStr))
                {
                    return connStr;
                }

                string Provider = "";
                string ExtendedProperties = "";

                // Database path and extension
                string DataSource = "Data source=" + dbpath + ";";

                // Provider string and extended properties
                switch (instType)
                {
                    case MSType.Access:
                        switch (MajorVersion)
                        {
                            //Case -1, 0 'Not found
                            //    MessageBox.Show("Microsoft Access must be installed on your computer before MODSIM can export its output.")
                            //    Return ""
                            //Case 1, 2, 7, 8, 9, 10 ' Access 1.1, 2.0, for Windows 95, 97, 2000, 2002, 2003
                            //    MessageBox.Show("Any Microsoft Access version before 2003 is not supported by MODSIM. Contact the MODSIM team.")
                            //    Return ""
                            case 11:
                                // Access 2003
                                Provider = "Provider=Microsoft.Jet.OLEDB.4.0;";
                                break;
                            case 12:
                            case 14:
                                // Access 2007, 2010
                                Provider = "Provider=Microsoft.ACE.OLEDB.12.0;";
                                break;
                            default:
                                // Newer or older or not found, default should be the newest version of Microsoft OLEDB Database Engine; MODSIM should still work without Office being installed
                                Provider = "Provider=Microsoft.ACE.OLEDB.12.0;";
                                break;
                        }
                        break;
                    case MSType.Excel:
                    case MSType.Text:
                        switch (MajorVersion)
                        {
                            //Case -1, 0 'Not found
                            //    MessageBox.Show("Microsoft Excel must be installed on your computer before MODSIM can export its output.")
                            //    Return ""
                            //Case 2, 3, 4, 5, 7 ' Excel 2.0, 3.0, 4.0, 5.0, for Windows 95
                            //    MessageBox.Show("Any Microsoft Excel version before 2003 is not supported by MODSIM. Contact the MODSIM team.")
                            //    Return ""
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                                // Excel 97, 2000, 2002, 2003
                                ExtendedProperties = ExtendedPropertyString("Excel 8.0");
                                Provider = "Provider=Microsoft.Jet.OLEDB.4.0;";
                                break;
                            case 12:
                            case 14:
                                // Excel 2007, 2010
                                ExtendedProperties = ExtendedPropertyString("Excel 12.0");
                                Provider = "Provider=Microsoft.ACE.OLEDB.12.0;";
                                break;
                            default:
                                // Newer or older or not found, default should be the newest version of Microsoft OLEDB Database Engine; MODSIM should still work without Office being installed
                                ExtendedProperties = ExtendedPropertyString("Excel 12.0");
                                Provider = "Provider=Microsoft.ACE.OLEDB.12.0;";
                                break;
                        }
                        if (instType == MSType.Text)
                        {
                            ExtendedProperties = ExtendedPropertyString("text");
                        }
                        break;
                    default:
                        return "";
                }
                // Even if Office is a version older than 2007, if this is a 64-bit process, it needs to use a version greater than Microsoft.Jet.OLEDB.4.0
                if (OSInfo.Is64BitProcess)
                {
                    Provider = "Provider=Microsoft.ACE.OLEDB.12.0;";
                }

                return Provider + DataSource + ExtendedProperties;
            }
        }

        // Converter functions
        /// <summary>Determines the name of the executable file of the MS Office component defined by the instance of MSType passed to the constructor.</summary>
        /// <returns>Returns the name of the executable file for the MS Office component defined by MSType.</returns>
        /// <remarks>If the current instance is MSType.Text then this returns "excel.exe"</remarks>
        public override string ToString()
        {
            return ToString(instType);
        }
        /// <summary>Determines the name of the executable file of the MS Office component defined by the instance of MSType passed to the constructor.</summary>
        /// <param name="type">Specifies the Microsoft Office component.</param>
        /// <returns>Returns the name of the executable file for the MS Office component defined by MSType.</returns>
        /// <remarks>If the current instance is MSType.Text then this returns "excel.exe"</remarks>
        public static string ToString(MSType type)
        {
            switch (type)
            {
                case MSType.Access:
                    return "MSACCESS.EXE";
                case MSType.Excel:
                    return "excel.exe";
                case MSType.Word:
                    return "winword.exe";
                case MSType.PowerPoint:
                    return "powerpnt.exe";
                case MSType.Text:
                    return "excel.exe";
                default:
                    throw new Exception("When converting the Microsoft component type to a string, the function was given an unrecognized type. This should not happen!");
            }
        }
        /// <summary>Gets the executable path for the current instance of the MS Office Component.</summary>
        /// <returns>Returns the full path to the executable of the current instance of the MS Office Component.</returns>
        public string MSOfficeExePath()
        {
            return MSOfficeExePath(instType);
        }
        /// <summary>Gets the executable path for the current instance of the MS Office Component.</summary>
        /// <param name="type">Specifies the Microsoft Office component.</param>
        /// <returns>Returns the full path to the executable of the current instance of the MS Office Component.</returns>
        public static string MSOfficeExePath(MSType type)
        {
            try
            {
                string name = ToString(type);
                RegistryKey regKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\" + name, false);
                if (regKey == null)
                {
                    return "";
                }
                return regKey.GetValue("").ToString();
            }
            catch
            {
                return "";
            }
        }
        /// <summary>Determines whether a specified Microsoft Office component is installed.</summary>
        /// <param name="type">Specifies the Microsoft Office component.</param>
        /// <returns>Returns true if the component is installed. Otherwise, returns false.</returns>
        /// <remarks>Components are checked by determining whether a specific registry exists on the local machine.</remarks>
        public static bool MSOfficeComponentIsInstalled(MSType type)
        {
            return !string.IsNullOrEmpty(MSOfficeExePath(type));
        }


        // Builder functions
        /// <summary>Builds the header portion (HDR) of the connection string (for text files and excel files only).</summary>
        /// <returns>Returns the header portion of the connection string.</returns>
        /// <remarks>May look like this: HDR=Yes</remarks>
        public string HeaderText()
        {
            if (instType != MSType.Text && instType != MSType.Excel)
            {
                return "";
            }
            if (HasHeader)
            {
                return "HDR=Yes";
            }
            else
            {
                return "HDR=No";
            }
        }
        /// <summary>The text for the delimiter portion (FMT) of a connection string (for text files only).</summary>
        /// <returns>Returns the delimiter text.</returns>
        /// <remarks>May look like this: FMT=Delimited(,)</remarks>
        public string DelimiterText()
        {
            string functionReturnValue = null;
            if (instType != MSType.Text)
            {
                return "";
            }
            functionReturnValue = "";
            if (instType == MSType.Text)
            {
                functionReturnValue = "FMT=Delimited";
                if (!string.IsNullOrEmpty(delim))
                {
                    functionReturnValue += "(" + delim + ")";
                }
            }
            return functionReturnValue;
        }
        /// <summary>Builds the HDR and FMT parts of the database connection string... Includes semicolons (;) at the beginning of each statement.</summary>
        /// <returns>Returns the HDR and FMT parts of the connection string.</returns>
        /// <remarks>May look like this: ;HDR=Yes;FMT=Delimited(,)</remarks>
        public string AppendHeaderAndDelimiterText()
        {
            string functionReturnValue = null;
            functionReturnValue = "";
            string tempstr = HeaderText();
            if (!string.IsNullOrEmpty(tempstr))
            {
                functionReturnValue += ";" + tempstr;
            }
            tempstr = DelimiterText();
            if (!string.IsNullOrEmpty(tempstr))
            {
                functionReturnValue += ";" + tempstr;
            }
            return functionReturnValue;
        }
        /// <summary>Builds a connection string for a text file within a query statement.</summary>
        /// <returns>Returns the connection string.</returns>
        /// <remarks>May look something like this: 'text;database=SomeDirectory;HDR=Yes;FMT=Delimited(,)'</remarks>
        public string TextConnStringInQuery()
        {
            return "[text;database=" + Path.GetDirectoryName(dbpath) + "\\" + AppendHeaderAndDelimiterText() + "].[" + Path.GetFileName(dbpath) + "]";
        }
        /// <summary>Builds the extended property portion of the connection string if desired.</summary>
        /// <param name="DBType"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public string ExtendedPropertyString(string DBType)
        {
            if (instType != MSType.Excel && instType != MSType.Text)
            {
                return "";
            }
            switch (Extension)
            {
                case ".xlsx":
                    DBType += " Xml";
                    break;
                case ".xlsm":
                    DBType += " Macro";
                    break;
            }
            return "Extended Properties=\"" + DBType + AppendHeaderAndDelimiterText() + "\";";
        }
        /// <summary>Returns the filter string for a open or save file dialog box given a MSType.</summary>
        /// <param name="SpecifiedMSType">The type of files to look for.</param>
        /// <returns>Returns the filter string for a open or save file dialog box given a MSType.</returns>
        public static string FilterString(MSType SpecifiedMSType)
        {
            string[] str = null;
            string begstr = null;
            string endstr = "";
            switch (SpecifiedMSType)
            {
                case MSType.Access:
                    begstr = "Access Database Files (";
                    str = AccessExtensions;
                    break;
                case MSType.Excel:
                    begstr = "Excel Files (";
                    str = ExcelExtensions;
                    break;
                case MSType.Word:
                    begstr = "Word Files (";
                    str = WordExtensions;
                    break;
                case MSType.PowerPoint:
                    begstr = "PowerPoint Files (";
                    str = PowerPointExtensions;
                    break;
                case MSType.Text:
                    begstr = "Text Files (";
                    str = TextExtensions;
                    break;
                default:
                    return "";
            }

            if (str.Length >= 1)
            {
                begstr += "*" + str[0];
                endstr += "*" + str[0];
            }
            for (int i = 1; i < str.Length; i++)
            {
                begstr += ", *" + str[i];
                endstr += ";*" + str[i];
            }

            return begstr + ")|" + endstr;
        }

    }
}
