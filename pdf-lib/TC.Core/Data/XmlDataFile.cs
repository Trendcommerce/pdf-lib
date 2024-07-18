using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using TC.Attributes;
using TC.Functions;

namespace TC.Data
{
    #region XLM-Data-File

    // XML-Data-File (07.12.2022, SME)
    public abstract class XmlDataFile : IDisposable
    {
        #region General

        // New empy Instance or from Default-File-Path
        protected XmlDataFile()
        {
            // set local properties
            InstanceID = CoreFC.GetGlobalInstanceID(this);
            _FilePath = DefaultFilePath;
            DataSet = GetNewDataSet();
            AfterCreatingDataSet();
            ReadData();

            // restart file-system-watcher
            StartFileSystemWatcher();
        }

        // New Instance from Path
        protected XmlDataFile(string filePath)
        {
            // error-handling
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            // set local properties
            InstanceID = CoreFC.GetGlobalInstanceID(this);
            _FilePath = filePath;
            DataSet = GetNewDataSet();
            AfterCreatingDataSet();
            ReadData();

            // restart file-system-watcher
            StartFileSystemWatcher();
        }

        #endregion

        #region Dispose-Handling

        // Dispose (09.12.2022, SME)
        public void Dispose()
        {
            try
            {
                // stop file-system-watcher
                ClearFileSystemWatcher();

                // dispose dataset
                DataSet.Dispose();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
            finally
            {
                GC.Collect();
            }
        }

        #endregion

        #region MUST OVERRIDE

        // MUST OVERRIDE: Get new DataSet
        protected abstract DataSet GetNewDataSet();

        #endregion

        #region OVERRIDABLE

        // OVERRIDABLE: DataSetName
        protected virtual string DataSetName => this.GetType().Name;

        // OVERRIDABLE: DefaultFileName
        protected virtual string DefaultFileName => this.GetType().Name + DefaultFileType;

        // OVERRIDABLE: DefaultFolderPath
        protected virtual string DefaultFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TC", CoreFC.GetEntryAssemblyName());

        // OVERRIDABLE: Default File-Type
        public virtual string DefaultFileType => ".xml";

        // OVERRIDABLE: After creating DataSet
        protected virtual void AfterCreatingDataSet() { }

        // OVERRIDABLE: After reading Data
        protected virtual void AfterReadingData() { }

        // OVERRIDABLE: After rejecting Changes
        protected virtual void AfterRejectingChanges() { }

        #endregion

        #region Properties

        // Instance-ID
        public readonly int InstanceID;

        // DefaultFilePath
        public string DefaultFilePath => Path.Combine(DefaultFolderPath, DefaultFileName);

        // FilePath
        private string _FilePath;
        public string FilePath => _FilePath;

        // Has Changes
        public bool HasChanges => DataSet.HasChanges();

        #endregion

        #region Data

        // DataSet
        protected readonly DataSet DataSet;

        // Read Data
        private void ReadData()
        {
            if (File.Exists(FilePath))
            {
                // read xml
                DataSet.ReadXml(FilePath);

                // accept changes
                DataSet.AcceptChanges();

                // after read
                AfterReadingData();
            }
        }

        // Refresh Data
        private void RefreshData()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    // read xml
                    var ds = GetNewDataSet();
                    ds.ReadXml(FilePath);
                    ds.AcceptChanges();

                    // merge data
                    DataFC.MergeData(DataSet, ds);

                    // get changes
                    var changes = DataSet.GetChanges();
                    // check
                    if (changes == null || changes.Tables.Count == 0)
                        return;

                    // debug-info
                    CoreFC.DPrint("Data refreshed on " + DateTime.Now.ToString("HH:mm:ss.fffffff"));
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Methods

        // Reject Changes
        public bool RejectChanges()
        {
            try
            {
                // Reject Changes
                DataSet.RejectChanges();

                // After rejecting Changes
                AfterRejectingChanges();

                // return
                return true;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Save
        public bool Save()
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(FilePath)) throw new Exception("Dateipfad noch nicht gesetzt!");

                // save
                return SaveAs(FilePath);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Is-Saving-Flag
        private bool _IsSaving;
        private DateTime _LastSavedOn = DateTime.Now;

        // Save as
        public bool SaveAs(string path)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
                if (DataSet == null) throw new Exception("Datenset nicht gesetzt!");
                if (!HasChanges) return true;

                // make sure folder exists
                CoreFC.CreateFolder(Path.GetDirectoryName(path));

                // set flag
                _IsSaving = true;
                _LastSavedOn = DateTime.Now;

                // write xml
                DataSet.WriteXml(path);

                // accept changes
                DataSet.AcceptChanges();

                // update file-path
                if (FilePath != path)
                {
                    // set new value
                    _FilePath = path;

                    // restart file-system-watcher
                    StartFileSystemWatcher();
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
                // clear flag
                _IsSaving = false;
            }
        }

        #endregion

        #region File-System-Watcher

        // File-System-Watcher
        private FileSystemWatcher _FileSystemWatcher;

        private DateTime _LastFileSystemWatcherAction;

        // Clear File-System-Watcher
        private void ClearFileSystemWatcher()
        {
            // stop current watcher
            if (_FileSystemWatcher != null)
            {
                _FileSystemWatcher.EnableRaisingEvents = false;
                _FileSystemWatcher.Dispose();
                _FileSystemWatcher = null;
            }
        }

        // Start File-System-Watcher
        private void StartFileSystemWatcher()
        {
            try
            {
                // stop current watcher
                ClearFileSystemWatcher();

                // exit-handling
                if (string.IsNullOrEmpty(FilePath)) return;

                // store folder-path + file-name
                var folderPath = Path.GetDirectoryName(FilePath);
                var fileName = Path.GetFileName(FilePath);

                // make sure folder exists
                CoreFC.CreateFolder(folderPath);

                // start watcher
                _FileSystemWatcher = new FileSystemWatcher(folderPath, fileName);
                _FileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                _FileSystemWatcher.EnableRaisingEvents = true;
                _LastFileSystemWatcherAction = DateTime.Now;

                // add event-handlers
                _FileSystemWatcher.Created += FileSystemWatcher_Created;
                _FileSystemWatcher.Changed += FileSystemWatcher_Changed;
                _FileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
                _FileSystemWatcher.Error += FileSystemWatcher_Error;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Event-Handler: Error in FileSystemWatcher
        private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            CoreFC.DPrint("Error in FileSystem-Watcher: " + e.GetException().Message);
        }

        // Error-Handler: File created
        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            CoreFC.DPrint("File created: " + e.ChangeType.ToString() + " => " + e.FullPath);
        }

        // Error-Handler: File changed
        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var now = DateTime.Now;
            var diffSaved = now - _LastSavedOn;
            var diffAction = now - _LastFileSystemWatcherAction;
            _LastFileSystemWatcherAction = DateTime.Now;

            if (_IsSaving) return;
            if (diffSaved.TotalSeconds < 0.1) return;

            // IMPORTANT: Every Event happens at least 2 times, so always go for the last one (07.12.2022, SME)
            if (diffAction.TotalSeconds > 0.1) return;

            // if code comes here, it means that the data needs to be refreshed
            CoreFC.DPrint("File changed: " + e.ChangeType.ToString() + " => " + e.FullPath + ", Diff to last saved: " + diffSaved.TotalSeconds + " sec., Diff to last Action: " + diffAction.TotalSeconds + " sec.");
            RefreshDataWithDelay();
        }

        // Error-Handler: File renamed
        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            CoreFC.DPrint("File renamed: " + e.ChangeType.ToString() + " => " + e.FullPath + ", before => " + e.OldFullPath);
        }

        // Refresh-Timer
        private Timer _RefreshTimer;

        // Start Refresh-Timer
        private void RefreshDataWithDelay(double delayInMilliSeconds = 100)
        {
            try
            {
                // stop timer
                if (_RefreshTimer != null)
                {
                    _RefreshTimer.Stop();
                    _RefreshTimer.Dispose();
                    _RefreshTimer = null;
                }

                // start new timer
                _RefreshTimer = new Timer(delayInMilliSeconds);
                _RefreshTimer.Elapsed += RefreshTimer_Elapsed;
                _RefreshTimer.Start();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR while refreshing data with delay: ErrorType = {ex.GetType()}, ErrorMessage = {ex.Message}");
            }
        }

        // Event-Handler: Refresh-Timer elapsed
        private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                // stopp timer
                _RefreshTimer.Stop();
                
                // refresh data
                RefreshData();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion
    }

    #endregion

    #region XML-Data-File with Table-Enum

    // XML-Data-File (07.12.2022, SME)
    public abstract class XmlDataFile<TTableEnum> : XmlDataFile where TTableEnum : struct
    {
        #region General

        // New empy Instance or from Default-File-Path
        protected XmlDataFile() : base()
        {
            // error-handling
            if (!typeof(TTableEnum).IsEnum) throw new Exception("Ungültige Tabellen-Enumeration");
        }

        // New Instance from Path
        protected XmlDataFile(string filePath): base(filePath)
        {
            // error-handling
            if (!typeof(TTableEnum).IsEnum) throw new Exception("Ungültige Tabellen-Enumeration");
        }

        #endregion

        #region Data

        #region Get new Data

        // MUST OVERRIDE: Get new DataSet
        protected override DataSet GetNewDataSet()
        {
            try
            {
                // error-handling
                if (!typeof(TTableEnum).IsEnum) throw new Exception("Ungültige Tabellen-Enumeration");

                // create tables
                var list = new Dictionary<TTableEnum, DataTable>();
                CoreFC.GetEnumValues<TTableEnum>().ToList().ForEach(tbl => list.Add(tbl, GetNewTable(tbl)));

                // create dataset
                var ds = new DataSet(DataSetName);
                list.Values.ToList().ForEach(tbl => ds.Tables.Add(tbl));

                // return
                ds.AcceptChanges();
                return ds;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get new Table by Table-Enum
        protected virtual DataTable GetNewTable(TTableEnum tableEnum)
        {
            try
            {
                // get table-properties from attributes
                var tableProperties = CoreFC.GetCustomAttribute<TableEnumPropertiesAttribute>(tableEnum);
                // check
                if (tableProperties == null) throw new Exception($"Tabellen-Eigenschaften für '{tableEnum}' konnten nicht ermittelt werden!");

                // list of column-property by enum
                var list = new Dictionary<object, ColumnPropertiesAttribute>();

                // loop throu column-enums
                foreach (var columnEnum in System.Enum.GetValues(tableProperties.ColumnEnum))
                {
                    // get column-properties from attributes
                    var columnProperties = CoreFC.GetCustomAttribute<ColumnPropertiesAttribute>(columnEnum);
                    // check
                    if (columnProperties == null) throw new Exception($"Spalten-Eigenschaften für '{tableEnum}.{columnEnum}' konnten nicht ermittelt werden!");
                    // add to list
                    list.Add(columnEnum, columnProperties);
                }

                // create table
                var table = new DataTable(tableEnum.ToString());

                // add columns
                DataFC.AddNewAutoIdColumn(table);
                foreach (var item in list)
                {
                    try
                    {
                        var column = item.Value.GetNewColumn(item.Key.ToString());
                        table.Columns.Add(column);
                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }

                // set default-view-properties
                table.DefaultView.Sort = tableProperties.DefaultSort;

                // return
                table.AcceptChanges();
                return table;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        // Get Table by Table-Enum
        public DataTable GetTable(TTableEnum tableEnum)
        {
            if (DataSet == null) return null;
            return DataSet.Tables[tableEnum.ToString()];
        }

        // Get Data-View by Table-Enum + Filter
        public DataView GetView(TTableEnum tableEnum, string filter = "")
        {
            try
            {
                // get table
                var table = GetTable(tableEnum);
                if (table == null) return null;

                // create view
                var view = new DataView(table);

                // set properties
                view.AllowNew = table.DefaultView.AllowNew;
                view.AllowEdit = table.DefaultView.AllowEdit;
                view.AllowDelete = table.DefaultView.AllowDelete;
                view.RowFilter = filter;

                // return
                return view;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Data-Rows by Table-Enum + Filter
        public DataRow[] GetRows(TTableEnum tableEnum, string filter = "", string sort = "")
        {
            // get table
            var table = GetTable(tableEnum);
            if (table == null) return new DataRow[] { };

            // return
            return table.Select(filter, sort);
        }

        #endregion

        // Get Table-Enum from Row (21.11.2022, SME)
        public TTableEnum? GetTableEnum(DataRow row)
        {
            if (row == null) return null;
            if (row.Table == null) return null;
            return CoreFC.GetEnumValue<TTableEnum>(row.Table.TableName);
        }
    }

    #endregion
}
