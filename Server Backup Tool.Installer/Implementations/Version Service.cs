// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using System.Reflection;

namespace ServerBackupTool.Installer.Implementations
{
    public class VersionService : IVersionService
    {
        private readonly ILoggerService _Logger;
        private readonly IRegistryService _RegistryService;
        private readonly IResourceService _ResourceService;
        private readonly IFileSystem _FileSystem;

        // Sets the class's global variables.
        public VersionService(
            ILoggerService logger,
            IRegistryService registryService,
            IResourceService resourceService,
            IFileSystem fileSystem)
        {
            _Logger = logger;
            _RegistryService = registryService;
            _ResourceService = resourceService;
            _FileSystem = fileSystem;
        }

        /// <summary>
        /// Retrieves all installed versions from the registry.
        /// </summary>
        public List<VersionInfoModel> GetAllInstallations()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Retrieving all installations from registry.");

            List<VersionInfoModel> installations = _RegistryService.GetAllInstallations();

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Found {installations.Count} installation(s).");

            return installations;
        }

        /// <summary>
        /// Reads the installed version from the registry for a specific server name.
        /// </summary>
        public VersionInfoModel? GetInstalledVersion(string serverName)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Reading installed version from registry for server '{serverName}'.");

            VersionInfoModel? version = _RegistryService.ReadUninstallEntry(serverName);

            if (version == null)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"No installed version found for server '{serverName}'.");
            }

            else
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Installed tool version: {version.ToolVersion}, API version: {version.ApiVersion} at {version.InstallPath}.");
            }

            return version;
        }

        /// <summary>
        /// Checks whether the bundled tool version is newer than the installed tool version.
        /// </summary>
        public bool IsToolUpdateAvailable(
            string installedToolVersion,
            string bundledToolVersion)
        {
            bool updateAvailable = false;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Comparing installed tool version '{installedToolVersion}' with bundled tool version '{bundledToolVersion}'.");

            if (Version.TryParse(
                installedToolVersion,
                out Version? installed) && Version.TryParse(
                    bundledToolVersion,
                    out Version? bundled))
            {
                updateAvailable = bundled > installed;
            }

            return updateAvailable;
        }

        /// <summary>
        /// Checks whether the bundled API version is newer than the installed API version.
        /// </summary>
        public bool IsApiUpdateAvailable(
            string installedApiVersion,
            string bundledApiVersion)
        {
            bool updateAvailable = false;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Comparing installed API version '{installedApiVersion}' with bundled API version '{bundledApiVersion}'.");

            if (Version.TryParse(
                installedApiVersion,
                out Version? installed) && Version.TryParse(
                    bundledApiVersion,
                    out Version? bundled))
            {
                updateAvailable = bundled > installed;
            }

            return updateAvailable;
        }

        /// <summary>
        /// Reads the version from the deployed Server Backup Tool assembly.
        /// </summary>
        public string GetBundledToolVersion(string installPath)
        {
            string bundledToolVersion = "0.0.0";

            string assemblyPath = Path.Combine(
                installPath,
                "ServerBackupTool.dll");

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Reading tool version from '{assemblyPath}'.");

            if (_FileSystem.FileExists(assemblyPath))
            {
                Version? assemblyVersion = AssemblyName.GetAssemblyName(assemblyPath).Version;

                if (assemblyVersion != null)
                {
                    bundledToolVersion = assemblyVersion.ToString(3);
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Bundled tool version: {bundledToolVersion}.");
            }

            else
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Tool assembly not found at '{assemblyPath}'. Returning default version.");
            }

            return bundledToolVersion;
        }

        /// <summary>
        /// Reads the version from the deployed Server Backup Tool API assembly.
        /// </summary>
        public string GetBundledApiVersion(string apiInstallPath)
        {
            string bundledApiVersion = "0.0.0";

            string assemblyPath = Path.Combine(
                apiInstallPath,
                "ServerBackupTool.API.dll");

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Reading API version from '{assemblyPath}'.");

            if (_FileSystem.FileExists(assemblyPath))
            {
                Version? assemblyVersion = AssemblyName.GetAssemblyName(assemblyPath).Version;

                if (assemblyVersion != null)
                {
                    bundledApiVersion = assemblyVersion.ToString(3);
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Bundled API version: {bundledApiVersion}.");
            }

            else
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"API assembly not found at '{assemblyPath}'. Returning default version.");
            }

            return bundledApiVersion;
        }

        /// <summary>
        /// Parses the tool version from the embedded resource filename.
        /// </summary>
        public string GetEmbeddedToolVersion() => ParseVersionFromResourceName(InstallerValues.Resources.ToolPrefix);

        /// <summary>
        /// Parses the API version from the embedded resource filename.
        /// </summary>
        public string GetEmbeddedApiVersion() => ParseVersionFromResourceName(InstallerValues.Resources.ApiPrefix);

        /// <summary>
        /// Finds an embedded resource matching the prefix and extracts the version from its filename.
        /// </summary>
        private string ParseVersionFromResourceName(string prefix)
        {
            string version = "0.0.0";

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Parsing version from embedded resource with prefix '{prefix}'.");

            string? resourceName = _ResourceService.FindResource(prefix);

            if (resourceName != null)
            {
                int prefixIndex = resourceName.IndexOf(
                    prefix,
                    StringComparison.OrdinalIgnoreCase);

                if (prefixIndex >= 0)
                {
                    int versionStart = prefixIndex + prefix.Length;
                    int extensionIndex = resourceName.IndexOf(
                        InstallerValues.Resources.ZipExtension,
                        versionStart,
                        StringComparison.OrdinalIgnoreCase);

                    if (extensionIndex > versionStart)
                    {
                        string parsedVersion = resourceName[versionStart..extensionIndex];

                        if (Version.TryParse(
                            parsedVersion,
                            out _))
                        {
                            version = parsedVersion;
                        }
                    }
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Embedded version for prefix '{prefix}': {version}.");
            }

            else
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Resource with prefix '{prefix}' not found. Returning default version.");
            }

            return version;
        }
    }
}
