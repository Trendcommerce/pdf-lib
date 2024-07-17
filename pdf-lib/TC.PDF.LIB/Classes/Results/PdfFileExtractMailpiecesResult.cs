using iText.Layout.Element;
using System;
using System.Collections.Generic;
using TC.PDF.LIB.Classes.Parameters;

namespace TC.PDF.LIB.Classes.Results
{
    // Result of Extract Mailpieces on PDF-File (22.05.2024, SME)
    public class PdfFileExtractMailpiecesResult : Core.PdfFileActionResult
    {
        // New Instance with PDF-File (22.05.2024, SME)
        protected internal PdfFileExtractMailpiecesResult(PdfFile pdfFile, MailpieceExtractionParameters parameters) : base(PdfActionEnum.ExtractMailpieces, pdfFile)
        {
            InitializeParameters(parameters);
        }

        // New Instance with PDF-Filepath (22.05.2024, SME)
        protected internal PdfFileExtractMailpiecesResult(string pdfFilePath, MailpieceExtractionParameters parameters) : base(PdfActionEnum.ExtractMailpieces, pdfFilePath)
        {
            InitializeParameters(parameters);
        }

        // Initialize Parameters
        private void InitializeParameters(MailpieceExtractionParameters parameters)
        {
            // error-handling
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            // set properties
            Parameters = parameters;
        }

        // Properties
        public MailpieceExtractionParameters Parameters { get; private set; }
        private readonly List<string> MailpiecesFoundList = new();
        public string[] MailpiecesFound => MailpiecesFoundList.ToArray();

        // Methods
        internal void AddFoundMailpiece(string mailpiece)
        {
            if (!string.IsNullOrEmpty(mailpiece) && !MailpiecesFoundList.Contains(mailpiece))
            {
                MailpiecesFoundList.Add(mailpiece);
            }
        }

    }
}
