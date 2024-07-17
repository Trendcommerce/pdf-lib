using System;

namespace TC.PDF.LIB.Classes.Results
{

    // Image-Extraction-Result of PDF-File (16.06.2023, SME)
    public class PdfFileImageExtractionResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        public PdfFileImageExtractionResult(PdfFile pdfFile) : base(PdfActionEnum.ExtractImages, pdfFile) { }

        // New Instance with PDF-Filepath
        public PdfFileImageExtractionResult(string pdfFilePath) : base(PdfActionEnum.ExtractImages, pdfFilePath) { }

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

        #region Additional

        // Count of extracted Images
        public int CountExtractedImages { get; internal set; } = 0;

        #endregion
    }
}
