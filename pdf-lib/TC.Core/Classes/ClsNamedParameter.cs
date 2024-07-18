using System;

namespace TC.Classes
{
    public class ClsNamedParameter
    {
        public ClsNamedParameter(string name, string value)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return Name + ": " + Value;
        }

        public string Name { get; }
        public string Value { get; }
    }
}
