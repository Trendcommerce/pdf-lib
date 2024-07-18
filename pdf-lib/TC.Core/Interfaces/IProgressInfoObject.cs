using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TC.Classes;

namespace TC.Interfaces
{
    // Interface for Progress-Info-Object (12.04.2023, SME)
    public interface IProgressInfoObject
    {
        // Event: Progress-Info changed
        public event EventHandler ProgressInfoChanged;

        // Event-Raiser: Progress-Info changed
        void OnProgressInfoChanged(object sender, EventArgs e);

        // Progress-Info
        public ProgressInfo ProgressInfo { get; }

        // Is Progress-Running
        public bool IsProgressRunning { get; }

        // Start Progress
        void StartProgress(int steps, string status = "");

        // End Progress
        void EndProgress();

        // Perform Progress-Step
        void PerformProgressStep(int count = 1);

        // Set Progress-Status
        void SetProgressStatus(string status, bool raiseChangeEvent = true, bool forceRaiseChangeEvent = false);
    }
}
