using System;
using System.Linq;
using TC.PDF.LIB.Classes;
using TC.Classes;
using TC.Functions;
using System.IO;

namespace TC.PDF.LIB.Data
{

    partial class PdfData
    {

        partial class PdfInfosDataTable
        {
            // PDF-Datei hinzufügen (08.05.2023, SME)
            public PdfInfosRow AddPDF(string pdfFilePath, ProgressInfo progressInfo = null)
            {
                try
                {
                    // exit-handling
                    if (string.IsNullOrEmpty(pdfFilePath)) return null;
                    if (this.Any(x => x.FilePath.Equals(pdfFilePath))) return this.First(x => x.FilePath.Equals(pdfFilePath));

                    // create file
                    var pdf = new PdfFile(pdfFilePath, true);

                    // create row
                    var row = this.NewPdfInfosRow();

                    // set file
                    row.PdfFile = pdf;

                    // set properties
                    row.FileName = pdf.FileName;
                    row.FileSizeInBytes = pdf.FileSize;
                    row.FileSize = pdf.FileSizeString;
                    row.IsReadOnly = pdf.IsReadOnly; // (05.07.2024, SME)
                    if (pdf.IsEncrypted.HasValue)
                    {
                        row.IsEncrypted = pdf.IsEncrypted.Value;
                    }
                    else
                    {
                        row.SetIsEncryptedNull();
                    }
                    row.PageCount = pdf.PageCount;
                    row.ObjectCount = pdf.PdfObjectCount;
                    row.ConformanceLevel = pdf.ConformanceLevel.ToString();
                    row.PdfVersion = pdf.PdfVersion;
                    row.AcrobatVersion = pdf.AcrobatVersion;
                    // row.CountUnembeddedFonts = pdf.UnembeddedFontList.Count;
                    row.FilePath = pdf.FilePath;

                    if (pdf.InitializeError != null)
                    {
                        row.InitializeErrorType = pdf.InitializeError.GetType().ToString();
                        row.InitializeErrorMessage = pdf.InitializeError.Message;
                    }

                    // add to table
                    row.Table.Rows.Add(row);
                    row.AcceptChanges();

                    // return
                    return row;
                }
                catch (Exception ex)
                {
                    CoreFC.ThrowError(ex); throw ex;
                }
            }

            // OVERRIDE: Begin Init (27.05.2023, SME)
            public override void BeginInit()
            {
                base.BeginInit();

                this.RowDeleted += PdfInfosDataTable_RowChanged;
            }

            // Event-Handler: Row changed / deleted (27.05.2023, SME)
            private void PdfInfosDataTable_RowChanged(object sender, System.Data.DataRowChangeEventArgs e)
            {
                if (e.Action == System.Data.DataRowAction.Delete)
                {
                    var row = e.Row as PdfInfosRow;
                    if (row != null) row.ClearPdfFile();
                    e.Row.AcceptChanges();
                }
            }
        }

        partial class PdfInfosRow
        {
            // PDF-File
            private PdfFile _PdfFile;
            public PdfFile PdfFile
            {
                get { return _PdfFile; }
                internal set
                {
                    if (PdfFile != null)
                    {
                        throw new Exception("PDF-File bereits gesetzt!");
                    }
                    else if (value != null)
                    {
                        _PdfFile = value;
                        InitializeInfos();

                        value.InfosChanged += PdfFile_InfosChanged;
                    }
                }
            }

            // Event-Handler: Info changed in PDF-File (27.05.2023, SME)
            private void PdfFile_InfosChanged(object sender, EventArgs e)
            {
                RefreshInfos();
            }

            // Initialize Infos (27.05.2023, SME)
            private void InitializeInfos()
            {
                // exit-handling
                if (PdfFile == null) return;

                // set properties
                this.FileName = PdfFile.FileName;
                this.FilePath = PdfFile.FilePath;
                if (PdfFile.InitializeError != null)
                {
                    this.InitializeErrorType = PdfFile.InitializeError.GetType().ToString();
                    this.InitializeErrorMessage = PdfFile.InitializeError.Message;
                }
                else
                {
                    this.SetInitializeErrorTypeNull();
                    this.SetInitializeErrorMessageNull();
                }

                // refresh properties
                RefreshInfos();
            }

            // Refresh Infos (27.05.2023, SME)
            internal void RefreshInfos()
            {
                try
                {
                    // exit-handling
                    if (PdfFile == null) return;

                    // update properties
                    this.FileSizeInBytes = PdfFile.FileSize;
                    this.FileSize = PdfFile.FileSizeString;
                    if (PdfFile.IsEncrypted.HasValue)
                    {
                        this.IsEncrypted = PdfFile.IsEncrypted.Value;
                    }
                    else
                    {
                        this.SetIsEncryptedNull();
                    }
                    this.PageCount = PdfFile.PageCount;
                    this.ObjectCount = PdfFile.PdfObjectCount;
                    this.ConformanceLevel = PdfFile.ConformanceLevel.ToString();
                }
                catch (Exception ex)
                {
                    CoreFC.DPrint("ERROR while refreshing infos in font-row: " + ex.Message);
                }
            }

            // Clear PDF-File
            internal void ClearPdfFile()
            {
                if (PdfFile != null)
                {
                    PdfFile.InfosChanged -= PdfFile_InfosChanged;
                    _PdfFile = null;
                }
            }

            // Exists (19.06.2023, SME)
            public bool Exists()
            {
                return File.Exists(FilePath);
            }
        }
    }
}
