using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MB = System.Reflection.MethodBase;
using TC.Functions;
using System.Runtime.CompilerServices;

namespace TC.Data
{
    #region Backup 22.11.2022

    //// Connection (10.11.2022, SME)
    //public class SqlDbConnection : TC.Classes.ClassWithLogging
    //{
    //    #region SHARED

    //    // SHARED: Get Connection-String (10.11.2022, SME)
    //    public static string GetConnectionString(SqlConnection connection)
    //    {
    //        if (connection == null) return "?";
    //        else return connection.ConnectionString;
    //    }

    //    #endregion

    //    #region General

    //    // Variables
    //    public readonly string Server;
    //    public readonly string Database;
    //    public readonly string User;
    //    private readonly string Password;
    //    internal readonly SqlConnection Connection;

    //    // Constants
    //    private const string CS_Trusted = "Server={SRV};Database={DB};Trusted_Connection=True;";
    //    private const string CS_User = "Server={SRV};Database={DB};User Id={USR};Password={PW};";
    //    public const string DEF_Filter_NoRows = "0 <> 0";

    //    // New Instance: Trusted Connection (10.11.2022, SME)
    //    public SqlDbConnection(string server, string database)
    //    {
    //        // error-handling
    //        if (string.IsNullOrEmpty(server))
    //            throw new ArgumentNullException(nameof(server));
    //        else if (string.IsNullOrEmpty(database))
    //            throw new ArgumentNullException(nameof(database));

    //        // set local properties
    //        Server = server;
    //        Database = database;
    //        Connection = GetNewConnection();

    //        // add event-handlers
    //        Connection.StateChange += Connection_StateChange;
    //        Connection.InfoMessage += Connection_InfoMessage;
    //        Connection.Disposed += Connection_Disposed;
    //    }

    //    // New Instance with User + Password (10.11.2022, SME)
    //    public SqlDbConnection(string server, string database, string user, string password) : this(server, database)
    //    {
    //        // error-handling
    //        if (string.IsNullOrEmpty(server))
    //            throw new ArgumentNullException(nameof(server));
    //        else if (string.IsNullOrEmpty(database))
    //            throw new ArgumentNullException(nameof(database));
    //        else if (!string.IsNullOrEmpty(user))
    //            throw new ArgumentNullException(nameof(user));
    //        else if (!string.IsNullOrEmpty(password))
    //            throw new ArgumentNullException(nameof(password));

    //        // set local properties
    //        Server = server;
    //        Database = database;
    //        User = user;
    //        Password = password;
    //        Connection = GetNewConnection();

    //        // add event-handlers
    //        Connection.StateChange += Connection_StateChange;
    //        Connection.InfoMessage += Connection_InfoMessage;
    //        Connection.Disposed += Connection_Disposed;
    //    }

    //    #endregion

    //    #region Event-Handling

    //    // Event-Handler: Connection-State changed (10.11.2022, SME)
    //    private void Connection_StateChange(object sender, StateChangeEventArgs e)
    //    {
    //        const string Output = "Connection-State changed: {0} -> {1}, CS = {2}";
    //        Log(string.Format(Output, e.OriginalState, e.CurrentState, GetConnectionString(sender as SqlConnection)));
    //    }

    //    // Event-Handler: Info-Message of Connection (10.11.2022, SME)
    //    private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
    //    {
    //        const string Output = "Connection Info-Message: CS = {0}, Message = {1}, Errors = {2}";
    //        Log(string.Format(Output, GetConnectionString(sender as SqlConnection), e.Message, "?"));
    //    }

    //    // Event-Handler: Connection disposed (10.11.2022, SME)
    //    private void Connection_Disposed(object sender, EventArgs e)
    //    {
    //        const string Output = "Connection disposed: CS = {0}";
    //        Log(string.Format(Output, GetConnectionString(sender as SqlConnection)));
    //    }

    //    #endregion

    //    #region Properties

    //    // Connection-State (10.11.2022, SME)
    //    public ConnectionState ConnectionState
    //    {
    //        get
    //        {
    //            return Connection.State;
    //        }
    //    }

    //    // Is Trusted (10.11.2022, SME)
    //    public bool IsTrusted
    //    {
    //        get
    //        {
    //            return string.IsNullOrEmpty(User);
    //        }
    //    }

    //    #endregion

    //    #region Connection-Handling

    //    // Get Connection-String (10.11.2022, SME)
    //    private string GetConnnectionString()
    //    {
    //        // initialize connection-string
    //        string cs = (IsTrusted) ? CS_Trusted : CS_User;

    //        // replace server + database
    //        cs = cs.Replace("{SRV}", Server);
    //        cs = cs.Replace("{DB}", Database);

    //        // replace user + password
    //        if (!IsTrusted)
    //        {
    //            cs = cs.Replace("{USR}", User);
    //            cs = cs.Replace("{PW}", Password);
    //        }

    //        // return
    //        return cs;
    //    }

    //    // Get new Connection (10.11.2022, SME)
    //    public SqlConnection GetNewConnection()
    //    {
    //        return new SqlConnection(GetConnnectionString());
    //    }

    //    // Connect (10.11.2022, SME)
    //    public bool Connect()
    //    {
    //        try
    //        {
    //            // exit if already connected
    //            if (ConnectionState == ConnectionState.Open) return true;

    //            // connect
    //            Connection.Open();

    //            // return
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            LogError(ex, MB.GetCurrentMethod());
    //            CoreFC.ThrowError(ex); throw ex;
    //        }
    //    }

    //    // Disconnect (10.11.2022, SME)
    //    public bool Disconnect()
    //    {
    //        try
    //        {
    //            // exit if already disconnected
    //            if (ConnectionState == ConnectionState.Closed) return true;

    //            // disconnect
    //            Connection.Close();

    //            // return
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            LogError(ex, MB.GetCurrentMethod());
    //            CoreFC.ThrowError(ex); throw ex;
    //        }
    //    }

    //    #endregion

    //    #region Methods

    //    // Get new opened Connection (22.11.2022, SME)
    //    public OpenedConnection GetNewOpenedConnection() => new(Connection);

    //    // Get Data-Table from SQL (11.11.2022, SME)
    //    public DataTable GetDataTableFromSql(string sql, string tableName, MissingSchemaAction missingSchemaAction = MissingSchemaAction.AddWithKey)
    //    {
    //        try
    //        {
    //            // error-handling
    //            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));
    //            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

    //            // create table
    //            DataTable table = new DataTable(tableName);

    //            // use opened connection
    //            using (OpenedSqlDbConnection con = new OpenedSqlDbConnection(this))
    //            {
    //                // use adapter
    //                using (SqlDataAdapter adapter = new SqlDataAdapter(sql, Connection))
    //                {
    //                    adapter.MissingSchemaAction = missingSchemaAction;
    //                    adapter.Fill(table);
    //                }
    //            }

    //            // return
    //            table.AcceptChanges();
    //            return table;
    //        }
    //        catch (Exception ex)
    //        {
    //            LogError(ex, MB.GetCurrentMethod());
    //            CoreFC.ThrowError(ex); throw ex;
    //        }
    //    }

    //    // Get empty Data-Table by Table-Name (11.11.2022, SME)
    //    public DataTable GetEmptyTable(string tableName, MissingSchemaAction missingSchemaAction = MissingSchemaAction.AddWithKey)
    //    {
    //        try
    //        {
    //            // error-handling
    //            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

    //            // set sql
    //            string sql = "SELECT * FROM dbo.[" + tableName + "] WHERE " + DEF_Filter_NoRows;

    //            // return
    //            return GetDataTableFromSql(sql, tableName);
    //        }
    //        catch (Exception ex)
    //        {
    //            LogError(ex, MB.GetCurrentMethod());
    //            CoreFC.ThrowError(ex); throw ex;
    //        }
    //    }

    //    #endregion

    //}

    #endregion

    // SQL-DB-Connection (16.11.2022, SRM)
    public class SqlDbConnection : IDisposable
    {

        #region General

        // New Instance from Server + Database + User + Password (16.11.2022, SRM)
        public SqlDbConnection(string server, string database, string user = "", string password = "", bool mars = false)
        {
            // error-handling
            if (string.IsNullOrEmpty(server)) throw new ArgumentNullException("Server");
            if (string.IsNullOrEmpty(database)) throw new ArgumentNullException("Database");
            if (string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(user)) throw new ArgumentNullException("Password");

            // set local properties
            Server = server;
            Database = database;
            _User = user;
            Password = password;
            MARS = mars;

            // set connection-string
            if (string.IsNullOrEmpty(user))
            {
                // trusted
                ConnectionString = CS_Trusted.Replace("{SRV}", server).Replace("{DB}", database);
            }
            else
            {
                // with user + password
                ConnectionString = CS_User.Replace("{SRV}", server).Replace("{DB}", database).Replace("{USR}", user).Replace("{PW}", password);
            }

            // handle mars
            if (mars) ConnectionString += "MultipleActiveResultSets=True;";

            // set connection
            Connection = new SqlConnection(ConnectionString);
        }

        // ToString
        public override string ToString()
        {
            return Database + " @ " + Server;
        }

        // Dispose (17.11.2022, SRM)
        public void Dispose()
        {
            Disconnect();
            Connection.Dispose();
        }

        #endregion

        #region Constants

        #region Connection-Strings

        private const string CS_Context = "context connection=true";
        private const string CS_Trusted = "Server={SRV};Database={DB};Trusted_Connection=True;";
        private const string CS_User = "Server={SRV};Database={DB};User Id={USR};Password={PW};";

        #endregion

        #region Constant SQLs

        // Constant SQL: Is existing Temp-Table (13.11.2022, SRM)
        private const string DEF_SQL_IsExistingTempTable = @"IF OBJECT_ID('tempdb..#{0}') IS NOT NULL SELECT 1 ELSE SELECT 0;";

        // Constant SQL: Drop Temp-Table (13.11.2022, SRM)
        private const string DEF_SQL_DropTempTable = @"IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}";

        #endregion

        public const string DEF_Filter_NoRows = "0 <> 0";

        #endregion

        #region Event-Handling

        // Event-Handler: Fill-Error on Adapter
        private void Adapter_FillError(object sender, FillErrorEventArgs e)
        {
            CoreFC.DPrint("ERROR: Fill-Error on Adapter => " + e.Errors.Message);
        }

        #endregion

        #region Properties

        // Server
        public readonly string Server;

        // Database
        public readonly string Database;

        // User
        private string _User;
        public string User
        {
            get
            {
                if (string.IsNullOrEmpty(_User))
                    _User = GetUserName();
                return _User;
            }
        }

        // Password
        private readonly string Password;

        // MARS
        public readonly bool MARS;

        // ConnectionString
        private readonly string ConnectionString;

        // Connection
        internal readonly SqlConnection Connection;

        // State
        public ConnectionState State
        {
            get { return Connection.State; }
        }

        #endregion

        #region Methods

        // get new opened connection (16.11.2022, SRM)
        public OpenedConnection GetNewOpenedConnection()
        {
            return new OpenedConnection(Connection);
        }

        // Connect (14.11.2022, SRM)
        public void Connect()
        {
            // store connection-state
            var connectionState = State;

            try
            {
                // handle connection-state
                switch (State)
                {
                    case ConnectionState.Closed:
                        Connection.Open();
                        break;
                    case ConnectionState.Open:
                        break;
                    case ConnectionState.Connecting:
                        break;
                    case ConnectionState.Executing:
                        break;
                    case ConnectionState.Fetching:
                        break;
                    case ConnectionState.Broken:
                        Connection.Close();
                        Connection.Open();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string msg = "Error while opening connection." + Environment.NewLine
                           + "State at start = " + connectionState.ToString() + Environment.NewLine
                           + "Current state = " + Connection.State.ToString() + Environment.NewLine
                           + "Connection-String = " + Connection.ConnectionString;
                throw new Exception(msg, ex);
                //throw new TC.Errors.CoreError(msg, ex);
            }
        }

        // Disconnect (16.11.2022, SRM)
        public void Disconnect()
        {
            try
            {
                Connection.Close();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Server-Name (11.11.2022, SME)
        private string GetServerName()
        {
            try
            {
                return ExecuteScalar("SELECT @@SERVERNAME;") as string;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while retrieving server-name!", ex);
            }
        }

        // Get DB-Name (11.11.2022, SME)
        private string GetDBName()
        {
            try
            {
                return ExecuteScalar("SELECT DB_NAME();") as string;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while retrieving database-name!", ex);
            }
        }

        // Get User-Name (16.11.2022, SME)
        private string GetUserName()
        {
            try
            {
                return ExecuteScalar("SELECT SUSER_SNAME();") as string;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while retrieving user-name!", ex);
            }
        }

        // Get empty Data-Table by Table-Name (11.11.2022, SME)
        public DataTable GetEmptyTable(string tableName, MissingSchemaAction missingSchemaAction = MissingSchemaAction.AddWithKey)
        {
            return ExecuteQueryFromSql("SELECT * FROM " + tableName + " WHERE " + DEF_Filter_NoRows, tableName, false, missingSchemaAction);
        }

        // Get Row-Count (25.11.2022, SME)
        public int GetRowCount(string tableName, string filter = "")
        {
            try
            {
                // set sql
                var sql = "SELECT COUNT(*) FROM " + tableName;
                if (!string.IsNullOrEmpty(filter))
                    sql += " WHERE " + filter;

                // get scalar-value
                var value = ExecuteScalar(sql);

                // return
                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get new Command (26.11.2022, SME)
        public SqlCommand GetNewCommand(string sql)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));
            return new(sql, Connection);
        }

        // Begin Transaction (26.11.2022, SME)
        public SqlTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }

        // Get new Bulk Copy (26.11.2022, SME)
        public SqlBulkCopy GetNewBulkCopy(SqlBulkCopyOptions options = SqlBulkCopyOptions.Default)
        {
            return new(Connection, options, null);
        }


        #endregion

        #region Execute (Non-Query, Scalar, Query)

        #region Execute Scalar

        // Execute Scalar with SQL (11.11.2022, SME)
        public object ExecuteScalar(string sql)
        {
            try
            {
                using (var con = GetNewOpenedConnection())
                {
                    using (var cmd = new SqlCommand(sql, con.Connection))
                    {
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }

        }

        #endregion

        #region Execute Non-Query

        // Execute Non-Query with SQL (13.11.2022, SRM)
        public int ExecuteNonQuery(string sql)
        {
            try
            {
                using (var con = GetNewOpenedConnection())
                {
                    using (var cmd = new SqlCommand(sql, con.Connection))
                    {
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Fill Query

        // Fill Query into Table
        private void FillQueryIntoTable(string sql, DataTable table, bool acceptChanges = false, MissingSchemaAction missingSchemaAction = MissingSchemaAction.Add)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException("SQL");
                if (table == null) throw new ArgumentNullException(nameof(table));

                // use opened connection
                using (var con = GetNewOpenedConnection())
                {
                    // use adapter
                    using (var adapter = new SqlDataAdapter(sql, con.Connection))
                    {
                        // set properties
                        adapter.MissingSchemaAction = missingSchemaAction;

                        // fill table
                        try
                        {
                            // add event-handlers
                            adapter.FillError += Adapter_FillError;

                            // fill
                            adapter.Fill(table);

                            // handle auto-id
                            if (table.PrimaryKey.Length == 1 && table.PrimaryKey.First().AutoIncrement)
                            {
                                var pk = table.PrimaryKey.First();
                                pk.AutoIncrementSeed = -1;
                                pk.AutoIncrementStep = -1;
                                pk.AutoIncrementSeed = -1;
                                pk.AutoIncrement = false;
                                pk.AutoIncrement = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            CoreFC.ThrowError(ex); throw ex;
                        }
                        finally
                        {
                            // remove event-handlers
                            adapter.FillError -= Adapter_FillError;
                        }

                        // accept changes
                        if (acceptChanges) table.AcceptChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Execute Query

        // Execute Query from SQL (13.11.2022, SRM)
        public DataTable ExecuteQueryFromSql(string sql, string tableName, bool acceptChanges = false, MissingSchemaAction missingSchemaAction = MissingSchemaAction.Add)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException("SQL");
                if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("TableName");

                // create table
                var table = new DataTable(tableName);

                // fill table
                FillQueryIntoTable(sql, table, acceptChanges, missingSchemaAction);

                // return
                return table;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Execute Query from Table-Name + Filter (13.11.2022, SRM)
        /// <summary>
        /// Execute Query from Table-Name + Filter
        /// </summary>
        /// <param name="tableName">Table-Name</param>
        /// <param name="filter">Filter</param>
        /// <param name="connection">Connection</param>
        /// <param name="acceptChanges">Flag that defines if changes will be accepted before return</param>
        /// <param name="missingSchemaAction">Missing-Schema-Action:
        /// Add => Adds primary-key-information, but no max. length
        /// AddWithKey => Adds primary-key-information, and also max. length
        /// Ignore => Ignores schema-information, e.g. getting data from temp-table will return empty table with no columns
        /// Error => Raises an error when schema-information are missing</param>
        /// <returns></returns>
        public DataTable ExecuteQuery(string tableName, string filter, bool acceptChanges = false, MissingSchemaAction missingSchemaAction = MissingSchemaAction.Add)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("TableName");

                // get empty table
                var table = GetEmptyTable(tableName, missingSchemaAction);

                // set sql
                string sql = "SELECT * FROM " + tableName;
                if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;

                // fill table
                FillQueryIntoTable(sql, table, acceptChanges, missingSchemaAction);

                // return
                return table;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Execute Query on Temp-Table (14.11.2022, SRM)
        public DataTable ExecuteQueryOnTempTable(string tempTableName, bool acceptChanges = false)
        {
            return ExecuteQuery("#" + tempTableName, string.Empty, acceptChanges, MissingSchemaAction.AddWithKey);
        }

        #endregion

        #endregion

        #region Temp-Table-Handling

        #region Is existing Temp-Table

        // Is existing Temp-Table (13.11.2022, SRM)
        public bool IsExistingTempTable(string tempTableName)
        {
            // exit-handling
            if (string.IsNullOrEmpty(tempTableName)) return false;

            // set sql
            string sql = string.Format(DEF_SQL_IsExistingTempTable, tempTableName);

            // get value
            var value = ExecuteScalar(sql);

            // return
            if (value == null) return false;
            else return value.ToString().Equals("1");
        }

        #endregion

        #region Drop Temp Table

        // Drop Temp Table (13.11.2022, SRM)
        public void DropTempTable(string tempTableName)
        {
            // exit-handling
            if (string.IsNullOrEmpty(tempTableName)) return;

            // set sql
            string sql = string.Format(DEF_SQL_DropTempTable, tempTableName);

            // execute
            ExecuteNonQuery(sql);
        }

        #endregion

        #endregion

        // Get Insert-Command (25.11.2022, SME)
        private SqlCommand GetInsertCommand(DataTable table, bool returnNewID = false)
        {
            try
            {
                // exit-handling
                if (table == null) throw new ArgumentNullException(nameof(table));

                // store columns without auto-increment
                var columns = table.Columns.OfType<DataColumn>().Where(col => !col.AutoIncrement).ToList();

                // set sql
                var sqlInsert = "INSERT INTO " + table.TableName + " (";
                var sqlValues = "VALUES (";
                columns.ForEach(col =>
                {
                    sqlInsert += col.ColumnName + ", ";
                    sqlValues += "@" + col.ColumnName + ", ";
                });
                sqlInsert = sqlInsert.Substring(0, sqlInsert.Length - 2) + ")";
                sqlValues = sqlValues.Substring(0, sqlValues.Length - 2) + ")";
                var sql = sqlInsert + Environment.NewLine + sqlValues;

                // add "return-auto-id"-statement
                if (returnNewID)
                {
                    // store auto-id-column
                    DataColumn autoIdPK = null;
                    if (table.PrimaryKey.Length == 1 && table.PrimaryKey.First().AutoIncrement)
                        autoIdPK = table.PrimaryKey.First();

                    if (autoIdPK != null)
                        sql += Environment.NewLine + "SELECT IDENT_CURRENT(" + DataFC.GetSqlString(table.TableName) + ")";
                }

                // create command
                var cmd = new SqlCommand(sql, Connection);

                // add parameters
                columns.ForEach(col =>
                {
                    SqlDbType sqlDbType = DataFC.GetSqlDbType(col.DataType);
                    cmd.Parameters.Add("@" + col.ColumnName, sqlDbType);
                });

                // return
                return cmd;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Delete-Command (25.11.2022, SME)
        private SqlCommand GetDeleteCommand(DataTable table)
        {
            try
            {
                // exit-handling
                if (table == null) throw new ArgumentNullException(nameof(table));

                // store pk-columns
                var pkColumns = table.PrimaryKey;
                // check pk-columns
                if (!pkColumns.Any())
                    throw new Exception("Row cannot be deleted because primary-key not set for table '" + table.TableName + "'.");

                // set sql
                var sql = new StringBuilder();
                sql.AppendLine("DELETE FROM " + table.TableName);
                sql.AppendLine("WHERE");
                foreach (var pkColumn in pkColumns)
                {
                    sql.Append("     " + pkColumn.ColumnName + " = @PK_" + pkColumn.ColumnName + " AND ");
                }
                sql = sql.Remove(sql.Length - " AND ".Length, " AND ".Length);

                // create delete-command
                var cmd = new SqlCommand(sql.ToString(), Connection);
                
                // add parameters
                pkColumns.ToList().ForEach(col =>
                {
                    SqlDbType sqlDbType = DataFC.GetSqlDbType(col.DataType);
                    cmd.Parameters.Add("@PK_" + col.ColumnName, sqlDbType);
                });

                // return
                return cmd;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Save Changes in Row (25.11.2022, SME)
        public void SaveChanges(DataRow row)
        {
            try
            {
                // exit-handling
                if (row == null) throw new ArgumentNullException(nameof(row));
                if (row.Table == null) throw new ArgumentNullException(nameof(row.Table));
                if (row.RowState == DataRowState.Unchanged) return;
                if (row.RowState == DataRowState.Detached)
                    throw new Exception("Unbehandelter Row-State: " + row.RowState);

                // declarations
                DataColumn[] pkColumns;

                // use opened connection
                using (var con = GetNewOpenedConnection())
                {
                    // store table
                    var table = row.Table;

                    // handle row-state
                    switch (row.RowState)
                    {
                        case DataRowState.Added:

                            #region Insert

                            // store auto-id-column
                            DataColumn autoIdPK = null;
                            if (table.PrimaryKey.Length == 1 && table.PrimaryKey.First().AutoIncrement)
                                autoIdPK = table.PrimaryKey.First();

                            // save changes by other method
                            SaveChanges(row, GetInsertCommand(table, autoIdPK != null), true);

                            #endregion

                            break;
                        case DataRowState.Modified:

                            #region Update

                            // store pk-columns
                            pkColumns = table.PrimaryKey;
                            // check pk-columns
                            if (!pkColumns.Any())
                                throw new Exception("Row cannot be updated because primary-key not set for table '" + table.TableName + "'.");

                            // store changed columns
                            var changedColumns = DataFC.GetChangedColumns(row);

                            // skip if no columns have changed
                            if (!changedColumns.Any()) return;

                            // set sql
                            var sql = new StringBuilder();
                            sql.AppendLine("UPDATE " + table.TableName);
                            sql.Append("SET");
                            foreach (DataColumn column in changedColumns)
                            {
                                if (pkColumns.Contains(column)) continue;

                                sql.Append(Environment.NewLine + "     " + column.ColumnName + " = @" + column.ColumnName + ",");
                            }
                            sql = sql.Remove(sql.Length - ",".Length, ",".Length);
                            sql.Append(Environment.NewLine);
                            sql.AppendLine("WHERE");
                            foreach (var pkColumn in pkColumns)
                            {
                                sql.Append("     " + pkColumn.ColumnName + " = @PK_" + pkColumn.ColumnName + " AND ");
                            }
                            sql = sql.Remove(sql.Length - " AND ".Length, " AND ".Length);

                            // create update-command
                            using (SqlCommand cmd = new SqlCommand(sql.ToString(), Connection))
                            {
                                // add parameters
                                changedColumns.ToList().ForEach(col =>
                                {
                                    SqlDbType sqlDbType = DataFC.GetSqlDbType(col.DataType);
                                    cmd.Parameters.Add("@" + col.ColumnName, sqlDbType).Value = row[col];
                                });
                                pkColumns.ToList().ForEach(col =>
                                {
                                    SqlDbType sqlDbType = DataFC.GetSqlDbType(col.DataType);
                                    cmd.Parameters.Add("@PK_" + col.ColumnName, sqlDbType).Value = row[col];
                                });

                                var result = cmd.ExecuteNonQuery();
                                if (result != 1)
                                    CoreFC.DPrint("Error while updating");
                                else
                                    row.AcceptChanges();
                            }

                            #endregion

                            break;
                        case DataRowState.Deleted:

                            #region Delete

                            // save changes by other method
                            SaveChanges(row, GetDeleteCommand(table), true);

                            #endregion

                            break;
                        default:
                            throw new Exception("Unbehandelter Row-State: " + row.RowState);
                    }

                    // accept changes (15.12.2022, SME)
                    row.AcceptChanges();
                }

            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Save Changes in Row with Command (25.11.2022, SME)
        private void SaveChanges(DataRow row, SqlCommand cmd, bool disposeCommandAtEnd)
        {
            try
            {
                // exit-handling
                if (row == null) throw new ArgumentNullException(nameof(row));
                if (cmd == null) throw new ArgumentNullException(nameof(cmd));
                if (row.Table == null) throw new ArgumentNullException(nameof(row.Table));
                if (row.RowState == DataRowState.Unchanged) return;
                if (row.RowState == DataRowState.Detached)
                    throw new Exception("Unbehandelter Row-State: " + row.RowState);

                // declarations
                DataColumn[] pkColumns;

                // use opened connection
                using (var con = GetNewOpenedConnection())
                {
                    // store table
                    var table = row.Table;

                    // handle row-state
                    switch (row.RowState)
                    {
                        case DataRowState.Added:

                            #region Insert

                            // store columns without auto-increment
                            var columns = table.Columns.OfType<DataColumn>().Where(col => !col.AutoIncrement).ToList();

                            // store auto-id-column
                            DataColumn autoIdPK = null;
                            if (table.PrimaryKey.Length == 1 && table.PrimaryKey.First().AutoIncrement)
                                autoIdPK = table.PrimaryKey.First();

                            try
                            {
                                // set parameter-values
                                columns.ForEach(col =>
                                {
                                    try
                                    {
                                        cmd.Parameters["@" + col.ColumnName].Value = row[col];
                                    }
                                    catch (Exception ex)
                                    {
                                        CoreFC.ThrowError(ex); throw ex;
                                    }
                                });

                                // execute
                                if (autoIdPK == null)
                                {
                                    var result = cmd.ExecuteNonQuery();
                                    if (result != 1)
                                    {
                                        CoreFC.DPrint("Error while inserting");
                                    }
                                }
                                else
                                {
                                    var result = cmd.ExecuteScalar();
                                    if (result == null)
                                        CoreFC.DPrint("Auto-ID = NULL!");
                                    else if (result == DBNull.Value)
                                        CoreFC.DPrint("Auto-ID = DB-NULL!");
                                    else
                                    {
                                        var isReadOnly = autoIdPK.ReadOnly;
                                        try
                                        {
                                            if (isReadOnly) autoIdPK.ReadOnly = false;

                                            row[autoIdPK] = result;
                                        }
                                        catch (Exception ex)
                                        {
                                            CoreFC.DPrint("ERROR while setting auto-id: " + ex.Message);
                                        }
                                        finally
                                        {
                                            autoIdPK.ReadOnly = isReadOnly;
                                        }
                                    }

                                }

                                // accept changes
                                row.AcceptChanges();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }

                            #endregion

                            break;
                        case DataRowState.Deleted:

                            #region Delete

                            // store pk-columns
                            pkColumns = table.PrimaryKey;
                            // check pk-columns
                            if (!pkColumns.Any())
                                throw new Exception("Row cannot be deleted because primary-key not set for table '" + table.TableName + "'.");

                            try
                            {
                                // set parameter-values
                                pkColumns.ToList().ForEach(col =>
                                {
                                    try
                                    {
                                        cmd.Parameters["@PK_" + col.ColumnName].Value = row[col, DataRowVersion.Original];
                                    }
                                    catch (Exception ex)
                                    {
                                        CoreFC.ThrowError(ex); throw ex;
                                    }
                                });

                                // Execute
                                var result = cmd.ExecuteNonQuery();
                                if (result != 1)
                                {
                                    CoreFC.DPrint("Error while deleting");
                                }

                                // Accept Changes
                                row.AcceptChanges();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.DPrint("ERROR while deleting row: " + ex.Message);
                                CoreFC.ThrowError(ex); throw ex;
                            }

                            #endregion

                            break;
                        default:
                            throw new Exception("Unbehandelter Row-State: " + row.RowState);
                    }
                }

            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                if (disposeCommandAtEnd && cmd != null) cmd.Dispose();
            }
        }

        // Save Changes in Rows (25.11.2022, SME)
        public void SaveChanges(DataRow[] rows)
        {
            try
            {
                // exit-handling
                if (rows == null) throw new ArgumentNullException(nameof(rows));
                if (rows.Length == 0) return;

                // check if from different tables
                var tables = rows.Select(row => row.Table).Distinct().ToArray();
                if (tables.Length != 1)
                    throw new NotImplementedException("Das Speichern von Datensätzen von mehreren Tabellen ist nicht implementiert!");

                // set table
                var table = tables.First();
                if (table == null) throw new Exception("Tabelle nicht gesetzt!");

                // store auto-id-column
                DataColumn autoIdPK = null;
                if (table.PrimaryKey.Length == 1 && table.PrimaryKey.First().AutoIncrement)
                    autoIdPK = table.PrimaryKey.First();

                // use opened connection
                using (var con = GetNewOpenedConnection())
                {
                    // insert rows
                    var rowsToInsert = rows.Where(row => row.RowState == DataRowState.Added);
                    if (rowsToInsert.Any())
                    {
                        // create insert-command
                        using (SqlCommand cmd = GetInsertCommand(table, autoIdPK != null))
                        {
                            // loop throu rows
                            rowsToInsert.ToList().ForEach(row => SaveChanges(row, cmd, false));
                        }
                    }

                    // update rows
                    var rowsToUpdate = rows.Where(row => row.RowState == DataRowState.Modified);
                    if (rowsToUpdate.Any())
                    {
                        // loop throu rows
                        rowsToUpdate.ToList().ForEach(row => SaveChanges(row));
                    }

                    // delete rows
                    var rowsToDelete = rows.Where(row => row.RowState == DataRowState.Deleted);
                    if (rowsToDelete.Any())
                    {
                        /// use delete-command
                        using (SqlCommand cmd = GetDeleteCommand(table))
                        {
                            // loop throu rows
                            rowsToDelete.ToList().ForEach(row => SaveChanges(row, cmd, false));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Save Changes (23.11.2022, SME)
        //public void SaveChanges(DataTable table)
        //{
        //    try
        //    {
        //        // exit-handling
        //        if (table == null) return;
        //        if (table.Rows.Count == 0) return;

        //        // store auto-id-column
        //        DataColumn autoIdPK = null;
        //        if (table.PrimaryKey.Length == 1 && table.PrimaryKey.First().AutoIncrement)
        //            autoIdPK = table.PrimaryKey.First();

        //        // use opened connection
        //        using (var con = GetNewOpenedConnection())
        //        {
        //            // insert rows
        //            var rowsToInsert = table.Select("", "", DataViewRowState.Added);
        //            if (rowsToInsert.Any())
        //            {
        //                // store columns without auto-increment
        //                var columns = table.Columns.OfType<DataColumn>().Where(col => !col.AutoIncrement).ToList();

        //                // create insert-command
        //                using (SqlCommand cmd = GetInsertCommand(table, autoIdPK != null))
        //                {
        //                    // loop throu rows
        //                    foreach (var row in table.Select("", "", DataViewRowState.Added))
        //                    {
        //                        try
        //                        {
        //                            // set parameter-values
        //                            columns.ForEach(col =>
        //                            {
        //                                try
        //                                {
        //                                    cmd.Parameters["@" + col.ColumnName].Value = row[col];
        //                                }
        //                                catch (Exception ex)
        //                                {
        //                                    CoreFC.ThrowError(ex); throw ex;
        //                                }
        //                            });

        //                            // execute
        //                            if (autoIdPK == null)
        //                            {
        //                                var result = cmd.ExecuteNonQuery();
        //                                if (result != 1)
        //                                {
        //                                    CoreFC.DPrint("Error while inserting");
        //                                }
        //                            }
        //                            else
        //                            {
        //                                var result = cmd.ExecuteScalar();
        //                                if (result == null)
        //                                    CoreFC.DPrint("Auto-ID = NULL!");
        //                                else if (result == DBNull.Value)
        //                                    CoreFC.DPrint("Auto-ID = DB-NULL!");
        //                                else
        //                                {
        //                                    var isReadOnly = autoIdPK.ReadOnly;
        //                                    try
        //                                    {
        //                                        if (isReadOnly) autoIdPK.ReadOnly = false;

        //                                        row[autoIdPK] = result;
        //                                    }
        //                                    catch (Exception ex)
        //                                    {
        //                                        CoreFC.DPrint("ERROR while setting auto-id: " + ex.Message);
        //                                    }
        //                                    finally
        //                                    {
        //                                        autoIdPK.ReadOnly = isReadOnly;
        //                                    }
        //                                }

        //                            }

        //                            // accept changes
        //                            row.AcceptChanges();
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            CoreFC.ThrowError(ex); throw ex;
        //                        }
        //                    }
        //                }
        //            }

        //            // update rows
        //            var rowsToUpdate = table.Select("", "", DataViewRowState.ModifiedCurrent);
        //            if (rowsToUpdate.Any())
        //            {
        //                // store pk-columns
        //                var pkColumns = table.PrimaryKey;
        //                // check pk-columns
        //                if (!pkColumns.Any())
        //                    throw new Exception("Row cannot be updated because primary-key not set for table '" + table.TableName + "'.");

        //                // loop throu rows
        //                foreach (var row in rowsToUpdate)
        //                {
        //                    // store changed columns
        //                    var changedColumns = DataFC.GetChangedColumns(row);

        //                    // skip if no columns have changed
        //                    if (!changedColumns.Any()) continue;

        //                    // set sql
        //                    var sql = new StringBuilder();
        //                    sql.AppendLine("UPDATE " + table.TableName);
        //                    sql.Append("SET");
        //                    foreach (DataColumn column in changedColumns)
        //                    {
        //                        if (pkColumns.Contains(column)) continue;

        //                        sql.Append(Environment.NewLine + "     " + column.ColumnName + " = @" + column.ColumnName + ",");
        //                    }
        //                    sql = sql.Remove(sql.Length - ",".Length, ",".Length);
        //                    sql.Append(Environment.NewLine);
        //                    sql.AppendLine("WHERE");
        //                    foreach (var pkColumn in pkColumns)
        //                    {
        //                        sql.Append("     " + pkColumn.ColumnName + " = @PK_" + pkColumn.ColumnName + " AND ");
        //                    }
        //                    sql = sql.Remove(sql.Length - " AND ".Length, " AND ".Length);

        //                    // create update-command
        //                    using (SqlCommand cmd = new SqlCommand(sql.ToString(), Connection))
        //                    {
        //                        // add parameters
        //                        changedColumns.ToList().ForEach(col =>
        //                        {
        //                            SqlDbType sqlDbType = DataFC.GetSqlDbType(col.DataType);
        //                            cmd.Parameters.Add("@" + col.ColumnName, sqlDbType).Value = row[col];
        //                        });
        //                        pkColumns.ToList().ForEach(col =>
        //                        {
        //                            SqlDbType sqlDbType = DataFC.GetSqlDbType(col.DataType);
        //                            cmd.Parameters.Add("@PK_" + col.ColumnName, sqlDbType).Value = row[col];
        //                        });

        //                        var result = cmd.ExecuteNonQuery();
        //                        if (result != 1)
        //                            CoreFC.DPrint("Error while updating");
        //                        else
        //                            row.AcceptChanges();
        //                    }
        //                }
        //            }

        //            // delete rows
        //            var rowsToDelete = table.Select("", "", DataViewRowState.Deleted);
        //            if (rowsToDelete.Any())
        //            {
        //                // store pk-columns
        //                var pkColumns = table.PrimaryKey;
        //                // check pk-columns
        //                if (!pkColumns.Any())
        //                    throw new Exception("Row cannot be deleted because primary-key not set for table '" + table.TableName + "'.");

        //                // use delete-command
        //                using (SqlCommand cmd = GetDeleteCommand(table))
        //                {
        //                    // loop throu rows
        //                    foreach (var row in rowsToDelete)
        //                    {
        //                        try
        //                        {
        //                            // set parameter-values
        //                            pkColumns.ToList().ForEach(col =>
        //                            {
        //                                try
        //                                {
        //                                    cmd.Parameters["@PK_" + col.ColumnName].Value = row[col, DataRowVersion.Original];
        //                                }
        //                                catch (Exception ex)
        //                                {
        //                                    CoreFC.ThrowError(ex); throw ex;
        //                                }
        //                            });

        //                            // Execute
        //                            var result = cmd.ExecuteNonQuery();
        //                            if (result != 1)
        //                            {
        //                                CoreFC.DPrint("Error while deleting");
        //                            }

        //                            // Accept Changes
        //                            row.AcceptChanges();
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            CoreFC.DPrint("ERROR while deleting row: " + ex.Message);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFC.ThrowError(ex); throw ex;
        //    }
        //}
    }
}
