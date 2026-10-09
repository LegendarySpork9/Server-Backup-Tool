// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Models.Related;

namespace ServerBackupTool.Installer.Models
{
    /// <summary>
    /// Stores the install options for the installer.
    /// </summary>
    public class InstallOptionsModel
    {
        public List<string> Components { get; set; } = [];
        public string InstallPath { get; set; } = string.Empty;
        public string ApiInstallPath { get; set; } = string.Empty;
        public string ToolTaskName { get; set; } = string.Empty;
        public string ApiTaskName { get; set; } = string.Empty;
        public ServerConfigModel ServerConfig { get; set; } = new();
        public TimerConfigModel TimerConfig { get; set; } = new();
        public EmailConfigModel? EmailConfig { get; set; }
        public ApiConfigModel? ApiConfig { get; set; }
    }
}
