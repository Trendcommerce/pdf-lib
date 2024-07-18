using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TC.Functions;

namespace TC.Attributes
{
    // Column-Properties-Attribute (07.12.2022, SME)
    public class ColumnPropertiesAttribute : System.Attribute
    {
        #region General

        public ColumnPropertiesAttribute(
            Type dataType,
            bool isRequired = false,
            bool isUnique = false,
            bool isReadOnly = false,
            int maxLength = 0,
            object defaultValue = null, 
            string columnName = "")
        {
            // error-handling
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));

            // set local properties
            DataType = dataType;
            IsRequired = isRequired;
            IsUnique = isUnique;
            IsReadOnly = isReadOnly;
            MaxLength = maxLength == 0 ? null : maxLength;
            DefaultValue = defaultValue;
            ColumnName = columnName;
        }

        #endregion

        #region Properties

        public readonly string ColumnName;
        public readonly Type DataType;
        public readonly bool IsRequired;
        public readonly bool IsUnique;
        public readonly bool IsReadOnly;
        public readonly int? MaxLength;
        public readonly object DefaultValue;

        #endregion

        #region Methods

        // Get new Column
        public DataColumn GetNewColumn(string columnName = "")
        {
            if (string.IsNullOrEmpty(columnName) && string.IsNullOrEmpty(ColumnName))
                throw new Exception("Spaltenname nicht gesetzt");

            return DataFC.GetNewColumn(columnName: columnName, dataType: DataType, isRequired: IsRequired, isReadOnly: IsReadOnly, maxLength: MaxLength, defaultValue: DefaultValue);
        }

        #endregion
    }
}
