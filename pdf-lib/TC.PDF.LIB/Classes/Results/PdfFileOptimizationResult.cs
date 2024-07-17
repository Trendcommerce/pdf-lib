using System;
using System.Linq;

namespace TC.PDF.LIB.Classes.Results
{
    // Resultat von Optimierung von PDF-File (12.05.2023, SME)
    public class PdfFileOptimizationResult : PdfFileEmbedFontsResult
    {
        #region General

        // New Instance with PDF-File
        public PdfFileOptimizationResult(PdfFile pdfFile) : base(PdfActionEnum.Optimize, pdfFile) { }

        // New Instance with PDF-Filepath
        public PdfFileOptimizationResult(string pdfFilePath) : base(PdfActionEnum.Optimize, pdfFilePath) { }

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
    }
}
