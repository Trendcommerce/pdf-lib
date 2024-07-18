namespace TC.Attributes
{
    public class CaptionAttribute : System.Attribute
    {
        public readonly string Caption;
        public CaptionAttribute(string caption)
        {
            Caption = caption;
        }
        public override string ToString()
        {
            return Caption;
        }
    }
}
