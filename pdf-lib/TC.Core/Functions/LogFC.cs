using System;
using System.IO;

namespace TC.Functions
{
    // Log-Functions (21.06.2023, SME)
    public static class LogFC
    {
        // Konstanten
        private const string LogFileType = ".log";
        private const string LogFolderName = "LogFiles";
        private const string LogDateTimeStamp = "yyyy-MM-dd HH:mm:ss";
        private const string LogFileNameDateStampFormat = "yyyy-MM-dd";
        private const string PlaceHolder_AssemblyName = "{AssemblyName}";
        private const string PlaceHolder_TimeStamp = "{TimeStamp}";
        private const string LogFileNamePattern = $"LogFile, {PlaceHolder_AssemblyName}, {PlaceHolder_TimeStamp}{LogFileType}";
        private const string LogFileNamePattern_Errors = $"LogFile, Errors, {PlaceHolder_AssemblyName}{LogFileType}";
        private const string LogFileHeaderLine = "Date/Time;User;Info";
        private const string Replacement_NewLine = @" ||| ";
        private const string Replacement_Semicolon = @"{,,..}";
        private const string Semicolon = @";";

        // Get Log-Folder-Path (21.06.2023, SME)
        private static string GetLogFolderPath(bool createIfNotExisting = true)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly == null || string.IsNullOrEmpty(assembly.Location)) 
                    assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string path = Path.GetTempPath();
                if (assembly != null && !string.IsNullOrEmpty(assembly.Location))
                    path = Path.GetDirectoryName(assembly.Location);
                path = Path.Combine(path, LogFolderName);
                if (createIfNotExisting && !Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Log-File-Path with Pattern (02.01.2024, SME)
        private static string GetLogFilePath(string logFileNamePattern, bool createIfNotExisting = true, string exeFileName = "")
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly == null || string.IsNullOrEmpty(assembly.Location))
                    assembly = System.Reflection.Assembly.GetExecutingAssembly();
                if (assembly == null || string.IsNullOrEmpty(assembly.Location))
                    assembly = null;
                var fileName = !string.IsNullOrEmpty(exeFileName) ? exeFileName : assembly == null ? "Unknown_Assembly" : Path.GetFileNameWithoutExtension(assembly.Location);
                fileName = logFileNamePattern.Replace(PlaceHolder_AssemblyName, fileName).Replace(PlaceHolder_TimeStamp, DateTime.Now.ToString(LogFileNameDateStampFormat));
                var filePath = Path.Combine(GetLogFolderPath(createIfNotExisting), fileName);
                if (createIfNotExisting && !File.Exists(filePath))
                {
                    using (var writer = new StreamWriter(filePath))
                    {
                        writer.WriteLine(LogFileHeaderLine);
                    }
                }
                return filePath;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Get Log-File-Path for normal Log-File (21.06.2023, SME)
        private static string GetLogFilePath(bool createIfNotExisting = true, string exeFileName = "")
        {
            return GetLogFilePath(LogFileNamePattern, createIfNotExisting, exeFileName);
        }

        // Get Log-File-Path for Error-Log-File (21.06.2023, SME)
        private static string GetLogFilePath_Errors(bool createIfNotExisting = true, string exeFileName = "")
        {
            return GetLogFilePath(LogFileNamePattern_Errors, createIfNotExisting, exeFileName);
        }

        // Write Log with Log-File-Path-Function (02.01.2024, SME)
        private static void WriteLogPrivate(string logFilePath, string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    var path = logFilePath;
                    var line = $"{DateTime.Now.ToString(LogDateTimeStamp)};{CoreFC.GetUserName()};{text.Replace(Environment.NewLine, Replacement_NewLine).Replace(Semicolon, Replacement_Semicolon)}";
                    using (var writer = new StreamWriter(path, true))
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                // IMPORTANT: DON'T throw error back, just write to console
                Console.WriteLine("ERROR while writing log: " + ex.Message);
            }
        }

        // Write Log (21.06.2023, SME)
        public static void WriteLog(string info, string exeFileName = "")
        {
            try
            {
                WriteLogPrivate(GetLogFilePath(true, exeFileName), info);
            }
            catch (Exception ex)
            {
                // IMPORTANT: DON'T throw error back, just write to console
                Console.WriteLine("ERROR while writing log: " + ex.Message);
            }
        }

        // Write Error (21.06.2023, SME)
        public static void WriteError(Exception error, string exeFileName = "")
        {
            try
            {
                if (error != null)
                {
                    WriteError(error, CoreFC.GetCallingMethod(), exeFileName);
                }
            }
            catch (Exception ex)
            {
                // IMPORTANT: DON'T throw error back, just write to console
                Console.WriteLine("ERROR while writing log-error: " + ex.Message);
            }
        }

        // Write Error (21.06.2023, SME)
        public static void WriteError(Exception error, System.Reflection.MethodBase callingMethod, string exeFileName = "")
        {
            try
            {
                if (error != null)
                {
                    // make sure calling method is set
                    if (callingMethod == null) callingMethod = CoreFC.GetCallingMethod();

                    // set msg
                    string msg = $"ERROR in {callingMethod}: Error-Type = {error.GetType()}, Error-Message = {error.Message}";

                    // write log + error-log
                    WriteLog(msg, exeFileName);
                    WriteLogPrivate(GetLogFilePath_Errors(true, exeFileName), msg);
                }
            }
            catch (Exception ex)
            {
                // IMPORTANT: DON'T throw error back, just write to console
                Console.WriteLine("ERROR while writing log-error: " + ex.Message);
            }
        }
    }
}
