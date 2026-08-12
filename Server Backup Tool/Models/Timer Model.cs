// Copyright © - 31/10/2024 - Toby Hunter
using Timer = System.Timers.Timer;

namespace ServerBackupTool.Models
{
    /// <summary>
    /// Stores the information about the timers.
    /// </summary>
    public class TimerModel
    {
        public required string TimerName { get; set; }
        public string? ElapsedMessage { get; set; }
        public bool Triggered { get; set; } = false;
        public required Timer TimerData { get; set; }
    }
}
