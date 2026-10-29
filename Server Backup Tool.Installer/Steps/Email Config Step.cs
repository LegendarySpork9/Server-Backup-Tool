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
        private readonly IAnsiConsole _Console;
        private readonly ILoggerService _Logger;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public EmailConfigStep(
            IAnsiConsole console,
            ILoggerService logger,
            InstallOptionsModel options)
        {
            _Console = console;
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

            bool configureEmail = _Console.Prompt(new ConfirmationPrompt("Configure email notifications?")
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
                string smtpHost = _Console.Prompt(new TextPrompt<string>("Enter the SMTP host:"));
                string smtpUsername = _Console.Prompt(new TextPrompt<string>("Enter the SMTP authentication username (email):"));
                string smtpPassword = _Console.Prompt(new TextPrompt<string>("Enter the SMTP password:").Secret());
                int port = _Console.Prompt(new TextPrompt<int>("Enter the SMTP port:").DefaultValue(InstallerValues.Defaults.SmtpPort));
                bool enableSsl = _Console.Prompt(new ConfirmationPrompt("Enable SSL?")
                {
                    DefaultValue = InstallerValues.Defaults.EnableSSL,
                    ShowDefaultValue = false
                });
                string fromEmail = _Console.Prompt(new TextPrompt<string>("Enter the from email address:").DefaultValue(smtpUsername));
                string fromName = _Console.Prompt(new TextPrompt<string>("Enter the from name:").DefaultValue(InstallerValues.Defaults.FromName));

                List<EmailTemplateModel> emails = [];

                while (_Console.Prompt(new ConfirmationPrompt("Add an email template?")
                {
                    ShowDefaultValue = false
                }))
                {
                    string triggerType = _Console.Prompt(new SelectionPrompt<string>()
                        .Title("Select the trigger type:")
                        .AddChoices(
                            "Open (system — sent on application startup)",
                            "Close (system — sent on application shutdown)",
                            "Heartbeat (system — sent when server heartbeat fails)",
                            "Custom (triggered by server output text)"));

                    _Console.MarkupLine($"Selected Trigger Type: [blue]{Markup.Escape(triggerType)}[/]");

                    string trigger;
                    bool isSystem;

                    if (triggerType.StartsWith("Custom"))
                    {
                        trigger = _Console.Prompt(new TextPrompt<string>("Enter the server output text to match:"));
                        isSystem = false;
                    }

                    else
                    {
                        trigger = triggerType.Split(' ')[0];
                        isSystem = true;
                    }

                    string subject = _Console.Prompt(new TextPrompt<string>("Enter the subject:").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Subject is required.")));
                    string content = _Console.Prompt(new TextPrompt<string>("Enter the content (HTML or path to .html file):").Validate(input => !string.IsNullOrWhiteSpace(input) ? ValidationResult.Success() : ValidationResult.Error("Content is required.")));

                    List<RecipientModel> recipients = [];

                    _Console.MarkupLine("[grey]At least one recipient is required.[/]");

                    string email = _Console.Prompt(new TextPrompt<string>("Enter the recipient email:"));
                    string name = _Console.Prompt(new TextPrompt<string>("Enter the recipient name:"));

                    recipients.Add(new RecipientModel
                    {
                        Email = email,
                        Name = name
                    });

                    while (_Console.Prompt(new ConfirmationPrompt("Add another recipient?")
                    {
                        ShowDefaultValue = false
                    }))
                    {
                        email = _Console.Prompt(new TextPrompt<string>("Enter the recipient email:"));
                        name = _Console.Prompt(new TextPrompt<string>("Enter the recipient name:"));

                        recipients.Add(new RecipientModel
                        {
                            Email = email,
                            Name = name
                        });
                    }

                    List<ImageModel> images = [];

                    while (_Console.Prompt(new ConfirmationPrompt("Add an inline image?")
                    {
                        ShowDefaultValue = false
                    }))
                    {
                        string imageKey = _Console.Prompt(new TextPrompt<string>("Enter the content ID (referenced in HTML as cid:value):"));
                        string imagePath = _Console.Prompt(new TextPrompt<string>("Enter the image file path:"));

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
                    SmtpUsername = smtpUsername,
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
