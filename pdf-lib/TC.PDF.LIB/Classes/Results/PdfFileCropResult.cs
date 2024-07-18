using System;

namespace TC.PDF.LIB.Classes.Results
{
    // Crop-Result (08.06.2023, SME)
    public class PdfFileCropResult : Core.PdfFileActionResult
    {
        #region General

        // PROTECTED: New Instance with Action + PDF-File
        protected internal PdfFileCropResult(PdfActionEnum action, PdfFile pdfFile) : base(action, pdfFile) { }

        // PROTECTED: New Instance with Action + PDF-Filepath
        protected internal PdfFileCropResult(PdfActionEnum action, string pdfFilePath) : base(action, pdfFilePath) { }

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
