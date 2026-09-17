// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using Spectre.Console;

namespace ServerBackupTool.Installer.Steps
{
    public class LocationStep
    {
        private readonly IAnsiConsole _Console;
        private readonly ILoggerService _Logger;
        private readonly IFileService _FileService;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public LocationStep(
            IAnsiConsole console,
            ILoggerService logger,
            IFileService fileService,
            InstallOptionsModel options)
        {
            _Console = console;
            _Logger = logger;
            _FileService = fileService;
            _Options = options;
        }

        /// <summary>
        /// Prompts the user for the installation path and validates write permissions.
        /// </summary>
        public async Task Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Entering location step.");

            while (true)
            {
                string path = _Console.Prompt(new TextPrompt<string>("Enter the install path:").DefaultValue(InstallerValues.Defaults.InstallPath));

                if (await _FileService.ValidateWritePermissions(path))
                {
                    _Options.InstallPath = path;
                    _Options.ApiInstallPath = path + ".API";

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Location step complete. Install path: {path}, API path: {path}.API.");

                    break;
                }

                _Console.MarkupLine("[red]The specified path is not writable. Please choose a different location.[/]");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Write permission validation failed for path: {path}.");
            }
        }
    }
}
