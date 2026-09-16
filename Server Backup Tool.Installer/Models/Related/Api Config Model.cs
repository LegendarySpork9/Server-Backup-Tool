// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Values;

namespace ServerBackupTool.Installer.Models.Related
{
    /// <summary>
    /// Stores the API configuration settings for the installer.
    /// </summary>
    public class ApiConfigModel
    {
        public string BindAddress { get; set; } = InstallerValues.Defaults.ApiBindAddress;
        public int HttpPort { get; set; } = InstallerValues.Defaults.ApiHttpPort;
        public int HttpsPort { get; set; } = InstallerValues.Defaults.ApiHttpsPort;
        public bool EnableHttps { get; set; }
        public string CertificatePath { get; set; } = string.Empty;
        public string CertificatePassword { get; set; } = string.Empty;
        public string DatabasePath { get; set; } = string.Empty;
        public string ArchiveDirectory { get; set; } = InstallerValues.Defaults.ArchiveDirectory;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ClientIdHash { get; set; } = string.Empty;
        public string ClientSecretHash { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
