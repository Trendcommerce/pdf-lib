//using System;
//using System.IO;
//using TC.Functions;
//using static TC.PDF.LIB.CONST_PDF;

//namespace TC.PDF.LIB.Classes
//{
//    // Compression-Result of PDF-File (03.04.2023, SME)
//    public class PdfFileCompressionResult
//    {
//        #region General

//        public PdfFileCompressionResult(string pdfFilePath)
//        {
//            // error-handling
//            if (string.IsNullOrEmpty(pdfFilePath)) throw new ArgumentNullException(nameof(pdfFilePath));
//            if (!File.Exists(pdfFilePath)) throw new FileNotFoundException("PDF-Datei nicht gefunden!", pdfFilePath);
//            if (!pdfFilePath.ToLower().EndsWith(DotPDF)) throw new ArgumentException("Ungültige PDF-Datei:" + Environment.NewLine + pdfFilePath, nameof(pdfFilePath));

//            // set local properties
//            FilePath = pdfFilePath;
//            FileName = Path.GetFileName(pdfFilePath);
//            FileSize = new FileInfo(pdfFilePath).Length;
//            FileSizeString = CoreFC.GetFsoSizeStringOptimal(FileSize);
//        }

//        #endregion

//        #region Properties

//        public string FileName { get; }
//        public long FileSize { get; }
//        public string FileSizeString { get; }
//        public string FilePath { get; }

//        private long _FileSizeAfterCompression;
//        public long FileSizeAfterCompression
//        {
//            get { return _FileSizeAfterCompression; }
//            set
//            {
//                // error-handling
//                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

//                // set value
//                _FileSizeAfterCompression = value;

//                // calculate other values
//                FileSizeStringAfterCompression = CoreFC.GetFsoSizeStringOptimal(value);
//                FileSizeDifference = FileSize - value;
//                FileSizeDifferenceString = CoreFC.GetFsoSizeStringOptimal(FileSizeDifference);

//                // calculate file-size-saved-percent
//                if (FileSizeDifference == 0)
//                {
//                    FileSizeSavedPercent = 0;
//                }
//                else if (FileSize == 0)
//                {
//                    FileSizeSavedPercent = 0;
//                }
//                else
//                {
//                    var x = FileSizeDifference * 100 / FileSize;
//                    FileSizeSavedPercent = (int)x;
//                }
//            }
//        }
//        public string FileSizeStringAfterCompression { get; private set; }
//        public TimeSpan CompressionDuration { get; set; }
//        public Exception CompressionError { get; set; }

//        public long FileSizeDifference { get; private set; }
//        public string FileSizeDifferenceString { get; private set; }

//        public int FileSizeSavedPercent { get; private set; }

//        #endregion

//        #region Methods

//        // Write Info to Console
//        public void WriteInfoToConsole()
//        {
//            if (CompressionError != null)
//            {
//                CoreFC.DPrint("ERROR while compressing PDF: " +  CompressionError.Message);
//            }
//            else if (FileSizeDifference == 0)
//            {
//                CoreFC.DPrint($"PDF compressed: Size-Difference = {FileSizeDifferenceString}, Duration = {CompressionDuration}");
//            }
//            else if (FileSizeDifference > 0)
//            {
//                CoreFC.DPrint($"PDF compressed: Size-Difference = {FileSizeDifferenceString}, Duration = {CompressionDuration}");
//            }
//            else
//            {
//                CoreFC.DPrint($"PDF compressed: Size-Difference = {FileSizeDifferenceString}, Duration = {CompressionDuration}");
//            }
//        }

//        // Show Msg-Box
//        public void ShowMsgBox()
//        {
//            if (CompressionError != null)
//            {
//                string msg = $"Beim Komprimieren des PDF's '{FileName}' ist folgender Fehler aufgetreten:" + Environment.NewLine + Environment.NewLine + CompressionError.Message;
//                WinFC.ShowMsgBox(msg, "PDF-Komprimierung", System.Windows.Forms.MessageBoxIcon.Warning);
//            }
//            else
//            {
//                string msg = $"Das PDF '{FileName}' wurde erfolgreich komprimiert." + Environment.NewLine + Environment.NewLine
//                           + $"- Grösse vorher: {FileSizeString}" + Environment.NewLine
//                           + $"- Grösse danach: {FileSizeStringAfterCompression}" + Environment.NewLine
//                           + $"- Grösse gespart: {FileSizeDifferenceString}" + Environment.NewLine
//                           + $"- Dauer: {CompressionDuration}";
//                WinFC.ShowMsgBox(msg, "PDF-Komprimierung");
//            }
//        }

//        #endregion
//    }
//}
