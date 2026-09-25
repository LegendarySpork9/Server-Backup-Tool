// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using Spectre.Console;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Steps
{
    public class ValidationStep
    {
        private readonly IAnsiConsole _Console;
        private readonly ILoggerService _Logger;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly IDatabaseInitialiser _DatabaseInitialiser;
        private readonly ITaskSchedulerService _TaskSchedulerService;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public ValidationStep(
            IAnsiConsole console,
            ILoggerService logger,
            IExtendedFileSystem fileSystem,
            IDatabaseInitialiser databaseInitialiser,
            ITaskSchedulerService taskSchedulerService,
            InstallOptionsModel options)
        {
            _Console = console;
            _Logger = logger;
            _FileSystem = fileSystem;
            _DatabaseInitialiser = databaseInitialiser;
            _TaskSchedulerService = taskSchedulerService;
            _Options = options;
        }

        /// <summary>
        /// Runs post-installation validation checks and displays the results.
        /// </summary>
        public async Task Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Running post-installation validation.");

            Table table = new();
            table.Border(TableBorder.Rounded);
            table.Title("[bold]Post-Installation Validation[/]");
            table.AddColumn("[bold]Check[/]");
            table.AddColumn("[bold]Result[/]");

            string configPath = Path.Combine(
                _Options.InstallPath,
                InstallerValues.Defaults.ToolConfigFileName);

            AddCheckResult(
                table,
                "Config file exists",
                CheckConfigFileExists(configPath));
            AddCheckResult(
                table,
                "Config file is valid XML",
                CheckConfigFileValid(configPath));
            AddCheckResult(
                table,
                "Database exists and is valid",
                await CheckDatabaseAsync());
            AddCheckResult(
                table,
                "Tool scheduled task exists",
                CheckScheduledTask(_Options.ToolTaskName));
            AddCheckResult(
                table,
                "ProgramData directory exists",
                CheckProgramDataDirectory());

            if (_Options.ApiConfig != null)
            {
                AddCheckResult(
                    table,
                    "API scheduled task exists",
                    CheckScheduledTask(_Options.ApiTaskName));
            }

            _Console.Write(table);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Post-installation validation completed.");
        }

        /// <summary>
        /// Adds a check result row to the validation table.
        /// </summary>
        private void AddCheckResult(
            Table table,
            string checkName,
            bool passed)
        {
            string result = passed ? "[green]PASS[/]" : "[red]FAIL[/]";

            table.AddRow(
                Markup.Escape(checkName),
                result);

            _Logger.LogMessage(
                passed ? StandardValues.LoggerValues.Info : StandardValues.LoggerValues.Warning,
                $"Validation check '{checkName}': {(passed ? "PASS" : "FAIL")}");
        }

        /// <summary>
        /// Checks whether the configuration file exists at the expected path.
        /// </summary>
        private bool CheckConfigFileExists(string configPath) => _FileSystem.FileExists(configPath);

        /// <summary>
        /// Checks whether the configuration file is valid XML.
        /// </summary>
        private bool CheckConfigFileValid(string configPath)
        {
            bool valid = false;

            try
            {
                if (_FileSystem.FileExists(configPath))
                {
                    XDocument.Load(configPath);
                    valid = true;
                }
            }

            catch
            {

            }

            return valid;
        }

        /// <summary>
        /// Checks whether the database exists and passes validation.
        /// </summary>
        private async Task<bool> CheckDatabaseAsync()
        {
            bool valid = false;

            try
            {
                if (_FileSystem.FileExists(_Options.ServerConfig.DatabasePath))
                {
                    (bool success, Exception? _) = await _DatabaseInitialiser.ValidateDatabase(_Options.ServerConfig.DatabasePath);

                    valid = success;
                }
            }

            catch
            {
                
            }

            return valid;
        }

        /// <summary>
        /// Checks whether the specified scheduled task exists in Task Scheduler.
        /// </summary>
        private bool CheckScheduledTask(string taskName)
        {
            bool exists = false;

            try
            {
                exists = _TaskSchedulerService.TaskExists(taskName);
            }

            catch
            {

            }

            return exists;
        }

        /// <summary>
        /// Checks whether the ProgramData directory exists.
        /// </summary>
        private bool CheckProgramDataDirectory() => _FileSystem.DirectoryExists(InstallerValues.Defaults.ProgramDataPath);
    }
}
