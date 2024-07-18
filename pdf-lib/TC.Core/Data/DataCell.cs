using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TC.Data
{
    // Data-Cell (05.12.2022, SME)
    public class DataCell
    {
        // New Instance
        public DataCell(DataRow row, string columnName)
        {
            // error-handling
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (row.Table == null) throw new ArgumentNullException(nameof(row.Table));
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));
            if (!row.Table.Columns.Contains(columnName)) throw new Exception($"Spalte '{columnName}' gehört nicht zur Tabelle '{row.Table.TableName}'");

            // set local properties
            Row = row;
            ColumnName = columnName;
        }

        // Properties
        public readonly DataRow Row;
        public readonly string ColumnName;
    }

    // Data-Cell-Value (05.12.2022, SME)
    public class DataCellValue: DataCell
    {
        // New Instance
        public DataCellValue(DataRow row, string columnName): base(row, columnName)
        {
            // set local properties
            Value = row[columnName];
        }

        // Properties
        public object Value { get; set; }
    }
}
