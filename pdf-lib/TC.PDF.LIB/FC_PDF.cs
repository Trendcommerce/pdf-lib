using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Filter;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TC.Classes;
using TC.Functions;
using TC.Global;
using TC.PDF.LIB.Classes;
using TC.PDF.LIB.Classes.Results;
using TC.PDF.LIB.Classes.Results.Core;
using TC.PDF.LIB.Data;
using TC.Resources.Fonts.LIB.Data;
using TC.Resources.Fonts.LIB.Functions;
using static TC.Constants.CoreConstants;
using static TC.PDF.LIB.CONST_PDF;
using iPDF = iText.Kernel.Pdf;

namespace TC.PDF.LIB
{
    public static class FC_PDF
    {
        #region OK to keep in this Function-Class

        #region Is valid PDF-Filepath

        // Ermittelt ob der mitgelierte String ein gültiger PDF-Dateipfad ist (03.04.2023, SME)
        public static bool IsValidPdfFilePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!File.Exists(path)) return false;
            if (!path.ToLower().EndsWith(DotPDF)) return false;
            return true;
        }

        #endregion

        #region Get PDF-FilePaths

        // PDF-Dateien von String-Liste ermitteln (03.04.2023, SME)
        public static string[] GetPdfFilePaths(string[] paths)
        {
            return paths.Where(IsValidPdfFilePath).ToArray();
        }

        #endregion

        #region Get PDF-Action

        // Get PDF-Action from String (28.04.2023, SME)
        public static PdfActionEnum? GetPdfAction(string action)
        {
            // exit-handling
            if (string.IsNullOrEmpty(action)) return null;

            // loop throu actions
            foreach (var actionEnum in CoreFC.GetEnumValues<PdfActionEnum>())
            {
                if (actionEnum.ToString().ToLower().Equals(action.ToLower()))
                {
                    return actionEnum;
                }
            }

            // no action found
            return null;
        }

        #endregion

        #region Get Conformance-Level

        // Get Conformance-Level-String (31.03.2023, SME)
        public static string GetConformanceLevelString(iPDF.PdfAConformanceLevel conformanceLevel)
        {
            if (conformanceLevel == null) return string.Empty;

            return "PDF_A_" + conformanceLevel.GetPart() + conformanceLevel.GetConformance();
        }

        // Get Conformance-Level-Enum (31.03.2023, SME)
        public static PdfConformanceLevelEnum GetConformanceLevelEnum(iPDF.PdfAConformanceLevel conformanceLevel)
        {
            string value = GetConformanceLevelString(conformanceLevel);
            if (string.IsNullOrEmpty(value))
            {
                return PdfConformanceLevelEnum.None;
            }
            else
            {
                var level = CoreFC.GetEnumValue<PdfConformanceLevelEnum>(value);
                if (level.HasValue)
                {
                    return level.Value;
                }
                else
                {
                    return PdfConformanceLevelEnum.Unknown;
                }
            }
        }

        #endregion

        #region Is empty Page

        // Ist leere Seite (20.04.2023, SME)
        public static bool IsEmptyPage(PdfPage page)
        {
            // Leer wenn Seite nicht gesetzt ist
            if (page == null) return true;

            // Leer wenn kein Content vorhanden ist
            if (!page.GetPdfObject().ContainsKey(PdfName.Contents)) return true;

            // Nicht leer wenn Text vorhanden ist
            if (!string.IsNullOrEmpty(ExtractTextFromPage(page))) return false;

            // Nicht leer wenn Bilder vorhanden sind
            if (PageHasImages(page)) return false;

            // In allen anderen Fällen leer
            return true;
        }

        #endregion

        #region Type-Checks

        // Prüft ob das PDF-Object ein Dictionary vom mitgelieferten Typ (= PdfName) ist (26.05.2023, SME)
        internal static bool IsDictionaryOfType(PdfObject pdfObject, PdfName typeName)
        {
            if (pdfObject == null) return false;
            if (typeName == null) throw new ArgumentNullException(nameof(typeName));
            if (!(pdfObject is PdfDictionary)) return false;
            var dict = (PdfDictionary)pdfObject;
            var type = dict.Get(PdfName.Type);
            if (type == null) return false;
            if (!(typeName.Equals(type))) return false;
            return true;
        }

        // Prüft ob das PDF-Object ein Dictionary vom mitgelieferten Sub-Typ (= PdfName) ist (16.06.2023, SME)
        internal static bool IsDictionaryOfSubType(PdfObject pdfObject, PdfName subTypeName)
        {
            if (pdfObject == null) return false;
            if (subTypeName == null) throw new ArgumentNullException(nameof(subTypeName));
            if (!(pdfObject is PdfDictionary)) return false;
            var dict = (PdfDictionary)pdfObject;
            var type = dict.Get(PdfName.Subtype);
            if (type == null) return false;
            if (!(subTypeName.Equals(type))) return false;
            return true;
        }

        // Prüft ob das PDF-Object ein Font ist (06.04.2023, SME)
        public static bool IsFont(PdfObject pdfObject)
        {
            return IsDictionaryOfType(pdfObject, PdfName.Font);
        }

        // Prüft ob das PDF-Object ein Resources-Objekt ist (26.05.2023, SME)
        public static bool IsResources(PdfObject pdfObject)
        {
            return IsDictionaryOfType(pdfObject, PdfName.Resources);
        }

        // Prüft ob das PDF-Object ein X-Objekt ist (16.06.2023, SME)
        public static bool IsXObject(PdfObject pdfObject)
        {
            return IsDictionaryOfType(pdfObject, PdfName.XObject);
        }

        // Prüft ob das PDF-Object ein Image ist (16.06.2023, SME)
        public static bool IsImage(PdfObject pdfObject)
        {
            if (pdfObject == null) return false;
            if (!(pdfObject is PdfStream)) return false;
            //if (!IsDictionaryOfType(pdfObject, PdfName.XObject)) return false;
            if (!IsDictionaryOfSubType(pdfObject, PdfName.Image)) return false;
            return true;
        }

        // Prüft ob das PDF-Object ein Form ist (16.06.2023, SME)
        public static bool IsForm(PdfObject pdfObject)
        {
            if (pdfObject == null) return false;
            if (!(pdfObject is PdfStream)) return false;
            //if (!IsDictionaryOfType(pdfObject, PdfName.XObject)) return false;
            if (!IsDictionaryOfSubType(pdfObject, PdfName.Form)) return false;
            return true;
        }

        #endregion

        #region Fonts

        // Is embedded Subset
        /// <summary>
        /// Finds out if the font is an embedded subset font
        /// </summary>
        /// <param name="name">font name</param>
        /// <returns>true if the name denotes an embedded subset font</returns>
        internal static bool IsEmbeddedSubset(String name)
        {
            //name = String.format("%s subset (%s)", name.substring(8), name.substring(1, 7));
            if (string.IsNullOrEmpty(name)) return false;
            if (name.StartsWith(Slash)) name = name.Substring(1);
            if (name.Length <= 7) return false;
            if (!name.Substring(6, 1).Equals(Plus)) return false;
            return true;
            //return name != null && name.Length > 8 && name.Substring(7, 1).Equals("+");
        }

        // Prüft ob das PDF-Object ein embedded Subset-Font ist (06.04.2023, SME)
        public static bool IsEmbeddedSubsetFont(PdfObject pdfObject)
        {
            if (!IsFont(pdfObject)) return false;
            var dict = (PdfDictionary)pdfObject;
            var baseFont = dict.Get(PdfName.BaseFont);
            if (baseFont == null) return false;
            if (!(IsEmbeddedSubset(baseFont.ToString()))) return false;
            return true;
        }

        // Prüft ob das mitgelieferte PDF-Dictionary eine eingebettete Schrift ist (23.05.2023, SME)
        // Code-Snippet: https://gist.github.com/warrengalyen/cebb9a4c8d6c17dfb1af01c3b7fedc11
        public static bool IsEmbeddedFont(PdfObject font)
        {
            // exit-handling
            if (font == null) return false;
            if (!IsFont(font)) return false;
            if (IsEmbeddedSubsetFont(font)) return true;

            // convert to dictionary
            var fontDictionary = font as PdfDictionary;

            // Font-Descriptor ermitteln
            var desc = fontDictionary.GetAsDictionary(PdfName.FontDescriptor);
            if (desc == null)
            {
                // Descendant Fonts ermitteln
                var descendant = fontDictionary.GetAsArray(PdfName.DescendantFonts);
                if (descendant == null)
                {
                    return false;
                }
                else
                {
                    if (descendant.Size() > 1)
                    {
                        Debugger.Break();
                    }
                    for (int i = 0; i < descendant.Size(); i++)
                    {
                        if (IsEmbeddedFont(descendant.GetAsDictionary(i)))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            else if (desc.Get(PdfName.FontFile) != null)
            {
                // Type 1 embedded
                return true;
            }
            else if (desc.Get(PdfName.FontFile2) != null)
            {
                // TrueType embedded
                return true;
            }
            else if (desc.Get(PdfName.FontFile3) != null)
            {
                // ? embedded
                return true;
            }
            else
            {
                // not embedded
                return false;
            }
        }

        // Präfix von empedded Subset-Font ermitteln (06.04.2023, SME)
        public static string GetEmbeddedSubsetFontPrefix(PdfObject pdfObject)
        {
            if (!IsEmbeddedSubsetFont(pdfObject)) return string.Empty;
            var dict = (PdfDictionary)pdfObject;
            var baseFont = dict.Get(PdfName.BaseFont);
            var prefix = baseFont.ToString().Substring(1);
            prefix = prefix.Substring(0, prefix.IndexOf("+"));
            return prefix;
        }

        #endregion

        #region Backup / Restore

        // Erstellt eine Backup-ZIP-Datei für das Merge-Ziel, in welchem alle zu mergenden PDFs enthalten sind (13.06.2023, SME)
        private static void BackupMergePdfsToZip(string mergeTargetFilePath, List<string> pdfFilePathsToMerge)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(mergeTargetFilePath)) throw new ArgumentNullException(nameof(mergeTargetFilePath));
                if (!Directory.Exists(Path.GetDirectoryName(mergeTargetFilePath))) throw new DirectoryNotFoundException("Der Ordner des PDF-Merge-Ziels wurde nicht gefunden!");
                if (pdfFilePathsToMerge == null || !pdfFilePathsToMerge.Any()) throw new ArgumentNullException(nameof(pdfFilePathsToMerge));
                if (CoreFC.IsFileLocked_WaitMaxSeconds(mergeTargetFilePath)) throw new Exception("Zu backupende Original-Datei ist gesperrt!" + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + mergeTargetFilePath);

                // TODO: Überschreiben eines bereits existierenden Merge-Ziels implementieren (13.06.2023, SME)

                // Backup-Pfad ermitteln
                string backupPath = GetBackupFilePath(mergeTargetFilePath, BakZip);

                // Backup-ZIP erstellen
                var data = new Data.PdfMergeInfo();
                var table = data.PdfMergeInfoTable;
                using (var zip = System.IO.Compression.ZipFile.Open(backupPath, System.IO.Compression.ZipArchiveMode.Create))
                {
                    // add files
                    foreach (var filePath in pdfFilePathsToMerge)
                    {
                        // create row
                        var row = table.NewPdfMergeInfoTableRow();
                        row.Guid = Guid.NewGuid();
                        row.Path = filePath;
                        row.Table.Rows.Add(row);

                        // create zip-entry
                        zip.CreateEntryFromFile(filePath, row.Guid.ToString() + DotPDF, CompressionLevel.Optimal);
                    }

                    // add data
                    data.AcceptChanges();
                    var entry = zip.CreateEntry(DataXml, CompressionLevel.Optimal);
                    using (var stream = entry.Open())
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            data.WriteXml(writer, System.Data.XmlWriteMode.IgnoreSchema);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Backup-Dateipfad ermitteln (13.06.2023, SME)
        private static string GetBackupFilePath(string originalFilePath, string fileTypeSuffix = BAK)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(originalFilePath)) throw new ArgumentNullException(nameof(originalFilePath));

                // Dateiendung zwischenspeichern
                string fileType = Path.GetExtension(originalFilePath);
                if (string.IsNullOrEmpty(fileType)) throw new Exception("Dateiendung konnte nicht ermittelt werden!" + CoreFC.Lines(2) + "Pfad: " + CoreFC.Lines() + originalFilePath);
                fileType += fileTypeSuffix;

                // Sicherstellen, dass "Originals"-Ordner existiert
                string backupPath = Path.Combine(Path.GetDirectoryName(originalFilePath), PdfBackupFolderName);
                if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);

                // Dateiname ohne Endung zum Backuppfad hinzufügen
                backupPath = Path.Combine(backupPath, Path.GetFileNameWithoutExtension(originalFilePath));

                // Sicherstellen, dass Backup-Dateipfad nicht existiert
                int counter = 0;
                while (File.Exists(backupPath + DOT + counter.ToString(ZweiNullen) + fileType))
                {
                    counter++;
                    if (counter > 99) throw new InvalidOperationException("Die maximale Anzahl der PDF-Backups von 99 ist erreicht!" + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + originalFilePath);
                }
                backupPath += DOT + counter.ToString(ZweiNullen) + fileType;

                // Rückgabe
                return backupPath;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Original-Datei in den Original-PDF-Ordner verschieben (09.05.2023, SME)
        // => Backup
        public static string MoveFileToOriginalPdfFolder(string originalFilePath)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(originalFilePath)) throw new ArgumentNullException(nameof(originalFilePath));
                if (!File.Exists(originalFilePath)) throw new FileNotFoundException("Zu backupende Original-Datei wurde nicht gefunden!", originalFilePath);
                if (CoreFC.IsFileLocked_WaitMaxSeconds(originalFilePath)) throw new Exception("Zu backupende Original-Datei ist gesperrt!" + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + originalFilePath);

                // Backup-Pfad ermitteln
                string backupPath = GetBackupFilePath(originalFilePath);

                // Datei verschieben/backupen
                File.Move(originalFilePath, backupPath);

                // Rückgabe
                return backupPath;
            }
            catch (Exception ex)
            {
                if (CoreFC.IsFileLockedError(ex))
                {
                    throw new TC.Errors.FileLockedError(originalFilePath);
                }
                else
                {
                    CoreFC.ThrowError(ex); throw ex;
                }
            }
        }

        // Ermittelt ob der mitgelieferte Dateipfad ein Backup-PDF ist (26.05.2023, SME)
        public static bool IsBackupPdfFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            if (!File.Exists(filePath)) return false;
            if (!filePath.ToLower().EndsWith(DotPDFBAK) && !filePath.ToLower().EndsWith(DotPdfBakZip)) return false;
            if (!Path.GetFileNameWithoutExtension(Path.GetDirectoryName(filePath)).Equals(PdfBackupFolderName)) return false;
            return true;
        }

        // Ermittelt ob der mitgelieferte Dateipfad ein Backup-PDF vom mitgelieferten Original-PDF ist (26.05.2023, SME)
        public static bool IsBackupPdfFileOf(string filePath, string originalFilePath)
        {
            // basic checks
            if (!IsBackupPdfFile(filePath)) return false;
            if (string.IsNullOrEmpty(originalFilePath)) return false;
            if (!File.Exists(originalFilePath)) return false;

            // store file-name without extension of original-file-path + backup-file-path
            var originalFileName = Path.GetFileNameWithoutExtension(originalFilePath);
            var backupFileName = Path.GetFileNameWithoutExtension(filePath);

            // further checks
            if (!backupFileName.StartsWith(originalFileName + DOT)) return false;

            // check counter
            var counter = backupFileName.Substring((originalFileName + DOT).Length);
            if (!CoreFC.IsNumeric(counter)) return false;
            if (counter.Contains(DOT)) return false;
            return true;
        }

        // Ermittelt den Zähler des Backup-Dateipfaded (26.05.2023, SME)
        public static int? GetCounterOfBackupFilePath(string filePath)
        {
            // exit-handling
            if (!IsBackupPdfFile(filePath)) return null;

            // store file-name without extension
            var backupFileName = Path.GetFileNameWithoutExtension(filePath);

            // get counter
            if (!backupFileName.Contains(DOT)) return null;
            var counter = backupFileName.Substring(backupFileName.LastIndexOf(DOT) + 1);
            if (!CoreFC.IsNumeric(counter)) return null;
            return int.Parse(counter);
        }

        // Ermittelt eine Liste der Backup-PDFs vom mitgelieferten Original-PDF sortiert nach Version (26.05.2023, SME)
        public static List<string> GetBackupFilePaths(string originalFilePath, bool returnEmptyListIfFileNotExists = true)
        {
            // init return-value
            var list = new List<string>();

            // exit-handling
            if (string.IsNullOrEmpty(originalFilePath)) return list;
            if (!File.Exists(originalFilePath) && returnEmptyListIfFileNotExists) return list;

            // set + check backup-folder-path
            var backupFolderPath = Path.Combine(Path.GetDirectoryName(originalFilePath), PdfBackupFolderName);
            if (!Directory.Exists(backupFolderPath)) return list;

            // get all backup-file-paths of original
            var search = Path.GetFileNameWithoutExtension(originalFilePath) + ".*" + DotPDFBAK;
            var backupFilePaths = Directory.GetFiles(backupFolderPath, search).Where(x => IsBackupPdfFileOf(x, originalFilePath)).ToList();

            // get all zip-backup-file-paths of original (13.06.2023, SME)
            var search2 = Path.GetFileNameWithoutExtension(originalFilePath) + ".*" + DotPdfBakZip;
            var backupFilePaths2 = Directory.GetFiles(backupFolderPath, search2).Where(x => IsBackupPdfFileOf(x, originalFilePath)).ToList();

            // combine the two lists
            if (backupFilePaths2.Any()) backupFilePaths.AddRange(backupFilePaths2);

            // return
            backupFilePaths.Sort();
            return backupFilePaths;
        }

        // Ermittelt den Dateipfad des Original-PDF anhand des mitgelieferten Backup-Dateipfades (26.05.2023, SME)
        public static string GetOriginalFilePath(string backupFilePath)
        {
            // exit-handling
            if (!IsBackupPdfFile(backupFilePath)) return string.Empty;

            // get + check counter
            var counter = GetCounterOfBackupFilePath(backupFilePath);
            if (!counter.HasValue) return string.Empty;

            // get original
            var fileName = Path.GetFileNameWithoutExtension(backupFilePath);
            var ending = DOT + counter.Value.ToString(ZweiNullen);
            if (!fileName.EndsWith(ending)) return string.Empty;
            fileName = fileName.Substring(0, fileName.Length - ending.Length) + DotPDF;
            return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(backupFilePath)), fileName);
        }

        // PDF-Version wieder herstellen (26.05.2023, SME)
        // => Restore
        public static bool RestorePDF(string backupFilePath, bool throwErrorOnFailure = true)
        {
            try
            {
                // exit-/error-handling
                if (string.IsNullOrEmpty(backupFilePath))
                {
                    if (!throwErrorOnFailure) return false;
                    throw new ArgumentNullException(nameof(backupFilePath));
                }
                if (!File.Exists(backupFilePath))
                {
                    if (!throwErrorOnFailure) return false;
                    throw new FileNotFoundException("Das Original kann nicht wieder hergestellt werden, weil die Backup-Datei nicht gefunden wurde!", backupFilePath);
                }
                if (!IsBackupPdfFile(backupFilePath))
                {
                    if (!throwErrorOnFailure) return false;
                    throw new ArgumentOutOfRangeException("Das Original kann nicht wieder hergestellt werden, weil die Backup-Datei ungültig ist!" + CoreFC.Lines(2) + "Backup-Dateipfad:" + CoreFC.Lines() + backupFilePath);
                }

                // store + check counter of backup-file
                var counter = GetCounterOfBackupFilePath(backupFilePath);
                if (!counter.HasValue)
                {
                    if (!throwErrorOnFailure) return false;
                    throw new ArgumentOutOfRangeException("Das Original kann nicht wieder hergestellt werden, weil der Zähler der Backup-Datei nicht ermittelt werden konnte!" + CoreFC.Lines(2)
                                                        + "Backup-Dateipfad:" + CoreFC.Lines() + backupFilePath);
                }

                // get + check file-path of original
                var originalFilePath = GetOriginalFilePath(backupFilePath);
                if (string.IsNullOrEmpty(originalFilePath))
                {
                    if (!throwErrorOnFailure) return false;
                    throw new ArgumentOutOfRangeException("Das Original kann nicht wieder hergestellt werden, weil der Original-Dateipfad nicht ermittelt werden konnte!" + CoreFC.Lines(2) + "Backup-Dateipfad:" + CoreFC.Lines() + backupFilePath);
                }
                if (File.Exists(originalFilePath))
                {
                    if (CoreFC.IsFileLocked_WaitMaxSeconds(originalFilePath))
                    {
                        if (!throwErrorOnFailure) return false;
                        throw new ArgumentOutOfRangeException("Das Original kann nicht wieder hergestellt werden, weil die Original-Dateipfad blockiert ist." + CoreFC.Lines(2)
                                                            + "Original-Dateipfad:" + CoreFC.Lines() + originalFilePath + CoreFC.Lines(2)
                                                            + "Backup-Dateipfad:" + CoreFC.Lines() + backupFilePath);
                    }
                }

                // restore original
                // => check if zip or normal backup (13.06.2023, SME)
                if (!backupFilePath.EndsWith(BakZip))
                {
                    // normal restore
                    if (File.Exists(originalFilePath)) File.Delete(originalFilePath);
                    File.Move(backupFilePath, originalFilePath);
                }
                else
                {
                    // restore from zip (13.06.2023, SME)
                    using (var zip = System.IO.Compression.ZipFile.Open(backupFilePath, System.IO.Compression.ZipArchiveMode.Read))
                    {
                        // get data
                        var dataEntry = zip.Entries.FirstOrDefault(x => x.Name.Equals(DataXml));
                        if (dataEntry == null) throw new Exception("Restore-Daten von Merge-ZIP konnten nicht gefunden werden!");
                        var data = new Data.PdfMergeInfo();
                        using (var stream = dataEntry.Open())
                        {
                            data.ReadXml(stream, System.Data.XmlReadMode.IgnoreSchema);
                        }
                        var table = data.PdfMergeInfoTable;
                        if (table.Rows.Count == 0) throw new Exception("Restore-Daten von Merge-ZIP sind leer!");

                        foreach (var entry in zip.Entries)
                        {
                            // Skip-Handling
                            if (entry.Name.Equals(DataXml)) continue;

                            // Guid extrahieren
                            string guid = entry.Name;
                            if (guid.EndsWith(DotPDF)) guid = guid.Substring(0, guid.Length - DotPDF.Length);   

                            // Datensatz ermitteln + prüfen
                            var row = table.FirstOrDefault(x => x.Guid.ToString().Equals(guid));
                            if (row == null) throw new Exception($"Restore-Daten von Merge-ZIP sind nicht korrekt: Eintrag '{entry.Name}' wurde nicht gefunden!");

                            // Pfad zwischenspeichern
                            var path = row.Path;

                            if (!File.Exists(path) || !CoreFC.IsFileLocked_WaitMaxSeconds(path))
                            {
                                // Sicherstellen, dass Datei nicht existiert
                                if (File.Exists(path)) File.Delete(path);

                                // Sicherstellen, dass übergeordneter Ordner existiert
                                var folder = Path.GetDirectoryName(path);
                                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                                // Datei entzippen
                                entry.ExtractToFile(path);
                            }
                            else
                            {
                                // TODO: Gesperrte Ziel-Dateien abhandeln (13.06.2023, SME)
                                Debugger.Break();
                            }
                        }
                    }
                }

                // delete this backup-file + all later versions
                var backupFilePaths = GetBackupFilePaths(originalFilePath);
                if (!backupFilePaths.Any())
                {
                    // WARNING: No Backup-Filepaths found
                    // => OK when first version restores
                    if (counter.Value == 0) return true;
                    // TODO: can be okay when first version has been deleted manually

                    // ERROR: No Backup-Filepaths found
                    if (!throwErrorOnFailure) return false;
                    throw new Exception("Das Original wurde wieder hergestellt, es wurden jedoch keine zu löschenden Backup-Dateien gefunden!" + CoreFC.Lines(2)
                                      + "Original-Dateipfad:" + CoreFC.Lines() + originalFilePath + CoreFC.Lines(2)
                                      + "Backup-Dateipfad:" + CoreFC.Lines() + backupFilePath);
                }
                else
                {
                    // loop throu backup-files (descending)
                    foreach (var bakFilePath in backupFilePaths.OrderByDescending(x => x).ToArray())
                    {
                        try
                        {
                            // get + check counter
                            var bakCounter = GetCounterOfBackupFilePath(bakFilePath);
                            if (!bakCounter.HasValue)
                            {
                                if (!throwErrorOnFailure) return false;
                                throw new Exception("Das Original wurde wieder hergestellt, folgende Backup-Datei konnte jedoch nicht gelöscht werden, weil der Zähler nicht ermittelt werden konnte!" + CoreFC.Lines(2)
                                                  + "Original-Dateipfad:" + CoreFC.Lines() + originalFilePath + CoreFC.Lines(2)
                                                  + "Backup-Dateipfad:" + CoreFC.Lines() + backupFilePath);
                            }
                            else if (bakCounter.Value < counter.Value)
                            {
                                // exit, da alle nötigen Backup-Dateien gelöscht wurden
                                break;
                            }
                            else
                            {
                                // Backup-Datei löschen
                                File.Delete(bakFilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            CoreFC.ThrowError(ex); throw ex;
                        }
                    }
                }

                // return
                return true;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // delete parent-folder if empty (08.06.2023, SME)
                CoreFC.DeleteEmptyFolder(System.IO.Path.GetDirectoryName(backupFilePath));
            }
        }

        #endregion

        #region Get References

        // Get all PDF-Objects that reference to given Object(s) by looping throu Pages (23.05.2023, SME)
        public static Dictionary<PdfObject, List<PdfObjectReference>> GetReferencesTo(PdfDocument document, params PdfObject[] referenceTo)
        {
            // declarations
            var found = new Dictionary<PdfObject, List<PdfObjectReference>>();
            var done = new List<PdfObject>();

            // exit-handling
            if (document == null) return found;
            if (referenceTo == null) return found;
            if (!referenceTo.Any()) return found;

            // loop throu pages
            for (int iPage = 1; iPage <= document.GetNumberOfPages(); iPage++)
            {
                // set + page
                var page = document.GetPage(iPage);
                if (page == null) continue;

                // collect in page
                CollectReferencesTo(page.GetPdfObject(), referenceTo, found, done, null, iPage, "Page_" + iPage.ToString());
            }

            // return
            return found;
        }

        // Sammelt die Referenzen
        private static void CollectReferencesTo(PdfObject searchIn, PdfObject[] referenceTo, Dictionary<PdfObject, List<PdfObjectReference>> found, List<PdfObject> done, PdfObject parent, int pageNumber, string path)
        {
            // exit-handling
            if (searchIn == null) return;
            if (done.Contains(searchIn)) return;

            // add to done-list
            done.Add(searchIn);

            // loop throu references to find/add matches
            foreach (var refObject in referenceTo)
            {
                // compare
                if (searchIn.Equals(refObject))
                {
                    // MATCH => add to list
                    if (!found.ContainsKey(refObject)) found.Add(refObject, new List<PdfObjectReference>());
                    found[refObject].Add(new PdfObjectReference(refObject, pageNumber, searchIn, parent, path));
                }
            }

            // recursive search
            if (searchIn is PdfDictionary)
            {
                // store dictionary
                var dic = (PdfDictionary)searchIn;

                // loop throu keys
                foreach (var key in dic.KeySet())
                {
                    // skip parent to avoid overflow
                    if (PdfName.Parent.Equals(key)) continue;

                    // get value by key
                    var value = dic.Get(key);

                    // recursive call
                    CollectReferencesTo(value, referenceTo, found, done, searchIn, pageNumber, path + @"\" + key.ToString().Substring(1));
                }
            }
            else if (searchIn is PdfArray)
            {
                // store array
                var arr = (PdfArray)searchIn;
                int iItem = 1;
                // loop throu items
                foreach (var item in arr)
                {
                    // recursive call
                    CollectReferencesTo(item, referenceTo, found, done, searchIn, pageNumber, path + @"\" + "Item_" + iItem++);
                }
            }
        }

        #endregion

        #region PDF-Object-Type-Enum

        // INTERNAL: Ermittelt die Enumeration des PDF-Object-Typen (26.05.2023, SME)
        internal static PdfObjectTypeEnum? GetPdfObjectType(PdfObject pdfObject)
        {
            if (pdfObject == null) return null;
            if (pdfObject is PdfArray) return PdfObjectTypeEnum.PdfArray;
            if (pdfObject is PdfBoolean) return PdfObjectTypeEnum.PdfBoolean;
            if (pdfObject is PdfStream) return PdfObjectTypeEnum.PdfStream;
            if (pdfObject is PdfDictionary) return PdfObjectTypeEnum.PdfDictionary;
            if (pdfObject is PdfName) return PdfObjectTypeEnum.PdfName;
            if (pdfObject is PdfNumber) return PdfObjectTypeEnum.PdfNumber;
            if (pdfObject is PdfString) return PdfObjectTypeEnum.PdfString;
            return PdfObjectTypeEnum.Unknown;
        }

        // Ermittelt das Bild der PDF-Objekt-Typen-Enumeration (26.05.2023, SME)
        public static System.Drawing.Image GetPdfObjectTypeImage(PdfObjectTypeEnum type)
        {
            switch (type)
            {
                case PdfObjectTypeEnum.PdfArray:
                    return Properties.Resources.PdfArray;
                case PdfObjectTypeEnum.PdfBoolean:
                    return Properties.Resources.PdfBoolean;
                case PdfObjectTypeEnum.PdfDictionary:
                    return Properties.Resources.PdfDictionary;
                case PdfObjectTypeEnum.PdfName:
                    return Properties.Resources.PdfName;
                case PdfObjectTypeEnum.PdfNumber:
                    return Properties.Resources.PdfNumber;
                case PdfObjectTypeEnum.PdfStream:
                    return Properties.Resources.PdfStream;
                case PdfObjectTypeEnum.PdfString:
                    return Properties.Resources.PdfString;
                case PdfObjectTypeEnum.Unknown:
                    return Properties.Resources.Unknown;
                default:
                    return Properties.Resources.Unknown;
            }
        }

        // Ermittelt den Image-Key der PDF-Object-Typen-Enumeration (26.05.2023, SME)
        public static string GetPdfObjectTypeImageKey(PdfObjectTypeEnum? type)
        {
            if (!type.HasValue) return "Unknown";
            else return type.Value.ToString();
        }

        // Ermittelt den Image-Key des PDF-Objects (26.05.2023, SME)
        public static string GetPdfObjectTypeImageKey(PdfObject pdfObject)
        {
            if (pdfObject == null) return "Unknown";
            var type = GetPdfObjectType(pdfObject);
            if (!type.HasValue) return "Unknown";
            return GetPdfObjectTypeImageKey(type.Value);
        }

        #endregion

        #region Merge

        // PDFs zusammenführen (10.05.2023, SME)
        // CHANGE: separatorPdfFilePath added as parameter (02.06.2023, SME)
        public static int MergeFiles(List<string> pdfFilePathsToMerge, string outputPdfFilePath, bool overwrite = false, ProgressInfo progressInfo = null, string separatorPdfFilePath = "", bool deleteOriginals = false)
        {
            // Error-Handling
            if (pdfFilePathsToMerge == null) return 0;
            if (!pdfFilePathsToMerge.Any()) return 0;
            if (string.IsNullOrEmpty(outputPdfFilePath)) throw new ArgumentNullException(nameof(outputPdfFilePath));
            if (File.Exists(outputPdfFilePath) && !overwrite) throw new Exception("Ziel-Datei für PDF-Merge existiert bereits und Überschreiben-Flag ist nicht gesetzt!" + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + outputPdfFilePath);
            if (!string.IsNullOrEmpty(separatorPdfFilePath) && !File.Exists(separatorPdfFilePath)) throw new Exception("Die PDF-Datei der Zwischenseiten wurde nicht gefunden!" + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + separatorPdfFilePath);

            try
            {
                // Zwischenseiten-PDF laden (02.06.2023, SME)
                if (!string.IsNullOrEmpty(separatorPdfFilePath))
                {
                    if (pdfFilePathsToMerge.Contains(separatorPdfFilePath))
                    {
                        throw new Exception("Das Zwischenseiten-PDF ist Bestandteil der Merge-Liste!");
                    }
                }

                // PDFs sammeln
                progressInfo?.SetStatus("Zu mergende PDF-Dateien werden geprüft ...");
                var pdfFiles = new List<PdfFile>();
                foreach (var filePath in pdfFilePathsToMerge)
                {
                    var pdf = new PdfFile(filePath);
                    pdfFiles.Add(pdf);
                }

                // Andere Methode aufrufen und Rückgabe
                return MergeFiles(pdfFiles, outputPdfFilePath, overwrite, progressInfo, separatorPdfFilePath, deleteOriginals);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // PDFs zusammenführen mit Liste der PDF-Dateien (19.06.2023, SME)
        public static int MergeFiles(List<PdfFile> pdfFilesToMerge, string outputPdfFilePath, bool overwrite = false, ProgressInfo progressInfo = null, string separatorPdfFilePath = "", bool deleteOriginals = false)
        {
            // Error-Handling
            if (pdfFilesToMerge == null) return 0;
            if (!pdfFilesToMerge.Any()) return 0;
            if (string.IsNullOrEmpty(outputPdfFilePath)) throw new ArgumentNullException(nameof(outputPdfFilePath));
            if (File.Exists(outputPdfFilePath) && !overwrite) throw new Exception("Ziel-Datei für PDF-Merge existiert bereits und Überschreiben-Flag ist nicht gesetzt!" + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + outputPdfFilePath);
            if (!string.IsNullOrEmpty(separatorPdfFilePath) && !File.Exists(separatorPdfFilePath)) throw new Exception("Die PDF-Datei der Zwischenseiten wurde nicht gefunden!" + CoreFC.Lines(2) + "Pfad:" + CoreFC.Lines() + separatorPdfFilePath);

            // Deklarationen
            int totalPages = 0;
            PdfFile separatorPdf = null;
            iPDF.PdfReader separatorPdfReader = null;
            iPDF.PdfDocument separatorPdfDocument = null;
            int separatorPdfPageCount = 0;
            PdfMerger merger = null;

            try
            {
                // Zwischenseiten-PDF laden (02.06.2023, SME)
                if (!string.IsNullOrEmpty(separatorPdfFilePath))
                {
                    progressInfo?.SetStatus("Zwischenseiten-PDF wird geprüft ...");
                    if (pdfFilesToMerge.Any(x => x.FilePath.Equals(separatorPdfFilePath)))
                    {
                        throw new Exception("Das Zwischenseiten-PDF ist Bestandteil der Merge-Liste!");
                    }
                    separatorPdf = new PdfFile(separatorPdfFilePath);
                    separatorPdf.Compress();
                    separatorPdfReader = new iPDF.PdfReader(separatorPdf.FilePath);
                    separatorPdfDocument = new iPDF.PdfDocument(separatorPdfReader);
                    separatorPdfPageCount = separatorPdfDocument.GetNumberOfPages();
                }

                // PDFs erst einmal einlesen um Anzahl Seiten zu berechnen für die Progress-Info und Abflug falls ein Fehler passiert
                progressInfo?.SetStatus("Zu mergende PDF-Dateien werden geprüft ...");
                var pdfsWithErrors = pdfFilesToMerge.Where(x => x.InitializeError != null).ToList();
                if (pdfsWithErrors.Any())
                {
                    if (pdfsWithErrors.Count == 1)
                    {
                        throw new Exception("PDFs können nicht zusammengeführt werden, da in einer Datei ein Fehler aufgetreten ist!" + CoreFC.Lines(2)
                                          + "Dateipfad:" + CoreFC.Lines() + pdfsWithErrors.First().FileName + CoreFC.Lines(2)
                                          + "Fehlermeldung:" + CoreFC.Lines() + pdfsWithErrors.First().InitializeError.Message);
                    }
                    else if (pdfsWithErrors.Count <= 10)
                    {
                        throw new Exception($"PDFs können nicht zusammengeführt werden, da in {pdfsWithErrors.Count} Dateien Fehler aufgetreten sind!" + CoreFC.Lines(2)
                                           + "Dateipfade:" + CoreFC.Lines() + "- " + string.Join(CoreFC.Lines() + "- ", pdfsWithErrors.Select(x => x.FileName)));
                    }
                    else
                    {
                        throw new Exception($"PDFs können nicht zusammengeführt werden, da in {pdfsWithErrors.Count} Dateien Fehler aufgetreten sind!" + CoreFC.Lines(2)
                                           + "Erste 10 Dateipfade:" + CoreFC.Lines() + "- " + string.Join(CoreFC.Lines() + "- ", pdfsWithErrors.Take(10).Select(x => x.FileName)));
                    }
                }

                // Alle PDFs backupen (Fehler wird ausgelöst falls blockiert)
                // CHANGE: PDFs in eine ZIP-Datei schreiben (13.06.2023, SME)
                progressInfo?.SetStatus("Original-PDFs werden gesichert ...");
                BackupMergePdfsToZip(outputPdfFilePath, pdfFilesToMerge.Select(x => x.FilePath).ToList());
                //var inputPdfFilePaths = new List<string>();
                //foreach (var pdf in pdfFilePathsToMerge)
                //{
                //    var path = MoveFileToOriginalPdfFolder(pdf);
                //    inputPdfFilePaths.Add(path);
                //}

                // Progress starten
                progressInfo?.SetTotalSteps(pdfFilesToMerge.Sum(x => x.PageCount));

                // ONLY TEMPORARY (29.05.2024, SME)
                //Debugger.Break();
                //int result = ClsPDF.MergeFiles(pdfFilesToMerge.Select(x => x.FilePath).ToList(), outputPdfFilePath);
                //if (result > 0) return result;
                //else if (result == 0) return 0;
                //else Debugger.Break();

                // Writer erstellen
                using (var writer = new FullCompressionPdfWriter(outputPdfFilePath))
                {
                    // Output-PDF erstellen
                    using (var targetPDF = new iPDF.PdfDocument(writer))
                    {
                        // Merger erstellen (19.10.2023, SME)
                        merger = new PdfMerger(targetPDF);

                        // Loop durch Input-PDFs
                        foreach (var inputPdfFile in pdfFilesToMerge)
                        {
                            // Progress-Status aktualisieren
                            progressInfo?.SetStatus($"{inputPdfFile.FileName} wird gemergt ...");

                            // Reader öffnen
                            using (var reader = new iPDF.PdfReader(inputPdfFile.FilePath))
                            {
                                // Input-PDF öffnen
                                using (var sourcePDF = new iPDF.PdfDocument(reader))
                                {
                                    // Page-Count zwischenspeichern
                                    var pageCount = sourcePDF.GetNumberOfPages();
                                    totalPages += pageCount;

                                    // Loop durch Seiten
                                    for (int iPage = 1; iPage <= pageCount; iPage++)
                                    {
                                        try
                                        {
                                            // Seite kopieren
                                            //merger.Merge(sourcePDF, iPage, iPage);
                                            sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                            // Ressourcen freigeben alle 100 Seiten
                                            if (iPage % 100 == 0) targetPDF.FlushCopiedObjects(sourcePDF);

                                            // perform step 
                                            progressInfo?.PerformStep();
                                        }
                                        catch (Exception ex)
                                        {
                                            CoreFC.ThrowError(ex); throw ex;
                                        }
                                    }

                                    // Ressourcen freigeben
                                    targetPDF.FlushCopiedObjects(sourcePDF);

                                    // Zwischenseiten einfügen (02.06.2023, SME)
                                    if (separatorPdfDocument != null && separatorPdfPageCount > 0)
                                    {
                                        try
                                        {
                                            separatorPdfDocument.CopyPagesTo(1, separatorPdfPageCount, targetPDF);
                                            targetPDF.FlushCopiedObjects(separatorPdfDocument);
                                        }
                                        catch (Exception ex)
                                        {
                                            CoreFC.ThrowError(ex); throw ex;
                                        }
                                    }
                                } // next page

                            } // pdf-reader

                            // release memory
                            GC.Collect();

                        } // next input-pdf

                        // Merger schliessen
                        merger.Close();
                        merger = null;

                    } // target-pdf-document

                } // pdf-writer

                // Originale der zu mergenden PDFs löschen
                // => Flag abfragen (15.06.2023, SME)
                if (deleteOriginals)
                {
                    foreach (var fileToDelete in pdfFilesToMerge)
                    {
                        try
                        {
                            System.IO.File.Delete(fileToDelete.FilePath);
                            fileToDelete.Dispose();
                        }
                        catch (Exception ex)
                        {
                            CoreFC.DPrint("ERROR while deleting merged pdf: " + ex.Message);
                        }
                    }
                }

                // return Anzahl gemergte Seiten
                return totalPages;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // close separator-pdf (02.06.2023, SME)
                if (separatorPdfDocument != null) separatorPdfDocument.Close();
                if (separatorPdfReader != null) separatorPdfReader.Close();

                // close merger (19.10.2023, SME)
                merger?.Close(); merger = null;

                // release memory
                GC.Collect();
            }
        }

        #endregion

        // Millimeters to Points
        public static float MillimetersToPoints(double mm)
        {
            return float.Parse(mm.ToString()) * 2.8346456693F;
        }

        // Get PDF-Name-String (26.06.2023, SME)
        internal static string GetPdfNameString(PdfName pdfName, string nullString = EmptyString)
        {
            if (pdfName == null) return nullString;
            return pdfName.ToString().Substring(1);
        }

        // Can have Child-Objects (26.06.2023, SME)
        internal static bool CanHaveChildObjects(PdfObject value)
        {
            if (value == null) return false;
            if (value is PdfDictionary) return true;
            if (value is PdfArray) return true;
            return false;
        }

        // Get Value-String of PDF-Object (04.04.2023, SME)
        // copied from frmPdfStructure (26.06.2023, SME)
        internal static string GetPdfObjectValueString(PdfObject value, bool includeCountChildren = true)
        {
            if (value == null) return NullString;
            else if (value is PdfName) return GetPdfNameString((PdfName)value);
            else if (value is PdfString) return value.ToString();
            else if (value is PdfNumber) return value.ToString();
            else if (value is PdfBoolean) return value.ToString();
            else if (value is PdfDictionary)
            {
                var dic = (PdfDictionary)value;
                PdfName type = null;
                if (dic.ContainsKey(PdfName.Type)) type = dic.GetAsName(PdfName.Type);
                if (!includeCountChildren)
                {
                    var count = dic.KeySet().Count;
                    if (type != null) return GetPdfNameString(type);
                    else return value.GetType().Name;
                }
                else
                {
                    var count = dic.KeySet().Count;
                    if (type != null) return GetPdfNameString(type) + $" (# {count})";
                    else return value.GetType().Name + $" (# {count})";
                }

            }
            else if (value is PdfArray && includeCountChildren)
            {
                var arr = (PdfArray)value;
                var count = arr.Count();
                return value.GetType().Name + $" (# {count})";
            }
            else return value.GetType().Name + ": " + value.ToString();
        }

        #endregion

        #region Auto-Optimize PDFs

        // Konstanten für Auto-Optimierung (26.06.2023, SME)
        private const string PdfOptimizerFolderName = "PDF_Optimizer";
        private const string SettingName_AutoOptimizePdfOption = "AutoOptimizePdfOption";
        private const string SettingName_AutoOptimizeWaitMaxSeconds = "AutoOptimizeWaitMaxSeconds";
        private const string SettingName_AutoOptimizeWriteWarningsForUnembeddedType3Fonts = "AutoOptimizeWriteWarningsForUnembeddedType3Fonts";
        private const string SettingName_AutoOptimizeWriteToDosForNewlyEmbeddedFonts = "AutoOptimizeWriteToDosForNewlyEmbeddedFonts";
        private const bool DefaultValue_AutoOptimizeWriteWarningsForUnembeddedType3Fonts = true;
        private const bool DefaultValue_AutoOptimizeWriteToDosForNewlyEmbeddedFonts = true;

        // Auto-Optimize PDFs (26.06.2023, SME)
        public static void AutoOptimizePDFs()
        {
            // Deklarationen
            PdfFile pdf = null;
            PdfFileActionResult result;
            Stopwatch sw = null;
            bool keepBackup = true;
            string targetFilePath;
            PdfAutoOptimizeOptionEnum? autoOptimizeOption = null;
            int waitMaxSeconds = 5;
            PdfFileFontInfoResult fontInfos = null;
            bool ownError = false;
            bool writeWarningsForUnembeddedType3Fonts = DefaultValue_AutoOptimizeWriteWarningsForUnembeddedType3Fonts;
            bool writeToDosForNewlyEmbeddedFonts = DefaultValue_AutoOptimizeWriteToDosForNewlyEmbeddedFonts;
            bool boolValue;

            try
            {
                #region Ordner festlegen + erstellen

                // get root-folder-path
                var rootFolderPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                if (System.IO.Path.GetFileName(rootFolderPath) != "Debug")
                {
                    rootFolderPath = System.IO.Path.GetDirectoryName(rootFolderPath);
                }
                rootFolderPath = System.IO.Path.Combine(rootFolderPath, PdfOptimizerFolderName);
                if (!Directory.Exists(rootFolderPath)) Directory.CreateDirectory(rootFolderPath);

                // input-folder
                string inputFolderPath = Path.Combine(rootFolderPath, InputFolderName);
                if (!Directory.Exists(inputFolderPath)) Directory.CreateDirectory(inputFolderPath);

                // quarantäne-folder
                string quarantaeneFolderPath = Path.Combine(rootFolderPath, QuarantaeneFolderName);
                if (!Directory.Exists(quarantaeneFolderPath)) Directory.CreateDirectory(quarantaeneFolderPath);

                // todo-folder
                string toDoFolderPath = Path.Combine(rootFolderPath, ToDoFolderName);
                if (!Directory.Exists(toDoFolderPath)) Directory.CreateDirectory(toDoFolderPath);

                // output-folder
                string outputFolderPath = Path.Combine(rootFolderPath, OutputFolderName);
                if (!Directory.Exists(outputFolderPath)) Directory.CreateDirectory(outputFolderPath);

                #endregion

                #region PDFs ermitteln

                // alle PDFs von Input-Ordner ermitteln
                // => und allen Unterordnern (07.08.2023, SME)
                // => jedoch nur die direkten Unterordner (10.07.2024, SME)
                var pdfFilePaths = Directory.GetFiles(inputFolderPath, StarDotPDF, SearchOption.TopDirectoryOnly).ToList();
                foreach (var subDirectoryPDFs in Directory.GetDirectories(inputFolderPath).SelectMany(x => Directory.GetFiles(x, StarDotPDF, SearchOption.TopDirectoryOnly)))
                {
                    pdfFilePaths.Add(subDirectoryPDFs);
                }
                if (!pdfFilePaths.Any()) return;

                #endregion

                #region Einstellungen setzen

                // Auto-Optimize-Option ermitteln (05.07.2023, SME)
                var settingValue = Global_TC_Core.GlobalAppSettings.GetSettingValue(SettingName_AutoOptimizePdfOption);
                if (string.IsNullOrEmpty(settingValue))
                {
                    // set default
                    autoOptimizeOption = PdfAutoOptimizeOptionEnum.Optimize;
                    Global_TC_Core.GlobalAppSettings.SetSettingValue(SettingName_AutoOptimizePdfOption, autoOptimizeOption.Value.ToString(), true);
                }
                else
                {
                    autoOptimizeOption = CoreFC.GetEnumValue<PdfAutoOptimizeOptionEnum>(settingValue);
                    if (!autoOptimizeOption.HasValue)
                    {
                        LogFC.WriteError(new Exception($"Auto-PDF-Optimierungs-Option konnte nicht ermittelt werden von '{settingValue}'! Die Option '{PdfAutoOptimizeOptionEnum.OnlyMoveToOutput}' wird stattdessen verwendet."), PdfOptimizerFolderName);
                        autoOptimizeOption = PdfAutoOptimizeOptionEnum.OnlyMoveToOutput;
                    }
                }

                // Auto-Optimize-WaitMaxSeconds ermitteln (14.12.2023, SME)
                settingValue = Global_TC_Core.GlobalAppSettings.GetSettingValue(SettingName_AutoOptimizeWaitMaxSeconds);
                if (string.IsNullOrEmpty(settingValue) || !CoreFC.IsNumeric(settingValue))
                {
                    // set default
                    waitMaxSeconds = 5;
                    Global_TC_Core.GlobalAppSettings.SetSettingValue(SettingName_AutoOptimizeWaitMaxSeconds, waitMaxSeconds.ToString(), true);
                }
                else
                {
                    waitMaxSeconds = int.Parse(settingValue);
                }

                // Write Warnings for unembedded Type3-Fonts ermitteln (05.07.2024, SME)
                settingValue = Global_TC_Core.GlobalAppSettings.GetSettingValue(SettingName_AutoOptimizeWriteWarningsForUnembeddedType3Fonts);
                if (string.IsNullOrEmpty(settingValue))
                {
                    // write default-setting to app-settings
                    Global_TC_Core.GlobalAppSettings.SetSettingValue(SettingName_AutoOptimizeWriteWarningsForUnembeddedType3Fonts, writeWarningsForUnembeddedType3Fonts.ToString(), true);
                }
                else if (bool.TryParse(settingValue, out boolValue))
                {
                    // apply value
                    if (writeWarningsForUnembeddedType3Fonts != boolValue)
                    {
                        writeWarningsForUnembeddedType3Fonts = boolValue;
                    }
                }
                else
                {
                    // invalid value => write error
                    LogFC.WriteError(new Exception($"Wert '{settingValue}' für Einstellung '{SettingName_AutoOptimizeWriteWarningsForUnembeddedType3Fonts}' konnte nicht als Boolean interpretiert werden! Der Standardwert '{writeWarningsForUnembeddedType3Fonts}' wird verwendet."), PdfOptimizerFolderName);
                }

                // Write ToDo's for newly-embedded Fonts ermitteln (05.07.2024, SME)
                settingValue = Global_TC_Core.GlobalAppSettings.GetSettingValue(SettingName_AutoOptimizeWriteToDosForNewlyEmbeddedFonts);
                if (string.IsNullOrEmpty(settingValue))
                {
                    // write default-setting to app-settings
                    Global_TC_Core.GlobalAppSettings.SetSettingValue(SettingName_AutoOptimizeWriteToDosForNewlyEmbeddedFonts, writeToDosForNewlyEmbeddedFonts.ToString(), true);
                }
                else if (bool.TryParse(settingValue, out boolValue))
                {
                    // apply value
                    if (writeToDosForNewlyEmbeddedFonts != boolValue)
                    {
                        writeToDosForNewlyEmbeddedFonts = boolValue;
                    }
                }
                else
                {
                    // invalid value => write error
                    LogFC.WriteError(new Exception($"Wert '{settingValue}' für Einstellung '{SettingName_AutoOptimizeWriteToDosForNewlyEmbeddedFonts}' konnte nicht als Boolean interpretiert werden! Der Standardwert '{writeToDosForNewlyEmbeddedFonts}' wird verwendet."), PdfOptimizerFolderName);
                }

                #endregion

                // set font-table
                FontDataDB.FontsDataTable fontTable = null;
                if (autoOptimizeOption.Value == PdfAutoOptimizeOptionEnum.Optimize)
                {

                    // update connection-string of fonts-db
                    FC_Fonts.UpdateConnectionString();
                    
                    // set font-table only then optimizing (05.07.2023, SME)
                    fontTable = FC_Fonts.GetFontsTable();
                }

                // Loop durch PDF-Dateipfade
                foreach (var pdfFilePath in pdfFilePaths.OrderBy(x => x))
                {
                    try
                    {
                        // skip-handling (13.12.2023, SME)
                        if (CoreFC.IsFileLocked_WaitMaxSeconds(pdfFilePath, waitMaxSeconds))
                        {
                            LogFC.WriteLog($"Datei wird ausgelassen, weil sie scheint blockiert zu sein. Pfad = {pdfFilePath}");
                            continue;
                        }

                        // Stopuhr starten
                        sw = Stopwatch.StartNew();

                        // Pfad-Infos zwischenspeichern
                        string pdfFileName = Path.GetFileName(pdfFilePath);
                        string pdfClientFolderPart = Path.GetDirectoryName(pdfFilePath).Substring(inputFolderPath.Length);
                        string pdfFileInfo = Path.Combine(pdfClientFolderPart, pdfFileName);
                        pdfClientFolderPart = pdfClientFolderPart.Substring(1);

                        // Log-Info
                        LogFC.WriteLog(CoreFC.XString(Equal, 100), PdfOptimizerFolderName);
                        switch (autoOptimizeOption.Value)
                        {
                            case PdfAutoOptimizeOptionEnum.OnlyMoveToOutput:
                                LogFC.WriteLog($"'...{pdfFileInfo}' wird nur verschoben ...", PdfOptimizerFolderName);
                                break;
                            case PdfAutoOptimizeOptionEnum.OnlyCompress:
                                LogFC.WriteLog($"'...{pdfFileInfo}' wird komprimiert ...", PdfOptimizerFolderName);
                                break;
                            case PdfAutoOptimizeOptionEnum.Optimize:
                                LogFC.WriteLog($"'...{pdfFileInfo}' wird optimiert ...", PdfOptimizerFolderName);
                                break;
                            default:
                                throw new NotImplementedException($"Die Option '{autoOptimizeOption}' wurde nicht behandelt!");
                        }

                        // OnlyMove abhandeln (05.07.2023, SME)
                        if (autoOptimizeOption.Value == PdfAutoOptimizeOptionEnum.OnlyMoveToOutput)
                        {
                            targetFilePath = Path.Combine(outputFolderPath, pdfClientFolderPart, pdfFileName);
                            CoreFC.MoveFile(pdfFilePath, targetFilePath);
                            continue;
                        }

                        // PDF-File erstellen
                        pdf = new PdfFile(pdfFilePath);

                        // Auto-Optimize-Option abhandeln
                        switch (autoOptimizeOption.Value)
                        {
                            case PdfAutoOptimizeOptionEnum.OnlyCompress:
                                // => PDF komprimieren
                                keepBackup = false;
                                result = pdf.Compress(null, keepBackup);
                                break;
                            case PdfAutoOptimizeOptionEnum.Optimize:
                                // Font-Infos von nicht eingebetteten Schriften ermitteln + abhandeln
                                fontInfos = pdf.GetFonts(null, true, true);
                                if (CoreFC.IsFileLocked_WaitMaxSeconds(pdfFilePath, waitMaxSeconds))
                                {
                                    LogFC.WriteLog($"Datei wird ausgelassen, weil sie scheint blockiert zu sein. Pfad = {pdfFilePath}");
                                    continue;
                                }
                                if (fontInfos.HasErrors)
                                {
                                    // FEHLER beim Ermitteln der nicht eingebetteten Schriften
                                    var errorMsg = $"Beim Ermitteln der nicht eingebetteten Schriften von '{Path.GetFileName(pdfFilePath)}' sind folgende Fehler aufgetreten:" + CoreFC.Lines(2) + string.Join(CoreFC.Lines(2), fontInfos.Errors.Select(x => x.Message));
                                    throw new Exception(errorMsg);
                                }
                                else if (!fontInfos.FontUsage.Any())
                                {
                                    // Keine nicht-eingebetteten Schriften
                                    // => PDF komprimieren
                                    keepBackup = false;
                                    result = pdf.Compress(null, keepBackup);
                                }
                                else
                                {
                                    // Schriften einbetten + PDF optimieren
                                    keepBackup = true;
                                    result = pdf.Optimize(null, false, true, fontTable, keepBackup);
                                }
                                break;
                            default:
                                throw new NotImplementedException($"Auto-Optimierungs-Option '{autoOptimizeOption.Value}' ist nicht implementiert!");
                        }

                        // Result-Fehler prüfen
                        if (result.HasErrors)
                        {
                            // FEHLER beim Optimieren des PDFs
                            ownError = true;
                            if (result.Errors.Length == 1)
                            {
                                var errorMsg = $"Beim Optimieren von '{Path.GetFileName(pdfFilePath)}' ist folgender Fehler aufgetreten:" + CoreFC.Lines(2) + result.Errors.First().Message;
                                throw new Exception(errorMsg, CoreFC.GetSourceError(result.Errors.First()));
                            }
                            else
                            {
                                var errorMsg = $"Beim Optimieren von '{Path.GetFileName(pdfFilePath)}' sind {result.Errors.Length:n0} Fehler aufgetreten!";
                                throw CoreFC.GetMultiError(errorMsg, result.Errors);
                            }
                        }

                        // => PDF freigeben
                        pdf.Dispose();

                        // PDF verschieben
                        if (fontInfos == null || !fontInfos.FontUsage.Any())
                        {
                            // PDF wurde nur komprimiert, keine Schriften eingebettet
                            // => direkt in Output-Ordner verschieben
                            targetFilePath = Path.Combine(outputFolderPath, pdfClientFolderPart, pdfFileName);
                            CoreFC.MoveFile(pdfFilePath, targetFilePath);
                        }
                        else
                        {
                            // Schriften wurden eingebettet
                            List<TcPdfFont> notEmbeddedFonts = new List<TcPdfFont>();

                            // Log-Infos
                            var optimizeResult = result as PdfFileOptimizationResult;
                            if (optimizeResult != null)
                            {
                                // Eingebettete Schriften
                                if (optimizeResult.NewlyEmbeddedFonts.Any())
                                {
                                    LogFC.WriteLog($"- Folgende Schriften wurden eingebettet:", PdfOptimizerFolderName);
                                    foreach (var embeddedFont in optimizeResult.NewlyEmbeddedFonts)
                                    {
                                        LogFC.WriteLog($"  - {embeddedFont.FontName}", PdfOptimizerFolderName);
                                    }
                                }

                                // Schriften welche nicht eingebettet werden konnten oder wurden
                                notEmbeddedFonts = optimizeResult.StillUnembeddedFonts.ToList();
                                foreach (var notEmbeddedFontAtStart in fontInfos.FontUsage)
                                {
                                    if (optimizeResult.NewlyEmbeddedFonts.Any(x => x.ToFontString().Equals(notEmbeddedFontAtStart.Font.ToFontString())))
                                    {
                                        // Weiterfahren, weil Schrift eingebettet werden konnte
                                        continue;
                                    }
                                    else if (notEmbeddedFonts.Any(x => x.ToFontString().Equals(notEmbeddedFontAtStart.Font.ToFontString())))
                                    {
                                        // Weiterfahren, weil Schrift bereits in Liste der nicht eingebetteten Schriften ist
                                        continue;
                                    }
                                    else
                                    {
                                        // Weitere nicht-eingebettete Schrift hinzufügen
                                        notEmbeddedFonts.Add(notEmbeddedFontAtStart.Font);
                                    }
                                }
                                if (notEmbeddedFonts.Any())
                                {
                                    LogFC.WriteLog($"- Folgende Schriften konnten NICHT eingebettet werden:", PdfOptimizerFolderName);
                                    foreach (var font in notEmbeddedFonts)
                                    {
                                        LogFC.WriteLog($"  - {font.FontName}", PdfOptimizerFolderName);
                                    }
                                }
                            }

                            #region ToDo's

                            // => ToDo's erstellen

                            // Daten erstellen
                            var todo = new PdfToDos();

                            // Voraussichtlicher Zielpfad definieren
                            targetFilePath = Path.Combine(toDoFolderPath, pdfClientFolderPart, pdfFileName);

                            // PDF-Eintrag erstellen
                            var pdfRow = todo.PDFs.NewPDFsRow();
                            pdfRow.FilePath = targetFilePath;
                            pdfRow.FileName = Path.GetFileName(targetFilePath);
                            pdfRow.Table.Rows.Add(pdfRow);

                            // ToDo's erstellen
                            var now = DateTime.Now;
                            // => ToDo von eingebetteten Schriften hinzufügen
                            if (optimizeResult != null)
                            {
                                // ToDo's NUR erstellen, falls gewünscht (05.07.2024, SME)
                                if (writeToDosForNewlyEmbeddedFonts)
                                {
                                    foreach (var font in optimizeResult.NewlyEmbeddedFonts)
                                    {
                                        // Font-Usage ermitteln
                                        var fontUsage = fontInfos.FontUsage.FirstOrDefault(x => x.Font.ToFontString().Equals(font.ToFontString()));

                                        // ToDo-Eintrag erstellen
                                        var toDoRow = todo.ToDos.NewToDosRow();
                                        toDoRow.PDF_ID = pdfRow.ID;
                                        toDoRow.AddedOn = now;
                                        toDoRow.ToDoTypeEnum = PdfToDoTypeEnum.ToDo;
                                        toDoRow.ToDoInfo = $"Einbettung von '{font.FontName}' ({font.SubType}) prüfen";
                                        toDoRow.ToDoInfoDetails = string.Format("Action={0};FontName={1};FontSubType={2}", 
                                                                                "Embedded", font.FontName, font.SubType);
                                        toDoRow.Table.Rows.Add(toDoRow);

                                        if (fontUsage != null)
                                        {
                                            // add pages (03.07.2023, SME)
                                            foreach (var page in fontUsage.Pages.Distinct().OrderBy(x => x))
                                            {
                                                var pageRow = todo.ToDoPages.NewToDoPagesRow();
                                                pageRow.ToDo_ID = toDoRow.ID;
                                                pageRow.Page = page;
                                                pageRow.Table.Rows.Add(pageRow);
                                            }
                                        }
                                    }
                                }
                            }
                            // => ToDo von Schriften, welche nicht eingebettet werden konnten hinzufügen
                            if (notEmbeddedFonts.Any())
                            {
                                foreach (var font in notEmbeddedFonts)
                                {
                                    // Type3-Fonts auslassen falls gewünscht (05.07.2024, SME)
                                    if (!writeWarningsForUnembeddedType3Fonts && !string.IsNullOrEmpty(font.SubType) && font.SubType.ToUpper() == "TYPE3")
                                    {
                                        continue;
                                    }

                                    // Font-Usage ermitteln
                                    var fontUsage = fontInfos.FontUsage.FirstOrDefault(x => x.Font.ToFontString().Equals(font.ToFontString()));

                                    // ToDo-Eintrag erstellen
                                    var toDoRow = todo.ToDos.NewToDosRow();
                                    toDoRow.PDF_ID = pdfRow.ID;
                                    toDoRow.AddedOn = now;
                                    toDoRow.ToDoTypeEnum = PdfToDoTypeEnum.Warning;
                                    toDoRow.ToDoInfo = $"'{font.FontName}' ({font.SubType}) konnte nicht eingebettet";
                                    toDoRow.ToDoInfoDetails = string.Format("Action={0};FontName={1};FontSubType={2}",
                                                                            "NotEmbedded", font.FontName, font.SubType);
                                    toDoRow.Table.Rows.Add(toDoRow);

                                    if (fontUsage != null)
                                    { 
                                        // add pages (03.07.2023, SME)
                                        foreach (var page in fontUsage.Pages.Distinct().OrderBy(x => x))
                                        {
                                            var pageRow = todo.ToDoPages.NewToDoPagesRow();
                                            pageRow.ToDo_ID = toDoRow.ID;
                                            pageRow.Page = page;
                                            pageRow.Table.Rows.Add(pageRow);
                                        }
                                    }
                                }
                            }

                            // Done-Action(s) hinzufügen
                            // => Clear Backups
                            var doneActionRow = todo.DoneActions.NewDoneActionsRow();
                            doneActionRow.PDF_ID = pdfRow.ID;
                            doneActionRow.Action = "ClearBackups";
                            doneActionRow.Parameters = string.Empty;
                            doneActionRow.Table.Rows.Add(doneActionRow);
                            // => Move to Output
                            doneActionRow = todo.DoneActions.NewDoneActionsRow();
                            doneActionRow.PDF_ID = pdfRow.ID;
                            doneActionRow.Action = "MoveTo";
                            doneActionRow.Parameters = "FolderPath=" + Path.Combine(outputFolderPath, pdfClientFolderPart);
                            doneActionRow.Table.Rows.Add(doneActionRow);

                            // XML schreiben
                            targetFilePath += ".todo";
                            if (File.Exists(targetFilePath)) File.Delete(targetFilePath);
                            // => jedoch nur, wenn ToDo's erstellt wurden (05.07.2024, SME)
                            if (todo.ToDos.Any())
                            {
                                todo.WriteXml(targetFilePath, System.Data.XmlWriteMode.IgnoreSchema);
                            }

                            #endregion

                            #region PDF verschieben

                            if (todo.ToDos.Any())
                            {
                                // PDF in ToDo-Ordner verschieben (05.07.2024, SME)

                                // => Backup-PDFs in ToDo-Ordner verschieben (29.06.2023, SME)
                                foreach (var backupFilePath in pdf.GetBackupFilePaths())
                                {
                                    try
                                    {
                                        targetFilePath = Path.Combine(toDoFolderPath, pdfClientFolderPart, PdfBackupFolderName, Path.GetFileName(backupFilePath));
                                        CoreFC.MoveFile(backupFilePath, targetFilePath);
                                        CoreFC.DeleteEmptyFolder(Path.GetDirectoryName(backupFilePath));
                                    }
                                    catch (Exception ex)
                                    {
                                        // do nothing, just log it
                                        LogFC.WriteError(ex, PdfOptimizerFolderName);
                                    }
                                }

                                // => in ToDo-Ordner verschieben
                                targetFilePath = Path.Combine(toDoFolderPath, pdfClientFolderPart, pdfFileName);
                                CoreFC.MoveFile(pdfFilePath, targetFilePath);
                            }
                            else
                            {
                                // Es wurden keine ToDo's generiert (wegen den Einstellungen) (05.07.2024, SME)

                                // => Backup-Dateien löschen
                                foreach (var backupFilePath in pdf.GetBackupFilePaths())
                                {
                                    try
                                    {
                                        CoreFC.DeleteFile(backupFilePath, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        // do nothing, just log it
                                        LogFC.WriteError(ex, PdfOptimizerFolderName);
                                    }
                                }

                                // => PDF direkt in Output-Ordner verschieben
                                targetFilePath = Path.Combine(outputFolderPath, pdfClientFolderPart, pdfFileName);
                                CoreFC.MoveFile(pdfFilePath, targetFilePath);
                            }

                            #endregion
                        }
                    }
                    catch (Exception ex)
                    {
                        // FEHLER beim Optimieren von PDF

                        // => PDF freigeben
                        pdf?.Dispose();

                        // Zeit seit letzter Änderung ermitteln (14.12.2023, SME)
                        var diff = DateTime.Now - (new FileInfo(pdfFilePath).LastWriteTime);

                        // => Fehler loggen
                        string errorMsg = ex.Message;
                        if (ownError)
                        {
                            errorMsg = ex.Message;
                        }
                        else
                        {
                            errorMsg = $"Beim Optimieren von '{Path.GetFileName(pdfFilePath)}' ist folgender Fehler aufgetreten:" + CoreFC.Lines(2) + errorMsg;
                        }
                        errorMsg += CoreFC.Lines() + $"Zeit seit letzter Änderung: {diff}";
                        errorMsg += CoreFC.Lines() + $"WaitMaxSeconds: {waitMaxSeconds} Sek.";
                        LogFC.WriteError(new Exception(errorMsg, CoreFC.GetSourceError(ex)), PdfOptimizerFolderName);

                        // => PDF in Quarantäne verschieben,
                        // => jedoch nur wenn kein FileLocked-Error (02.01.2024, SME)
                        if (!CoreFC.IsFileLockedError(ex) && !CoreFC.IsFileLockedError(ex.InnerException))
                        {
                            targetFilePath = Path.Combine(quarantaeneFolderPath, DateTime.Now.ToString(DateTimeStamp) + ", " + Path.GetFileName(pdfFilePath));
                            try
                            {
                                CoreFC.MoveFile(pdfFilePath, targetFilePath);

                                // write error-email to inform about file in quarantäne (02.01.2024, SME)
                                ClsError.WriteErrorHTML(new Exception(errorMsg + CoreFC.Lines(2) + $"Die Datei '{pdfFilePath}' wurde in die Quarantäne verschoben!", CoreFC.GetSourceError(ex)));

                            }
                            catch (Exception exMove)
                            {
                                // FEHLER beim Verschieben in Quarantäne
                                errorMsg = $"Beim Verschieben von '{Path.GetFileName(pdfFilePath)}' in die Quarantäne ist folgender Fehler aufgetreten:" + CoreFC.Lines(2) + exMove.Message;
                                var newError = new Exception(errorMsg, CoreFC.GetSourceError(exMove));
                                LogFC.WriteError(newError, PdfOptimizerFolderName);
                                ClsError.WriteErrorHTML(newError);
                            }
                        } 
                    }
                    finally
                    {
                        // Log-Infos
                        sw.Stop();
                        LogFC.WriteLog($"Dauer: {sw.Elapsed}", PdfOptimizerFolderName);
                        LogFC.WriteLog(CoreFC.XString(Equal, 100), PdfOptimizerFolderName);
                    }
                }

                // Backup-Ordner löschen
                var backupFolderPath = Path.Combine(inputFolderPath, PdfBackupFolderName);
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
                LogFC.WriteError(ex, PdfOptimizerFolderName);
                ClsError.WriteErrorHTML(ex);
            }
        }

        #endregion

        #region Auto-Compress Images

        // Konstanten für Auto-Komprimierung der PDF-Bilder (07.07.2023, SME)
        private const string SettingName_AutoCompressPdfImagesOption = "AutoCompressPdfImagesOption";
        private const string SettingName_ImageCompressionMaxWidthHeight = "ImageCompressionMaxWidthHeight";
        private const string SettingName_ImageCompressionHandleXML = "ImageCompressionHandleXML";
        private const string SettingName_ImageCompressionHandleXmlTags = "ImageCompressionHandleXmlTags";

        // Auto-Compress Images (07.07.2023, SME)
        public static void AutoCompressImages(string rootFolderPath)
        {
            // Deklarationen
            PdfFile pdf = null;
            PdfFileActionResult result;
            Stopwatch sw = null;
            string targetFilePath;
            PdfAutoCompressImagesOptionEnum? autoOption = null;
            int maxWidthHeight;
            bool keepBackup = true;
            string settingValue;
            const string TempFolderName = "TEMP";

            try
            {
                #region Error-Handling

                // error-handling
                if (!Directory.Exists(rootFolderPath))
                {
                    throw new DirectoryNotFoundException("Root-Ordnerpfad nicht gefunden! Bilder von PDFs können nicht automatisch komprimiert werden! Pfad: " + rootFolderPath);
                }

                #endregion

                #region Ordner festlegen + erstellen

                string inputFolderPath = Path.Combine(rootFolderPath, InputFolderName);
                if (!Directory.Exists(inputFolderPath)) Directory.CreateDirectory(inputFolderPath);

                string quarantaeneFolderPath = Path.Combine(rootFolderPath, QuarantaeneFolderName);
                if (!Directory.Exists(quarantaeneFolderPath)) Directory.CreateDirectory(quarantaeneFolderPath);

                string toDoFolderPath = Path.Combine(rootFolderPath, ToDoFolderName);
                if (!Directory.Exists(toDoFolderPath)) Directory.CreateDirectory(toDoFolderPath);

                string outputFolderPath = Path.Combine(rootFolderPath, OutputFolderName);
                if (!Directory.Exists(outputFolderPath)) Directory.CreateDirectory(outputFolderPath);

                var tempFolderPath = Path.Combine(inputFolderPath, TempFolderName);
                if (!Directory.Exists(tempFolderPath)) Directory.CreateDirectory(tempFolderPath);

                #endregion

                #region PDF-Handling

                // alle PDFs von Input-Ordner ermitteln
                var pdfFilePaths = Directory.GetFiles(inputFolderPath, StarDotPDF);
                if (pdfFilePaths.Any())
                {

                    // Option ermitteln
                    settingValue = Global_TC_Core.GlobalAppSettings.GetSettingValue(SettingName_AutoCompressPdfImagesOption);
                    if (string.IsNullOrEmpty(settingValue))
                    {
                        // set default
                        autoOption = PdfAutoCompressImagesOptionEnum.CompressImages;
                        Global_TC_Core.GlobalAppSettings.SetSettingValue(SettingName_AutoCompressPdfImagesOption, autoOption.Value.ToString(), true);
                    }
                    else
                    {
                        autoOption = CoreFC.GetEnumValue<PdfAutoCompressImagesOptionEnum>(settingValue);
                        if (!autoOption.HasValue)
                        {
                            LogFC.WriteError(new Exception($"Auto-PDF-Bild-Komprimierungs-Option konnte nicht ermittelt werden von '{settingValue}'! Die Option '{PdfAutoCompressImagesOptionEnum.OnlyMoveToOutput}' wird stattdessen verwendet."));
                            autoOption = PdfAutoCompressImagesOptionEnum.OnlyMoveToOutput;
                        }
                    }

                    // Max Width/Height ermitteln
                    settingValue = Global_TC_Core.GlobalAppSettings.GetSettingValue(SettingName_ImageCompressionMaxWidthHeight);
                    if (string.IsNullOrEmpty(settingValue))
                    {
                        // set default
                        maxWidthHeight = 1024;
                        Global_TC_Core.GlobalAppSettings.SetSettingValue(SettingName_ImageCompressionMaxWidthHeight, maxWidthHeight.ToString(), true);
                    }
                    else if (!CoreFC.IsNumeric(settingValue))
                    {
                        maxWidthHeight = 1024;
                        LogFC.WriteError(new Exception($"Max. Höhe/Breite konnte nicht ermittelt werden von '{settingValue}'! Der Standardwert '{maxWidthHeight}' wird stattdessen verwendet."));
                    }
                    else
                    {
                        maxWidthHeight = int.Parse(settingValue);
                    }

                    // Loop durch PDF-Dateipfade
                    foreach (var pdfFilePath in pdfFilePaths)
                    {
                        try
                        {
                            // Stopuhr starten
                            sw = Stopwatch.StartNew();

                            // Log-Info
                            LogFC.WriteLog(CoreFC.XString(Equal, 100));
                            switch (autoOption.Value)
                            {
                                case PdfAutoCompressImagesOptionEnum.OnlyMoveToOutput:
                                    LogFC.WriteLog($"'{Path.GetFileName(pdfFilePath)}' wird nur verschoben ...");
                                    break;
                                case PdfAutoCompressImagesOptionEnum.CompressImages:
                                    LogFC.WriteLog($"'{Path.GetFileName(pdfFilePath)}': Bilder werden komprimiert ...");
                                    break;
                                default:
                                    throw new NotImplementedException($"Die Option '{autoOption}' wurde nicht behandelt!");
                            }

                            // OnlyMove abhandeln
                            if (autoOption.Value == PdfAutoCompressImagesOptionEnum.OnlyMoveToOutput)
                            {
                                targetFilePath = Path.Combine(outputFolderPath, Path.GetFileName(pdfFilePath));
                                if (File.Exists(targetFilePath)) File.Delete(targetFilePath);
                                File.Move(pdfFilePath, targetFilePath);
                                continue;
                            }

                            // PDF-File erstellen
                            pdf = new PdfFile(pdfFilePath);

                            // Option abhandeln
                            switch (autoOption.Value)
                            {
                                case PdfAutoCompressImagesOptionEnum.CompressImages:
                                    keepBackup = false;
                                    result = pdf.CompressImages(null, maxWidthHeight, keepBackup);
                                    break;
                                default:
                                    throw new NotImplementedException($"Die Option '{autoOption}' wurde nicht behandelt!");
                            }

                            // Result-Fehler prüfen
                            if (result.HasErrors)
                            {
                                // FEHLER beim Komprimieren der Bilder
                                var errorMsg = $"Beim Komprimieren der Bilder in '{Path.GetFileName(pdfFilePath)}' sind folgende Fehler aufgetreten:" + CoreFC.Lines(2) + string.Join(CoreFC.Lines(2), result.Errors.Select(x => x.Message));
                                throw new Exception(errorMsg);
                            }

                            // => PDF freigeben
                            pdf.Dispose();

                            // PDF verschieben
                            // => direkt in Output-Ordner verschieben
                            targetFilePath = Path.Combine(outputFolderPath, Path.GetFileName(pdfFilePath));
                            if (File.Exists(targetFilePath)) File.Delete(targetFilePath);
                            File.Move(pdfFilePath, targetFilePath);
                        }
                        catch (Exception ex)
                        {
                            // FEHLER beim Komprimieren der Bilder

                            // => PDF freigeben
                            pdf?.Dispose();

                            // => Fehler loggen
                            var errorMsg = $"Beim Komprimieren der Bilder in '{Path.GetFileName(pdfFilePath)}' ist folgender Fehler aufgetreten:" + CoreFC.Lines(2) + ex.Message;
                            LogFC.WriteError(new Exception(errorMsg));

                            // => PDF in Quarantäne verschieben
                            targetFilePath = Path.Combine(quarantaeneFolderPath, DateTime.Now.ToString(DateTimeStamp) + ", " + Path.GetFileName(pdfFilePath));
                            try
                            {
                                CoreFC.MoveFile(pdfFilePath, targetFilePath);
                            }
                            catch (Exception exMove)
                            {
                                // FEHLER beim Verschieben in Quarantäne
                                errorMsg = $"Beim Verschieben von '{Path.GetFileName(pdfFilePath)}' in die Quarantäne ist folgender Fehler aufgetreten:" + CoreFC.Lines(2) + exMove.Message;
                                LogFC.WriteError(new Exception(errorMsg));
                            }
                        }
                        finally
                        {
                            // Log-Infos
                            sw.Stop();
                            LogFC.WriteLog($"Dauer: {sw.Elapsed}");
                            LogFC.WriteLog(CoreFC.XString(Equal, 100));
                        }
                    }

                }

                #endregion

                #region XML-Handling

                // Prüfen ob XMLs überhaupt behandelt werden sollen
                settingValue = Global_TC_Core.GlobalAppSettings.GetSettingValue(SettingName_ImageCompressionHandleXML);
                if (string.IsNullOrEmpty(settingValue))
                {
                    // XMLs werden nicht behandelt
                    return;
                }
                else if (settingValue.ToUpper() != true.ToString().ToUpper())
                {
                    // XMLs werden nicht behandelt
                    return;
                }

                // Zu behandelnde XML-Tags ermitteln
                settingValue = Global_TC_Core.GlobalAppSettings.GetSettingValue(SettingName_ImageCompressionHandleXmlTags);
                if (string.IsNullOrEmpty(settingValue))
                {
                    // WARNUNG: Keine zu behandelnden XML-Tags gesetzt
                    LogFC.WriteError(new Exception("XMLs sollten behandelt werden, es sind aber keine zu behandelnden XML-Tags gesetzt!"));
                    return;
                }
                string[] tagNamesArray = settingValue.Split(';');

                // alle PDFs von Input-Ordner ermitteln
                var xmlFilePaths = Directory.GetFiles(inputFolderPath, StarDotXML);
                if (xmlFilePaths.Any())
                {
                    // Deklarationen
                    const string startTagFormula = "<{0}>";
                    const string endTagFormula = "</{0}>";

                    // Loop durch XML-Dateien
                    foreach (var xmlFilePath in xmlFilePaths)
                    {
                        try
                        {
                            // Stopuhr starten
                            sw = Stopwatch.StartNew();

                            // Log-Info
                            LogFC.WriteLog(CoreFC.XString(Equal, 100));
                            LogFC.WriteLog($"'{Path.GetFileName(xmlFilePath)}' wird komprimiert ...");

                            // declarations
                            string currentTagName = string.Empty;
                            string currentTagStart = string.Empty;
                            string currentTagEnd = string.Empty;
                            var output = new StringBuilder();
                            var toCompress = new StringBuilder();
                            int startIndex;
                            bool readToCompress = false;
                            Encoding encoding = null;

                            // set variables
                            string folderPath = Path.GetDirectoryName(xmlFilePath);
                            string fileName = Path.GetFileNameWithoutExtension(xmlFilePath);
                            string saveAs = Path.Combine(folderPath, TempFolderName, fileName + DotPDF);

                            // read file
                            using (var reader = new StreamReader(xmlFilePath))
                            {
                                // set encoding
                                encoding = reader.CurrentEncoding;

                                // read until end
                                while (reader.Peek() >= 0)
                                {
                                    // read line
                                    string line = reader.ReadLine();

                                    // check if reading to compress
                                    if (readToCompress)
                                    {
                                        // => check end-tag
                                        var endIndex = line.IndexOf(currentTagEnd);
                                        if (endIndex < 0)
                                        {
                                            // no end-tag found

                                            // => add line to compress
                                            toCompress.Append(line.Trim());
                                        }
                                        else
                                        {
                                            // end-tag found

                                            // => extract text before end-tag
                                            string lineToDo = line.Substring(0, endIndex);
                                            // => add line to compress
                                            toCompress.Append(lineToDo.Trim());
                                            // => compress
                                            var base64 = GetCompressedBase64(toCompress.ToString(), saveAs);
                                            // => add to output
                                            output.AppendLine(base64);
                                            // => add end
                                            output.AppendLine(line.Substring(endIndex));
                                            // => switch off flag
                                            readToCompress = false;
                                            // => clear current end-tag
                                            currentTagEnd = string.Empty;
                                            currentTagStart = string.Empty;
                                            currentTagName = string.Empty;
                                        }
                                        continue;
                                    }

                                    // find tag if not set
                                    if (string.IsNullOrEmpty(currentTagStart))
                                    {
                                        foreach (var tagName in tagNamesArray)
                                        {
                                            try
                                            {
                                                var index = line.IndexOf(string.Format(startTagFormula, tagName));
                                                if (index >= 0)
                                                {
                                                    currentTagName = tagName;
                                                    currentTagStart = string.Format(startTagFormula, tagName);
                                                    currentTagEnd = string.Format(endTagFormula, tagName);
                                                    break;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                    }

                                    // check if tag is set
                                    if (string.IsNullOrEmpty(currentTagName))
                                    {
                                        // line without specified tag
                                        // => add to output
                                        output.AppendLine(line);
                                        continue;
                                    }

                                    // check if line with tag
                                    startIndex = line.IndexOf(currentTagStart);
                                    if (startIndex < 0)
                                    {
                                        // line without specified tag
                                        // => add to output
                                        output.AppendLine(line);
                                        continue;
                                    }
                                    else
                                    {
                                        // start-tag found

                                        // store line without start-tag
                                        string lineToDo = line.Substring(startIndex + currentTagStart.Length);

                                        // get index of end-tag
                                        var endIndex = lineToDo.IndexOf(currentTagEnd);
                                        if (endIndex < 0)
                                        {
                                            // no end-tag found
                                            // => start reading
                                            readToCompress = true;
                                            toCompress.Append(lineToDo);
                                            // => add start of line to output
                                            output.AppendLine(line.Substring(0, startIndex) + currentTagStart);
                                        }
                                        else
                                        {
                                            // store line without end-tag
                                            lineToDo = lineToDo.Substring(0, endIndex);

                                            try
                                            {
                                                // get compressed base64
                                                var base64 = GetCompressedBase64(lineToDo, saveAs);

                                                // add switched/compressed value to output
                                                output.Append(line.Substring(0, startIndex));
                                                output.Append(currentTagStart);
                                                output.Append(base64);
                                                output.Append(lineToDo.Substring(endIndex));
                                                output.Append(Environment.NewLine);
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                    }
                                }
                            }

                            // write file
                            string outputFilePath = Path.Combine(outputFolderPath, Path.GetFileName(xmlFilePath));
                            using (var writer = new StreamWriter(outputFilePath, false, encoding))
                            {
                                writer.Write(output);
                            }

                            // delete input-file
                            CoreFC.DeleteFile(xmlFilePath);
                        }
                        catch (Exception ex)
                        {
                            // FEHLER beim Komprimieren der XML-Datei

                            // => Fehler loggen
                            var errorMsg = $"Beim Komprimieren der Bilder in '{Path.GetFileName(xmlFilePath)}' ist folgender Fehler aufgetreten:" + CoreFC.Lines(2) + ex.Message;
                            LogFC.WriteError(new Exception(errorMsg));

                            // => XML 1:1 in Output verschieben
                            targetFilePath = Path.Combine(outputFolderPath, Path.GetFileName(xmlFilePath));
                            try
                            {
                                CoreFC.MoveFile(xmlFilePath, targetFilePath);
                            }
                            catch (Exception exMove)
                            {
                                // FEHLER beim Verschieben in Output
                                errorMsg = $"Beim Verschieben von '{Path.GetFileName(xmlFilePath)}' in den Output-Ordner ist folgender Fehler aufgetreten:" + CoreFC.Lines(2) + exMove.Message;
                                LogFC.WriteError(new Exception(errorMsg));
                            }
                        }
                        finally
                        {
                            // Log-Infos
                            sw.Stop();
                            LogFC.WriteLog($"Dauer: {sw.Elapsed}");
                            LogFC.WriteLog(CoreFC.XString(Equal, 100));
                        }
                    }
                }

                #endregion

                // Backup-Ordner löschen
                var backupFolderPath = Path.Combine(inputFolderPath, PdfBackupFolderName);
                if (Directory.Exists(backupFolderPath))
                {
                    if (CoreFC.IsEmptyFolder(backupFolderPath))
                    {
                        Directory.Delete(backupFolderPath);
                    }
                }

                // Temp-Backup-Ordner löschen
                backupFolderPath = Path.Combine(tempFolderPath, PdfBackupFolderName);
                if (Directory.Exists(backupFolderPath))
                {
                    if (CoreFC.IsEmptyFolder(backupFolderPath))
                    {
                        Directory.Delete(backupFolderPath);
                    }
                }

                // Temp-Ordner löschen
                if (Directory.Exists(tempFolderPath))
                {
                    if (CoreFC.IsEmptyFolder(tempFolderPath))
                    {
                        Directory.Delete(tempFolderPath);
                    }
                }

            }
            catch (Exception ex)
            {
                LogFC.WriteError(ex);
            }
        }

        // Get compressed Base64 (07.07.2023, SME)
        private static string GetCompressedBase64(string toCompress, string saveAs)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(toCompress)) return string.Empty;

                // convert base64 to bytes
                var bytes = Convert.FromBase64String(toCompress);

                // save as pdf
                if (File.Exists(saveAs)) File.Delete(saveAs);
                File.WriteAllBytes(saveAs, bytes);

                // load pdf
                var pdf = new PdfFile(saveAs, false);

                // compress images
                var result = pdf.CompressImages(null, 1024, false);

                // read bytes
                var newBytes = File.ReadAllBytes(saveAs);

                // convert bytes to base64
                var base64 = Convert.ToBase64String(newBytes);

                // dispose pdf
                pdf.Dispose();

                // delete written pdf
                if (File.Exists(saveAs)) File.Delete(saveAs);

                // return
                return base64;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Font-Functions

        #region Unembed Fonts

        // Unembed Fonts (20.04.2023, SME)
        // Diese Methode ist noch nicht vollständig getestet und funktioniert noch nicht korrekt (20.04.2023, SME)
        public static void UnembedFonts(string pdfFilePath)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(pdfFilePath)) throw new ArgumentNullException(nameof(pdfFilePath));
                if (!File.Exists(pdfFilePath)) throw new FileNotFoundException("PDF-Datei nicht gefunden!", pdfFilePath);
                if (!pdfFilePath.ToLower().EndsWith(DotPDF)) throw new ArgumentException("Ungültige PDF-Datei:" + Environment.NewLine + pdfFilePath, nameof(pdfFilePath));

                // Deklarationen
                var newFontList = new Dictionary<string, PdfFont>();

                var targetPath = pdfFilePath + "_FontsUnembedded.pdf";
                using (var reader = new PdfReader(pdfFilePath))
                {
                    using (var sourcePDF = new PdfDocument(reader))
                    {
                        // writer-properties
                        var writerProperties = new WriterProperties();
                        writerProperties.UseSmartMode();
                        writerProperties.SetFullCompressionMode(true);

                        using (var writer = new PdfWriter(targetPath, writerProperties))
                        {
                            using (var targetPDF = new PdfDocument(writer))
                            {
                                var pageCount = sourcePDF.GetNumberOfPages();
                                var sw = Stopwatch.StartNew();
                                int percent = 0;
                                bool oneByOne = true;

                                // copy + handle pages
                                if (!oneByOne)
                                {
                                    // all pages at once
                                    sourcePDF.CopyPagesTo(1, pageCount, targetPDF);

                                    if (pageCount != targetPDF.GetNumberOfPages())
                                    {
                                        Debugger.Break();
                                    }

                                    // loop throu pages
                                    for (int iPage = 1; iPage <= pageCount; iPage++)
                                    {
                                        var page = targetPDF.GetPage(iPage);
                                        UnembedFonts(page);

                                        int newPercent = iPage * 100 / pageCount;
                                        if (percent != newPercent)
                                        {
                                            percent = newPercent;
                                            CoreFC.DPrint($"{percent} %");
                                        }
                                    }

                                    sw.Stop();
                                    CoreFC.DPrint($"Duration of copying pdf-pages all at once: {sw.Elapsed}");
                                }
                                else
                                {
                                    // copy page by page
                                    for (int iPage = 1; iPage <= pageCount; iPage++)
                                    {
                                        sourcePDF.CopyPagesTo(iPage, iPage, targetPDF);

                                        var page = targetPDF.GetPage(iPage);
                                        UnembedFonts(page);

                                        int newPercent = iPage * 100 / pageCount;
                                        if (percent != newPercent)
                                        {
                                            percent = newPercent;
                                            CoreFC.DPrint($"{percent} %");
                                        }
                                    }

                                    if (pageCount != targetPDF.GetNumberOfPages())
                                    {
                                        Debugger.Break();
                                    }

                                    sw.Stop();
                                    CoreFC.DPrint($"Duration of copying pdf-pages one by one: {sw.Elapsed}");
                                }

                                // close objects
                                sourcePDF.Close();
                                targetPDF.Close();
                                writer.Close();
                                reader.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Unembed Fonts in Page (20.04.2023, SME)
        private static void UnembedFonts(PdfPage page)
        {
            try
            {
                // exit-handling
                if (page == null) return;

                // ressourcen ermitteln
                var resources = page.GetResources();

                // Switch Fonts directly
                var fonts = resources.GetResource(PdfName.Font);
                UnembedFonts(fonts);

                // Switch Fonts in XObject
                var xObject = resources.GetResource(PdfName.XObject);
                if (xObject != null)
                {
                    foreach (PdfName key in xObject.KeySet())
                    {
                        var xChildObject = xObject.Get(key);
                        if (xChildObject != null)
                        {
                            if (xChildObject is PdfStream)
                            {
                                var stream = (PdfStream)xChildObject;
                                var xChildObjectResources = stream.GetAsDictionary(PdfName.Resources);
                                if (xChildObjectResources != null)
                                {
                                    var xChildObjectResourcesFont = xChildObjectResources.GetAsDictionary(PdfName.Font);
                                    UnembedFonts(xChildObjectResourcesFont);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Unembed Fonts in Fonts-Dictionary (20.04.2023, SME)
        private static void UnembedFonts(PdfDictionary fonts)
        {
            try
            {
                // exit-handling
                if (fonts == null) return;

                foreach (var key in fonts.KeySet().ToArray())
                {
                    var oldFont = fonts.GetAsDictionary(key);
                    try
                    {
                        // check font-descriptor
                        if (oldFont.ContainsKey(PdfName.FontDescriptor))
                        {
                            // remove font-descriptor to unembed the font
                            // => this doesn't work, otherwise font looks really strange, font-file must be removed (09.05.2023, SME)
                            //oldFont.Remove(PdfName.FontDescriptor);

                            // store font-descriptor
                            var fontDescriptor = oldFont.GetAsDictionary(PdfName.FontDescriptor);
                            if (fontDescriptor != null)
                            {
                                // remove font-files
                                if (fontDescriptor.ContainsKey(PdfName.FontFile))
                                {
                                    fontDescriptor.Remove(PdfName.FontFile);
                                }
                                else if (fontDescriptor.ContainsKey(PdfName.FontFile2))
                                {
                                    fontDescriptor.Remove(PdfName.FontFile2);
                                }
                                else if (fontDescriptor.ContainsKey(PdfName.FontFile3))
                                {
                                    fontDescriptor.Remove(PdfName.FontFile3);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #endregion

        #region TO ORDER

        // Extract Text from PDF-Filepath (10.10.2023, SME)
        public static Dictionary<int, string> ExtractText(string pdfFilePath)
        {
            try
            {
                // declare return-value
                Dictionary<int, string> returnValue = new Dictionary<int, string>();


                // open pdf-reader + -document
                using (PdfReader pdfReader = new PdfReader(pdfFilePath))
                {
                    using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
                    {
                        // loop throu pages
                        for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                        {
                            // store page
                            var page = pdfDocument.GetPage(pageNum);

                            // retrieve page-text
                            string pageText = ExtractTextFromPage(page);

                            // add page-text to return-value
                            returnValue.Add(pageNum, pageText);
                        }
                    }
                }

                // return
                return returnValue;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Extract Text from PDF-Filepath (06.11.2023, SME)
        public static Dictionary<int, string> ExtractTextFromPDF(string pdfFilePath)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(pdfFilePath)) throw new ArgumentNullException(nameof(pdfFilePath));
                if (!File.Exists(pdfFilePath)) throw new FileNotFoundException("Datei nicht gefunden", pdfFilePath);
                if (!pdfFilePath.ToLower().EndsWith(".pdf")) throw new ArgumentOutOfRangeException(nameof(pdfFilePath), "Ungültiges PDF-Format");

                // declare return-value
                var returnValue = new Dictionary<int, string>();

                // use reader
                using (var reader = new PdfReader(pdfFilePath))
                {
                    // use document
                    using (var document = new PdfDocument(reader))
                    {
                        // loop throu pages
                        int pageCount = document.GetNumberOfPages();
                        for (int iPage = 1; iPage <= pageCount; iPage++)
                        {
                            returnValue.Add(iPage, ExtractTextFromPage(document.GetPage(iPage)));
                        }
                    }
                }

                // return
                return returnValue;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
                // =>  ExceptionDispatchInfo.Capture(ex).Throw(); throw ex;
            }
        }

        // Extract Text from Page (10.10.2023, SME)
        internal static string ExtractTextFromPage(PdfPage page) //, ExtractTextFromPageModeEnum mode = ExtractTextFromPageModeEnum.GetFromAllAndCompare)
        {
            try
            {
                // exit-handling
                if (page == null) return string.Empty;

                // IMPORTANT: create new instance of strategy every single time in a loop or for every page,
                // otherwise the old text from previous pages will be included as well (10.10.2023, SME)
                //var strategy = new LocationTextExtractionStrategy();

                //// handle mode
                //switch (mode)
                //{
                //    case ExtractTextFromPageModeEnum.GetTextFromPageViaPdfTextExtractor:
                //        return PdfTextExtractor.GetTextFromPage(page, strategy);

                //    case ExtractTextFromPageModeEnum.GetResultantTextFromStrategy:
                //        return strategy.GetResultantText();

                //    case ExtractTextFromPageModeEnum.GetFromAllAndCompare:
                //        // declarations
                //        var textList = new Dictionary<ExtractTextFromPageModeEnum, string>();
                //        var durationList = new Dictionary<ExtractTextFromPageModeEnum, TimeSpan>();

                //        // fill lists
                //        foreach (var modeEnum in CoreFC.GetEnumValues<ExtractTextFromPageModeEnum>())
                //        {
                //            // skip-handling
                //            if (modeEnum == mode) continue;

                //            // start watch
                //            var sw = Stopwatch.StartNew();

                //            // handle mode to get text
                //            textList.Add(modeEnum, ExtractTextFromPage(page, modeEnum));

                //            // stop watch
                //            sw.Stop();

                //            // add duration to list
                //            durationList.Add(modeEnum, sw.Elapsed);
                //        }

                //        // compare texts
                //        if (textList.Values.Distinct().Count() > 1)
                //        {
                //            Console.WriteLine("WARNING: There are different text-results while extracting text from pdf-page! SME should check this!!!");
                //        }

                //        // list modes by duration
                //        int i = 0;
                //        Console.WriteLine("Debug-Info for ExtractTextFromPdfPage by Duration:");
                //        foreach (var durationItem in durationList.OrderBy(x => x.Value).ThenBy(x => x.Key.ToString())) 
                //        {
                //            Console.WriteLine($"{++i}. {durationItem.Key} => {durationItem.Value}");
                //        }

                //        // return
                //        return textList.Values.First();

                //    default:
                //        throw new ArgumentOutOfRangeException(nameof(mode), $"Unbehandelter Modus: {mode}");
                //}

                // retrieve page-text
                //string pageText2 = strategy.GetResultantText(); // IMPORTANT: this doesn't return a text!
                //var sw1 = Stopwatch.StartNew();
                //string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                //sw1.Stop();
                //var sw2 = Stopwatch.StartNew();
                string pageText3 = PdfTextExtractor.GetTextFromPage(page);
                //sw2.Stop();

                //if (sw1.Elapsed < sw2.Elapsed)
                //{
                //    Debugger.Break();
                //}

                //if (pageText != pageText3)
                //{
                //    return pageText;
                //}

                // return
                return pageText3;
                //if (pageText == pageText2)
                //{
                //    return pageText;
                //}
                //else
                //{
                //    return pageText2;
                //}
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
                // =>  ExceptionDispatchInfo.Capture(ex).Throw(); throw ex;
            }
        }

        // Extract Text from Page-Area (v1) (22.04.2024, SME)
        internal static string ExtractTextFromPageArea_v1(PdfPage page, iText.Kernel.Geom.Rectangle area)
        {
            try
            {
                // exit-handling
                if (page == null) return string.Empty;

                var textEventListener = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, textEventListener);
                var text2 = textEventListener.GetResultantText(area);
                if (text == text2) return text2;
                return text2;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Extract Text from Page-Area (v2) (22.04.2024, SME)
        internal static string ExtractTextFromPageArea_v2(PdfPage page, iText.Kernel.Geom.Rectangle area)
        {
            try
            {
                // exit-handling
                if (page == null) return string.Empty;

                var filterListener = new FilteredTextEventListener(new LocationTextExtractionStrategy(), new TextRegionEventFilter(area));
                var text = PdfTextExtractor.GetTextFromPage(page, filterListener);
                return text;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Extract Text from Page-Area (v3) (22.04.2024, SME)
        // => Diese Version bringt gar nichts, da immer der Text der vorgängigen Seiten ebenfalls zurückgeliefert wird (23.04.2024, SME)
        //internal static string ExtractTextFromPageArea_v3(PdfPage page, FilteredTextEventListener filterListener)
        //{
        //    try
        //    {
        //        // exit-handling
        //        if (page == null) return string.Empty;
        //        if (filterListener == null) return string.Empty;

        //        var text = PdfTextExtractor.GetTextFromPage(page, filterListener);
        //        return text;
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFC.ThrowError(ex); throw ex;
        //    }
        //}

        // Ist Seite mit Bildern (11.10.2023, SME)
        internal static bool PageHasImages(PdfPage page)
        {
            if (page == null) return false;
            var resources = page.GetResources();
            if (resources == null) return false;
            foreach (var resourceName in resources.GetResourceNames())
            {
                var resourceObject = resources.GetPdfObject().Get(resourceName);
                if (IsImage(resourceObject)) return true;
            }
            return false;
        }

        #endregion

        #region REMARKED / BACKUP

        #endregion

        // Optimize PDFs (17.02.2024, SME)
        public static IEnumerable<PdfFileOptimizationResult> OptimizePDFs(string[] pdfFilePaths, bool keepBackup = false)
        {
            var returnList = new List<PdfFileOptimizationResult>();

            try
            {
                // exit-handling
                if (pdfFilePaths == null || !pdfFilePaths.Any()) return returnList;

                // declare error-list
                var errorList = new List<Exception>();

                // get fonts-table
                var fontsTable = FC_Fonts.GetFontsTable();

                // loop throu pdf-file-paths
                foreach (var pdfFilePath in pdfFilePaths)
                {
                    try
                    {
                        // skip-handling
                        if (string.IsNullOrEmpty(pdfFilePath)) continue;
                        if (!File.Exists(pdfFilePath)) continue;
                        if (!pdfFilePath.ToLower().EndsWith(DotPDF)) continue;

                        // use pdf
                        using (var pdf = new PdfFile(pdfFilePath, false))
                        {
                            var result = pdf.Optimize(false, true, fontsTable, keepBackup);
                            returnList.Add(result);
                            if (result.HasErrors)
                            {
                                errorList.AddRange(result.Errors);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorList.Add(ex);
                    }
                }

                // throw errors if any
                if (errorList.Any())
                {
                    throw new AggregateException(errorList.ToArray());
                }

                // return
                return returnList;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }
    }
}
