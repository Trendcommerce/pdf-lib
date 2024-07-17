using System;
using TC.Functions;

namespace TC.Data
{
    // Column-Filter (28.11.2022, SME)
    public class ColumnFilter
    {
        // New Instance with only column-name + filter-value => equal filter
        public ColumnFilter(string columnName, object filterValue) : this(columnName, "=", filterValue) {}

        // New Instance with all Properties
        public ColumnFilter(string columnName, string filterOption, object filterValue)
        {
            // error-handling
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));
            if (string.IsNullOrEmpty(filterOption)) throw new ArgumentNullException(nameof(filterOption));

            // set local properties
            ColumnName = columnName;
            FilterOption = filterOption;
            FilterValue = filterValue;
        }

        // ToString
        public override string ToString()
        {
            return "[" + ColumnName + "] " + FilterOption + " " + CoreFC.IifNullOrDbNull<object>(FilterValue, "NULL").ToString();
        }

        // Properties
        public readonly string ColumnName;
        public readonly string FilterOption;
        public readonly object FilterValue;
    }
}
