using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using TC.Classes;
using TC.Constants;
using TC.Functions;
using TC.Resources.Fonts.LIB.Classes;
using TC.Resources.Fonts.LIB.Data.FontDataDBTableAdapters;
using TC.Resources.Fonts.LIB.Functions;
using TC.Resources.Fonts.LIB.Interfaces;
using static TC.Constants.CoreConstants;

namespace TC.Resources.Fonts.LIB.Data
{

    partial class FontDataDB
    {
        #region Methods

        #region Sync Fonts

        // Flag: Is Synchronizing (21.06.2023, SME)
        public bool IsSynchronizing { get; private set; }

        // Sync Fonts (21.06.2023, SME)
        public void SyncFonts(ProgressInfo progressInfo = null)
        {
            // exit-handling
            if (IsSynchronizing) return;

            try
            {
                // error-handling
                if (CoreFC.GetNetzwerkTyp() != Enums.NetzwerkTyp.ClientNetz) throw new InvalidOperationException("Die Schrift-Daten können nur im Client-Netz synchronisiert werden");

                // Flag setzen
                IsSynchronizing = true;

                // Daten laden
                progressInfo?.SetStatus("Daten werden ermittelt von Datenbank ...");
                var adapter = new FontsWithStreamTableAdapter();
                var fontTable = adapter.GetData();
                fontTable.AcceptChanges();

                // Nicht mehr existierende Datensätze entfernen
                progressInfo?.SetStatus("Nicht mehr existierende Schriften werden entfernt ...");
                foreach (var fontRow in fontTable)
                {
                    if (!File.Exists(fontRow.FontFilePath))
                    {
                        if (fontRow.IsDeletedOnNull())
                        {
                            fontRow.DeletedOn = DateTime.Now;
                        }
                    }
                }
                var changes = fontTable.GetChanges() as FontDataDB.FontsWithStreamDataTable;
                if (changes != null)
                {
                    adapter.Update(changes);
                    fontTable.AcceptChanges();
                }

                // Alle Schrift-Dateien ermitteln
                progressInfo?.SetStatus("Schrift-Dateien werden ermittelt ...");
                var fileList = new List<string>();
                foreach (var fontFolder in FC_Fonts.DefaultFontFolderPaths_Client.Split(';'))
                {
                    if (Directory.Exists(fontFolder))
                    {
                        fileList.AddRange(Directory.GetFiles(fontFolder, StarDotTTF, SearchOption.AllDirectories));
                        fileList.AddRange(Directory.GetFiles(fontFolder, StarDotOTF, SearchOption.AllDirectories));
                    }
                }
                // Alle Mac-Dateien entfernen
                fileList.RemoveAll(x => Path.GetFileName(x).StartsWith(DOT));

                // exit-handling
                if (!fileList.Any()) return;

                // Anzahl Schritte in Progress-Info aktualisieren
                progressInfo?.SetTotalSteps(fileList.Count);

                // Loop durch Schrift-Dateien (sortiert)
                var now = DateTime.Now;
                progressInfo?.SetStatus("Schrift-Dateien werden eingelesen ...");
                foreach (var fontFilePath in fileList.OrderBy(x => x).ToArray())
                {
                    try
                    {
                        // create font-info
                        var fontInfo = new FontInfo(fontFilePath);

                        // set font-row (get or create)
                        var row = fontTable.FirstOrDefault(r => r.FontFilePath.Equals(fontFilePath));
                        if (row == null)
                        {
                            row = fontTable.NewFontsWithStreamRow();
                            row.AddedOn = now;
                            row.ChangedOn = now;
                            row.FileStream = File.ReadAllBytes(fontFilePath);
                            row.Synonyms = string.Empty;
                        }

                        // update infos
                        row.UpdateInfos(fontInfo);

                        // add to table if necessary
                        if (row.RowState == System.Data.DataRowState.Detached) row.Table.Rows.Add(row);
                        else if (row.RowState == DataRowState.Modified)
                        {
                            row.ChangedOn = now;
                            row.FileStream = File.ReadAllBytes(fontFilePath);
                        }

                        // perform step
                        progressInfo?.PerformStep();
                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }

                // Speichern
                changes = fontTable.GetChanges() as FontDataDB.FontsWithStreamDataTable;
                if (changes != null)
                {
                    progressInfo?.SetStatus("Änderungen werden in Datenbank gespeichert ...");
                    adapter.Update(changes);
                    fontTable.AcceptChanges();

                    // Sync to PROD (22.06.2023, SME)
                    if (CoreFC.GetNetzwerkTyp() != Enums.NetzwerkTyp.ProdNetz)
                    {
                        progressInfo?.SetStatus("Änderungen werden in PROD-Datenbank synchronisiert ...");
                        SyncFontsInPROD();
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                // clear flag
                IsSynchronizing = true;
            }
        }

        // Sync Fonts in PROD (22.06.2023, SME)
        public void SyncFontsInPROD()
        {
            try
            {
                // exit-handling
                if (CoreFC.GetNetzwerkTyp() == Enums.NetzwerkTyp.ProdNetz) return;

                // Sync to Prod
                using (var query = new FontDataDBTableAdapters.QueriesTableAdapter())
                {
                    query.spSyncToProd();
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #endregion

        #region File-System-Watcher

        // List of Font-Change-Watchers with corresponding Watcher-File-Text (27.05.2023, SME)
        private Dictionary<FileSystemWatcher, string> FontChangeWatcherList = new Dictionary<FileSystemWatcher, string>();

        // Refresh-Font-Changes-Timer (27.05.2023, SME)
        private readonly System.Timers.Timer RefreshFontChangesTimer = new System.Timers.Timer(500);

        // Flag: Has active Font-Change-Watchers (27.05.2023, SME)
        public bool HasActiveFontChangeWatcher => FontChangeWatcherList.Any();

        // Font-Change-Watchers aktivieren (27.05.2023, SME)
        public void ActivateFontWatcher()
        {
            try
            {
                // exit-handling
                if (HasActiveFontChangeWatcher) return;
                if (CoreFC.GetNetzwerkTyp() != Enums.NetzwerkTyp.ClientNetz) throw new InvalidOperationException("Font-Watchers können nur im Client-Netz aktiviert werden");

                // Event-Handlers setzen
                RefreshFontChangesTimer.Elapsed -= RefreshFontChangesTimer_Elapsed;
                RefreshFontChangesTimer.Elapsed += RefreshFontChangesTimer_Elapsed;

                // Font-Watcher-Datei-Text ermitteln
                var watcherFileTextStart = GetWatcherFileTextStart();

                // Loop durch Font-Folders
                foreach (var fontFolder in FC_Fonts.DefaultFontFolderPaths_Client.Split(';'))
                {
                    // Watcher-Dateipfad zwischenspeichern
                    var watcherFilePath = GetWatcherFilePath(fontFolder);

                    // skip-handling
                    if (!Directory.Exists(fontFolder)) continue;
                    if (string.IsNullOrEmpty(watcherFilePath)) continue;
                    if (File.Exists(watcherFilePath))
                    {
                        // weiterfahren, weil bereits ein Watcher aktiv ist
                        continue;
                    }

                    // Watcher erstellen
                    var fsw = new FileSystemWatcher();

                    // Eigenschaften setzen
                    fsw.EnableRaisingEvents = false;
                    fsw.Path = fontFolder;
                    fsw.IncludeSubdirectories = true;
                    fsw.Filter = StarDotStar; // => damit auch Änderungen in Ordners behandelt werden
                    fsw.NotifyFilter =
                        NotifyFilters.FileName |
                        NotifyFilters.DirectoryName |
                        NotifyFilters.LastWrite |
                        NotifyFilters.Security |
                        NotifyFilters.CreationTime |
                        NotifyFilters.LastAccess |
                        NotifyFilters.Attributes |
                        NotifyFilters.Size;

                    // Event-Handling hinzufügen
                    fsw.Created -= Font_CreatedChangedDeletedRenamed;
                    fsw.Changed -= Font_CreatedChangedDeletedRenamed;
                    fsw.Deleted -= Font_CreatedChangedDeletedRenamed;
                    fsw.Renamed -= Font_CreatedChangedDeletedRenamed;
                    fsw.Created += Font_CreatedChangedDeletedRenamed;
                    fsw.Changed += Font_CreatedChangedDeletedRenamed;
                    fsw.Deleted += Font_CreatedChangedDeletedRenamed;
                    fsw.Renamed += Font_CreatedChangedDeletedRenamed;
                    fsw.EnableRaisingEvents = true;

                    // Watcher der Liste hinzufügen
                    FontChangeWatcherList.Add(fsw, watcherFilePath);

                    // Watcher-Datei schreiben
                    File.WriteAllText(watcherFilePath, watcherFileTextStart);
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Font-Change-Watcher deaktivieren (27.05.2023, SME)
        public void DeactiveFontWatcher()
        {
            try
            {
                // exit-handling
                if (!HasActiveFontChangeWatcher) return;

                // Loop durch Watcher-Einträge
                foreach (var fsw in FontChangeWatcherList.Keys.ToArray())
                {
                    // clear watcher
                    fsw.EnableRaisingEvents = false;
                    fsw.Created -= Font_CreatedChangedDeletedRenamed;
                    fsw.Changed -= Font_CreatedChangedDeletedRenamed;
                    fsw.Deleted -= Font_CreatedChangedDeletedRenamed;
                    fsw.Renamed -= Font_CreatedChangedDeletedRenamed;
                    fsw.Dispose();

                    // delete watcher-file
                    var path = FontChangeWatcherList[fsw];
                    if (File.Exists(path)) File.Delete(path);

                    // remove
                    FontChangeWatcherList.Remove(fsw);
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Watcher-File-Path (27.05.2023, SME)
        private string GetWatcherFilePath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return string.Empty;
            if (!Directory.Exists(folderPath)) return string.Empty;
            return Path.Combine(folderPath, "FontWatcher" + DotWatch);
        }

        // Get Watcher-File-Text-Start (27.05.2023, SME)
        private string GetWatcherFileTextStart()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Font-Folder has active File-Watcher, created");
                sb.AppendLine($"By: {CoreFC.GetUserName()}");
                sb.AppendLine($"On: {DateTime.Now}");
                sb.AppendLine($"In: {System.Reflection.Assembly.GetEntryAssembly()?.GetName()}");
                sb.AppendLine($"InstanceGuid: {CoreConstants.InstanceGuid.ToString().ToUpper()}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // List of pending Font-Changes (27.05.2023, SME)
        private readonly List<FileSystemEventArgs> PendingFontChanges = new List<FileSystemEventArgs>();

        // Add Font-Change (27.05.2023, SME)
        private void AddFontChange(FileSystemEventArgs e)
        {
            try
            {
                // Debug-Info
                //if (e is RenamedEventArgs)
                //{
                //    Console.WriteLine($"Font-Rename: Type = {e.ChangeType}, FullPath = {e.FullPath}, OldFullPath = {((RenamedEventArgs)e).OldFullPath}");
                //}
                //else
                //{
                //    Console.WriteLine($"Font-Change: Type = {e.ChangeType}, FullPath = {e.FullPath}");
                //}

                // stop timer
                RefreshFontChangesTimer.Stop();

                // add change to list
                lock (PendingFontChanges)
                {
                    PendingFontChanges.Add(e);
                }

                // restart timer
                RefreshFontChangesTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FEHLER beim Hinzufügen eines Font-Changes in der Font-Table: " + ex.Message);
            }
        }

        // Event-Handler: Font created / changed / deleted (27.05.2023, SME)
        private void Font_CreatedChangedDeletedRenamed(object sender, FileSystemEventArgs e)
        {
            AddFontChange(e);
        }

        // Event-Handler: Refresh-Font-Changes-Timer elapsed (27.05.2023, SME)
        private void RefreshFontChangesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // switch off timer
                RefreshFontChangesTimer.Stop();

                // handle pending changes
                if (!HandlePendingFontChanges())
                {
                    // handling failed => restart timer
                    RefreshFontChangesTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FEHLER im Refresh-Font-Changes-Timer der Font-Table: " + ex.Message);
            }
        }

        // Pendente Font-Changes abhandeln (27.05.2023, SME)
        private bool IsHandlingPendingFontChanges = false;
        private bool HandlePendingFontChanges()
        {
            // exit-handling (outside of try-catch to not clear flag on finally)
            if (IsHandlingPendingFontChanges) return false;

            // store now
            var now = DateTime.Now;

            try
            {
                // set flag
                IsHandlingPendingFontChanges = true;

                // copy changes locally + clear list
                FileSystemEventArgs[] fsoChanges;
                lock (PendingFontChanges)
                {
                    fsoChanges = PendingFontChanges.ToArray();
                    PendingFontChanges.Clear();
                }

                // exit-handling
                if (!fsoChanges.Any()) return true;

                // use adapter
                using (var adapter = new FontsWithStreamTableAdapter())
                {
                    // use table
                    using (var table = new FontsWithStreamDataTable())
                    {
                        // loop throu paths
                        foreach (var fsoPath in fsoChanges.Select(x => x.FullPath).Distinct().OrderBy(x => x).ToArray())
                        {
                            try
                            {
                                // store changes + change-types for this path
                                var changes = fsoChanges.Where(x => x.FullPath.Equals(fsoPath)).ToArray();

                                // handle fso-type
                                if (Directory.Exists(fsoPath))
                                {
                                    // Folder changed
                                    // => only handle rename
                                    if (changes.Any(x => x.ChangeType == WatcherChangeTypes.Renamed))
                                    {
                                        var rename = changes.First(x => x.ChangeType == WatcherChangeTypes.Renamed);
                                        if (rename is RenamedEventArgs)
                                        {
                                            // Folder renamed
                                            // => Update all Rows that start with this Folder-Path
                                            Console.WriteLine($"Folder renamed: {rename.FullPath}, old Path = {((RenamedEventArgs)rename).OldFullPath}");
                                            var oldPrefix = ((RenamedEventArgs)rename).OldFullPath;
                                            var newPrefix = rename.FullPath;
                                            adapter.FillByPathPrefix(table, oldPrefix + "%");
                                            var rows = table.Where(x => x.FontFilePath.ToLower().StartsWith(oldPrefix.ToLower())).ToList();
                                            if (rows.Any())
                                            {
                                                try
                                                {
                                                    foreach (var row in rows)
                                                    {
                                                        row.FontFilePath = newPrefix + row.FontFilePath.Substring(oldPrefix.Length);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    CoreFC.ThrowError(ex); throw ex;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Folder renamed: {rename.FullPath}");
                                        }
                                    }
                                }
                                else if (File.Exists(fsoPath))
                                {
                                    // File changed

                                    // check if TTF or OTF
                                    if (fsoPath.ToLower().EndsWith(DotTTF))
                                    {
                                        // TTF => go on
                                    }
                                    else if (fsoPath.ToLower().EndsWith(DotOTF))
                                    {
                                        // OTF => go on
                                    }
                                    else
                                    {
                                        // Other File => continue
                                        continue;
                                    }

                                    // check if renamed
                                    if (changes.Any(x => x.ChangeType == WatcherChangeTypes.Renamed))
                                    {
                                        // File renamed
                                        var rename = changes.First(x => x.ChangeType == WatcherChangeTypes.Renamed);
                                        if (rename is RenamedEventArgs)
                                        {
                                            Console.WriteLine($"File renamed: {rename.FullPath}, old Path = {((RenamedEventArgs)rename).OldFullPath}");
                                            var oldPrefix = ((RenamedEventArgs)rename).OldFullPath;
                                            var newPrefix = rename.FullPath;
                                            adapter.FillByPathPrefix(table, oldPrefix);
                                            var rows = table.Where(x => x.FontFilePath.ToLower().StartsWith(oldPrefix.ToLower())).ToList();
                                            if (rows.Any())
                                            {
                                                try
                                                {
                                                    foreach (var row in rows)
                                                    {
                                                        row.FontFilePath = newPrefix + row.FontFilePath.Substring(oldPrefix.Length);
                                                        row.FontFileName = Path.GetFileNameWithoutExtension(rename.FullPath);
                                                        row.FontFileType = Path.GetExtension(rename.FullPath).ToLower();
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    CoreFC.ThrowError(ex); throw ex;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"File renamed: {rename.FullPath}");
                                        }
                                    }
                                    // check if created
                                    else if (changes.Any(x => x.ChangeType == WatcherChangeTypes.Created))
                                    {
                                        Console.WriteLine($"File created: {fsoPath}");
                                        HandleFileChange(fsoPath, adapter, table, now);
                                    }
                                    // other change
                                    else
                                    {
                                        Console.WriteLine($"File changed: {fsoPath}");
                                        HandleFileChange(fsoPath, adapter, table, now);
                                    }
                                }
                                else
                                {
                                    // FSO deleted
                                    var change = changes.Last();
                                    if (change.ChangeType == WatcherChangeTypes.Deleted)
                                    {
                                        Console.WriteLine($"FSO deleted: {change.FullPath}");
                                        var prefix = change.FullPath;
                                        adapter.FillByPathPrefix(table, prefix);
                                        var rows = table.Where(x => x.RowState != DataRowState.Deleted && x.FontFilePath.ToLower().StartsWith(prefix.ToLower()) && x.IsDeletedOnNull()).ToList();
                                        if (rows.Any())
                                        {
                                            rows.ForEach(row =>
                                            {
                                                if (!File.Exists(row.FontFilePath)) row.DeletedOn = now;
                                            });
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"UNHANDLED Font-Change: Type = {change.ChangeType}, Path = {change.FullPath}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                CoreFC.ThrowError(ex); throw ex;
                            }
                        }

                        // save changes
                        var dataChanges = table.GetChanges() as FontsWithStreamDataTable;
                        if (dataChanges != null)
                        {
                            foreach (var row in dataChanges)
                            {
                                row.ChangedOn = now;
                            }
                            adapter.Update(dataChanges);
                        }
                    }
                }

                // Sync to PROD (22.06.2023, SME)
                if (CoreFC.GetNetzwerkTyp() != Enums.NetzwerkTyp.ProdNetz)
                {
                    SyncFontsInPROD();
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
                // clearing
                IsHandlingPendingFontChanges = false;
            }
        }

        // Handle File-Change (27.05.2023, SME)
        private void HandleFileChange(string filePath, FontsWithStreamTableAdapter adapter, FontsWithStreamDataTable table, DateTime now)
        {
            try
            {
                // create font-info
                var fontInfo = new FontInfo(filePath);

                // load data
                adapter.FillByPathPrefix(table, filePath);

                var rows = table.Where(x => x.RowState != DataRowState.Deleted && x.FontFilePath.ToLower().Equals(filePath.ToLower())).ToList();
                if (!rows.Any())
                {
                    // create
                    var row = table.NewFontsWithStreamRow();
                    row.AddedOn = now;
                    row.ChangedOn = now;
                    row.Synonyms = string.Empty;
                    row.UpdateInfos(fontInfo);
                    row.FileStream = File.ReadAllBytes(filePath);
                    row.Table.Rows.Add(row);
                }
                else
                {
                    // update
                    rows.ForEach(row =>
                    {
                        row.UpdateInfos(fontInfo);
                        if (row.RowState == DataRowState.Modified)
                        {
                            row.ChangedOn = now;
                            row.FileStream = File.ReadAllBytes(filePath);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Child-Objects

        #region Table of Fonts

        partial class FontsDataTable
        {
            // Fonts-Row anhand von Font-Objekt ermitteln (26.04.2023, SME)
            // CHANGE: 16.05.2023 by SME: ITcFont statt TcPdfFont
            // CHANGE: 17.05.2023 by SME: Parameter hinzugefügt => onlyOKFonts
            public FontDataDB.FontsRow GetBestFontRow(ITcFont fontToEmbed, bool onlyOKFonts = true)
            {
                return FC_Fonts.GetBestFontRow(this, fontToEmbed, onlyOKFonts);
            }
        }

        #endregion

        #region Row of Fonts

        partial class FontsRow
        {
            // Enthält Synonym (10.05.2023, SME)
            public bool ContainsSynonym(string synonym)
            {
                if (string.IsNullOrEmpty(synonym)) return false;
                if (string.IsNullOrEmpty(this.Synonyms)) return false;
                return (";" + this.Synonyms.ToLower() + ";").Contains(synonym.ToLower());
            }

            // Get Font-Stream (20.06.2023, SME)
            public byte[] GetFontStream() => FC_Fonts.GetFontStream(this);
        }

        #endregion

        #region Table of Fonts with Stream

        partial class FontsWithStreamRow
        {
            // Set Cell-Value (27.05.2023, SME)
            private void SetCellValue(DataColumn column, object value)
            {
                DataFC.SetCellValue(this, column, value);
            }

            // Update Infos from Font-Info
            internal void UpdateInfos(FontInfo fontInfo)
            {
                try
                {
                    // store table
                    var table = this.tableFontsWithStream;

                    // update infos
                    SetCellValue(table.FontNameColumn, fontInfo.FontName);
                    SetCellValue(table.FontFamilyColumn, fontInfo.FontFamily.Name);
                    SetCellValue(table.StyleColumn, fontInfo.FontStyle);
                    SetCellValue(table.WeightColumn, fontInfo.FontWeight);
                    SetCellValue(table.FontFileNameColumn, Path.GetFileNameWithoutExtension(fontInfo.FontFilePath));
                    SetCellValue(table.FontFileTypeColumn, Path.GetExtension(fontInfo.FontFilePath).ToLower());
                    SetCellValue(table.FontFilePathColumn, fontInfo.FontFilePath);

                    // last file-write-time
                    var file = new FileInfo(fontInfo.FontFilePath);
                    SetCellValue(table.LastFileWriteTimeColumn, file.LastWriteTime);

                    // error-message
                    if (fontInfo.InitializeError != null)
                    {
                        SetCellValue(table.ErrorMessageColumn, fontInfo.InitializeError.Message);
                    }
                    else
                    {
                        SetCellValue(table.ErrorMessageColumn, null);
                    }
                }
                catch (Exception ex)
                {
                    CoreFC.ThrowError(ex); throw ex;
                }
            }
        }

        #endregion

        #endregion
    }

}

namespace TC.Resources.Fonts.LIB.Data.FontDataDBTableAdapters
{
    partial class TableAdapterManager
    {

    }

    partial class FontsChangesTableAdapter
    {

    }
}