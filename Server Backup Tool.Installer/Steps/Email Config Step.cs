// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Values;
using Spectre.Console;

namespace ServerBackupTool.Installer.Steps
{
    public class EmailConfigStep
    {
        private readonly ILoggerService _Logger;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public EmailConfigStep(
            ILoggerService logger,
            InstallOptionsModel options)
        {
            _Logger = logger;
            _Options = options;
        }

        /// <summary>
        /// Prompts the user for email notification configuration settings.
        /// </summary>
        public void Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Entering email config step.");

            bool configureEmail = AnsiConsole.Prompt(new ConfirmationPrompt("Configure email notifications?")
            {
                ShowDefaultValue = false
            });

            if (!configureEmail)
            {
                _Options.EmailConfig = null;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Email config step skipped.");
            }

            else
            {
                string smtpHost = AnsiConsole.Prompt(new TextPrompt<string>("Enter the SMTP host:"));
                string smtpPassword = AnsiConsole.Prompt(new TextPrompt<string>("Enter the SMTP password:").Secret());
                int port = AnsiConsole.Prompt(new TextPrompt<int>("Enter the SMTP port:").DefaultValue(InstallerValues.Defaults.SmtpPort));
                bool enableSsl = AnsiConsole.Prompt(new ConfirmationPrompt("Enable SSL?")
                {
                    DefaultValue = InstallerValues.Defaults.EnableSSL,
                    ShowDefaultValue = false
                });
                string fromEmail = AnsiConsole.Prompt(new TextPrompt<string>("Enter the from email address:"));
                string fromName = AnsiConsole.Prompt(new TextPrompt<string>("Enter the from name:").DefaultValue(InstallerValues.Defaults.FromName));

                List<EmailTemplateModel> emails = [];

                while (AnsiConsole.Prompt(new ConfirmationPrompt("Add an email template?")
                {
                    ShowDefaultValue = false
                }))
                {
                    string triggerType = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title("Select the trigger type:")
                        .AddChoices(
                            "Open (system — sent on application startup)",
                            "Close (system — sent on application shutdown)",
                            "Heartbeat (system — sent when server heartbeat fails)",
                            "Custom (triggered by server output text)"));

                    AnsiConsole.MarkupLine($"Selected Trigger Type: [blue]{Markup.Escape(triggerType)}[/]");

                    string trigger;
                    bool isSystem;

                    if (triggerType.StartsWith("Custom"))
                    {
                        trigger = AnsiConsole.Prompt(new TextPrompt<string>("Enter the server output text to match:"));
                        isSystem = false;
                    }

                    else
                    {
                        trigger = triggerType.Split(' ')[0];
                        isSystem = true;
                    }

                    string subject = AnsiConsole.Prompt(new TextPrompt<string>("Enter the subject:").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Subject is required.")));
                    string content = AnsiConsole.Prompt(new TextPrompt<string>("Enter the content (HTML or path to .html file):").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Content is required.")));

                    List<RecipientModel> recipients = [];

                    AnsiConsole.MarkupLine("[grey]At least one recipient is required.[/]");

                    string email = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient email:"));
                    string name = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient name:"));

                    recipients.Add(new RecipientModel
                    {
                        Email = email,
                        Name = name
                    });

                    while (AnsiConsole.Prompt(new ConfirmationPrompt("Add another recipient?")
                    {
                        ShowDefaultValue = false
                    }))
                    {
                        email = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient email:"));
                        name = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient name:"));

                        recipients.Add(new RecipientModel
                        {
                            Email = email,
                            Name = name
                        });
                    }

                    List<ImageModel> images = [];

                    while (AnsiConsole.Prompt(new ConfirmationPrompt("Add an inline image?")
                    {
                        ShowDefaultValue = false
                    }))
                    {
                        string imageKey = AnsiConsole.Prompt(new TextPrompt<string>("Enter the content ID (referenced in HTML as cid:value):"));
                        string imagePath = AnsiConsole.Prompt(new TextPrompt<string>("Enter the image file path:"));

                        images.Add(new ImageModel
                        {
                            Key = imageKey,
                            Path = imagePath
                        });
                    }

                    emails.Add(new EmailTemplateModel
                    {
                        Trigger = trigger,
                        IsSystem = isSystem,
                        Subject = subject,
                        Content = content,
                        Recipients = recipients,
                        Images = images
                    });
                }

                _Options.EmailConfig = new EmailConfigModel
                {
                    Enabled = true,
                    SmtpHost = smtpHost,
                    SmtpPassword = smtpPassword,
                    Port = port,
                    EnableSSL = enableSsl,
                    FromEmail = fromEmail,
                    FromName = fromName,
                    Emails = emails
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Email config step complete. Template(s): {emails.Count}.");
            }
        }
    }
}
