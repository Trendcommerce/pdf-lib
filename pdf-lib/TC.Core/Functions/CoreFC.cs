using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using TC.Classes;
using TC.Constants;
using TC.Enums;
using static TC.Constants.CoreConstants;

namespace TC.Functions
{
    // Functions (11.11.2022, SME)
    public static class CoreFC
    {

        #region IMPORTANT

        // Throw Error (12.05.2023, SME)
        public static void ThrowError(Exception ex)
        {
            if (ex != null)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }

        // Get Source-Error (11.04.2024, SME)
        public static Exception GetSourceError(Exception ex)
        {
            if (ex == null) return null;
            else return ExceptionDispatchInfo.Capture(ex).SourceException;
        }

        // Get Multi-Error (11.04.2024, SME)
        public static AggregateException GetMultiError(string msg, params Exception[] errors)
        {
            // Liste der Inner-Exceptions zusammenstellen
            var errorList = new List<Exception>();
            if (errors != null && errors.Any())
            {
                foreach (var error in errors)
                {
                    errorList.Add(CoreFC.GetSourceError(error));
                }
            }

            // Multi-Error auslösen
            return new AggregateException(msg, errorList.ToArray());
        }

        // XString (15.11.2022, SME)
        public static string XString(string text, int count)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(text)) return string.Empty;
                if (count == 0) return string.Empty;
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

                var sb = new StringBuilder();
                for (int i = 0; i < count; i++)
                {
                    sb.Append(text);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Lines  (30.11.2022, SME)
        public static string Lines(int count = 1) => XString(Environment.NewLine, count);
        
        // Get User-Name (17.12.2023, SME)
        public static string GetUserName(bool addDebugPrefixWhenDebuggerAttached = true)
        {
            var user = Environment.UserName;
            if (Debugger.IsAttached && addDebugPrefixWhenDebuggerAttached)
            {
                return user + "_Debug";
            }
            else
            {
                return user;
            }
        }

        // Is DEV-Task-User (21.02.2024, SME)
        public static bool IsDevTaskUser()
        {
            if (CoreFC.GetUserName().ToUpper().Equals(DEVTASK))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Is identical IEnumerable (25.11.2022, SME)
        public static bool IsIdenticalIEnumerable<T>(IEnumerable<T> ienumerable1, IEnumerable<T> ienumerable2)
        {
            if (ienumerable1 == null && ienumerable2 == null) return true;
            if (ienumerable1 == null || ienumerable2 == null) return false;
            if (ienumerable1.Count() != ienumerable2.Count()) return false;
            for (int i = 0; i < ienumerable1.Count(); i++)
            {
                if (!ienumerable1.ElementAt(i).Equals(ienumerable2.ElementAt(i)))
                    return false;
            }
            return true;
        }

        // IifNullOrDbNull (27.11.2022, SME)
        public static T IifNullOrDbNull<T>(object value, T nullValue)
        {
            try
            {
                if (value == null) return nullValue;
                if (value == DBNull.Value) return nullValue;
                return (T)value;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Wait (12.04.2023, SME)
        public static void Wait(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        // Get calling Method (14.04.2023, SME)
        public static System.Reflection.MethodBase GetCallingMethod()
        {
            // get method-info of current method (GetCallingMethod)
            // var method = System.Reflection.MethodBase.GetCurrentMethod();

            // StackFrame(1).GetMethod gives Method-Info of current Method
            // StackFrame(2).GetMethod gives Method-Info of Method that called this Method
            return new System.Diagnostics.StackFrame(2).GetMethod();
        }

        // Is Numeric (20.04.2023, SME)
        public static bool IsNumeric(object expression)
        {
            return Microsoft.VisualBasic.Information.IsNumeric(expression);
        }

        // Get Full-Error-Message (09.04.2024, SME)
        public static string GetFullErrorMessage(Exception error)
        {
            // exit-handling
            if (error == null) return string.Empty;
            if (error.InnerException == null) return error.Message;

            // initialize return-value with error-message
            var sb = new StringBuilder();
            sb.AppendLine(error.Message);

            // add all inner exceptions
            var innerError = error.InnerException;
            while(innerError != null)
            {
                sb.AppendLine();
                sb.AppendLine(innerError.Message);
                innerError = innerError.InnerException;
            }

            // return
            return sb.ToString().Trim();
        }

        // Get File-Size (30.05.2024, SME)
        public static long GetFileSize(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return -1;
            if (!File.Exists(filePath)) return -1;
            return new FileInfo(filePath).Length;
        }

        // Get Size from File-Stream (30.05.2024, SME)
        public static long GetFileStreamSize(string filePath, bool throwErrorBack = true)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath)) return -1;
                if (!File.Exists(filePath)) return -1;
                
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    return fs.Length;
                }
            }
            catch (Exception ex)
            {
                if (throwErrorBack)
                {
                    ThrowError(ex); throw ex;
                }
                else
                {
                    return -1;
                }
            }
        }

        // Is File Read-Only (05.07.2024, SME)
        public static bool IsFileReadOnly(string filePath, bool throwFileMissingException = false)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                if (throwFileMissingException)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }
                return false;
            }
            else if (!File.Exists(filePath))
            {
                if (throwFileMissingException)
                {
                    throw new FileNotFoundException("Datei wurde nicht gefunden!" + CoreFC.Lines() + "Pfad:" + CoreFC.Lines() + filePath, filePath);
                }
                return false;
            }
            else
            {
                var file = new FileInfo(filePath);
                if (file.IsReadOnly) return true;
                else return false;
            }
        }

        /// <summary>
        /// Prüft, ob eine Datei von einem anderen Prozess blockiert wird (max. 10 mal während einer Sekunde)
        /// <paramref name="path2File"/>
        /// <param name="throwFileMissingException">Soll ein Error geworfen werden, wenn das File nicht vorhanden ist?</param>
        /// </summary>
        public static bool IsFileLocked
            (string path2File, bool throwFileMissingException = false,
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "", 
            [CallerFilePath] string callerFilePath2 = "", [CallerLineNumber] int callerLineNumber2 = 0, [CallerMemberName] string callerMemberName2 = "")
        {
            bool locked = true;
            if (File.Exists(path2File))
            {
                int i = 0;
                while (locked && i < 10)
                {
                    try
                    {
                        using (FileStream fs = new(path2File, FileMode.Open, FileAccess.ReadWrite)) { fs.Close(); fs.Dispose(); }
                        locked = false;
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(50);
                        List<string> lst = new() { $"{DateTime.Now.ToString(TimeStamp)}  |  {Path.GetFileName(path2File)}  |  {Path.GetFileNameWithoutExtension(callerFilePath)}.{callerMemberName} Zeile {callerLineNumber}:", ex.Message };
                        if (callerFilePath != callerFilePath2 || callerLineNumber != callerLineNumber2 || callerMemberName != callerMemberName2)
                        {
                            lst.Insert(1, $"{Path.GetFileNameWithoutExtension(callerFilePath2)}.{callerMemberName2} Zeile {callerLineNumber2}");
                        }
                        //try
                        //{
                        //    var logFolder = Path.GetDirectoryName(LogFile);
                        //    if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
                        //    File.AppendAllLines(LogFile, lst);
                        //}
                        //catch (Exception logex)
                        //{
                        //    ClsError.WriteErrorHTML(new Exception(string.Join(Environment.NewLine, lst), logex));
                        //}
                        Thread.Sleep(50);
                        i++;
                    }
                }
            }
            else
            {
                locked = false;
                if (throwFileMissingException)
                {
                    throw new System.IO.FileNotFoundException($"Datei '{Path.GetFileName(path2File)}' ist nicht vorhanden.{Environment.NewLine}{Path.GetFileName(path2File)}  |  {Path.GetFileNameWithoutExtension(callerFilePath2)}.{callerMemberName2} Zeile {callerLineNumber2}{Environment.NewLine}{Path.GetFileName(path2File)}  |  {Path.GetFileNameWithoutExtension(callerFilePath)}.{callerMemberName} Zeile {callerLineNumber}");
                }
            }
            return locked;
        }

        /// <summary>
        /// Prüft für maximal Sekunden, ob eine Datei von einem anderen Prozess blockiert wird
        /// </summary>
        /// <param name="path2File"></param>
        /// <param name="maxSeconds"></param>
        /// <param name="throwFileMissingException">Soll ein Error geworfen werden, wenn das File nicht vorhanden ist?</param>
        /// <returns></returns>
        public static bool IsFileLocked_WaitMaxSeconds
            (string path2File, int maxSeconds = 3, bool throwFileMissingException = false, 
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            maxSeconds = maxSeconds.Equals(0) ? 1 : maxSeconds;
            bool locked = true;
            for (int i = 0; i < maxSeconds; i++)
            {
                locked = IsFileLocked(path2File, throwFileMissingException, callerFilePath, callerLineNumber, callerMemberName);
                if (!locked) { break; }
            }
            return locked;
        }

        // Is File-Locked-Error (02.01.2024, SME)
        public static bool IsFileLockedError(Exception ex)
        {
            if (ex == null) return false;
            if (ex is IOException && 
                (ex.Message == "The process cannot access the file because it is being used by another process." 
                || ex.Message == "Der Prozess kann nicht auf die Datei zugreifen, da sie bereits von einem anderen Prozess verwendet wird."))
            {
                return true;
            }
            if (ex is TC.Errors.FileLockedError) return true;
            return false;
        }

        #region Netzwerk-Typ

        /// <summary>
        /// Befinden wir uns im Client- oder im Prod-Netz?
        /// </summary>
        /// <returns></returns>
        public static NetzwerkTyp GetNetzwerkTyp()
        {
            try
            {
                // Host-Name zwischenspeichern
                var hostName = Dns.GetHostName();

                // Host-Entry zwischenspeichern
                var hostEntry = Dns.GetHostEntry(hostName);

                // IP-Adressen zwischenspeichern
                var addressList = hostEntry.AddressList;

                // Erstes internes Netzwerk zwischenspeichern
                var firstInternNetwork = addressList.First(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                // Relevanter Teil zwischenspeichern
                int part = Convert.ToInt32(firstInternNetwork.ToString().Split(DOT_Char)[2]);

                // Rückgabe
                if (part == 5) return NetzwerkTyp.ProdNetz;
                else return NetzwerkTyp.ClientNetz;
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }
            //=> Convert.ToInt32(Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString().Split(DOT_Char)[2]) == 5
            //? NetzwerkTyp.ProdNetz
            //: NetzwerkTyp.ClientNetz;

        #endregion

        #endregion

        #region Debug

        #region DPrint

        // DPrint = Console.WriteLine (21.06.2023, SME)
        public static void DPrint(string info)
        {
            Console.WriteLine(info);
        }

        #endregion

        #region Write

        // Write Debug-Info (11.11.2022, SME)
        public static void WriteDebugInfo(string info)
        {
            Global.Global_TC_Core.GlobalDebugInfoHandler.WriteInfo(info);
        }

        // Write Error (11.11.2022, SME)
        public static void WriteError(System.Exception error, System.Reflection.MethodBase method)
        {
            Global.Global_TC_Core.GlobalDebugInfoHandler.WriteError(error, method);
        }

        // Write Event-Info (11.11.2022, SME)
        public static void WriteEventInfo(MethodBase method, object sender, EventArgs e)
        {
            Global.Global_TC_Core.GlobalDebugInfoHandler.WriteEventInfo(method, sender, e);
        }

        #endregion

        #region Print

        // Print Column-Infos to Console (11.11.2022, SME)
        public static void Print_ColumnInfos(DataTable table)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("==================================================");
            sb.AppendLine("Table: " + table.TableName);
            sb.AppendLine("Columns:");
            foreach (DataColumn column in table.Columns)
            {
                sb.AppendLine("- " + column.ColumnName + ", " + column.DataType.Name + ", " + column.MaxLength);
            }
            sb.AppendLine("==================================================");

            WriteDebugInfo(sb.ToString().TrimEnd());
        }

        #endregion

        #endregion

        #region Attribute-Functions

        // get custom-attribute of value (04.12.2022, SME)
        public static TAttributeType GetCustomAttribute<TAttributeType>(object value)
        {
            try
            {
                if (value == null) 
                    throw new ArgumentNullException(nameof(value));
                if (!value.GetType().IsEnum) 
                    return value.GetType().GetCustomAttributes(false).OfType<TAttributeType>().FirstOrDefault();

                var field = value.GetType().GetField(value.ToString());
                if (field == null) 
                    throw new Exception("Enumerations-Wert nicht gefunden: " + value.ToString());

                return field.GetCustomAttributes(false).OfType<TAttributeType>().FirstOrDefault();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region Enum-Functions

        // Get Enum-Values (17.11.2022, SME)
        public static IEnumerable<TEnumType> GetEnumValues<TEnumType>(params TEnumType[] enumValuesToExclude)
        {
            if (enumValuesToExclude == null || !enumValuesToExclude.Any())
                return Enum.GetValues(typeof(TEnumType)).OfType<TEnumType>();
            else
                return Enum.GetValues(typeof(TEnumType)).OfType<TEnumType>().Where(enumValue => !enumValuesToExclude.Contains(enumValue));
        }

        // Get Enum-Value from String (17.11.2022, SME)
        public static Nullable<TEnumType> GetEnumValue<TEnumType>(string text) where TEnumType : struct
        {
            if (string.IsNullOrEmpty(text)) return null;
            TEnumType value;
            if (!Enum.TryParse(text, out value)) return null;
            return (Nullable<TEnumType>)value;
        }

        // Get Enum-Values to Value-Caption-Array (04.12.2022, SME)
        public static IEnumerable<ValueCaption<TEnumType>> GetEnumValuesToValueCaptions<TEnumType>(params TEnumType[] enumValuesToExclude)
        {
            List<ValueCaption<TEnumType>> list = new();
            foreach (var enumValue in GetEnumValues<TEnumType>(enumValuesToExclude))
            {
                list.Add(new(enumValue, GetCaption(enumValue)));
            }
            return list;
        }

        #endregion

        #region GetCaption

        // Get Caption of Value (Handle Caption-Attribute) (29.11.2022, SME)
        public static string GetCaption(object value)
        {
            try
            {
                if (value == null) return string.Empty;

                //if (!value.GetType().IsEnum) return value.ToString();

                //var field = value.GetType().GetField(value.ToString());
                //if (field == null) return value.ToString();
                //var attributes = field.GetCustomAttributes(false);
                //if (attributes == null) return value.ToString();

                var caption = GetCustomAttribute<Attributes.CaptionAttribute>(value);
                if (caption == null) 
                    return value.ToString();
                return caption.Caption;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region IsType-Functions

        // Is Decimal Type (31.10.2022, SRM)
        public static bool IsDecimalType(Type type)
        {
            if (type == typeof(float)) return true;
            if (type == typeof(double)) return true;
            if (type == typeof(decimal)) return true;
            return false;
        }

        // Is DateTime Type (31.10.2022, SRM)
        public static bool IsDateTimeType(Type type)
        {
            if (type == typeof(DateTime)) return true;
            if (type == typeof(DateTimeOffset)) return true;
            return false;
        }

        // Is Numeric Type (31.10.2022, SRM)
        public static bool IsNumericType(Type type)
        {
            if (type == typeof(byte)) return true;
            if (type == typeof(sbyte)) return true;
            if (type == typeof(short)) return true;
            if (type == typeof(ushort)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(uint)) return true;
            if (type == typeof(long)) return true;
            if (type == typeof(ulong)) return true;
            return IsDecimalType(type);
        }

        #endregion

        #region Dictionary-Functions

        // Get Dictionary to String (21.11.2022, SME)
        public static string GetDictionaryToString(Dictionary<string, string> dictionary, string keyValueDelimiter = CoreConstants.DEF_KeyValueDelimiter, string itemDelimiter = CoreConstants.DEF_ItemDelimiter)
        {
            try
            {
                // exit-handling
                if (dictionary == null) return string.Empty;
                if (!dictionary.Any()) return string.Empty;
                if (string.IsNullOrEmpty(keyValueDelimiter)) throw new ArgumentNullException(nameof(keyValueDelimiter));
                if (string.IsNullOrEmpty(itemDelimiter)) throw new ArgumentNullException(nameof(itemDelimiter));

                // create string-builder
                StringBuilder sb = new();

                // loop throu items
                foreach (var item in dictionary)
                {
                    if (item.Key.Contains(keyValueDelimiter)) throw new Exception("Key '" + item.Key + "' contains delimiter '" + keyValueDelimiter + "'!");
                    if (item.Key.Contains(itemDelimiter)) throw new Exception("Key '" + item.Key + "' contains delimiter '" + itemDelimiter + "'!");
                    if (item.Value.Contains(keyValueDelimiter)) throw new Exception("Value '" + item.Value + "' contains delimiter '" + keyValueDelimiter + "'!");
                    if (item.Value.Contains(itemDelimiter)) throw new Exception("Value '" + item.Value + "' contains delimiter '" + itemDelimiter + "'!");

                    sb.Append(item.Key + keyValueDelimiter + item.Value + itemDelimiter);
                }

                // remove last delimiter
                sb = sb.Remove(sb.Length - itemDelimiter.Length, itemDelimiter.Length);

                // return
                return sb.ToString();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Dictionary to DataTable (01.12.2022, SME)
        public static DataTable GetDictionaryToDataTable(Dictionary<string, string> dictionary, string keyColumnName = "Key", string valueColumnName = "Value")
        {
            try
            {
                // exit-handling
                if (dictionary == null) return null;
                if (string.IsNullOrEmpty(keyColumnName)) throw new ArgumentNullException(nameof(keyColumnName));
                if (string.IsNullOrEmpty(valueColumnName)) throw new ArgumentNullException(nameof(valueColumnName));
                if (keyColumnName == valueColumnName) throw new Exception("Key- und Wert-Spaltenname dürfen nicht identisch sein!");

                // create new table
                var table = new DataTable("DictionaryTable");

                // add columns
                var column = DataFC.AddNewColumn(table, keyColumnName, typeof(string), true, true, true);
                table.PrimaryKey = new DataColumn[] { column };
                DataFC.AddNewColumn(table, valueColumnName, typeof(string));

                // set default-view-properties
                table.DefaultView.Sort = keyColumnName;
                table.DefaultView.AllowNew = false;
                table.DefaultView.AllowEdit = false;
                table.DefaultView.AllowDelete = false;

                // add rows
                foreach (var keyValue in dictionary)
                {
                    var row = table.NewRow();
                    row[0] = keyValue.Key;
                    row[1] = keyValue.Value;
                    table.Rows.Add(row);
                }

                // return
                table.AcceptChanges();
                return table;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Dictionary from String (21.11.2022, SME)
        public static Dictionary<string, string> GetDictionaryFromString(string value, string keyValueDelimiter = CoreConstants.DEF_KeyValueDelimiter, string itemDelimiter = CoreConstants.DEF_ItemDelimiter)
        {
            try
            {
                // create dictionary
                Dictionary<string, string> dict = new();

                // exit-handling
                if (string.IsNullOrEmpty(value)) return dict;
                if (string.IsNullOrEmpty(keyValueDelimiter)) throw new ArgumentNullException(nameof(keyValueDelimiter));
                if (string.IsNullOrEmpty(itemDelimiter)) throw new ArgumentNullException(nameof(itemDelimiter));

                // loop throu key-value-pairs
                foreach (var keyValuePair in value.Split(itemDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        // split key and value
                        var keyAndValue = keyValuePair.Split(keyValueDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        // handle length
                        switch (keyAndValue.Length)
                        {
                            case 0:
                                throw new Exception("Invalid Dictionary-String: " + value);
                            case 1:
                                // only key, no value
                                dict.Add(keyAndValue.First(), string.Empty);
                                break;
                            case 2:
                                // key and value
                                dict.Add(keyAndValue.First(), keyAndValue.Last());
                                break;
                            default:
                                throw new Exception("Invalid Dictionary-String: " + value);
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFC.ThrowError(ex); throw ex;
                    }
                }

                // return
                return dict;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Dictionary from DataTable (01.12.2022, SME)
        public static Dictionary<string, string> GetDictionaryFromDataTable(DataTable dataTable, string keyColumnName = "Key", string valueColumnName = "Value")
        {
            try
            {
                // exit-handling
                if (dataTable == null) return null;
                if (string.IsNullOrEmpty(keyColumnName)) throw new ArgumentNullException(nameof(keyColumnName));
                if (string.IsNullOrEmpty(valueColumnName)) throw new ArgumentNullException(nameof(valueColumnName));
                if (keyColumnName == valueColumnName) throw new Exception("Key- und Wert-Spaltenname dürfen nicht identisch sein!");
                if (!dataTable.Columns.Contains(keyColumnName)) throw new Exception("Spalte '" + keyColumnName + "' nicht gefunden in Tabelle '" + dataTable.TableName + "'");
                if (!dataTable.Columns.Contains(valueColumnName)) throw new Exception("Spalte '" + valueColumnName + "' nicht gefunden in Tabelle '" + dataTable.TableName + "'");

                // create dictionary
                Dictionary<string, string> dictionary = new();

                // loop throu rows
                foreach (DataRow row in dataTable.Select())
                {
                    dictionary.Add(row[keyColumnName].ToString(), row[valueColumnName].ToString());
                }

                // return
                return dictionary;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region File-System-Functions

        #region FSO-Size-Functions

        // get optimal FSO-Size-Type (31.03.2023, SME)
        public static FsoSizeTypeEnum GetFsoSizeTypeOptimal(long FsoSize)
        {
            try
            {
                if (FsoSize <= 0)
                    return FsoSizeTypeEnum.Byte;
                else
                {
                    foreach (var eFsoSizeType in GetEnumValues<FsoSizeTypeEnum>().OrderByDescending(f => System.Convert.ToInt64(f)))
                    {
                        var size = (decimal)FsoSize / System.Convert.ToDecimal(eFsoSizeType);
                        if (FsoSize / System.Convert.ToInt64(eFsoSizeType) >= 0.9)
                        {
                            return eFsoSizeType;
                        }
                    }
                    return FsoSizeTypeEnum.Byte;
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // get FSO-Size-String (31.03.2023, SME)
        public static string GetFsoSizeString(long FsoSize, FsoSizeTypeEnum FsoSizeType, int Digits = -1)
        {
            try
            {
                // make sure fso-size-type is set
                if (FsoSizeType == FsoSizeTypeEnum.None)
                    FsoSizeType = FsoSizeTypeEnum.Byte;

                // set digits if necessary (-1 means auto)
                if (Digits == -1)
                {
                    if (FsoSizeType == FsoSizeTypeEnum.Byte)
                        Digits = 0;
                    else if (FsoSizeType == FsoSizeTypeEnum.KB)
                        Digits = 0;
                    else
                        Digits = 2;
                }

                // Calculate FSO-Size in given FSO-Size-Type
                double nFsoSize = Convert.ToDouble(FsoSize) / Convert.ToDouble(FsoSizeType);

                // Set Return-Value
                var sReturn = string.Empty;
                if (Digits > -1)
                    sReturn = Math.Round(nFsoSize, Digits).ToString();
                else
                    sReturn = nFsoSize.ToString();
                if (FsoSizeType == FsoSizeTypeEnum.Byte)
                {
                    if (nFsoSize == 1)
                        sReturn += " Byte";
                    else
                        sReturn += " Bytes";
                }
                else
                    sReturn += " " + FsoSizeType.ToString();

                return sReturn;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // get optimal FSO-Size-String (31.03.2023, SME)
        public static string GetFsoSizeStringOptimal(long FsoSize, int Digits = -1)
        {
            try
            {
                return GetFsoSizeString(FsoSize: FsoSize, Digits: Digits, FsoSizeType: GetFsoSizeTypeOptimal(FsoSize));
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region UNC

        // DLL-Import to get UNC-Path (27.10.2023, SME)
        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WNetGetConnection(
            [MarshalAs(UnmanagedType.LPTStr)] string localName,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName,
            ref int length);

        // Is UNC-Path (27.10.2023, SME)
        public static bool IsUNCPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!path.StartsWith(UncPathPrefix)) return false;
            if (File.Exists(path)) return true;
            if (Directory.Exists(path)) return true;
            return false;
        }

        // Get UNC-Path (27.10.2023, SME)
        // copied from https://gist.github.com/ambyte/01664dc7ee576f69042c
        /// <summary>
        /// Given a path, returns the UNC path or the original. (No exceptions
        /// are raised by this function directly). For example, "P:\2008-02-29"
        /// might return: "\\networkserver\Shares\Photos\2008-02-09"
        /// </summary>
        /// <param name="originalPath">The path to convert to a UNC Path</param>
        /// <returns>A UNC path. If a network drive letter is specified, the
        /// drive letter is converted to a UNC or network path. If the 
        /// originalPath cannot be converted, it is returned unchanged.</returns>
        public static string GetUNCPath(string originalPath)
        {
            StringBuilder sb = new StringBuilder(512);
            int size = sb.Capacity;

            // look for the {LETTER}: combination ...
            if (originalPath.Length > 2 && originalPath[1] == ':')
            {
                // don't use char.IsLetter here - as that can be misleading
                // the only valid drive letters are a-z && A-Z.
                char c = originalPath[0];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                {
                    int error = WNetGetConnection(originalPath.Substring(0, 2),
                        sb, ref size);
                    if (error == 0)
                    {
                        DirectoryInfo dir = new DirectoryInfo(originalPath);

                        string path = Path.GetFullPath(originalPath)
                            .Substring(Path.GetPathRoot(originalPath).Length);
                        return Path.Combine(sb.ToString().TrimEnd(), path);
                    }
                }
            }

            return originalPath;
        }

        // Get UNC-Path (27.10.2023, SME)
        //public static string GetUNCPath(string path)
        //{
        //    try
        //    {
        //        // exit-handling
        //        if (string.IsNullOrEmpty(path)) return string.Empty;
        //        if (!File.Exists(path) && !Directory.Exists(path))
        //        {
        //            throw new Exception("Nicht existierender Pfad: " + path);
        //        }
        //        if (IsUNCPath(path)) return path;

        //        // store drive-letter
        //        string driveLetter = path.Substring(0, 3);

        //        // find drive + check drive-type
        //        var drive = DriveInfo.GetDrives().FirstOrDefault(x => x.Name == driveLetter);
        //        if (drive == null) return path;
        //        if (drive.DriveType != System.IO.DriveType.Network) return path;

        //        // get unc-path
        //        string uncName = Strings.Space(160);
        //        int length = uncName.Length;
        //        int result = WNetGetConnection(path.Substring(0, 2), uncName, ref length);
        //        if (result != 0) return path;
        //        var uncPath = new StringBuilder();
        //        uncName = uncName.Trim();
        //        foreach (var c in uncName)
        //        {
        //            int asciiValue = Strings.Asc(c);
        //            if (asciiValue > 0)
        //                uncPath.Append(c);
        //            else
        //                break;
        //        }
        //        uncPath.Append(path.Substring(2));

        //        // return
        //        return uncPath.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        ThrowError(ex); throw ex;
        //    }
        //}

        #endregion

        // Get Startup-Filepath (01.12.2022, SME)
        public static string GetStartupFilePath() => Assembly.GetEntryAssembly().Location;

        // Get Startup-Folderpath (01.12.2022, SME)
        public static string GetStartupFolderPath() => Path.GetDirectoryName(GetStartupFilePath());

        // Create Folder (if necessary) (07.12.2022, SME)
        public static void CreateFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var folder = new DirectoryInfo(path);
            if (folder.Exists) return;

            try
            {
                folder.Create();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Find Files in Folder (08.12.2022, SME)
        public static List<FileInfo> FindFilesInFolder(DirectoryInfo folder, string filter, bool recursive = false)
        {
            try
            {
                // error-handling
                if (folder == null) throw new ArgumentNullException(nameof(folder));
                if (string.IsNullOrEmpty(filter)) throw new ArgumentNullException(nameof(filter));
                if (!folder.Exists) throw new DirectoryNotFoundException("Ordner nicht gefunden: " + folder.FullName);

                // create list
                var list = new List<FileInfo>();

                // collect files
                CollectFilesInFolder(folder, filter, list, recursive);

                // return
                return list;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Find Files in Folder (08.12.2022, SME)
        public static List<FileInfo> FindFilesInFolder(DirectoryInfo folder, bool recursive, params string[] filters)
        {
            try
            {
                // error-handling
                if (folder == null) throw new ArgumentNullException(nameof(folder));
                if (filters == null || !filters.Any()) throw new ArgumentNullException(nameof(filters));
                if (!folder.Exists) throw new DirectoryNotFoundException("Ordner nicht gefunden: " + folder.FullName);

                // create list
                var list = new List<FileInfo>();

                // loop throu filters + find files
                foreach (var filter in filters)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            var filterList = FindFilesInFolder(folder, filter, recursive);
                            if (filterList != null)
                            {
                                foreach (var file in filterList)
                                {
                                    if (!list.Contains(file)) {
                                        if (!list.Any(x => x.FullName == file.FullName))
                                        {
                                            list.Add(file);
                                        }
                                        else
                                        {
                                            CoreFC.DPrint("File already in List, but as different instance: " + file.FullName);
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

                // return
                return list;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Collect Files in Folder (08.12.2022, SME)
        private static void CollectFilesInFolder(DirectoryInfo folder, string filter, List<FileInfo> fileList, bool recursive = false)
        {
            foreach (var file in folder.GetFiles(filter))
            {
                fileList.Add(file);
            }
            if (recursive)
            {
                foreach (var subFolder in folder.GetDirectories())
                {
                    CollectFilesInFolder(subFolder, filter, fileList, recursive);
                }
            }
        }

        // Is empty Folder (08.06.2023, SME)
        public static bool IsEmptyFolder(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return false;
                if (!Directory.Exists(path)) return false;
                if (Directory.GetFileSystemEntries(path, StarDotStar, SearchOption.TopDirectoryOnly).Any()) return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Delete empty Folder (08.06.2023, SME)
        public static bool DeleteEmptyFolder(string path)
        {
            if (!IsEmptyFolder(path)) return false;
            try
            {
                Directory.Delete(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Move File (29.06.2023, SME)
        public static bool MoveFile(string moveFrom, string moveTo)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(moveFrom)) throw new ArgumentNullException(nameof(moveFrom));
                if (!File.Exists(moveFrom)) throw new FileNotFoundException("Zu verschiebende Datei nicht gefunden!", moveFrom);
                if (string.IsNullOrEmpty(moveTo)) throw new ArgumentNullException(nameof(moveTo));

                // delete target if necessary
                if (File.Exists(moveTo)) File.Delete(moveTo);

                // make sure target-folder exists
                var targetFolder = Path.GetDirectoryName(moveTo);
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                // move file
                File.Move(moveFrom, moveTo);

                // return
                return true;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Delete File (29.06.2023, SME)
        public static bool DeleteFile(string fileToDelete, bool deleteParentFolderIfEmpty = false)
        {
            try
            {
                // error-handling
                if (string.IsNullOrEmpty(fileToDelete)) throw new ArgumentNullException(nameof(fileToDelete));

                // delete file
                if (File.Exists(fileToDelete))
                {
                    File.Delete(fileToDelete);
                }

                // delete parent-folder if empty + flag is set
                if (deleteParentFolderIfEmpty)
                {
                    var parentFolder = Path.GetDirectoryName(fileToDelete);
                    if (IsEmptyFolder(parentFolder))
                    {
                        try
                        {
                            Directory.Delete(parentFolder);
                        }
                        catch (Exception)
                        {
                            // do nothing
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
        }

        #endregion

        #region Global-Instance

        // List of Global-Objects
        private static readonly List<object> GlobalObjects = new List<object>();

        // Get Global-Instance-ID (09.12.2022, SME)
        public static int GetGlobalInstanceID(object obj)
        {
            if (obj == null) return 0;
            if (!GlobalObjects.Contains(obj)) GlobalObjects.Add(obj);
            return GlobalObjects.IndexOf(obj) + 1;
        }

        #endregion

        #region JSON

        // JSON to XML (27.10.2023, SME)
        // copied from https://code-maze.com/csharp-convert-json-to-xml-or-xml-to-json/
        public static string JsonToXml(string json)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(json)) return string.Empty;

                // create x-doc
                XDocument doc = null;
                try
                {
                    doc = JsonConvert.DeserializeXNode(json, "ROOT", true, true)!;
                }
                catch (Exception)
                {
                    // add a root + try again
                    json = @"{""XML_ROOT"":" + json + "}";
                    doc = JsonConvert.DeserializeXNode(json, "ROOT", true, true)!;
                }

                // set declaration if necessary
                var declaration = doc.Declaration ?? new("1.0", null, null);

                // return
                return $"{declaration}{Environment.NewLine}{doc}";
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // JSON to DataSet (27.10.2023, SME)
        public static DataSet JsonToDataSet(string json)
        {
            try
            {
                // convert json to xml
                string xml = JsonToXml(json);

                // convert xml to dataset
                return XmlToDataSet(xml);
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        #region XML

        // XML to Dataset (27.10.2023, SME)
        public static DataSet XmlToDataSet(string xml)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(xml)) return null;

                // create dataset
                var data = new DataSet();

                // read xml via stream
                using (var stream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(xml);
                        writer.Flush();
                        stream.Position = 0;
                        data.ReadXml(stream);
                        data.AcceptChanges();
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        #endregion

        // Get Duration-String from TimeSpan (26.11.2022, SME)
        public static string GetDurationString(TimeSpan timeSpan)
        {
            try
            {
                if (timeSpan == null)
                    return string.Empty;
                else if (timeSpan.TotalDays >= 1)
                    return timeSpan.ToString();
                else if (timeSpan.TotalHours < 1)
                    return new DateTime(timeSpan.Ticks).ToString("mm:ss");
                else
                    return new DateTime(timeSpan.Ticks).ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Command-Line-Arguments (03.04.2023, SME)
        public static List<string> GetCommandLineArguments()
        {
            // store command-line-arguments
            var args = Environment.GetCommandLineArgs().ToList();
            // remove first entry, because that's the entry-assembly
            args.RemoveAt(0);
            // return
            return args;
        }

        // Is Equal Byte-Array (03.04.2023, SME)
        public static bool IsEqualByteArray(byte[] a, byte[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        // Datei öffnen (16.05.2023, SME)
        public static Process OpenFile(string filePath)
        {
            // error-handling
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Zu öffnende Datei nicht gefunden:" + Lines(2) + filePath);

            // return
            return StartProcess(filePath);
        }

        // Datei in Explorer öffnen (16.05.2023, SME)
        // Quelle: https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
        public static Process OpenFileInExplorer(string filePath)
        {
            // error-handling
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Zu öffnende Datei nicht gefunden:" + Lines(2) + filePath);

            // return
            string arguments = "/select, \"" + filePath + "\"";
            return StartProcess("explorer.exe", arguments);
        }

        // Ordner öffnen (16.05.2023, SME)
        public static Process OpenFolder(string folderPath)
        {
            // error-handling
            if (string.IsNullOrEmpty(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException("Zu öffnender Ordner nicht gefunden:" + Lines(2) + folderPath);

            // return
            return StartProcess(folderPath);
        }

        // Prozess starten (16.05.2023, SME)
        // CHANGE: 01.03.2024, SME: use process-start-info for .NET 8
        public static Process StartProcess(string fileName, string arguments = "")
        {
            // error-handling
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            // create new process-start-info
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = true
            };

            // set arguments
            if (!string.IsNullOrEmpty(arguments)) psi.Arguments = arguments;

            // start process + return
            return Process.Start(psi);
        }

        // Get Image-Format (16.06.2023, SME)
        public static ImageFormat GetImageFormat(Image image)
        {
            try
            {
                // exit-handling
                if (image == null) return null;
                if (image.RawFormat == null) return null;

                if (image.RawFormat.Guid.Equals(ImageFormat.Bmp.Guid)) return ImageFormat.Bmp;
                if (image.RawFormat.Guid.Equals(ImageFormat.Emf.Guid)) return ImageFormat.Emf;
                if (image.RawFormat.Guid.Equals(ImageFormat.Exif.Guid)) return ImageFormat.Exif;
                if (image.RawFormat.Guid.Equals(ImageFormat.Gif.Guid)) return ImageFormat.Gif;
                if (image.RawFormat.Guid.Equals(ImageFormat.Icon.Guid)) return ImageFormat.Icon;
                if (image.RawFormat.Guid.Equals(ImageFormat.Jpeg.Guid)) return ImageFormat.Jpeg;
                if (image.RawFormat.Guid.Equals(ImageFormat.MemoryBmp.Guid)) return ImageFormat.MemoryBmp;
                if (image.RawFormat.Guid.Equals(ImageFormat.Png.Guid)) return ImageFormat.Png;
                if (image.RawFormat.Guid.Equals(ImageFormat.Tiff.Guid)) return ImageFormat.Tiff;
                if (image.RawFormat.Guid.Equals(ImageFormat.Wmf.Guid)) return ImageFormat.Wmf;
                return image.RawFormat;
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // Get Image-Format-Enum (16.06.2023, SME)
        public static ImageFormatEnum? GetImageFormatEnum(Image image)
        {
            try
            {
                if (image == null) return null;
                return GetImageFormatEnum(image.RawFormat);
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // Get Image-Format-Enum (16.06.2023, SME)
        public static ImageFormatEnum? GetImageFormatEnum(ImageFormat format)
        {
            try
            {
                if (format == null) return null;
                if (format.Guid.Equals(ImageFormat.Bmp.Guid)) return ImageFormatEnum.Bmp;
                if (format.Guid.Equals(ImageFormat.Emf.Guid)) return ImageFormatEnum.Emf;
                if (format.Guid.Equals(ImageFormat.Exif.Guid)) return ImageFormatEnum.Exif;
                if (format.Guid.Equals(ImageFormat.Gif.Guid)) return ImageFormatEnum.Gif;
                if (format.Guid.Equals(ImageFormat.Icon.Guid)) return ImageFormatEnum.Icon;
                if (format.Guid.Equals(ImageFormat.Jpeg.Guid)) return ImageFormatEnum.Jpeg;
                if (format.Guid.Equals(ImageFormat.MemoryBmp.Guid)) return ImageFormatEnum.MemoryBmp;
                if (format.Guid.Equals(ImageFormat.Png.Guid)) return ImageFormatEnum.Png;
                if (format.Guid.Equals(ImageFormat.Tiff.Guid)) return ImageFormatEnum.Tiff;
                if (format.Guid.Equals(ImageFormat.Wmf.Guid)) return ImageFormatEnum.Wmf;
                return ImageFormatEnum.Unknown;
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // Save Image as (16.06.2023, SME)
        public static void SaveImage(Image image, string filePathWithoutExtension, bool overwrite = false)
        {
            try
            {
                // exit-handling
                if (image == null) return;
                if (string.IsNullOrEmpty(filePathWithoutExtension)) return;

                // get image-format
                var imageFormat = GetImageFormatEnum(image);

                // add extension
                string extension = ".jpg";
                if (imageFormat.HasValue)
                {
                    switch (imageFormat.Value)
                    {
                        case ImageFormatEnum.MemoryBmp:
                            break;
                        case ImageFormatEnum.Unknown:
                            break;
                        default:
                            extension = "." + imageFormat.Value.ToString().ToLower();
                            break;
                    }
                }

                // make sure folder exists
                var folder = System.IO.Path.GetDirectoryName(filePathWithoutExtension);
                if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);

                // check existance + set path
                string path = filePathWithoutExtension;
                if (File.Exists(path + extension))
                {
                    if (overwrite)
                    {
                        File.Delete(path + extension);
                    }
                    else
                    {
                        // add counter
                        int counter = 2;
                        while (File.Exists(path + $" - #{counter}" + extension))
                        {
                            counter++;
                        }
                        path += $" - #{counter}";
                    }
                }
                path += extension;

                // save image
                image.Save(path);
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // Get Byte-Array from Image (16.06.2023, SME)
        // Copied from SUT-Code
        public static byte[] GetByteArray(Image image, ImageFormat format = null, bool toCMYK = false)
        {
            try
            {
                // exit-handling
                if (image == null) return null;

                // make sure format is set
                if (format == null) format = GetImageFormat(image);
                var formatEnum = GetImageFormatEnum(format);

                // write memory-stream + return
                using (var ms = new MemoryStream())
                {
                    var clone = image.Clone() as Image;
                    clone.Save(ms, format);
                    clone.Dispose();
                    if (!toCMYK) return ms.ToArray();
                    ms.Close();

                    // CMYK-Handling
                    var saveAs = Path.Combine(Path.GetTempPath(), @"ImageToCMYK.jpg");
                    if (File.Exists(saveAs)) File.Delete(saveAs);
                    clone = image.Clone() as Image;
                    clone.Save(saveAs);
                    clone.Dispose();

                    using (var reader = File.OpenRead(saveAs))
                    {
                        BitmapSource myBitmapSource = BitmapFrame.Create(reader);
                        FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();
                        newFormatedBitmapSource.BeginInit();
                        newFormatedBitmapSource.Source = myBitmapSource;
                        newFormatedBitmapSource.DestinationFormat = PixelFormats.Cmyk32;
                        newFormatedBitmapSource.EndInit();

                        BitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(newFormatedBitmapSource));

                        using (var cmykStream = new MemoryStream())
                        {
                            encoder.Save(cmykStream);
                            return cmykStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // List of loaded Images with Stream (16.06.2023, SME)
        private static readonly Dictionary<Image, MemoryStream> LoadedImagesList = new Dictionary<Image, MemoryStream>();

        // Add loaded Image (16.06.2023, SME)
        private static void AddLoadedImage(Image image, MemoryStream stream)
        {
            try
            {
                // exit-handling
                if (image == null) return;
                if (stream == null) return;

                // add to list
                LoadedImagesList.Add(image, stream);

                // add dispose-handlers
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // Get Image from Byte-Array (16.06.2023, SME)
        // Copied from SUT-Code
        public static Image GetImage(byte[] bytes)
        {
            try
            {
                // exit-handling
                if (bytes == null || !bytes.Any()) return null;

                // fill memory-stream + return
                // REMARKED (16.06.2023, SME)
                //using (MemoryStream ms = new MemoryStream(bytes))
                //{
                //    return Image.FromStream(ms).Clone() as Image;
                //}

                // CHANGE: DON'T dispose memory-stream, otherwise image cannot be accessed anymore + error "Allgemeiner Fehler in GDI+" will be raised!!! (16.06.2023, SME)
                var ms = new MemoryStream(bytes);
                var image = Image.FromStream(ms);


                // return
                return image;
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // Get optimize Image as Byte-Array (16.06.2023, SME)
        // Copied from SUT-Code
        public static byte[] GetOptimizedImageAsByteArray(Image image, int maxWidthHeight = 1024)
        {
            try
            {
                // exit-handling
                if (image == null) return null;

                // store bytes / size before
                var bytesBefore = GetByteArray(image);
                var sizeBefore = GetFsoSizeStringOptimal(bytesBefore.Length);

                // get new dimensions
                int w = 0;
                int h = 0;
                if (image.Width < maxWidthHeight && image.Height < maxWidthHeight)
                {
                    // keep original dimensions (15.06.2023, SRM)
                    w = image.Width;
                    h = image.Height;
                }
                else if (image.Width > image.Height)
                {
                    // landscape
                    w = maxWidthHeight;
                    h = maxWidthHeight * image.Height / image.Width;
                }
                else if (image.Height > image.Width)
                {
                    // portrait
                    h = maxWidthHeight;
                    w = maxWidthHeight * image.Width / image.Height;
                }
                else
                {
                    // square
                    w = maxWidthHeight;
                    h = maxWidthHeight;
                }

                // create new image
                using (var newImage = new Bitmap(w, h))
                {

                    // use graphics
                    using (var g = Graphics.FromImage(newImage))
                    {
                        // set properties of graphics
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        // draw image
                        g.DrawImage(image, new Rectangle(0, 0, w, h));

                        // apply properties
                        foreach (var prop in image.PropertyItems)
                        {
                            try
                            {
                                newImage.SetPropertyItem(prop);
                            }
                            catch (Exception ex)
                            {
                                CoreFC.DPrint("ERROR while applying image-property: " + ex.Message);
                            }
                        }

                        // reload image
                        var bytes = GetByteArray(newImage, image.RawFormat, IsCMYK(image));
                        var sizeAfter = GetFsoSizeStringOptimal(bytes.Length);
                        CoreFC.DPrint($"Optimize Image: Size before = {sizeBefore}, Size after = {sizeAfter}");

                        // return
                        if (bytesBefore.Length <= bytes.Length)
                        {
                            return bytesBefore;
                        }
                        else
                        {
                            return bytes;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // Compress Image (30.11.2021, SRM)
        // CHANGE: "forceCompression" added as parameter (15.06.2023, SRM)
        // TODO: set default-value of "forceCompression" to false (15.06.2023, SRM)
//        private void CompressImage(System.IO.FileInfo imageFile, int maxWidthHeight, short[] ratingFilter, bool forceCompression = true)
//        {
//            var sizeOriginal = imageFile.Length;

//            try
//            {
//                using (var original = FC.GetImage(imageFile.FullName))
//                {

//                    // exit-handling
//                    if (original == null)
//                        throw new Exception("Image could not be loaded: " + imageFile.FullName);
//                    else if (original.Width <= maxWidthHeight && original.Height <= maxWidthHeight && !forceCompression)
//                        // no compression needed
//                        return;
//                    else if (MyFC.ImageContainsKeyword(original, this.txt_DontCompressTag.Text))
//                        // no compression wanted
//                        return;
//                    else
//                    {
//                        var rating = MyFC.GetRatingFromImage(original);
//                        if (!ratingFilter.Contains(rating))
//                            // no compression wanted
//                            return;
//                    }

//                    // write log
//                    this.WriteLog("- Compressing " + imageFile.FullName + " ...");

//                    // get new dimensions
//                    int w = 0;
//                    int h = 0;
//                    if (original.Width < maxWidthHeight && original.Height < maxWidthHeight)
//                    {
//                        // keep original dimensions (15.06.2023, SRM)
//                        w = original.Width;
//                        h = original.Height;
//                    }
//                    else if (original.Width > original.Height)
//                    {
//                        // landscape
//                        w = maxWidthHeight;
//                        h = maxWidthHeight / (double)original.Width * original.Height;
//                    }
//                    else if (original.Height > original.Width)
//                    {
//                        // portrait
//                        h = maxWidthHeight;
//                        w = maxWidthHeight / (double)original.Height * original.Width;
//                    }
//                    else
//                    {
//                        // square
//                        w = maxWidthHeight;
//                        h = maxWidthHeight;
//                    }

//                    // compress image
//                    using (var newImage = new Bitmap(w, h))
//                    {
//                        var graph = Graphics.FromImage(newImage);
//                        {
//                            var withBlock = graph;
//                            withBlock.CompositingQuality = CompositingQuality.HighQuality;
//                            withBlock.SmoothingMode = SmoothingMode.HighQuality;
//                            withBlock.InterpolationMode = InterpolationMode.HighQualityBicubic;
//                            // draw image
//                            withBlock.DrawImage(original, new Rectangle(0, 0, w, h));
//                        }

//                        // apply properties
//                        foreach (var prop in original.PropertyItems)
//                        {
//                            try
//                            {
//                                newImage.SetPropertyItem(prop);
//                            }
//                            catch (Exception ex)
//                            {
//                                throw ex;
//                            }
//                        }

//                        // apply compressed-keyword
//                        MyFC.AddKeywordToImage(newImage, this.txt_CompressedTag.Text);

//                        // save image
//                        try
//                        {
//                            // save image
//                            newImage.Save(imageFile.FullName, original.RawFormat);
//                            // refresh file-info
//                            imageFile.Refresh();
//                            // calculate space saved
//                            var spaceSaved = sizeOriginal - imageFile.Length;
//                            // add space saved
//                            if (this.InvokeRequired)
//                                ;/* Cannot convert ExpressionStatementSyntax, CONVERSION ERROR: Conversion for AddAssignmentStatement not implemented, please report this issue in 'Me.SpaceSaved += spaceSaved' at character 4570
//   at ICSharpCode.CodeConverter.CSharp.VisualBasicConverter.NodesVisitor.DefaultVisit(SyntaxNode node)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.VisitAssignmentStatement(AssignmentStatementSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.AssignmentStatementSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.Visit(SyntaxNode node)
//   at ICSharpCode.CodeConverter.CSharp.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.VisitAssignmentStatement(AssignmentStatementSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.AssignmentStatementSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at ICSharpCode.CodeConverter.CSharp.VisualBasicConverter.NodesVisitor.VisitSingleLineLambdaExpression(SingleLineLambdaExpressionSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.SingleLineLambdaExpressionSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.Visit(SyntaxNode node)
//   at ICSharpCode.CodeConverter.CSharp.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.VisitSingleLineLambdaExpression(SingleLineLambdaExpressionSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.SingleLineLambdaExpressionSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at ICSharpCode.CodeConverter.CSharp.VisualBasicConverter.NodesVisitor.VisitSimpleArgument(SimpleArgumentSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleArgumentSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.Visit(SyntaxNode node)
//   at ICSharpCode.CodeConverter.CSharp.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.VisitSimpleArgument(SimpleArgumentSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleArgumentSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at ICSharpCode.CodeConverter.CSharp.VisualBasicConverter.NodesVisitor.<>c__DisplayClass83_0.<ConvertArguments>b__0(ArgumentSyntax a, Int32 i)
//   at System.Linq.Enumerable.<SelectIterator>d__5`2.MoveNext()
//   at System.Linq.Enumerable.WhereEnumerableIterator`1.MoveNext()
//   at Microsoft.CodeAnalysis.CSharp.SyntaxFactory.SeparatedList[TNode](IEnumerable`1 nodes)
//   at ICSharpCode.CodeConverter.CSharp.VisualBasicConverter.NodesVisitor.VisitArgumentList(ArgumentListSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.Visit(SyntaxNode node)
//   at ICSharpCode.CodeConverter.CSharp.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.VisitArgumentList(ArgumentListSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at ICSharpCode.CodeConverter.CSharp.VisualBasicConverter.NodesVisitor.VisitInvocationExpression(InvocationExpressionSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.Visit(SyntaxNode node)
//   at ICSharpCode.CodeConverter.CSharp.CommentConvertingNodesVisitor.DefaultVisit(SyntaxNode node)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.VisitInvocationExpression(InvocationExpressionSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at ICSharpCode.CodeConverter.CSharp.VisualBasicConverter.MethodBodyVisitor.VisitExpressionStatement(ExpressionStatementSyntax node)
//   at Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionStatementSyntax.Accept[TResult](VisualBasicSyntaxVisitor`1 visitor)
//   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor`1.Visit(SyntaxNode node)
//   at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.ConvertWithTrivia(SyntaxNode node)
//   at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)

//Input: 
//                                        Me.Invoke(Sub() Me.SpaceSaved += spaceSaved)

// */
//                            else
//                                this.SpaceSaved += spaceSaved;
//                        }
//                        catch (Exception ex)
//                        {
//                            throw ex;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new SutERR.UnhandledError(ex);
//            }
//        }


        // Get optimize Image (16.06.2023, SME)
        // Copied from SUT-Code
        public static Image GetOptimizedImage(Image image)
        {
            try
            {
                // exit-handling
                if (image == null) return image;

                // get optimized image as byte-array
                var bytes = GetOptimizedImageAsByteArray(image);
                if (bytes == null) return image;
                if (!bytes.Any()) return image;

                // return
                return GetImage(bytes);
            }
            catch (Exception ex)
            {
                ThrowError(ex); throw ex;
            }
        }

        // Is CMYK-Image (28.06.2023, SME)
        // copied from https://stackoverflow.com/questions/4315335/how-to-identify-cmyk-images-using-c-sharp
        public static bool IsCMYK(Image image)
        {
            var flags = (ImageFlags)image.Flags;
            if (flags.HasFlag(ImageFlags.ColorSpaceCmyk) || flags.HasFlag(ImageFlags.ColorSpaceYcck))
            {
                return true;
            }

            return (int)image.PixelFormat == ImagePixelFormat32bppCMYK;
        }

        // Get Name of Entry-Assembly (03.07.2023, SME)
        public static string GetEntryAssemblyName()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var name = assembly?.GetName()?.Name;
            return (string.IsNullOrEmpty(name) ? "UNKNOWN" : name);
        }

        // Get FilePath of Entry-Assembly (05.07.2023, SME)
        public static string GetEntryAssemblyFilePath()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            if (assembly == null) throw new Exception("Entry-Assembly konnte nicht ermittelt werden!");
            if (string.IsNullOrEmpty(assembly.Location)) throw new Exception("Pfad von Entry-Assembly konnte nicht ermittelt werden!");
            return assembly.Location;
        }

        // Get FileName of Entry-Assembly (05.07.2023, SME)
        public static string GetEntryAssemblyFileName()
        {
            return Path.GetFileName(GetEntryAssemblyFilePath());
        }

        // Get FileName without Extension of Entry-Assembly (05.07.2023, SME)
        public static string GetEntryAssemblyFileNameWoExtension()
        {
            return Path.GetFileNameWithoutExtension(GetEntryAssemblyFilePath());
        }

        // Get FolderPath of Entry-Assembly (05.07.2023, SME)
        public static string GetEntryAssemblyFolderPath()
        {
            return Path.GetDirectoryName(GetEntryAssemblyFilePath());
        }

        // Is Image-File (21.07.2023, SME)
        public static bool IsImageFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            if (!File.Exists(filePath)) return false;
            if (KnownImageFileTypes.Contains(Path.GetExtension(filePath).ToLower()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        // Umgebung mittels Startup-Ordnerpfad ermitteln
        /// <summary>
         /// Gibt die aktuelle Umgebung basierend auf dem StartupPath zurück
         /// </summary>
        public static CurrentEnvironment? GetEnvironmentByStartupPath()
        {
            string startupPath = GetStartupFolderPath();

            if (startupPath.ToUpper().StartsWith(@"C:\DEV"))
            {
                return CurrentEnvironment.DEV;
            }
            else if (startupPath.ToUpper().StartsWith(@"C:\DEVNET"))
            {
                return CurrentEnvironment.DEV;
            }
            else
            {
                switch (Path.GetFileName(startupPath).ToUpper())
                {
                    case PROD: return CurrentEnvironment.PROD;
                    case TEST: return CurrentEnvironment.TEST;
                    case TEST_INT: return CurrentEnvironment.TEST_INT;
                    case TEST_DEV: return CurrentEnvironment.DEV;
                    case DEBUG: return CurrentEnvironment.DEV;
                }
            }
            return null;
        }

        // Herausfinden ob childType von parentType erbt (26.04.2023, SME)
        public static bool IsDerivedType(Type childType, Type parentType)
        {
            if (childType == null) return false;
            if (parentType == null) return false;
            if (parentType.Equals(childType)) return true;
            if (childType.BaseType == null) return false;
            return IsDerivedType(childType.BaseType, parentType);
        }

        // HTML to Text (22.01.2024, SME)
        public static string HtmlToText(string html)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(html)) return string.Empty;

                // Decode HTML entities
                string decodedHtml = WebUtility.HtmlDecode(html);

                // Remove HTML tags
                string plainText = System.Text.RegularExpressions.Regex.Replace(decodedHtml, "<.*?>", "");

                // return
                return plainText;

            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        public static void TryCopyFile(string from, string to, bool overwrite = true, int maxWaitIteration = 1, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            try
            {
                File.Copy(from, to, overwrite);
            }
            catch
            {
                IsFileLocked_WaitMaxSeconds(from, maxWaitIteration, true, callerFilePath, callerLineNumber, callerMemberName);
                IsFileLocked_WaitMaxSeconds(to, maxWaitIteration, false, callerFilePath, callerLineNumber, callerMemberName);
                try
                {
                    File.Copy(from, to, overwrite);
                }
                catch (Exception ex)
                {
                    throw new Exception($"TryCopyFile{Environment.NewLine}MaxWaitSeconds: {maxWaitIteration}{Environment.NewLine}From: {from}{Environment.NewLine}To: {to}", ExceptionDispatchInfo.Capture(ex).SourceException);
                }
            }
        }

        public static void TryDeleteFile(string delme, int maxWaitIteration = 1, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            try
            {
                // exit wenn Datei nicht existiert
                if (!File.Exists(delme)) return;

                // Datei löschen (versuchen)
                File.Delete(delme);
            }
            catch
            {
                // Warten
                IsFileLocked_WaitMaxSeconds(delme, maxWaitIteration, false, callerFilePath, callerLineNumber, callerMemberName);

                try
                {
                    // nochmals versuchen
                    File.Delete(delme);
                }
                catch (Exception ex)
                {
                    // Fehler auslösen
                    throw new Exception($"TryDeleteFile{Environment.NewLine}MaxWaitSeconds: {maxWaitIteration}{Environment.NewLine}File2Delete: {delme}", ExceptionDispatchInfo.Capture(ex).SourceException);
                }
            }
        }

        public static void TryDeleteFileCatch(string delme, int maxWaitIteration = 5, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            try
            {
                File.Delete(delme);
            }
            catch
            {
                IsFileLocked_WaitMaxSeconds(delme, maxWaitIteration, false, callerFilePath, callerLineNumber, callerMemberName);
                try { File.Delete(delme); } catch { }
            }
        }

        public static void TryMoveFile(string from, string to, int maxWaitIteration = 1, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = "")
        {
            try
            {
                File.Move(from, to);
            }
            catch
            {
                IsFileLocked_WaitMaxSeconds(from, maxWaitIteration, true, callerFilePath, callerLineNumber, callerMemberName);
                IsFileLocked_WaitMaxSeconds(to, maxWaitIteration, false, callerFilePath, callerLineNumber, callerMemberName);
                try
                {
                    File.Move(from, to);
                }
                catch (Exception ex)
                {
                    throw new Exception($"TryMoveFile{Environment.NewLine}MaxWaitSeconds: {maxWaitIteration}{Environment.NewLine}From: {from}{Environment.NewLine}To: {to}", ExceptionDispatchInfo.Capture(ex).SourceException);
                }
            }
        }

        // Get Lines from Text (28.03.2024, SME)
        /// <summary>
        /// Ermittelt die Zeilen des Texts und gibt diese als String-Array zurück
        /// </summary>
        /// <param name="text">Text welcher in Zeilen aufgeteilt werden soll</param>
        /// <param name="delimiters">Trennzeichen nach denen gesplittet werden soll.
        /// Falls nicht gesetzt werden die Trennzeichen automatisch ermittelt.
        /// Im Normalfall sind die Trennzeichen entweder Character 13 (\r) oder Character 10 (\n) oder Environment.NewLine (Kombination von beiden anderen Trennzeichen)</param>
        /// <param name="ignoreEmptyLines">Flag das bestimmt ob Leerzeilen ignoriert werden sollen</param>
        /// <returns></returns>
        public static string[] GetLines(string text, char[] delimiters = null, bool ignoreEmptyLines = false)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(text)) return [];

                // make sure delimiters are set
                if (delimiters == null || !delimiters.Any())
                {
                    if (text.Contains(Environment.NewLine))
                    {
                        delimiters = Environment.NewLine.ToCharArray();
                    }
                    else if (text.Contains(CR) && text.Contains(LF))
                    {
                        // text contains CR + LF, but not in a row
                        // => Replace CR with LF + use LF as delimiter (08.04.2024, SME)
                        text = text.Replace(CR, LF);
                        delimiters = [LF];
                    }
                    else if (text.Contains(CR))
                    {
                        delimiters = [CR];
                    }
                    else if (text.Contains(LF))
                    {
                        delimiters = [LF];
                    }
                    else
                    {
                        delimiters = Environment.NewLine.ToCharArray();
                    }
                }

                // if the delimiters are more then 1 character, the split will leave a lot of empty lines, when not splitted with the option "RemoveEmptyEntries"
                // => so if the flag "ignoreEmptyLines" isn't set, then replace the delimiters with a LF to use only 1 character as a splitter
                if (delimiters.Length > 1 && !ignoreEmptyLines)
                {
                    string toReplace = string.Join("", delimiters);
                    text = text.Replace(toReplace, LF.ToString());
                    delimiters = [LF];
                }

                // get lines
                var options = ignoreEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;
                var lines = text.Split(delimiters, options).ToList();

                // return
                return lines.ToArray();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Job-ID from File-Name (28.03.2024, SME)
        public static int? GetJobIDfromFileName(string fileName)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(fileName)) return null;

                // store file-name without file-extension
                fileName = Path.GetFileNameWithoutExtension(fileName);

                // split file-name by "_"
                var parts = fileName.Split('_');

                // search for any numeric value that matches job-id-length
                foreach ( var part in parts )
                {
                    if (part.Length == JobIdLength)
                    {
                        int intValue;
                        if (int.TryParse(part, out intValue))
                        {
                            if (intValue > 0) return intValue;
                        }
                    }
                }

                // return
                return null;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get SX/DX from File-Name (28.03.2024, SME)
        public static SxDxEnum? GetSxDx(string fileName)
        {
            try
            {
                // exit-handling
                if (string.IsNullOrEmpty(fileName)) return null;

                // store file-name without file-extension
                fileName = Path.GetFileNameWithoutExtension(fileName);

                // loop throu plex-modes
                foreach (var sx_dx in CoreFC.GetEnumValues<SxDxEnum>())
                {
                    if (fileName.Contains("_" + sx_dx.ToString() + "_"))
                    {
                        return sx_dx;
                    }
                }

                // return
                return null;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get RAM-Usage (30.05.2024, SME)
        public static long GetRamUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                if (process == null) return 0;
                return process.PrivateMemorySize64;
            }
            catch (Exception ex)
            {
                CoreFC.DPrint("ERROR in GetRamUsage: " + ex.Message);
                return 0;
            }
        }

        // Get RAM-Usage in MB (30.05.2024, SME)
        public static int GetRamUsageInMB()
        {
            try
            {
                long ramUsage = GetRamUsage();
                return (int)(ramUsage / (long)FsoSizeTypeEnum.MB);
            }
            catch (Exception ex)
            {
                CoreFC.DPrint("ERROR in GetRamUsageInMB: " + ex.Message);
                return 0;
            }
        }

        // Get RAM-Usage-Status (30.05.2024, SME)
        public static RamUsageStatusEnum GetRamUsageStatus(long ramUsage, long warningAfter, long criticalAfter)
        {
            try
            {
                if (ramUsage >= criticalAfter) return RamUsageStatusEnum.Critical;
                if (ramUsage >= warningAfter) return RamUsageStatusEnum.Warning;
                return RamUsageStatusEnum.OK;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get RAM-Usage-Status from MB (30.05.2024, SME)
        public static RamUsageStatusEnum GetRamUsageStatusFromMB(int ramUsageInMB, int warningAfterInMB, int criticalAfterInMB)
        {
            try
            {
                if (ramUsageInMB >= criticalAfterInMB) return RamUsageStatusEnum.Critical;
                if (ramUsageInMB >= warningAfterInMB) return RamUsageStatusEnum.Warning;
                return RamUsageStatusEnum.OK;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

    }
}
