// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Models;

namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for the version service.
    /// </summary>
    public interface IVersionService
    {
        List<VersionInfoModel> GetAllInstallations();
        VersionInfoModel? GetInstalledVersion(string serverName);
        bool IsToolUpdateAvailable(string installedToolVersion, string bundledToolVersion);
        bool IsApiUpdateAvailable(string installedApiVersion, string bundledApiVersion);
        string GetBundledToolVersion(string installPath);
        string GetBundledApiVersion(string apiInstallPath);
        string GetEmbeddedToolVersion();
        string GetEmbeddedApiVersion();
    }
}
