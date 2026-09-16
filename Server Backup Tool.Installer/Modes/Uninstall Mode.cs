// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using Spectre.Console;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Modes
{
    public class UninstallMode
    {
        private readonly ILoggerService _Logger;
        private readonly IFileService _FileService;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly ITaskSchedulerService _TaskSchedulerService;
        private readonly IRegistryService _RegistryService;
        private readonly IVersionService _VersionService;

        // Sets the class's global variables.
        public UninstallMode(
            ILoggerService logger,
            IFileService fileService,
            IExtendedFileSystem fileSystem,
            ITaskSchedulerService taskSchedulerService,
            IRegistryService registryService,
            IVersionService versionService)
        {
            _Logger = logger;
            _FileService = fileService;
            _FileSystem = fileSystem;
            _TaskSchedulerService = taskSchedulerService;
            _RegistryService = registryService;
            _VersionService = versionService;
        }

        /// <summary>
        /// Runs the uninstall process.
        /// </summary>
        public void Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Starting uninstall mode.");

            VersionInfoModel? installed = SelectInstallation();

            if (installed == null)
            {
                AnsiConsole.MarkupLine("[red]No existing installation found.[/]");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    "No existing installation found.");
            }

            else
            {
                AnsiConsole.MarkupLine($"Installation found at: [blue]{Markup.Escape(installed.InstallPath)}[/]");
                AnsiConsole.MarkupLine($"Tool version: [blue]{Markup.Escape(installed.ToolVersion)}[/]");

                if (!string.IsNullOrEmpty(installed.ApiInstallPath))
                {
                    AnsiConsole.MarkupLine($"API location:  [blue]{Markup.Escape(installed.ApiInstallPath)}[/]");
                    AnsiConsole.MarkupLine($"API version:   [blue]{Markup.Escape(!string.IsNullOrEmpty(installed.ApiVersion) ? installed.ApiVersion : "Unknown")}[/]");
                }

                AnsiConsole.WriteLine();

                if (!AnsiConsole.Prompt(new ConfirmationPrompt("[red]Are you sure you want to uninstall the Server Backup Tool?[/]")
                {
                    DefaultValue = false,
                    ShowDefaultValue = false
                }))
                {
                    AnsiConsole.MarkupLine("[yellow]Uninstall cancelled.[/]");

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Uninstall cancelled by user.");
                }

                else
                {
                    bool apiInstalled = !string.IsNullOrEmpty(installed.ApiInstallPath) && _FileSystem.DirectoryExists(installed.ApiInstallPath);

                    string uninstallTarget = "Everything";

                    if (apiInstalled)
                    {
                        uninstallTarget = AnsiConsole.Prompt(new SelectionPrompt<string>()
                            .Title("What would you like to uninstall?")
                            .AddChoices(
                                "Everything (Tool and API)",
                                "API only"));
                    }

                    if (uninstallTarget.StartsWith("API only"))
                    {
                        UninstallApiOnly(installed);
                    }

                    else
                    {
                        UninstallEverything(installed);
                    }

                    AnsiConsole.MarkupLine("[green]Uninstall completed.[/]");

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Uninstall mode completed.");
                }
            }
        }

        /// <summary>
        /// Uninstalls only the API component, preserving the tool.
        /// </summary>
        private void UninstallApiOnly(VersionInfoModel installed)
        {
            if (!string.IsNullOrEmpty(installed.ApiTaskName) && _TaskSchedulerService.TaskExists(installed.ApiTaskName))
            {
                AnsiConsole.MarkupLine($"Removing API scheduled task '{Markup.Escape(installed.ApiTaskName)}'...");

                (bool apiTaskRemoved, Exception? apiTaskEx) = _TaskSchedulerService.RemoveScheduledTask(installed.ApiTaskName);

                if (!apiTaskRemoved)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to remove API scheduled task: {Markup.Escape(apiTaskEx?.Message ?? "Unknown error")}[/]");
                }
            }

            if (!string.IsNullOrEmpty(installed.ApiInstallPath) && _FileSystem.DirectoryExists(installed.ApiInstallPath))
            {
                AnsiConsole.MarkupLine("Removing API files...");

                (bool apiDirDeleted, Exception? apiDirEx) = _FileService.DeleteDirectory(installed.ApiInstallPath);

                if (!apiDirDeleted)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to remove API directory: {Markup.Escape(apiDirEx?.Message ?? "Unknown error")}[/]");
                }
            }

            AnsiConsole.MarkupLine("Updating registry entry...");

            _RegistryService.WriteUninstallEntry(
                installed.ServerName,
                installed.InstallPath,
                string.Empty,
                installed.ToolVersion,
                string.Empty,
                installed.ToolTaskName,
                string.Empty);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "API uninstalled. Tool preserved.");
        }

        /// <summary>
        /// Uninstalls everything including the tool and API.
        /// </summary>
        private void UninstallEverything(VersionInfoModel installed)
        {
            if (!string.IsNullOrEmpty(installed.ToolTaskName) && _TaskSchedulerService.TaskExists(installed.ToolTaskName))
            {
                AnsiConsole.MarkupLine($"Removing scheduled task '{Markup.Escape(installed.ToolTaskName)}'...");

                (bool taskRemoved, Exception? taskEx) = _TaskSchedulerService.RemoveScheduledTask(installed.ToolTaskName);

                if (!taskRemoved)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to remove scheduled task: {Markup.Escape(taskEx?.Message ?? "Unknown error")}[/]");
                }
            }

            if (!string.IsNullOrEmpty(installed.ApiTaskName) && _TaskSchedulerService.TaskExists(installed.ApiTaskName))
            {
                AnsiConsole.MarkupLine($"Removing API scheduled task '{Markup.Escape(installed.ApiTaskName)}'...");

                (bool apiTaskRemoved, Exception? apiTaskEx) = _TaskSchedulerService.RemoveScheduledTask(installed.ApiTaskName);

                if (!apiTaskRemoved)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to remove API scheduled task: {Markup.Escape(apiTaskEx?.Message ?? "Unknown error")}[/]");
                }
            }

            bool keepDatabase = AnsiConsole.Prompt(new ConfirmationPrompt("Keep the database file?")
            {
                DefaultValue = true,
                ShowDefaultValue = false
            });
            bool keepLogs = AnsiConsole.Prompt(new ConfirmationPrompt("Keep log files?")
            {
                DefaultValue = true,
                ShowDefaultValue = false
            });

            AnsiConsole.MarkupLine("Removing registry entry...");

            _RegistryService.RemoveUninstallEntry(installed.ServerName);

            if (!keepDatabase)
            {
                string dbPath = GetDatabasePath(installed.InstallPath);

                if (_FileSystem.FileExists(dbPath))
                {
                    try
                    {
                        _FileSystem.DeleteFile(dbPath);

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Database file deleted: {dbPath}");
                    }

                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Failed to delete database: {Markup.Escape(ex.Message)}[/]");
                    }
                }

                string dbDirectory = Path.GetDirectoryName(dbPath) ?? string.Empty;

                if (!string.IsNullOrEmpty(dbDirectory) && _FileSystem.DirectoryExists(dbDirectory) && !dbDirectory.Equals(
                    installed.InstallPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _FileService.DeleteDirectory(dbDirectory);
                }
            }

            if (!keepLogs)
            {
                string logsPath = Path.Combine(
                    installed.InstallPath,
                    "Logs");
                string archivePath = Path.Combine(
                    installed.InstallPath,
                    InstallerValues.Defaults.ArchiveDirectory);

                if (_FileSystem.DirectoryExists(logsPath))
                {
                    _FileService.DeleteDirectory(logsPath);
                }

                if (_FileSystem.DirectoryExists(archivePath))
                {
                    _FileService.DeleteDirectory(archivePath);
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Log files deleted.");

                AnsiConsole.MarkupLine("Removing application files...");

                (bool installDirDeleted, Exception? installDirEx) = _FileService.DeleteDirectory(installed.InstallPath);

                if (!installDirDeleted)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to remove install directory: {Markup.Escape(installDirEx?.Message ?? "Unknown error")}[/]");
                }
            }

            if (!string.IsNullOrEmpty(installed.ApiInstallPath) && _FileSystem.DirectoryExists(installed.ApiInstallPath))
            {
                AnsiConsole.MarkupLine("Removing API files...");

                (bool apiDirDeleted, Exception? apiDirEx) = _FileService.DeleteDirectory(installed.ApiInstallPath);

                if (!apiDirDeleted)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to remove API directory: {Markup.Escape(apiDirEx?.Message ?? "Unknown error")}[/]");
                }
            }
        }

        /// <summary>
        /// Reads the database path from the installed App.config, falling back to the default location.
        /// </summary>
        private string GetDatabasePath(string installPath)
        {
            string configPath = Path.Combine(
                installPath,
                InstallerValues.Defaults.ToolConfigFileName);
            string defaultPath = Path.Combine(
                InstallerValues.Defaults.ProgramDataPath,
                InstallerValues.Defaults.DatabaseFileName);

            string dbPath = defaultPath;

            if (_FileSystem.FileExists(configPath))
            {
                try
                {
                    XDocument config = XDocument.Load(configPath);
                    string? configuredPath = config.Root?.Element("serverBackup")?
                        .Element("databaseDetails")?
                        .Attribute("path")?.Value;

                    if (!string.IsNullOrEmpty(configuredPath))
                    {
                        dbPath = configuredPath;
                    }
                }

                catch (Exception ex)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Failed to read database path from config: {ex.Message}. Using default.");
                }
            }

            return dbPath;
        }

        /// <summary>
        /// Enumerates all installations and prompts the user to select one if multiple exist.
        /// </summary>
        private VersionInfoModel? SelectInstallation()
        {
            List<VersionInfoModel> installations = _VersionService.GetAllInstallations();
            VersionInfoModel? selected = null;

            if (installations.Count == 1)
            {
                selected = installations[0];

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Auto-selected single installation: {selected.ServerName}.");
            }

            else if (installations.Count > 1)
            {
                string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Multiple installations found. Which installation would you like to uninstall?")
                    .AddChoices(installations.Select(i => $"{i.ServerName} (v{i.ToolVersion} at {i.InstallPath})")));

                int selectedIndex = installations.FindIndex(i => choice.StartsWith(
                    $"{i.ServerName} (v{i.ToolVersion}",
                    StringComparison.Ordinal));

                if (selectedIndex >= 0)
                {
                    selected = installations[selectedIndex];
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"User selected installation: {selected?.ServerName}.");
            }

            return selected;
        }
    }
}
