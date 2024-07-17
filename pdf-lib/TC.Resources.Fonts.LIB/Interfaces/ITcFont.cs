namespace TC.Resources.Fonts.LIB.Interfaces
{
    public interface ITcFont
    {
        // FontName
        string FontName { get; }

        //// BaseFont
        //string BaseFont { get; }

        //// BaseFontPrefix + -Suffix
        //string BaseFontPrefix { get; }
        //string BaseFontSuffix { get; }

        // Encoding
        
        string Encoding { get; }

        // FontFamily
        string FontFamily { get; }

        // SubType
        string SubType { get; }

        // IsEmbedded
        bool IsEmbedded { get; }

        // IsEmbeddedSubset
        bool IsEmbeddedSubset { get; }
    }
}
