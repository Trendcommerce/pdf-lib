using System;
using System.Data;
using System.IO;
using System.Linq;
using TC.Functions;

namespace TC.PDF.LIB.Data
{
    partial class PdfToDos
    {

        partial class PDFsRow
        {
            // Get pending ToDo-Rows (04.07.2023, SME)
            public ToDosRow[] GetPendingToDoRows()
            {
                return GetToDosRows().Where(t => t.IsStatusNull() || !t.Status).ToArray();
            }

            // Has pending ToDo-Rows (04.07.2023, SME)
            public bool HasPendingToDoRows() => GetPendingToDoRows().Any();

            // Perform Done-Actions (05.07.2023, SME)
            public bool PerformDoneActions()
            {
                bool moveToDone = false;

                try
                {
                    // store todos
                    PdfToDos todos = tablePDFs.DataSet as PdfToDos;
                    if (todos == null) throw new Exception("PDF-ToDo-Datenset konnte nicht ermittelt werden!");

                    // => Folge-Aktionen ausführen
                    foreach (var doneAction in todos.DoneActions)
                    {
                        switch (doneAction.Action)
                        {
                            case "ClearBackups":
                                // Clear Backups

                                foreach (var backupFilePath in FC_PDF.GetBackupFilePaths(this.FilePath))
                                {
                                    CoreFC.DeleteFile(backupFilePath, true);
                                }

                                break;
                            case "MoveTo":
                                // Move To

                                // check file-locked with wait (10.07.2024, SME)
                                CoreFC.IsFileLocked_WaitMaxSeconds(this.FilePath, 5);

                                // Loop throu Parameters
                                foreach (var paramNameValue in doneAction.Parameters.Split(';'))
                                {
                                    try
                                    {
                                        // Split Name/Value
                                        var nameValue = paramNameValue.Split('=');
                                        var value = nameValue.Last();
                                        switch (nameValue.First())
                                        {
                                            case "FolderPath":
                                                if (!Directory.Exists(value))
                                                {
                                                    if (Directory.Exists(Path.GetDirectoryName(value)))
                                                    {
                                                        Directory.CreateDirectory(value);
                                                    }
                                                }
                                                if (Directory.Exists(value))
                                                {
                                                    // move file
                                                    var moveTo = Path.Combine(value, this.FileName);
                                                    if (File.Exists(moveTo)) File.Delete(moveTo);
                                                    File.Move(this.FilePath, moveTo);
                                                    // move todo-file
                                                    moveToDone = true;
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        CoreFC.ThrowError(ex); throw ex;
                                    }
                                }

                                break;
                            default:
                                break;
                        }
                    }

                    // return
                    return moveToDone;
                }
                catch (Exception ex)
                {
                    CoreFC.ThrowError(ex); throw ex;
                }
            }

            // Save ToDo's (04.07.2023, SME)
            public bool SaveToDos()
            {
                try
                {
                    // store todos
                    PdfToDos todos = tablePDFs.DataSet as PdfToDos;
                    if (todos == null) throw new Exception("PDF-ToDo-Datenset konnte nicht ermittelt werden!");

                    // save todo's
                    var todoFilePath = this.FilePath + ".todo";
                    todos.WriteXml(todoFilePath, XmlWriteMode.IgnoreSchema);

                    // declarations
                    bool moveToDone = false;

                    // check if no more todo's
                    if (!HasPendingToDoRows())
                    {
                        // alles erledigt
                        // => Folge-Aktionen ausführen
                        moveToDone = PerformDoneActions();
                    }

                    // move to done
                    if (moveToDone)
                    {
                        // move to done
                        var doneFolderPath = Path.Combine(Path.GetDirectoryName(todoFilePath), "Done");
                        if (!Directory.Exists(doneFolderPath)) Directory.CreateDirectory(doneFolderPath);
                        var fileExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(todoFilePath)) + Path.GetExtension(todoFilePath);
                        var doneFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(todoFilePath));
                        doneFileName += ", " + DateTime.Now.ToString("yyyy-MM-dd, HH-mm-ss") + fileExtension;
                        var doneFilePath = Path.Combine(doneFolderPath, doneFileName);
                        if (File.Exists(doneFilePath)) File.Delete(doneFilePath);
                        File.Move(todoFilePath, doneFilePath);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    CoreFC.ThrowError(ex); throw ex;
                }
            }
        }

        partial class ToDosRow
        {
            // Get next ToDo-Row (03.07.2023, SME)
            public ToDosRow GetNextToDoRow()
            {
                var index = tableToDos.Rows.IndexOf(this);
                if (index < tableToDos.Rows.Count - 1)
                {
                    return tableToDos.Rows[index + 1] as ToDosRow;
                }
                else
                {
                    // it's the last one
                    return null;
                }
            }

            // ToDo-Type-Enum (04.07.2023, SME)
            public PdfToDoTypeEnum? ToDoTypeEnum
            {
                get
                {
                    return CoreFC.GetEnumValue<PdfToDoTypeEnum>(this.ToDoType);
                }
                set
                {
                    if (!value.HasValue) throw new System.ArgumentNullException(nameof(ToDoTypeEnum));

                    this.ToDoType = value.ToString();
                }
            }

            // Is Warning (04.07.2023, SME)
            public bool IsWarning()
            {
                return ToDoTypeEnum.HasValue && ToDoTypeEnum.Value == PdfToDoTypeEnum.Warning;
            }

            // Is Warning of unembedded Font (04.07.2023, SME)
            public bool IsWarningOfNotEmbeddedFont()
            {
                if (!IsWarning()) return false;
                if (string.IsNullOrEmpty(ToDoInfoDetails)) return false;
                if (!ToDoInfoDetails.StartsWith("Action=NotEmbedded;")) return false;
                return true;
            }

            // Is Warning of unembedded Type3-Font (04.07.2023, SME)
            public bool IsWarningOfNotEmbeddedType3Font()
            {
                if (!IsWarningOfNotEmbeddedFont()) return false;
                if (!ToDoInfoDetails.Split(';').Contains("FontSubType=Type3")) return false;
                return true;
            }

            // Get pending Page-Rows (04.07.2023, SME)
            public ToDoPagesRow[] GetPendingToDoPagesRow()
            {
                return GetToDoPagesRows().Where(t => t.IsStatusNull() || !t.Status).ToArray();
            }

            // Has pending Page-Rows (04.07.2023, SME)
            public bool HasPendingToDoPagesRow() => GetPendingToDoPagesRow().Any();

            // Get ToDo-Info-Detail-Part (20.07.2023, SME)
            public string GetToDoInfoDetailPart(string partName)
            {
                if (string.IsNullOrEmpty(partName)) return string.Empty;
                if (string.IsNullOrEmpty(ToDoInfoDetails)) return string.Empty;
                return ToDoInfoDetails.Split(';').FirstOrDefault(x => x.StartsWith(partName + "="));
            }

            // Get ToDo-Info-Detail-Part-Value (20.07.2023, SME)
            public string GetToDoInfoDetailPartValue(string partName)
            {
                string part = GetToDoInfoDetailPart(partName);
                if (string.IsNullOrEmpty(part)) return string.Empty;
                return part.Substring(partName.Length + 1);
            }
        }

        partial class ToDoPagesRow
        {
            // Get next Page-Row (03.07.2023, SME)
            public ToDoPagesRow GetNextPageRow(bool onlyPending = false)
            {
                var rows = tableToDoPages.Where(x => x.ToDo_ID == this.ToDo_ID && (!onlyPending || x.IsStatusNull())).ToList();
                var index = rows.IndexOf(this);
                if (index < rows.Count - 1)
                {
                    return rows[index + 1] as ToDoPagesRow;
                }
                else
                {
                    // it's the last one
                    return null;
                }
            }
        }
    }
}
