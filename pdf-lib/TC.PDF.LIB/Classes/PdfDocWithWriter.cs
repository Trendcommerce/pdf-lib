namespace TC.PDF.LIB.Classes
{
    public class PdfDocWithWriter : iText.Kernel.Pdf.PdfDocument
    {
        #region Neue Instanz

        public PdfDocWithWriter(string outputFilePath)
            : base(new iText.Kernel.Pdf.PdfWriter(outputFilePath)) { }
        public PdfDocWithWriter(string outputFilePath, bool useFullCompressionMode)
            : base(new iText.Kernel.Pdf.PdfWriter(outputFilePath, new iText.Kernel.Pdf.WriterProperties().UseSmartMode().SetFullCompressionMode(useFullCompressionMode))) { }
        public PdfDocWithWriter(string inputFilePath, string outputFilePath)
            : base(new iText.Kernel.Pdf.PdfReader(inputFilePath), new iText.Kernel.Pdf.PdfWriter(outputFilePath)) { }
        public PdfDocWithWriter(string inputFilePath, string outputFilePath, bool useFullCompressionMode)
            : base(new iText.Kernel.Pdf.PdfReader(inputFilePath), new iText.Kernel.Pdf.PdfWriter(outputFilePath, new iText.Kernel.Pdf.WriterProperties().UseSmartMode().SetFullCompressionMode(useFullCompressionMode))) { }

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
