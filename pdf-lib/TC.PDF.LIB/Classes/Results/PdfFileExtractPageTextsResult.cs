using System;
using System.Collections.Generic;
using System.Linq;
using TC.Functions;

namespace TC.PDF.LIB.Classes.Results
{
    public class PdfFileExtractPageTextsResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File (05.04.2024, SME)
        public PdfFileExtractPageTextsResult(PdfFile pdfFile) : base(PdfActionEnum.ExtractPageTexts, pdfFile) { }

        #endregion

        #region Pages

        // Liste der Seiten
        private readonly List<PdfPageInfoWithText> PageList = new();
        public PdfPageInfoWithText[] Pages => this.PageList.OrderBy(x => x.PageIndex).ToArray();

        // Seite hinzufügen
        protected internal void AddPage(PdfPageInfoWithText page)
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
    }
}
