using iText.Kernel.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TC.PDF.LIB.Classes.Results
{
    // Restore-Result of PDF-File (26.05.2023, SME)
    public class PdfFileRestoreResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        public PdfFileRestoreResult(PdfActionEnum action, PdfFile pdfFile, string backupFilePath) : base(action, pdfFile) 
        {
            Initialize(action, backupFilePath);
        }

        // New Instance with PDF-Filepath
        public PdfFileRestoreResult(PdfActionEnum action, string pdfFilePath, string backupFilePath) : base(action, pdfFilePath)
        {
            Initialize(action, backupFilePath);
        }

        // Initialize
        private void Initialize(PdfActionEnum action, string backupFilePath)
        {
            // check action
            if (!action.ToString().StartsWith("Restore")) throw new ArgumentOutOfRangeException(nameof(action));

            // set local properties
            BackupFilePath = backupFilePath;
            if (string.IsNullOrEmpty(backupFilePath)) BackupFileName = string.Empty;
            else BackupFileName = System.IO.Path.GetFileName(backupFilePath);
        }

        // ToString
        public override string ToString()
        {
            try
            {
                return this.GetType().Name + $": Action = {Action}, BackupFileName = {BackupFileName}, FileName = {FileName}, Size = {FileSize}, Path = {FilePath}";
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        #endregion

        #region Properties

        // Backup-Filename
        public string BackupFileName { get; private set; }

        // Backup-Filepath
        public string BackupFilePath { get; private set; }

        #endregion
    }
}
