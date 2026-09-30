// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Models
{
    /// <summary>
    /// Stores the configuration for a custom timer.
    /// </summary>
    public class CustomTimerModel
    {
        public string Name { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
