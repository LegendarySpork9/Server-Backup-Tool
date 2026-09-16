// Copyright © - Unpublished - Toby Hunter
using System.Security.Cryptography;
using System.Text;
using ServerBackupTool.Common.Functions;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Values;
using Spectre.Console;

namespace ServerBackupTool.Installer.Steps
{
    public class ApiConfigStep
    {
        private readonly ILoggerService _Logger;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public ApiConfigStep(
            ILoggerService logger,
            InstallOptionsModel options)
        {
            _Logger = logger;
            _Options = options;
        }

        /// <summary>
        /// Prompts the user for API configuration settings and generates credentials.
        /// </summary>
        public void Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Entering API config step.");

            if (!_Options.Components.Contains("Server Backup Tool API"))
            {
                _Options.ApiConfig = null;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "API config step skipped. Component not selected.");
            }

            else
            {
                AnsiConsole.MarkupLine("[grey]Use 0.0.0.0 to listen on all network interfaces, or a specific IP to restrict access.[/]");
                string bindAddress = AnsiConsole.Prompt(new TextPrompt<string>("Enter the IP address the API should listen on:").DefaultValue(InstallerValues.Defaults.ApiBindAddress));
                int httpPort = AnsiConsole.Prompt(new TextPrompt<int>("Enter the HTTP port:").DefaultValue(InstallerValues.Defaults.ApiHttpPort));
                bool enableHttps = AnsiConsole.Prompt(new ConfirmationPrompt("Enable HTTPS?")
                {
                    ShowDefaultValue = false
                });
                int httpsPort = 0;
                string certificatePath = string.Empty;
                string certificatePassword = string.Empty;

                if (enableHttps)
                {
                    httpsPort = AnsiConsole.Prompt(new TextPrompt<int>("Enter the HTTPS port:").DefaultValue(InstallerValues.Defaults.ApiHttpsPort));
                    certificatePath = AnsiConsole.Prompt(new TextPrompt<string>("Enter the path to the SSL certificate (.pfx):").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Certificate path is required.")));
                    certificatePassword = AnsiConsole.Prompt(new TextPrompt<string>("Enter the certificate password:").Secret());
                }

                string databasePath = AnsiConsole.Prompt(new TextPrompt<string>("Enter the API database path:").DefaultValue(_Options.ServerConfig.DatabasePath));
                string archiveDirectory = AnsiConsole.Prompt(new TextPrompt<string>("Enter the archive directory:").DefaultValue(Path.Combine(
                    _Options.InstallPath,
                    InstallerValues.Defaults.ArchiveDirectory)));
                string webhookSecretPlain = AnsiConsole.Prompt(new TextPrompt<string>("Enter the webhook signing secret (shared with webhook consumers):").Secret());
                
                byte[] webhookHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(webhookSecretPlain));
                string webhookSecret = Convert.ToHexString(webhookHashBytes)
                    .ToLowerInvariant();

                string clientId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                string clientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                string clientIdHash = HashFunction.HashValue(clientId);
                string clientSecretHash = HashFunction.HashValue(clientSecret);

                Table credentialsTable = new();
                credentialsTable.Border(TableBorder.Rounded);
                credentialsTable.Title("[yellow bold]API Credentials — Copy These Now[/]");
                credentialsTable.AddColumn("[bold]Setting[/]");
                credentialsTable.AddColumn("[bold]Value[/]");
                credentialsTable.AddRow(
                    "Client ID",
                    clientId);
                credentialsTable.AddRow(
                    "Client Secret",
                    clientSecret);

                AnsiConsole.WriteLine();
                AnsiConsole.Write(credentialsTable);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]WARNING: These credentials will NOT be shown again. Copy them now.[/]");
                AnsiConsole.MarkupLine("Press [green]Enter[/] to continue...");
                Console.ReadLine();

                _Options.ApiConfig = new ApiConfigModel
                {
                    BindAddress = bindAddress,
                    HttpPort = httpPort,
                    HttpsPort = httpsPort,
                    EnableHttps = enableHttps,
                    CertificatePath = certificatePath,
                    CertificatePassword = certificatePassword,
                    DatabasePath = databasePath,
                    ArchiveDirectory = archiveDirectory,
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    ClientIdHash = clientIdHash,
                    ClientSecretHash = clientSecretHash,
                    WebhookSecret = webhookSecret
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "API config step complete. Credentials generated.");
            }
        }
    }
}
