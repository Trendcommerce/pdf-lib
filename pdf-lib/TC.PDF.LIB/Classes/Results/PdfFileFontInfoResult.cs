using System;
using System.Collections.Generic;

namespace TC.PDF.LIB.Classes.Results
{
    // Font-Info-Result of PDF-File (23.06.2023, SME)
    public class PdfFileFontInfoResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        public PdfFileFontInfoResult(PdfFile pdfFile) : base(PdfActionEnum.ShowFontInfos, pdfFile) { }

        // New Instance with PDF-Filepath
        public PdfFileFontInfoResult(string pdfFilePath) : base(PdfActionEnum.ShowFontInfos, pdfFilePath) { }

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

        #region Specific

        // List of Font-Usage
        private readonly List<TcPdfFontUsage> FontUsageList = new List<TcPdfFontUsage>();
        public TcPdfFontUsage[] FontUsage => FontUsageList.ToArray();

        // Add Font-Usage
        internal TcPdfFontUsage AddFontUsage(TcPdfFont font)
        {
            var fontUsage = new TcPdfFontUsage(font);
            FontUsageList.Add(fontUsage);
            return fontUsage;
        }

        #endregion
    }
}
