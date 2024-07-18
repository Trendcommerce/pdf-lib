using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TC.Interfaces;

namespace TC.Classes
{
    // Object with Progress-Info (26.11.2022, SME)
    public abstract class ProgressInfoObject : IProgressInfoObject
    {
        #region Progress-Handling

        // Event: Progress-Info changed
        public event EventHandler ProgressInfoChanged;
        // Event-Raiser: Progress-Info changed
        public void OnProgressInfoChanged(object sender, EventArgs e)
        {
            ProgressInfoChanged?.Invoke(sender, e);
        }

        // Progress-Info
        private ProgressInfo _ProgressInfo;
        public ProgressInfo ProgressInfo => _ProgressInfo;

        // Is Progress-Running
        public bool IsProgressRunning => ProgressInfo != null && ProgressInfo.IsRunning;

        // Start Progress
        public void StartProgress(int steps, string status = "")
        {
            if (IsProgressRunning) throw new Exception("Progress läuft bereits!");
            _ProgressInfo = new();
            OnProgressInfoChanged(this, EventArgs.Empty);
            ProgressInfo.Start(steps, status);
        }

        // End Progress
        public void EndProgress()
        {
            if (IsProgressRunning) ProgressInfo.End();
        }

        // Perform Progress-Step
        public void PerformProgressStep(int count = 1)
        {
            if (IsProgressRunning) ProgressInfo.PerformStep(count);
        }

        // Set Progress-Status
        public void SetProgressStatus(string status, bool raiseChangeEvent = true, bool forceRaiseChangeEvent = false)
        {
            if (IsProgressRunning) ProgressInfo.SetStatus(status, raiseChangeEvent, forceRaiseChangeEvent);
        }

        #endregion
    }
}
