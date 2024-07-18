using System;
using System.Collections.Generic;
using System.Linq;

namespace TC.PDF.LIB.Classes.Results
{
    public class PdfFileEmbedFontsResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        public PdfFileEmbedFontsResult(PdfFile pdfFile) : base(PdfActionEnum.EmbedFonts, pdfFile) { }

        // New Instance with PDF-Filepath
        public PdfFileEmbedFontsResult(string pdfFilePath) : base(PdfActionEnum.EmbedFonts, pdfFilePath) { }

        // PROTECTED: New Instance with Action + PDF-File
        protected PdfFileEmbedFontsResult(PdfActionEnum action, PdfFile pdfFile) : base(action, pdfFile) { }

        // PROTECTED: New Instance with Action + PDF-Filepath
        protected PdfFileEmbedFontsResult(PdfActionEnum action, string pdfFilePath) : base(action, pdfFilePath) { }

        // ToString
        public override string ToString()
        {
            try
            {
                return this.GetType().Name + $": Action = {Action}, FileName = {FileName}, Size = {FileSize}, Path = {FilePath}";
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        #endregion

        #region Newly embedded Fonts

        // List of Fonts that have been embedded (12.05.2023, SME)
        private readonly List<TcPdfFont> NewlyEmbeddedFontsList = new List<TcPdfFont>();
        public TcPdfFont[] NewlyEmbeddedFonts => NewlyEmbeddedFontsList.ToArray();

        // Add Font to newly embedded (12.05.2023, SME)
        protected internal void AddFontToNewlyEmbedded(TcPdfFont font)
        {
            // exit-handling
            if (font == null) return;
            if (NewlyEmbeddedFontsList.Contains(font)) return;
            if (NewlyEmbeddedFonts.Any(x => x.ToFontString().Equals(font.ToFontString()))) return;

            // add to list
            NewlyEmbeddedFontsList.Add(font);
        }

        #endregion

        #region Still unembedded Fonts

        // List of still unembedded Fonts (12.05.2023, SME)
        private readonly List<TcPdfFont> StillUnembeddedFontsList = new List<TcPdfFont>();
        public TcPdfFont[] StillUnembeddedFonts => StillUnembeddedFontsList.ToArray();

        // Add Font to still unembedded (12.05.2023, SME)
        protected internal void AddFontToStillUnembedded(TcPdfFont font)
        {
            // exit-handling
            if (font == null) return;
            if (StillUnembeddedFontsList.Contains(font)) return;
            if (StillUnembeddedFontsList.Any(x => x.ToFontString().Equals(font.ToFontString()))) return;

            // add to list
            StillUnembeddedFontsList.Add(font);
        }

        #endregion
    }
}
