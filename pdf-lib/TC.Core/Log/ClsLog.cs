using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TC.Functions;
using static TC.Constants.CoreConstants;

namespace TC.Log
{
    // Imported from MEDA (21.02.2024, SME)
    public static class ClsLog
    {
        #region Konstanten / Variablen 

        public const string LogFolderName = "Logs";
        private static string LogFile_log;
        private static readonly string[] stringArrayNewLine = new string[] { Environment.NewLine };
        private const string underscore = "_";
        private const string blank = " ";
        private const string br = "<br>";

        private static ILogger Logger;

        #endregion

        #region Methoden

        // Log-Verzeichnis ermitteln (22.02.2024, SME) (22.02.2024, SME)
        /// <summary>
        /// Log-Verzeichnis ermitteln, mit der Option das Verzeichnis zu erstellen falls nötig
        /// </summary>
        /// <returns></returns>
        public static string GetLogFolderPath(bool createIfNotExisting = true)
        {
            string path = Path.Combine(CoreFC.GetStartupFolderPath(), LogFolderName);
            if (createIfNotExisting)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            return path;
        }

        // Logger initialisieren
        /// <summary>
        /// Initialisieren des Loggers
        /// </summary>
        public static void InitLogger(bool forceReinit = false)
        {
            if (Logger == null || forceReinit)
            {
                Logger = SetupLogger();
            }
        }

        // Logger einrichten
        /// <summary>
        /// Den Logger initialisieren
        /// </summary>
        /// <param name="IsDevTask"></param>
        private static ILogger SetupLogger(bool WriteHeaderInfo = true, bool tryAgain = true)
        {
            try
            {
                // Log-Ordnerpfad + isDevTask-Flag zwischenspeichern
                string logFolderPath = GetLogFolderPath(true);
                bool isDevTask = CoreFC.IsDevTaskUser();

                // Allenfalls vorhandenen Logger flushen und beenden.
                Serilog.Log.CloseAndFlush();

                LogFile_log = Path.Combine(logFolderPath, Assembly.GetEntryAssembly().GetName().Name + underscore + DateTime.Now.ToString(yyyyMMdd) + $"{(isDevTask ? "_DevTask" : string.Empty)}" + DotLOG);

                /*
                 *  Description
                 *  shared:true - Mehrere Prozesse auf LogFile zugreifen lassen (wichtig für parallele Ausführung von Scheduled Task)
                 *  retainedFileCountLimit: null - Per Default werden max 31 LogFiles angelegt, ältere werden gelöscht. Default aufheben und alle LogFiles aufbewahren
                 *  outputTemplate - Definiert, wie die Loggingeinträge gestylt werden sollen
                 */
                var sep = "  |  ";
                var user = isDevTask ? string.Empty : CoreFC.GetUserName() + sep;
                Serilog.Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(LogFile_log, shared: true, retainedFileCountLimit: null, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss}" + sep + user + "{Message}{NewLine}{Exception}")
                    .CreateLogger();

                Thread.Sleep(100);

                if (WriteHeaderInfo)
                {
                    Serilog.Log.Information(LogDashes);
                    Serilog.Log.Information($"********** {CoreFC.GetUserName()} {DateTime.Now.ToString(dd_MM_yyyy_HH_mm_ss)}");
                    Serilog.Log.Information(LogDashes);
                }
            }
            catch (Exception ex)
            {
                ClsError.WriteErrorHTML(ex);
                Thread.Sleep(100);
                if (tryAgain) SetupLogger(false, false);
                else CoreFC.ThrowError(ex);
            }

            return Serilog.Log.Logger;
        }

        // Fehler-Meldungstext für Log ermitteln
        private static string GetLogErrorMessage(Exception ex)
        {
            // Fehler-Text initialisiert mit Präfix + Message (09.01.2023, SME)
            var errorMessage = "FEHLER: " + Environment.NewLine + ex.Message;

            // Alle inneren Fehler hinzufügen
            var innerError = ex.InnerException;
            while (innerError != null)
            {
                errorMessage += Environment.NewLine + Environment.NewLine + innerError.Message;
                innerError = innerError.InnerException;
            }

            // Rückgabe
            return errorMessage;
        }

        #endregion

        #region Log schreiben

        // PRIVATE: Log-Information schreiben (22.02.2024, SME)
        private static void WriteLogInfo(string text)
        {
            // Logger initialisieren (wird nur initialisiert falls noch nicht geschehen)
            InitLogger();

            // Log-Info schreiben
            Serilog.Log.Information(text);
        }

        // PRIVATE: Log-Fehler schreiben (22.02.2024, SME)
        private static void WriteLogError(string text)
        {
            // Logger initialisieren (wird nur initialisiert falls noch nicht geschehen)
            InitLogger();

            // Log-Info schreiben
            Serilog.Log.Error(text);
        }

        // Log schreiben
        /// <summary>
        /// Log schreiben
        /// </summary>
        /// <param name="logEntry"></param>
        public static void WriteLog(string logEntry)
        {
            if (logEntry.Contains(br)) { logEntry = logEntry.Replace(br, Environment.NewLine); }
            if (logEntry.Contains(Environment.NewLine))
            {
                foreach (string line in logEntry.Split(stringArrayNewLine, StringSplitOptions.None))
                {
                    WriteLogInfo(line);
                }
            }
            else
            {
                WriteLogInfo(logEntry);
            }
        }

        // Log schreiben mit Stopuhr
        /// <summary>
        /// Das Log aktualisieren
        /// </summary>
        /// <param name="logEntry"></param>
        /// <param name="sw"></param>
        public static void WriteLog(string logEntry, Stopwatch sw)
        {
            if (logEntry.Contains(br)) { logEntry = logEntry.Replace(br, Environment.NewLine); }
            if (logEntry.Contains(Environment.NewLine))
            {
                foreach (string line in logEntry.Split(stringArrayNewLine, StringSplitOptions.None))
                {
                    WriteLogInfo(sw.Elapsed.ToString(hhmmss) + blank + line);
                }
            }
            else
            {
                WriteLogInfo(sw.Elapsed.ToString(hhmmss) + blank + logEntry);
            }
        }

        // Fehler loggen
        /// <summary>
        /// Fehler ins Log schreiben
        /// </summary>
        /// <param name="ex">Fehler</param>
        public static void WriteError(Exception ex)
        {
            // exit-handling
            if (ex == null) return;

            // Typ abhandeln
            if (ex is AggregateException)
            {
                // Sammel-Fehler
                WriteError(((AggregateException)ex).InnerExceptions.ToList());
            }
            else
            {
                // Normaler Fehler
                WriteError(GetLogErrorMessage(ex));
            }
        }

        // Fehler loggen
        /// <summary>
        /// Fehler ins Log schreiben
        /// </summary>
        /// <param name="errorMessage"></param>
        public static void WriteError(string errorMessage)
        {
            if (errorMessage.Contains(br)) { errorMessage = errorMessage.Replace(br, Environment.NewLine); }
            if (errorMessage.Contains(Environment.NewLine))
            {
                foreach (string line in errorMessage.Split(stringArrayNewLine, StringSplitOptions.None))
                {
                    WriteLogError(line);
                }
            }
            else
            {
                WriteLogError(errorMessage);
            }
        }

        #endregion

        // Fehler-Liste loggen
        /// <summary>
        /// Das Log aktualisieren
        /// </summary>
        /// <param name="lstExceptions"></param>
        public static void WriteError(IEnumerable<Exception> lstExceptions)
        {
            HashSet<string> hsExMsg = new();
            foreach (Exception ex in lstExceptions)
            {
                var ex2add = GetLogErrorMessage(ex);

                if (!hsExMsg.Contains(ex2add))
                {
                    hsExMsg.Add(ex2add);
                }
            }

            foreach (var errorMessage in hsExMsg)
            {
                WriteError(errorMessage);
            }
        }

    }
}
