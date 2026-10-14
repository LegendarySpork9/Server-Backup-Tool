// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Models;

namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for the Windows Registry service.
    /// </summary>
    public interface IRegistryService
    {
        (bool, Exception?) WriteUninstallEntry(string serverName, string installPath, string apiInstallPath, string toolVersion, string apiVersion, string toolTaskName, string apiTaskName);
        (bool, Exception?) RemoveUninstallEntry(string serverName);
        VersionInfoModel? ReadUninstallEntry(string serverName);
        List<VersionInfoModel> GetAllInstallations();
    }
}
