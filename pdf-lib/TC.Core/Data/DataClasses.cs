using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TC.Interfaces;

namespace TC.Data
{
    // User-Info (14.11.2022, SRM)
    public class UserInfo
     {
          public readonly string UserName;

          public UserInfo(string userName)
          {
               // error-handling
               if (string.IsNullOrEmpty(userName)) throw new ArgumentNullException("UserName");

               // set local properties
               UserName = userName;
          }

          public override string ToString()
          {
               return UserName;
          }

          private int _UserID;
          public int UserID
          {
               get { return _UserID; }
               set
               {
                    if (UserID == 0)
                         _UserID = value;
                    else
                         throw new InvalidOperationException("User-ID cannot be set to " + value + " because it's already set to " + UserID);
               }
          }
     }

     // Database-Info (14.11.2022, SRM)
     public class DatabaseInfo: IDatabaseInfo
     {
        #region General

        // New Instance (03.02.2024, SME)
        public DatabaseInfo(string serverName, string databaseName)
        {
            // error-handling
            if (string.IsNullOrEmpty(serverName)) throw new ArgumentNullException(nameof(serverName));
            if (string.IsNullOrEmpty(databaseName)) throw new ArgumentNullException(nameof(databaseName));

            // set properties
            ServerName = serverName;
            DatabaseName = databaseName;
        }

        // ToString
        public override string ToString()
        {
            return DatabaseName + " @ " + ServerName;
        }

        #endregion

        #region Properties

        public string ServerName { get; }
        public string DatabaseName { get; }

        #endregion
    }

    // Table-Info (14.11.2022, SRM)
    public class TableInfo
     {
          // Variables
          public readonly string SchemaName;
          public readonly string TableName;
          public readonly string TableFullName;

          // New Instance
          public TableInfo(string schemaName, string tableName)
          {
               // error-handling
               if (string.IsNullOrEmpty(schemaName)) throw new ArgumentNullException("SchemaName");
               if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("TableName");

               // set local properties
               SchemaName = schemaName;
               TableName = tableName;
               TableFullName = string.Format("[{0}].[{1}]", schemaName, tableName);
          }

          // ToString
          public override string ToString()
          {
               return TableFullName;
          }

          // Table-ID
          private int _TableID;
          public int TableID
          {
               get { return _TableID; }
               set
               {
                    if (TableID == 0)
                         _TableID = value;
                    else
                         throw new InvalidOperationException("Table-ID cannot be set to " + value + " because it's already set to " + TableID);
               }
          }

          // Database-ID
          private int _DatabaseID;
          public int DatabaseID
          {
               get { return _DatabaseID; }
               set
               {
                    if (DatabaseID == 0)
                         _DatabaseID = value;
                    else
                         throw new InvalidOperationException("Database-ID cannot be set to " + value + " because it's already set to " + DatabaseID);
               }
          }

          // List of Column-Infos
          private readonly List<ColumnInfo> ColumnInfoList = new List<ColumnInfo>();
          public ColumnInfo[] ColumnInfos
          {
               get { return ColumnInfoList.ToArray(); }
          }

          // Contains Column by Column-Name (14.11.2022, SRM)
          public bool ContainsColumnInfo(string columnName)
          {
               return ColumnInfoList.Any(col => col.ColumnName == columnName);
          }

          // Get Column-Info by Column-Name (14.11.2022, SRM)
          public ColumnInfo GetColumnInfo(string columnName)
          {
               return ColumnInfoList.FirstOrDefault(col => col.ColumnName == columnName);
          }

          // Add Column-Info (14.11.2022, SRM)
          public void AddColumnInfo(ColumnInfo columnInfo)
          {
               if (columnInfo == null) return;
               if (ColumnInfoList.Contains(columnInfo)) return;
               if (columnInfo.TableInfo != this) throw new InvalidOperationException("Cannot add Column-Info because Table-Info doesn't match!");
               ColumnInfoList.Add(columnInfo);
          }

     }

     // Column-Info (14.11.2022, SRM)
     public class ColumnInfo
     {
          // Variables
          public readonly TableInfo TableInfo;
          public readonly string ColumnName;

          // New Instance by Data-Column (14.11.2022, SRM)
          public ColumnInfo(TableInfo tableInfo, DataColumn column)
          {
               // error-handling
               if (tableInfo == null) throw new ArgumentNullException("Table-Info");
               if (column == null) throw new ArgumentNullException("Data-Column");

               // set local properties
               TableInfo = tableInfo;
               ColumnName = column.ColumnName;
               _ColumnIndex = column.Ordinal + 1;
               _DataType = column.DataType.Name;
               _MaxLength = column.MaxLength;
               _WriteValueHistory = true;
               _HasChanged = true;
          }

          // New Instance by all Parameters (14.11.2022, SRM)
          public ColumnInfo(TableInfo tableInfo, string columnName, int columnIndex, string dataType, int? maxLength, bool writeValueHistory)
          {
               // error-handling
               if (tableInfo == null) throw new ArgumentNullException("Table-Info");
               if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException("Column-Name");

               // set local properties
               TableInfo = tableInfo;
               ColumnName = columnName;
               _ColumnIndex = columnIndex;
               _DataType = dataType;
               _MaxLength = maxLength;
               _WriteValueHistory = writeValueHistory;
          }

          // HasChanged-Flag (14.11.2022, SRM)
          private bool _HasChanged = false;
          public bool HasChanged { get { return _HasChanged; } }

          // Clear Changed-Flag
          public void ClearChangedFlag()
          {
               _HasChanged = false;
          }

          // Column-ID
          private int _ColumnID;
          public int ColumnID
          {
               get { return _ColumnID; }
               set
               {
                    // error-handling
                    if (ColumnID != 0)
                         throw new InvalidOperationException("Column-ID cannot be set to " + value + " because it's already set to " + ColumnID);

                    // set column-id
                    _ColumnID = value;

                    // update change-flag
                    _HasChanged = true;
               }
          }

          // Column-Index
          private int _ColumnIndex;
          public int ColumnIndex
          {
               get { return _ColumnIndex; }
               set
               {
                    if (ColumnIndex != value)
                    {
                         _ColumnIndex = value;
                         _HasChanged = true;
                    }
               }
          }

          // DataType
          private string _DataType;
          public string DataType
          {
               get { return _DataType; }
               set
               {
                    if (DataType != value)
                    {
                         _DataType = value;
                         _HasChanged = true;
                    }
               }
          }

          // MaxLength
          private int? _MaxLength;
          public int? MaxLength
          {
               get { return _MaxLength; }
               set
               {
                    if (MaxLength != value)
                    {
                         _MaxLength = value;
                         _HasChanged = true;
                    }
               }
          }

          // WriteValueHistory
          private bool _WriteValueHistory;
          public bool WriteValueHistory
          {
               get { return _WriteValueHistory; }
               set
               {
                    if (WriteValueHistory != value)
                    {
                         _WriteValueHistory = value;
                         _HasChanged = true;
                    }
               }
          }

          // Sync with Data-Column (14.11.2022, SRM)
          public void Sync(DataColumn column)
          {
               if (column == null) return;
               if (column.ColumnName != this.ColumnName) throw new InvalidOperationException("Sync with invalid Data-Column! Column-Name = " + this.ColumnName + ", Data-Column-Name = " + column.ColumnName);

               ColumnIndex = column.Ordinal + 1;
               DataType = column.DataType.Name;
               MaxLength = column.MaxLength;
          }
     }
}
