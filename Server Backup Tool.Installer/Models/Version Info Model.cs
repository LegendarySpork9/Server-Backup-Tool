// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Models
{
    /// <summary>
    /// Stores the version information for the server backup tool and its API.
    /// </summary>
    public class VersionInfoModel
    {
        public string ServerName { get; set; } = string.Empty;
        public string ToolVersion { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
        public string ApiInstallPath { get; set; } = string.Empty;
        public string ToolTaskName { get; set; } = string.Empty;
        public string ApiTaskName { get; set; } = string.Empty;
        public DateTime InstalledAt { get; set; }
    }
}
