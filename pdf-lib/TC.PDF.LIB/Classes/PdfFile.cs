using iText.Forms;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser.Filter;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TC.Classes;
using TC.Enums;
using TC.Functions;
using TC.PDF.LIB.Classes.Parameters;
using TC.PDF.LIB.Classes.Results;
using TC.PDF.LIB.Classes.Results.Core;
using TC.PDF.LIB.Classes.TC_Specific;
using TC.PDF.LIB.Errors;
using TC.Resources.Fonts.LIB.Data;
using static TC.Constants.CoreConstants;
using static TC.PDF.LIB.CONST_PDF;
using iPDF = iText.Kernel.Pdf;

namespace TC.PDF.LIB.Classes
{
    // PDF-Info (31.03.2023, SME)
    public class PdfFile : IDisposable
    {
        #region Allgemein

        // Neue Instanz (31.03.2023, SME)
        /// <summary>
        /// Neue Instanz erstellen
        /// </summary>
        /// <param name="pdfFilePath">PDF-Dateipfad</param>
        /// <param name="readInfos">Flag das definiert, ob PDF-Infos (Anzahl Seiten/PDF-Objekte, Konformität) eingelesen werden sollen oder nicht. Falls false, dann wird weder ein PDF-Reader noch ein PDF-Document geöffnet/instanziert.</param>
        /// <param name="progressInfo">Falls die Progress-Info gesetzt ist, wird der Progress-Info-Status aktualisiert bei jedem Schritt und kann in einem Frontend dargestellt werden</param>
        /// <param name="useFileNameInProgressInfoStatus">Flag das definiert, ob der PDF-Dateiname im Progress-Info-Status angezeigt werden soll oder nicht</param>
        /// <param name="keepReaderAndDocumentOpened">Flag das definiert, ob der PDF-Reader und das PDF-Document offen gelassen werden sollen für eine folgende Aktion
        /// ACHTUNG: Wird der PDF-Reader offen gelassen, so ist der Dateipfad blockiert. Soll der PDF-Reader geschlossen werden, muss auch das PDF-Document geschlossen werden, weil sonst ein Fehler auftritt.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public PdfFile(string pdfFilePath, bool readInfos = true, ProgressInfo progressInfo = null, bool useFileNameInProgressInfoStatus = false, bool keepReaderAndDocumentOpened = false)
        {

            // error-handling
            if (string.IsNullOrEmpty(pdfFilePath)) throw new ArgumentNullException(nameof(pdfFilePath));
            if (!File.Exists(pdfFilePath)) throw new FileNotFoundException("PDF-Datei nicht gefunden!", pdfFilePath);
            if (!pdfFilePath.ToLower().EndsWith(DotPDF)) throw new ArgumentException("Ungültige PDF-Datei:" + Environment.NewLine + pdfFilePath, nameof(pdfFilePath));

            // set local properties
            FilePath = pdfFilePath;
            FileName = System.IO.Path.GetFileName(pdfFilePath);
            FileNamePure = System.IO.Path.GetFileNameWithoutExtension(pdfFilePath);
            FileSize = 0;
            FileSizeString = CoreFC.GetFsoSizeStringOptimal(FileSize);
            RefreshFileInfos();
            ConformanceLevel = PdfConformanceLevelEnum.Unknown;
            PdfVersion = "?";
            AcrobatVersion = "?";
            PageCount = -1;
            PdfObjectCount = -1;

            // Read Infos
            if (readInfos)
            {
                try
                {
                    // ONLY FOR TESTING
                    var action = string.Empty;
                    var sw = Stopwatch.StartNew();

                    // "Für Dateiname"-Variable setzen
                    string forFileName = useFileNameInProgressInfoStatus ? " für " + FileName : string.Empty;

                    // reader
                    action = "Creating PDF-Reader ...";
                    //action = "Creating PDF-Reader ...";
                    //progressInfo?.SetStatus($"PDF-Reader{forFileName} wird geöffnet ...");
                    PdfReader = new iPDF.PdfReader(pdfFilePath);
                    //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw = Stopwatch.StartNew();

                    // document
                    //action = "Creating PDF-Document ...";
                    //progressInfo?.SetStatus($"PDF-Dokument{forFileName} wird geöffnet ...");
                    PdfDocument = new iPDF.PdfDocument(PdfReader);
                    //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw = Stopwatch.StartNew();

                    // Read Infos (Page-Count + Conformance-Level)
                    ReadInfos(progressInfo, useFileNameInProgressInfoStatus);

                    // Get List of unembedded Fonts
                    //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw = Stopwatch.StartNew();
                    //action = "Getting List of unembedded Fonts ...";
                    //UnembeddedFontList = FC_PDF.GetUnembeddedFonts_Old(document);
                    //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw = Stopwatch.StartNew();

                    // Close / Dispose Document + Reader
                    if (!keepReaderAndDocumentOpened)
                    {
                        // Close / Dispose Document
                        action = "Closing / Disposing PDF-Document ...";
                        //progressInfo?.SetStatus($"PDF-Dokument{forFileName} wird geschlossen ...");
                        PdfDocument = null;
                        //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw = Stopwatch.StartNew();

                        // Close / Dispose Reader
                        action = "Disposing PDF-Reader ...";
                        //progressInfo?.SetStatus($"PDF-Reader{forFileName} wird geschlossen ...");
                        PdfReader = null;
                        //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw.Stop();
                    }

                    // Uhr stoppen
                    sw.Stop();
                }
                catch (Exception ex)
                {
                    InitializeError = ex;
                }
            }
        }

        // To String (31.03.2023, SME)
        public override string ToString()
        {
            try
            {
                return $"{FileName} ({FileSizeString}) ({PageCount} Pages) ({ConformanceLevel})";
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }

        #endregion

        #region Events

        // Infos changed (27.05.2023, SME)
        public event EventHandler InfosChanged;

        // Perform DoEvents (18.10.2023, SME)
        public event EventHandler PerformDoEvents;

        #endregion

        #region IDisposable

        // Dispose (12.05.2023, SME)
        public void Dispose()
        {
            CloseReaderAndDocument();
            ActionResultList.Clear();
        }

        #endregion

        #region Properties

        public string FileName { get; }
        public string FileNamePure { get; }
        public long FileSize { get; private set; }
        public string FileSizeString { get; private set; }
        public string FilePath { get; }
        public bool IsReadOnly { get; private set; }
        public bool? IsEncrypted { get; private set; }
        public int PageCount { get; private set; }
        public int PdfObjectCount { get; private set; }
        public PdfConformanceLevelEnum ConformanceLevel { get; private set; }
        public string PdfVersion { get; private set; }
        public string AcrobatVersion { get; private set; }
        public Exception InitializeError { get; }

        // PDF-Reader (11.05.2023, SME)
        private iPDF.PdfReader _PdfReader;
        public iPDF.PdfReader PdfReader
        {
            get { return _PdfReader; }
            private set
            {
                if (PdfReader != value)
                {
                    // close current reader
                    if (PdfReader != null)
                    {
                        PdfReader.Close();
                        ((IDisposable)PdfReader).Dispose();
                    }

                    // set new value
                    _PdfReader = value;
                }
            }
        }

        // PDF-Document (11.05.2023, SME)
        private iPDF.PdfDocument _PdfDocument;
        public iPDF.PdfDocument PdfDocument
        {
            get { return _PdfDocument; }
            private set
            {
                if (PdfDocument != value)
                {
                    // close current document
                    if (PdfDocument != null)
                    {
                        PdfDocument.Close();
                        ((IDisposable)PdfDocument).Dispose();
                    }

                    // set new value
                    _PdfDocument = value;
                }
            }
        }

        // List of Action-Results (27.05.2023, SME)
        private readonly List<PdfFileActionResult> ActionResultList = new List<PdfFileActionResult>();
        public PdfFileActionResult[] ActionResults => ActionResultList.ToArray();

        #endregion

        #region Methods

        // Refresh File-Infos (27.05.2023, SME)
        // CHANGE: 05.07.2024 by SRM: RefreshFileSizeInfo => RefreshFileInfo
        // CHANGE: 05.07.2024 by SRM: Refresh IsReadOnly-Flag
        public void RefreshFileInfos()
        {
            // declarations
            bool infosChanges = false;

            try
            {
                // update infos
                if (!File.Exists(FilePath))
                {
                    // File not existing

                    // => update read-only-flag (05.07.2024, SME)
                    if (IsReadOnly != false)
                    {
                        IsReadOnly = false;
                        infosChanges = true;
                    }

                    // => update file-size (05.07.2024, SME)
                    if (FileSize != 0)
                    {
                        FileSize = 0;
                        FileSizeString = CoreFC.GetFsoSizeStringOptimal(FileSize);
                        infosChanges = true;
                    }
                }
                else
                {
                    // File existing

                    // => set file-info
                    var fileInfo = new FileInfo(FilePath);

                    // => update read-only-flag (05.07.2024, SME)
                    if (IsReadOnly != fileInfo.IsReadOnly)
                    {
                        IsReadOnly = fileInfo.IsReadOnly;
                        infosChanges = true;
                    }

                    // => update file-size (05.07.2024, SME)
                    if (FileSize != fileInfo.Length)
                    {
                        FileSize = fileInfo.Length;
                        FileSizeString = CoreFC.GetFsoSizeStringOptimal(FileSize);
                        infosChanges = true;
                    }
                }

                // raise change-event
                if (infosChanges)
                {
                    InfosChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Refresh Infos by reopening PDF-Reader and -Document (27.05.2023, SME)
        private void RefreshInfos(ProgressInfo progressInfo = null, bool useFileNameInProgressInfoStatus = false)
        {
            try
            {
                // close reader + document
                CloseReaderAndDocument();

                // check if file is locked (11.04.2024, SME)
                CoreFC.IsFileLocked_WaitMaxSeconds(FilePath, 15);

                // reopen reader + document
                PdfReader = new iPDF.PdfReader(FilePath);
                PdfDocument = new iPDF.PdfDocument(PdfReader);

                // read infos
                ReadInfos(progressInfo, useFileNameInProgressInfoStatus);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // close reader + document
                CloseReaderAndDocument();
            }

        }

        // Read Infos (12.05.2023, SME)
        private void ReadInfos(ProgressInfo progressInfo = null, bool useFileNameInProgressInfoStatus = false, bool updateProgressStatus = false)
        {
            ReadInfos(progressInfo, useFileNameInProgressInfoStatus, PdfReader, PdfDocument, updateProgressStatus);
        }

        // Read Infos from given Reader + Document (27.05.2023, SME)
        private void ReadInfos(ProgressInfo progressInfo, bool useFileNameInProgressInfoStatus, PdfReader reader, PdfDocument document, bool updateProgressStatus = false)
        {
            // Declarations
            var action = string.Empty;
            // "Für Dateiname"-Variable setzen
            string forFileName = useFileNameInProgressInfoStatus ? " für " + FileName : string.Empty;
            bool changed = false;

            // start new timer
            var sw = Stopwatch.StartNew();

            try
            {
                // Is Encrypted (25.05.2023, SME)
                if (reader != null)
                {
                    var isEncrypted = reader.IsEncrypted();
                    if (!(isEncrypted.Equals(IsEncrypted)))
                    {
                        IsEncrypted = isEncrypted;
                        changed = true;
                    }
                }

                // Get Page-Count
                if (document != null)
                {
                    try
                    {
                        action = "Getting Page-Count ...";
                        if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus($"Anzahl Seiten{forFileName} werden ermittelt ...");
                        var pageCount = document.GetNumberOfPages();
                        if (PageCount != pageCount)
                        {
                            PageCount = pageCount;
                            changed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFC.DPrint($"ERROR while getting page-count for file '{FilePath}': {ex.Message}");
                    }
                    finally
                    {
                        //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw = Stopwatch.StartNew();
                    }
                }

                // Get PDF-Object-Count
                if (document != null)
                {
                    try
                    {
                        action = "Getting PDF-Object-Count ...";
                        if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus($"Anzahl Objekte{forFileName} werden ermittelt ...");
                        var pdfObjectCount = document.GetNumberOfPdfObjects();
                        if (PdfObjectCount != pdfObjectCount)
                        {
                            PdfObjectCount = pdfObjectCount;
                            changed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFC.DPrint($"ERROR while getting pdf-object-count for file '{FilePath}': {ex.Message}");
                    }
                    finally
                    {
                        //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw = Stopwatch.StartNew();
                    }
                }

                // Get Conformance-Level
                if (reader != null)
                {
                    try
                    {
                        action = "Getting Conformance-Level ...";
                        if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus($"Konformität{forFileName} wird ermittelt ...");
                        var conformanceLevel = FC_PDF.GetConformanceLevelEnum(reader.GetPdfAConformanceLevel());
                        if (ConformanceLevel != conformanceLevel)
                        {
                            ConformanceLevel = conformanceLevel;
                            changed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFC.DPrint($"ERROR while getting conformance-level for file '{FilePath}': {ex.Message}");
                    }
                    finally
                    {
                        //CoreFC.DPrint($"{action} (Duration = {sw.Elapsed})"); sw = Stopwatch.StartNew();
                    }
                }

                // Get PDF-Version (13.07.2023, SME)
                PdfVersion = string.Empty;
                if (document != null)
                {
                    if (PdfVersion != document.GetPdfVersion().ToString())
                    {
                        PdfVersion = document.GetPdfVersion().ToString();
                        AcrobatVersion = GetAcrobatVersion(PdfVersion);
                        changed = true;
                    }
                }

                // raise change-event (27.05.2023, SME)
                if (changed) InfosChanged?.Invoke(this, EventArgs.Empty);

                // refresh file-size (27.05.2023, SME)
                RefreshFileInfos();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                sw.Stop();
            }
        }

        // Original-Datei in den Original-PDF-Ordner verschieben (12.05.2023, SME)
        private string MoveFileToOriginalPdfFolder()
        {
            // close reader + document
            CloseReaderAndDocument();

            // move file to original pdf folder
            return FC_PDF.MoveFileToOriginalPdfFolder(this.FilePath);
        }

        // Original-Datei wieder herstellen (25.05.2023, SME)
        private void RestoreOriginal(string backupFilePath)
        {
            try
            {
                // exit-handling
                if (FilePath.Equals(backupFilePath)) return;

                // error-handling
                if (string.IsNullOrEmpty(backupFilePath)) return; // throw new ArgumentNullException(nameof(backupFilePath));
                if (!File.Exists(backupFilePath)) throw new FileNotFoundException("Original kann nicht wieder hergestellt werden, weil die Backup-Datei nicht gefunden wurde!", backupFilePath);

                // delete original
                if (File.Exists(FilePath)) File.Delete(FilePath);

                // move backup
                CloseReaderAndDocument();
                File.Move(backupFilePath, FilePath);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                RefreshFileInfos();
            }
        }

        // Reader + Document schliessen (25.05.2023, SME)
        private void CloseReaderAndDocument()
        {
            // clear objects
            if (PdfDocument != null)
            {
                PdfDocument.Close();
                PdfDocument = null;
            }
            if (PdfReader != null)
            {
                PdfReader.Close();
                PdfReader = null;
            }

            // clear memory
            GC.Collect();
        }

        // PDF-Objekte ermitteln (27.05.2023, SME)
        public List<TcPdfObject> GetPdfObjects(ProgressInfo progressInfo, bool leaveReaderAndDocumentOpened = false, bool catalogsFirst = false)
        {
            var list = new List<TcPdfObject>();

            try
            {
                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(FilePath);
                }
                var reader = PdfReader;

                // document
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var document = PdfDocument;

                // read infos
                ReadInfos();

                // store object-count
                var countObjects = document.GetNumberOfPdfObjects();

                if (!catalogsFirst)
                {
                    // loop throu objects other then catalog
                    for (int i = 1; i <= countObjects; i++)
                    {
                        try
                        {
                            // get object
                            var pdfObject = document.GetPdfObject(i);

                            // skip-handling
                            if (pdfObject == null) continue;

                            // add to list
                            list.Add(new TcPdfObject(pdfObject, null));

                            // perform step
                            progressInfo?.PerformStep();
                        }
                        catch (Exception ex)
                        {
                            CoreFC.ThrowError(ex); throw ex;
                        }
                    }
                }
                else
                {
                    // loop throu catalogs
                    for (int i = 1; i <= countObjects; i++)
                    {
                        try
                        {
                            // get object
                            var pdfObject = document.GetPdfObject(i);

                            // skip-handling
                            if (pdfObject == null) continue;
                            if (!FC_PDF.IsDictionaryOfType(pdfObject, PdfName.Catalog)) continue;

                            // add to list
                            list.Add(new TcPdfObject(pdfObject, null, true, true));

                            // perform step
                            progressInfo?.PerformStep();
                        }
                        catch (Exception ex)
                        {
                            CoreFC.ThrowError(ex); throw ex;
                        }
                    }

                    // loop throu objects other then catalog
                    for (int i = 1; i <= countObjects; i++)
                    {
                        try
                        {
                            // get object
                            var pdfObject = document.GetPdfObject(i);

                            // skip-handling
                            if (pdfObject == null) continue;
                            if (FC_PDF.IsDictionaryOfType(pdfObject, PdfName.Catalog)) continue;

                            // check if pdf-object is already child-object of any loaded objects
                            bool isAlreadyInList = false;
                            foreach (var item in list)
                            {
                                if (item.IsChildObject(pdfObject, true))
                                {
                                    isAlreadyInList = true;
                                    break;
                                }
                            }
                            if (isAlreadyInList) continue;

                            // add to list
                            list.Add(new TcPdfObject(pdfObject, null));

                            // perform step
                            progressInfo?.PerformStep();
                        }
                        catch (Exception ex)
                        {
                            CoreFC.ThrowError(ex); throw ex;
                        }
                    }
                }

                // return
                return list;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // clearing
                if (!leaveReaderAndDocumentOpened) CloseReaderAndDocument();
            }
        }

        // Get all PDF-Objects that reference to given Object(s) by looping throu Pages (23.05.2023, SME)
        public Dictionary<PdfObject, List<PdfObjectReference>> GetReferencesTo(params PdfObject[] referenceTo)
        {
            return FC_PDF.GetReferencesTo(PdfDocument, referenceTo);
        }

        // Get Acrobat-Version (13.07.2023, SME)
        private string GetAcrobatVersion(string pdfVersion)
        {
            // exit-handling
            if (string.IsNullOrEmpty(pdfVersion)) return string.Empty;

            // Beispiel von PDF-Version: PDF-1.6
            // => Letztes Zeichen abhandeln
            string versionString = pdfVersion.Split('-').Last();
            if (!CoreFC.IsNumeric(versionString)) return string.Empty;
            double version = double.Parse(versionString);
            if (version >= 1.7) return "Acrobat >= 8.x";
            if (version >= 1.6) return "Acrobat 7.x";
            if (version >= 1.5) return "Acrobat 6.x";
            if (version >= 1.4) return "Acrobat 5.x";
            if (version >= 1.3) return "Acrobat 4.x";
            if (version >= 1.2) return "Acrobat 3.x";
            if (version >= 1.1) return "Acrobat 2.x";
            if (version >= 1.0) return "Acrobat 1.x";
            return string.Empty;
        }

        // Get Backup-Folderpath (07.08.2023, SME)
        private string GetBackupFolderPath()
        {
            return Path.Combine(Path.GetDirectoryName(this.FilePath), PdfBackupFolderName);
        }

        // Delete empty Backup-Folder (07.08.2023, SME)
        private void DeleteEmptyBackupFolder()
        {
            try
            {
                string backupFolderPath = GetBackupFolderPath();
                if (Directory.Exists(backupFolderPath))
                {
                    if (CoreFC.IsEmptyFolder(backupFolderPath))
                    {
                        Directory.Delete(backupFolderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Compression

        // PDF komprimieren (31.03.2023, SME)
        public PdfFileCompressionResult Compress(ProgressInfo progressInfo = null, bool keepBackup = true)
        {
            // ONLY FOR TESTING: Handle Version
            int useVersion = 1;
            switch (useVersion)
            {
                case 2:
                    return Compress_V2(progressInfo, keepBackup);
                case 3:
                    return Compress_V3(progressInfo, keepBackup);
                default:
                    // go on
                    break;
            }

            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileCompressionResult(this);

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen (09.05.2023, SME)
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"{pageCount:n0} Seiten werden komprimiert ...");
                            }
                            else
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                progressInfo.SetStatus($"{pageCount:n0} Seiten werden komprimiert ...");
                            }
                        }

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // copy page
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // flush all 100 pages to reduce memory-usage
                                if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // perform step
                                if (progressInfo != null) progressInfo.PerformStep();

                                // perform doevents every 10'000 pages to reduce memory-usage (18.10.2023, SME)
                                if (iPage % 10000 == 0) PerformDoEvents?.Invoke(this, EventArgs.Empty);
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        targetPDF.Close();
                        writer.Close();
                        sourcePDF.Close();
                        reader.Close();
                    }
                }

                // delete backup if not wanted (26.06.2023, SME)
                if (!keepBackup)
                {
                    File.Delete(inputPath);
                    this.DeleteEmptyBackupFolder();
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos(progressInfo);

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        private iPDF.PdfDocument GetNewPdfDocument(iPDF.PdfReader reader, FullCompressionPdfWriter writer)
        {
            try
            {
                if (reader != null && writer != null) return new PdfDocument(reader, writer);
                else if (reader != null) return new PdfDocument(reader);
                else if (writer != null) return new PdfDocument(writer);
                else throw new Exception("Weder PDF-Reader noch -Writer gesetzt!");
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // PDF komprimieren (V2 - Memory-Verbrauch optimiert) (17.10.2023, SME)
        private PdfFileCompressionResult Compress_V2(ProgressInfo progressInfo = null, bool keepBackup = true)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileCompressionResult(this);

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen (09.05.2023, SME)
                inputPath = MoveFileToOriginalPdfFolder();

                #region TEST 1: Reader + Writer auf selbe Datei

                // TEST: PDF-Document öffnen mit Reader + Writer auf dieselbe Datei (19.10.2023, SME)
                // Löst WIE ERWARTET einen Fehler aus
                // => Reader + Writer können NICHT auf die selbe Datei zeigen
                bool doTest1 = false;
                if (doTest1)
                {
                    try
                    {
                        using (var reader_1 = new PdfReader(inputPath))
                        {
                            using (var writer_1 = new PdfWriter(inputPath))
                            {
                                Console.WriteLine("ES FUNKTIONIERT !!! Hätte ich nicht erwartet!");
                                Debugger.Break();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Löst WIE ERWARTET einen Fehler aus
                        Console.WriteLine("Fehler wie erwartet: " + ex.Message);
                        Debugger.Break();
                    }
                }

                #endregion

                // set start-page (17.10.2023, SME)
                //int startPage = 1;
                bool reopen = false;
            ReopenFile:

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                using (var fileStream = File.OpenWrite(this.FilePath))
                {

                // writer
                using (var writer = new FullCompressionPdfWriter(fileStream))
                {
                    // target-pdf
                    using (var targetPDF = GetNewPdfDocument(null, writer))
                    {
                        // store number of pages
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // store number of target-pdf-pages
                        var pageCountTarget = targetPDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"{pageCount:n0} Seiten werden komprimiert ...");
                            }
                            else
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                progressInfo.SetStatus($"{pageCount:n0} Seiten werden komprimiert ...");
                            }
                        }

                            // copy page
                            sourcePDF.CopyPagesTo(1, pageCount, targetPDF);

                        //    // copy pages
                        //    for (int iPage = startPage; iPage <= pageCount; iPage++)
                        //{
                        //    try
                        //    {
                        //        // copy page
                        //        sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                        //        // flush all 100 pages to reduce memory-usage
                        //        if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                        //        // perform step
                        //        if (progressInfo != null) progressInfo.PerformStep();

                        //        // reopen target-file every 100'000 pages to reduce memory-usage (17.10.2023, SME)
                        //        if (iPage % 10000 == 0)
                        //        {
                        //            //reader = null; sourcePDF = null;
                        //            //CloseReaderAndDocument();

                        //            //// reader
                        //            //if (PdfReader == null)
                        //            //{
                        //            //    PdfReader = new iPDF.PdfReader(inputPath);
                        //            //}
                        //            //reader = PdfReader;

                        //            //// source-pdf
                        //            //if (PdfDocument == null)
                        //            //{
                        //            //    PdfDocument = new iPDF.PdfDocument(reader);
                        //            //}
                        //            //sourcePDF = PdfDocument;

                        //            writer.Flush();
                        //            fileStream.Flush();

                        //                PerformDoEvents?.Invoke(this, EventArgs.Empty);

                        //                //startPage = iPage + 1;
                        //                //reopen = true;
                        //                //break;
                        //            }
                        //        }
                        //    catch (Exception ex)
                        //    {
                        //        CoreFC.ThrowError(ex); throw ex;
                        //    }
                        //}

                        //// flush
                        //targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        //targetPDF.Close();
                        //writer.Close();
                        //sourcePDF.Close();
                        //reader.Close();
                    }
                    }

                }

                // reopen (17.10.2023, SME)
                if (reopen)
                {
                    reopen = false;
                    reader = null; sourcePDF = null;
                    CloseReaderAndDocument();
                    PerformDoEvents?.Invoke(this, EventArgs.Empty);
                    goto ReopenFile;
                }

                // delete backup if not wanted (26.06.2023, SME)
                if (!keepBackup)
                {
                    File.Delete(inputPath);
                    this.DeleteEmptyBackupFolder();
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos(progressInfo);

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        // PDF komprimieren (V3 - Simple Compression) (19.10.2023, SME)
        private PdfFileCompressionResult Compress_V3(ProgressInfo progressInfo = null, bool keepBackup = true)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileCompressionResult(this);

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen (09.05.2023, SME)
                inputPath = MoveFileToOriginalPdfFolder();

                // store number of pages
                var pageCount = this.PageCount;

                // start progress
                if (progressInfo != null)
                {
                    if (!progressInfo.IsRunning)
                    {
                        progressInfo.Start(pageCount, $"{pageCount:n0} Seiten werden komprimiert ...");
                    }
                    else
                    {
                        progressInfo.SetTotalSteps(pageCount);
                        progressInfo.SetStatus($"{pageCount:n0} Seiten werden komprimiert ...");
                    }
                }

                // V1 / V2
                int useVersion = 2;
                switch (useVersion)
                {
                    case 1:
                        // V1: Compression-Level = Best Compression
                        using (PdfDocument pdfDocument = new PdfDocument(new PdfReader(inputPath), new PdfWriter(this.FilePath, new WriterProperties().SetCompressionLevel(CompressionConstants.BEST_COMPRESSION))))
                        {
                            // You can add any additional processing here if needed, such as adding content, modifying the PDF, etc.
                        }
                        break;
                    case 2:
                        // V2: Smart Full Compression Mode
                        using (PdfDocument pdfDocument = new PdfDocument(new PdfReader(inputPath), new PdfWriter(this.FilePath, FullCompressionPdfWriter.GetFullCompressionWriterProperties())))
                        {
                            // You can add any additional processing here if needed, such as adding content, modifying the PDF, etc.
                        }
                        break;
                    default:
                        throw new Exception("Ungültige Compress-Version: " + useVersion);
                }

                // perform steps
                progressInfo?.PerformStep(pageCount);

                // delete backup if not wanted (26.06.2023, SME)
                if (!keepBackup)
                {
                    File.Delete(inputPath);
                    this.DeleteEmptyBackupFolder();
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos(progressInfo);

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Fonts

        #region Get Fonts

        // Liste der Schriften ermitteln von PDF-Dateipfad (06.04.2023, SME)
        public PdfFileFontInfoResult GetFonts(ProgressInfo progressInfo = null, bool onlyNotEmbedded = false, bool includePageNumbers = false)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileFontInfoResult(this);

            try
            {
                // init return-list
                var list = new Dictionary<TcPdfFont, int>();

                // get font-objects
                var fontObjectList = GetFontObjects(progressInfo, onlyNotEmbedded, false);

                // fill return-list by summaryzing font-objects
                if (fontObjectList.Any())
                {

                    // Retrieve References if flag is set (23.06.2023, SME)
                    Dictionary<PdfObject, List<PdfObjectReference>> fontsToSwitch = null;
                    if (includePageNumbers)
                    {
                        var sw = Stopwatch.StartNew();
                        fontsToSwitch = FC_PDF.GetReferencesTo(PdfDocument, fontObjectList.Where(x => !FC_PDF.IsEmbeddedFont(x)).ToArray());
                        sw.Stop();
                        Console.WriteLine($"Duration of getting references to unembedded fonts: {sw.Elapsed}");
                        //if (fontsToSwitch.Any())
                        //{
                        //    var output = new System.Text.StringBuilder();
                        //    output.AppendLine($"Embed Fonts in {FileName}");
                        //    foreach (var font in fontsToSwitch)
                        //    {
                        //        output.AppendLine($"- Switch Font '{font.Key}'");
                        //        foreach (var refObj in font.Value)
                        //        {
                        //            output.AppendLine($"  - {refObj.ReferencingObjectPath}");
                        //        }
                        //    }
                        //    CoreFC.DPrint(output.ToString());
                        //}
                    }
                    
                    // loop throu font-object + add to return-list
                    foreach (var fontObject in fontObjectList)
                    {
                        if (fontsToSwitch == null || !fontsToSwitch.Any())
                        {
                            AddFontToResult(new TcPdfFont(fontObject), result, null);
                        }
                        else if (!fontsToSwitch.ContainsKey(fontObject))
                        {
                            AddFontToResult(new TcPdfFont(fontObject), result, null);
                        } 
                        else
                        {
                            try
                            {
                                AddFontToResult(new TcPdfFont(fontObject), result, fontsToSwitch[fontObject]);
                            }
                            catch (Exception ex)
                            {
                                // end with error
                                result.EndWithError(ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);
            }
            finally
            {
                // clearing
                CloseReaderAndDocument();
            }

            // return
            ActionResultList.Add(result);
            return result;
        }

        // Font dem Result hinzufügen (06.04.2023, SME)
        private void AddFontToResult(TcPdfFont font, PdfFileFontInfoResult result, List<PdfObjectReference> references)
        {
            try
            {
                // exit-handling
                if (font == null) return;
                if (result == null) return;

                // update list
                if (!result.FontUsage.Any(x => x.Font.ToFontString().Equals(font.ToFontString())))
                {
                    // add to list
                    var fontUsage = result.AddFontUsage(font);

                    // add pages (23.06.2023, SME)
                    if (references != null && references.Any())
                    {
                        fontUsage.AddPages(references.Select(x => x.PageNumber).Distinct().OrderBy(x => x).ToArray());
                    }
                }
                else
                {
                    // update counter
                    result.FontUsage.Where(x => x.Font.ToFontString().Equals(font.ToFontString())).ToList().ForEach(x =>
                    {
                        // update counter
                        x.Count++;
                        // add pages (23.06.2023, SME)
                        if (references != null && references.Any())
                        {
                            x.AddPages(references.Select(r => r.PageNumber).Distinct().OrderBy(r => r).ToArray());
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }
        }

        #endregion

        #region Get Font-Objects

        // Liste der Font-Objekte ermitteln (26.05.2023, SME)
        public List<PdfDictionary> GetFontObjects(ProgressInfo progressInfo = null, bool onlyNotEmbedded = false)
        {
            return GetFontObjects(progressInfo, onlyNotEmbedded, true);
        }

        // PRIVATE: Liste der Font-Objekte ermitteln (26.05.2023, SME)
        private List<PdfDictionary> GetFontObjects(ProgressInfo progressInfo, bool onlyNotEmbedded, bool closeReaderAndDocument)
        {
            try
            {
                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(this.FilePath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // return
                return GetFontObjects(PdfDocument, progressInfo, onlyNotEmbedded);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // clearing
                if (closeReaderAndDocument) CloseReaderAndDocument();
            }
        }

        // PRIVATE: Liste der Font-Objekte ermitteln (26.05.2023, SME)
        private List<PdfDictionary> GetFontObjects(PdfDocument pdfDocument, ProgressInfo progressInfo, bool onlyNotEmbedded)
        {
            try
            {
                // create return-list
                var list = new List<PdfDictionary>();

                // store object-count
                var countObjects = pdfDocument.GetNumberOfPdfObjects();

                // start progress
                var performStep = false;
                if (progressInfo != null)
                {
                    if (progressInfo.IsRunning)
                    {
                        if (progressInfo.TotalSteps == 0)
                        {
                            progressInfo.SetTotalSteps(countObjects);
                            performStep = true;
                        }
                        else
                        {
                            performStep = true;
                        }
                    }
                    else
                    {
                        progressInfo.Start(countObjects);
                        performStep = true;
                    }
                }

                // set status
                if (progressInfo != null)
                {
                    if (onlyNotEmbedded)
                    {
                        //progressInfo.SetStatus("Nicht eingebettete Schrift-Objekte werden ermittelt ...");
                    }
                    else
                    {
                        //progressInfo.SetStatus("Schrift-Objekte werden ermittelt ...");
                    }
                }

                // loop throu pdf-objects
                for (int i = 1; i <= countObjects; i++)
                {
                    try
                    {
                        // store pdf-object
                        var obj = pdfDocument.GetPdfObject(i);

                        // skip-handling
                        if (!FC_PDF.IsFont(obj)) continue;
                        if (onlyNotEmbedded && FC_PDF.IsEmbeddedFont(obj)) continue;

                        // add to list
                        list.Add(obj as PdfDictionary);
                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex); throw ex;
                    }
                    finally
                    {
                        // perform step
                        if (performStep) progressInfo.PerformStep();
                    }
                } // Loop durch PDF-Objects

                // return
                return list;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Embed Fonts

        // Schriften einbetten mit Angabe von Fonts-Tabelle(n) (12.05.2023, SME)
        public PdfFileEmbedFontsResult EmbedFonts(bool onlyOKFonts, bool loopThrouAllObjects, FontDataDB.FontsDataTable fontTable)
        {
            return EmbedFonts(null, onlyOKFonts, loopThrouAllObjects, fontTable);
        }

        // Schriften einbetten mit Progress-Info und Angabe von Fonts-Tabelle(n) (12.05.2023, SME)
        public PdfFileEmbedFontsResult EmbedFonts(ProgressInfo progressInfo, bool onlyOKFonts, bool loopThrouAllObjects, FontDataDB.FontsDataTable fontTable)
        {
            return EmbedFontsPrivate(progressInfo, fontTable, onlyOKFonts, loopThrouAllObjects);
        }

        // PRIVATE: Schriften einbetten und Resultat zurückliefern (12.05.2023, SME)
        // WICHTIG: Kann auch für Optimierung verwendet werden! (27.05.2023, SME)
        private PdfFileEmbedFontsResult EmbedFontsPrivate
        (
            ProgressInfo progressInfo,
            FontDataDB.FontsDataTable fontTable,
            bool onlyOKFonts,
            bool loopThrouAllObjects,
            bool exitWhenNoUnembeddedFontsFound = true,
            PdfFileEmbedFontsResult result = null,
            FontDataDB.FontsRow fontRow = null,
            TcPdfFont[] fonts = null,
            bool keepBackup = true
        )
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            if (result == null) result = new PdfFileEmbedFontsResult(this);

            // Deklarationen
            var newFontList = new Dictionary<TcPdfFont, PdfFont>();
            bool performStep = false;
            bool endProgressAtEnd = false;
            bool? embedFonts = null;

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Auf nicht eingebettete Schriften prüfen
                if (exitWhenNoUnembeddedFontsFound)
                {
                    //if (progressInfo != null) progressInfo.SetStatus("Nicht eingebettete Schriften werden ermittelt ...");
                    var unembeddedFonts = GetFontObjects(null, true, true);
                    embedFonts = unembeddedFonts.Any();
                    if (!unembeddedFonts.Any())
                    {
                        // update number of pages in result (21.02.2024, SME)
                        result.PageCount = PageCount;

                        // perform step(s)
                        if (PageCount > 0 && progressInfo != null && progressInfo.IsRunning && progressInfo.TotalSteps > 0) progressInfo.PerformStep(PageCount);
                        // end + return
                        result.End();
                        return result;
                    }
                }

                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos(progressInfo);

                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // update number of pages in result (21.02.2024, SME)
                        result.PageCount = pageCount;

                        // set embed-flag if necessary
                        if (!embedFonts.HasValue)
                        {
                            if (fontRow != null && fonts != null && fonts.Any())
                            {
                                embedFonts = true;
                            }
                            else
                            {
                                embedFonts = GetFontObjects(sourcePDF, null, true).Any();
                            }
                        }

                        // start progress
                        if (progressInfo != null)
                        {
                            string status = embedFonts.Value ? "Schriften werden eingebettet ..." : "Seiten werden kopiert ...";
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, status);
                                performStep = true;
                                endProgressAtEnd = true;
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                progressInfo.SetStatus(status);
                                performStep = true;
                            }
                            else
                            {
                                //progressInfo.SetStatus(status);
                                performStep = true;
                            }
                        }

                        // create parameters
                        var parameters = new EmbedFontsParameters(fontTable, newFontList, targetPDF, result, onlyOKFonts, new List<PdfObject>(), progressInfo, loopThrouAllObjects, fontRow, fonts);
                        var done = new List<PdfObject>();

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // copy page
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // embed fonts
                                if (embedFonts.Value)
                                {
                                    var page = targetPDF.GetPage(iPage);
                                    this.SwitchFonts(page, parameters);
                                }

                                // perform step
                                if (performStep) progressInfo.PerformStep();

                                // flush all 100 pages to reduce memory-usage (14.06.2023, SME)
                                // => IMPORTANT: DON'T flush while embedding / switching fonts, otherwise it will not work!!! (14.06.2023, SME)
                                //if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // update infos
                        ReadInfos(progressInfo, true, reader, targetPDF);

                        // close objects
                        sourcePDF.Close();
                        targetPDF.Close();
                        writer.Close();
                        reader.Close();
                    }
                }

                // delete backup if not wanted (26.06.2023, SME)
                if (!keepBackup)
                {
                    File.Delete(inputPath);
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // clearing
                CloseReaderAndDocument();
                // refresh file-size-info
                RefreshFileInfos();
                // end progress-info
                if (progressInfo != null && endProgressAtEnd) progressInfo.End();
            }

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Switch Fonts

        // Bestimmte Fonts auswechseln (27.05.2023, SME)
        public PdfFileEmbedFontsResult SwitchFonts(ProgressInfo progressInfo, FontDataDB.FontsRow fontRow, params TcPdfFont[] fonts)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileEmbedFontsResult(this);

            try
            {
                // error-handling
                if (fontRow == null) throw new ArgumentNullException(nameof(fontRow));
                if (fonts == null || !fonts.Any()) throw new ArgumentNullException(nameof(fonts));

                // call embed-function with correct parameters
                return EmbedFontsPrivate(progressInfo, null, false, true, false, result, fontRow, fonts);
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);
            }

            // return
            ActionResultList.Add(result);
            return result;
        }

        // Fonts auswechseln von Page
        private void SwitchFonts(PdfPage page, EmbedFontsParameters parameters)
        {
            try
            {
                #region Exit-Handling

                // exit-handling
                if (page == null) return;
                if (parameters == null) return;
                if (parameters.DoneList.Contains(page.GetPdfObject())) return;

                #endregion

                #region Ressourcen ermitteln

                // Ressourcen ermitteln + prüfen
                PdfResources resources = page.GetResources();
                if (resources == null) return;

                #endregion

                #region Switch Fonts in Font

                // Switch Fonts directly
                var fonts = resources.GetResource(PdfName.Font);
                if (fonts != null)
                {
                    SwitchFonts(fonts, parameters);
                }

                #endregion

                #region Switch Fonts in XObject

                // Switch Fonts in XObject
                var xObject = resources.GetResource(PdfName.XObject);
                SwitchFontsInObject(xObject, parameters);

                #endregion

                #region Switch Fonts in all Children of Resources

                if (parameters.LoopThrouAllObjects)
                {
                    SwitchFontsInObject(resources.GetPdfObject(), parameters);
                }

                #endregion
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Fonts auswechseln von Fonts-Dictionary
        private void SwitchFonts(PdfDictionary fonts, EmbedFontsParameters parameters)
        {
            //const string TEST_FontName = @"Gilroy-Light-Identity-H";

            try
            {
                // exit-handling
                if (fonts == null) return;
                if (parameters == null) return;
                if (parameters.DoneList.Contains(fonts)) return;

                // set force-switch-flag (27.05.2023, SME)
                bool forceSwitch = (parameters.FontRow != null && parameters.Fonts != null && parameters.Fonts.Any());

                // loop throu keys
                foreach (var key in fonts.KeySet().ToArray())
                {
                    try
                    {
                        bool doSwitch = false;

                        // Font-Dictionary ermitteln
                        var oldFont = fonts.GetAsDictionary(key);

                        // Auslassen, falls Font eingebettet ist
                        // ONLY TEMPORARY remarked to test "Replace Font Gilroy-Light-Identity-H" (25.05.2023, SME)
                        if (FC_PDF.IsEmbeddedFont(oldFont) && !forceSwitch)
                        {
                            continue;
                        }

                        // TC-PDF-Font-Objekt erstellen und prüfen
                        var oldFontObject = new TcPdfFont(oldFont);
                        if (forceSwitch && parameters.Fonts.Any(x => x.ToFontString().Equals(oldFontObject.ToFontString())))
                        {
                            CoreFC.DPrint("Force to switch font");
                            doSwitch = true;
                        }
                        else if (oldFontObject.BaseFont == null)
                        {
                            // Auslassen, wenn BaseFont nicht gesetzt ist (23.05.2023, SME)
                            parameters.Result.AddFontToStillUnembedded(oldFontObject);
                            continue;
                        }

                        // HERE THE MAGIC HAPPENS

                        // get existing font or create new font
                        PdfFont newFont = null;
                        if (parameters.NewFontList.Any(x => x.Key.ToFontString().Equals(oldFontObject.ToFontString())))
                        {
                            // get existing font
                            newFont = parameters.NewFontList.First(x => x.Key.ToFontString().Equals(oldFontObject.ToFontString())).Value;
                        }
                        else
                        {
                            // create new font

                            // get the specified or best matching font-row
                            FontDataDB.FontsRow fontRow = null;
                            if (doSwitch) fontRow = parameters.FontRow;
                            else fontRow = oldFontObject.GetBestFontRow(parameters.FontsTable, parameters.OnlyOKFonts);
                            if (fontRow == null)
                            {
                                parameters.Result.AddFontToStillUnembedded(oldFontObject);
                                continue;
                            }

                            // set encoding
                            string encoding = string.Empty;
                            if (oldFontObject.Encoding == null)
                            {
                                encoding = iText.IO.Font.PdfEncodings.MACROMAN;
                            }
                            else
                            {
                                switch (oldFontObject.Encoding)
                                {
                                    case "WinAnsiEncoding":
                                        encoding = iText.IO.Font.PdfEncodings.WINANSI; break;
                                    case "MacRomanEncoding":
                                        encoding = iText.IO.Font.PdfEncodings.MACROMAN; break;
                                    case "Identity-H":
                                        encoding = iText.IO.Font.PdfEncodings.IDENTITY_H; break;
                                    case "":
                                    case "NULL":
                                        encoding = iText.IO.Font.PdfEncodings.MACROMAN;
                                        if (oldFontObject.IsEmbedded) continue;
                                        break;
                                    default:
                                        encoding = iText.IO.Font.PdfEncodings.MACROMAN;

                                        break;
                                }
                            }

                            // ADD NEW FONT
                            var fontBytes = fontRow.GetFontStream();
                            if (fontBytes == null || !fontBytes.Any())
                            {
                                continue;
                            }
                            newFont = PdfFontFactory.CreateFont(fontBytes, encoding, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
                            newFont.SetSubset(false);
                            parameters.TargetPdf.AddFont(newFont);
                            parameters.NewFontList.Add(oldFontObject, newFont);
                        }

                        // SWITCH FONT => EMBED FONT
                        //CoreFC.DPrint($"Switching Font: {oldFontObject} ({oldFont})");
                        fonts.Remove(key);
                        var newFontObject = newFont.GetPdfObject();
                        fonts.Put(key, newFontObject);
                        parameters.Result.AddFontToNewlyEmbedded(oldFontObject);

                        // ONLY FOR TESTING: to check if error will be added multiple times (12.05.2023, SME)
                        // => error will be added only once (12.05.2023, SME)
                        //throw new Exception("Test-Fehler");
                    }
                    catch (Exception ex)
                    {
                        parameters.Result.AddError(ex);
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }

                // add to done-list
                parameters.DoneList.Add(fonts);
            }
            catch (Exception ex)
            {
                parameters.Result.AddError(ex);
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Fonts auswechseln in PDF-Object
        private void SwitchFontsInObject(PdfObject pdfObject, EmbedFontsParameters parameters)
        {
            try
            {
                // exit-handling
                if (pdfObject == null) return;
                if (parameters == null) return;
                if (parameters.DoneList.Contains(pdfObject)) return;

                // handle object-type
                if (pdfObject is PdfDictionary)
                {
                    var dic = (PdfDictionary)pdfObject;
                    foreach (var key in dic.KeySet())
                    {
                        // Skip-Handling
                        if (key.Equals(PdfName.Parent)) continue;

                        // Store Value
                        var value = dic.Get(key);
                        if (value == null) continue;
                        if (parameters.DoneList.Contains(value)) continue;

                        if (key.Equals(PdfName.Font) && value is PdfDictionary)
                        {
                            SwitchFonts((PdfDictionary)value, parameters);
                        }
                        else
                        {
                            SwitchFontsInObject(value, parameters);
                        }
                    }

                    // add to done
                    parameters.DoneList.Add(pdfObject);
                }
                else if (pdfObject is PdfArray)
                {
                    var arr = (PdfArray)pdfObject;
                    foreach (var item in arr)
                    {
                        SwitchFontsInObject(item, parameters);
                    }

                    // add to done
                    parameters.DoneList.Add(pdfObject);
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #endregion

        #region Optimization

        // PDF optimieren mit Angabe von Fonts-Tabelle(n) (12.05.2023, SME)
        public PdfFileOptimizationResult Optimize(bool onlyOKFonts, bool loopThrouAllObjects, FontDataDB.FontsDataTable fontsTable, bool keepBackup = true)
        {
            return OptimizePrivate(null, onlyOKFonts, loopThrouAllObjects, fontsTable, keepBackup);
        }

        // PDF optimieren mit Progress-Info und Angabe von Fonts-Tabellen (12.05.2023, SME)
        public PdfFileOptimizationResult Optimize(ProgressInfo progressInfo, bool onlyOKFonts, bool loopThrouAllObjects, FontDataDB.FontsDataTable fontTable, bool keepBackup = true)
        {
            return OptimizePrivate(progressInfo, onlyOKFonts, loopThrouAllObjects, fontTable, keepBackup);
        }

        // PRIVATE: PDF optimieren (12.05.2023, SME)
        private PdfFileOptimizationResult OptimizePrivate(ProgressInfo progressInfo, bool onlyOKFonts, bool loopThrouAllObjects, FontDataDB.FontsDataTable fontTable, bool keepBackup = true)
        {
            // set result (error will be raised if invalid pdf-file)
            var result = new PdfFileOptimizationResult(this);

            // embed fonts (this will compress as well)
            result = EmbedFontsPrivate(progressInfo, fontTable, onlyOKFonts, loopThrouAllObjects, false, result, null, null, keepBackup) as PdfFileOptimizationResult;

            // return
            return result;
        }

        #endregion

        #region Extract Pages

        // Seiten extrahieren von PDF (15.06.2023, SME)
        public PdfFileExtractPagesResult ExtractPages(int pageFrom, int pageTo, ProgressInfo progressInfo = null)
        {
            // init result
            var result = new PdfFileExtractPagesResult(this, pageFrom, pageTo);

            // exit-handling
            if (result.HasErrors) return result;

            try
            {
                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(FilePath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Seiten-Anzahl ermitteln + prüfen
                int pageCount = sourcePDF.GetNumberOfPages();
                if (pageCount < pageTo)
                {
                    // TODO: Dani/André fragen, ob hier ein Fehler ausgelöst werden soll, oder einfach die maximale Seitenzahl verwendet werden soll
                    throw new Exception($"Die gewünschte Seite Bis ({pageTo}) übersteigt die max. Anzahl Seiten ({pageCount})!");
                }

                // Prüfen ob Verschlüsselt
                // => egal bei ExtractPages (15.06.2023, SME)
                //if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                //{
                //    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                //}

                // set output-path
                string outputPath = FilePath.Substring(0, FilePath.Length - DotPDF.Length) + $", S {pageFrom} - {pageTo}" + DotPDF;

                // writer
                using (var writer = new FullCompressionPdfWriter(outputPath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // copy pages
                        for (int iPage = pageFrom; iPage <= pageTo; iPage++)
                        {
                            // copy page
                            sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                            // flush all 100 pages to reduce memory-usage
                            if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                            // perform step
                            progressInfo?.PerformStep();
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        targetPDF.Close();
                        writer.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                // => Nicht nötig, da das Original nicht verändert wird
                //RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                // braucht es für ExtractPages nicht (15.06.2023, SME)
                //RefreshInfos();

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Backup / Restore

        // Get Backup-Filepaths (26.05.2023, SME)
        public List<string> GetBackupFilePaths()
        {
            return FC_PDF.GetBackupFilePaths(FilePath);
        }

        // Restore Original (= first Version) (26.05.2023, SME)
        public PdfFileRestoreResult RestoreOriginal()
        {
            return Restore(PdfActionEnum.RestoreOriginal, string.Empty);
        }

        // Restore latest Version (26.05.2023, SME)
        public PdfFileRestoreResult RestoreLatestVersion()
        {
            return Restore(PdfActionEnum.RestoreLatestVersion, string.Empty);
        }

        // Restore specific Version (26.05.2023, SME)
        public PdfFileRestoreResult Restore(string backupFilePath)
        {
            return Restore(PdfActionEnum.RestoreSpecificVersion, backupFilePath);
        }

        // PRIVATE: Restore (26.05.2023, SME)
        private PdfFileRestoreResult Restore(PdfActionEnum action, string backupFilePath)
        {
            // init result
            var result = new PdfFileRestoreResult(action, this, backupFilePath);

            try
            {
                // set backup-filepath if necessary
                if (string.IsNullOrEmpty(backupFilePath))
                {
                    switch (action)
                    {
                        case PdfActionEnum.RestoreOriginal:
                        case PdfActionEnum.RestoreLatestVersion:

                            // get backup-filepaths
                            var backupFilePaths = GetBackupFilePaths();
                            if (backupFilePaths.Any())
                            {
                                // set backup-filepath according to action
                                if (action == PdfActionEnum.RestoreOriginal)
                                {
                                    backupFilePath = backupFilePaths.First();
                                }
                                else
                                {
                                    backupFilePath = backupFilePaths.Last();
                                }
                            }

                            break;
                        default:
                            break;
                    }
                }

                // exit-handling
                if (string.IsNullOrEmpty(backupFilePath))
                {
                    result.End();
                    return result;
                }

                // restore
                FC_PDF.RestorePDF(backupFilePath, true);

                // end
                result.End();
            }
            catch (Exception ex)
            {
                result.EndWithError(ex);
            }
            finally
            {
                // refresh infos
                RefreshInfos();
            }

            // return
            return result;
        }

        #endregion

        #region Crop

        // Crop (08.06.2023, SME)
        public PdfFileCropResult Crop(PdfActionEnum cropAction, ProgressInfo progressInfo = null, bool updateProgressStatus = false)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileCropResult(cropAction, this);

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Action prüfen
                if (cropAction != PdfActionEnum.Crop_3_12_mm && cropAction != PdfActionEnum.Crop_A4) throw new InvalidOperationException("Ungültige Crop-Aktion: " + cropAction.ToString());

                // Prüfen ob verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus("PDF-Reader wird initialisiert ...");
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus("Quell-PDF-Dokument wird initialisiert ...");
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos(progressInfo, false, updateProgressStatus);

                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Grösse ermitteln
                float widthTo_Trim = iText.Kernel.Geom.PageSize.A4.GetWidth();
                float heightTo_Trim = iText.Kernel.Geom.PageSize.A4.GetHeight();
                float zweimal_3mm = 2 * FC_PDF.MillimetersToPoints(3);
                float zweimal_12mm = 2 * FC_PDF.MillimetersToPoints(12);
                float width, height;

                // writer
                if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus("PDF-Writer wird initialisiert ...");
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus("Ziel-PDF-Dokument wird initialisiert ...");
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus("Anzahl Seiten werden ermittelt ...");
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"{pageCount:n0} Seiten werden zugeschnitten ...");
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                progressInfo.SetStatus($"{pageCount:n0} Seiten werden zugeschnitten ...");
                            }
                        }

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // copy page
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // store page
                                var pdfPage = targetPDF.GetPage(iPage);

                                // store size
                                height = pdfPage.GetMediaBox().GetHeight();
                                width = pdfPage.GetMediaBox().GetWidth();

                                // set new size
                                if (cropAction == PdfActionEnum.Crop_A4)
                                {
                                    // A4
                                    height = height > heightTo_Trim ? (height - heightTo_Trim) / 2 : 0;
                                    width = width > widthTo_Trim ? (width - widthTo_Trim) / 2 : 0;
                                }
                                else if (cropAction == PdfActionEnum.Crop_3_12_mm)
                                {
                                    // 3/12 mm
                                    heightTo_Trim = height - zweimal_3mm;
                                    widthTo_Trim = width - zweimal_12mm;
                                    height = height > heightTo_Trim ? (height - heightTo_Trim) / 2 : 0;
                                    width = width > widthTo_Trim ? (width - widthTo_Trim) / 2 : 0;
                                }

                                // crop
                                if (height > 0 || width > 0)
                                {
                                    var rectPortrait = new iText.Kernel.Geom.Rectangle(width, height, widthTo_Trim, heightTo_Trim);
                                    pdfPage.SetCropBox(rectPortrait);
                                }

                                // flush all 100 pages to reduce memory-usage
                                if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // perform step
                                progressInfo?.PerformStep();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        if (progressInfo != null && updateProgressStatus) progressInfo.SetStatus("Zugeschnittenes PDF wird geschrieben ...");
                        sourcePDF.Close();
                        targetPDF.Close();
                        writer.Close();
                        reader.Close();
                    }
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos(progressInfo);

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        // Crop to A4 (08.06.2023, SME)
        public PdfFileCropResult Crop_A4(ProgressInfo progressInfo = null, bool updateProgressStatus = false)
        {
            return Crop(PdfActionEnum.Crop_A4, progressInfo, updateProgressStatus);
        }

        // Crop to 3/12 mm (08.06.2023, SME)
        public PdfFileCropResult Crop_3_12_mm(ProgressInfo progressInfo = null, bool updateProgressStatus = false)
        {
            return Crop(PdfActionEnum.Crop_3_12_mm, progressInfo, updateProgressStatus);
        }

        #endregion

        #region Zwischenseite(n) einfügen

        // Zwischenseite(n) einfügen (14.06.2023, SME)
        public PdfFileActionResult InsertSeparatorPage(string separatorPdfFilePath, int separatorPdfAfterPageCount, ProgressInfo progressInfo = null)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileActionResult(PdfActionEnum.InsertSeparatorPage, this);

            // init input-path
            string inputPath = FilePath;

            // declarations
            PdfFile separatorPdf = null;
            iPDF.PdfReader separatorPdfReader = null;
            iPDF.PdfDocument separatorPdfDocument = null;
            int separatorPdfPageCount = 0;

            try
            {
                // error-handling
                if (string.IsNullOrEmpty(separatorPdfFilePath)) throw new ArgumentNullException(nameof(separatorPdfFilePath));
                if (separatorPdfFilePath.Equals(FilePath)) throw new ArgumentOutOfRangeException(nameof(separatorPdfFilePath));
                if (separatorPdfAfterPageCount <= 0) throw new ArgumentOutOfRangeException(nameof(separatorPdfAfterPageCount));

                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Zwischenseiten-PDF prüfen + setzen
                separatorPdf = new PdfFile(separatorPdfFilePath);
                separatorPdfReader = new iPDF.PdfReader(separatorPdf.FilePath);
                separatorPdfDocument = new iPDF.PdfDocument(separatorPdfReader);
                separatorPdfPageCount = separatorPdfDocument.GetNumberOfPages();

                // Original-Datei backupen
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    //progressInfo?.SetStatus("PDF-Reader wird initialisiert ...");
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    //progressInfo?.SetStatus("Quell-PDF-Dokument wird initialisiert ...");
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                //progressInfo?.SetStatus("PDF-Writer wird initialisiert ...");
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    //progressInfo?.SetStatus("Ziel-PDF-Dokument wird initialisiert ...");
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        //progressInfo?.SetStatus("Anzahl Seiten werden ermittelt ...");
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"{pageCount:n0} Seiten werden kopiert ...");
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                //progressInfo.SetStatus($"{pageCount:n0} Seiten werden kopiert ...");
                            }
                        }

                        // set next insert-page-number
                        int insertPageNumber = separatorPdfAfterPageCount;

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // copy page
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // flush all 100 pages to reduce memory-usage
                                if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // insert page(s)
                                if (iPage == insertPageNumber)
                                {
                                    for (int iInsertPage = 1; iInsertPage <= separatorPdfPageCount; iInsertPage++)
                                    {
                                        separatorPdfDocument.CopyPagesTo(iInsertPage, iInsertPage, targetPDF);
                                    }
                                    insertPageNumber += separatorPdfAfterPageCount;
                                }

                                // perform step
                                progressInfo?.PerformStep();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        //progressInfo?.SetStatus("PDF wird geschrieben ...");
                        sourcePDF.Close();
                        targetPDF.Close();
                        writer.Close();
                        reader.Close();
                    }
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos(progressInfo);

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        // Leerseite einfügen (05.07.2024, SME)
        public PdfFileActionResult InsertEmptyPage(int emptyPageAfterPageCount, ProgressInfo progressInfo = null)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileActionResult(PdfActionEnum.InsertEmptyPage, this);

            // init input-path
            string inputPath = FilePath;

            try
            {
                // error-handling
                if (emptyPageAfterPageCount <= 0) throw new ArgumentOutOfRangeException(nameof(emptyPageAfterPageCount));

                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    //progressInfo?.SetStatus("PDF-Reader wird initialisiert ...");
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    //progressInfo?.SetStatus("Quell-PDF-Dokument wird initialisiert ...");
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                //progressInfo?.SetStatus("PDF-Writer wird initialisiert ...");
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    //progressInfo?.SetStatus("Ziel-PDF-Dokument wird initialisiert ...");
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        //progressInfo?.SetStatus("Anzahl Seiten werden ermittelt ...");
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"{pageCount:n0} Seiten werden kopiert ...");
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                //progressInfo.SetStatus($"{pageCount:n0} Seiten werden kopiert ...");
                            }
                        }

                        // set next insert-page-number
                        int insertPageNumber = emptyPageAfterPageCount;

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // copy page
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // flush all 100 pages to reduce memory-usage
                                if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // insert page(s)
                                if (iPage == insertPageNumber)
                                {
                                    targetPDF.AddNewPage();
                                    insertPageNumber += emptyPageAfterPageCount;
                                }

                                // perform step
                                progressInfo?.PerformStep();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        //progressInfo?.SetStatus("PDF wird geschrieben ...");
                        sourcePDF.Close();
                        targetPDF.Close();
                        writer.Close();
                        reader.Close();
                    }
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos(progressInfo);

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Remove empty Pages

        // Leere Seiten entfernen (20.04.2023, SME)
        public PdfFileActionResult RemoveEmptyPages(ProgressInfo progressInfo = null)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileActionResult(PdfActionEnum.RemoveEmptyPages, this);

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"Leere Seiten werden entfernt ...");
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                progressInfo.SetStatus($"Leere Seiten werden entfernt ...");
                            }
                        }

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // Seite ermitteln
                                var page = sourcePDF.GetPage(iPage);

                                // Prüfen ob Seite leer ist
                                if (!FC_PDF.IsEmptyPage(page))
                                {
                                    // Seite kopieren, da nicht leer
                                    sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);
                                }

                                // flush all 100 pages to reduce memory-usage
                                if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // perform step
                                progressInfo?.PerformStep();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        targetPDF.Close();
                        writer.Close();
                    }
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos();

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Multipy Pages

        // Seiten multiplizieren (20.04.2023, SME)

        public PdfFileActionResult MultiplyPages(int factor, ProgressInfo progressInfo = null)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileActionResult(PdfActionEnum.MultiplyPages, this);

            try
            {
                // error-/exit-handling
                if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
                if (factor == 1)
                {
                    ActionResultList.Add(result);
                    return result;
                }

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(FilePath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // set output-path
                string outputPath = FilePath.Substring(0, FilePath.Length - DotPDF.Length) + $", x {factor}" + DotPDF;

                // writer
                using (var writer = new FullCompressionPdfWriter(outputPath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount * factor, $"Seiten werden multipliziert ...");
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount * factor);
                                progressInfo.SetStatus($"Seiten werden multipliziert ...");
                            }
                        }

                        // Faktor-Loop
                        for (int i = 1; i <= factor; i++)
                        {
                            // Loop durch Seiten
                            for (int iPage = 1; iPage <= pageCount; iPage++)
                            {
                                // Seite kopieren
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);
                                
                                // flush all 100 pages to reduce memory-usage
                                if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // perform step
                                progressInfo?.PerformStep();
                            }
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        targetPDF.Close();
                        writer.Close();
                    }
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                // nicht nötig by MultiplyPages, da Original nicht verändert wird (15.06.2023, SME)
                //RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                // nicht nötig by MultiplyPages, da Original nicht verändert wird (15.06.2023, SME)
                //RefreshInfos();

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Split

        // PDF splitten (14.06.2023, SME)
        public PdfFileActionResult Split(int splitPageCount, ProgressInfo progressInfo = null)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileActionResult(PdfActionEnum.Split, this);

            // declarations
            FullCompressionPdfWriter writer = null;
            iPDF.PdfDocument targetPDF = null;

            try
            {
                // error-handling
                if (splitPageCount <= 0) throw new ArgumentOutOfRangeException(nameof(splitPageCount));

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(FilePath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // store page-count
                var pageCount = sourcePDF.GetNumberOfPages();

                // exit if split-page-count smaller then page-count of pdf
                if (pageCount <= splitPageCount)
                {
                    ActionResultList.Add(result);
                    return result;
                }

                // store number of digits
                var digits = pageCount.ToString().Length;
                var format = CoreFC.XString("0", digits);

                // set file-path-prefix
                string filePathPrefix = FilePath.Substring(0, FilePath.Length - DotPDF.Length);

                // initialize next-split-counter
                int nextSplit = splitPageCount;

                // loop throu pages
                for (int iPage = 1; iPage <= pageCount; iPage++)
                {
                    try
                    {
                        // create new writer + document if necessary
                        if (writer == null)
                        {
                            string outputFilePath = filePathPrefix + $", {iPage.ToString(format)} - ";
                            if (nextSplit < pageCount)
                            {
                                outputFilePath += nextSplit.ToString(format);
                            }
                            else
                            {
                                outputFilePath += (pageCount).ToString(format);
                            }
                            outputFilePath += DotPDF;
                            writer = new FullCompressionPdfWriter(outputFilePath);
                            targetPDF = new PdfDocument(writer);
                        }

                        // copy page
                        sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                        // flush all 100 pages to reduce memory-usage
                        if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                        // check for next split
                        if (iPage == nextSplit)
                        {
                            targetPDF.FlushCopiedObjects(sourcePDF);
                            targetPDF.Close();
                            writer.Close();
                            targetPDF = null;
                            writer = null;
                            nextSplit += splitPageCount;
                        }

                        // perform step
                        progressInfo?.PerformStep();
                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }

                // closing
                targetPDF?.FlushCopiedObjects(sourcePDF);
                targetPDF?.Close();
                writer?.Close();
                targetPDF = null;
                writer = null;
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                // nicht nötig by SplitPDF, da Original nicht verändert wird (15.06.2023, SME)
                //RestoreOriginal(inputPath);
            }
            finally
            {
                // closing
                if (targetPDF != null) targetPDF.Close();
                if (writer != null) writer.Close();

                // refresh infos
                // nicht nötig by MultiplyPages, da Original nicht verändert wird (15.06.2023, SME)
                //RefreshInfos();

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Images

        #region Compress Images

        // Compress Images (16.06.2023, SME)
        public PdfFileImageCompressionnResult CompressImages(ProgressInfo progressInfo = null, int maxWidthHeight = 1024, bool keepBackup = true)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileImageCompressionnResult(this, maxWidthHeight);

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"Bilder werden optimiert ...");
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                progressInfo.SetStatus($"Bilder werden komprimiert ...");
                            }
                        }

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // copy page
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // store new page
                                var page = targetPDF.GetPage(iPage);

                                // store resources
                                var resources = page.GetResources();

                                // optimize images
                                CompressImages(resources.GetPdfObject(), result);
                                
                                // flush all 100 pages to reduce memory-usage
                                //if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // perform step
                                progressInfo?.PerformStep();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // flush
                        //targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        targetPDF.Close();
                        writer.Close();
                    }
                }

                // delete backup if not wanted
                if (!keepBackup)
                {
                    CloseReaderAndDocument();
                    File.Delete(inputPath);
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos(progressInfo);

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        // Compress Images  (16.06.2023, SME)
        private void CompressImages(PdfDictionary resources, PdfFileImageCompressionnResult result)
        {
            try
            {
                // exit-handling
                if (resources == null) return;
                if (result == null) return;

                // loop throu dictionary-entries
                foreach (var key in resources.KeySet().ToArray())
                {
                    try
                    {
                        // store + check value
                        var value = resources.Get(key);
                        if (value == null) continue;

                        // check if image
                        if (!FC_PDF.IsImage(value))
                        {
                            // no image => recursive call
                            if (value is PdfDictionary)
                            {
                                CompressImages(value as PdfDictionary, result);
                            }
                        }
                        else
                        {
                            try
                            {
                                // convert to stream
                                var pdfStream = (PdfStream)value;

                                // skip-handling
                                if (!pdfStream.IsIndirect()) continue;

                                string colorspace = pdfStream.ContainsKey(PdfName.ColorSpace) ? pdfStream.Get(PdfName.ColorSpace).ToString() : "";

                                // store name
                                PdfName name = pdfStream.ContainsKey(PdfName.Name) ? pdfStream.GetAsName(PdfName.Name): null;

                                // store bytes
                                var bytes = pdfStream.GetBytes(true);

                                // skip if lower then 100kb
                                if (bytes.Length < 100 * (int)FsoSizeTypeEnum.KB) continue;

                                var pdfImage = new PdfImageXObject(pdfStream);
                                byte[] pdfImageBytes;
                                try
                                {
                                    pdfImageBytes = pdfImage.GetImageBytes(true);
                                }
                                catch (Exception ex)
                                {
                                    CoreFC.DPrint("ERROR: " + ex.Message);
                                    pdfImageBytes = null;
                                    continue;
                                }

                                if (pdfImageBytes.Length != bytes.Length)
                                {
                                    Console.WriteLine($"WARNING: Length of Image-Bytes don't match! Bytes = {bytes.Length}, Image-Bytes = {pdfImageBytes.Length}");
                                    // Debugger.Break();
                                }

                                // skip images that are smaller then 500 KB
                                // if (bytes.Length < 500 * 1024) continue;

                                
                                if (!pdfStream.ContainsKey(PdfName.Filter)) continue;
                                //if (!PdfName.DCTDecode.Equals(pdfStream.GetAsName(PdfName.Filter))) continue;

                                // get image
                                var image = CoreFC.GetImage(pdfImageBytes);
                                bool isCMYK = CoreFC.IsCMYK(image);
                                var imageBytes = CoreFC.GetByteArray(image);
                                var imageBytesOptimized = CoreFC.GetOptimizedImageAsByteArray(image, result.MaxWidthHeight);
                                var imageOptimized = CoreFC.GetImage(imageBytesOptimized);
                                bool isCMYKafter = CoreFC.IsCMYK(imageOptimized);
                                if (isCMYK != isCMYKafter)
                                {
                                    Debugger.Break();
                                }

                                // check + add saved byte-length
                                var savedByteLength = pdfImageBytes.Length - imageBytesOptimized.Length;
                                if (savedByteLength <= 0) continue;
                                result.SavedImageSize += savedByteLength;

                                // create new image
                                var newImageData = ImageDataFactory.Create(imageBytesOptimized);
                                var newImage = new PdfImageXObject(newImageData);
                                var newImageObject = newImage.GetPdfObject();

                                // apply name
                                if (name != null)
                                {
                                    if (newImageObject.ContainsKey(PdfName.Name))
                                    {
                                        newImageObject.Remove(PdfName.Name);
                                    }
                                    newImageObject.Put(PdfName.Name, name);
                                }

                                // switch image
                                resources.Remove(key);
                                resources.Put(key, newImageObject);

                                // update counter
                                result.CountCompressedImages++;
                            }
                            catch (Exception ex)
                            {
                                result.AddError(ex);
                                CoreFC.DPrint("ERROR while extracting image: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddError(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }
        }

        #endregion

        #region Extract Images

        // Extract Images (16.06.2023, SME)
        // IMPORTANT => DON'T USE event-listener!!! way to slow + blocks the whole system + writes down lots of duplicated images (16.06.2023, SME)
        public PdfFileImageExtractionResult ExtractImages(ProgressInfo progressInfo = null)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileImageExtractionResult(this);

            try
            {
                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(FilePath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // store number of objects
                // var totalSteps = useEventListener ? sourcePDF.GetNumberOfPages() : sourcePDF.GetNumberOfPdfObjects();
                var totalSteps = sourcePDF.GetNumberOfPdfObjects();

                // start progress
                if (progressInfo != null)
                {
                    if (!progressInfo.IsRunning)
                    {
                        progressInfo.Start(totalSteps, $"Bilder werden extrahiert ...");
                    }
                    else if (progressInfo.TotalSteps == 0)
                    {
                        progressInfo.SetTotalSteps(totalSteps);
                        progressInfo.SetStatus($"Bilder werden extrahiert ...");
                    }
                }

                // set target-folder-path
                string targetFolderPath = FilePath + " - Images";

                // try to delete if target-folder-path exists
                if (Directory.Exists(targetFolderPath))
                {
                    try
                    {
                        Directory.Delete(targetFolderPath, true);
                    }
                    catch (Exception ex)
                    {
                        CoreFC.DPrint($"ERROR while deleting images-folder '{targetFolderPath}': {ex.Message}");

                        // delete all files possible
                        foreach (var file in Directory.GetFiles(targetFolderPath))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception exFile)
                            {
                                CoreFC.DPrint($"ERROR while deleting images-file '{file}': {exFile.Message}");
                            }
                        }
                    }
                }

                #region With Event-Listener (REMARKED)

                // => DON'T USE event-listener!!! way to slow + blocks the whole system + writes down lots of duplicated images (16.06.2023, SME)
                //if (useEventListener)
                //{
                //    // with event-listener + looping throu pages
                //    var listener = new ImageExtractor(targetFolderPath, result);
                //    var parser = new PdfCanvasProcessor(listener);
                //    for (int iPage = 1; iPage <= totalSteps; iPage++)
                //    {
                //        parser.ProcessPageContent(sourcePDF.GetPage(iPage));
                //        progressInfo?.PerformStep();
                //    }
                //}

                #endregion

                // without event-listener + looping throu objects
                string name = string.Empty;

                // loop throu objects
                for (int iObject = 1; iObject <= totalSteps; iObject++)
                {
                    try
                    {
                        // reset name
                        name = string.Empty;

                        // store object
                        var pdfObject = sourcePDF.GetPdfObject(iObject);

                        // skip-handling
                        if (pdfObject == null) continue;

                        // handle image
                        if (FC_PDF.IsImage(pdfObject))
                        {
                            this.ExtractImage(pdfObject, targetFolderPath, result, string.Empty);
                            continue;
                        }

                        // handle form
                        if (FC_PDF.IsForm(pdfObject))
                        {
                            this.ExtractImagesFromForm(pdfObject, targetFolderPath, result);
                            continue;
                        }

                        //// store resources
                        //var resources = pdfObject.GetResources();

                        //// extract images from resources
                        //this.ExtractImagesFromResources(resources.GetPdfObject(), targetFolderPath, result);
                    }
                    catch (Exception ex)
                    {
                        result.AddError(ex);
                    }
                    finally
                    {
                        // perform step
                        progressInfo?.PerformStep();
                    }
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);
            }
            finally
            {
                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        // Extract Image from PDF-Form (06.07.2023, SME)
        private void ExtractImagesFromForm(PdfObject pdfForm, string targetFolderPath, PdfFileImageExtractionResult result)
        {
            try
            {
                // exit-handling
                if (pdfForm == null) return;
                if (result == null) return;
                if (!FC_PDF.IsForm(pdfForm)) return;

                // handle resources
                var form = pdfForm as PdfStream;
                if (form != null && form.ContainsKey(PdfName.Resources))
                {
                    var resources = form.GetAsDictionary(PdfName.Resources);
                    ExtractImagesFromResources(resources, targetFolderPath, result);
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }
        }

        // Extract Images from PDF-Resources-Dictionary (06.07.2023, SME)
        private void ExtractImagesFromResources(PdfDictionary resources, string targetFolderPath, PdfFileImageExtractionResult result)
        {
            try
            {
                // exit-handling
                if (resources == null) return;
                if (result == null) return;
                if (!resources.ContainsKey(PdfName.XObject)) return;

                // store + handle x-object
                var xObject = resources.Get(PdfName.XObject);
                if (FC_PDF.IsImage(xObject))
                {
                    ExtractImage(xObject, targetFolderPath, result, string.Empty);
                }
                else if (FC_PDF.IsForm(xObject))
                {
                    ExtractImagesFromForm(xObject, targetFolderPath, result);
                }
                else if (xObject is PdfDictionary dic)
                {
                    foreach (var key in dic.KeySet().ToArray())
                    {
                        var item = dic.Get(key);
                        if (FC_PDF.IsImage(item))
                        {
                            ExtractImage(item, targetFolderPath, result, key.ToString().Substring(1));
                        }
                        else if (FC_PDF.IsForm(item))
                        {
                            ExtractImagesFromForm(item, targetFolderPath, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }
        }

        // Extract single Image (06.07.2023, SME)
        private void ExtractImage(PdfObject pdfImageObject, string targetFolderPath, PdfFileImageExtractionResult result, string name)
        {
            // declarations
            string fileType = ".jpg";

            try
            {
                // exit-handling
                if (pdfImageObject == null) return;
                if (!FC_PDF.IsImage(pdfImageObject)) return;
                if (result == null) return;

                // convert to stream
                var pdfStream = (PdfStream)pdfImageObject;

                // store bytes
                var bytes = pdfStream.GetBytes(true);

                // store mask
                PdfImageXObject imageMask = null;
                if (pdfStream.ContainsKey(PdfName.SMask))
                {
                    var sMask = pdfStream.Get(PdfName.SMask);
                    if (FC_PDF.IsImage(sMask))
                    {
                        imageMask = new PdfImageXObject(sMask as PdfStream);
                    }
                }

                // get image-bytes
                PdfImageXObject pdfImage;
                if (imageMask == null)
                {
                    pdfImage = new PdfImageXObject(pdfStream);
                }
                else
                {
                    try
                    {
                        pdfImage = new PdfImageXObject(pdfStream);

                        byte[] imageDataBytes;
                        try
                        {
                            imageDataBytes = pdfImage.GetImageBytes(true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            try
                            {
                                imageDataBytes = pdfImage.GetImageBytes(false);
                            }
                            catch (Exception ex2)
                            {
                                throw ex2;
                            }
                        }
                        ImageData imageData;
                        try
                        {
                            imageData = ImageDataFactory.Create(imageDataBytes);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        pdfImage = new PdfImageXObject(imageData, imageMask);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        throw ex;
                    }
                }
                byte[] pdfImageBytes;
                try
                {
                    fileType = pdfImage.IdentifyImageFileExtension();
                    if (!fileType.StartsWith(".")) fileType = "." + fileType;
                    pdfImageBytes = pdfImage.GetImageBytes(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR while getting image-bytes: {ex.Message}");
                    throw ex;
                }

                // get image
                var image = CoreFC.GetImage(bytes);

                // make sure name is set
                if (string.IsNullOrEmpty(name)) name = Guid.NewGuid().ToString();
                else if (name.StartsWith("/")) name = name.Substring(1);

                // save image
                var path = Path.Combine(targetFolderPath, name);
                if (File.Exists(path)) Debugger.Break();
                CoreFC.SaveImage(image, path, false);

                // update counter
                result.CountExtractedImages++;
            }
            catch (Exception ex)
            {
                // set new error
                if (!string.IsNullOrEmpty(name))
                {
                    ex = new Exception($"Beim Extrahieren von Bild '{name} - {pdfImageObject}' ist folgender Fehler aufgetreten:" + CoreFC.Lines() + ex.Message);
                }
                else if (pdfImageObject != null)
                {
                    ex = new Exception($"Beim Extrahieren von Bild '{pdfImageObject}' ist folgender Fehler aufgetreten:" + CoreFC.Lines() + ex.Message);
                }
                else
                {
                    ex = new Exception($"Beim Extrahieren eines Bildes (Objekt # {pdfImageObject}) ist folgender Fehler aufgetreten:" + CoreFC.Lines() + ex.Message);
                }

                // add error to result
                result.AddError(ex);
            }
        }

        #endregion

        #endregion

        #region Rückseiten-Prüfung /-Entfernung

        // Ermittelt, welche Rückseiten NICHT leer sind (13.07.2023, SME)
        public PdfFileBackpageCheckResult GetBackpagesWithContent(ProgressInfo progressInfo = null)
        {
            // init result
            var result = new PdfFileBackpageCheckResult(this);

            // exit-handling
            if (result.HasErrors) return result;

            try
            {
                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(FilePath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // loop throu pages
                for (int iPage = 2; iPage <= sourcePDF.GetNumberOfPages(); iPage += 2)
                {
                    // store page
                    var page = sourcePDF.GetPage(iPage);

                    // check if empty
                    if (!FC_PDF.IsEmptyPage(page))
                    {
                        // page is not empty => add to listl
                        result.AddBackpageWithContent(iPage);
                    }

                    // perform step
                    progressInfo?.PerformStep();
                }
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);
            }
            finally
            {
                // close reader + document
                CloseReaderAndDocument();
            }

            // return
            ActionResultList.Add(result);
            return result;
        }

        // Entfernt alle Rückseiten, aber nur wenn alle leer sind (12.10.2023, SME)
        public PdfFileActionResult RemoveBackpagesWhenAllEmpty(ProgressInfo progressInfo = null)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileActionResult(PdfActionEnum.RemoveBackpagesWhenAllEmpty, this);

            // exit-handling
            if (result.HasErrors) return result;

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // 1st check if all backpages are empty
                var checkResult = GetBackpagesWithContent(progressInfo);
                if (checkResult.HasErrors)
                {
                    checkResult.Errors.ToList().ForEach(x => result.AddError(x));
                    ActionResultList.Add(result);
                    return result;
                }
                else if (checkResult.BackpagesWithContent.Any())
                {
                    throw new Exception($"Folgende Rückseiten sind nicht leer: {string.Join(" + ", checkResult.BackpagesWithContent)}");
                }

                // Original-Datei backupen
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"Rückseiten werden entfernt ...");
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                progressInfo.SetStatus($"Rückseiten werden entfernt ...");
                            }
                        }

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage +=2)
                        {
                            try
                            {
                                // Seite ermitteln
                                var page = sourcePDF.GetPage(iPage);

                                // Seite kopieren
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // flush all 100 pages to reduce memory-usage
                                if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // perform step
                                progressInfo?.PerformStep();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        targetPDF.Close();
                        writer.Close();
                    }
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos();

                // close reader + document
                CloseReaderAndDocument();
            }

            // return
            ActionResultList.Add(result);
            return result;
        }


        #endregion

        #region Rotation

        // Seiten drehen (16.10.2023, SME)
        public PdfFileRotationResult RotatePages(RotationParameters parameters, ProgressInfo progressInfo = null)
        {
            // set result / return-value (error will be raised if invalid pdf-file)
            var result = new PdfFileRotationResult(this, parameters);

            // init input-path
            string inputPath = FilePath;

            try
            {
                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        if (progressInfo != null)
                        {
                            // start progress
                            if (!progressInfo.IsRunning)
                            {
                                progressInfo.Start(pageCount, $"Seiten werden gedreht ...");
                            }
                            else if (progressInfo.TotalSteps == 0)
                            {
                                progressInfo.SetTotalSteps(pageCount);
                                progressInfo.SetStatus($"Seiten werden gedreht ...");
                            }
                        }

                        // define rotate-method
                        Action<PdfPage> rotate = (page) => 
                        {
                            var rotation = page.GetRotation();
                            var size = page.GetPageSizeWithRotation();

                            switch (parameters.Direction)
                            {
                                case PdfRotationDirectionEnum.Portrait_ToLeft:
                                    if (size.GetHeight() < size.GetWidth()) 
                                    {
                                        rotation -= 90;
                                        page.SetRotation(rotation);
                                    }
                                    break;
                                case PdfRotationDirectionEnum.Portrait_ToRight:
                                    if (size.GetHeight() < size.GetWidth())
                                    {
                                        rotation += 90;
                                        page.SetRotation(rotation);
                                    }
                                    break;
                                case PdfRotationDirectionEnum.Landscape_ToLeft:
                                    if (size.GetHeight() > size.GetWidth())
                                    {
                                        rotation -= 90;
                                        page.SetRotation(rotation);
                                    }
                                    break;
                                case PdfRotationDirectionEnum.Landscape_ToRight:
                                    if (size.GetHeight() > size.GetWidth())
                                    {
                                        rotation += 90;
                                        page.SetRotation(rotation);
                                    }
                                    break;
                                default:
                                    rotation += (int)parameters.Direction;
                                    page.SetRotation(rotation);
                                    break;
                            }
                        };

                        // loop throu pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // copy page
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // Seite ermitteln
                                var page = targetPDF.GetPage(iPage);

                                // Seite drehen falls nötig
                                switch (parameters.PagesOption)
                                {
                                    case PdfRotationPagesOptionEnum.AllPages:
                                        rotate(page);
                                        break;
                                    case PdfRotationPagesOptionEnum.EvenPages:
                                        if (iPage % 2 == 0) rotate(page);
                                        break;
                                    case PdfRotationPagesOptionEnum.OddPages:
                                        if (iPage % 2 != 0) rotate(page);
                                        break;
                                    case PdfRotationPagesOptionEnum.SpecificPages:
                                        if (parameters.SpecificPages.Contains(iPage)) rotate(page);
                                        break;
                                    default:
                                        break;
                                }

                                // flush all 100 pages to reduce memory-usage
                                if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                // perform step
                                progressInfo?.PerformStep();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }                        

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        targetPDF.Close();
                        writer.Close();
                    }
                }

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                RefreshInfos();

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Check Print-PDF

        // Print-PDF prüfen (02.04.2024, SME)
        public PdfPrintFileCheckResult CheckPrintPdf(ProgressInfo progressInfo = null)
        {
            #region INFO

            /*
             * Überprüft wird die SDL.
             * 
             * Aufbau von SDL: {JobID}-{Mailpiece}-{Sendung}/{TotalSendungen}-{Blatt}/{TotalBlatt}-{BlattNummer}[-{Beilagen}][ RS]
             * 
             * Überprüft wird Folgendes:
             * - Ist die SDL (falls vorhanden) korrekt aufgebaut? (weitere Infos => siehe SDL-Klasse: INFO
             * - Stimmt die Job-ID aus der SDL mit der Job-ID aus dem Dateinamen überein?
             * - Ist die Mailpiece eindeutig?
             * - Sendung aufsteigend
             * - Blatt aufsteigend innerhalb von Sendung
             * - Blattnummer aufsteigend
             */

            #endregion

            // init result
            var result = new PdfPrintFileCheckResult(this);

            // Deklarationen
            PrintPdfPageTypeEnum pageType = PrintPdfPageTypeEnum.Unbekannt;
            bool istLeer;
            List<string> sdlStringList = new();
            SDL sdl;
            List<Exception> pageErrors = new();
            bool istDuplex = false;

            // Methode um Page-Error der Liste hinzuzufügen (damit ein Haltepunkt gesetzt werden kann)
            var addPageError = new Action<Exception>((error) =>
            {
                if (error != null)
                {
                    pageErrors.Add(error);
                }
            });

            try
            {
                #region Start

                // reader
                if (PdfReader == null) PdfReader = new iPDF.PdfReader(this.FilePath);
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null) PdfDocument = new iPDF.PdfDocument(reader);
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                #endregion

                #region Job-ID + SX/DX ermitteln von Dateiname

                // Job-ID ermitteln
                int? jobID = CoreFC.GetJobIDfromFileName(this.FileName);
                result.JobID = jobID;
                if (!jobID.HasValue)
                {
                    result.AddError(new PrintPdfError(PrintPdfErrorEnum.JobIdNotFound, this, "Job-ID konnte nicht ermittelt werden von Dateinamen!"));
                }

                // SX/DX ermitteln
                SxDxEnum? sx_dx = CoreFC.GetSxDx(this.FileName);
                result.SX_DX = sx_dx;
                if (!sx_dx.HasValue)
                {
                    result.AddError(new PrintPdfError(PrintPdfErrorEnum.SxDxNotFound, this, "SX/DX konnte nicht ermittelt werden von Dateinamen!"));
                }
                else if (sx_dx.Value == SxDxEnum.DX)
                {
                    istDuplex = true;
                }

                #endregion

                // Seiten-Infos ermitteln + prüfen NUR wenn Job-ID und Plex-Info vorhanden (19.04.2024, SME)
                if (!jobID.HasValue && !sx_dx.HasValue)
                {
                    // FEHLER: Seiten-Infos können nicht geprüft werden, weil weder Job-ID noch Plex-Info vorhanden
                    result.AddError(new PrintPdfError(PrintPdfErrorEnum.MissingInfos, this, "Seiten-Infos können nicht geprüft werden, weil weder Job-ID noch Plex-Info vorhanden!"));
                }
                else if (!jobID.HasValue)
                {
                    // FEHLER: Seiten-Infos können nicht geprüft werden, weil Job-ID nicht vorhanden
                    result.AddError(new PrintPdfError(PrintPdfErrorEnum.MissingInfos, this, "Seiten-Infos können nicht geprüft werden, weil Job-ID nicht vorhanden!"));
                }
                else if (!sx_dx.HasValue)
                {
                    // FEHLER: Seiten-Infos können nicht geprüft werden, weil Plex-Info nicht vorhanden
                    result.AddError(new PrintPdfError(PrintPdfErrorEnum.MissingInfos, this, "Seiten-Infos können nicht geprüft werden, weil Plex-Info nicht vorhanden!"));
                }
                else
                {
                    #region Seiten abhandeln um Seiten-Infos zu ermitteln

                    // Loop durch Seiten
                    for (int iPage = 1; iPage <= PageCount; iPage++)
                    {
                        try
                        {
                            // Variablen zurücksetzen
                            istLeer = false;
                            sdlStringList.Clear();
                            sdl = null;
                            pageErrors.Clear();

                            // Seite zwischenspeichern
                            var page = sourcePDF.GetPage(iPage);

                            // Text von Seite extrahieren
                            var text = FC_PDF.ExtractTextFromPage(page);

                            // Zeilen von Text ermitteln
                            var lines = CoreFC.GetLines(text, null, true).ToList();

                            // Zeilen abhandeln
                            if (!lines.Any())
                            {
                                istLeer = true;
                            }
                            else
                            {
                                if (lines.Contains(this.FileNamePure))
                                {
                                    // muss Vor- oder Nachlauf-Seite sein
                                    // => SOL-Info prüfen
                                    if (!lines.Any(x => x.EndsWith(DotSOL)))
                                    {
                                        // KEINE SOL-Info
                                        addPageError(new PrintPdfPageError(PrintPdfErrorEnum.NoSolInfo, this, iPage, "Keine SOL-Info in Vor-/Nachlauf-Seite!"));
                                    }
                                    else
                                    {
                                        // SOL-Info validieren
                                        // => SOL-Info-Zeilen zwischenspeichern
                                        var solInfoLines = lines.Where(x => x.EndsWith(DotSOL)).ToArray();
                                        if (solInfoLines.Length > 1)
                                        {
                                            // WARNUNG: Mehrere SOL-Info-Zeilen
                                            addPageError(new PrintPdfPageError(PrintPdfErrorEnum.TooManySolInfos, this, iPage, $"Mehrere SOL-Info-Zeilen ({solInfoLines.Length}x) in Vor-/Nachlauf-Seite!"));
                                        }
                                        else
                                        {
                                            // 1 SOL-Info-Zeile wie erwartet
                                            // => Dateiname ohne Endung zwischenspeichern
                                            var solInfoLine = solInfoLines.First();
                                            var solFileNamePure = solInfoLine.Substring(0, solInfoLine.Length - DotSOL.Length);
                                            // => Dateiname ohne Endung muss der Job-ID entsprechen, also nummerisch sein
                                            if (!CoreFC.IsNumeric(solFileNamePure))
                                            {
                                                // WARNUNG: SOL-Dateiname ist nicht nummerisch
                                                addPageError(new PrintPdfPageError(PrintPdfErrorEnum.InvalidSolFileName, this, iPage, $"SOL-Dateiname ist nicht nummerisch: {solFileNamePure}"));
                                            }
                                            else
                                            {
                                                // SOL-Dateiname muss auch im PDF-Dateinamen vorkommen
                                                if (!this.FileNamePure.Contains("_" + solFileNamePure + "_"))
                                                {
                                                    // WARNUNG: SOL-Dateiname kommt nicht im PDF-Dateinamen vor
                                                    addPageError(new PrintPdfPageError(PrintPdfErrorEnum.InvalidSolFileName, this, iPage, $"SOL-Dateiname kommt nicht im PDF-Dateinamen vor: {solFileNamePure}"));
                                                }
                                                else
                                                {
                                                    // SOL-Info-Zeile kann entfernt werden
                                                    lines.Remove(solInfoLine);
                                                    // PDF-Dateiname kann ebenfalls entfernt werden
                                                    lines.RemoveAll(x => x == this.FileNamePure);
                                                    // => Jetzt sollte nur noch Vorlauf oder Nachlauf übrig sein
                                                    if (lines.Any(x => x.ToUpper().Equals(VORLAUF)))
                                                    {
                                                        pageType = PrintPdfPageTypeEnum.Vorlauf;
                                                    }
                                                    else if (lines.Any(x => x.ToUpper().Equals(NACHLAUF)))
                                                    {
                                                        pageType = PrintPdfPageTypeEnum.Nachlauf;
                                                    }
                                                    else
                                                    {
                                                        // WARNUNG: Weder Vor- noch Nachlauf => was ist es??
                                                        pageType = PrintPdfPageTypeEnum.Unbekannt;
                                                        addPageError(new PrintPdfPageError(PrintPdfErrorEnum.UnknownPageType, this, iPage, "Seitentyp wurde nicht erkannt! (Weder Vor- noch Nachlauf)"));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // muss Füllseite sein oder Trennblbatt oder Sendungs-Seite
                                    // => Mögliche SDL-Werte zwischenspeichern + prüfen
                                    string[] possibleSDLValues = [];
                                    if (jobID.HasValue)
                                    {
                                        possibleSDLValues = lines.Where(x => x.StartsWith(jobID.Value.ToString() + "-")).ToArray();
                                        if (!possibleSDLValues.Any())
                                        {
                                            possibleSDLValues = lines.Where(x => x.StartsWith("SDL: " + jobID.Value.ToString() + "-")).ToArray();
                                        }
                                    }
                                    if (!possibleSDLValues.Any())
                                    {
                                        // muss Füllseite oder Trennblatt sein
                                        if (lines.Any(x => x.ToUpper().Equals(TC.Constants.SolConst.FUELL)))
                                        {
                                            // ist Füllseite
                                            pageType = PrintPdfPageTypeEnum.Fuellseite;
                                        }
                                        else if (lines.Any(x => x.ToUpper().Equals(FUELLSEITE)))
                                        {
                                            // ist Trennblatt
                                            pageType = PrintPdfPageTypeEnum.Fuellseite;
                                        }
                                        else if (lines.Any(x => x.ToUpper().Equals(TRENNBLATT)))
                                        {
                                            // ist Trennblatt
                                            pageType = PrintPdfPageTypeEnum.Trennblatt;
                                        }
                                        else if (text == FUELLSEITE_TEXT)
                                        {
                                            // ist Füllseite
                                            pageType = PrintPdfPageTypeEnum.Fuellseite;
                                        }
                                        else if (text == TRENNBLATT_TEXT)
                                        {
                                            // ist Trennblatt
                                            pageType = PrintPdfPageTypeEnum.Trennblatt;
                                        }
                                        else if (istDuplex && iPage % 2 == 0)
                                        {
                                            // Rückseite
                                            if (pageType == PrintPdfPageTypeEnum.Sendung)
                                            {
                                                // OKAY => Rückseite von Sendung hat keine SDL
                                            }
                                            else
                                            {
                                                pageType = PrintPdfPageTypeEnum.Unbekannt;
                                                addPageError(new PrintPdfPageError(PrintPdfErrorEnum.UnknownPageType, this, iPage, "Seitentyp wurde nicht erkannt! (Weder Füllseite noch Trennblatt noch Sendungs-Seite)"));
                                            }
                                        }
                                        else
                                        {
                                            // Weder Füllseite noch Trennblatt noch Sendungs-Seite (weil keine SDL)
                                            // => was ist es??
                                            pageType = PrintPdfPageTypeEnum.Unbekannt; 
                                            addPageError(new PrintPdfPageError(PrintPdfErrorEnum.UnknownPageType, this, iPage, "Seitentyp wurde nicht erkannt! (Weder Füllseite noch Trennblatt noch Sendungs-Seite (weil keine SDL))"));
                                        }
                                    }
                                    else
                                    {
                                        // SDL prüfen
                                        // - Beispiel einer SDL: "796300102-0000047697-3/26-3/3-00009"
                                        // - Aufbau: {9-stellige JobID}-{Mailpiece}-{Sendung/Sendungen}-{Blatt/Blätter}-{Beilagen-Info}[ RS]
                                        // => somit muss eine SDL gesplittet bei "-" genau 5 Teile haben
                                        foreach (var possibleSDLValue in possibleSDLValues)
                                        {
                                            try
                                            {
                                                // SDL zwischenspeichern
                                                var possibleSDL = possibleSDLValue;

                                                // Präfix entfernen falls nötig
                                                if (possibleSDL.StartsWith("SDL: "))
                                                {
                                                    possibleSDL = possibleSDL.Substring("SDL: ".Length).TrimEnd();
                                                }

                                                // Prüfen ob gültige SDL
                                                // => SDL-Fehler zwischenspeichern
                                                var sdlErrors = SDL.GetErrors(possibleSDL);
                                                if (sdlErrors.Any())
                                                {
                                                    // SDL-Fehler 
                                                    foreach (var sdlError in sdlErrors)
                                                    {
                                                        addPageError(sdlError);
                                                    }
                                                }
                                                else 
                                                { 
                                                    if (!sdlStringList.Any())
                                                    {
                                                        sdlStringList.Add(possibleSDL);
                                                    }
                                                    else if (sdlStringList.Contains(possibleSDL))
                                                    {
                                                        Console.WriteLine($"Die SDL ist bereits in der SDL-Liste: {possibleSDL}");
                                                    }
                                                    else
                                                    {
                                                        // WARNUNG: Weitere gültige SDL!!!
                                                        // => zur Liste hinzufügen und am Schluss abhandeln
                                                        sdlStringList.Add(possibleSDL);
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.Message);
                                            }
                                        }

                                        // SDL-Anzahl prüfen
                                        switch (sdlStringList.Count)
                                        {
                                            case 0:
                                                // ACHTUNG: Keine gültige SDL gefunden
                                                // => was ist es für eine Seite??
                                                pageType = PrintPdfPageTypeEnum.Unbekannt;
                                                addPageError(new PrintPdfPageError(PrintPdfErrorEnum.UnknownPageType, this, iPage, "Seitentyp wurde nicht erkannt! (Keine gültige SDL gefunden!)"));
                                                break;

                                            case 1:
                                                pageType = PrintPdfPageTypeEnum.Sendung;
                                                break;

                                            default:
                                                // ACHTUNG: Mehrere gültige SDLs gefunden
                                                // => was ist es für eine Seite??
                                                pageType = PrintPdfPageTypeEnum.Unbekannt;
                                                addPageError(new PrintPdfPageError(PrintPdfErrorEnum.UnknownPageType, this, iPage, $"Seitentyp wurde nicht erkannt! ({sdlStringList.Count} gültige SDLs gefunden!)"));
                                                break;
                                        }
                                    }
                                }
                            }

                            // Seiten-Info hinzufügen
                            bool istRS = iPage % 2 == 0;
                            if (pageType != PrintPdfPageTypeEnum.Sendung)
                            {
                                result.AddPage(new PrintPdfPageInfo(this, iPage, pageType, istRS, istLeer, pageErrors.ToArray()));
                            }
                            else
                            {
                                try
                                {
                                    if (sdlStringList.Count == 1) sdl = SDL.FromString(sdlStringList.First());
                                    result.AddPage(new PrintPdfShipmentPageInfo(this, iPage, sdl, istRS, istLeer, pageErrors.ToArray()));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            // error
                            CoreFC.ThrowError(ex); throw ex;
                        }
                        finally
                        {
                            // perform step
                            progressInfo?.PerformStep();
                        }
                    }

                    #endregion

                    #region Sendungs-Seiten bzw. deren SDL prüfen

                    // Sendungs-Seiten zwischenspeichern
                    var shipmentPages = result.Pages.OfType<PrintPdfShipmentPageInfo>().ToList();

                    // Prüfen ob Sendungs-Seiten vorhanden
                    if (shipmentPages.Any())
                    {
                        /*
                         * Folgende Punkte prüfen:
                         * - Stimmt die Job-ID aus der SDL mit der Job-ID aus dem Dateinamen überein?
                         * - Sind die Sendungen fortlaufend?
                         * - Sind die Blätter innerhalb der Sendung fortlaufend und ohne Unterbruch?
                         * - Ist die Mailpiece eindeutig?
                         */

                        // Deklarationen
                        List<long> mailpieceList = new();
                        long mailpieceCurrent = 0;
                        int iSendung = 0;
                        int iBlatt = 0;
                        SDL sdlCurrent = null;
                        SDL sdlBefore = null;
                        bool istRSgemPlexInfo;
                        bool istRSgemSDL;

                        // Methode um Page-Error der Liste hinzuzufügen (damit ein Haltepunkt gesetzt werden kann)
                        var addShipmentPageError = new Action<PrintPdfShipmentPageInfo, Exception>((page, error) =>
                        {
                            if (page != null && error != null)
                            {
                                page.AddError(error);
                            }
                        });

                        // Loop durch Sendungs-Seiten
                        foreach (var shipmentPage in shipmentPages)
                        {
                            try
                            {
                                // SDL zwischenspeichern
                                sdlCurrent = shipmentPage.SDL;

                                // RS-Flags setzen + prüfen
                                if (istDuplex && shipmentPage.PageIndex % 2 == 0)
                                {
                                    istRSgemPlexInfo = true;
                                }
                                else
                                {
                                    istRSgemPlexInfo = false;
                                }
                                if (sdlCurrent != null)
                                {
                                    istRSgemSDL = sdlCurrent.IstRS;
                                }
                                else
                                {
                                    istRSgemSDL = istRSgemPlexInfo;
                                }
                                if (istRSgemPlexInfo != istRSgemSDL)
                                {
                                    // FEHLER: RS-Infos stimmen nicht überein
                                    addShipmentPageError(shipmentPage, new Exception($"RS-Infos stimmen nicht überein! gem. Plex-Info = {istRSgemPlexInfo}, gem. SDL = {istRSgemSDL}"));
                                }

                                // Job-ID prüfen
                                if (sdlCurrent != null && sdlCurrent.JobID.ToString() != jobID.Value.ToString())
                                {
                                    // FEHLER: Job-ID aus SDL stimmt nicht überein mit Mailpiece aus PDF-Dateiname!
                                    addShipmentPageError(shipmentPage, new Exception("Job-ID aus SDL stimmt nicht überein mit Job-ID aus PDF-Dateiname!"));
                                }

                                // Sendungswechsel / Blattwechsel prüfen
                                if (iSendung == 0)
                                {
                                    // 1. Seite von 1. Sendung
                                    if (sdlCurrent == null)
                                    {
                                        // FEHLER: SDL erwartet, aber nicht gesetzt!
                                        addShipmentPageError(shipmentPage, new Exception($"SDL erwartet, aber nicht gesetzt!"));
                                    }
                                    else
                                    {
                                        // Sendungs-Index prüfen
                                        iSendung = sdlCurrent.Sendung;
                                        if (iSendung != 1)
                                        {
                                            // FEHLER: 1. Sendung erwartet, ?. Sendung geliefert!
                                            addShipmentPageError(shipmentPage, new Exception($"1. Sendung erwartet, {iSendung}. Sendung geliefert!"));
                                        }

                                        // Blatt-Index prüfen
                                        iBlatt = sdlCurrent.Blatt;
                                        if (iBlatt != 1)
                                        {
                                            // FEHLER: 1. Blatt erwartet, ?. Blatt geliefert!
                                            addShipmentPageError(shipmentPage, new Exception($"1. Blatt erwartet, {iBlatt}. Blatt geliefert!"));
                                        }

                                        // Eindeutigkeit von Mailpiece prüfen
                                        if (mailpieceList.Contains(sdlCurrent.Mailpiece))
                                        {
                                            // FEHLER: Mehrfach vorkommente Mailpiece
                                            addShipmentPageError(shipmentPage, new Exception($"Mailpiece '{sdlCurrent.MailpieceString}' scheint nicht eindeutig zu sein!"));
                                        }
                                        else
                                        {
                                            mailpieceList.Add(sdlCurrent.Mailpiece);
                                        }
                                        mailpieceCurrent = sdlCurrent.Mailpiece;
                                    }
                                }
                                else if (sdlCurrent == null)
                                {
                                    // ACHTUNG: SDL nicht gesetzt => okay bei Rückseiten
                                    if (!shipmentPage.IstRS)
                                    {
                                        addShipmentPageError(shipmentPage, new Exception("SDL nicht gesetzt auf Vordersseite!"));
                                    }
                                    else
                                    {
                                        sdlCurrent = sdlBefore;
                                    }
                                }
                                else if (iSendung != sdlCurrent.Sendung)
                                {
                                    // Sendungs-Wechsel

                                    // => Sendungs-Mischung prüfen (Sendung muss bei Duplex auf ungerader Seite beginnen!!!)
                                    if (istDuplex && shipmentPage.PageIndex % 2 == 0)
                                    {
                                        addShipmentPageError(shipmentPage, new Exception("ACHTUNG: Sendungs-Mischung!!! Neue Sendung beginnt auf Rückseite!"));
                                    }

                                    // => Prüfen ob letztes Blatt erreicht ist
                                    if (sdlBefore == null)
                                    {
                                        // FEHLER: Prüfen ob letztes Blatt erreicht ist kann nicht geprüft werden, weil Vor-SDL nicht gesetzt ist!
                                        addShipmentPageError(shipmentPage, new Exception("Prüfen ob letztes Blatt erreicht ist kann nicht geprüft werden, weil Vor-SDL nicht gesetzt ist!"));
                                    }
                                    else if (iBlatt != sdlBefore.BlattTotal)
                                    {
                                        // FEHLER: Blatt-Total wurde nicht erreicht in vorheriger Sendung!
                                        addShipmentPageError(shipmentPage, new Exception("Blatt-Total wurde nicht erreicht in vorheriger Sendung!"));
                                    }
                                    else
                                    {
                                        // OKAY
                                    }

                                    // => Prüfen ob Sendung fortlaufend ist
                                    if ((iSendung + 1) != sdlCurrent.Sendung)
                                    {
                                        // FEHLER: Nicht fortlaufender Sendungs-Index!
                                        addShipmentPageError(shipmentPage, new Exception($"Nicht fortlaufender Sendungs-Index! Erwartet = {(iSendung + 1)}, Geliefert = {sdlCurrent.Sendung}"));
                                    }

                                    // Neue Sendung setzen
                                    iSendung = sdlCurrent.Sendung;

                                    // Blatt setzen
                                    iBlatt = sdlCurrent.Blatt;

                                    // Blatt prüfen
                                    if (iBlatt != 1)
                                    {
                                        // FEHLER: 1. Blatt erwartet, ?. Blatt geliefert!
                                        addShipmentPageError(shipmentPage, new Exception($"1. Blatt erwartet, {iBlatt}. Blatt geliefert!"));
                                    }

                                    // => Eindeutigkeit von Mailpiece prüfen
                                    if (mailpieceList.Contains(sdlCurrent.Mailpiece))
                                    {
                                        // FEHLER: Mehrfach vorkommente Mailpiece
                                        addShipmentPageError(shipmentPage, new Exception($"Mailpiece '{sdlCurrent.MailpieceString}' scheint nicht eindeutig zu sein!"));
                                    }
                                    else
                                    {
                                        mailpieceList.Add(sdlCurrent.Mailpiece);
                                    }
                                    mailpieceCurrent = sdlCurrent.Mailpiece;
                                }
                                else
                                {
                                    // ev. Blattwechsel

                                    // => Mailpiece prüfen
                                    if (mailpieceCurrent != sdlCurrent.Mailpiece)
                                    {
                                        // ACHTUNG: Mailpiece stimmt nicht überein innerhalb der Sendung!
                                        addShipmentPageError(shipmentPage, new Exception($"Mailpiece stimmt nicht überein innerhalb der Sendung! Erwartet = '{mailpieceCurrent}', Geliefert = '{sdlCurrent.MailpieceString}'"));

                                        // Mailpiece aktualisieren
                                        mailpieceCurrent = sdlCurrent.Mailpiece;
                                    }

                                    // => Blatt-Index prüfen
                                    if (sdlCurrent.IstRS)
                                    {
                                        // Rückseite => Blatt-Index muss überstimmen
                                        if (iBlatt != sdlCurrent.Blatt)
                                        {
                                            // FEHLER: Blatt-Index von Vorder- und Rückseite stimmt nicht überein!
                                            addShipmentPageError(shipmentPage, new Exception($"Blatt-Index von Vorder- und Rückseite stimmt nicht überein!! Vorderseite = '{iBlatt}', Rückseite = '{sdlCurrent.Blatt}'"));
                                        }
                                    } 
                                    else
                                    {
                                        if ((iBlatt + 1) != sdlCurrent.Blatt)
                                        {
                                            // FEHLER: Blatt-Index nicht wie erwartet!
                                            addShipmentPageError(shipmentPage, new Exception($"Blatt-Index nicht wie erwartet! Erwartet = '{(iBlatt + 1)}', Geliefert = '{sdlCurrent.Blatt}'"));
                                        }
                                    }
                                    iBlatt = sdlCurrent.Blatt;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            finally
                            {
                                // SDL before aktualisieren
                                sdlBefore = sdlCurrent;
                            }
                        }

                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }
            finally
            {
                // close reader + document
                CloseReaderAndDocument();
            }

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Extract Mailpieces

        // Mailpieces extrahieren (22.05.2024, SME)
        public PdfFileExtractMailpiecesResult ExtractMailpieces(MailpieceExtractionParameters parameters, ProgressInfo progressInfo = null)
        {
            // init result
            var result = new PdfFileExtractMailpiecesResult(this, parameters);

            // exit-handling
            if (result.HasErrors) return result;

            try
            {
                // Print-PDF prüfen + Resultat zwischenspeichern (22.05.2024, SME)
                var checkPrintPdfResult = this.CheckPrintPdf(progressInfo);
                // => Fehler prüfen
                if (checkPrintPdfResult.HasAnyError)
                {
                    var errors = checkPrintPdfResult.GetAllErrors();
                    if (errors.Length == 1)
                    {
                        result.AddError(new Exception("Beim Prüfen des Druck-PDFs ist ein Fehler aufgetreten!", errors.First()));
                    }
                    else
                    {
                        result.AddError(new AggregateException($"Beim Prüfen des Druck-PDFs sind {errors.Length:n0} Fehler aufgetreten!", errors));
                    }
                    return result;
                }

                // reader
                if (PdfReader == null)
                {
                    PdfReader = new iPDF.PdfReader(FilePath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt
                // => egal bei ExtractMailpieces (22.05.2024, SME)

                // set output-path
                string outputPath = FilePath.Substring(0, FilePath.Length - DotPDF.Length) + $"_byMPs" + DotPDF;

                // writer
                using (var writer = new FullCompressionPdfWriter(outputPath))
                {
                    // target-pdf
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // Vorlauf
                        if (parameters.IncludeVorNachlauf)
                        {
                            foreach (var page in checkPrintPdfResult.Pages.Where(x => x.PageType == PrintPdfPageTypeEnum.Vorlauf).OrderBy(x => x.PageIndex))
                            {
                                try
                                {
                                    sourcePDF.CopyPagesTo(page.PageIndex, page.PageIndex, targetPDF);
                                }
                                catch (Exception ex)
                                {
                                    CoreFC.ThrowError(ex); throw ex;
                                }
                            }
                        }

                        // Mailpieces
                        var todoMailpieces = parameters.Mailpieces.ToList();
                        var pagesWithSDL = checkPrintPdfResult.Pages.OfType<PrintPdfShipmentPageInfo>().ToList();
                        var pagesWithoutSDL = pagesWithSDL.Where(x => x.SDL == null).ToList();
                        long mp;
                        foreach (var mailpiece in todoMailpieces.ToArray())
                        {
                            try
                            {
                                // Prüfen ob Mailpiece nummerisch ist
                                if (long.TryParse(mailpiece, out mp))
                                {
                                    // Seiten mit dieser Mailpiece zwischenspeichern
                                    var todoPages = pagesWithSDL.Where(x => x.SDL != null && x.SDL.Mailpiece == mp).OrderBy(x => x.PageIndex).ToList();
                                    if (!todoPages.Any())
                                    {
                                        // Mailpiece nicht gefunden
                                        // => kein Fehler auslösen gem. Reto (24.05.2024, SME)
                                        // result.AddError(new Exception($"Mailpiece nicht gefunden: '{mailpiece}'"));
                                        todoMailpieces.Remove(mailpiece);
                                    }
                                    else
                                    {
                                        // Mailpiece gefunden
                                        result.AddFoundMailpiece(mailpiece); // Mailpiece zur Liste der gefundenen MP's hinzufügen (24.05.2024, SME)
                                        todoMailpieces.Remove(mailpiece);
                                        int pageFrom = todoPages.First().PageIndex;
                                        int pageTo = todoPages.Last().PageIndex;
                                        if (pageTo % 2 != 0 && checkPrintPdfResult.SX_DX.HasValue && checkPrintPdfResult.SX_DX.Value == SxDxEnum.DX)
                                        {
                                            pageTo++;
                                        }
                                        for (int iPage = pageFrom; iPage <= pageTo; iPage++)
                                        {
                                            try
                                            {
                                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);
                                            }
                                            catch (Exception ex)
                                            {
                                                CoreFC.ThrowError(ex); throw ex;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Nicht nummerische Mailpiece
                                    result.AddError(new InvalidCastException($"Nicht nummerische Mailpiece: '{mailpiece}'"));
                                    todoMailpieces.Remove(mailpiece);
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }
                        if (todoMailpieces.Any())
                        {
                            foreach (var mailpiece in todoMailpieces)
                            {
                                // Mailpiece nicht gefunden
                                // => kein Fehler auslösen gem. Reto (24.05.2024, SME)
                                // result.AddError(new Exception($"Mailpiece nicht gefunden: '{mailpiece}'"));
                            }
                        }

                        // Nachlauf
                        if (parameters.IncludeVorNachlauf)
                        {
                            foreach (var page in checkPrintPdfResult.Pages.Where(x => x.PageType == PrintPdfPageTypeEnum.Nachlauf).OrderBy(x => x.PageIndex))
                            {
                                try
                                {
                                    sourcePDF.CopyPagesTo(page.PageIndex, page.PageIndex, targetPDF);
                                }
                                catch (Exception ex)
                                {
                                    CoreFC.ThrowError(ex); throw ex;
                                }
                            }
                        }

                        // flush
                        targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        targetPDF.Close();
                        writer.Close();

                        // delete pdf if no mailpieces were found
                        if (!result.MailpiecesFound.Any())
                        {
                            CoreFC.IsFileLocked_WaitMaxSeconds(outputPath, 5);
                            CoreFC.TryDeleteFile(outputPath, 5);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                // => Nicht nötig, da das Original nicht verändert wird
                //RestoreOriginal(inputPath);
            }
            finally
            {
                // refresh infos
                // braucht es für ExtractMailpieces nicht (22.05.2024, SME)
                //RefreshInfos();

                // close reader + document
                CloseReaderAndDocument();
            }

            // refresh file-size-info
            RefreshFileInfos();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Extract Page-Texts

        // Extract Page-Texts (05.04.2024, SME)
        public PdfFileExtractPageTextsResult ExtractPageTexts(ProgressInfo progressInfo = null)
        {
            // init result
            var result = new PdfFileExtractPageTextsResult(this);
            
            // Version-Testing
            int version = 3; // 0 = Page-Text, 1 = Page-Area-Text (v1), 2 = Page-Area-Text (v2)
            var sw = Stopwatch.StartNew();
            /*
             * V0: 
             * V1:
             * V2:
             */

            try
            {
                #region Start

                // reader
                if (PdfReader == null) PdfReader = new iPDF.PdfReader(this.FilePath);
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null) PdfDocument = new iPDF.PdfDocument(reader);
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                #endregion

                // 1. Alle Seiten zwischenspeichern und dem Resultat hinzufügen (Normaler Modus) (ohne Progress-Step)
                var pageList = new Dictionary<PdfPage, PdfPageInfoWithText>();
                for (int iPage = 1; iPage <= PageCount; iPage++)
                {
                    try
                    {
                        // Seite zwischenspeichern
                        var page = sourcePDF.GetPage(iPage);

                        // Neue Seiten-Info erstellen
                        var pageInfo = new PdfPageInfoWithText(this, iPage, "?");

                        // Objekte der Liste hinzufügen
                        pageList.Add(page, pageInfo);

                        // Page-Info ebenfalls dem Resultat hinzufügen
                        result.AddPage(pageInfo);
                    }
                    catch (Exception ex)
                    {
                        // error
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }

                // 2. Text von allen Seiten extrahieren und im Resultat nachtragen (Paralleler Modus)
                var area = new iText.Kernel.Geom.Rectangle(x: 0, y: 421, width: 595, height: 210);
                var filterListener = new FilteredTextEventListener(new LocationTextExtractionStrategy(), new TextRegionEventFilter(area));
                var setPageText = new Action<PdfPage, PdfPageInfoWithText>((page, pageInfo) =>
                {
                    try
                    {
                        string text;
                        if (version == 0) text = FC_PDF.ExtractTextFromPage(page);
                        else if (version == 1) text = FC_PDF.ExtractTextFromPageArea_v1(page, area);
                        else if (version == 2) text = FC_PDF.ExtractTextFromPageArea_v2(page, area);
                        // => Diese Version bringt gar nichts, da immer der Text der vorgängigen Seiten ebenfalls zurückgeliefert wird (23.04.2024, SME)
                        //else if (version == 3) text = FC_PDF.ExtractTextFromPageArea_v3(page, filterListener);
                        else text = FC_PDF.ExtractTextFromPage(page);

                        pageInfo.SetText(text);
                    }
                    catch (Exception ex)
                    {
                        pageInfo.AddError(ex);
                    }
                    finally
                    {
                        // perform step
                        if (progressInfo != null)
                        {
                            lock (progressInfo)
                            {
                                progressInfo.PerformStep();
                            }
                        }
                    }
                });
                //Parallel.ForEach(pageList, new ParallelOptions { MaxDegreeOfParallelism = 3 }, x => setPageText(x.Key, x.Value));
                //pageList.AsParallel().ForAll(x => setPageText(x.Key, x.Value)); // => DOESN'T WORK !!! (05.04.2024, SME)
                pageList.ToList().ForEach(x => setPageText(x.Key, x.Value));
                //foreach (var item in pageList)
                //{
                //    setPageText(item.Key, item.Value);
                //}

            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }

            sw.Stop();
            Console.WriteLine($"Version {version} took {sw.Elapsed} for {PageCount:n0} Pages");
            Debugger.Break();

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region Remove Vor-/Nachlauf

        // Vor-/Nachlauf entfernen (28.06.2024, SME)
        public PdfFileRemoveVorNachlaufResult RemoveVorNachlauf(ProgressInfo progressInfo = null)
        {
            // init result
            var result = new PdfFileRemoveVorNachlaufResult(this);

            // Deklarationen
            PrintPdfPageTypeEnum pageType = PrintPdfPageTypeEnum.Unbekannt;
            string inputPath = string.Empty;

            try
            {
                #region Start

                // reader
                if (PdfReader == null) PdfReader = new iPDF.PdfReader(this.FilePath);
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null) PdfDocument = new iPDF.PdfDocument(reader);
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                #endregion

                #region SX/DX ermitteln

                // SX/DX ermitteln
                SxDxEnum? sx_dx = CoreFC.GetSxDx(this.FileName);
                bool isDX = sx_dx.HasValue && sx_dx.Value == SxDxEnum.DX;

                #endregion

                #region Seiten abhandeln um Vor-/Nachlauf-Seiten zu ermitteln

                // Loop durch Seiten um Vor-/Nachlauf zu erkennen
                for (int iPage = 1; iPage <= PageCount; iPage++)
                {
                    try
                    {
                        // Seite zwischenspeichern
                        var page = sourcePDF.GetPage(iPage);

                        // Text von Seite extrahieren
                        var text = FC_PDF.ExtractTextFromPage(page);

                        // Zeilen von Text ermitteln
                        var lines = CoreFC.GetLines(text, null, true).ToList();

                        // Zeilen abhandeln
                        if (lines.Any(x => x.ToUpper().Equals(VORLAUF)))
                        {
                            pageType = PrintPdfPageTypeEnum.Vorlauf;
                        }
                        else if (lines.Any(x => x.ToUpper().Equals(NACHLAUF)))
                        {
                            pageType = PrintPdfPageTypeEnum.Nachlauf;
                        }
                        else if (pageType == PrintPdfPageTypeEnum.Vorlauf || pageType == PrintPdfPageTypeEnum.Nachlauf)
                        {
                            if (isDX && iPage % 2 == 0)
                            {
                                // Rückseite von Vor-/Nachlauf
                            }
                            else if (!lines.Any())
                            {
                                // Leere Seite
                                pageType = PrintPdfPageTypeEnum.Fuellseite;
                            }
                            else
                            {
                                // Irgendeine andere Seite (wahrscheinlich Sendungseite)
                                pageType = PrintPdfPageTypeEnum.Unbekannt;
                            }
                        }
                        else
                        {
                            // Irgendeine andere Seite (wahrscheinlich Sendungseite)
                            pageType = PrintPdfPageTypeEnum.Unbekannt;
                        }

                        // Vor-/Nachlauf-Seite dem Resultat hinzufügen
                        if (pageType == PrintPdfPageTypeEnum.Vorlauf) result.AddVorlaufSeite(iPage);
                        else if (pageType == PrintPdfPageTypeEnum.Nachlauf) result.AddNachlaufSeite(iPage);
                    }
                    catch (Exception ex)
                    {
                        // error
                        CoreFC.ThrowError(ex); throw ex;
                    }
                    finally
                    {
                        // perform step
                        progressInfo?.PerformStep();
                    }
                }

                #endregion

                #region Seiten extrahieren ohne Vor-/Nachlauf

                // Prüfen ob Vor-/Nachlauf vorhanden
                if (result.HatVorlaufSeiten || result.HatNachlaufSeiten)
                {
                    // Prüfen ob Verschlüsselt
                    if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                    {
                        throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                    }

                    // Original-Datei backupen
                    inputPath = MoveFileToOriginalPdfFolder();

                    // reader
                    if (PdfReader == null)
                    {
                        PdfReader = new iPDF.PdfReader(inputPath);
                    }
                    reader = PdfReader;

                    // source-pdf
                    if (PdfDocument == null)
                    {
                        PdfDocument = new iPDF.PdfDocument(reader);
                    }
                    sourcePDF = PdfDocument;

                    // read infos
                    ReadInfos();

                    // Prüfen ob Verschlüsselt
                    if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                    {
                        throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                    }

                    // writer
                    using (var writer = new FullCompressionPdfWriter(this.FilePath))
                    {
                        // target-pdf
                        using (var targetPDF = new iPDF.PdfDocument(writer))
                        {
                            // store number of pages
                            var pageCount = sourcePDF.GetNumberOfPages();

                            // copy pages
                            for (int iPage = 1; iPage <= pageCount; iPage++)
                            {
                                try
                                {
                                    // Auslassen wenn Vor-/Nachlauf
                                    if (result.IstVorlaufSeite(iPage)) continue;
                                    if (result.IstNachlaufSeite(iPage)) continue;

                                    // Seite kopieren, da weder Vor- noch Nachlauf
                                    sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                    // flush all 100 pages to reduce memory-usage
                                    if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);
                                }
                                catch (Exception ex)
                                {
                                    CoreFC.ThrowError(ex); throw ex;
                                }
                                finally
                                {
                                    // perform step
                                    progressInfo?.PerformStep();
                                }
                            }

                            // flush
                            targetPDF.FlushCopiedObjects(sourcePDF);

                            // close objects
                            targetPDF.Close();
                            writer.Close();
                        }
                    }
                }

                #endregion

                // refresh infos
                RefreshInfos();

                // end
                result.End();
            }
            catch (Exception ex)
            {
                // end with error
                result.EndWithError(ex);

                // restore original
                RestoreOriginal(inputPath);
            }

            // return
            ActionResultList.Add(result);
            return result;
        }

        #endregion

        #region TESTING

        // Perform TEST (28.05.2023, SME)
        // => remove green text + magenta
        public PdfFileActionResult PerformTEST(ProgressInfo progressInfo = null)
        {
            var onlyComporessImages = false;
            if (onlyComporessImages) return CompressImages(progressInfo);

            bool onlyExtractPageTexts = true;
            if (onlyExtractPageTexts)
            {
                var resultPageTexts = ExtractPageTexts(progressInfo);
                return resultPageTexts;
            }

            var result = new PdfFileActionResult(PdfActionEnum.TEST, this);

            // init input-path
            string inputPath = FilePath;
            //int done = 0;

            try
            {
                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value) throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");

                // Original-Datei backupen (09.05.2023, SME)
                // inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null) PdfReader = new iPDF.PdfReader(inputPath);
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null) PdfDocument = new iPDF.PdfDocument(reader);
                var sourcePDF = PdfDocument;

                // read infos
                ReadInfos();

                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value) throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");

                #region Feld-Werte ermitteln über Annotations (REMARKED)

                //// declarations
                //var returnList = new Dictionary<int, Dictionary<string, string>>();

                //// store number of pages
                //var pageCount = sourcePDF.GetNumberOfPages();

                //// start progress
                //if (progressInfo != null)
                //{
                //    if (!progressInfo.IsRunning)
                //    {
                //        progressInfo.Start(pageCount, $"Feldwerte werden ermittelt ...");
                //    }
                //    else if (progressInfo.TotalSteps == 0)
                //    {
                //        progressInfo.SetTotalSteps(pageCount);
                //        progressInfo.SetStatus($"Feldwerte werden ermittelt ...");
                //    }
                //}

                //// loop throu pages
                //for (int iPage = 1; iPage <= pageCount; iPage++)
                //{
                //    try
                //    {
                //        // store page
                //        var page = sourcePDF.GetPage(iPage);

                //        // store annotations
                //        var annots = page.GetAnnotations();

                //        // read field-values
                //        if (annots != null && annots.Any())
                //        {
                //            // create field-value-list
                //            var fieldValueList = new Dictionary<string, string>();

                //            // loop throu annotations
                //            foreach (var annot in annots)
                //            {
                //                // store + check title
                //                var title = annot.GetTitle();
                //                if (title == null) continue;

                //                // get value
                //                string value = string.Empty;
                //                var pdfObject = annot.GetPdfObject();
                //                if (pdfObject != null && pdfObject.ContainsKey(PdfName.V))
                //                {
                //                    value = pdfObject.Get(PdfName.V).ToString();
                //                }

                //                // add field-value
                //                fieldValueList.Add(title.GetValue(), value);
                //            }

                //            // add field-value-list to return-list
                //            returnList.Add(iPage, fieldValueList);
                //        }

                //        // perform step
                //        progressInfo?.PerformStep();
                //    }
                //    catch (Exception ex)
                //    {
                //        CoreFC.ThrowError(ex); throw ex;
                //    }
                //}

                //if (returnList.Any())
                //{
                //    Debugger.Break();
                //}

                #endregion

                #region Feld-Werte ermitteln über Acro-Form (REMARKED)

                //PdfAcroForm pdfAcroForm = PdfAcroForm.GetAcroForm(sourcePDF, false);
                //if (pdfAcroForm != null)
                //{
                //    var fields = pdfAcroForm.GetFormFields();
                //    if (fields != null && fields.Any())
                //    {
                //        var fieldValueList = new Dictionary<string, object>();
                //        foreach (var field in fields)
                //        {
                //            try
                //            {
                //                fieldValueList.Add(field.Key, field.Value);
                //            }
                //            catch (Exception ex)
                //            {
                //                Console.WriteLine(ex.Message);
                //            }
                //        }
                //        Debugger.Break();
                //    }
                //}

                #endregion

                #region Text von Seiten ermitteln

                var pageTextList = new Dictionary<int, string>();

                for (int iPage = 1; iPage <= PageCount; iPage++)
                {
                    try
                    {
                        // store page
                        var page = sourcePDF.GetPage(iPage);

                        // extract text
                        var text = FC_PDF.ExtractTextFromPage(page);

                        // add to list
                        pageTextList.Add(iPage, text);
                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }

                #endregion

                #region Extract Images (REMARKED)

                //var listener = new ImageRenderListener(System.IO.Path.GetFileNameWithoutExtension(FilePath));
                //var parser = new PdfCanvasProcessor(listener);
                //for (int i = 1; i <= sourcePDF.GetNumberOfPages(); i++)
                //{
                //    parser.ProcessPageContent(sourcePDF.GetPage(i));
                //    progressInfo?.PerformStep();
                //}

                //// loop throu objects
                //var objectCount = sourcePDF.GetNumberOfPdfObjects();
                //for (int iObject = 1; iObject <= objectCount; iObject++)
                //{
                //    try
                //    {
                //        var pdfObject = sourcePDF.GetPdfObject(iObject);
                //        if (pdfObject == null) continue;
                //        if (!(pdfObject is PdfStream)) continue;
                //        if (!(pdfObject.IsIndirect())) continue;

                //        var pdfStream = (PdfStream)pdfObject;
                //        if (!(PdfName.XObject.Equals(pdfStream.Get(PdfName.Type)))) continue;
                //        if (!(PdfName.Image.Equals(pdfStream.Get(PdfName.Subtype)))) continue;



                //        try
                //        {
                //            string width = pdfStream.ContainsKey(PdfName.Width) ? pdfStream.Get(PdfName.Width).ToString() : "";
                //            string height = pdfStream.ContainsKey(PdfName.Height) ? pdfStream.Get(PdfName.Height).ToString() : "";
                //            string decode = pdfStream.ContainsKey(PdfName.DecodeParms) ? pdfStream.Get(PdfName.DecodeParms).ToString() : "";
                //            string bitspercomponent = pdfStream.ContainsKey(PdfName.BitsPerComponent) ? pdfStream.Get(PdfName.BitsPerComponent).ToString() : "";
                //            string colorspace = pdfStream.ContainsKey(PdfName.ColorSpace) ? pdfStream.Get(PdfName.ColorSpace).ToString() : "";
                //            string name = pdfStream.ContainsKey(PdfName.Name) ? pdfStream.Get(PdfName.Name).ToString() : "";

                //            var bytes = pdfStream.GetBytes(true);
                //            // skip images that are smaller then 500 KB
                //            if (bytes.Length < 500 * 1024) continue;

                //            //var imageStream = new MemoryStream(bytes);
                //            //var image = System.Drawing.Image.FromStream(imageStream);
                //            var image = CoreFC.GetImage(bytes);

                //            // WARNING: disposing the image-stream will lead to an error ("Allgemeiner Fehler in GDI+")
                //            //imageStream.Dispose();

                //            //var form = new frmImage(image);
                //            //form.Show();
                //        }
                //        catch (Exception ex)
                //        {
                //            CoreFC.DPrint("ERROR while extracting image: " + ex.Message);
                //        }

                //        //var imageRenderer = new ImageRenderInfo()
                //        //ImageRenderInfo imgRI = ImageRenderInfo.CreateForXObject(new GraphicsState(), (PRIndirectReference)obj, tg);
                //        //PdfImageObject image = imgRI.GetImage();
                //        //Image dotnetImg = image.GetDrawingImage();

                //    }
                //    catch (Exception ex)
                //    {
                //        throw ex;
                //    }
                //}

                #endregion

                #region REMARKED

                //var tree = sourcePDF.GetStructTreeRoot();
                //var pageX = sourcePDF.GetPage(1);

                //// XMPL to extract text
                ////iText.Kernel.Pdf.Canvas.Parser.Listener.ITextExtractionStrategy strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.SimpleTextExtractionStrategy();
                //iText.Kernel.Pdf.Canvas.Parser.Listener.ITextExtractionStrategy strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.SimpleTextExtractionStrategy();
                //string currentText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(pageX, strategy);
                ////iText.Kernel.Pdf.Canvas.Parser.Util.PdfCanvasParser xyz = null;

                //currentText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(
                //    Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));
                //Debugger.Break();

                //var resources = pageX.GetResources();
                //var xObject = resources.GetResource(PdfName.XObject);
                //if (xObject != null)
                //{
                //    foreach (var key in ((PdfDictionary)xObject).KeySet().ToArray())
                //    {
                //        var image = resources.GetImage(key);
                //        if (image != null)
                //        {
                //            try
                //            {
                //                var imageBytes = image.GetImageBytes();
                //                var path = inputPath + "_page1_" + key.ToString().Substring(1) + ".png";
                //                if (File.Exists(path)) File.Delete(path);
                //                File.WriteAllBytes(path, imageBytes);
                //                Debugger.Break();
                //            }
                //            catch (Exception ex)
                //            {
                //                CoreFC.DPrint(ex);
                //            }
                //        }
                //    }
                //}

                //var contentCount = pageX.GetContentStreamCount();
                //for (int iStream = 0; iStream < contentCount; iStream++)
                //{
                //    var stream = pageX.GetContentStream(iStream);
                //    if (stream != null)
                //    {
                //        var bytes = stream.GetBytes();
                //        var text = System.Text.Encoding.UTF8.GetString(bytes);
                //        Debugger.Break();
                //    }
                //}


                //// get acro-form (31.05.2023, SME)
                //var acroForm = PdfAcroForm.GetAcroForm(sourcePDF, false);
                //if (acroForm != null)
                //{
                //    var fields = acroForm.GetFormFields();
                //    Debugger.Break();
                //}

                //// writer
                //using (var writer = new FullCompressionPdfWriter(this.FilePath))
                //{
                //    // target-pdf
                //    using (var targetPDF = new iPDF.PdfDocument(writer))
                //    {
                //        // store number of pages
                //        var pageCount = sourcePDF.GetNumberOfPages();

                //        // copy pages
                //        for (int iPage = 1; iPage <= pageCount; iPage++)
                //        {
                //            try
                //            {
                //                // copy page
                //                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                //                // EXECUTE TEST-CODE HERE
                //                var page = targetPDF.GetPage(iPage);
                //                done = ReplaceCmykValuesInPage(page);
                //            }
                //            catch (Exception ex)
                //            {
                //                CoreFC.ThrowError(ex); throw ex;
                //            }
                //        }

                //        // close objects
                //        sourcePDF.Close();
                //        targetPDF.Close();
                //        writer.Close();
                //        reader.Close();

                //        // return
                //        return done;
                //    }
                //}

                #endregion
            }
            catch (Exception ex)
            {
                // restore original
                RestoreOriginal(inputPath);

                // throw error
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // close
                CloseReaderAndDocument();
            }

            // return
            ActionResultList.Add(result);
            return result;
        }

        // Get Field-Value-List from PDF-FilePath (31.01.2024, SME)
        //public Dictionary<int, Dictionary<string, string>> GetFileValueListFromPdf(string pdfFilePath)
        //{
        //    // declarations
        //    var returnList = new Dictionary<int, Dictionary<string, string>>();

        //    // use reader
        //    using (var reader = new iPDF.PdfReader(pdfFilePath))
        //    {
        //        // use document
        //        using (var document = new iPDF.PdfDocument(reader))
        //        {
        //            // loop throu pages
        //            for (int iPage = 1; iPage <= document.GetNumberOfPages(); iPage++)
        //            {
        //                try
        //                {
        //                    // store page
        //                    var page = document.GetPage(iPage);

        //                    // store annotations
        //                    var annots = page.GetAnnotations();

        //                    // read field-values
        //                    if (annots != null && annots.Any())
        //                    {
        //                        // create field-value-list
        //                        var fieldValueList = new Dictionary<string, string>();

        //                        // loop throu annotations
        //                        foreach (var annot in annots)
        //                        {
        //                            // store + check title
        //                            var title = annot.GetTitle();
        //                            if (title == null) continue;

        //                            // get value
        //                            string value = string.Empty;
        //                            var pdfObject = annot.GetPdfObject();
        //                            if (pdfObject != null && pdfObject.ContainsKey(PdfName.V))
        //                            {
        //                                value = pdfObject.Get(PdfName.V).ToString();
        //                            }

        //                            // add field-value
        //                            fieldValueList.Add(title.GetValue(), value);
        //                        }

        //                        // add field-value-list to return-list
        //                        returnList.Add(iPage, fieldValueList);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    CoreFC.ThrowError(ex); throw ex;
        //                }
        //            }
        //        }
        //    }

        //    // return
        //    return returnList;
        //}

        #endregion

        // Remove PDF-Object by Path (27.05.2023, SME)
        public bool RemovePdfObjectByPath(List<string> path)
        {
            // init input-path
            string inputPath = FilePath;
            bool status = false;

            try
            {
                // exit-handling
                if (path == null || !path.Any()) return false;
                if (path.First() != "Page") return false;
                path.RemoveAt(0);
                if (!path.Any()) return false;
                if (!CoreFC.IsNumeric(path.First())) return false;
                var pageIndex = int.Parse(path.First());
                path.RemoveAt(0);
                if (!path.Any()) return false;

                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // Original-Datei backupen (09.05.2023, SME)
                inputPath = MoveFileToOriginalPdfFolder();

                // reader
                if (PdfReader == null)
                {
                    //if (progressInfo != null) progressInfo.SetStatus("PDF-Reader wird initialisiert ...");
                    PdfReader = new iPDF.PdfReader(inputPath);
                }
                var reader = PdfReader;

                // source-pdf
                if (PdfDocument == null)
                {
                    //if (progressInfo != null) progressInfo.SetStatus("Quell-PDF-Dokument wird initialisiert ...");
                    PdfDocument = new iPDF.PdfDocument(reader);
                }
                var sourcePDF = PdfDocument;

                // read infos
                //ReadInfos(progressInfo);
                ReadInfos();

                // Prüfen ob Verschlüsselt (25.05.2023, SME)
                if (this.IsEncrypted.HasValue && this.IsEncrypted.Value)
                {
                    throw new Exception("Das PDF ist geschützt und kann somit nicht bearbeitet werden!");
                }

                // writer
                //if (progressInfo != null) progressInfo.SetStatus("PDF-Writer wird initialisiert ...");
                using (var writer = new FullCompressionPdfWriter(this.FilePath))
                {
                    // target-pdf
                    //if (progressInfo != null) progressInfo.SetStatus("Ziel-PDF-Dokument wird initialisiert ...");
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // store number of pages
                        //if (progressInfo != null) progressInfo.SetStatus("Anzahl Seiten werden ermittelt ...");
                        var pageCount = sourcePDF.GetNumberOfPages();

                        // start progress
                        //if (progressInfo != null)
                        //{
                        //    if (!progressInfo.IsRunning)
                        //    {
                        //        progressInfo.Start(pageCount, $"{pageCount:n0} Seiten werden kopiert ...");
                        //    }
                        //    else
                        //    {
                        //        progressInfo.SetTotalSteps(pageCount);
                        //        progressInfo.SetStatus($"{pageCount:n0} Seiten werden kopiert ...");
                        //    }
                        //}

                        // copy pages
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            try
                            {
                                // copy page
                                sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                // flush all 100 pages to reduce memory-usage
                                //if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                if (pageIndex.Equals(iPage))
                                {
                                    //targetPDF.FlushCopiedObjects(sourcePDF);
                                    var page = targetPDF.GetPage(iPage);
                                    if (page != null)
                                    {
                                        // find object + remove it
                                        PdfObject parentObject = page.GetPdfObject();
                                        PdfName currentKey = null;
                                        while (path.Any() && parentObject != null)
                                        {
                                            if (parentObject is PdfDictionary)
                                            {
                                                currentKey = null;
                                                ICollection<PdfName> keySet = null;
                                                try
                                                {
                                                    keySet = ((PdfDictionary)parentObject).KeySet();
                                                    if (keySet == null) throw new Exception("KeySet could not be retrieved!");
                                                }
                                                catch (Exception ex)
                                                {
                                                    throw new Exception("Error while retrieving KeySet: " + ex.Message);
                                                }

                                                foreach (var key in keySet.ToArray())
                                                {
                                                    if (path.First().Equals(key.ToString().Substring(1)))
                                                    {
                                                        currentKey = key;
                                                        break;
                                                    }
                                                }
                                                if (currentKey == null) throw new Exception($"Der Key für '{path.First()}' wurde nicht gefunden!");

                                                if (path.Count == 1)
                                                {
                                                    // remove this object
                                                    try
                                                    {
                                                        ((PdfDictionary)parentObject).Remove(currentKey);
                                                        status = true;
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        throw new Exception("ERROR while removing Key: " + ex.Message);
                                                    }
                                                    path.Clear();
                                                    break;
                                                }
                                                else
                                                {
                                                    // continue search
                                                    try
                                                    {
                                                        parentObject = ((PdfDictionary)parentObject).Get(currentKey);
                                                        path.RemoveAt(0);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        throw new Exception("ERROR while retrieving Object by Key: " + ex.Message);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                throw new Exception("Unhandled PDF-Object-Type: " + parentObject.GetType().Name);
                                            }
                                        }
                                    }

                                }

                                // perform step
                                //if (progressInfo != null) progressInfo.PerformStep();
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // flush
                        //targetPDF.FlushCopiedObjects(sourcePDF);

                        // close objects
                        //if (progressInfo != null) progressInfo.SetStatus("Optimiertes PDF wird geschrieben ...");
                        sourcePDF.Close();
                        targetPDF.Close();
                        writer.Close();
                        reader.Close();

                        // return
                        return status;
                    }
                }
            }
            catch (Exception ex)
            {
                // restore original
                RestoreOriginal(inputPath);

                // throw error
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // close reader + document
                CloseReaderAndDocument();
            }
        }

        // Replace CMYK-Values in Page (28.05.2023, SME)
        private int ReplaceCmykValuesInPage(PdfPage page)
        {
            int done = 0;

            try
            {
                var resources = page.GetResources();
                var colorSpacesDic = resources.GetResource(PdfName.ColorSpace);
                if (colorSpacesDic != null)
                {
                    foreach (var key in colorSpacesDic.KeySet().ToArray())
                    {
                        var colorSpace = resources.GetColorSpace(key);
                        if (colorSpace != null)
                        {
                            var colorSpaceObj = colorSpace.GetPdfObject();
                            if (colorSpaceObj is PdfArray)
                            {
                                var colorSpaceArray = (PdfArray)colorSpaceObj;
                                bool isPerforation = false;
                                bool isLaser = false;
                                //bool isDeviceCMYK = false;
                                bool handleNextAsDeviceCMYK = false;
                                foreach (var item in colorSpaceArray)
                                {
                                    if (item is PdfName)
                                    {
                                        var name = item.ToString().Substring(1);
                                        switch (name.ToLower())
                                        {
                                            case "perforation":
                                                isPerforation = true;
                                                handleNextAsDeviceCMYK = false;
                                                break;
                                            case "laser":
                                                isLaser = true;
                                                handleNextAsDeviceCMYK = false;
                                                break;
                                            case "devicecmyk":
                                                //isDeviceCMYK = true;
                                                if (isLaser || isPerforation)
                                                {
                                                    handleNextAsDeviceCMYK = true;
                                                }
                                                else
                                                {
                                                    handleNextAsDeviceCMYK = false;
                                                }
                                                break;
                                            default:
                                                handleNextAsDeviceCMYK = false;
                                                break;
                                        }
                                    }
                                    else if (item is PdfDictionary)
                                    {
                                        var dict = (PdfDictionary)item;

                                        if (handleNextAsDeviceCMYK)
                                        {
                                            foreach (var cmykKey in dict.KeySet().ToArray())
                                            {
                                                if (cmykKey.ToString().Substring(1).ToLower().Equals("c1"))
                                                {
                                                    var c1Obj = dict.Get(cmykKey);
                                                    if (c1Obj is PdfArray)
                                                    {
                                                        //iText.Kernel.Pdf.Canvas.Parser.PdfDocumentContentParser parser = null;
                                                        //iText.Kernel.Pdf.Canvas.Parser.PdfCanvasProcessor canvasProcessor = null;

                                                        var c1Array = (PdfArray)c1Obj;
                                                        foreach (var c1Item in c1Array)
                                                        {
                                                            if (c1Item is PdfNumber)
                                                            {
                                                                var c1 = (PdfNumber)c1Item;
                                                                var value = c1.GetValue();
                                                                if (value != 0)
                                                                {
                                                                    c1.SetValue(0);
                                                                    done++;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            handleNextAsDeviceCMYK = false;
                                        }
                                    }
                                    else
                                    {
                                        handleNextAsDeviceCMYK = false;
                                    }
                                }
                            }
                        }
                    }
                }

                return done;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }
    }
}