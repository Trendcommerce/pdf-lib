using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TC.Attributes
{
    // Table-Enum-Properties (07.12.2022, SME)
    public class TableEnumPropertiesAttribute : System.Attribute
    {
        // New Instance
        public TableEnumPropertiesAttribute(Type columnEnum, string defaultSort = "")
        {
            // error-handling
            if (columnEnum == null) throw new ArgumentNullException(nameof(columnEnum));
            if (!columnEnum.IsEnum) throw new Exception("Ungültige Spalten-Enumeration");

            // set local properties
            ColumnEnum = columnEnum;
            DefaultSort = defaultSort;
        }

        // Properties
        public readonly Type ColumnEnum;
        public string DefaultSort;
    }
}
