using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using TC.Classes;
using TC.Data;
using TC.Enums;
using TC.Extensions;

namespace TC.Functions
{

    // Data-Functions (13.11.2022, SRM)
    public static class DataFC
    {
        #region IMPORTANT

        /*
         * ALL code in this class MUST BE in sync between TC.Core + TC_SQL_CLR !!!
         * => when ever a change is made, it must be applied in both code (13.11.2022, SRM)
         */

        // Get SQL-Server depending on Network-Type (02.02.2024, SME)
        public static SqlServerEnum? GetSqlServerEnum()
        {
            try
            {
                // store netzwerk-typ
                var netzwerkTyp = CoreFC.GetNetzwerkTyp();
                switch (netzwerkTyp)
                {
                    case NetzwerkTyp.ProdNetz: return SqlServerEnum.PROD_Server;
                    case NetzwerkTyp.ClientNetz: return SqlServerEnum.CLIENT_Server;
                    default: throw new ArgumentOutOfRangeException("Netzwerktyp", $"Unbehandelter Netzwerktyp: {netzwerkTyp}");
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get SQL-Server-Type depending on Current Environment (22.03.2024, SME)
        public static SqlServerTypeEnum? GetSqlServerTypeEnum()
        {
            try
            {
                // store current environment
                var currentEnvironment = CoreFC.GetEnvironmentByStartupPath();
                if (!currentEnvironment.HasValue) return null;

                // handle current environment
                switch (currentEnvironment.Value)
                {
                    case CurrentEnvironment.PROD:
                        return SqlServerTypeEnum.PROD;

                    case CurrentEnvironment.TEST:
                    case CurrentEnvironment.TEST_INT:
                    case CurrentEnvironment.DEV:
                        return SqlServerTypeEnum.TEST;

                    default:
                        throw new ArgumentOutOfRangeException("Umgebung", $"Unbehandelte Umgebung: {currentEnvironment.Value}");
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get SQL-Server-Name depending on Network-Type and Current Environment (22.03.2024, SME)
        public static string GetSqlServerNameDependingOnNetworkAndEnvironment()
        {
            try
            {
                // get sql-server-enum + sql-server-type-enum
                var sqlServerEnum = GetSqlServerEnum();
                var sqlServerTypeEnum = GetSqlServerTypeEnum();

                // error-handling
                if (!sqlServerEnum.HasValue)
                {
                    throw new Exception("SQL-Server-Enum konnte nicht ermittelt werden!");
                }
                else if (!sqlServerTypeEnum.HasValue)
                {
                    throw new Exception("SQL-Servertyp-Enum konnte nicht ermittelt werden!");
                }

                // return
                if (sqlServerTypeEnum.Value == SqlServerTypeEnum.PROD)
                {
                    return sqlServerEnum.ToString();
                }
                else
                {
                    return sqlServerEnum.ToString() + sqlServerTypeEnum.ToString().ToLower();
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get SQL-Database-Name depending on Network-Type and Current Environment (22.03.2024, SME)
        // CHANGE: 06.06.2024 by SME: Parameters added: useTestForTestInt + useTestForTestDev
        public static string GetSqlDatabaseNameDependingOnNetworkAndEnvironment(bool useTestForTestInt = false, bool useTestForTestDev = false)
        {
            try
            {
                // get database-prefix + environment
                var databasePrefix = TC.Global.Global_TC_Core.CustomerDatabasePrefix;
                var environment = CoreFC.GetEnvironmentByStartupPath();

                // error-handling
                if (string.IsNullOrEmpty(databasePrefix))
                {
                    throw new Exception("Kunden-Datenbank-Präfix ist nicht gesetzt!");
                }
                else if (!environment.HasValue)
                {
                    throw new Exception("Umgebung konnte nicht ermittelt werden!");
                }

                // return
                switch (environment.Value)
                {
                    case CurrentEnvironment.PROD:
                        return databasePrefix;

                    case CurrentEnvironment.TEST:
                        return databasePrefix + "_" + environment.Value.ToString();
                    
                    case CurrentEnvironment.TEST_INT:
                        if (useTestForTestInt) return databasePrefix + "_TEST";
                        return databasePrefix + "_" + environment.Value.ToString();
                    
                    case CurrentEnvironment.DEV:
                        if (useTestForTestDev) return databasePrefix + "_TEST";
                        return databasePrefix + "_" + TC.Constants.CoreConstants.TEST_DEV;
                    
                    default:
                        throw new Exception($"Unbehandelte Umgebung: {environment.Value}");
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Constants

        #region Constant SQLs

        // Constant SQL: Is existing Temp-Table (13.11.2022, SRM)
        private const string DEF_SQL_IsExistingTempTable = @"IF OBJECT_ID('tempdb..#{0}') IS NOT NULL SELECT 1 ELSE SELECT 0;";

        // Constant SQL: Drop Temp-Table (13.11.2022, SRM)
        private const string DEF_SQL_DropTempTable = @"IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}";

        #endregion

        #region Constant Replacements

        // Constant Replacement: DB-Null-Value (13.11.2022, SRM)
        private const string DEF_Replacement_DBNullValue = "[DBNull]";

        #endregion

        #region Connection-Strings

        private const string CS_Context = "context connection=true";
        private const string CS_Trusted = "Server={SRV};Database={DB};Trusted_Connection=True;";
        private const string CS_User = "Server={SRV};Database={DB};User Id={USR};Password={PW};";

        #endregion

        public const string DEF_Filter_NoRows = "0 <> 0";
        public const string DEF_ServerNameToInstanceSeparator = @"\";

        #endregion

        #region Basic-Functions

        // Read Schema-Infos from SQL-DB (27.11.2023, SME)
        public static void UpdateSchemaInfosFromDB(DataSet dataset, string connectionString)
        {
            try
            {
                // exit-handling
                if (dataset == null) return;
                if (string.IsNullOrEmpty(connectionString)) return;

                // use connection
                using (var con = new SqlConnection(connectionString))
                {
                    // open connection
                    con.Open();

                    // update schema by other method
                    UpdateSchemaInfosFromDB(dataset, con);
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex);
            }
        }
        public static void UpdateSchemaInfosFromDB(DataSet dataset, SqlConnection connection)
        {
            try
            {
                // exit-handling
                if (dataset == null) return;
                if (connection == null) return;

                // declarations
                const string SQL = "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_DEFAULT IS NOT NULL ORDER BY TABLE_SCHEMA, TABLE_NAME";
                const string SchemaColumnName = "TABLE_SCHEMA";
                const string TableColumnName = "TABLE_NAME";
                const string ColumnColumnName = "COLUMN_NAME";
                const string DefaultValueColumnName = "COLUMN_DEFAULT";
                const string Prefix = "(";
                const string Suffix = ")";
                const string SingleQuote = "'";
                var schemaInfos = new DataTable();

                // use command
                using (var cmd = new System.Data.SqlClient.SqlCommand(SQL, connection))
                {
                    // use adapter
                    using (var adapter = new System.Data.SqlClient.SqlDataAdapter(cmd))
                    {
                        // fill schema-table
                        adapter.Fill(schemaInfos);
                    }
                }

                // loop throu schema-rows
                foreach (var schemaRow in schemaInfos.Select())
                {
                    try
                    {
                        // skip-handling
                        if (schemaRow[SchemaColumnName].ToString() != "dbo") continue;
                        if (!dataset.Tables.Contains(schemaRow[TableColumnName].ToString())) continue;

                        // get data-table from data-set
                        var dataTable = dataset.Tables[schemaRow[TableColumnName].ToString()];

                        // skip if column not existing
                        if (!dataTable.Columns.Contains(schemaRow[ColumnColumnName].ToString())) continue;

                        // store data-column
                        var dataColumn = dataTable.Columns[schemaRow[ColumnColumnName].ToString()];

                        // store default-value-text
                        string defaultValueText = schemaRow[DefaultValueColumnName].ToString();

                        // remove prefix + suffix
                        while (defaultValueText.StartsWith(Prefix) && defaultValueText.EndsWith(Suffix))
                        {
                            defaultValueText = defaultValueText.Substring(Prefix.Length);
                            defaultValueText = defaultValueText.Substring(0, defaultValueText.Length - Suffix.Length);
                        }
                        // remove single quotes for strings
                        while (defaultValueText.StartsWith(SingleQuote) && defaultValueText.EndsWith(SingleQuote))
                        {
                            defaultValueText = defaultValueText.Substring(SingleQuote.Length);
                            defaultValueText = defaultValueText.Substring(0, defaultValueText.Length - SingleQuote.Length);
                        }

                        // apply default-value depening on data-type of column
                        if (dataColumn.DataType == typeof(string))
                        {
                            dataColumn.DefaultValue = defaultValueText;
                        }
                        else if (dataColumn.DataType == typeof(bool))
                        {
                            switch (defaultValueText)
                            {
                                case "0":
                                case "false":
                                    dataColumn.DefaultValue = false; break;
                                case "1":
                                case "-1":
                                case "true":
                                    dataColumn.DefaultValue = true; break;
                                default:
                                    break;
                            }
                        }
                        else if (dataColumn.DataType == typeof(DateTime) || dataColumn.DataType == typeof(DateTimeOffset))
                        {
                            if (DateTime.TryParse(defaultValueText, out DateTime dateTime)) { dataColumn.DefaultValue = dateTime; }
                        }
                        else if (dataColumn.DataType == typeof(int))
                        {
                            if (int.TryParse(defaultValueText, out int i)) { dataColumn.DefaultValue = i; }
                        }
                        else
                        {
                            try
                            {
                                dataColumn.DefaultValue = Convert.ChangeType(defaultValueText, dataColumn.DataType);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"ERROR while setting default-value of column '{dataColumn.ColumnName}' to '{defaultValueText}': {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Get Data-Table from Enum-Type
        public static DataTable GetDataTableFromEnum<TEnumType>(bool sortByToString = true, bool addNullValue = false) where TEnumType : struct
        {
            try
            {
                // create table
                DataTable table = new("EnumTable_" + typeof(TEnumType).ToString());

                // create columns
                AddNewColumn(table, "ID", typeof(int), !addNullValue, true, true);
                AddNewColumn(table, "Name", typeof(string), true, true, true);
                AddNewColumn(table, "ToString", typeof(string), true, false, true);

                // handle null-value
                if (addNullValue)
                {
                    var row = table.NewRow();
                    row["ID"] = DBNull.Value;
                    row["Name"] = string.Empty;
                    row["ToString"] = string.Empty;
                    row.Table.Rows.Add(row);
                }

                // add rows
                foreach (var enumValue in CoreFC.GetEnumValues<TEnumType>())
                {
                    var row = table.NewRow();
                    row["ID"] = Convert.ToInt32(enumValue);
                    row["Name"] = enumValue.ToString();
                    row["ToString"] = CoreFC.GetCaption(enumValue);
                    row.Table.Rows.Add(row);
                }

                // set sort
                table.DefaultView.Sort = sortByToString ? "ToString" : "ID";

                // return
                table.AcceptChanges();
                return table;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Data-Row from Object (like DataBoundItem of DataGridView) (17.11.2022, SME)
        public static DataRow GetDataRow(object value)
        {
            if (value == null) return null;
            if (value is DataRow) return (DataRow)value;
            if (value is DataRowView) return ((DataRowView)value).Row;
            return null;
        }

        // Get Data-Table from Object (13.11.2023, SME)
        public static DataTable GetDataTable(object value)
        {
            if (value == null) return null;
            if (value is DataTable) return (DataTable)value;
            if (value is DataView) return ((DataView)value).Table;
            return null;
        }

        // Get Data-View from Object (13.11.2023, SME)
        public static DataView GetDataView(object value)
        {
            if (value == null) return null;
            if (value is DataView) return (DataView)value;
            if (value is DataTable) return ((DataTable)value).DefaultView;
            return null;
        }

        // Get new Column (07.12.2022, SME)
        public static DataColumn GetNewColumn(
            string columnName,
            Type dataType,
            bool isRequired = false,
            bool isUnique = false,
            bool isReadOnly = false,
            int? maxLength = null,
            object defaultValue = null, 
            string caption = "")
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));
                if (dataType == null) throw new ArgumentNullException(nameof(dataType));

                // create column
                var column = new DataColumn();

                // set properties
                column.ColumnName = columnName;
                column.DataType = dataType;
                column.AllowDBNull = !isRequired;
                column.Unique = isUnique;
                column.ReadOnly = isReadOnly;
                if (maxLength.HasValue) column.MaxLength = maxLength.Value;
                if (defaultValue != null) column.DefaultValue = defaultValue;
                column.Caption = string.IsNullOrEmpty(caption) ? columnName : caption;

                // return
                return column;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Add new Column to Table (15.11.2022, SME)
        public static DataColumn AddNewColumn(
            DataTable table,
            string columnName,
            Type dataType,
            bool isRequired = false,
            bool isUnique = false,
            bool isReadOnly = false,
            int? maxLength = null,
            object defaultValue = null, 
            string caption = "")
        {
            try
            {
                // exit-handling
                if (table == null) throw new ArgumentNullException(nameof(table));

                // create column
                var column = GetNewColumn(columnName: columnName, dataType: dataType, isRequired: isRequired, maxLength: maxLength, defaultValue: defaultValue, caption: caption);

                // add to column
                table.Columns.Add(column);

                // return
                return column;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Add new Auto-ID-Column to Table (15.11.2022, SME)
        public static DataColumn AddNewAutoIdColumn(DataTable table, string columnName = "ID", bool isPrimaryKey = true)
        {
            try
            {
                // create column
                var column = AddNewColumn(table, columnName, typeof(int), true, true, true);

                // set additional properties
                column.AutoIncrement = true;
                column.AutoIncrementSeed = 1;

                // set primary-key
                if (isPrimaryKey)
                {
                    table.PrimaryKey = new DataColumn[] { column };
                }

                // return
                return column;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Checks if Column contains DB-Null-Values (18.11.2022, SME)
        public static bool HasDBNullValues(DataColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));
            if (string.IsNullOrEmpty(column.ColumnName)) throw new ArgumentNullException(nameof(column.ColumnName));
            if (column.Table == null) throw new Exception("Table of Column '" + column.ColumnName + "' not set!");

            return column.Table.Select("[" + column.ColumnName + "] IS NULL").Any();
        }

        // Get distinct Values of Column with given ValueType (18.11.2022, SME)
        public static IEnumerable<TValueType> GetDistinctValues<TValueType>(DataColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));
            if (string.IsNullOrEmpty(column.ColumnName)) throw new ArgumentNullException(nameof(column.ColumnName));
            if (column.Table == null) throw new Exception("Table of Column '" + column.ColumnName + "' not set!");

            if (typeof(TValueType) == typeof(object))
                return column.Table.Select("", "[" + column.ColumnName + "]").Select(row => row[column.ColumnName]).Distinct() as IEnumerable<TValueType>;
            else
                throw new NotImplementedException("GetDistinctValues of ValueType '" + typeof(TValueType).ToString() + "'");
        }

        // Get distinct Values of Column (18.11.2022, SME)
        public static IEnumerable<object> GetDistinctValues(DataColumn column)
        {
            return GetDistinctValues<object>(column);
        }

        #endregion

        #region Connection

        // Get Connection-String for Context-Connection (14.11.2022, SRM)
        public static string GetConnectionString_Context()
        {
            return CS_Context;
        }

        // Get Connection-String (Trusted)
        public static string GetConnectionString(string server, string database)
        {
            return CS_Trusted
                   .Replace("{SRV}", server)
                   .Replace("{DB}", database);
        }

        // Get Connection-String (with User + Password)
        public static string GetConnectionString(string server, string database, string user, string password)
        {
            return CS_User
                   .Replace("{SRV}", server)
                   .Replace("{DB}", database)
                   .Replace("{USR}", user)
                   .Replace("{PW}", password);
        }

        // Get new Connection from Context (11.11.2022, SME)
        public static SqlConnection GetNewConnectionFromContext()
        {
            return new SqlConnection(GetConnectionString_Context());
        }

        // Get new Connection (Trusted) (13.11.2022, SRM)
        public static SqlConnection GetNewConnection(string server, string database)
        {
            return new SqlConnection(GetConnectionString(server, database));
        }

        // Get new Connection with User + Password (13.11.2022, SRM)
        public static SqlConnection GetNewConnection(string server, string database, string user, string password)
        {
            return new SqlConnection(GetConnectionString(server, database, user, password));
        }

        // Get Database from Connection-String (03.02.2024, SME)
        public static string GetDatabase(string connectionString)
        {
            return new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        }

        // Get Server from Connection-String (03.02.2024, SME)
        public static string GetDBServer(string connectionString)
        {
            return new SqlConnectionStringBuilder(connectionString).DataSource;
        }

        // Get Database-Info from Connection-String (03.02.2024, SME)
        public static DatabaseInfo GetDatabaseInfo(string connectionString)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(connectionString)) return null;

                // create cs-builder
                var csBuilder = new SqlConnectionStringBuilder(connectionString);

                // return
                return new DatabaseInfo(csBuilder.DataSource, csBuilder.InitialCatalog);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }


        #endregion

        #region Execute (Non-Query, Scalar, Query)

        #region Execute Scalar

        // Execute Scalar with SQL + Connection (11.11.2022, SME)
        public static object ExecuteScalar(string sql, SqlConnection connection)
        {
            using (var cmd = new SqlCommand(sql, connection))
            {
                return ExecuteScalar(cmd);
            }
        }

        // Execute Scalar with Command (11.11.2022, SRM)
        public static object ExecuteScalar(SqlCommand command)
        {
            using (var con = new OpenedConnection(command.Connection))
            {
                return command.ExecuteScalar();
            }
        }

        #endregion

        #region Execute Non-Query

        // Execute Non-Query with SQL + Connection (13.11.2022, SRM)
        public static int ExecuteNonQuery(string sql, SqlConnection connection)
        {
            try
            {
                using (var cmd = new SqlCommand(sql, connection))
                {
                    return ExecuteNonQuery(cmd);
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Execute Non-Query with Command (13.11.2022, SRM)
        public static int ExecuteNonQuery(SqlCommand command)
        {
            using (var con = new OpenedConnection(command.Connection))
            {
                return command.ExecuteNonQuery();
            }
        }

        #endregion

        #region Execute Query

        // Execute Query from SQL (13.11.2022, SRM)
        public static DataTable ExecuteQueryFromSql(string tableName, string sql, SqlConnection connection, bool acceptChanges = false, MissingSchemaAction missingSchemaAction = MissingSchemaAction.Add)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("TableName");
                if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException("SQL");
                if (connection == null) throw new ArgumentNullException("Connection");

                // use opened connection
                using (var con = new OpenedConnection(connection))
                {
                    // use adapter
                    using (var adapter = new SqlDataAdapter(sql, connection))
                    {
                        // create table
                        var table = new DataTable(tableName);

                        // set properties
                        adapter.MissingSchemaAction = missingSchemaAction;

                        // fill table
                        try
                        {
                            // add event-handlers
                            adapter.FillError += Adapter_FillError;

                            // fill
                            adapter.Fill(table);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                        finally
                        {
                            // remove event-handlers
                            adapter.FillError -= Adapter_FillError;
                        }

                        // accept changes
                        if (acceptChanges) table.AcceptChanges();

                        // return
                        return table;
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Event-Handler: Fill-Error on Adapter
        private static void Adapter_FillError(object sender, FillErrorEventArgs e)
        {
            CoreFC.DPrint("ERROR: Fill-Error on Adapter => " + e.Errors.Message);
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
        public static DataTable ExecuteQuery(string tableName, string filter, SqlConnection connection, bool acceptChanges = false, MissingSchemaAction missingSchemaAction = MissingSchemaAction.Add)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("TableName");
                if (connection == null) throw new ArgumentNullException("Connection");

                // set sql
                string sql = "SELECT * FROM " + tableName;
                if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;

                // return
                return ExecuteQueryFromSql(tableName, sql, connection, acceptChanges, missingSchemaAction);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Execute Query on Temp-Table (14.11.2022, SRM)
        public static DataTable ExecuteQueryOnTempTable(string tempTableName, SqlConnection connection, bool acceptChanges = false)
        {
            return ExecuteQuery("#" + tempTableName, string.Empty, connection, acceptChanges, MissingSchemaAction.AddWithKey);
        }

        #endregion

        #endregion

        #region Temp-Table-Handling

        #region Is existing Temp-Table

        // Is existing Temp-Table (13.11.2022, SRM)
        public static bool IsExistingTempTable(string tempTableName, SqlConnection connection)
        {
            // exit-handling
            if (string.IsNullOrEmpty(tempTableName)) return false;
            if (connection == null) throw new ArgumentNullException("Connection");

            // set sql
            string sql = string.Format(DEF_SQL_IsExistingTempTable, tempTableName);

            // get value
            var value = ExecuteScalar(sql, connection);

            // return
            if (value == null) return false;
            else return value.ToString().Equals("1");
        }

        #endregion

        #region Drop Temp Table

        // Drop Temp Table (13.11.2022, SRM)
        public static void DropTempTable(string tempTableName, SqlConnection connection)
        {
            // exit-handling
            if (string.IsNullOrEmpty(tempTableName)) return;
            if (connection == null) throw new ArgumentNullException("Connection");

            // set sql
            string sql = string.Format(DEF_SQL_DropTempTable, tempTableName);

            // execute
            ExecuteNonQuery(sql, connection);
        }

        #endregion

        #endregion

        // Set Cell-Value (27.05.2023, SME)
        public static void SetCellValue(DataRow row, DataColumn column, object value)
        {
            bool resetReadOnly = false;

            try
            {
                // make sure value is set
                if (value == null) value = DBNull.Value;

                // exit if equal
                if (row[column].Equals(value)) return;
                // exit if date + same tostring
                if (CoreFC.IsDateTimeType(value.GetType()))
                {
                    if (row[column].ToString().Equals(value.ToString())) return;
                }

                // set read-only-flag
                if (column.ReadOnly)
                {
                    resetReadOnly = true;
                    column.ReadOnly = false;
                }

                // store old value
                var oldValue = row[column];

                // set value
                row[column] = value;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // reset read-only
                if (resetReadOnly) column.ReadOnly = true;
            }
        }

        // Get Cell-Value-String by Cell-Value (13.11.2022, SRM)
        public static string GetCellValueString(object value, string dbNullValueReplacement = DEF_Replacement_DBNullValue)
        {
            if (value == DBNull.Value) return dbNullValueReplacement;
            else return value.ToString();
        }

        // Get Cell-Value-String by Row + Column (13.11.2022, SRM)
        public static string GetCellValueString(DataRow row, DataColumn column, string dbNullValueReplacement = DEF_Replacement_DBNullValue)
        {
            return GetCellValueString(row[column], dbNullValueReplacement);
        }

        // Get String of Table-Content (13.11.2022, SRM)
        public static string GetTableToString(DataTable table)
        {
            // exit-handling
            if (table == null) return string.Empty;
            if (table.Rows.Count == 0) return string.Empty;

            // create string-builder
            var sb = new StringBuilder();

            // add table-name
            sb.AppendLine("TableName = " + table.TableName);

            // add rows
            sb.AppendLine("Rows:");
            int i = 1;
            foreach (DataRow row in table.Select("", GetSortByPrimaryKey(table)))
            {
                // add row-info
                sb.AppendLine("- Row " + i.ToString());

                // add cell-values
                foreach (DataColumn column in table.Columns)
                {
                    sb.AppendLine("  - " + column.ColumnName + " = " + GetCellValueString(row, column));
                }

                // update index
                i++;
            }

            // return
            return sb.ToString().Trim();
        }

        // Get Row-Count from Data-Table (13.11.2022, SRM)
        public static int GetRowCount(DataTable table)
        {
            if (table == null) return -1;
            else return table.Rows.Count;
        }

        // Get SortBy PrimaryKey (14.11.2022, SRM)
        public static string GetSortByPrimaryKey(DataTable table)
        {
            if (table == null) return string.Empty;
            if (table.PrimaryKey == null) return string.Empty;
            if (table.PrimaryKey.Length == 0) return string.Empty;
            if (table.PrimaryKey.Length == 1) return table.PrimaryKey[0].ColumnName;

            // set sort-by
            var sql = string.Empty;
            foreach (var column in table.PrimaryKey)
            {
                sql += "[" + column.ColumnName + "], ";
            }
            return sql.Substring(0, sql.Length - 2);
        }

        // get value-column-enum from column (14.11.2022, SRM)
        public static ValueColumnEnum? GetValueColumnEnum(DataColumn column)
        {
            if (column == null) return null;
            return GetValueColumnEnum(column.DataType, column.MaxLength);
        }

        // get value-column-enum from type + max. length (31.10.2022, SRM)
        public static ValueColumnEnum? GetValueColumnEnum(Type type, int maxLength = 0)
        {
            if (type == null) return null;
            if (type == typeof(bool)) return ValueColumnEnum.BooleanValue;
            if (type == typeof(Guid)) return ValueColumnEnum.GuidValue;
            if (type == typeof(Byte[])) return ValueColumnEnum.BinaryValue;
            if (type == typeof(Int64)) return ValueColumnEnum.BigIntValue;
            if (type == typeof(UInt64)) return ValueColumnEnum.BigIntValue;
            if (type == typeof(string))
            {
                if (maxLength < 0) return ValueColumnEnum.LongTextValue;
                if (maxLength > 255) return ValueColumnEnum.LongTextValue;
                return ValueColumnEnum.ShortTextValue;
            }
            if (CoreFC.IsDateTimeType(type)) return ValueColumnEnum.DateTimeValue;
            if (CoreFC.IsDecimalType(type)) return ValueColumnEnum.DecimalValue;
            if (CoreFC.IsNumericType(type)) return ValueColumnEnum.IntValue;
            return null;
        }

        // get sql-db-type from data-column (26.11.2022, SME)
        public static SqlDbType GetSqlDbType(DataColumn column)
        {
            return GetSqlDbType(column.DataType, column.MaxLength);
        }

        // get sql-db-type from data-column (26.11.2022, SME)
        public static SqlDbType GetSqlDbType(ValueColumnEnum valueColumnEnum)
        {
            switch (valueColumnEnum)
            {
                case ValueColumnEnum.BooleanValue:
                    return SqlDbType.Bit;
                case ValueColumnEnum.IntValue:
                    return SqlDbType.Int;
                case ValueColumnEnum.BigIntValue:
                    return SqlDbType.BigInt;
                case ValueColumnEnum.DecimalValue:
                    return SqlDbType.Decimal;
                case ValueColumnEnum.DateTimeValue:
                    return SqlDbType.DateTime;
                case ValueColumnEnum.GuidValue:
                    return SqlDbType.UniqueIdentifier;
                case ValueColumnEnum.ShortTextValue:
                    return SqlDbType.NVarChar;
                case ValueColumnEnum.LongTextValue:
                    return SqlDbType.NVarChar;
                case ValueColumnEnum.BinaryValue:
                    return SqlDbType.VarBinary;
                default:
                    return SqlDbType.NVarChar;
            }
        }

        // get sql-db-type from value-column-enum (31.10.2022, SRM)
        public static SqlDbType GetSqlDbType(Type type, int maxLength = 0)
        {
            try
            {
                var valueColumnEnum = GetValueColumnEnum(type, maxLength);
                if (!valueColumnEnum.HasValue)
                    return SqlDbType.NVarChar;
                else
                    return GetSqlDbType(valueColumnEnum.Value);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Save new Rows (14.11.2022, SRM)
        public static void SaveNewRows(DataTable table, SqlConnection connection)
        {
            try
            {
                // exit-handling
                if (connection == null) return;
                if (table == null) return;
                if (table.Rows.Count == 0) return;

                // use opened connection
                using (var con = new OpenedConnection(connection))
                {
                    // store columns without auto-increment
                    var columns = table.Columns.OfType<DataColumn>().Where(col => !col.AutoIncrement).ToList();

                    // set sql
                    var sqlInsert = "INSERT INTO dbo.[" + table.TableName + "] (";
                    var sqlValues = "VALUES (";
                    columns.ForEach(col =>
                    {
                        sqlInsert += col.ColumnName + ", ";
                        sqlValues += "@" + col.ColumnName + ", ";
                    });
                    sqlInsert = sqlInsert.Substring(0, sqlInsert.Length - 2) + ")";
                    sqlValues = sqlValues.Substring(0, sqlValues.Length - 2) + ")";
                    var sql = sqlInsert + Environment.NewLine + sqlValues;

                    // create insert-command
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        // add parameters
                        columns.ForEach(col =>
                        {
                            SqlDbType sqlDbType = GetSqlDbType(col.DataType);
                            cmd.Parameters.Add("@" + col.ColumnName, sqlDbType);
                        });

                        // loop throu rows
                        foreach (var row in table.Select("", "", DataViewRowState.Added))
                        {
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
                                var result = ExecuteNonQuery(cmd);
                                if (result != 1)
                                {
                                    CoreFC.DPrint("Error while inserting");
                                }
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get SQL-String (14.11.2022, SRM)
        public static string GetSqlString(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        // Get Server-Name (11.11.2022, SME)
        public static string GetServerName(SqlConnection connection)
        {
            return ExecuteScalar("SELECT @@SERVERNAME;", connection) as string;
        }

        // Get DB-Name (11.11.2022, SME)
        public static string GetDBName(SqlConnection connection)
        {
            return ExecuteScalar("SELECT DB_NAME();", connection) as string;
        }

        // Open Connection (14.11.2022, SRM)
        public static void Connect(SqlConnection connection)
        {
            // exit-handling
            if (connection == null) return;

            // store connection-state
            var connectionState = connection.State;

            try
            {
                // handle connection-state
                switch (connection.State)
                {
                    case ConnectionState.Closed:
                        connection.Open();
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
                        connection.Close();
                        connection.Open();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string msg = "Error while opening connection." + Environment.NewLine
                           + "State at start = " + connectionState.ToString() + Environment.NewLine
                           + "Current state = " + connection.State.ToString() + Environment.NewLine
                           + "Connection-String = " + connection.ConnectionString;
                throw new TC.Errors.CoreError(msg, ex);
            }
        }

        // Get changed Columns from Row (23.11.2022, SME)
        public static IEnumerable<DataColumn> GetChangedColumns(DataRow row)
        {
            List<DataColumn> columns = new();

            if (row == null) return columns;
            if (row.Table == null) return columns;
            if (row.Table.Columns.Count == 0) return columns;
            if (row.RowState == DataRowState.Unchanged) return columns;
            if (row.RowState != DataRowState.Modified && row.RowState != DataRowState.Added) return columns;

            foreach (DataColumn column in row.Table.Columns)
            {
                if (!row[column].Equals(row[column, DataRowVersion.Original]))
                    columns.Add(column);
            }

            return columns;
        }

        // Is PK-Column
        public static bool IsPkColumn(DataColumn column)
        {
            if (column == null) return false;
            if (column.Table == null) return false;
            if (column.Table.PrimaryKey == null) return false;
            return column.Table.PrimaryKey.Contains(column);
        }

        // Get Column-Names from Data-Columns
        public static string[] GetColumnNames(IEnumerable<DataColumn> columns)
        {
            if (columns == null || !columns.Any()) return new string[] { };
            return columns.Select(col => col.ColumnName).Distinct().ToArray();
        }

        // Get Column-Names-String from Data-Columns
        public static string GetColumnNamesString(string separator, IEnumerable<DataColumn> columns)
        {
            return string.Join(separator, GetColumnNames(columns));
        }

        // Get PK-Column-Names-String from Data-Table
        public static string GetPkColumnNamesString(string separator, DataTable table)
        {
            if (table == null) return string.Empty;
            return GetColumnNamesString(separator, table.PrimaryKey);
        }

        // Get Column-Names from String + Delimiter
        public static string[] GetColumnNames(string columnNamesString, string delimiter)
        {
            if (string.IsNullOrEmpty(columnNamesString)) return new string[] { };
            if (string.IsNullOrEmpty(delimiter)) throw new ArgumentNullException(nameof(delimiter));

            List<string> list = new();

            foreach (var columnName in columnNamesString.Split(delimiter.ToCharArray()))
            {
                var col = columnName.ToString();
                if (!string.IsNullOrEmpty(col) && !list.Contains(col))
                    list.Add(col);
            }

            return list.ToArray();
        }

        // Get Cell-Values that contain certain Value (e.g. Semikolon) (30.11.2022, SME)
        public static Dictionary<DataRow, List<DataColumn>> GetCellValuesWithValue(DataView dataView, string value)
        {
            if (dataView == null) throw new ArgumentNullException(nameof(dataView));
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            // declarations
            var list = new Dictionary<DataRow, List<DataColumn>>();

            // loop throu rows
            foreach (DataRowView row in dataView)
            {
                // loop throu columns
                foreach (DataColumn column in dataView.Table.Columns)
                {
                    // check occurance of value
                    if (row.Row[column].ToString().Contains(value))
                    {
                        // fill list
                        if (!list.ContainsKey(row.Row))
                            list.Add(row.Row, new());
                        list[row.Row].Add(column);
                    }
                }
            }

            // return
            return list;
        }

        // Replace Cell-Values that contain certain Value with Replacement (e.g. Semikolon) (12.12.2022, SME)
        public static void ReplaceCellValues(DataView dataView, string find, string replacement)
        {
            if (dataView == null) throw new ArgumentNullException(nameof(dataView));
            if (string.IsNullOrEmpty(find)) throw new ArgumentNullException(nameof(find));
            if (find == replacement) throw new ArgumentOutOfRangeException(nameof(replacement), "Der Ersatzwert ist identisch mit dem zu suchenden Wert!");

            // loop throu rows
            foreach (DataRowView row in dataView)
            {
                // loop throu columns
                foreach (DataColumn column in dataView.Table.Columns)
                {
                    // check occurance of value
                    if (row.Row[column].ToString().Contains(find))
                    {
                        row.Row[column] = row.Row[column].ToString().Replace(find, replacement);
                    }
                }
            }
        }

        // Write CSV from Data-View (30.11.2022, SME)
        public static void WriteCSV(DataView dataView, string path, string delimiter = ";", bool overwrite = false, Encoding encoding = null, bool throwErrorWhenDelimiterIsContainedInCellValue = false, ProgressInfo progressInfo = null)
        {
            try
            {
                #region Error-Handling

                if (dataView == null) throw new ArgumentNullException(nameof(dataView));
                if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
                if (!path.ToLower().EndsWith(".csv")) throw new ArgumentOutOfRangeException(nameof(path));
                if (File.Exists(path) && !overwrite) throw new Exception("Die CSV-Datei existiert bereits und das Overwrite-Flag ist nicht gesetzt." + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + path);
                if (string.IsNullOrEmpty(delimiter)) throw new ArgumentNullException(nameof(delimiter));

                if (throwErrorWhenDelimiterIsContainedInCellValue)
                {
                    if (progressInfo != null) progressInfo.SetStatus("Zellenwerte werden nach Trennzeichen durchsucht...");
                    var found = GetCellValuesWithValue(dataView, delimiter);
                    if (found.Any()) throw new Exception("Gewisse Zellenwerte enthalten das Trennzeichen '" + delimiter + "'!");
                }

                #endregion

                // make sure encoding is set
                if (encoding == null) encoding = Encoding.Default;

                // use file-writer
                using (var writer = new StreamWriter(path, false, encoding))
                {
                    // write header
                    writer.WriteLine(string.Join(delimiter, dataView.Table.Columns.OfType<DataColumn>().Select(col => col.ColumnName)));

                    // start progress
                    if (progressInfo != null)
                    {
                        progressInfo.SetTotalSteps(dataView.Count);
                        progressInfo.SetStatus("CSV-Daten werden geschrieben ...");
                    }

                    // loop throu rows
                    foreach (DataRowView row in dataView)
                    {
                        try
                        {
                            // write row
                            writer.WriteLine(string.Join(delimiter, row.Row.ItemArray));
                            // perform step
                            if (progressInfo != null) progressInfo.PerformStep();
                        }
                        catch (Exception ex)
                        {
                            CoreFC.ThrowError(ex); throw ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #region Merge Data

        // Merge DataSet (07.12.2022, SME)
        public static void MergeData(DataSet originalDataSet, DataSet mergeDataSet)
        {
            try
            {
                // error-handling
                if (originalDataSet == null) throw new ArgumentNullException(nameof(originalDataSet));
                if (mergeDataSet == null) throw new ArgumentNullException(nameof(mergeDataSet));

                // merge data
                originalDataSet.Merge(mergeDataSet, true);

                // loop throu tables in original-data to accept changes
                foreach (DataTable originalTable in originalDataSet.Tables)
                {
                    try
                    {
                        // merge table
                        if (mergeDataSet.Tables.Contains(originalTable.TableName))
                        {
                            // accept changes after merge
                            AcceptChangesAfterMergeData(originalTable, mergeDataSet.Tables[originalTable.TableName]);
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Merge Table (07.12.2022, SME)
        public static void MergeData(DataTable originalTable, DataTable mergeTable)
        {
            try
            {
                // error-handling
                if (originalTable == null) throw new ArgumentNullException(nameof(originalTable));
                if (mergeTable == null) throw new ArgumentNullException(nameof(mergeTable));

                // merge data
                originalTable.Merge(mergeTable, true);

                // accept changes after merging tables
                AcceptChangesAfterMergeData(originalTable, mergeTable);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Accept Changes after merging Table (07.12.2022, SME)
        // => all rows that haven't really been changed will be accepted => set to unchanged
        private static void AcceptChangesAfterMergeData(DataTable originalTable, DataTable mergeTable)
        {
            try
            {
                // error-handling
                if (originalTable == null) throw new ArgumentNullException(nameof(originalTable));
                if (mergeTable == null) throw new ArgumentNullException(nameof(mergeTable));

                // store changes
                var changes = originalTable.GetChanges();
                var changesCount = changes == null ? 0 : changes.Rows.Count;

                // accept changes on every changed row that hasn't got changes
                if (changesCount > 0)
                {
                    // loop throu changed rows + check if columns have changed + if so, then accept changes
                    foreach (DataRow row in originalTable.Select("", "", DataViewRowState.ModifiedCurrent))
                    {
                        try
                        {
                            if (row.RowState == DataRowState.Modified)
                            {
                                var changedColumns = DataFC.GetChangedColumns(row);
                                if (changedColumns == null || !changedColumns.Any())
                                {
                                    row.AcceptChanges();
                                }
                                else
                                {
                                    // report differences (31.01.2024, SME)
                                    try
                                    {
                                        Console.WriteLine($"Following Changes have been found in Table '{row.Table.TableName}' and Row '{row.GetPKString()}':");
                                        foreach (var changedColumn in changedColumns)
                                        {
                                            try
                                            {
                                                Console.WriteLine($"- {changedColumn.ColumnName}: '{row[changedColumn, DataRowVersion.Original]}' => '{row[changedColumn]}'");
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine($"- ERROR while reporting Cell-Value-Change: Column = {changedColumn.ColumnName}, ErrorType = {ex.GetType()}, ErrorMessage = {ex.Message}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"ERROR while reporting Cell-Value-Changes: ErrorType = {ex.GetType()}, ErrorMessage = {ex.Message}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CoreFC.ThrowError(ex); throw ex;
                        }
                    }

                    // store change-count again + compare
                    var changesNew = originalTable.GetChanges();
                    var changesCountNew = changesNew == null ? 0 : changesNew.Rows.Count;
                    if (changesCount == changesCountNew)
                    {
                        Console.WriteLine($"Changes-Count: {changesCount:n0}");
                    }
                    else if (changesCountNew != 0)
                    {
                        Console.WriteLine($"Changes-Count: {changesCount:n0}, new Changes-Count: {changesCountNew:n0}");
                    }
                }

            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        // Check Connection (22.06.2023, SME)
        public static bool CheckConnection(string connectionString, bool throwErrorBack = false)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

                // open connection
                using (var con = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    try
                    {
                        con.Open();
                        con.Close();
                    }
                    catch (Exception ex)
                    {
                        var msg = "Beim Verbinden mit der Datenbank ist ein Fehler aufgetreten!" + CoreFC.Lines(2) 
                                + "Verbindungstext:" + Environment.NewLine + con.ConnectionString + CoreFC.Lines(2) 
                                + "Fehlermeldung:" + CoreFC.Lines() + ex.Message;
                        throw new Exception(msg);
                    }
                }

                // return
                return true;
            }
            catch (Exception ex)
            {
                if (throwErrorBack)
                {
                    CoreFC.ThrowError(ex); throw ex;
                }
                else
                {
                    return false;
                }
            }
        }

        // Update Connection-Strings in Settings (03.02.2024, SME)
        // CHANGE: 06.06.2024 by SME: Parameters added: useTestForTestInt + useTestForTestDev
        public static void UpdateConnectionStringsInSettings(ApplicationSettingsBase settings, bool useTestForTestInt = false, bool useTestForTestDev = false)
        {
            const string ServerProperty = "Data Source={0};";
            const string DatabasePropeprty = "Initial Catalog={0}";

            try
            {
                // exit-handling
                if (settings == null) return;
                // if (string.IsNullOrEmpty(sqlServerTypeString)) return;

                // get server depending on network + environment
                string serverName;
                try
                {
                    serverName = DataFC.GetSqlServerNameDependingOnNetworkAndEnvironment();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR while retrieving Server depending on Network + Environment: " + ex.Message);
                    return;
                }

                // get database depending on network + environment
                string databaseName;
                try
                {
                    databaseName = DataFC.GetSqlDatabaseNameDependingOnNetworkAndEnvironment(useTestForTestInt, useTestForTestDev);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR while retrieving Database depending on Network + Environment: " + ex.Message);
                    return;
                }

                // loop throu settings
                foreach (var setting in settings.Properties.OfType<SettingsProperty>().ToArray())
                {
                    // loop throu attributes to figure out if it's a connection-string
                    bool isCS = false;
                    foreach (var attribute in setting.Attributes)
                    {
                        if (attribute is SpecialSettingAttribute specialAttribute)
                        {
                            if (specialAttribute.SpecialSetting == SpecialSetting.ConnectionString)
                            {
                                // it's a connection-string
                                isCS = true;
                                break;
                            }
                        }
                        else if (attribute is System.Collections.DictionaryEntry entry)
                        {
                            if (entry.Value is SpecialSettingAttribute specialAttribute2)
                            {
                                if (specialAttribute2.SpecialSetting == SpecialSetting.ConnectionString)
                                {
                                    // it's a connection-string
                                    isCS = true;
                                    break;
                                }
                            }
                        }
                    }

                    // handle connection-string
                    if (isCS)
                    {
                        // get setting-value
                        var settingValue = settings.PropertyValues[setting.Name];

                        // get connection-string
                        var csValue = settingValue?.PropertyValue;
                        if (csValue == null) csValue = setting.DefaultValue;
                        if (csValue != null && csValue is string cs)
                        {
                            // store original
                            string csOriginal = cs.ToString();

                            // get server from cs
                            string server = DataFC.GetDBServer(cs);
                            if (!string.IsNullOrEmpty(server))
                            {
                                if (server.ToLower() != serverName.ToLower())
                                {
                                    // change server
                                    cs = cs.Replace(string.Format(ServerProperty, server), string.Format(ServerProperty, serverName));
                                }
                            }

                            // get database from cs
                            string database = DataFC.GetDatabase(cs);
                            if (!string.IsNullOrEmpty(database))
                            {
                                if (database.ToLower() != databaseName.ToLower())
                                {
                                    if (database.ToUpper().StartsWith(TC.Global.Global_TC_Core.CustomerDatabasePrefix.ToUpper()))
                                    {
                                        // change database
                                        cs = cs.Replace(string.Format(DatabasePropeprty, database), string.Format(DatabasePropeprty, databaseName));
                                    }
                                }
                            }

                            // update cs in setting-value
                            if (csOriginal != cs)
                            {
                                if (settingValue != null) settingValue.PropertyValue = cs;
                                else setting.DefaultValue = cs;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

    } // DataFC

} // NS: TC.Functions