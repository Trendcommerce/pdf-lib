using System;
using TC.Functions;

namespace TC.Classes
{
    public class ProgressInfo //: System.ComponentModel.ISynchronizeInvoke
    {
        #region General

        // New Instance (26.11.2022, SME)
        public ProgressInfo() //double refreshTimerInterval = 250, System.ComponentModel.ISynchronizeInvoke synchronizingObject = null)
        {
            //// error-handling
            //if (refreshTimerInterval <= 0) throw new ArgumentOutOfRangeException(nameof(refreshTimerInterval));

            //// initialize refresh-timer
            //InitializeRefreshTimer(refreshTimerInterval, synchronizingObject);
        }

        #endregion

        #region Errors

        public class ProgressCancelError: System.Exception
        {
            public readonly ProgressInfo ProgressInfo;
            public ProgressCancelError(ProgressInfo progressInfo): base("Prozess abgebrochen")
            {
                ProgressInfo = progressInfo;
            }
        }

        #endregion

        #region Events

        public event EventHandler Started;
        public event EventHandler Cancelled;
        public event EventHandler Ended;
        public event EventHandler StepPerformed;
        public event EventHandler StatusChanged;
        public event EventHandler TotalStepsChanged;
        public event EventHandler PerformRefresh;

        #endregion

        #region Properties

        //  started on
        private DateTime? _StartedOn;
        public DateTime? StartedOn => _StartedOn;

        //  ended on
        private DateTime? _EndedOn;
        public DateTime? EndedOn => _EndedOn;

        // Duration
        public TimeSpan Duration
        {
            get
            {
                if (!StartedOn.HasValue) return TimeSpan.Zero;
                if (!EndedOn.HasValue) return DateTime.Now - StartedOn.Value;
                return EndedOn.Value - StartedOn.Value;
            }
        }

        // Status-Prefix (24.05.2023, SME)
        private string _StatusPrefix;
        public string StatusPrefix => _StatusPrefix;

        // Status
        private string _Status;
        public string Status => GetStatus();

        // Steps
        private int _TotalSteps;
        public int TotalSteps => _TotalSteps;

        // Step
        private int _Step;
        public int Step => _Step;

        // Is Running
        public bool IsRunning => StartedOn.HasValue && !EndedOn.HasValue;

        // Main-Action (24.05.2023, SME)
        private object _MainAction;
        public object MainAction => _MainAction;

        // Refresh-Step-Interval (03.02.2024, SME)
        private int RefreshStepInterval = 1;

        // Next Refresh on Step
        private int NextRefreshOnStep = 0;

        #endregion

        #region Methods

        // Start without Main-Action
        public void Start(int totalSteps, string status = "")
        {
            Start(totalSteps, null, status);
        }

        // Start with Main-Action (24.05.2023, SME)
        public void Start(int totalSteps, object mainAction, string status = "")
        {
            if (IsRunning) throw new Exception("Prozess wurde bereits gestartet");
            if (IsCancelled) throw new Exception("Prozess wurde abgebrochen");
            if (totalSteps < 0) throw new ArgumentOutOfRangeException(nameof(totalSteps));
            _StartedOn = DateTime.Now;
            _TotalSteps = totalSteps;
            _Status = status;
            _MainAction = mainAction;
            //StartRefreshTimer();
            Started?.Invoke(this, EventArgs.Empty);

            // refresh (03.02.2024, SME)
            this.Refresh();

            // Update Refresh-Step-Interval (03.02.2024, SME)
            UpdateRefreshStepInterval();
        }

        // Update Refresh-Step-Interval (03.02.2024, SME)
        private void UpdateRefreshStepInterval()
        {
            try
            {
                if (this.TotalSteps == 0)
                {
                    RefreshStepInterval = 1;
                }
                else if (this.TotalSteps <= 100)
                {
                    RefreshStepInterval = 1;
                }
                else
                {
                    var interval = this.TotalSteps / 250; // every 0.25%
                    if (interval < 1) interval = 1;
                    RefreshStepInterval = interval;
                }

                // Update next Refresh on Step (03.02.2024, SME)
                UpdateNextRefreshOnStep();
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Update next Refresh on Step (03.02.2024, SME)
        private void UpdateNextRefreshOnStep()
        {
            try
            {
                NextRefreshOnStep = this.Step + RefreshStepInterval;
            }
            catch (Exception ex)
            {
                CoreFC.ThrowError(ex); throw ex;
            }
        }

        // Set Total Steps
        public void SetTotalSteps(int totalSteps)
        {
            if (TotalSteps == totalSteps) return;
            if (TotalSteps != 0) throw new Exception("Anzahl Schritte sind bereits definiert und können nicht überschrieben werden");
            if (totalSteps < 0) throw new ArgumentOutOfRangeException(nameof(totalSteps));
            if (!IsRunning) throw new Exception("Anzahl Schritte können erst überschrieben werden, wenn der Prozess bereits gestartet ist.");

            // set total steps
            _TotalSteps = totalSteps;

            // raise change-event
            TotalStepsChanged?.Invoke(this, EventArgs.Empty);

            // refresh (03.02.2024, SME)
            this.Refresh();

            // Update Refresh-Step-Interval (03.02.2024, SME)
            UpdateRefreshStepInterval();
        }

        // End 
        public void End()
        {
            if (!IsRunning) return;
            _EndedOn = DateTime.Now;

            var percent = GetPercentDone();
            if (percent != 100)
            {
                CoreFC.DPrint($"Progress ended with {percent} %.");
            }
            
            //StopRefreshTimer();
            
            Ended?.Invoke(this, EventArgs.Empty);

            // refresh (03.02.2024, SME)
            this.Refresh();

            // clear memory (14.06.2023, SME)
            GC.Collect();
        }

        // Perform Step
        public void PerformStep(int count = 1)
        {
            if (IsCancelled) throw new ProgressCancelError(this);
            _Step += count;
            StepPerformed?.Invoke(this, EventArgs.Empty);

            // handle refresh (03.02.2024, SME)
            if (Step >= NextRefreshOnStep)
            {
                Refresh();
                UpdateNextRefreshOnStep();
            }
        }

        // Set Status
        public void SetStatus(string status, bool raiseChangeEvent = true, bool forceRaiseChangeEvent = false)
        {
            if (IsCancelled) throw new ProgressCancelError(this);
            if (Status != status)
            {
                _Status = status;
                if (raiseChangeEvent) StatusChanged?.Invoke(this, EventArgs.Empty);
                Refresh();
            } 
            else if (forceRaiseChangeEvent)
            {
                StatusChanged?.Invoke(this, EventArgs.Empty);
                Refresh();
            }
        }

        // Set Status-Prefix (24.05.2023, SME)
        public void SetStatusPrefix(string statusPrefix, bool raiseChangeEvent = true, bool forceRaiseChangeEvent = false)
        {
            if (IsCancelled) throw new ProgressCancelError(this);
            if (StatusPrefix != statusPrefix)
            {
                _StatusPrefix = statusPrefix;
                if (raiseChangeEvent)
                    StatusChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (forceRaiseChangeEvent)
            {
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Set Status-Prefix + Status (24.05.2023, SME)
        public void SetStatusAndPrefix(string status, string statusPrefix, bool raiseChangeEvent = true, bool forceRaiseChangeEvent = false)
        {
            if (IsCancelled) throw new ProgressCancelError(this);
            if (StatusPrefix != statusPrefix || _Status != status)
            {
                _Status = status;
                _StatusPrefix = statusPrefix;
                if (raiseChangeEvent)
                    StatusChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (forceRaiseChangeEvent)
            {
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Get Percent Done
        public int GetPercentDone()
        {
            if (TotalSteps == 0) return 0;
            else if (Step == 0) return 0;
            else return Step * 100 / TotalSteps;
        }

        // Get Duration-String
        public string GetDurationString() => CoreFC.GetDurationString(Duration);

        // Get Status (24.05.2023, SME)
        private string GetStatus()
        {
            if (string.IsNullOrEmpty(StatusPrefix)) return _Status;
            if (string.IsNullOrEmpty(_Status)) return StatusPrefix;
            return StatusPrefix + " " + _Status;
        }

        // Refresh (30.10.2023, SME)
        public void Refresh()
        {
            PerformRefresh?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Cancel-Handling

        // Cancelled on
        private DateTime? _CancelledOn;
        public DateTime? CancelledOn => _CancelledOn;

        // Is Cancelled
        public bool IsCancelled => _CancelledOn.HasValue;

        // Cancel
        public void Cancel()
        {
            if (!IsCancelled)
            {
                _CancelledOn = DateTime.Now;
                _Status = "Prozess wird abgebrochen ...";
                StatusChanged?.Invoke(this, EventArgs.Empty);
                Cancelled?.Invoke(this, EventArgs.Empty);

                // refresh (03.02.2024, SME)
                this.Refresh();
            }
        }

        #endregion

        #region Refresh-Timer-Handling (REMARKED)

        //// Event: Refresh Infos
        //public event EventHandler RefreshInfos;
        //private void OnRefreshInfos()
        //{
        //    RefreshInfos?.Invoke(this, EventArgs.Empty);
        //}

        //// Refresh Timer
        //private System.Timers.Timer RefreshTimer;

        //// Initialize Refresh-Timer
        //private void InitializeRefreshTimer(double interval, System.ComponentModel.ISynchronizeInvoke synchronizingObject)
        //{
        //    if (RefreshTimer == null)
        //    {
        //        RefreshTimer = new System.Timers.Timer();
        //        RefreshTimer.Interval = interval;
        //        RefreshTimer.Elapsed += RefreshTimer_Elapsed;
        //        RefreshTimer.SynchronizingObject = synchronizingObject;
        //    }
        //}

        //// Start Refresh Timer
        //private void StartRefreshTimer()
        //{
        //    RefreshTimer.Stop();
        //    RefreshTimer.Start();
        //}

        //// Stop Refresh Timer
        //private void StopRefreshTimer()
        //{
        //    if (RefreshTimer != null)
        //    {
        //        RefreshTimer.Stop();
        //    }
        //}

        //// Event-Handler: Tick on Refresh-Timer
        //private void RefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    try
        //    {
        //        CoreFC.DPrint("Tick on Refresh-Timer in Progress-Info");
        //        //_InvokeRequired = true;

        //        //this.Invoke(() => OnRefreshInfos(), null);

        //        RefreshInfos?.Invoke(this, EventArgs.Empty);
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //    finally
        //    {
        //        //_InvokeRequired = false;
        //    }
        //}

        #endregion

        #region Invoke-Handling (REMARKED)

        //public IAsyncResult BeginInvoke(Delegate method, object[] args)
        //{
        //    if (method == null) return null;
        //    else return method.DynamicInvoke(args) as IAsyncResult;
        //}

        //public object EndInvoke(IAsyncResult result)
        //{
        //    throw new NotImplementedException();
        //}

        //public object Invoke(Delegate method, object[] args)
        //{
        //    try
        //    {
        //        if (method != null)
        //        {
        //            return method.DynamicInvoke(args); 
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //private bool _InvokeRequired;
        //public bool InvokeRequired 
        //{
        //    get
        //    {
        //        if (_InvokeRequired) return true;
        //        else return false;
        //    }
        //}

        #endregion
    }
}
