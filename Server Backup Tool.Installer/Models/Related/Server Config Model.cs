// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Values;

namespace ServerBackupTool.Installer.Models.Related
{
    /// <summary>
    /// Stores the configuration for a server in the installer.
    /// </summary>
    public class ServerConfigModel
    {
        public string ServerName { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public string ServerDirectory { get; set; } = string.Empty;
        public string StartFile { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string DatabasePath { get; set; } = string.Empty;
        public int PollingIntervalMs { get; set; } = InstallerValues.Defaults.PollingIntervalMs;
    }
}
