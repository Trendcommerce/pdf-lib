using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using TC.Functions;
using static TC.Constants.CoreConstants;
using static TC.PDF.LIB.CONST_PDF;

namespace TC.PDF.LIB.Classes.Results.Core
{
    // Result of Action performed on PDF-File (12.05.2023, SME)
    public class PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        protected internal PdfFileActionResult(PdfActionEnum action, PdfFile pdfFile)
        {
            // error-handling
            if (pdfFile == null) throw new ArgumentNullException(nameof(pdfFile));

            // set local properties
            Action = action;
            StartedOn = DateTime.Now;
            FilePath = pdfFile.FilePath;
            FileName = pdfFile.FileName;
            FileSize = pdfFile.FileSize;
            FileSizeString = pdfFile.FileSizeString;
        }

        // New Instance with PDF-Filepath
        protected internal PdfFileActionResult(PdfActionEnum action, string pdfFilePath)
        {
            // error-handling
            if (string.IsNullOrEmpty(pdfFilePath)) throw new ArgumentNullException(nameof(pdfFilePath));
            if (!File.Exists(pdfFilePath)) throw new FileNotFoundException("PDF-Datei nicht gefunden!", pdfFilePath);
            if (!pdfFilePath.ToLower().EndsWith(DotPDF)) throw new ArgumentException("Ungültige PDF-Datei:" + Environment.NewLine + pdfFilePath, nameof(pdfFilePath));

            // set local properties
            Action = action;
            StartedOn = DateTime.Now;
            FilePath = pdfFilePath;
            FileName = Path.GetFileName(pdfFilePath);
            FileSize = new FileInfo(pdfFilePath).Length;
            FileSizeString = CoreFC.GetFsoSizeStringOptimal(FileSize);
        }

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

        #region Properties

        public PdfActionEnum Action { get; }
        public DateTime StartedOn { get; }
        public bool IsRunning => !EndedOn.HasValue;
        public DateTime? EndedOn { get; private set; }
        public TimeSpan Duration
        {
            get
            {
                if (IsRunning) return DateTime.Now - StartedOn;
                else return EndedOn.Value - StartedOn;
            }
        }
        public PdfFile PdfFile { get; }
        public string FileName { get; }
        public long FileSize { get; }
        public string FileSizeString { get; }
        public string FilePath { get; }
        public int? PageCount { get; protected internal set; }

        private readonly List<Exception> ErrorList = new List<Exception>();
        public virtual Exception[] Errors => ErrorList.ToArray();
        public bool HasErrors => ErrorList.Any();
        public int ErrorCount => ErrorList.Count;

        // File-Size after (moved from Compression-Result) (08.06.2023, SME)

        private long _FileSizeAfter;
        public long FileSizeAfter
        {
            get { return _FileSizeAfter; }
            private set
            {
                // error-handling
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

                // set value
                _FileSizeAfter = value;

                // calculate other values
                FileSizeStringAfter = CoreFC.GetFsoSizeStringOptimal(value);
                FileSizeDifference = FileSize - value;
                FileSizeDifferenceString = CoreFC.GetFsoSizeStringOptimal(FileSizeDifference);

                // calculate file-size-saved-percent
                if (FileSizeDifference == 0)
                {
                    FileSizeSavedPercent = 0;
                }
                else if (FileSize == 0)
                {
                    FileSizeSavedPercent = 0;
                }
                else
                {
                    var x = FileSizeDifference * 100 / FileSize;
                    FileSizeSavedPercent = (int)x;
                }
            }
        }
        public string FileSizeStringAfter { get; private set; }
        public long FileSizeDifference { get; private set; }
        public string FileSizeDifferenceString { get; private set; }

        public int FileSizeSavedPercent { get; private set; }

        #endregion

        #region Methods

        // End
        protected internal virtual void End()
        {
            if (IsRunning)
            {
                // set ended-on
                EndedOn = DateTime.Now;
                // set new size
                FileSizeAfter = new FileInfo(FilePath).Length;
            }
        }

        // End with Error
        protected internal void EndWithError(Exception error)
        {
            End();
            AddError(error, CoreFC.GetCallingMethod());
        }

        // Add Error
        protected internal void AddError(Exception error, System.Reflection.MethodBase callingMethod = null)
        {
            if (error != null)
            {
                // make sure calling method is set
                if (callingMethod == null) callingMethod = CoreFC.GetCallingMethod();
                // Log
                LogFC.WriteError(error, callingMethod);

                // Source-Exception ermitteln
                error = ExceptionDispatchInfo.Capture(error).SourceException;

                // exit wenn bereits in Liste
                if (ErrorList.Contains(error)) return;

                // Zur Liste hinzufügen
                ErrorList.Add(error);
            }
        }

        #endregion
    }
}
