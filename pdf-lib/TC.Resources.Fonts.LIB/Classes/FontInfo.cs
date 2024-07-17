using System;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using TC.Resources.Fonts.LIB.Functions;
using TC.Functions;

namespace TC.Resources.Fonts.LIB.Classes
{
    // Font-Info (03.04.2023, SME)
    public class FontInfo
    {
        // Neue Instanz
        public FontInfo(string fontFilePath)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(fontFilePath)) throw new ArgumentNullException(nameof(fontFilePath));
                if (!File.Exists(fontFilePath)) throw new FileNotFoundException("Font-Datei nicht gefunden.", fontFilePath);

                // set local properties
                FontFilePath = fontFilePath;
                FontFileName = Path.GetFileName(fontFilePath);
                FontFamily = FC_Fonts.GetFontFamily(fontFilePath);
                FontStyle = string.Empty;
                FontWeight = string.Empty;

                if (fontFilePath.ToLower().EndsWith(".ttf") || fontFilePath.ToLower().EndsWith(".otf"))
                {
                    // Code-Snippet von https://stackoverflow.com/questions/33254973/c-sharp-get-font-style-from-ttf
                    // oder https://stackoverflow.com/questions/4785077/how-to-determine-if-a-windows-os-font-would-support-bold-or-italic
                    var uri = new Uri(fontFilePath);
                    GlyphTypeface font = null;
                    try
                    {
                        font = new GlyphTypeface(uri);
                        FontStyle = font.Style.ToString();
                        FontWeight = font.Weight.ToString();
                    }
                    catch (Exception ex)
                    {
                        CoreFC.DPrint($"ERROR while reading font-info of '{fontFilePath}': " + ex.Message);
                        InitializeError = ex;
                    }
                    finally
                    {
                        if (font != null)
                        {
                        }
                    }
                }

                // ONLY TEMPORARY: Add Font-Style + -Weight to global Infos (16.05.2023, SME)
                //FC_Fonts.AddFontStyle(FontStyle);
                //FC_Fonts.AddFontWeight(FontWeight);

                string fontName = FontFamily.Name;
                if (FontStyle != "Normal" && !fontName.Contains(FontStyle))
                {
                    fontName += " " + FontStyle;
                }
                if (FontWeight != "Normal" && !CoreFC.IsNumeric(FontWeight) && !fontName.Contains(FontWeight))
                {
                    fontName += " " + FontWeight;
                }
                FontName = fontName;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // ToString
        public override string ToString()
        {
            try
            {
                return $"Font: Family = {FontFamily.Name}, Style = {FontStyle}, Weight = {FontWeight}, FileName = {FontFileName}";
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        public string FontFileName { get; }
        public string FontFilePath { get; }
        public System.Drawing.FontFamily FontFamily { get; }
        public string FontName { get; }

        public string FontStyle { get; }
        public string FontWeight { get; }
        public Exception InitializeError { get; }

    }
}
