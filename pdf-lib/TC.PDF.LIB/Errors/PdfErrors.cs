using System;
using TC.Interfaces;
using TC.PDF.LIB.Classes;

namespace TC.PDF.LIB.Errors
{
    #region PDF-Fehler

    // Fehler auf PDF-Ebene (28.03.2024, SME)
    public class PdfError : TC.Errors.CoreError
    {
        #region General

        #region New Instance with PDF-File-Instance

        // New Instance with Inner-Exception
        public PdfError(PdfFile pdf, Exception innerException) : base(ErrorMessage, innerException) { Initialize(pdf); }

        // New Instance with Message
        public PdfError(PdfFile pdf, string message) : base(message) { Initialize(pdf); }

        // New Instance with Message + Inner-Exception
        public PdfError(PdfFile pdf, string message, Exception innerException) : base(message, innerException) { Initialize(pdf); }

        #endregion

        #region New Instance with PDF-File-Info

        // New Instance with Inner-Exception
        public PdfError(System.IO.FileInfo pdfFileInfo, Exception innerException) : base(ErrorMessage, innerException) { Initialize(pdfFileInfo); }

        // New Instance with Message
        public PdfError(System.IO.FileInfo pdfFileInfo, string message) : base(message) { Initialize(pdfFileInfo); }

        // New Instance with Message + Inner-Exception
        public PdfError(System.IO.FileInfo pdfFileInfo, string message, Exception innerException) : base(message, innerException) { Initialize(pdfFileInfo); }

        #endregion

        #region New Instance with PDF-File-Path

        // New Instance with Inner-Exception
        public PdfError(string pdfFilePath, Exception innerException) : base(ErrorMessage, innerException) { Initialize(pdfFilePath); }

        // New Instance with Message
        public PdfError(string pdfFilePath, string message) : base(message) { Initialize(pdfFilePath); }

        // New Instance with Message + Inner-Exception
        public PdfError(string pdfFilePath, string message, Exception innerException) : base(message, innerException) { Initialize(pdfFilePath); }

        #endregion

        #region Constants

        private const string ErrorMessage = "Fehler in PDF";

        #endregion

        #endregion

        #region Initialize

        // Initialize from PDF-File-Instance (28.03.2024, SME)
        private void Initialize(PdfFile pdf)
        {
            // error-handling
            if (pdf == null) throw new ArgumentNullException(nameof(pdf));

            // set properties
            this.Pdf = pdf;
            this.PdfFilePath = pdf.FilePath;
            this.PdfFileName = pdf.FileName;
            this.PdfFileNamePure = pdf.FileNamePure;

            // add parameters
            base.AddParameter(new TC.Classes.ClsNamedParameter("PDF-Dateipfad", this.PdfFilePath));
        }

        // Initialize from File-Instance (28.03.2024, SME)
        private void Initialize(System.IO.FileInfo file)
        {
            // error-handling
            if (file == null) throw new ArgumentNullException(nameof(file));

            // set properties
            this.PdfFilePath = file.FullName;
            this.PdfFileName = file.Name;
            this.PdfFileNamePure = System.IO.Path.GetFileNameWithoutExtension(file.FullName);

            // add parameters
            base.AddParameter(new TC.Classes.ClsNamedParameter("PDF-Dateipfad", this.PdfFilePath));
        }

        // Initialize from File-Path (28.03.2024, SME)
        private void Initialize(string filePath)
        {
            // error-handling
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            // set properties
            this.PdfFilePath = filePath;
            this.PdfFileName = System.IO.Path.GetFileName(filePath);
            this.PdfFileNamePure = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // add parameters
            base.AddParameter(new TC.Classes.ClsNamedParameter("PDF-Dateipfad", this.PdfFilePath));
        }

        #endregion

        #region Properties

        // PDF
        public PdfFile Pdf { get; private set; }

        // PDF-Filepath
        public string PdfFilePath { get; private set; }

        // PDF-Filename
        public string PdfFileName { get; private set; }

        // PDF-Filename without Extension
        public string PdfFileNamePure { get; private set; }

        #endregion
    }

    #endregion

    #region PDF-Fehler mit Error-Code

    // Fehler auf PDF-Ebene mit Error-Code (08.04.2024, SME)
    public class PdfError<TErrorCodeEnum>: PdfError, IErrorWithErrorCode where TErrorCodeEnum : struct
    {
        #region General

        #region New Instance with PDF-File-Instance

        // New Instance with Inner-Exception
        public PdfError(TErrorCodeEnum errorCode, PdfFile pdf, Exception innerException) : base(pdf, innerException) { SetErrorCode(errorCode); }

        // New Instance with Message
        public PdfError(TErrorCodeEnum errorCode, PdfFile pdf, string message) : base(pdf, message) { SetErrorCode(errorCode); }

        // New Instance with Message + Inner-Exception
        public PdfError(TErrorCodeEnum errorCode, PdfFile pdf, string message, Exception innerException) : base(pdf, message, innerException) { SetErrorCode(errorCode); }

        #endregion

        #region New Instance with PDF-File-Info

        // New Instance with Inner-Exception
        public PdfError(TErrorCodeEnum errorCode, System.IO.FileInfo pdfFileInfo, Exception innerException) : base(pdfFileInfo, innerException) { SetErrorCode(errorCode); }

        // New Instance with Message
        public PdfError(TErrorCodeEnum errorCode, System.IO.FileInfo pdfFileInfo, string message) : base(pdfFileInfo, message) { SetErrorCode(errorCode); }

        // New Instance with Message + Inner-Exception
        public PdfError(TErrorCodeEnum errorCode, System.IO.FileInfo pdfFileInfo, string message, Exception innerException) : base(pdfFileInfo, message, innerException) { SetErrorCode(errorCode); }

        #endregion

        #region New Instance with PDF-File-Path

        // New Instance with Inner-Exception
        public PdfError(TErrorCodeEnum errorCode, string pdfFilePath, Exception innerException) : base(pdfFilePath, innerException) { SetErrorCode(errorCode); }

        // New Instance with Message
        public PdfError(TErrorCodeEnum errorCode, string pdfFilePath, string message) : base(pdfFilePath, message) { SetErrorCode(errorCode); }

        // New Instance with Message + Inner-Exception
        public PdfError(TErrorCodeEnum errorCode, string pdfFilePath, string message, Exception innerException) : base(pdfFilePath, message, innerException) { SetErrorCode(errorCode); }

        #endregion

        #endregion

        #region IErrorWithErrorCode-Implementation

        // Error-Code (08.04.2024, SME)
        public object ErrorCode => ErrorCodeEnum;

        #endregion

        #region Error-Code

        // Error-Code
        public TErrorCodeEnum ErrorCodeEnum { get; private set; }

        // Set Error-Code
        private void SetErrorCode(TErrorCodeEnum errorCode)
        {
            this.ErrorCodeEnum = errorCode;
            base.AddParameter(new("Error-Code", errorCode.ToString()));
        }

        #endregion
    }

    #endregion

    #region PDF-Seiten-Fehler

    // Fehler auf PDF-Seiten-Ebene (28.03.2024, SME)
    public class PdfPageError : PdfError
    {
        #region General

        #region New Instance with PDF-File-Instance

        // New Instance with Inner-Exception
        public PdfPageError(PdfFile pdf, int pageIndex, Exception innerException) : base(pdf, ErrorMessage, innerException) { Initialize(pageIndex); }

        // New Instance with Message
        public PdfPageError(PdfFile pdf, int pageIndex, string message) : base(pdf, message) { Initialize(pageIndex); }

        // New Instance with Message + Inner-Exception
        public PdfPageError(PdfFile pdf, int pageIndex, string message, Exception innerException) : base(pdf, message, innerException) { Initialize(pageIndex); }

        #endregion

        #region New Instance with PDF-File-Info

        // New Instance with Inner-Exception
        public PdfPageError(System.IO.FileInfo pdfFileInfo, int pageIndex, Exception innerException) : base(pdfFileInfo, ErrorMessage, innerException) { Initialize(pageIndex); }

        // New Instance with Message
        public PdfPageError(System.IO.FileInfo pdfFileInfo, int pageIndex, string message) : base(pdfFileInfo, message) { Initialize(pageIndex); }

        // New Instance with Message + Inner-Exception
        public PdfPageError(System.IO.FileInfo pdfFileInfo, int pageIndex, string message, Exception innerException) : base(pdfFileInfo, message, innerException) { Initialize(pageIndex); }

        #endregion

        #region New Instance with PDF-File-Path

        // New Instance with Inner-Exception
        public PdfPageError(string pdfFilePath, int pageIndex, Exception innerException) : base(pdfFilePath, ErrorMessage, innerException) { Initialize(pageIndex); }

        // New Instance with Message
        public PdfPageError(string pdfFilePath, int pageIndex, string message) : base(pdfFilePath, message) { Initialize(pageIndex); }

        // New Instance with Message + Inner-Exception
        public PdfPageError(string pdfFilePath, int pageIndex, string message, Exception innerException) : base(pdfFilePath, message, innerException) { Initialize(pageIndex); }

        #endregion

        #region Constants

        private const string ErrorMessage = "Fehler in PDF-Seite";

        #endregion

        #endregion

        #region Initialize

        // Initialize (28.03.2024, SME)
        private void Initialize(int pageIndex)
        {
            // set properties
            this.PageIndex = PageIndex;

            // add parameters
            base.AddParameter(new TC.Classes.ClsNamedParameter("Seite", this.PageIndex.ToString()));
        }

        #endregion

        #region Properties

        // Page-Index
        public int PageIndex { get; private set; }

        #endregion
    }

    #endregion

    #region PDF-Seiten-Fehler mit Error-Code

    // Fehler auf PDF-Seiten-Ebene mit Error-Code (08.04.2024, SME)
    public class PdfPageError<TErrorCodeEnum> : PdfPageError, IErrorWithErrorCode where TErrorCodeEnum : struct
    {
        #region General

        #region New Instance with PDF-File-Instance

        // New Instance with Inner-Exception
        public PdfPageError(TErrorCodeEnum errorCode, PdfFile pdf, int pageIndex, Exception innerException) : base(pdf, pageIndex, innerException) { SetErrorCode(errorCode); }

        // New Instance with Message
        public PdfPageError(TErrorCodeEnum errorCode, PdfFile pdf, int pageIndex, string message) : base(pdf, pageIndex, message) { SetErrorCode(errorCode); }

        // New Instance with Message + Inner-Exception
        public PdfPageError(TErrorCodeEnum errorCode, PdfFile pdf, int pageIndex, string message, Exception innerException) : base(pdf, pageIndex, message, innerException) { SetErrorCode(errorCode); }

        #endregion

        #region New Instance with PDF-File-Info

        // New Instance with Inner-Exception
        public PdfPageError(TErrorCodeEnum errorCode, System.IO.FileInfo pdfFileInfo, int pageIndex, Exception innerException) : base(pdfFileInfo, pageIndex, innerException) { SetErrorCode(errorCode); }

        // New Instance with Message
        public PdfPageError(TErrorCodeEnum errorCode, System.IO.FileInfo pdfFileInfo, int pageIndex, string message) : base(pdfFileInfo, pageIndex, message) { SetErrorCode(errorCode); }

        // New Instance with Message + Inner-Exception
        public PdfPageError(TErrorCodeEnum errorCode, System.IO.FileInfo pdfFileInfo, int pageIndex, string message, Exception innerException) : base(pdfFileInfo, pageIndex, message, innerException) { SetErrorCode(errorCode); }

        #endregion

        #region New Instance with PDF-File-Path

        // New Instance with Inner-Exception
        public PdfPageError(TErrorCodeEnum errorCode, string pdfFilePath, int pageIndex, Exception innerException) : base(pdfFilePath, pageIndex, innerException) { SetErrorCode(errorCode); }

        // New Instance with Message
        public PdfPageError(TErrorCodeEnum errorCode, string pdfFilePath, int pageIndex, string message) : base(pdfFilePath, pageIndex, message) { SetErrorCode(errorCode); }

        // New Instance with Message + Inner-Exception
        public PdfPageError(TErrorCodeEnum errorCode, string pdfFilePath, int pageIndex, string message, Exception innerException) : base(pdfFilePath, pageIndex, message, innerException) { SetErrorCode(errorCode); }

        #endregion

        #endregion

        #region IErrorWithErrorCode-Implementation

        // Error-Code (08.04.2024, SME)
        public object ErrorCode => ErrorCodeEnum;

        #endregion

        #region Error-Code

        // Error-Code
        public TErrorCodeEnum ErrorCodeEnum { get; private set; }

        // Set Error-Code
        private void SetErrorCode(TErrorCodeEnum errorCode)
        {
            this.ErrorCodeEnum = errorCode;
            base.AddParameter(new("Error-Code", errorCode.ToString()));
        }

        #endregion
    }

    #endregion
}
