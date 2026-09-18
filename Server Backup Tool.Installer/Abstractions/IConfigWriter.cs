// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for the configuration file writer.
    /// </summary>
    public interface IConfigWriter
    {
        XDocument GenerateAppConfig(InstallOptionsModel options);
        string GenerateApiAppSettings(ApiConfigModel apiConfig, ServerConfigModel serverConfig);
        Task<(bool, Exception?)> WriteConfig(string path, XDocument config);
        Task<(bool, Exception?)> WriteApiSettings(string path, string json);
        (bool, List<string>) MigrateAppConfig(XDocument existingConfig, XDocument referenceConfig);
        (bool, List<string>) MigrateApiAppSettings(string existingJson, string referenceJson);
    }
}
