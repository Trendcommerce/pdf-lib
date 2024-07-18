using System;
using System.Collections.Generic;

namespace TC.PDF.LIB.Classes.Results
{
    // Rückseiten-Prüfungs-Resultat (13.07.2023, SME)
    public class PdfFileBackpageCheckResult : Core.PdfFileActionResult
    {
        #region General

        // PROTECTED: New Instance with Action + PDF-File
        protected internal PdfFileBackpageCheckResult(PdfFile pdfFile) : base(PdfActionEnum.BackpageCheck, pdfFile) { }

        // PROTECTED: New Instance with Action + PDF-Filepath
        protected internal PdfFileBackpageCheckResult(string pdfFilePath) : base(PdfActionEnum.BackpageCheck, pdfFilePath) { }

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

        // Liste der Seiten mit Rückseite mit Inhalt
        private readonly List<int> BackpagesWithContentList = new List<int>();
        public int[] BackpagesWithContent => BackpagesWithContentList.ToArray();

        // Rückseite mit Inhalt hinzufügen
        internal void AddBackpageWithContent(int page)
        {
            if (!BackpagesWithContentList.Contains(page))
            {
                BackpagesWithContentList.Add(page);
            }
        }

        #endregion
    }
}
