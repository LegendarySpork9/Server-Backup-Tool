// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Models.Related
{
    /// <summary>
    /// Stores the configuration for timers.
    /// </summary>
    public class TimerConfigModel
    {
        public string BackupTime { get; set; } = string.Empty;
        public List<CustomTimerModel> CustomTimers { get; set; } = [];
    }
}
