using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using TC.Enums;
using TC.Errors;
using static TC.Constants.CoreConstants;

namespace TC.Functions
{
    public static class ClsError
    {
        #region Error-HTML schreiben mit Exception + Tabelle

        /// <summary>
        /// Schreibt die Exception als HTML in den Ordner DEVError, von wo aus dann ein Email verschickt wird
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="dt">Betroffene DataTable</param>
        public static void WriteErrorHTML(Exception ex, DataTable dt)
        {
            string rowErrors = string.Empty;
            foreach (DataRow row in dt.Rows)
            {
                if (row.RowError.Length > 0)
                {
                    rowErrors += $"Tabelle '{dt.TableName}': {row.RowError}{Environment.NewLine}";
                }
            }

            if (rowErrors.Length > 0)
            {
                WriteErrorHTML(new Exception(rowErrors, ex));
            }
            else
            {
                WriteErrorHTML(ex);
            }
        }

        #endregion

        #region Error-HTML schreiben mit Exception + DataSet

        /// <summary>
        /// Schreibt die Exception als HTML in den Ordner DEVError, von wo aus dann ein Email verschickt wird
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="ds">Betroffenes DataSet</param>
        public static void WriteErrorHTML(Exception ex, DataSet ds)
        {
            string rowErrors = string.Empty;
            foreach (DataTable tbl in ds.Tables)
            {
                foreach (DataRow oRow in tbl.Rows)
                {
                    if (oRow.RowError.Length > 0)
                    {
                        rowErrors += $"Tabelle '{tbl.TableName}': {oRow.RowError}{Environment.NewLine}";
                    }
                }
            }

            if (rowErrors.Length > 0)
            {
                WriteErrorHTML(new Exception(rowErrors, ex));
            }
            else
            {
                WriteErrorHTML(ex);
            }
        }

        #endregion

        #region Error-HTML schreiben mit Exception-Liste

        /// <summary>
        /// Fasst alle Exceptions aus cq zu einer zusammen und leert dabei cq (TryDequeue)
        /// </summary>
        /// <param name="cq"></param>
        public static void WriteErrorHTML(List<Exception> lstException)
        {
            string beginnError = $"<br>{LogDashes}<br>";
            StringBuilder error = new();
            HashSet<string> hsErrors = new();

            for (int i = 0; i < lstException.Count; i++)
            {
                Exception ex = lstException[i];
                if (hsErrors.Contains(ex.Message))
                {
                    continue;
                }
                else
                {
                    hsErrors.Add(ex.Message);
                }
                error.Append(beginnError);

                // Callstack-Infos hinzufügen
                StackFrame callStack = new(1, true);
                AddCallStackInfoToErrorText(callStack, error);

                // Infos von Fehler hinzufügen (inkl. allen Inner-Exceptions)
                AddErrorInfoToErrorText(ex, error);

                // Zeilenumbruch
                error.Append("<br>");
            }

            WriteErrorHTML(new Exception("Exception aus List", new Exception(error.ToString())));
        }

        #endregion

        #region Error-HTML schreiben mit Exception

        /// <summary>
        /// Schreibt die Exception als HTML in den Ordner DEVError, von wo aus dann ein Email verschickt wird
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="DEVError">Pfad des Ordners DEVError</param>
        public static void WriteErrorHTML(Exception ex)
        {
            // exit-handling
            if (ex == null) return;
            if (ex is AggregateException multiError)
            {
                var errorList = multiError.InnerExceptions.ToList();
                errorList.Insert(0, multiError);
                WriteErrorHTML(errorList);
                return;
            }

            try
            {
                // Assembly-Name ermitteln
                string assembly = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

                // DEV-Error-Ordnerpfad ermitteln
                const string deverror = "DEVError";
                string DEVError = Path.Combine(CoreFC.GetStartupFolderPath(), deverror);
                while (!Directory.Exists(DEVError))
                {
                    try
                    {
                        DEVError = Path.Combine(Directory.GetParent(Directory.GetParent(DEVError).ToString()).ToString(), deverror);
                    }
                    catch
                    {
                        DEVError = Path.Combine(CoreFC.GetStartupFolderPath(), deverror);
                        if (!Directory.Exists(DEVError)) Directory.CreateDirectory(DEVError);
                    }
                }

                // UserName + MachineName + Zeitpunkt setzen
                StringBuilder error = new StringBuilder("<b>").Append(assembly).Append("</b> (").Append(CoreFC.GetStartupFolderPath());
                //error.Append(")<br><br>UserName: ").Append(System.DirectoryServices.AccountManagement.UserPrincipal.Current.DisplayName).Append("<br>");
                error.Append(")<br><br>UserName: ").Append(Environment.UserName).Append("<br>");
                error.Append("MachineName: ").Append(Environment.MachineName).Append("<br>").Append(DateTime.Now.ToString(dd_MM_yyyy_HH_mm_ss)).Append("<br><br>");

                // Callstack-Infos hinzufügen
                StackFrame callStack = new(1, true);
                AddCallStackInfoToErrorText(callStack, error);

                // Infos von Fehler hinzufügen (inkl. allen Inner-Exceptions)
                AddErrorInfoToErrorText(ex, error);

                string XMLGUID = Guid.NewGuid().ToString();
                string bodystart = "<html><head><meta charset=\"UTF-8\"><meta name=\"XMLGUID\" content=\"" + XMLGUID + "\"></head><style>body,table{FONT-SIZE:12px;COLOR:#000000;FONT-FAMILY:Verdana,Geneva,Arial,Helvetica,sans-serif}</style><body>";
                string bodyend = "</body></html>";

                CurrentEnvironment? ErrorEnvironment = CoreFC.GetEnvironmentByStartupPath();
                //string ErrorEnvironmentName = Enum.GetName(typeof(CurrentEnvironment), ClsUtils.GetEnvironmentByStartupPath());
                string ErrorEnvironmentName = ErrorEnvironment.HasValue ? ErrorEnvironment.ToString() : "PROD";

                TC.Classes.clsMailEntry mailEntry = new()
                {
                    ApplicationName = assembly,
                    Customer = TC.Global.Global_TC_Core.CustomerId,
                    DBEnvironment = ErrorEnvironmentName,
                    ErrorDate = DateTime.Now,
                    MachineName = Environment.MachineName,
                    MailMessage = bodystart + error.ToString().Replace(Environment.NewLine, "<br>") + bodyend,
                    XMLGUID = XMLGUID
                };

                string errorxml = Path.Combine(DEVError, $"{assembly}_Error_{DateTime.Now:yyyyMMdd_HHmmss_ffff}{DotXML}");
                string errorhtml = errorxml.Replace(DotXML, ".html");
                Thread.Sleep(7);
                if (!Directory.Exists(Path.GetDirectoryName(errorxml))) Directory.CreateDirectory(Path.GetDirectoryName(errorxml));
                while (File.Exists(errorxml) || File.Exists(errorhtml))
                {
                    Thread.Sleep(1);
                    errorxml = Path.Combine(DEVError, $"{assembly}_Error_{DateTime.Now:yyyyMMdd_HHmmss_ffff}{DotXML}");
                    errorhtml = errorxml.Replace(DotXML, ".html");
                    Thread.Sleep(6);
                }
                File.WriteAllText(errorxml, mailEntry.XmlSerialize(), Encoding.UTF8);

                if (ErrorEnvironmentName.Equals(DEV))
                {
                    File.WriteAllText(errorhtml, bodystart + error + bodyend);
                }
            }
            catch (Exception eex)
            {
                throw new Exception("Unbehandelter Fehler beim Schreiben des Error-Emails:" + Environment.NewLine + Environment.NewLine + eex.ToString());
                //Exception exc = new Exception(eex.Message, ex);
                //WriteErrorHTML(exc);
            }
        }

        #endregion

        #region Weitere Methoden

        public static string XmlSerialize<T>(this T objectToSerialize)
        {
            using (StringWriter writer = new TC.Classes.Utf8StringWriter())
            {
                XmlTextWriter xmlWriter = new(writer);

                xmlWriter.Formatting = Formatting.Indented;
                new XmlSerializer(typeof(T)).Serialize(xmlWriter, objectToSerialize);
                return writer.ToString();
            }
        }

        // Stack-Frame von Error ermitteln (27.04.2023, SME)
        /// <summary>
        /// Ermittelt den Strack-Frame bzw. den Code-Punkt, von welchem der Fehler ausgelöst wurde
        /// </summary>
        /// <param name="ex">Fehler-Objekt</param>
        /// <returns></returns>
        private static StackFrame GetStackFrameFromError(Exception ex)
        {
            // exit-handling
            if (ex == null) return null;

            if (ex is CoreError tcgError)
            {
                return tcgError.ErrorCalledFrom;
            }
            else
            {
                return (new StackTrace(ex, true)).GetFrame(0);
            }
        }

        // Call-Stack-Info zu Error-Text hinzufügen (27.04.2023, SME)
        private static void AddCallStackInfoToErrorText(StackFrame callStack, StringBuilder error)
        {
            // exit-handling
            if (callStack == null) return;
            if (error == null) return;

            // Infos zwischenspeichern
            string file = callStack.GetFileName() ?? string.Empty;
            string meth = callStack.GetMethod().ToString() ?? string.Empty;
            string numb = callStack.GetFileLineNumber().ToString() ?? string.Empty;

            // Infos prüfen
            if (file.Length + meth.Length + numb.Length > 0)
            {
                // Infos hinzufügen
                error.Append("<br><table>");
                if (file.Length > 0) error.Append("<tr><td valign=\"top\"><b>File:</b> <td valign=\"top\">").Append(file).Append("</td></tr>");
                if (meth.Length > 0) error.Append("<tr><td valign=\"top\"><b>Method:</b> <td valign=\"top\">").Append(meth).Append("</td></tr>");
                if (numb.Length > 0) error.Append("<tr><td valign=\"top\"><b>Line:</b> <td valign=\"top\">").Append(numb).Append("</td></tr>");
                error.Append("</table>");
            }
        }

        // Infos über Error-Objekt zu Error-Text hinzufügen (inkl. allen Inner-Exceptions) (27.04.2023, SME)
        private static void AddErrorInfoToErrorText(Exception ex, StringBuilder errorText)
        {
            // exit-handling
            if (ex == null) return;
            if (errorText == null) return;

            // Fehlermeldung hinzufügen
            errorText.Append("<br><b>").Append(ex.Message).Append("</b><br>");

            // Callstack-Infos von Fehler hinzufügen
            AddCallStackInfoToErrorText(GetStackFrameFromError(ex), errorText);

            // Source + StackTrace + TargetSite hinzufügen
            if (ex.Source != null && ex.Source.Length > 0) errorText.Append("<br><br><b>Source:</b> ").Append(ex.Source);
            if (ex.StackTrace != null && ex.StackTrace.Length > 0) errorText.Append("<br><br><b>StackTrace:</b> <br>").Append(ex.StackTrace);
            if (ex.TargetSite != null && ex.TargetSite.ToString().Length > 0) errorText.Append("<br><br><b>TargetSite:</b><br>").Append(ex.TargetSite);

            // Inner Exception(s) abhandeln
            while (ex.InnerException != null)
            {
                // Inner Exception zwischenspeichern
                ex = ex.InnerException;

                // Titel hinzufügen
                errorText.Append("<br><br><br><b>InnerException:</b><br><table><tr><td>&nbsp;&nbsp;&nbsp;&nbsp;</td><td colspan=\"2\" valign=\"top\">");
                errorText.Append("<b>").Append(ex.Message).Append("</b></td></tr>");

                try
                {
                    StackFrame callStack = GetStackFrameFromError(ex);
                    if (callStack != null)
                    {
                        string file = callStack.GetFileName() ?? string.Empty;
                        string meth = callStack.GetMethod().ToString() ?? string.Empty;
                        string numb = callStack.GetFileLineNumber().ToString() ?? string.Empty;
                        if (file.Length > 0) errorText.Append("<tr><td></td><td valign=\"top\"><b>File:&nbsp;</b></td><td valign=\"top\">").Append(file).Append("</td></tr>");
                        if (meth.Length > 0) errorText.Append("<tr><td></td><td valign=\"top\"><b>Method:&nbsp;</b></td><td valign=\"top\">").Append(meth).Append("</td></tr>");
                        if (numb.Length > 0) errorText.Append("<tr><td></td><td valign=\"top\"><b>Line:&nbsp;</b></td><td valign=\"top\">").Append(numb).Append("</td></tr>");
                    }
                }
                catch { }
                if (ex.Source != null && ex.Source.Length > 0) errorText.Append("<tr><td></td><td valign=\"top\"><b>Source:&nbsp;</b></td><td valign=\"top\">").Append(ex.Source).Append("</td></tr>");
                if (ex.StackTrace != null && ex.StackTrace.Length > 0) errorText.Append("<tr><td></td><td valign=\"top\"><b>StackTrace:&nbsp;</b></td><td valign=\"top\">").Append(ex.StackTrace).Append("</td></tr>");
                if (ex.TargetSite != null && ex.TargetSite.ToString().Length > 0) errorText.Append("<tr><td></td><td valign=\"top\"><b>TargetSite:&nbsp;</b></td><td valign=\"top\">").Append(ex.TargetSite).Append("</td></tr>");
                errorText.Append("</table><br><br>");
            }
        }

        #endregion
    }
}
