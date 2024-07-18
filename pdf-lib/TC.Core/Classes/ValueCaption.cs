using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TC.Classes
{
    // Value + Caption (25.11.2022, SME)
    public class ValueCaption
    {
        // New Instance
        public ValueCaption(object value, string caption)
        {
            Value = value;
            Caption = caption;
        }

        // ToString
        public override string ToString()
        {
            return Caption;
        }

        // Properties
        public readonly object Value;
        public readonly string Caption;
    }

    // Value + Caption of Value-Type
    public class ValueCaption<TValueType> : ValueCaption
    {
        // New Instance
        public ValueCaption(TValueType value, string caption): base(value, caption)
        {
            Value = value;
        }

        // Properties
        public new readonly TValueType Value;
    }
}
