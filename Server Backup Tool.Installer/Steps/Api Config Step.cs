// Copyright © - Unpublished - Toby Hunter
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        private readonly IAnsiConsole _Console;
        private readonly ILoggerService _Logger;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public ApiConfigStep(
            IAnsiConsole console,
            ILoggerService logger,
            InstallOptionsModel options)
        {
            _Console = console;
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
                _Console.MarkupLine("[grey]Use 0.0.0.0 to listen on all network interfaces, or a specific IP to restrict access.[/]");
                string bindAddress = _Console.Prompt(new TextPrompt<string>("Enter the IP address the API should listen on:").DefaultValue(InstallerValues.Defaults.ApiBindAddress));
                int httpPort = _Console.Prompt(new TextPrompt<int>("Enter the HTTP port:").DefaultValue(InstallerValues.Defaults.ApiHttpPort));
                bool enableHttps = _Console.Prompt(new ConfirmationPrompt("Enable HTTPS?")
                {
                    ShowDefaultValue = false
                });
                int httpsPort = 0;
                string certificateFormat = string.Empty;
                string certificatePath = string.Empty;
                string certificateKeyPath = string.Empty;
                string certificatePassword = string.Empty;

                if (enableHttps)
                {
                    httpsPort = _Console.Prompt(new TextPrompt<int>("Enter the HTTPS port:").DefaultValue(InstallerValues.Defaults.ApiHttpsPort));

                    certificateFormat = _Console.Prompt(new SelectionPrompt<string>()
                        .Title("Select the certificate format:")
                        .AddChoices("PFX", "PEM"));

                    if (certificateFormat == "PFX")
                    {
                        certificatePath = _Console.Prompt(new TextPrompt<string>("Enter the path to the SSL certificate (.pfx):").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Certificate path is required.")));
                        certificatePassword = _Console.Prompt(new TextPrompt<string>("Enter the certificate password:").Secret());

                        try
                        {
                            X509Certificate2 cert = X509CertificateLoader.LoadPkcs12FromFile(
                                certificatePath,
                                certificatePassword);

                            if (!cert.HasPrivateKey)
                            {
                                _Console.MarkupLine("[red]The certificate does not contain a private key. HTTPS requires a certificate with a private key.[/]");

                                _Logger.LogMessage(
                                    StandardValues.LoggerValues.Error,
                                    "PFX certificate missing private key.");
                            }

                            cert.Dispose();
                        }

                        catch (Exception ex)
                        {
                            _Console.MarkupLine($"[red]Failed to load certificate: {Markup.Escape(ex.Message)}[/]");

                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Error,
                                $"Failed to load PFX certificate: {ex.Message}");
                        }
                    }

                    else
                    {
                        certificatePath = _Console.Prompt(new TextPrompt<string>("Enter the path to the certificate file (.pem):").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Certificate path is required.")));
                        certificateKeyPath = _Console.Prompt(new TextPrompt<string>("Enter the path to the private key file (.pem):").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Key path is required.")));

                        try
                        {
                            X509Certificate2 cert = X509Certificate2.CreateFromPemFile(
                                certificatePath,
                                certificateKeyPath);

                            if (!cert.HasPrivateKey)
                            {
                                _Console.MarkupLine("[red]The certificate does not contain a private key. HTTPS requires a certificate with a private key.[/]");

                                _Logger.LogMessage(
                                    StandardValues.LoggerValues.Error,
                                    "PEM certificate missing private key.");
                            }

                            cert.Dispose();
                        }

                        catch (Exception ex)
                        {
                            _Console.MarkupLine($"[red]Failed to load certificate: {Markup.Escape(ex.Message)}[/]");

                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Error,
                                $"Failed to load PEM certificate: {ex.Message}");
                        }
                    }
                }

                string databasePath = _Console.Prompt(new TextPrompt<string>("Enter the API database path:").DefaultValue(_Options.ServerConfig.DatabasePath));
                string archiveDirectory = _Console.Prompt(new TextPrompt<string>("Enter the archive directory:").DefaultValue(Path.Combine(
                    _Options.InstallPath,
                    InstallerValues.Defaults.ArchiveDirectory)));
                string webhookSecretPlain = _Console.Prompt(new TextPrompt<string>("Enter the webhook signing secret (shared with webhook consumers):").Secret());
                
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

                _Console.WriteLine();
                _Console.Write(credentialsTable);
                _Console.WriteLine();
                _Console.MarkupLine("[yellow]WARNING: These credentials will NOT be shown again. Copy them now.[/]");
                _Console.MarkupLine("Press [green]Enter[/] to continue...");
                _Console.Prompt(new TextPrompt<string>("").AllowEmpty());

                _Options.ApiConfig = new ApiConfigModel
                {
                    BindAddress = bindAddress,
                    HttpPort = httpPort,
                    HttpsPort = httpsPort,
                    EnableHttps = enableHttps,
                    CertificateFormat = certificateFormat,
                    CertificatePath = certificatePath,
                    CertificateKeyPath = certificateKeyPath,
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
