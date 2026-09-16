// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using Spectre.Console;

namespace ServerBackupTool.Installer.Steps
{
    public class ComponentSelectionStep
    {
        private readonly ILoggerService _Logger;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public ComponentSelectionStep(
            ILoggerService logger,
            InstallOptionsModel options)
        {
            _Logger = logger;
            _Options = options;
        }

        /// <summary>
        /// Prompts the user to select which components to install.
        /// </summary>
        public void Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Entering component selection step.");

            List<string> selected = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
                .Title("Select components to install:")
                .Required()
                .AddChoices(
                    "Server Backup Tool",
                    "Server Backup Tool API")
                .Select("Server Backup Tool"));

            _Options.Components = selected;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Component selection complete. Selected: {string.Join(
                    ", ",
                    selected)}.");
        }
    }
}
