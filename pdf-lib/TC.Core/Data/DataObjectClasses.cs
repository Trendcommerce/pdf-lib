using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TC.Functions;

namespace TC.Data.Core
{
    #region Basic Data-Object

    // Data-Object (24.11.2022, SME)
    public abstract class DataObject
    {
        public object Tag { get; set; }
    }

    #endregion
}

namespace TC.Data
{
    #region Event-Args

    public class SqlDatabaseEventArgs: EventArgs
    {
        public readonly SqlDatabase Database;
        public SqlDatabaseEventArgs(SqlDatabase database)
        {
            Database = database;
        }
    }

    public class SqlTableEventArgs : EventArgs
    {
        public readonly SqlTable Table;
        public SqlTableEventArgs(SqlTable table)
        {
            Table = table;
        }
    }

    public class SqlColumnEventArgs : EventArgs
    {
        public readonly SqlColumn Column;
        public SqlColumnEventArgs(SqlColumn column)
        {
            Column = column;
        }
    }

    public class SqlRelationEventArgs : EventArgs
    {
        public readonly SqlRelation Relation;
        public SqlRelationEventArgs(SqlRelation relation)
        {
            Relation = relation;
        }
    }

    #endregion

    #region Server

    public class SqlServer: Core.DataObject
    {
        #region General

        // New Instance (22.11.2022, SME)
        public SqlServer(ServerTypes type, string serverName, string instanceName = "", object tag = null)
        {
            // error-handling
            if (string.IsNullOrEmpty(serverName))
                throw new ArgumentNullException(nameof(serverName));

            // set local properties
            ServerType = type;
            ServerName = serverName;
            InstanceName = instanceName;
            ServerInstanceName = (string.IsNullOrEmpty(InstanceName) ? serverName : serverName + DataFC.DEF_ServerNameToInstanceSeparator + instanceName);
            Connection = new(ServerInstanceName, "master");
            Tag = tag;
        }

        // ToString
        public override string ToString()
        {
            return ServerInstanceName;
        }

        #endregion

        #region Enumerations

        // Enumerations of Server-Types
        // IMPORTANT: When ever a new enum is added, setting connection-string must be adjusted!!! (23.11.2022, SME)
        public enum ServerTypes
        {
            SqlServer
        }

        #endregion

        #region Events

        // Database-Events
        public delegate void DatabaseEventHandler(object sender, SqlDatabaseEventArgs e);
        public event DatabaseEventHandler DatabaseAdded;
        public event DatabaseEventHandler DatabaseRemoved;

        // Table-Events
        public delegate void TableEventHandler(object sender, SqlTableEventArgs e);
        public event TableEventHandler TableAdded;
        public event TableEventHandler TableRemoved;

        // Column-Events
        public delegate void ColumnEventHandler(object sender, SqlColumnEventArgs e);
        public event ColumnEventHandler ColumnAdded;
        public event ColumnEventHandler ColumnRemoved;

        // Relation-Events
        public delegate void RelationEventHandler(object sender, SqlRelationEventArgs e);
        public event RelationEventHandler RelationAdded;
        public event RelationEventHandler RelationRemoved;

        #endregion

        #region Properties

        // Type
        public readonly ServerTypes ServerType;

        // ServerName
        public readonly string ServerName;

        // InstanceName
        public readonly string InstanceName;

        // ServerInstanceName
        public readonly string ServerInstanceName;

        // Connection
        internal readonly SqlDbConnection Connection;

        #endregion

        #region Databases

        // List of Databases
        private readonly List<SqlDatabase> DatabaseList = new();
        public SqlDatabase[] Databases => DatabaseList.OrderBy(db => db.DatabaseName).ToArray();

        // Contains Database
        public bool ContainsDatabase(string databaseName) => DatabaseList.Any(db => db.DatabaseName.Equals(databaseName));

        // Get Database
        public SqlDatabase GetDatabase(string databaseName) => DatabaseList.FirstOrDefault(db => db.DatabaseName.Equals(databaseName));

        // Refresh Databases
        public void RefreshDatabases(bool refreshTables = false, bool refreshColumns = false, bool refreshRelations = false)
        {
            try
            {
                // use opened connection
                using (var con = Connection.GetNewOpenedConnection())
                {
                    // set sql
                    var sql = "SELECT database_id AS DB_ID, name AS DatabaseName FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')";

                    // get data
                    var data = Connection.ExecuteQueryFromSql(sql, "Databases");

                    // list of all databases (leftovers will be removed at end)
                    var list = DatabaseList.ToList();

                    // loop throu rows + add new databases
                    foreach (var row in data.Select("", "DatabaseName")) 
                    {
                        var name = row["DatabaseName"].ToString();
                        var db = GetDatabase(name);
                        if (db != null)
                            list.Remove(db);
                        else
                        {
                            db = new(this, name);
                            DatabaseList.Add(db);
                            db.TableAdded += Database_TableAdded;
                            db.TableRemoved += Database_TableRemoved;
                            db.ColumnAdded += Database_Table_ColumnAdded;
                            db.ColumnRemoved += Database_Table_ColumnRemoved;
                            db.RelationAdded += Database_RelationAdded;
                            db.RelationRemoved += Database_RelationRemoved;
                            DatabaseAdded?.Invoke(this, new(db));
                        }
                    }

                    // remove not anymore existing databases
                    if (list.Any())
                    {
                        list.ForEach(db =>
                        {
                            DatabaseList.Remove(db);
                            db.TableAdded -= Database_TableAdded;
                            db.TableRemoved -= Database_TableRemoved;
                            db.ColumnAdded -= Database_Table_ColumnAdded;
                            db.ColumnRemoved -= Database_Table_ColumnRemoved;
                            db.RelationAdded -= Database_RelationAdded;
                            db.RelationRemoved -= Database_RelationRemoved;
                            DatabaseRemoved?.Invoke(this, new(db));
                        });
                    }

                    // refresh tables
                    if (refreshTables)
                        DatabaseList.ForEach(db => db.RefreshTables(refreshColumns));

                    // refresh relations
                    if (refreshRelations)
                        DatabaseList.ForEach(db => db.RefreshRelations());
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Event-Handler: Relation added
        private void Database_RelationAdded(object sender, SqlRelationEventArgs e)
        {
            RelationAdded?.Invoke(this, e);
        }

        // Event-Handler: Relation removed
        private void Database_RelationRemoved(object sender, SqlRelationEventArgs e)
        {
            RelationRemoved?.Invoke(this, e);
        }

        // Event-Handler: Table added
        private void Database_TableAdded(object sender, SqlTableEventArgs e)
        {
            TableAdded?.Invoke(this, e);
        }

        // Event-Handler: Table removed
        private void Database_TableRemoved(object sender, SqlTableEventArgs e)
        {
            TableRemoved?.Invoke(this, e);
        }

        // Event-Handler: Column added
        private void Database_Table_ColumnAdded(object sender, SqlColumnEventArgs e)
        {
            ColumnAdded?.Invoke(this, e);
        }

        // Event-Handler: Column removed
        private void Database_Table_ColumnRemoved(object sender, SqlColumnEventArgs e)
        {
            ColumnRemoved?.Invoke(this, e);
        }

        #endregion
    }

    #endregion

    #region Database

    public class SqlDatabase : Core.DataObject
    {
        #region General

        // New Instance (22.11.2022, SME)
        public SqlDatabase(SqlServer server, string databaseName)
        {
            // error-handling
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            else if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            // set local properties
            Server = server;
            DatabaseName = databaseName;
            Connection = new(server.ServerInstanceName, databaseName);
        }

        // ToString
        public override string ToString()
        {
            return DatabaseName + " @ " + Server.ServerInstanceName;
        }

        #endregion

        #region Events

        // Table-Events
        public delegate void TableEventHandler(object sender, SqlTableEventArgs e);
        public event TableEventHandler TableAdded;
        public event TableEventHandler TableRemoved;

        // Column-Events
        public delegate void ColumnEventHandler(object sender, SqlColumnEventArgs e);
        public event ColumnEventHandler ColumnAdded;
        public event ColumnEventHandler ColumnRemoved;

        // Relation-Events
        public delegate void RelationEventHandler(object sender, SqlRelationEventArgs e);
        public event RelationEventHandler RelationAdded;
        public event RelationEventHandler RelationRemoved;

        #endregion

        #region Properties

        // Server
        public readonly SqlServer Server;

        // Database-Name
        public readonly string DatabaseName;

        // Connection
        internal readonly SqlDbConnection Connection;

        #endregion

        #region Tables

        // List of Tables
        private readonly List<SqlTable> TableList = new();
        public SqlTable[] Tables => TableList.OrderBy(tbl => tbl.TableFullName).ToArray();

        // Contains Table
        public bool ContainsTable(string schemaName, string tableName) => TableList.Any(tbl => tbl.SchemaName.Equals(schemaName) && tbl.TableName.Equals(tableName));

        // Get Table
        public SqlTable GetTable(string schemaName, string tableName) => TableList.FirstOrDefault(tbl => tbl.SchemaName.Equals(schemaName) && tbl.TableName.Equals(tableName));

        // Refresh Tables
        public void RefreshTables(bool refreshColumns = false)
        {
            try
            {
                // use opened connection
                using (var con = Connection.GetNewOpenedConnection())
                {
                    #region set sql

                    var sb = new StringBuilder();
                    sb.AppendLine("SELECT TBL.object_id AS TableId, SCH.name AS SchemaName, TBL.name AS TableName ");
                    sb.AppendLine("FROM sys.tables AS TBL ");
                    sb.AppendLine("INNER JOIN sys.schemas AS SCH ON SCH.schema_id = TBL.schema_id ");
                    sb.AppendLine("WHERE TBL.name NOT IN ('sysdiagrams')");
                    sb.AppendLine("ORDER BY SCH.name, TBL.name");

                    var sql = sb.ToString().Trim();

                    #endregion

                    // get data
                    var data = Connection.ExecuteQueryFromSql(sql, "Tables");

                    // list of all tables (leftovers will be removed at end)
                    var list = TableList.ToList();

                    // loop throu rows + add new tables
                    foreach (var row in data.Select("", "SchemaName, TableName"))
                    {
                        var schema = row["SchemaName"].ToString();
                        var name = row["TableName"].ToString();
                        var tblName = string.Format("[{0}].[{1}]", schema, name);
                        var emptyTable = Connection.GetEmptyTable(tblName, MissingSchemaAction.AddWithKey);
                        var tbl = GetTable(schema, name);
                        if (tbl != null)
                        {
                            // refresh empty table
                            tbl.RefreshEmptyTable();
                            // remove from list
                            list.Remove(tbl);
                        }
                        else
                        {
                            // create new table
                            tbl = new(this, schema, name);
                            TableList.Add(tbl);
                            tbl.ColumnAdded += Table_ColumnAdded;
                            tbl.ColumnRemoved += Table_ColumnRemoved;
                            TableAdded?.Invoke(this, new(tbl));
                        }
                    }

                    // remove not anymore existing tables
                    if (list.Any())
                    {
                        list.ForEach(tbl =>
                        {
                            TableList.Remove(tbl);
                            tbl.ColumnAdded -= Table_ColumnAdded;
                            tbl.ColumnRemoved -= Table_ColumnRemoved;
                            TableRemoved?.Invoke(this, new(tbl));
                        });
                    }

                    // refresh columns
                    if (refreshColumns)
                        TableList.ForEach(tbl => tbl.RefreshColumns(tbl.EmptyTable));
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Event-Handler: Column added to Table
        private void Table_ColumnAdded(object sender, SqlColumnEventArgs e)
        {
            ColumnAdded?.Invoke(this, e);
        }

        // Event-Handler: Column rwemoved from Table
        private void Table_ColumnRemoved(object sender, SqlColumnEventArgs e)
        {
            ColumnRemoved?.Invoke(this, e);
        }

        #endregion

        #region Relations

        // List of Relations
        internal readonly List<SqlRelation> RelationList = new();
        public SqlRelation[] Relations => RelationList.OrderBy(rel => rel.ChildTable.TableFullName).ToArray();

        // Refresh Relations
        public void RefreshRelations()
        {
            try
            {
                // use opened connection
                using (var con = Connection.GetNewOpenedConnection())
                {
                    #region set sql

                    var sb = new StringBuilder();
                    sb.AppendLine("SELECT FK.object_id AS FKID, FK.name AS FKName, FK_Col.constraint_column_id AS ColumnIndex,");
                    sb.AppendLine("     SCH_Parent.name AS ParentSchemaName, TBL_Parent.name AS ParentTableName, COL_Parent.name AS ParentColumnName,");
                    sb.AppendLine("     SCH_Child.name AS ChildSchemaName, TBL_Child.name AS ChildTableName, COL_Child.name AS ChildColumnName");
                    sb.AppendLine("FROM sys.foreign_key_columns AS FK_Col");
                    sb.AppendLine("INNER JOIN sys.foreign_keys AS FK ON FK.object_id = FK_Col.constraint_object_id");
                    sb.AppendLine("INNER JOIN sys.tables AS TBL_Parent ON TBL_Parent.object_id = FK.referenced_object_id");
                    sb.AppendLine("INNER JOIN sys.schemas AS SCH_Parent ON SCH_Parent.schema_id = TBL_Parent.schema_id");
                    sb.AppendLine("INNER JOIN sys.columns AS COL_Parent ON COL_Parent.object_id = TBL_Parent.object_id AND COL_Parent.column_id = FK_Col.referenced_column_id");
                    sb.AppendLine("INNER JOIN sys.tables AS TBL_Child ON TBL_Child.object_id = FK.parent_object_id");
                    sb.AppendLine("INNER JOIN sys.schemas AS SCH_Child ON SCH_Child.schema_id = TBL_Child.schema_id");
                    sb.AppendLine("INNER JOIN sys.columns AS COL_Child ON COL_Child.object_id = TBL_Child.object_id AND COL_Child.column_id = FK_Col.parent_column_id");


                    var sql = sb.ToString().Trim();

                    #endregion

                    // get data
                    var data = Connection.ExecuteQueryFromSql(sql, "Relations");

                    // list of all relations (leftovers will be removed at end)
                    var list = RelationList.ToList();

                    int fkID = 0;
                    SqlTable parentTable = null;
                    List<string> parentColumnNames = new();
                    SqlTable childTable = null;
                    List<string> childColumnNames = new();
                    string schemaName;
                    string tableName;
                    SqlRelation relation;

                    // loop throu rows + add new relations
                    foreach (var row in data.Select("", "FKID, ColumnIndex"))
                    {
                        // check if fk-id changed
                        if (fkID != (int)row[0]) {
                            // starting new relation
                            // => add current relation if possible
                            if (fkID != 0)
                            {
                                relation = new(parentTable, parentColumnNames.ToArray(), childTable, childColumnNames.ToArray());
                                if (!RelationList.Any(rel => rel.IsIdentical(relation)))
                                {
                                    RelationList.Add(relation);
                                    RelationAdded?.Invoke(this, new(relation));
                                }
                                list.RemoveAll(rel => rel.IsIdentical(relation));
                            }

                            // set fk-id
                            fkID = (int)row[0];

                            // set tables
                            schemaName = row["ParentSchemaName"].ToString();
                            tableName = row["ParentTableName"].ToString();
                            parentTable = GetTable(schemaName, tableName);
                            if (parentTable == null)
                                throw new Exception("Über-Tabelle konnte nicht ermittelt werden: " + schemaName + "." + tableName);
                            schemaName = row["ChildSchemaName"].ToString();
                            tableName = row["ChildTableName"].ToString();
                            childTable = GetTable(schemaName, tableName);
                            if (childTable == null)
                                throw new Exception("Unter-Tabelle konnte nicht ermittelt werden: " + schemaName + "." + tableName);

                            // clearing
                            parentColumnNames.Clear();
                            childColumnNames.Clear();
                        }

                        // add column-names
                        parentColumnNames.Add(row["ParentColumnName"].ToString());
                        childColumnNames.Add(row["ChildColumnName"].ToString());
                    }
                    // => add last relation if possible
                    if (fkID != 0)
                    {
                        relation = new(parentTable, parentColumnNames.ToArray(), childTable, childColumnNames.ToArray());
                        if (!RelationList.Any(rel => rel.IsIdentical(relation)))
                        {
                            RelationList.Add(relation);
                            RelationAdded?.Invoke(this, new(relation));
                        }
                        list.RemoveAll(rel => rel.IsIdentical(relation));
                    }

                    // remove not anymore existing relations
                    if (list.Any())
                    {
                        list.ForEach(rel =>
                        {
                            RelationList.Remove(rel);
                            RelationRemoved?.Invoke(this, new(rel));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }



        #endregion

        #region Methods

        // Refresh Schema
        public void RefreshSchema()
        {
            RefreshTables(true);
            RefreshRelations();
        }

        #endregion
    }

    #endregion

    #region Table

    public class SqlTable : Core.DataObject
    {
        #region General

        // New Instance (22.11.2022, SME)
        public SqlTable(SqlDatabase database, string schemaName, string tableName)
        {
            // error-handling
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            else if (string.IsNullOrEmpty(schemaName))
                throw new ArgumentNullException(nameof(schemaName));
            else if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            // set local properties
            Database = database;
            SchemaName = schemaName;
            TableName = tableName;
            TableFullName = string.Format("[{0}].[{1}]", schemaName, tableName);
            RefreshEmptyTable();
        }

        // ToString
        public override string ToString()
        {
            return TableFullName + " (" + Database.ToString() + ")";
        }

        #endregion

        #region Events

        // Column-Events
        public delegate void ColumnEventHandler(object sender, SqlColumnEventArgs e);
        public event ColumnEventHandler ColumnAdded;
        public event ColumnEventHandler ColumnRemoved;

        #endregion

        #region Properties

        // Database
        public readonly SqlDatabase Database;

        // Schema-Name
        public readonly string SchemaName;

        // Table-Name
        public readonly string TableName;

        // Table-Fullname
        public readonly string TableFullName;

        // Connection
        internal SqlDbConnection Connection => Database.Connection;

        // Empty Table
        private DataTable _EmptyTable;
        public DataTable EmptyTable => _EmptyTable;

        #endregion

        #region Columns

        // List of Columns
        private readonly List<SqlColumn> ColumnList = new();
        public SqlColumn[] Columns => ColumnList.OrderBy(col => col.ColumnIndex).ThenBy(col => col.ColumnName).ToArray();

        // Contains Column
        public bool ContainsColumn(string columnName) => ColumnList.Any(col => col.ColumnName.Equals(columnName));

        // Get Column
        public SqlColumn GetColumn(string columnName) => ColumnList.FirstOrDefault(col => col.ColumnName.Equals(columnName));

        // Refresh Columns
        public void RefreshColumns()
        {
            try
            {
                // use opened connection
                using (var con = Connection.GetNewOpenedConnection())
                {
                    // refresh empty table
                    RefreshEmptyTable();

                    // refresh columns by other method
                    RefreshColumns(EmptyTable);
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Refresh Columns with empty Table (27.11.2022, SME)
        internal void RefreshColumns(DataTable emptyTable)
        {
            try
            {
                // update table-properties from empty table (PrimaryKeyType + PrimaryKeyColumnNames)
                

                // use opened connection
                using (var con = Connection.GetNewOpenedConnection())
                {
                    // list of all columns (leftovers will be removed at end)
                    var list = ColumnList.ToList();

                    // loop throu columns + add new columns
                    foreach (DataColumn column in emptyTable.Columns)
                    {
                        var index = column.Ordinal + 1;
                        var name = column.ColumnName;
                        var col = GetColumn(name);
                        if (col != null)
                        {
                            col.UpdateProperties(column);
                            list.Remove(col);
                        }
                        else
                        {
                            col = new(this, column);
                            ColumnList.Add(col);
                            ColumnAdded?.Invoke(this, new(col));
                        }
                    }

                    // remove not anymore existing columns
                    if (list.Any())
                    {
                        list.ForEach(col =>
                        {
                            ColumnList.Remove(col);
                            ColumnRemoved?.Invoke(this, new(col));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Relations

        // Parent-Relations
        public SqlRelation[] ParentRelations => Database.Relations.Where(rel => rel.ChildTable.Equals(this)).OrderBy(rel => rel.ParentTable.TableFullName).ToArray();

        // Child-Relations
        public SqlRelation[] ChildRelations => Database.Relations.Where(rel => rel.ParentTable.Equals(this)).OrderBy(rel => rel.ChildTable.TableFullName).ToArray();

        #endregion

        // refresh empty table
        public void RefreshEmptyTable()
        {
            try
            {
                _EmptyTable = Connection.GetEmptyTable(TableFullName, MissingSchemaAction.AddWithKey);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }
    }

    #endregion

    #region Column

    public class SqlColumn : Core.DataObject
    {
        #region General

        // New Instance with all properties (22.11.2022, SME)
        public SqlColumn(SqlTable table, string columnName, int columnIndex, string dataType, int maxLength, bool isInPK)
        {
            // error-handling
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            else if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException(nameof(columnName));
            else if (columnIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            // set local properties
            Table = table;
            ColumnName = columnName;
            _ColumnIndex = columnIndex;
            _DataTypeName = dataType;
            _MaxLength = maxLength;
            _IsInPK = isInPK;
        }

        // New Instance from data-column (24.11.2022, SME)
        public SqlColumn(SqlTable table, DataColumn column)
        {
            // error-handling
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            else if (column == null)
                throw new ArgumentNullException(nameof(column));

            // set local properties
            Table = table;
            ColumnName = column.ColumnName;
            _ColumnIndex = column.Ordinal + 1;
            _DataTypeName = column.DataType.Name;
            _DataType = column.DataType;
            _MaxLength = column.MaxLength;
            _IsInPK = DataFC.IsPkColumn(column);
        }

        // ToString
        public override string ToString()
        {
            return Table.TableFullName + ".[" + ColumnName + "] (" + Table.Database.ToString() + ")";
        }

        #endregion

        #region Properties

        // Table
        public readonly SqlTable Table;

        // ColumnIndex
        private int _ColumnIndex;
        public int ColumnIndex => _ColumnIndex;

        // ColumnName
        public readonly string ColumnName;

        // DataType
        private string _DataTypeName;
        public string DataTypeName => _DataTypeName;
        private Type _DataType;
        public Type DataType => _DataType;

        // MaxLength
        private int _MaxLength;
        public int MaxLength => _MaxLength;

        // IsInPK
        private bool _IsInPK;
        public bool IsInPK => _IsInPK;

        // IsInFK
        public bool IsInFK => Table.ParentRelations.Any(rel => rel.ChildColumnNames.Contains(ColumnName));

        #endregion

        #region Methods

        // update properties from data-column
        internal void UpdateProperties(DataColumn column)
        {
            // exit-handling
            if (column == null) throw new ArgumentNullException(nameof(column));
            if (ColumnName != column.ColumnName) throw new Exception("Ungültige Spalte");

            // update column-index
            if (ColumnIndex != column.Ordinal + 1)
                _ColumnIndex = column.Ordinal + 1;

            // update data-type
            if (DataTypeName != column.DataType.Name)
            {
                _DataTypeName = column.DataType.Name;
                _DataType = column.DataType;
            }

            // update max-length
            if (MaxLength != column.MaxLength)
                _MaxLength = column.MaxLength;

            // update is-in-pk
            if (IsInPK != DataFC.IsPkColumn(column))
                _IsInPK = !IsInPK;
        }

        #endregion
    }

    #endregion

    #region Relation

    public class SqlRelation : Core.DataObject
    {
        #region General

        // New Instance (22.11.2022, SME)
        public SqlRelation(SqlTable parentTable, string[] parentColumnNames, SqlTable childTable, string[] childColumnNames)
        {
            // error-handling
            if (parentTable == null) throw new ArgumentNullException(nameof(parentTable));
            if (parentColumnNames == null || !parentColumnNames.Any()) throw new ArgumentNullException(nameof(parentColumnNames));
            if (childTable == null) throw new ArgumentNullException(nameof(childTable));
            if (childColumnNames == null || !childColumnNames.Any()) throw new ArgumentNullException(nameof(childColumnNames));

            // set local properties
            ParentTable = parentTable;
            ParentColumnNames = parentColumnNames;
            ChildTable = childTable;
            ChildColumnNames = childColumnNames;
        }

        // ToString
        public override string ToString()
        {
            return ChildTable.TableFullName + " (" + string.Join(" + ", ChildColumnNames) + ") to " + ParentTable.TableFullName + " (" + string.Join(" + ", ParentColumnNames) + ")";
        }

        #endregion

        #region Properties

        public readonly SqlTable ParentTable;
        public readonly string[] ParentColumnNames;
        public readonly SqlTable ChildTable;
        public readonly string[] ChildColumnNames;

        #endregion

        #region Methods

        // Is Identical
        public bool IsIdentical(SqlRelation relation)
        {
            if (relation == null) return false;
            if (relation == this) return true;
            if (relation.ParentTable != this.ParentTable) return false;
            if (relation.ChildTable != this.ChildTable) return false;
            if (!CoreFC.IsIdenticalIEnumerable<string>(relation.ParentColumnNames, this.ParentColumnNames)) return false;
            if (!CoreFC.IsIdenticalIEnumerable<string>(relation.ChildColumnNames, this.ChildColumnNames)) return false;
            return true;
        }

        #endregion
    }

    #endregion
}
