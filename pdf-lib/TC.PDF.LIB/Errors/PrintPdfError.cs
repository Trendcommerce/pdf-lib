using System;
using TC.PDF.LIB.Classes;

namespace TC.PDF.LIB.Errors
{
    #region PDF-Fehler

    // Fehler auf PDF-Ebene mit Error-Code (08.04.2024, SME)
    public class PrintPdfError: PdfError<PrintPdfErrorEnum>
    {
        #region General

        #region New Instance with PDF-File-Instance

        // New Instance with Inner-Exception
        public PrintPdfError(PrintPdfErrorEnum errorCode, PdfFile pdf, Exception innerException) : base(errorCode, pdf, innerException) { }

        // New Instance with Message
        public PrintPdfError(PrintPdfErrorEnum errorCode, PdfFile pdf, string message) : base(errorCode, pdf, message) { }

        // New Instance with Message + Inner-Exception
        public PrintPdfError(PrintPdfErrorEnum errorCode, PdfFile pdf, string message, Exception innerException) : base(errorCode, pdf, message, innerException) { }

        #endregion

        #endregion
    }

    #endregion

    #region PDF-Seiten-Fehler

    // Fehler auf PDF-Seiten-Ebene mit Error-Code (08.04.2024, SME)
    public class PrintPdfPageError: PdfPageError<PrintPdfErrorEnum>
    {
        #region General

        #region New Instance with PDF-File-Instance

        // New Instance with Inner-Exception
        public PrintPdfPageError(PrintPdfErrorEnum errorCode, PdfFile pdf, int pageIndex, Exception innerException) : base(errorCode, pdf, pageIndex, innerException) { }

        // New Instance with Message
        public PrintPdfPageError(PrintPdfErrorEnum errorCode, PdfFile pdf, int pageIndex, string message) : base(errorCode, pdf, pageIndex, message) { }

        // New Instance with Message + Inner-Exception
        public PrintPdfPageError(PrintPdfErrorEnum errorCode, PdfFile pdf, int pageIndex, string message, Exception innerException) : base(errorCode, pdf, pageIndex, message, innerException) { }

        #endregion

        #endregion
    }

    #endregion
}
