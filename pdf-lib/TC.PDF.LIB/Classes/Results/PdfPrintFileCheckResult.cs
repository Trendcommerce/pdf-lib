using System;
using System.Collections.Generic;
using System.Linq;
using TC.Enums;
using TC.Functions;

namespace TC.PDF.LIB.Classes.Results
{
    public class PdfPrintFileCheckResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        public PdfPrintFileCheckResult(PdfFile pdfFile) : base(PdfActionEnum.CheckPrintPdf, pdfFile) { }

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

        #region PDF-Properties

        // Job-ID
        public int? JobID { get; internal set; }

        // SX / DX
        public SxDxEnum? SX_DX { get; internal set; }

        #endregion

        #region Pages

        // Liste der Seiten
        private readonly List<PrintPdfPageInfo> PageList = new();
        public PrintPdfPageInfo[] Pages => this.PageList.OrderBy(x => x.PageIndex).ToArray();

        // Seite hinzufügen
        protected internal void AddPage(PrintPdfPageInfo page)
        {
            try
            {
                // error-handling
                if (page == null) throw new ArgumentNullException(nameof(page));
                if (PageList.Contains(page)) return;
                if (PageList.Any(x => x.PageIndex == page.PageIndex))
                {
                    throw new ArgumentOutOfRangeException(nameof(page), $"Es existiert bereits eine Seite mit dem Index {page.PageIndex}!");
                }

                // add to list
                PageList.Add(page);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Error-Handling

        // Has any Errors (globally or in pages) (22.05.2024, SME)
        public bool HasAnyError => base.HasErrors || this.PageList.Any(x => x.HasErrors);

        // Count of total Errors (globally or in pages) (22.05.2024, SME)
        public int TotalErrorCount => base.ErrorCount + this.Pages.Sum(p => p.ErrorCount);

        // Get all Errors (22.05.2024, SME)
        public Exception[] GetAllErrors()
        {
            var errorList = new List<Exception>();
            errorList.AddRange(base.Errors);
            errorList.AddRange(this.PageList.SelectMany(x => x.Errors));
            return errorList.ToArray();
        }

        #endregion
    }
}
