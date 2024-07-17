using System;
using System.Collections.Generic;
using System.Linq;
using TC.Functions;
using TC.PDF.LIB.Classes.TC_Specific;

namespace TC.PDF.LIB.Classes
{
    #region PdfPageInfo (Core)

    // Page-Info (02.04.2024, SME)
    public class PdfPageInfo
    {
        #region General

        // Neue Instanz (02.04.2024, SME)
        protected internal PdfPageInfo(PdfFile pdfFile, int pageIndex, Exception[] errors = null)
        {
            // error-handling
            if (pdfFile == null) throw new ArgumentNullException(nameof(pdfFile));
            if (pageIndex <= 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));

            // set properties
            PdfFile = pdfFile;
            PageIndex = pageIndex;
            if (errors != null && errors.Any()) ErrorList.AddRange(errors);
        }

        // ToString
        public override string ToString()
        {
            try
            {
                return $"Seite {PageIndex:n0} von PDF '{PdfFile.FileName}'";
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        #endregion

        #region Properties

        public PdfFile PdfFile { get; }
        public int PageIndex { get; }

        #endregion

        #region Error-List-Handling

        // Properties
        private readonly List<Exception> ErrorList = new();
        public Exception[] Errors => ErrorList.ToArray();
        public int ErrorCount => ErrorList.Count;
        public bool HasErrors => ErrorList.Any();

        // Fehler hinzufügen
        protected internal void AddError(Exception error)
        {
            if (error != null && !ErrorList.Contains(error)) ErrorList.Add(error);
        }

        #endregion
    }

    #endregion

    #region PdfPageInfo with Text

    // Page-Info with Text (05.04.2024, SME)
    public class PdfPageInfoWithText: PdfPageInfo
    {
        #region General

        // Neue Instanz (05.04.2024, SME)
        protected internal PdfPageInfoWithText(PdfFile pdfFile, int pageIndex, string text = "", Exception[] errors = null): base(pdfFile, pageIndex, errors)
        {
            // set properties
            if (string.IsNullOrEmpty(text)) Text = string.Empty;
            else Text = text;
        }

        // ToString
        public override string ToString()
        {
            try
            {
                return $"{base.ToString()} (TextLength = {TextLength:n0})";
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        #endregion

        #region Text-Handling

        public string Text { get; private set; }
        public long TextLength => Text.LongCount();

        internal void SetText(string text)
        {
            try
            {
                Text = text;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion
    }

    #endregion

    #region PrintPdfPageInfo

    // Page-Info für Druck-PDF (02.04.2024, SME)
    public class PrintPdfPageInfo: PdfPageInfo
    {
        // Neue Instanz (02.04.2024, SME)
        protected internal PrintPdfPageInfo(PdfFile pdfFile, int pageIndex, PrintPdfPageTypeEnum pageType, bool istRS = false, bool istLeer = false, Exception[] errors = null) : base(pdfFile, pageIndex, errors)
        {
            // error-handling
            if (!Enum.IsDefined(typeof(PrintPdfPageTypeEnum), pageType)) throw new ArgumentOutOfRangeException(nameof(pageType));

            // set properties
            PageType = pageType;
            IstRS = istRS;
            IstLeer = istLeer;
        }

        // ToString
        public override string ToString()
        {
            try
            {
                if (IstRS)
                {
                    return $"Seite {PageIndex:n0} (RS) von PDF '{PdfFile.FileName}' ({PageType})";
                }
                else
                {
                    return $"Seite {PageIndex:n0} von PDF '{PdfFile.FileName}' ({PageType})";
                }
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        // Properties
        public PrintPdfPageTypeEnum PageType { get; }
        public bool IstRS { get; }
        public bool IstLeer { get; }
    }

    #endregion

    #region PrintPdfShipmentPageInfo

    // Page-Info für Sendungs-Seite von Druck-PDF (02.04.2024, SME)
    public class PrintPdfShipmentPageInfo : PrintPdfPageInfo
    {
        // Neue Instanz (02.04.2024, SME)
        protected internal PrintPdfShipmentPageInfo(PdfFile pdfFile, int pageIndex, SDL sdl, bool istRS = false, bool istLeer = false, Exception[] errors = null) : base(pdfFile, pageIndex, PrintPdfPageTypeEnum.Sendung, istRS, istLeer, errors)
        {
            // error-handling
            // => SDL darf nur leer sein bei Rückseiten ODER wenn Fehler gesetzt sind
            if (sdl == null && !istRS && (errors == null || !errors.Any())) throw new ArgumentNullException(nameof(sdl));

            // set properties
            SDL = sdl;
        }

        // Properties
        public SDL SDL { get; }
    }

    #endregion
}
