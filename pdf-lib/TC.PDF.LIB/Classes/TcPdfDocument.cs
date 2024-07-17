using iText.Kernel.Pdf;

namespace TC.PDF.LIB.Classes
{
    public class TcPdfDocument : PdfDocument
    {
        #region Neue Instanz

        #region Standard-Konstruktoren

        public TcPdfDocument(PdfReader reader) : base(reader) { }
        public TcPdfDocument(PdfWriter writer) : base(writer) { }
        public TcPdfDocument(PdfReader reader, DocumentProperties properties) : base(reader, properties) { }
        public TcPdfDocument(PdfReader reader, PdfWriter writer) : base(reader, writer) { }
        public TcPdfDocument(PdfWriter writer, DocumentProperties properties) : base(writer, properties) { }
        public TcPdfDocument(PdfReader reader, PdfWriter writer, StampingProperties properties) : base(reader, writer, properties) { }

        #endregion

        #region Erweiterte Konstruktoren

        // TODO

        #endregion

        #endregion

        #region OVERRIDES

        // OVERRIDE: Close
        public override void Close()
        {
            if (!this.closed)
            {
                try
                {
                    base.Close();
                }
                catch
                {
                    for (int i = 0; i < this.GetNumberOfPdfObjects(); i++)
                    {
                        iText.Kernel.Pdf.PdfObject pdfObject = this.GetPdfObject(i);
                        if (pdfObject is iText.Kernel.Pdf.PdfStream)
                        {
                            iText.Kernel.Pdf.PdfStream castedObject = (iText.Kernel.Pdf.PdfStream)pdfObject;
                            iText.Kernel.Pdf.PdfObject filters = castedObject.Get(iText.Kernel.Pdf.PdfName.Filter);
                            if (filters != null && filters.IsIndirect() && filters.IsArray())
                            {
                                iText.Kernel.Pdf.PdfArray copy = new iText.Kernel.Pdf.PdfArray((iText.Kernel.Pdf.PdfArray)filters);
                                castedObject.Put(iText.Kernel.Pdf.PdfName.Filter, copy);
                            }
                        }

                    }
                    base.Close();
                }
            }
        }

        #endregion
    }
}
