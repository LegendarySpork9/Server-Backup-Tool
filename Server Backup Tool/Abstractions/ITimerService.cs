// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Models.Configuration;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the timer service operations.
    /// </summary>
    public interface ITimerService
    {
        string SetTimers(TimerCollection timerDetails, TimeSpan[] timerDurations);
        void StartTimers();
        void StartQueuedCommandCheckTimer();
        void RestartHeartbeat();
        void WaitForClose();
    }
}
