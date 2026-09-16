// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Values;

namespace ServerBackupTool.Installer.Models.Related
{
    /// <summary>
    /// Stores the email configuration settings for the installer.
    /// </summary>
    public class EmailConfigModel
    {
        public bool Enabled { get; set; }
        public int Port { get; set; } = InstallerValues.Defaults.SmtpPort;
        public bool EnableSSL { get; set; } = InstallerValues.Defaults.EnableSSL;
        public string SmtpHost { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = InstallerValues.Defaults.FromName;
        public List<EmailTemplateModel> Emails { get; set; } = [];
    }
}
