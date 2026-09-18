// Copyright © - Unpublished - Toby Hunter
using Microsoft.Win32;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using System.Runtime.Versioning;

namespace ServerBackupTool.Installer.Implementations
{
    [SupportedOSPlatform("windows")]
    public class RegistryService : IRegistryService
    {
        private readonly ILoggerService _Logger;
        private readonly RegistryKey _RegistryRoot;

        // Sets the class's global variables.
        public RegistryService(
            ILoggerService logger,
            RegistryKey? registryRoot = null)
        {
            _Logger = logger;
            _RegistryRoot = registryRoot ?? Registry.LocalMachine;
        }

        /// <summary>
        /// Writes the uninstall registry entry for Add/Remove Programs using a per-server key.
        /// </summary>
        public (bool, Exception?) WriteUninstallEntry(
            string serverName,
            string installPath,
            string apiInstallPath,
            string toolVersion,
            string apiVersion,
            string toolTaskName,
            string apiTaskName)
        {
            bool written = false;
            Exception? exception = null;

            try
            {
                string keyPath = $@"{InstallerValues.Registry.UninstallKeyBase}\{InstallerValues.Registry.UninstallKeyPrefix}_{serverName}";

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Writing uninstall registry entry at '{keyPath}'.");

                using (RegistryKey key = _RegistryRoot.CreateSubKey(keyPath))
                {
                    key.SetValue(
                        "DisplayName",
                        $"{InstallerValues.Registry.DisplayName} - {serverName}");
                    key.SetValue(
                        "DisplayVersion",
                        toolVersion);
                    key.SetValue(
                        "ToolVersion",
                        toolVersion);
                    key.SetValue(
                        "ApiVersion",
                        apiVersion);
                    key.SetValue(
                        "Publisher",
                        InstallerValues.Registry.Publisher);
                    key.SetValue(
                        "InstallLocation",
                        installPath);
                    key.SetValue(
                        "UninstallString",
                        $"\"{Path.Combine(
                            installPath,
                            "ServerBackupToolInstaller.exe")}\" --uninstall");
                    key.SetValue(
                        "DisplayIcon",
                        Path.Combine(
                            installPath,
                            @"Content\Logo.ico"));
                    key.SetValue(
                        "NoModify",
                        1,
                        RegistryValueKind.DWord);
                    key.SetValue(
                        "NoRepair",
                        1,
                        RegistryValueKind.DWord);
                    key.SetValue(
                        "InstallDate",
                        DateTime.UtcNow.ToString("yyyyMMdd"));
                    key.SetValue(
                        "ApiInstallLocation",
                        apiInstallPath);
                    key.SetValue(
                        "ToolTaskName",
                        toolTaskName);
                    key.SetValue(
                        "ApiTaskName",
                        apiTaskName);
                    key.SetValue("ServerName", serverName);
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Uninstall registry entry written.");

                written = true;
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Write Registry Entry: {ex.Message}");

                exception = ex;
            }

            return (
                written,
                exception);
        }

        /// <summary>
        /// Removes the uninstall registry entry for the specified server name.
        /// </summary>
        public (bool, Exception?) RemoveUninstallEntry(string serverName)
        {
            bool removed = false;
            Exception? exception = null;

            try
            {
                string keyPath = $@"{InstallerValues.Registry.UninstallKeyBase}\{InstallerValues.Registry.UninstallKeyPrefix}_{serverName}";

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Removing uninstall registry entry at '{keyPath}'.");

                _RegistryRoot.DeleteSubKeyTree(
                    keyPath,
                    false);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Uninstall registry entry removed.");

                removed = true;
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Remove Registry Entry: {ex.Message}");

                exception = ex;
            }

            return (
                removed,
                exception);
        }

        /// <summary>
        /// Reads the installed version info from the registry for the specified server name.
        /// </summary>
        public VersionInfoModel? ReadUninstallEntry(string serverName)
        {
            VersionInfoModel? versionInfo = null;

            string keyPath = $@"{InstallerValues.Registry.UninstallKeyBase}\{InstallerValues.Registry.UninstallKeyPrefix}_{serverName}";

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Reading uninstall registry entry at '{keyPath}'.");

            using (RegistryKey? key = _RegistryRoot.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    versionInfo = ReadVersionInfoFromKey(
                        key,
                        serverName);
                }
            }

            return versionInfo;
        }

        /// <summary>
        /// Enumerates all ServerBackupTool installations from the registry.
        /// </summary>
        public List<VersionInfoModel> GetAllInstallations()
        {
            List<VersionInfoModel> installations = [];

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Enumerating all ServerBackupTool installations from registry.");

            using (RegistryKey? uninstallKey = _RegistryRoot.OpenSubKey(InstallerValues.Registry.UninstallKeyBase))
            {
                if (uninstallKey != null)
                {
                    string[] subKeyNames = uninstallKey.GetSubKeyNames();

                    foreach (string subKeyName in subKeyNames)
                    {
                        if (subKeyName.StartsWith(
                            $"{InstallerValues.Registry.UninstallKeyPrefix}_",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            string extractedServerName = subKeyName[(InstallerValues.Registry.UninstallKeyPrefix.Length + 1)..];

                            using (RegistryKey? subKey = uninstallKey.OpenSubKey(subKeyName))
                            {
                                if (subKey != null)
                                {
                                    VersionInfoModel? info = ReadVersionInfoFromKey(
                                        subKey,
                                        extractedServerName);

                                    if (info != null)
                                    {
                                        installations.Add(info);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Found {installations.Count} installation(s).");

            return installations;
        }

        /// <summary>
        /// Reads a VersionInfoModel from an open registry key.
        /// </summary>
        private static VersionInfoModel? ReadVersionInfoFromKey(
            RegistryKey key,
            string serverName)
        {
            VersionInfoModel? versionInfo = null;

            string? toolVersion = key.GetValue("ToolVersion") as string ?? key.GetValue("DisplayVersion") as string;
            string? apiVersion = key.GetValue("ApiVersion") as string;
            string? installPath = key.GetValue("InstallLocation") as string;
            string? apiInstallPath = key.GetValue("ApiInstallLocation") as string;
            string? installDate = key.GetValue("InstallDate") as string;
            string? toolTaskName = key.GetValue("ToolTaskName") as string;
            string? apiTaskName = key.GetValue("ApiTaskName") as string;
            string? storedServerName = key.GetValue("ServerName") as string;

            if (!string.IsNullOrEmpty(toolVersion) && !string.IsNullOrEmpty(installPath))
            {
                DateTime installedAt = DateTime.TryParseExact(
                    installDate,
                    "yyyyMMdd",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime parsed) ? parsed : DateTime.MinValue;

                versionInfo = new VersionInfoModel
                {
                    ServerName = storedServerName ?? serverName,
                    ToolVersion = toolVersion,
                    ApiVersion = apiVersion ?? string.Empty,
                    InstallPath = installPath,
                    ApiInstallPath = apiInstallPath ?? string.Empty,
                    ToolTaskName = toolTaskName ?? string.Empty,
                    ApiTaskName = apiTaskName ?? string.Empty,
                    InstalledAt = installedAt
                };
            }

            return versionInfo;
        }
    }
}
