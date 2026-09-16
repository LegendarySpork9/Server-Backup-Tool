// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Values;
using Spectre.Console;

namespace ServerBackupTool.Installer.Steps
{
    public class ServerConfigStep
    {
        private readonly ILoggerService _Logger;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public ServerConfigStep(
            ILoggerService logger,
            IExtendedFileSystem fileSystem,
            InstallOptionsModel options)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
            _Options = options;
        }

        /// <summary>
        /// Prompts the user for server configuration settings.
        /// </summary>
        public void Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Entering server config step.");

            string serverName = AnsiConsole.Prompt(new TextPrompt<string>("Enter the server name:").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Server name is required.")));
            string toolTaskName = AnsiConsole.Prompt(new TextPrompt<string>("Enter a name for the scheduled task for the backup tool:").DefaultValue($"Server Backup Tool - {serverName}"));

            _Options.ToolTaskName = toolTaskName;

            if (_Options.Components.Contains("Server Backup Tool API"))
            {
                string apiTaskName = AnsiConsole.Prompt(new TextPrompt<string>("Enter a name for the scheduled task for the API:").DefaultValue($"Server Backup Tool API - {serverName}"));

                _Options.ApiTaskName = apiTaskName;
            }

            string game = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select the game:")
                .AddChoices(InstallerValues.Games));

            AnsiConsole.MarkupLine($"Selected Game: [blue]{Markup.Escape(game)}[/]");

            string serverDirectory = AnsiConsole.Prompt(new TextPrompt<string>("Enter the server directory:").Validate(input => _FileSystem.DirectoryExists(input) ? ValidationResult.Success() : ValidationResult.Error("The specified directory does not exist.")));
            string startFile = AnsiConsole.Prompt(new TextPrompt<string>("Enter the start file (relative path):").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Start file is required.")));
            string ipAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter the IP address:").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("IP address is required.")));
            string defaultDbPath = Path.Combine(
                InstallerValues.Defaults.ProgramDataPath,
                InstallerValues.Defaults.DatabaseFileName);
            string databasePath = AnsiConsole.Prompt(new TextPrompt<string>("Enter the database path:").DefaultValue(defaultDbPath));
            int pollingIntervalMs = AnsiConsole.Prompt(new TextPrompt<int>("Enter the polling interval (ms):").DefaultValue(InstallerValues.Defaults.PollingIntervalMs));

            _Options.ServerConfig = new ServerConfigModel
            {
                ServerName = serverName,
                Game = game,
                ServerDirectory = serverDirectory,
                StartFile = startFile,
                IPAddress = ipAddress,
                DatabasePath = databasePath,
                PollingIntervalMs = pollingIntervalMs
            };

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Server config step complete. Server: {serverName}, Game: {game}.");
        }
    }
}
