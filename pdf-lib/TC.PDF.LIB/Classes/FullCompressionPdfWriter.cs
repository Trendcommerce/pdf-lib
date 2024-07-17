using iPDF = iText.Kernel.Pdf;

namespace TC.PDF.LIB.Classes
{
    // Full-Compression-PDF-Writer
    public class FullCompressionPdfWriter : iPDF.PdfWriter
    {
        // Get Full-Compression-Writer-Properties (19.04.2023, SME)
        public static iPDF.WriterProperties GetFullCompressionWriterProperties()
        {
            var writerProperties = new iPDF.WriterProperties();
            writerProperties.UseSmartMode();
            writerProperties.SetFullCompressionMode(true);
            return writerProperties;
        }

        // New Instance by Filepath
        public FullCompressionPdfWriter(string filePath) : base(filePath, GetFullCompressionWriterProperties()) { }

        // New Instance by Filestream (18.10.2023, SME)
        public FullCompressionPdfWriter(System.IO.FileStream fileStream) : base(fileStream, GetFullCompressionWriterProperties()) { }

        // Flush
        public override void Flush()
        {
            base.Flush();
        }
    }
}
