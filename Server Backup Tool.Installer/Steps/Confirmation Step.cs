// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using Spectre.Console;

namespace ServerBackupTool.Installer.Steps
{
    public class ConfirmationStep
    {
        private readonly IAnsiConsole _Console;
        private readonly ILoggerService _Logger;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public ConfirmationStep(
            IAnsiConsole console,
            ILoggerService logger,
            InstallOptionsModel options)
        {
            _Console = console;
            _Logger = logger;
            _Options = options;
        }

        /// <summary>
        /// Renders a summary table of the selected installation options and prompts the user to confirm.
        /// </summary>
        public void Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Displaying installation summary for confirmation.");

            _Console.Clear();

            Table table = new();
            table.Border(TableBorder.Rounded);
            table.Title("[bold]Installation Summary[/]");
            table.AddColumn("[bold]Setting[/]");
            table.AddColumn("[bold]Value[/]");

            table.AddRow(
                "Install Path",
                Markup.Escape(_Options.InstallPath));
            table.AddRow(
                "Components",
                Markup.Escape(string.Join(
                    ", ",
                    _Options.Components)));
            table.AddRow(
                "Server Name",
                Markup.Escape(_Options.ServerConfig.ServerName));
            table.AddRow(
                "Game",
                Markup.Escape(_Options.ServerConfig.Game));
            table.AddRow(
                "Server Directory",
                Markup.Escape(_Options.ServerConfig.ServerDirectory));
            table.AddRow(
                "Start File",
                Markup.Escape(_Options.ServerConfig.StartFile));
            table.AddRow(
                "IP Address",
                Markup.Escape(_Options.ServerConfig.IPAddress));
            table.AddRow(
                "Database Path",
                Markup.Escape(_Options.ServerConfig.DatabasePath));
            table.AddRow(
                "Backup Time",
                Markup.Escape(_Options.TimerConfig.BackupTime));
            table.AddRow(
                "Timers Configured",
                Markup.Escape(_Options.TimerConfig.CustomTimers.Count.ToString()));
            table.AddRow(
                "Email Enabled",
                _Options.EmailConfig != null && _Options.EmailConfig.Enabled ? "Yes" : "No");

            if (_Options.EmailConfig != null && _Options.EmailConfig.Enabled)
            {
                table.AddRow(
                    "Emails Configured",
                    Markup.Escape(_Options.EmailConfig.Emails.Count.ToString()));
            }

            table.AddRow(
                "Tool Task Name",
                Markup.Escape(_Options.ToolTaskName));

            if (_Options.ApiConfig != null)
            {
                table.AddRow(
                    "API Enabled",
                    "Yes");
                table.AddRow(
                    "API HTTP",
                    $"http://{Markup.Escape(_Options.ApiConfig.BindAddress)}:{_Options.ApiConfig.HttpPort}");

                if (_Options.ApiConfig.EnableHttps)
                {
                    table.AddRow(
                        "API HTTPS",
                        $"https://{Markup.Escape(_Options.ApiConfig.BindAddress)}:{_Options.ApiConfig.HttpsPort}");
                    table.AddRow(
                        "SSL Certificate",
                        Markup.Escape(_Options.ApiConfig.CertificatePath));
                }

                table.AddRow(
                    "API Install Path",
                    Markup.Escape(_Options.ApiInstallPath));
                table.AddRow(
                    "API Task Name",
                    Markup.Escape(_Options.ApiTaskName));
            }

            else
            {
                table.AddRow(
                    "API Enabled",
                    "No");
            }

            _Console.Write(table);
            _Console.WriteLine();

            bool confirmed = _Console.Prompt(new ConfirmationPrompt("Proceed with installation?")
            {
                ShowDefaultValue = false
            });

            if (!confirmed)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Installation cancelled by user.");

                throw new OperationCanceledException("Installation cancelled by user.");
            }

            _Console.Clear();

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Installation confirmed by user.");
        }
    }
}
