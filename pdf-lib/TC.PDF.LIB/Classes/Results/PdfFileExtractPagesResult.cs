using System;
using static TC.Constants.CoreConstants;

namespace TC.PDF.LIB.Classes.Results
{
    // Result of Extract Pages on PDF-File
    public class PdfFileExtractPagesResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        protected internal PdfFileExtractPagesResult(PdfFile pdfFile, int pageFrom, int pageTo) : base(PdfActionEnum.ExtractPages, pdfFile) 
        {
            InitializeParameters(pageFrom, pageTo);
        }

        // New Instance with PDF-Filepath
        protected internal PdfFileExtractPagesResult(string pdfFilePath, int pageFrom, int pageTo) : base(PdfActionEnum.ExtractPages, pdfFilePath)
        {
            InitializeParameters(pageFrom, pageTo);
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

        #region Parameters

        // Initialize Parameters
        private void InitializeParameters(int pageFrom, int pageTo)
        {
            try
            {
                // error-handling
                if (pageFrom <= 0) throw new ArgumentOutOfRangeException(nameof(pageFrom));
                if (pageTo <= 0) throw new ArgumentOutOfRangeException(nameof(pageTo));
                if (pageTo < pageFrom) throw new ArgumentOutOfRangeException(nameof(pageTo));

                // set parameters
                PageFrom = pageFrom;
                PageTo = pageTo;
                PageCount = pageTo - pageFrom + 1;

                // set output-path
                OutputFilePath = FilePath.Substring(0, FilePath.Length - DotPDF.Length) + $", S {pageFrom} - {pageTo}" + DotPDF;
            }
            catch (Exception ex)
            {
                AddError(ex);
            }
        }

        // Parameters
        public int PageFrom { get; private set; }
        public int PageTo { get; private set; }
        public new int PageCount { get; private set; }
        public string OutputFilePath { get; private set; }

        #endregion
    }
}
