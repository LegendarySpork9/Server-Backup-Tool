﻿// Copyright © - unpublished - Toby Hunter
using Timer = System.Timers.Timer;

namespace ServerBackupTool.Models
{
    internal class TimerModel
    {
        public string? TimerName { get; set; }
        public string? ElapsedMessage { get; set; }
        public bool Triggered { get; set; } = false;
        public Timer? TimerData { get; set; }
    }
}
