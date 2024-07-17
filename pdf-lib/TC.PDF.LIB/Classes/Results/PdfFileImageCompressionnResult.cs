using System;

namespace TC.PDF.LIB.Classes.Results
{
    // Image-Optimization-Result of PDF-File (16.06.2023, SME)
    public class PdfFileImageCompressionnResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        public PdfFileImageCompressionnResult(PdfFile pdfFile, int maxWidthHeight = 1024) : base(PdfActionEnum.CompressImages, pdfFile) 
        {
            SetMaxWidthHeight(maxWidthHeight);
        }

        // New Instance with PDF-Filepath
        public PdfFileImageCompressionnResult(string pdfFilePath, int maxWidthHeight = 1024) : base(PdfActionEnum.CompressImages, pdfFilePath) 
        {
            SetMaxWidthHeight(maxWidthHeight);
        }

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

        // Count of compressed Images
        public int CountCompressedImages { get; internal set; } = 0;

        // Saved Image-Size
        public long SavedImageSize { get; internal set; } = 0;

        // Max Width / Height
        public int MaxWidthHeight { get; private set; }

        // Set Max Width / Height
        private void SetMaxWidthHeight(int maxWidthHeight)
        {
            if (maxWidthHeight < 100) throw new ArgumentOutOfRangeException(nameof(maxWidthHeight));
            MaxWidthHeight = maxWidthHeight;
        }

        #endregion
    }
}
