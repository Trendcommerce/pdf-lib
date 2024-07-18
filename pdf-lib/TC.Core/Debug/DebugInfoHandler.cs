using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TC.Functions;

namespace TC.Debug
{
    // Debug-Info-Handler (11.11.2022, SME)
    public class DebugInfoHandler
    {
        #region General

        // New Instance (11.11.2022, SME)
        public DebugInfoHandler()
        {
            // initialize settings
            InitSettings();
        }

        #endregion

        #region Classes

        public class DebugInfoLineEventArgs : System.EventArgs
        {
            public readonly string DebugInfo;

            public DebugInfoLineEventArgs(string debugInfo)
            {
                DebugInfo = debugInfo;
            }
        }

        #endregion

        #region Events

        // Info written
        public delegate void InfoWrittenEventHandler(object sender, DebugInfoLineEventArgs e);
        public event InfoWrittenEventHandler InfoWritten;
        protected virtual void OnInfoWritten(DebugInfoLineEventArgs e)
        {
            InfoWritten?.Invoke(this, e);
        }
        protected virtual void OnInfoWritten(string info)
        {
            OnInfoWritten(new DebugInfoLineEventArgs(info));
        }

        #endregion

        #region Settings

        // Initialize Settings (11.11.2022, SME)
        private void InitSettings()
        {
            WriteToConsole = true;
            RaiseEvent = true;
        }

        // Flag that defines if info will be written to console (11.11.2022, SME)
        public bool WriteToConsole { get; set; }

        // Flag that defines if event will be raised (11.11.2022, SME)
        public bool RaiseEvent { get; set; }

        #endregion

        #region Methods

        // Write Debug-Info (11.11.2022, SME)
        public void WriteInfo(string info)
        {
            // Write to Console
            if (WriteToConsole) CoreFC.DPrint(info);

            // Raise Event
            if (RaiseEvent) OnInfoWritten(info);
        }

        // Write Error (11.11.2022, SME)
        public void WriteError(Exception error, MethodBase method)
        {
            string text = "ERROR of type '" + error.GetType().Name + "' in '" + method.Name + "': " + error.Message;
            WriteInfo(text);
        }

        // Write Event-Info (11.11.2022, SME)
        public void WriteEventInfo(MethodBase method, object sender, EventArgs e)
        {
            string text = "Event-Info: Method = {0}, Sender-Type = {1}, Sender-ToString = {2}, EventArgs-Type = {3}, EventArgs-ToString = {4}";
            text = string.Format(text, method.ToString(), sender.GetType().ToString(), sender.ToString(), e.GetType().ToString(), e.ToString());
            WriteInfo(text);
        }

        #endregion
    }
}
