using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static TC.Constants.CoreConstants;

namespace TC.Extensions
{
    // Data-Extensions (04.01.2024, SME)
    public static class DataExtensions
    {
        #region DataSet

        // Has Data (12.01.2024, SME)
        public static bool HasData(this DataSet dataSet)
        {
            if (dataSet == null) return false;
            if (dataSet.Tables.Count == 0) return false;
            return dataSet.Tables.OfType<DataTable>().Any(tbl => tbl.HasData());
        }

        #endregion

        #region DataTable

        // Has AddedOn-Column (30.01.2024, SME)
        public static bool HasAddedOnColumn(this DataTable table)
        {
            if (table == null) return false;
            return table.Columns.Contains(ColumnName_AddedOn);
        }

        // Has ChangedOn-Column (04.01.2024, SME)
        public static bool HasChangedOnColumn(this DataTable table)
        {
            if (table == null) return false;
            return table.Columns.Contains(ColumnName_ChangedOn);
        }

        // Has Data (12.01.2024, SME)
        public static bool HasData(this DataTable table)
        {
            if (table == null) return false;
            return table.Rows.Count > 0;
        }

        #endregion

        #region DataRow

        // Add Row to Table (04.01.2024, SME)
        public static void AddToTable(this DataRow row)
        {
            if (row == null) return;
            if (row.RowState != DataRowState.Detached) return;
            row.SetAddedOn();
            row.UpdateChangedOn();
            row.Table.Rows.Add(row);
        }

        // Has AddedOn-Column (30.01.2024, SME)
        public static bool HasAddedOnColumn(this DataRow row)
        {
            if (row == null) return false;
            if (row.Table == null) return false;
            return row.Table.HasAddedOnColumn();
        }

        // Has ChangedOn-Column (04.01.2024, SME)
        public static bool HasChangedOnColumn(this DataRow row)
        {
            if (row == null) return false;
            if (row.Table == null) return false;
            return row.Table.HasChangedOnColumn();
        }

        // Set AddedOn (30.01.2024, SME)
        private static void SetAddedOn(this DataRow row, bool onlyIfNotSet = true, DateTime? value = null)
        {
            if (row == null) return;
            if (!row.HasAddedOnColumn()) return;
            if (row[ColumnName_AddedOn] != DBNull.Value) return;
            if (!value.HasValue) value = DateTime.Now;
            row[ColumnName_AddedOn] = value.Value;
        }

        // Update ChangedOn (04.01.2024, SME)
        public static void UpdateChangedOn(this DataRow row, bool onlyIfChanged = true, DateTime? value = null)
        {
            if (row == null) return;
            if (!row.HasChangedOnColumn()) return;
            if (row.RowState == DataRowState.Unchanged && onlyIfChanged) return;
            if (!value.HasValue) value = DateTime.Now;
            row[ColumnName_ChangedOn] = value.Value;
        }

        // Prepare for Save (04.01.2024, SME)
        public static void PrepareForSave(this DataRow row)
        {
            // exit-handling
            if (row == null) return;
            if (row.RowState == DataRowState.Unchanged) return;

            // set added-on
            row.SetAddedOn();

            // update changed-on
            row.UpdateChangedOn();

            // add to table
            row.AddToTable();
        }

        // Is New (05.01.2024, SME)
        public static bool IsNew(this DataRow row)
        {
            if (row == null) return true;
            if (row.RowState == DataRowState.Detached) return true;
            if (row.RowState == DataRowState.Added) return true;
            return false;
        }

        // Get PK-String (12.01.2024, SME)
        public static string GetPKString(this DataRow row)
        {
            try
            {
                if (row == null) return "Row NOT SET!";
                if (row.Table == null) return "Table of Row NOT SET!";
                if (row.Table.PrimaryKey == null || !row.Table.PrimaryKey.Any()) return "Row without PK!";
                var list = new List<string>();
                foreach (DataColumn column in row.Table.PrimaryKey)
                {
                    list.Add($"{column.ColumnName} = '{row[column.ColumnName]}'");
                }
                return string.Join(" + ", list);
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }

        #endregion
    }
}
