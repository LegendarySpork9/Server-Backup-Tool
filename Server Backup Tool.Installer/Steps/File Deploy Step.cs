// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using Spectre.Console;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Steps
{
    public class FileDeployStep
    {
        private readonly IAnsiConsole _Console;
        private readonly ILoggerService _Logger;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly IResourceService _ResourceService;
        private readonly IConfigWriter _ConfigWriter;
        private readonly IDatabaseInitialiser _DatabaseInitialiser;
        private readonly ITaskSchedulerService _TaskSchedulerService;
        private readonly IRegistryService _RegistryService;
        private readonly IVersionService _VersionService;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public FileDeployStep(
            IAnsiConsole console,
            ILoggerService logger,
            IExtendedFileSystem fileSystem,
            IResourceService resourceService,
            IConfigWriter configWriter,
            IDatabaseInitialiser databaseInitialiser,
            ITaskSchedulerService taskSchedulerService,
            IRegistryService registryService,
            IVersionService versionService,
            InstallOptionsModel options)
        {
            _Console = console;
            _Logger = logger;
            _FileSystem = fileSystem;
            _ResourceService = resourceService;
            _ConfigWriter = configWriter;
            _DatabaseInitialiser = databaseInitialiser;
            _TaskSchedulerService = taskSchedulerService;
            _RegistryService = registryService;
            _VersionService = versionService;
            _Options = options;
        }

        /// <summary>
        /// Deploys all installation files with a progress display.
        /// </summary>
        public async Task Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Starting file deployment.");

            await _Console.Progress()
                .AutoClear(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async context =>
                {
                    ProgressTask directoryTask = context.AddTask("Creating directories");

                    CreateDirectories(directoryTask);

                    string? toolResourceName = _ResourceService.FindResource(InstallerValues.Resources.ToolPrefix);

                    ProgressTask toolBinariesTask = context.AddTask("Installing Server Backup Tool");

                    ExtractBinaries(
                        toolBinariesTask,
                        toolResourceName ?? string.Empty,
                        _Options.InstallPath);

                    if (_Options.ApiConfig != null)
                    {
                        string? apiResourceName = _ResourceService.FindResource(InstallerValues.Resources.ApiPrefix);

                        ProgressTask apiBinariesTask = context.AddTask("Installing Server Backup Tool API");

                        ExtractBinaries(
                            apiBinariesTask,
                            apiResourceName ?? string.Empty,
                            _Options.ApiInstallPath);
                    }

                    ProgressTask configTask = context.AddTask("Generating configuration");

                    await GenerateConfigurationAsync(configTask);

                    if (_Options.ApiConfig != null)
                    {
                        ProgressTask apiConfigTask = context.AddTask("Generating API configuration");

                        await GenerateApiConfigurationAsync(apiConfigTask);
                    }

                    ProgressTask databaseTask = context.AddTask("Creating database");

                    await CreateDatabaseAsync(databaseTask);

                    ProgressTask scheduledTaskTask = context.AddTask("Registering scheduled task");

                    RegisterScheduledTask(
                        scheduledTaskTask,
                        _Options.ToolTaskName,
                        Path.Combine(
                            _Options.InstallPath,
                            "Server Backup Tool.exe"));

                    if (_Options.ApiConfig != null)
                    {
                        ProgressTask apiTaskTask = context.AddTask("Registering API scheduled task");

                        RegisterScheduledTask(
                            apiTaskTask,
                            _Options.ApiTaskName,
                            Path.Combine(
                                _Options.ApiInstallPath,
                                "Server Backup Tool.API.exe"));
                    }

                    ProgressTask registryTask = context.AddTask("Writing registry entry");

                    WriteRegistryEntry(registryTask);
                });

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "File deployment completed.");
        }

        /// <summary>
        /// Creates the installation directories.
        /// </summary>
        private void CreateDirectories(ProgressTask task)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Creating directories at {_Options.InstallPath}.");

            List<string> directories =
            [
                _Options.InstallPath,
                Path.Combine(
                    _Options.InstallPath,
                    "Logs"),
                Path.Combine(
                    _Options.InstallPath,
                    InstallerValues.Defaults.ArchiveDirectory),
                InstallerValues.Defaults.ProgramDataPath
            ];

            if (_Options.ApiConfig != null)
            {
                directories.Add(_Options.ApiInstallPath);
            }

            foreach (string directory in directories)
            {
                _FileSystem.CreateDirectory(directory);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Directories created.");

            task.Value = 100;
        }

        /// <summary>
        /// Extracts embedded binary resources to the install directory.
        /// </summary>
        private void ExtractBinaries(
            ProgressTask task,
            string resourceName,
            string destinationPath)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Extracting '{resourceName}' to {destinationPath}.");

            if (_ResourceService.ResourceExists(resourceName))
            {
                (bool extracted, Exception? exception) = _ResourceService.ExtractResource(
                    resourceName,
                    destinationPath);

                if (!extracted)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        $"Failed to extract '{resourceName}': {exception?.Message}");

                    throw new InvalidOperationException(
                        $"Failed to install binaries from '{resourceName}'.",
                        exception);
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"'{resourceName}' extracted.");
            }

            else
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Resource '{resourceName}' not found. Skipping binary extraction.");
            }

            task.Value = 100;
        }

        /// <summary>
        /// Generates and writes the App.config file.
        /// </summary>
        private async Task GenerateConfigurationAsync(ProgressTask task)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Generating App.config.");

            XDocument config = _ConfigWriter.GenerateAppConfig(_Options);
            string configPath = Path.Combine(
                _Options.InstallPath,
                InstallerValues.Defaults.ToolConfigFileName);

            (bool written, Exception? exception) = await _ConfigWriter.WriteConfig(
                configPath,
                config);

            if (!written)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to write App.config: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to generate configuration.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "App.config written.");

            task.Value = 100;
        }

        /// <summary>
        /// Generates and writes the API appsettings.json file.
        /// </summary>
        private async Task GenerateApiConfigurationAsync(ProgressTask task)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Generating appsettings.json.");

            string json = _ConfigWriter.GenerateApiAppSettings(
                _Options.ApiConfig!,
                _Options.ServerConfig);
            string apiSettingsPath = Path.Combine(
                _Options.ApiInstallPath,
                "appsettings.json");

            (bool written, Exception? exception) = await _ConfigWriter.WriteApiSettings(
                apiSettingsPath,
                json);

            if (!written)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to write appsettings.json: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to generate API configuration.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "appsettings.json written.");

            task.Value = 100;
        }

        /// <summary>
        /// Creates and initialises the SQLite database.
        /// </summary>
        private async Task CreateDatabaseAsync(ProgressTask task)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Creating database.");

            (bool initialised, Exception? exception) = await _DatabaseInitialiser.InitialiseDatabase(_Options.ServerConfig.DatabasePath);

            if (!initialised)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to create database: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to create database.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Database created.");

            task.Value = 100;
        }

        /// <summary>
        /// Registers a Windows scheduled task with the given name and executable.
        /// </summary>
        private void RegisterScheduledTask(
            ProgressTask task,
            string taskName,
            string executablePath)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Registering scheduled task '{taskName}'.");

            (bool created, Exception? exception) = _TaskSchedulerService.CreateScheduledTask(
                taskName,
                executablePath);

            if (!created)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to register scheduled task: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to register scheduled task.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Scheduled task registered.");

            task.Value = 100;
        }

        /// <summary>
        /// Writes the uninstall registry entry.
        /// </summary>
        private void WriteRegistryEntry(ProgressTask task)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Writing registry entry.");

            string toolVersion = _VersionService.GetBundledToolVersion(_Options.InstallPath);
            string apiVersion = _Options.ApiConfig != null ? _VersionService.GetBundledApiVersion(_Options.ApiInstallPath) : string.Empty;

            (bool written, Exception? exception) = _RegistryService.WriteUninstallEntry(
                _Options.ServerConfig.ServerName,
                _Options.InstallPath,
                _Options.ApiInstallPath,
                toolVersion,
                apiVersion,
                _Options.ToolTaskName,
                _Options.ApiTaskName);

            if (!written)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to write registry entry: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to write registry entry.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Registry entry written.");

            task.Value = 100;
        }
    }
}
