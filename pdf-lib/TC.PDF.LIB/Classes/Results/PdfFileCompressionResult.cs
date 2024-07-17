using System;

namespace TC.PDF.LIB.Classes.Results
{
    // Compression-Result of PDF-File (03.04.2023, SME)
    // CHANGE: 12.05.2023 by SME: Inherit from PdfFileActionResult
    public class PdfFileCompressionResult: Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        public PdfFileCompressionResult(PdfFile pdfFile) : base(PdfActionEnum.Compress, pdfFile) { }

        // New Instance with PDF-Filepath
        public PdfFileCompressionResult(string pdfFilePath) : base(PdfActionEnum.Compress, pdfFilePath) { }

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
