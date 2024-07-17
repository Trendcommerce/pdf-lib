namespace TC.Attributes
{
    // Type-Attribute (07.06.2024, SME)
    public class TypeAttribute : System.Attribute
    {
        public TypeAttribute(System.Type type)
        {
            this.Type = type;
        }

        public readonly System.Type Type;
    }
}