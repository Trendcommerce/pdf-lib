using System;
using System.Data;
using System.Data.SqlClient;
using TC.Data;
using MB = System.Reflection.MethodBase;
using System.Text;
using TC.Enums;
using System.Collections;
using System.Collections.Generic;

namespace TC.Functions
{
     // SQL-CLR-Functions (13.11.2022, SRM)
     public static class SQL_CLR_FC
     {
          #region IMPORTANT

          /*
           * ALL code in this class MUST BE in sync between TC.Core + TC_SQL_CLR !!!
           * => when ever a change is made, it must be applied in both code (13.11.2022, SRM)
           */

          #endregion

          #region Constants

          #region Constant SQLs

          // Constant SQL: Insert Trigger-Info (13.11.2022, SRM)
          private const string DEF_SQL_TriggerInfo_Insert = @"INSERT INTO dbo.{@TableName} (RaisedOn, TableName, DataChangeType, Count_Inserted, Count_Deleted) VALUES(@RaisedOn, @TableName, @DataChangeType, @Count_Inserted, @Count_Deleted);";

          // Constant SQL: Insert Log-Info (13.11.2022, SRM)
          private const string DEF_SQL_Log_Insert = @"INSERT INTO dbo.{@TableName} (LogDateTime, LogType, LogMessage, MethodName) VALUES(@LogDateTime, @LogType, @LogMessage, @MethodName);";

          #endregion

          #region Constant Table-Names

          // Constant Table-Name: Inserted Trigger-Data (13.11.2022, SRM)
          private const string DEF_TableName_TriggerDataInserted = "TriggerDataInserted";

          // Constant Table-Name: Deleted Trigger-Data (13.11.2022, SRM)
          private const string DEF_TableName_TriggerDataDeleted = "TriggerDataDeleted";

          // Constant Table-Name: Trigger-Info (13.11.2022, SRM)
          private const string DEF_TableName_TriggerInfo = "TriggerInfo";

          // Constant Table-Name: Log of SQL-CLR (13.11.2022, SRM)
          public const string DEF_TableName_Log = "Log_SQL_CLR";

          // Constant Table-Name: Settings (14.11.2022, SRM)
          public const string DEF_TableName_Settings = "TSYS_Settings";

          #endregion

          // Constant Server-Name for local Server
          public const string DEF_ServerName_Local = "{LOCAL}";

          // Constant Database-Name for local Database
          public const string DEF_DatabaseName_Local = "{LOCAL}";

          #endregion

          #region Enumerations

          // Enumeration of Settings (14.11.2022, SRM)
          public enum SettingEnum
          {
               ValueHistoryDB_Server,
               ValueHistoryDB_Database
          }

          #endregion

          #region Connections

          #region Local Connection

          // Local Connection (14.11.2022, SRM)
          private static readonly SqlConnection _Connection_Local = DataFC.GetNewConnectionFromContext();
          public static SqlConnection Connection_Local
          {
               get
               {
                    try
                    {
                         DataFC.Connect(_Connection_Local);
                         return _Connection_Local;
                    }
                    catch (Exception ex)
                    {
                         CoreFC.ThrowError(ex); throw ex;
                    }
               }
          }

          #endregion

          #region Value-History-DB-Connection

          // Value-History-DB Connection (14.11.2022, SRM)
          private static readonly SqlConnection _Connection_ValueHistoryDB = new SqlConnection(GetConnectionString_ValueHistoryDB(Connection_Local));
          public static SqlConnection Connection_ValueHistoryDB
          {
               get
               {
                    try
                    {
                         DataFC.Connect(_Connection_ValueHistoryDB);
                         return _Connection_ValueHistoryDB;
                    }
                    catch (Exception ex)
                    {
                         CoreFC.ThrowError(ex); throw ex;
                    }
               }
          }

          #endregion

          #endregion

          #region Log-Handling

          // Add Error to Log (13.11.2022, SRM)
          public static void AddLogError(SqlConnection connection, string tableName, MB method, Exception ex, string title = "")
          {
               // declare msg
               string msg = string.Empty;

               // add title
               if (!string.IsNullOrEmpty(title)) msg += title + Environment.NewLine + Environment.NewLine;

               // add error
               msg += ex.GetType().ToString() + ": " + Environment.NewLine + ex.Message;

               // add inner errors
               var innerError = ex.InnerException;
               while (innerError != null)
               {
                    msg += Environment.NewLine + Environment.NewLine + innerError.GetType().ToString() + Environment.NewLine + innerError.Message;
                    innerError = innerError.InnerException;
               }

               // add stack-trace
               msg += Environment.NewLine + Environment.NewLine + "Stack-Trace:" + Environment.NewLine + ex.StackTrace;

               // add log
               AddLog(connection, tableName, LogTypeEnum.Error, method, msg);
          }

          // Add Log with all parameters (13.11.2022, SRM)
          public static void AddLog(SqlConnection connection, string TableName, LogTypeEnum logType, MB method, string message)
          {
               try
               {
                    AddLog(connection, TableName, logType.ToString(), method.ToString(), message);
               }
               catch (Exception)
               {
                    // do nothing
                    throw;
               }
          }

          // Add Log with all resolved parameters (13.11.2022, SRM)
          public static void AddLog(SqlConnection connection, string TableName, string logType, string method, string message)
          {
               try
               {
                    // set sql
                    string sql = DEF_SQL_Log_Insert.Replace("{@TableName}", TableName);

                    // using command
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                         // set parameters
                         cmd.Parameters.Add("@LogDateTime", SqlDbType.DateTime).Value = DateTime.Now;
                         cmd.Parameters.Add("@LogType", SqlDbType.VarChar).Value = logType;
                         cmd.Parameters.Add("@LogMessage", SqlDbType.NVarChar).Value = message;
                         cmd.Parameters.Add("@MethodName", SqlDbType.NVarChar).Value = method;

                         // execute non-query
                         DataFC.ExecuteNonQuery(cmd);
                    }
               }
               catch (Exception)
               {
                    // do nothing
                    throw;
               }
          }

          #endregion

          #region Trigger-Handling

          #region Handle Trigger

          // Handle Trigger (13.11.2022, SRM)
          public static void HandleTrigger(int triggerId, SqlConnection connection, SqlConnection connectionValueHistoryDB)
          {
               string action = string.Empty;

               try
               {
                    action = "create opened connection";
                    using (var con = new OpenedConnection(connection))
                    {
                         action = "create opened connection to value-history-db";
                         using (var conValueHistoryDB = new OpenedConnection(connectionValueHistoryDB))
                         {
                              action = "GetTriggerDataInserted";
                              var inserted = GetTriggerDataInserted(connection);

                              action = "GetTriggerDataDeleted";
                              var deleted = GetTriggerDataDeleted(connection);

                              action = "GetDataChangeType";
                              var dataChangeType = GetDataChangeType(inserted, deleted);

                              action = "GetTableNameFromTriggerId";
                              var tableInfo = GetTableNameFromTriggerId(triggerId, connection);

                              action = "AddTriggerInfo";
                              AddTriggerInfo(connection, tableInfo.TableFullName, dataChangeType, DataFC.GetRowCount(inserted), DataFC.GetRowCount(deleted));

                              action = "Log TriggerDataInserted";
                              AddLog(connection, DEF_TableName_Log, LogTypeEnum.Debug, MB.GetCurrentMethod(), DataFC.GetTableToString(inserted));

                              action = "Log TriggerDataDeleted";
                              AddLog(connection, DEF_TableName_Log, LogTypeEnum.Debug, MB.GetCurrentMethod(), DataFC.GetTableToString(deleted));

                              action = "Get Database-ID";
                              int dbID = tableInfo.DatabaseID;
                              if (dbID == 0)
                              {
                                   dbID = GetDatabaseID(connection, connectionValueHistoryDB, false);
                                   tableInfo.DatabaseID = dbID;
                              }

                              action = "Get Table-ID";
                              int tableID = tableInfo.TableID;
                              if (tableID == 0)
                              {
                                   tableID = GetTableID(connection, connectionValueHistoryDB, tableInfo);
                                   tableInfo.TableID = tableID;
                              }

                              action = "Sync Columns";
                              var table = (inserted == null) ? deleted : inserted;
                              if (table != null)
                              {
                                   foreach (DataColumn column in table.Columns)
                                   {
                                        var columnInfo = tableInfo.GetColumnInfo(column.ColumnName);
                                        if (columnInfo == null)
                                        {
                                             // add new column-info
                                             var columnRow = GetColumnRow(connection, connectionValueHistoryDB, tableInfo, column);
                                             if (columnRow == null)
                                             {
                                                  // add new column
                                                  columnInfo = new ColumnInfo(tableInfo, column);
                                             }
                                             else
                                             {
                                                  // sync existing column
                                                  columnInfo = new ColumnInfo(tableInfo, 
                                                                              column.ColumnName, 
                                                                              (int)columnRow["ColumnIndex"], 
                                                                              columnRow["DataType"].ToString(), 
                                                                              (columnRow["MaxLength"].ToString().Length == 0 ? null : (int)columnRow["MaxLength"]), 
                                                                              (bool)columnRow["WriteValueHistory"]);

                                                  columnInfo.Sync(column);
                                             }
                                             // add to list
                                             tableInfo.AddColumnInfo(columnInfo);
                                        } 
                                        else
                                        {
                                             // update existing column-info
                                             columnInfo.Sync(column);
                                        }

                                   }
                              }
                         }
                    }
               }
               catch (Exception ex)
               {
                    AddLogError(connection, DEF_TableName_Log, MB.GetCurrentMethod(), ex, "Error while handling trigger in action " + action);
                    CoreFC.ThrowError(ex); throw ex;
               }
          }

          #endregion

          #region Trigger-Data-Handling

          // Drop TriggerData-TempTables (13.11.2022, SRM)
          public static void DropTriggerDataTempTables(SqlConnection connection)
          {
               using (var con = new OpenedConnection(connection))
               {
                    DataFC.DropTempTable(DEF_TableName_TriggerDataInserted, connection);
                    DataFC.DropTempTable(DEF_TableName_TriggerDataDeleted, connection);
               }
          }

          // Get Trigger-Data (13.11.2022, SRM)
          private static DataTable GetTriggerData(string tableName, SqlConnection connection)
          {
               using (var con = new OpenedConnection(connection))
               {
                    var action = string.Empty;

                    try
                    {
                         action = "IsExistingTempTable";
                         if (!DataFC.IsExistingTempTable(tableName, connection))
                              return null;
                         else
                         {
                              action = "ExecuteQuery";
                              return DataFC.ExecuteQueryOnTempTable(tableName, connection);
                         }
                    }
                    catch (Exception ex)
                    {
                         AddLogError(connection, DEF_TableName_Log, MB.GetCurrentMethod(), ex, "Error while getting trigger-data of " + tableName + " in action " + action);
                         return null;
                    }
               }
          }

          // Get Inserted-Trigger-Data (13.11.2022, SRM)
          private static DataTable GetTriggerDataInserted(SqlConnection connection)
          {
               return GetTriggerData(DEF_TableName_TriggerDataInserted, connection);
          }

          // Get Deleted-Trigger-Data (13.11.2022, SRM)
          private static DataTable GetTriggerDataDeleted(SqlConnection connection)
          {
               return GetTriggerData(DEF_TableName_TriggerDataDeleted, connection);
          }

          #endregion

          #region Table-Name by Trigger-ID

          // Get Table-Row from Trigger-ID (14.11.2022, SRM)
          private static DataRow GetTableRowFromTriggerId(int triggerId, SqlConnection connection)
          {
               try
               {
                    #region Set SQL

                    // create string-builder
                    var sb = new StringBuilder();

                    // fill string-builder
                    sb.AppendLine("SELECT ");
                    sb.AppendLine("     SCH.name AS SchemaName, ");
                    sb.AppendLine("     OBJ.name AS TableName ");
                    sb.AppendLine("FROM ");
                    sb.AppendLine("     sys.triggers AS TR ");
                    sb.AppendLine("INNER JOIN ");
                    sb.AppendLine("     sys.objects AS OBJ ON OBJ.object_id = TR.parent_id ");
                    sb.AppendLine("INNER JOIN ");
                    sb.AppendLine("     sys.schemas AS SCH ON SCH.schema_id = OBJ.schema_id ");
                    sb.AppendLine("WHERE ");
                    sb.AppendLine("     TR.object_id = " + triggerId);

                    // set sql
                    var sql = sb.ToString().Trim();

                    #endregion

                    // get data
                    var data = DataFC.ExecuteQueryFromSql("TableInfo", sql, connection);

                    // return
                    if (data == null) return null;
                    if (data.Rows.Count == 0) return null;
                    if (data.Rows.Count > 1) return null;
                    return data.Rows[0];
               }
               catch (Exception ex)
               {
                    CoreFC.ThrowError(ex); throw ex;
               }
          }

          // List of Table-Info by Trigger-ID (14.11.2022, SRM)
          private static readonly Dictionary<int, TableInfo> TableListByTriggerId = new Dictionary<int, TableInfo>();

          // Get Table-Info from Trigger-ID (14.11.2022, SRM)
          private static TableInfo GetTableNameFromTriggerId(int triggerId, SqlConnection connection)
          {
               // exit-handling
               if (TableListByTriggerId.ContainsKey(triggerId)) return TableListByTriggerId[triggerId];

               // get table-name by trigger-id
               var row = GetTableRowFromTriggerId(triggerId, connection);
               if (row == null) return null;

               var tableInfo = new TableInfo(row["SchemaName"].ToString(), row["TableName"].ToString());

               // add to list
               TableListByTriggerId.Add(triggerId, tableInfo);

               // log
               AddLog(connection, DEF_TableName_Log, LogTypeEnum.Info, MB.GetCurrentMethod(), "Table-Name added to TableListByTriggerId: TableName = " + tableInfo.TableFullName + ", TriggerId = " + triggerId);

               // return
               return tableInfo;
          }

          #endregion

          #region Add Trigger-Info

          // Add Trigger-Info (13.11.2022, SRM)
          private static void AddTriggerInfo(SqlConnection connection, string table, DataChangeTypeEnum? dataChangeType, int countInserted, int countDeleted)
          {
               using (var con = new OpenedConnection(connection))
               {
                    try
                    {
                         // set sql
                         string sql = DEF_SQL_TriggerInfo_Insert.Replace("{@TableName}", DEF_TableName_TriggerInfo);

                         // using command
                         using (var cmd = new SqlCommand(sql, connection))
                         {
                              // set parameters
                              cmd.Parameters.Add("@RaisedOn", SqlDbType.DateTime).Value = DateTime.Now;
                              cmd.Parameters.Add("@TableName", SqlDbType.NVarChar).Value = table;
                              cmd.Parameters.Add("@DataChangeType", SqlDbType.VarChar).Value = (!dataChangeType.HasValue) ? "?" : dataChangeType.ToString();
                              cmd.Parameters.Add("@Count_Inserted", SqlDbType.Int).Value = countInserted;
                              cmd.Parameters.Add("@Count_Deleted", SqlDbType.Int).Value = countDeleted;

                              // execute non-query
                              DataFC.ExecuteNonQuery(cmd);
                         }
                    }
                    catch (Exception)
                    {
                         // do nothing
                         throw;
                    }
               }
          }

          #endregion

          #endregion

          #region Setting-Handling

          #region Get Setting-Table

          // Get Setting-Table by Filter (14.11.2022, SRM)
          public static DataTable GetSettingTable(string filter, SqlConnection connection)
          {
               return DataFC.ExecuteQuery(DEF_TableName_Settings, filter, connection);
          }

          // Get Setting-Table by Setting-Enum-Array (14.11.2022, SRM)
          public static DataTable GetSettingTable(SqlConnection connection, params SettingEnum[] settingEnums)
          {
               // exit-handling
               if (settingEnums == null) return null;
               if (settingEnums.Length == 0) return null;

               // set filter
               var filter = "SettingName IN ('" + string.Join<SettingEnum>("', '", settingEnums) + "')";

               // return
               return GetSettingTable(filter, connection);
          }

          #endregion

          #region Get Setting-Value

          // Get Setting-Value from Setting-Row (14.11.2022, SRM)
          public static string GetSettingValue(DataRow settingRow)
          {
               if (settingRow == null) return string.Empty;
               return settingRow["SettingValue"].ToString();
          }

          // Get Setting-Value from Setting-Name (14.11.2022, SRM)
          public static string GetSettingValue(string settingName, SqlConnection connection)
          {
               // exit-handling
               if (string.IsNullOrEmpty(settingName)) return string.Empty;

               // set filter
               var filter = "SettingName = " + DataFC.GetSqlString(settingName);
               // get data
               var data = GetSettingTable(filter, connection);
               // return
               if (data == null) return string.Empty;
               if (data.Rows.Count == 0) return string.Empty;
               if (data.Rows.Count > 1) return string.Empty;
               return GetSettingValue(data.Rows[0]);
          }

          // Get Setting-Value from Setting-Enum (14.11.2022, SRM)
          public static string GetSettingValue(SettingEnum settingEnum, SqlConnection connection)
          {
               return GetSettingValue(settingEnum.ToString(), connection);
          }

          #endregion

          #endregion

          #region Value-History-DB-Handling

          // Get Connection-String of Value-History-DB (14.11.2022, SRM)
          public static string GetConnectionString_ValueHistoryDB(SqlConnection connection)
          {
               using (var con = new OpenedConnection(connection))
               {
                    // get setting-values
                    var server = GetSettingValue(SettingEnum.ValueHistoryDB_Server, connection);
                    var database = GetSettingValue(SettingEnum.ValueHistoryDB_Database, connection);

                    // check setting-values
                    if (string.IsNullOrEmpty(server)) return string.Empty;
                    if (string.IsNullOrEmpty(database)) return string.Empty;

                    // handle local
                    if (server.ToUpper() == DEF_ServerName_Local) server = DataFC.GetServerName(connection);
                    if (database.ToUpper() == DEF_DatabaseName_Local) server = DataFC.GetServerName(connection);

                    // return
                    return DataFC.GetConnectionString(server, database);
               }
          }

          // Get Database-ID (14.11.2022, SRM)
          public static int GetDatabaseID(SqlConnection connection, SqlConnection connectionValueHistoryDB, bool addLog)
          {
               string action = string.Empty;

               try
               {
                    // exit-handling
                    if (connection == null) throw new ArgumentNullException("Connection");
                    if (connectionValueHistoryDB == null) throw new ArgumentNullException("ValueHistoryDB-Connection");

                    // ONLY FOR TESTING: add connection-infos to log
                    if (addLog)
                    {
                         AddLog(connection, DEF_TableName_Log, LogTypeEnum.Info, MB.GetCurrentMethod(), string.Format("CS = {0}, State = {1}", connection.ConnectionString, connection.State));
                         AddLog(connection, DEF_TableName_Log, LogTypeEnum.Info, MB.GetCurrentMethod(), string.Format("CS = {0}, State = {1}", connectionValueHistoryDB.ConnectionString, connectionValueHistoryDB.State));
                    }

                    //// use opened connection
                    //action = "use opened connection";
                    //using (var con = new OpenedConnection(connection))
                    //{
                    // get server + database
                    string server, database;
                    try
                    {
                         action = "GetServerName";
                         server = DataFC.GetServerName(connection);
                    }
                    catch (Exception ex)
                    {
                         AddLogError(connection, DEF_TableName_Log, MB.GetCurrentMethod(), ex, "Error while GetServerName");
                         CoreFC.ThrowError(ex); throw ex;
                    }
                    try
                    {
                         action = "GetDBName";
                         database = DataFC.GetDBName(connection);
                    }
                    catch (Exception ex)
                    {
                         AddLogError(connection, DEF_TableName_Log, MB.GetCurrentMethod(), ex, "Error while GetDBName");
                         CoreFC.ThrowError(ex); throw ex;
                    }

                    #region Set SQL

                    action = "set sql";

                    // create string-builder
                    var sb = new StringBuilder();

                    // fill
                    sb.AppendLine("DECLARE @ID as int; ");
                    sb.AppendLine("SELECT @ID = ID_Database FROM dbo.Databases WHERE ServerName = @ServerName AND DatabaseName = @DatabaseName; ");
                    sb.AppendLine("IF (@ID IS NULL) BEGIN ");
                    sb.AppendLine("     INSERT INTO dbo.Databases (ServerName, DatabaseName, AddedOn) ");
                    sb.AppendLine("     VALUES (@ServerName, @DatabaseName, GetDate()); ");
                    sb.AppendLine("     SELECT @ID = ID_Database FROM dbo.Databases WHERE ServerName = @ServerName AND DatabaseName = @DatabaseName; ");
                    sb.AppendLine("END ");
                    sb.AppendLine("SELECT @ID AS ID; ");

                    // set sql
                    var sql = sb.ToString().Trim();

                    #endregion

                    //// use opened connection to value-history-db
                    //action = "use opened connection to value-history-db";
                    //using (var conValueHistoryDB = new OpenedConnection(Connection_ValueHistoryDB))
                    //{
                    // use command
                    action = "use command";
                    using (var cmd = new SqlCommand(sql, connectionValueHistoryDB))
                    {
                         // add parameters
                         action = "add parameters to command";
                         cmd.Parameters.Add("@ServerName", SqlDbType.VarChar).Value = server;
                         cmd.Parameters.Add("@DatabaseName", SqlDbType.VarChar).Value = database;

                         try
                         {
                              // get value
                              action = "get value from command";
                              var value = DataFC.ExecuteScalar(cmd);

                              // ONLY FOR TESTING: Log (14.11.2022, SRM)
                              if (addLog)
                                   AddLog(connection, DEF_TableName_Log, LogTypeEnum.Info, MB.GetCurrentMethod(), "GetDatabaseID: Server = " + server + ", Database = " + database + ", Database-ID = " + DataFC.GetCellValueString(value));

                              // return
                              if (value == null) return 0;
                              else if (value == DBNull.Value) return 0;
                              else return (int)value;
                         }
                         catch (Exception ex)
                         {
                              AddLogError(connection, DEF_TableName_Log, MB.GetCurrentMethod(), ex, "Error while getting database-id by execute-scalar");
                              CoreFC.ThrowError(ex); throw ex;
                         }
                    }
                    //}
                    //}
               }
               catch (Exception ex)
               {
                    AddLogError(connection, DEF_TableName_Log, MB.GetCurrentMethod(), ex, "Error while getting database-id in action " + action);
                    CoreFC.ThrowError(ex); throw ex;
               }
          }

          // Get Table-ID (14.11.2022, SRM)
          private static int GetTableID(SqlConnection connection, SqlConnection connectionValueHistoryDB, TableInfo tableInfo)
          {
               string action = string.Empty;

               try
               {
                    // exit-handling
                    if (connection == null) throw new ArgumentNullException("Connection");
                    if (connectionValueHistoryDB == null) throw new ArgumentNullException("ValueHistoryDB-Connection");
                    if (tableInfo == null) throw new ArgumentNullException("Table-Info");
                    if (tableInfo.DatabaseID == 0) throw new ArgumentOutOfRangeException("Table-Info", "Database-ID not set!");

                    #region Set SQL

                    action = "set sql";

                    // create string-builder
                    var sb = new StringBuilder();

                    // fill
                    sb.AppendLine("DECLARE @ID as int; ");
                    sb.AppendLine("SELECT @ID = ID_Table FROM dbo.Tables WHERE Database_ID = @Database_ID AND SchemaName = @SchemaName AND TableName = @TableName; ");
                    sb.AppendLine("IF (@ID IS NULL) BEGIN ");
                    sb.AppendLine("     INSERT INTO dbo.Tables (Database_ID, SchemaName, TableName, TableFullName, WriteValueHistory, AddedOn) ");
                    sb.AppendLine("     VALUES (@Database_ID, @SchemaName, @TableName, @TableFullName, 1, GetDate()); ");
                    sb.AppendLine("     SELECT @ID = ID_Table FROM dbo.Tables WHERE Database_ID = @Database_ID AND SchemaName = @SchemaName AND TableName = @TableName; ");
                    sb.AppendLine("END ");
                    sb.AppendLine("SELECT @ID AS ID; ");

                    // set sql
                    var sql = sb.ToString().Trim();

                    #endregion

                    // use command
                    action = "use command";
                    using (var cmd = new SqlCommand(sql, connectionValueHistoryDB))
                    {
                         // add parameters
                         action = "add parameters to command";
                         cmd.Parameters.Add("@Database_ID", SqlDbType.Int).Value = tableInfo.DatabaseID;
                         cmd.Parameters.Add("@SchemaName", SqlDbType.NVarChar).Value = tableInfo.SchemaName;
                         cmd.Parameters.Add("@TableName", SqlDbType.NVarChar).Value = tableInfo.TableName;
                         cmd.Parameters.Add("@TableFullName", SqlDbType.NVarChar).Value = tableInfo.TableFullName;

                         try
                         {
                              // get value
                              action = "get value from command";
                              var value = DataFC.ExecuteScalar(cmd);

                              // return
                              if (value == null) return 0;
                              else if (value == DBNull.Value) return 0;
                              else return (int)value;
                         }
                         catch (Exception ex)
                         {
                              CoreFC.ThrowError(ex); throw ex;
                         }
                    }
               }
               catch (Exception ex)
               {
                    AddLogError(connection, DEF_TableName_Log, MB.GetCurrentMethod(), ex, "Error while getting table-id in action " + action);
                    CoreFC.ThrowError(ex); throw ex;
               }
          }

          // Get Column-Row (14.11.2022, SRM)
          private static DataRow GetColumnRow(SqlConnection connection, SqlConnection connectionValueHistoryDB, TableInfo tableInfo, DataColumn column)
          {
               string action = string.Empty;

               try
               {
                    // exit-handling
                    if (connection == null) throw new ArgumentNullException("Connection");
                    if (connectionValueHistoryDB == null) throw new ArgumentNullException("ValueHistoryDB-Connection");
                    if (tableInfo == null) throw new ArgumentNullException("Table-Info");
                    if (tableInfo.DatabaseID == 0) throw new ArgumentOutOfRangeException("Table-Info", "Database-ID not set!");
                    if (tableInfo.TableID == 0) throw new ArgumentOutOfRangeException("Table-Info", "Table-ID not set!");
                    if (column == null) throw new ArgumentNullException("Data-Column");

                    // set filter
                    action = "set filter";
                    var filter = "Table_ID = {0} AND ColumnName = {1}";
                    filter = string.Format(filter, tableInfo.TableID, DataFC.GetSqlString(column.ColumnName));

                    // get data
                    action = "get data";
                    var data = DataFC.ExecuteQuery("Columns", filter, connectionValueHistoryDB);

                    // return
                    if (data == null) return null;
                    if (data.Rows.Count != 1) return null;
                    return data.Rows[0];
               }
               catch (Exception ex)
               {
                    AddLogError(connection, DEF_TableName_Log, MB.GetCurrentMethod(), ex, "Error in action " + action);
                    CoreFC.ThrowError(ex); throw ex;
               }
          }

          #endregion

          // Get Data-Change-Type (13.11.2022, SRM)
          private static DataChangeTypeEnum? GetDataChangeType(DataTable inserted, DataTable deleted)
          {
               if (inserted == null || deleted == null)
                    return null;
               else if (inserted.Rows.Count == 0 && deleted.Rows.Count == 0)
                    return null;
               else if (inserted.Rows.Count == 0)
                    return DataChangeTypeEnum.Delete;
               else if (deleted.Rows.Count == 0)
                    return DataChangeTypeEnum.Insert;
               else
                    return DataChangeTypeEnum.Update;
          }

     }
}
