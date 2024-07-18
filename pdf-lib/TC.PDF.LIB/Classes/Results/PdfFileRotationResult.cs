using System;
using TC.PDF.LIB.Classes.Parameters;

namespace TC.PDF.LIB.Classes.Results
{
    // Resultat von Rotation von PDF-File (16.10.2023, SME)
    public class PdfFileRotationResult : Core.PdfFileActionResult
    {
        #region General

        // New Instance with PDF-File
        public PdfFileRotationResult(PdfFile pdfFile, RotationParameters parameters) : base(PdfActionEnum.RotatePages, pdfFile) 
        {
            // error-handling
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            // set properties
            Parameters = parameters;
        }

        // New Instance with PDF-Filepath
        public PdfFileRotationResult(string pdfFilePath, RotationParameters parameters) : base(PdfActionEnum.RotatePages, pdfFilePath)
        {
            // error-handling
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            // set properties
            Parameters = parameters;
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

        #region Specific

        // Rotation-Parameters
        public RotationParameters Parameters { get; }

        #endregion
    }
}
