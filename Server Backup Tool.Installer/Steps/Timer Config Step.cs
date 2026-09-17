// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using Spectre.Console;

namespace ServerBackupTool.Installer.Steps
{
    public class TimerConfigStep
    {
        private readonly IAnsiConsole _Console;
        private readonly ILoggerService _Logger;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public TimerConfigStep(
            IAnsiConsole console,
            ILoggerService logger,
            InstallOptionsModel options)
        {
            _Console = console;
            _Logger = logger;
            _Options = options;
        }

        /// <summary>
        /// Prompts the user for timer configuration settings.
        /// </summary>
        public void Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Entering timer config step.");

            string backupTime = _Console.Prompt(new TextPrompt<string>("Enter the backup time (HH:mm:ss):").Validate(input => TimeSpan.TryParse(
                input,
                out _) ? ValidationResult.Success() : ValidationResult.Error("Please enter a valid time in HH:mm:ss format.")));

            List<CustomTimerModel> customTimers = [];

            while (_Console.Prompt(new ConfirmationPrompt("Add a custom timer?")
            {
                ShowDefaultValue = false
            }))
            {
                string name = _Console.Prompt(new TextPrompt<string>("Enter the timer name:"));
                string time = _Console.Prompt(new TextPrompt<string>("Enter the timer time (HH:mm:ss):").Validate(input => TimeSpan.TryParse(
                    input,
                    out _) ? ValidationResult.Success() : ValidationResult.Error("Please enter a valid time in HH:mm:ss format.")));
                string message = _Console.Prompt(new TextPrompt<string>("Enter the timer message:"));

                customTimers.Add(new CustomTimerModel
                {
                    Name = name,
                    Time = time,
                    Message = message
                });
            }

            _Options.TimerConfig = new TimerConfigModel
            {
                BackupTime = backupTime,
                CustomTimers = customTimers
            };

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Timer config step complete. Backup time: {backupTime}, Custom timers: {customTimers.Count}.");
        }
    }
}
