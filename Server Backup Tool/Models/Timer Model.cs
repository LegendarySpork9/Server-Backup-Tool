// Copyright © - 31/10/2024 - Toby Hunter
using Timer = System.Timers.Timer;

namespace ServerBackupTool.Models
{
    // Stores the information about the timers.
    public class TimerModel
    {
        public string? TimerName { get; set; }
        public string? ElapsedMessage { get; set; }
        public bool Triggered { get; set; } = false;
        public Timer? TimerData { get; set; }
    }
}
