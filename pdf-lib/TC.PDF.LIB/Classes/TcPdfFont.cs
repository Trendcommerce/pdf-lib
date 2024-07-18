using iText.Kernel.Pdf;
using System;
using System.Linq;
using TC.Resources.Fonts.LIB.Data;
using TC.Resources.Fonts.LIB.Interfaces;
using TC.Functions;
using static TC.Constants.CoreConstants;

namespace TC.PDF.LIB.Classes
{
    public class TcPdfFont: ITcFont
    {
        #region General

        // Neue Instanz (03.04.2023, SME)
        public TcPdfFont(PdfDictionary font)
        {
            try
            {
                // error-handling
                if (font == null) throw new ArgumentNullException(nameof(font));
                if (font.GetAsName(PdfName.Type) == null) throw new ArgumentOutOfRangeException(nameof(font));
                if (!PdfName.Font.Equals(font.GetAsName(PdfName.Type)))
                {
                    var type = font.GetAsName(PdfName.Type);
                    var fontType = PdfName.Font;
                    throw new ArgumentOutOfRangeException(nameof(font));
                }

                // set local properties
                //FontDictionary = font;
                Name = string.Empty;
                IsEmbedded = FC_PDF.IsEmbeddedFont(font);
                EncodingDifferences = string.Empty;

                // store keys
                var keys = font.KeySet();

                // loop throu keys
                foreach (var key in keys)
                {
                    // store value
                    var value = font.Get(key);
                    var valueType = value.GetType();

                    if (key.Equals(PdfName.BaseFont))
                    {
                        BaseFont = GetToString(value);
                        if (BaseFont != NullString)
                        {
                            IsEmbeddedSubset = FC_PDF.IsEmbeddedSubset(BaseFont.ToString());
                            if (IsEmbeddedSubset) IsEmbedded = true;
                        }
                    }
                    else if (key.Equals(PdfName.Encoding))
                    {
                        if (value is PdfName)
                        {
                            Encoding = GetToString(value as PdfName);
                        }
                        else if (FC_PDF.IsDictionaryOfType(value, PdfName.Encoding))
                        {
                            var encodingDic = value as PdfDictionary;
                            if (encodingDic.ContainsKey(PdfName.BaseEncoding))
                            {
                                var baseEncoding = encodingDic.Get(PdfName.BaseEncoding);
                                if (baseEncoding == null)
                                {
                                    Encoding = NullString;
                                }
                                else if (baseEncoding is PdfName)
                                {
                                    Encoding = GetToString(baseEncoding as PdfName);
                                }
                                else
                                {
                                    Encoding = NullString;
                                }
                            }
                            else
                            {
                                Encoding = NullString;
                            }
                            if (encodingDic.ContainsKey(PdfName.Differences))
                            {
                                // encoding has differences
                                var encodingDifferences = encodingDic.Get(PdfName.Differences);
                                this.EncodingDifferences = GetToString(encodingDifferences);
                            }
                        }
                        else if (value is PdfDictionary)
                        {
                            var encodingDic = value as PdfDictionary;
                            if (encodingDic.ContainsKey(PdfName.BaseEncoding))
                            {
                                var baseEncoding = encodingDic.Get(PdfName.BaseEncoding);
                                if (baseEncoding == null)
                                {
                                    Encoding = NullString;
                                }
                                else if (baseEncoding is PdfName)
                                {
                                    Encoding = GetToString(baseEncoding as PdfName);
                                }
                                else
                                {
                                    Encoding = NullString;
                                }
                                if (encodingDic.ContainsKey(PdfName.Differences))
                                {
                                    // encoding has differences
                                    var encodingDifferences = encodingDic.Get(PdfName.Differences);
                                    this.EncodingDifferences = GetToString(encodingDifferences);
                                }
                            }
                            else
                            {
                                Encoding = NullString;
                            }
                        }
                        else
                        {
                            Encoding = NullString;
                        }
                        
                    }
                    else if (key.Equals(PdfName.FontDescriptor))
                    {
                        var fontDescriptor = value as PdfDictionary;
                        if (fontDescriptor != null)
                        {
                            if (fontDescriptor.ContainsKey(PdfName.FontFamily))
                            {
                                var fontFamily = fontDescriptor.Get(PdfName.FontFamily);
                                FontFamily = GetToString(fontFamily);
                            }
                        }
                    }
                    else if (key.Equals(PdfName.Subtype))
                        SubType = GetToString(value);
                    //else if (key.Equals(PdfName.FirstChar))
                    //    FirstChar = value as PdfNumber;
                    //else if (key.Equals(PdfName.LastChar))
                    //    LastChar = value as PdfNumber;
                    //else if (key.Equals(PdfName.Widths))
                    //    Widths = value as PdfArray;
                    else if (key.Equals(PdfName.Name))
                        Name = GetToString(value);
                    else if (key.Equals(PdfName.DescendantFonts))
                        IsDescendant = true;
                    else
                    {
                        // CoreFC.DPrint($"Unhandled Key while initializing TcPdfFont: {key}");
                    }
                }

                // Base-Font-Prefix + -Suffix setzen
                if (IsEmbeddedSubset && BaseFont != null)
                {
                    BaseFontPrefix = FC_PDF.GetEmbeddedSubsetFontPrefix(font);
                    if (!string.IsNullOrEmpty(BaseFontPrefix))
                    {
                        if (BaseFont.StartsWith(BaseFontPrefix + "+"))
                        {
                            BaseFontSuffix = BaseFont.Substring(BaseFontPrefix.Length + 1);
                        }
                    }
                }

                // Font-Name setzen
                if (!string.IsNullOrEmpty(BaseFontSuffix) && BaseFontSuffix != NullString)
                {
                    FontName = BaseFontSuffix;
                }
                else if (!string.IsNullOrEmpty(BaseFont) && BaseFont != NullString)
                {
                    FontName = BaseFont;
                }
                else if (!string.IsNullOrEmpty(Name) && Name != NullString)
                {
                    FontName = Name;
                }
                else
                {
                    FontName = "?";
                }

                // Space-Replacement ersetzen in FontName (18.07.2023, SME)
                if (FontName.Contains(SpaceReplacement))
                {
                    FontName = FontName.Replace(SpaceReplacement, Space);
                }

                // Font-String setzen
                FontString = $"BaseFont = {FontName}";
                FontString += $", SubType = {SubType}";
                if (FontFamily != null) FontString += $", FontFamily = {FontFamily}";
                if (Encoding != null) FontString += $", Encoding = {Encoding}";
                FontString += $", Embedded = {IsEmbedded}";
                if (IsEmbedded) FontString += ", SubSet";
                if (IsDescendant) FontString += ", Descendant";
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
                return $"PDF-Font: {ToFontString()}";
            }
            catch (Exception ex)
            {
                CoreFC.DPrint("ERROR: " + ex.Message);
                return base.ToString();
            }
        }

        // ToFontString
        public string ToFontString()
        {
            return FontString;
        }

        #endregion

        #region Properties

        // Font-Dictionary
        // => REMARKED coz of memory-usage-optimization (14.06.2023, SME)
        // public readonly PdfDictionary FontDictionary;

        // Font-Name
        public string FontName { get; }

        // Font-String
        private readonly string FontString;

        // BaseFont
        //public PdfName BaseFont { get; }
        public string BaseFont { get; } // => GetToString(BaseFont);

        // BaseFontPrefix + -Suffix
        public string BaseFontPrefix { get; }
        public string BaseFontSuffix { get; }

        // Name
        public string Name { get; }

        // Encoding
        //public PdfName Encoding { get; }
        public string Encoding { get; } // => GetToString(Encoding);

        // Encoding-Differences
        public string EncodingDifferences { get; }

        // FontDescriptor
        // => REMARKED coz of memory-usage-optimization (14.06.2023, SME)
        // public PdfDictionary FontDescriptor { get; }

        // FontFamily
        //public PdfString FontFamily { get; }
        public string FontFamily { get; } // => GetToString(FontFamily);

        // SubType
        //public PdfName SubType { get; }
        public string SubType { get; } //=> GetToString(SubType);

        // FirstChar
        // => REMARKED coz of memory-usage-optimization (14.06.2023, SME)
        //public PdfNumber FirstChar { get; }

        // LastChar
        // => REMARKED coz of memory-usage-optimization (14.06.2023, SME)
        //public PdfNumber LastChar { get; }

        // Widths
        // => REMARKED coz of memory-usage-optimization (14.06.2023, SME)
        //public PdfArray Widths { get; }

        // IsEmbedded
        public bool IsEmbedded { get; }

        // IsEmbeddedSubset
        public bool IsEmbeddedSubset { get; }

        // IsDescendant
        public bool IsDescendant { get; }

        #endregion

        #region Methods

        // Get PDF-Name-String (03.04.2023, SME)
        private string GetPdfNameString(PdfName name)
        {
            return FC_PDF.GetPdfNameString(name, NullString);
        }

        // Get ToString (03.04.2023, SME)
        private string GetToString(PdfObject pdfObject)
        {
            if (pdfObject == null) return NullString;
            if (pdfObject is PdfName) return GetPdfNameString((PdfName)pdfObject);
            if (pdfObject is PdfString) return ((PdfString)pdfObject).ToString();
            return pdfObject.ToString();
        }

        // Get best Font-Row in Fonts-Table (20.04.2023, SME)
        public FontDataDB.FontsRow GetBestFontRow(FontDataDB.FontsDataTable fontsTable, bool onlyOKFonts = true)
        {
            if (fontsTable == null) return null;
            else return fontsTable.GetBestFontRow(this, onlyOKFonts);
        }

        #endregion
    }
}
