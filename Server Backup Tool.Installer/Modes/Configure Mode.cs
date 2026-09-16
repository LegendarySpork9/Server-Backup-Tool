// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Functions;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using Spectre.Console;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Modes
{
    public class ConfigureMode
    {
        private readonly ILoggerService _Logger;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly IConfigWriter _ConfigWriter;
        private readonly IVersionService _VersionService;

        // Sets the class's global variables.
        public ConfigureMode(
            ILoggerService logger,
            IExtendedFileSystem fileSystem,
            IConfigWriter configWriter,
            IVersionService versionService)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
            _ConfigWriter = configWriter;
            _VersionService = versionService;
        }

        /// <summary>
        /// Runs the configure mode.
        /// </summary>
        public async Task Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Starting configure mode.");

            VersionInfoModel? installed = SelectInstallation();

            if (installed == null)
            {
                AnsiConsole.MarkupLine("[red]No existing installation found. Please run the installer first.[/]");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    "No existing installation found.");
            }

            else
            {
                string configPath = Path.Combine(
                    installed.InstallPath,
                    InstallerValues.Defaults.ToolConfigFileName);

                if (!_FileSystem.FileExists(configPath))
                {
                    AnsiConsole.MarkupLine("[red]Configuration file not found.[/]");

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        $"Config file not found at {configPath}.");
                }

                else
                {
                    XDocument? config = null;

                    try
                    {
                        config = XDocument.Load(configPath);
                    }

                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Failed to load configuration: {Markup.Escape(ex.Message)}[/]");
                    }

                    if (config != null)
                    {
                        string apiSettingsPath = !string.IsNullOrEmpty(installed.ApiInstallPath) ? Path.Combine(
                            installed.ApiInstallPath,
                            "appsettings.json") : string.Empty;
                        bool apiInstalled = !string.IsNullOrEmpty(apiSettingsPath) && _FileSystem.FileExists(apiSettingsPath);

                        Dictionary<string, object>? apiSettings = null;
                        JsonDocument? apiDocument = null;

                        if (apiInstalled)
                        {
                            try
                            {
                                string apiJson = await _FileSystem.ReadAllText(apiSettingsPath);
                                apiDocument = JsonDocument.Parse(apiJson);
                                apiSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(apiDocument.RootElement.GetRawText()) ?? [];
                            }

                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to load API settings: {Markup.Escape(ex.Message)}[/]");

                                apiInstalled = false;
                            }
                        }

                        bool continueEditing = true;

                        while (continueEditing)
                        {
                            List<string> choices =
                            [
                                "Server Details",
                                "Backup and Timers",
                                "Email Notifications",
                                "Database Settings"
                            ];

                            if (apiInstalled)
                            {
                                choices.Add("API Settings");
                            }

                            choices.Add("Save and Exit");
                            choices.Add("Exit Without Saving");

                            string section = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                .Title("Which section would you like to edit?")
                                .AddChoices(choices));

                            switch (section)
                            {
                                case "Server Details":
                                    EditServerDetails(config);
                                    break;
                                case "Backup and Timers":
                                    EditTimerDetails(config);
                                    break;
                                case "Email Notifications":
                                    EditEmailNotifications(config);
                                    break;
                                case "Database Settings":
                                    EditDatabaseSettings(config);
                                    break;
                                case "API Settings":
                                    EditApiSettings(
                                        apiDocument!,
                                        apiSettings!);
                                    break;
                                case "Save and Exit":
                                    await SaveAllAsync(
                                        configPath,
                                        config,
                                        apiSettingsPath,
                                        apiSettings);
                                    continueEditing = false;
                                    break;
                                case "Exit Without Saving":
                                    continueEditing = false;
                                    break;
                            }
                        }

                        apiDocument?.Dispose();
                    }
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configure mode completed.");
        }

        /// <summary>
        /// Edits the server details section of the configuration.
        /// </summary>
        private void EditServerDetails(XDocument config)
        {
            XElement? serverDetails = config.Root?.Element("serverBackup")?
                .Element("serverDetails");

            if (serverDetails != null)
            {
                string currentName = serverDetails.Attribute("name")?.Value ?? string.Empty;
                string currentGame = serverDetails.Attribute("game")?.Value ?? string.Empty;
                string currentLocation = serverDetails.Attribute("location")?.Value ?? string.Empty;
                string currentStartFile = serverDetails.Attribute("startFile")?.Value ?? string.Empty;
                string currentIpAddress = serverDetails.Attribute("ipAddress")?.Value ?? string.Empty;

                serverDetails.SetAttributeValue(
                    "name",
                    AnsiConsole.Prompt(new TextPrompt<string>("Server name:").DefaultValue(currentName)));
                serverDetails.SetAttributeValue(
                    "game",
                    AnsiConsole.Prompt(new TextPrompt<string>("Game:").DefaultValue(currentGame)));
                serverDetails.SetAttributeValue(
                    "location",
                    AnsiConsole.Prompt(new TextPrompt<string>("Server directory:").DefaultValue(currentLocation)));
                serverDetails.SetAttributeValue(
                    "startFile",
                    AnsiConsole.Prompt(new TextPrompt<string>("Start file:").DefaultValue(currentStartFile)));
                serverDetails.SetAttributeValue(
                    "ipAddress",
                    AnsiConsole.Prompt(new TextPrompt<string>("IP address:").DefaultValue(currentIpAddress)));

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Server details updated.");
            }

            else
            {
                AnsiConsole.MarkupLine("[red]Server details section not found.[/]");
            }
        }

        /// <summary>
        /// Edits the timer details section of the configuration.
        /// </summary>
        private void EditTimerDetails(XDocument config)
        {
            XElement? timerDetails = config.Root?.Element("serverBackup")?
                .Element("timerDetails");

            if (timerDetails != null)
            {
                bool continueEditing = true;

                while (continueEditing)
                {
                    XElement? timersElement = timerDetails.Element("timers");
                    List<XElement> existingTimers = timersElement?.Elements("timer")
                        .ToList() ?? [];

                    string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title($"Backup and Timers ({existingTimers.Count} custom timer(s) configured)")
                        .AddChoices(
                            "Edit Backup Time",
                            "Add Custom Timer",
                            "Remove Custom Timer",
                            "Back"));

                    switch (choice)
                    {
                        case "Edit Backup Time":
                            string currentBackupTime = timerDetails.Attribute("backupTime")?.Value ?? string.Empty;
                            timerDetails.SetAttributeValue(
                                "backupTime",
                                AnsiConsole.Prompt(new TextPrompt<string>("Backup time (HH:mm:ss):").DefaultValue(currentBackupTime)
                                    .Validate(input => TimeSpan.TryParse(
                                        input,
                                        out _) ? ValidationResult.Success() : ValidationResult.Error("Please enter a valid time in HH:mm:ss format."))));
                            break;

                        case "Add Custom Timer":
                            string timerName = AnsiConsole.Prompt(new TextPrompt<string>("Enter the timer name:"));
                            string timerTime = AnsiConsole.Prompt(new TextPrompt<string>("Enter the timer time (HH:mm:ss):").Validate(input => TimeSpan.TryParse(
                                input,
                                out _) ? ValidationResult.Success() : ValidationResult.Error("Please enter a valid time in HH:mm:ss format.")));
                            string timerMessage = AnsiConsole.Prompt(new TextPrompt<string>("Enter the timer message:"));

                            if (timersElement == null)
                            {
                                timersElement = new XElement("timers");
                                timerDetails.Add(timersElement);
                            }

                            timersElement.Add(new XElement("timer",
                                new XAttribute("name", timerName),
                                new XAttribute("time", timerTime),
                                new XAttribute("message", timerMessage)));

                            int newCount = timersElement.Elements("timer")
                                .Count() + 1;
                            timerDetails.SetAttributeValue(
                                "count",
                                newCount);

                            AnsiConsole.MarkupLine($"[green]Timer '{Markup.Escape(timerName)}' added.[/]");
                            break;

                        case "Remove Custom Timer":
                            if (existingTimers.Count == 0)
                            {
                                AnsiConsole.MarkupLine("[yellow]No custom timers to remove.[/]");
                            }

                            else
                            {
                                List<string> timerChoices = [.. existingTimers.Select(t => $"{t.Attribute("name")?.Value} ({t.Attribute("time")?.Value})")];

                                string selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                    .Title("Select a timer to remove:")
                                    .AddChoices(timerChoices));

                                int selectedIndex = timerChoices.IndexOf(selected);

                                if (selectedIndex >= 0)
                                {
                                    existingTimers[selectedIndex].Remove();

                                    int updatedCount = (timersElement?.Elements("timer")
                                        .Count() ?? 0) + 1;
                                    timerDetails.SetAttributeValue(
                                        "count",
                                        updatedCount);

                                    AnsiConsole.MarkupLine($"[green]Timer removed.[/]");
                                }
                            }
                            break;

                        case "Back":
                            continueEditing = false;
                            break;
                    }
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Timer details updated.");
            }

            else
            {
                AnsiConsole.MarkupLine("[red]Timer details section not found.[/]");
            }
        }

        /// <summary>
        /// Edits the email notifications section of the configuration.
        /// </summary>
        private void EditEmailNotifications(XDocument config)
        {
            XElement? notifications = config.Root?.Element("serverBackup")?
                .Element("notifications");

            if (notifications != null)
            {
                bool continueEditing = true;

                while (continueEditing)
                {
                    XElement? emailsElement = notifications.Element("emails");
                    List<XElement> existingEmails = emailsElement?.Elements("email")
                        .ToList() ?? [];

                    string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title($"Email Notifications ({existingEmails.Count} template(s) configured)")
                        .AddChoices(
                            "Toggle Enabled",
                            "Edit SMTP Settings",
                            "Edit From Address",
                            "Add Email Templates",
                            "Edit Email Templates",
                            "Remove Email Templates",
                            "Back"));

                    switch (choice)
                    {
                        case "Toggle Enabled":
                            string currentEnabled = notifications.Attribute("enabled")?.Value ?? "false";
                            bool enabled = AnsiConsole.Prompt(new ConfirmationPrompt("Enable email notifications?")
                            {
                                DefaultValue = currentEnabled.Equals(
                                    "true",
                                    StringComparison.OrdinalIgnoreCase),
                                ShowDefaultValue = false
                            });
                            notifications.SetAttributeValue(
                                "enabled",
                                enabled);
                            break;

                        case "Edit SMTP Settings":
                            XElement? provider = notifications.Element("provider");

                            if (provider != null)
                            {
                                string currentHost = provider.Attribute("name")?.Value ?? string.Empty;
                                provider.SetAttributeValue(
                                    "name",
                                    AnsiConsole.Prompt(new TextPrompt<string>("SMTP host:").DefaultValue(currentHost)));
                                provider.SetAttributeValue(
                                    "password",
                                    AnsiConsole.Prompt(new TextPrompt<string>("SMTP password:").Secret()));
                            }
                            break;

                        case "Edit From Address":
                            XElement? fromAddress = notifications.Element("fromAddress");

                            if (fromAddress != null)
                            {
                                string currentEmail = fromAddress.Attribute("email")?.Value ?? string.Empty;
                                string currentName = fromAddress.Attribute("name")?.Value ?? InstallerValues.Defaults.FromName;
                                fromAddress.SetAttributeValue(
                                    "email",
                                    AnsiConsole.Prompt(new TextPrompt<string>("From email:").DefaultValue(currentEmail)));
                                fromAddress.SetAttributeValue(
                                    "name",
                                    AnsiConsole.Prompt(new TextPrompt<string>("From name:").DefaultValue(currentName)));
                            }
                            break;

                        case "Add Email Templates":
                            AddEmailTemplate(notifications);
                            break;

                        case "Edit Email Templates":
                            if (existingEmails.Count == 0)
                            {
                                AnsiConsole.MarkupLine("[yellow]No email templates to edit.[/]");
                            }

                            else
                            {
                                List<string> editTemplateChoices = [.. existingEmails.Select(e => $"{e.Attribute("trigger")?.Value} ({(e.Attribute("system")?.Value == "true" || e.Attribute("system")?.Value == "True" ? "System" : "Custom")})")];

                                string editSelected = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                    .Title("Select a template to edit:")
                                    .AddChoices(editTemplateChoices));

                                int editSelectedIndex = editTemplateChoices.IndexOf(editSelected);

                                if (editSelectedIndex >= 0)
                                {
                                    EditEmailTemplate(existingEmails[editSelectedIndex]);
                                }
                            }
                            break;

                        case "Remove Email Templates":
                            if (existingEmails.Count == 0)
                            {
                                AnsiConsole.MarkupLine("[yellow]No email templates to remove.[/]");
                            }

                            else
                            {
                                List<string> templateChoices = [.. existingEmails.Select(e => $"{e.Attribute("trigger")?.Value} ({(e.Attribute("system")?.Value == "true" || e.Attribute("system")?.Value == "True" ? "System" : "Custom")})")];

                                string selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                    .Title("Select a template to remove:")
                                    .AddChoices(templateChoices));

                                int selectedIndex = templateChoices.IndexOf(selected);

                                if (selectedIndex >= 0)
                                {
                                    existingEmails[selectedIndex].Remove();

                                    AnsiConsole.MarkupLine("[green]Email template removed.[/]");
                                }
                            }
                            break;

                        case "Back":
                            continueEditing = false;
                            break;
                    }
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Email notification settings updated.");
            }

            else
            {
                AnsiConsole.MarkupLine("[red]Notifications section not found.[/]");
            }
        }

        /// <summary>
        /// Adds a new email template to the notifications section.
        /// </summary>
        private void AddEmailTemplate(XElement notifications)
        {
            XElement? emailsElement = notifications.Element("emails");

            if (emailsElement == null)
            {
                emailsElement = new XElement("emails");
                notifications.Add(emailsElement);
            }

            string triggerType = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select the trigger type:")
                .AddChoices(
                    "Open (system — sent on application startup)",
                    "Close (system — sent on application shutdown)",
                    "Heartbeat (system — sent when server heartbeat fails)",
                    "Custom (triggered by server output text)"));

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

            XElement email = new("email",
                new XAttribute("trigger", trigger),
                new XAttribute("system", isSystem));

            XElement addresses = new("addresses");

            AnsiConsole.MarkupLine("[grey]At least one recipient is required.[/]");

            string recipientEmail = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient email:"));
            string recipientName = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient name:"));

            addresses.Add(new XElement("toAddress",
                new XAttribute("email", recipientEmail),
                new XAttribute("name", recipientName)));

            while (AnsiConsole.Prompt(new ConfirmationPrompt("Add another recipient?")
            {
                ShowDefaultValue = false
            }))
            {
                recipientEmail = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient email:"));
                recipientName = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient name:"));

                addresses.Add(new XElement("toAddress",
                    new XAttribute("email", recipientEmail),
                    new XAttribute("name", recipientName)));
            }

            email.Add(addresses);
            email.Add(new XElement("subject", new XAttribute("value", subject)));
            email.Add(new XElement("content", new XAttribute("value", content)));

            XElement images = new("images");

            while (AnsiConsole.Prompt(new ConfirmationPrompt("Add an inline image?")
            {
                ShowDefaultValue = false
            }))
            {
                string imageKey = AnsiConsole.Prompt(new TextPrompt<string>("Enter the content ID (referenced in HTML as cid:value):"));
                string imagePath = AnsiConsole.Prompt(new TextPrompt<string>("Enter the image file path:"));

                images.Add(new XElement("image",
                    new XAttribute("key", imageKey),
                    new XAttribute("path", imagePath)));
            }

            email.Add(images);
            emailsElement.Add(email);

            AnsiConsole.MarkupLine($"[green]Email template '{Markup.Escape(trigger)}' added.[/]");
        }

        /// <summary>
        /// Edits an existing email template.
        /// </summary>
        private void EditEmailTemplate(XElement emailElement)
        {
            bool continueEditing = true;

            while (continueEditing)
            {
                string currentTrigger = emailElement.Attribute("trigger")?.Value ?? string.Empty;
                string currentSystem = emailElement.Attribute("system")?.Value ?? "false";
                string currentSubject = emailElement.Element("subject")?
                    .Attribute("value")?.Value ?? string.Empty;
                string currentContent = emailElement.Element("content")?
                    .Attribute("value")?.Value ?? string.Empty;
                XElement? addressesElement = emailElement.Element("addresses");
                List<XElement> existingRecipients = addressesElement?.Elements("toAddress")
                    .ToList() ?? [];

                string action = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title($"Editing template: {Markup.Escape(currentTrigger)} ({(currentSystem == "true" || currentSystem == "True" ? "System" : "Custom")})")
                    .AddChoices(
                        "Edit Subject",
                        "Edit Content",
                        "Add Recipient",
                        "Remove Recipient",
                        "Manage Images",
                        "Back"));

                switch (action)
                {
                    case "Edit Subject":
                        XElement? subjectElement = emailElement.Element("subject");

                        if (subjectElement != null)
                        {
                            subjectElement.SetAttributeValue(
                                "value",
                                AnsiConsole.Prompt(new TextPrompt<string>("Subject:").DefaultValue(currentSubject)));
                        }
                        break;

                    case "Edit Content":
                        XElement? contentElement = emailElement.Element("content");

                        if (contentElement != null)
                        {
                            contentElement.SetAttributeValue(
                                "value",
                                AnsiConsole.Prompt(new TextPrompt<string>("Content (HTML or path to .html file):").DefaultValue(currentContent)));
                        }
                        break;

                    case "Add Recipient":
                        if (addressesElement == null)
                        {
                            addressesElement = new XElement("addresses");
                            emailElement.Add(addressesElement);
                        }

                        string recipientEmail = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient email:"));
                        string recipientName = AnsiConsole.Prompt(new TextPrompt<string>("Enter the recipient name:"));

                        addressesElement.Add(new XElement("toAddress",
                            new XAttribute("email", recipientEmail),
                            new XAttribute("name", recipientName)));

                        AnsiConsole.MarkupLine($"[green]Recipient '{Markup.Escape(recipientEmail)}' added.[/]");
                        break;

                    case "Remove Recipient":
                        if (existingRecipients.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No recipients to remove.[/]");
                        }

                        else
                        {
                            List<string> recipientChoices = [.. existingRecipients.Select(r => $"{r.Attribute("name")?.Value} ({r.Attribute("email")?.Value})")];

                            string selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                .Title("Select a recipient to remove:")
                                .AddChoices(recipientChoices));

                            int selectedIndex = recipientChoices.IndexOf(selected);

                            if (selectedIndex >= 0)
                            {
                                existingRecipients[selectedIndex].Remove();

                                AnsiConsole.MarkupLine("[green]Recipient removed.[/]");
                            }
                        }
                        break;

                    case "Manage Images":
                        ManageTemplateImages(emailElement);
                        break;

                    case "Back":
                        continueEditing = false;
                        break;
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Email template updated.");
        }

        /// <summary>
        /// Manages inline images for an email template.
        /// </summary>
        private void ManageTemplateImages(XElement emailElement)
        {
            XElement? imagesElement = emailElement.Element("images");

            if (imagesElement == null)
            {
                imagesElement = new XElement("images");
                emailElement.Add(imagesElement);
            }

            bool continueManaging = true;

            while (continueManaging)
            {
                List<XElement> existingImages = [.. imagesElement.Elements("image")];

                string action = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title($"Images ({existingImages.Count} configured)")
                    .AddChoices(
                        "Add Image",
                        "Remove Image",
                        "Back"));

                switch (action)
                {
                    case "Add Image":
                        string imageKey = AnsiConsole.Prompt(new TextPrompt<string>("Enter the content ID (referenced in HTML as cid:value):"));
                        string imagePath = AnsiConsole.Prompt(new TextPrompt<string>("Enter the image file path:"));

                        imagesElement.Add(new XElement("image",
                            new XAttribute("key", imageKey),
                            new XAttribute("path", imagePath)));

                        AnsiConsole.MarkupLine($"[green]Image '{Markup.Escape(imageKey)}' added.[/]");
                        break;

                    case "Remove Image":
                        if (existingImages.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No images to remove.[/]");
                        }

                        else
                        {
                            List<string> imageChoices = [.. existingImages.Select(i => $"{i.Attribute("key")?.Value} ({i.Attribute("path")?.Value})")];

                            string selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                .Title("Select an image to remove:")
                                .AddChoices(imageChoices));

                            int selectedIndex = imageChoices.IndexOf(selected);

                            if (selectedIndex >= 0)
                            {
                                existingImages[selectedIndex].Remove();

                                AnsiConsole.MarkupLine("[green]Image removed.[/]");
                            }
                        }
                        break;

                    case "Back":
                        continueManaging = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Edits the database settings section of the configuration.
        /// </summary>
        private void EditDatabaseSettings(XDocument config)
        {
            XElement? databaseDetails = config.Root?.Element("serverBackup")?
                .Element("databaseDetails");

            if (databaseDetails != null)
            {
                string currentPath = databaseDetails.Attribute("path")?.Value ?? string.Empty;
                string currentInterval = databaseDetails.Attribute("pollingInterval")?.Value ?? "1000";

                databaseDetails.SetAttributeValue(
                    "path",
                    AnsiConsole.Prompt(new TextPrompt<string>("Database path:").DefaultValue(currentPath)));
                databaseDetails.SetAttributeValue(
                    "pollingInterval",
                    AnsiConsole.Prompt(new TextPrompt<string>("Polling interval (ms):").DefaultValue(currentInterval)));

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Database settings updated.");
            }

            else
            {
                AnsiConsole.MarkupLine("[red]Database details section not found.[/]");
            }
        }

        /// <summary>
        /// Edits the API appsettings.json settings in memory.
        /// </summary>
        private void EditApiSettings(
            JsonDocument document,
            Dictionary<string, object> settings)
        {
            string apiSection = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Which API section would you like to edit?")
                .AddChoices(
                    "Database",
                    "Archive Settings",
                    "Authentication",
                    "Webhook",
                    "Back"));

            switch (apiSection)
            {
                case "Database":
                    EditApiDatabase(
                        document,
                        settings);
                    break;
                case "Archive Settings":
                    EditApiArchiveSettings(
                        document,
                        settings);
                    break;
                case "Authentication":
                    EditApiAuthentication(settings);
                    break;
                case "Webhook":
                    EditApiWebhook(
                        document,
                        settings);
                    break;
                case "Back":
                    break;
            }
        }

        /// <summary>
        /// Edits the API database settings.
        /// </summary>
        private void EditApiDatabase(
            JsonDocument document,
            Dictionary<string, object> settings)
        {
            JsonElement dbElement = document.RootElement.GetProperty("Database");
            string currentPath = dbElement.GetProperty("Path")
                .GetString() ?? string.Empty;
            string currentServerName = dbElement.GetProperty("ServerName")
                .GetString() ?? string.Empty;
            int currentPollingInterval = dbElement.GetProperty("PollingIntervalMs")
                .GetInt32();

            string path = AnsiConsole.Prompt(new TextPrompt<string>("Database path:").DefaultValue(currentPath));
            string serverName = AnsiConsole.Prompt(new TextPrompt<string>("Server name:").DefaultValue(currentServerName));
            int pollingInterval = AnsiConsole.Prompt(new TextPrompt<int>("Polling interval (ms):").DefaultValue(currentPollingInterval));

            settings["Database"] = new
            {
                Path = path,
                ServerName = serverName,
                PollingIntervalMs = pollingInterval
            };

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "API database settings updated.");
        }

        /// <summary>
        /// Edits the API archive settings.
        /// </summary>
        private void EditApiArchiveSettings(
            JsonDocument document,
            Dictionary<string, object> settings)
        {
            JsonElement archiveElement = document.RootElement.GetProperty("ArchiveSettings");
            string currentDirectory = archiveElement.GetProperty("ArchiveDirectory")
                .GetString() ?? InstallerValues.Defaults.ArchiveDirectory;

            string archiveDirectory = AnsiConsole.Prompt(new TextPrompt<string>("Archive directory:").DefaultValue(currentDirectory));

            settings["ArchiveSettings"] = new
            {
                ArchiveDirectory = archiveDirectory
            };

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "API archive settings updated.");
        }

        /// <summary>
        /// Edits the API authentication settings by regenerating credentials.
        /// </summary>
        private void EditApiAuthentication(Dictionary<string, object> settings)
        {
            if (AnsiConsole.Prompt(new ConfirmationPrompt("Regenerate API credentials?")
            {
                DefaultValue = false,
                ShowDefaultValue = false
            }))
            {
                string clientId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                string clientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

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

                settings["Authentication"] = new
                {
                    ClientId = HashFunction.HashValue(clientId),
                    ClientSecret = HashFunction.HashValue(clientSecret)
                };

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "API authentication credentials regenerated.");
            }
        }

        /// <summary>
        /// Edits the API webhook settings.
        /// </summary>
        private void EditApiWebhook(
            JsonDocument document,
            Dictionary<string, object> settings)
        {
            JsonElement webhookElement = document.RootElement.GetProperty("Webhook");
            string currentSecret = webhookElement.GetProperty("Secret")
                .GetString() ?? string.Empty;
            int currentTimeout = webhookElement.GetProperty("TimeoutSeconds")
                .GetInt32();
            int currentRetries = webhookElement.GetProperty("MaxRetries")
                .GetInt32();

            string secret = currentSecret;

            if (AnsiConsole.Prompt(new ConfirmationPrompt("Change webhook secret?")
            {
                DefaultValue = false,
                ShowDefaultValue = false
            }))
            {
                string newSecretPlain = AnsiConsole.Prompt(new TextPrompt<string>("Enter the new webhook signing secret:").Secret());
                byte[] webhookHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(newSecretPlain));
                secret = Convert.ToHexString(webhookHashBytes)
                    .ToLowerInvariant();
            }

            int timeout = AnsiConsole.Prompt(new TextPrompt<int>("Timeout (seconds):").DefaultValue(currentTimeout));
            int retries = AnsiConsole.Prompt(new TextPrompt<int>("Max retries:").DefaultValue(currentRetries));

            settings["Webhook"] = new
            {
                Secret = secret,
                TimeoutSeconds = timeout,
                MaxRetries = retries
            };

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "API webhook settings updated.");
        }

        /// <summary>
        /// Saves both the App.config and API settings with backups.
        /// </summary>
        private async Task SaveAllAsync(
            string configPath,
            XDocument config,
            string apiSettingsPath,
            Dictionary<string, object>? apiSettings)
        {
            _FileSystem.CopyFile(
                configPath,
                configPath + ".bak",
                true);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Config backed up to {configPath}.bak.");

            (bool configWritten, Exception? configException) = await _ConfigWriter.WriteConfig(
                configPath,
                config);

            if (configWritten)
            {
                AnsiConsole.MarkupLine("[green]App.config saved.[/]");
            }

            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to save App.config: {Markup.Escape(configException?.Message ?? "Unknown error")}[/]");
            }

            if (apiSettings != null && !string.IsNullOrEmpty(apiSettingsPath))
            {
                _FileSystem.CopyFile(
                    apiSettingsPath,
                    apiSettingsPath + ".bak",
                    true);

                string updatedJson = JsonSerializer.Serialize(
                    apiSettings,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                (bool apiWritten, Exception? apiException) = await _ConfigWriter.WriteApiSettings(
                    apiSettingsPath,
                    updatedJson);

                if (apiWritten)
                {
                    AnsiConsole.MarkupLine("[green]API settings saved.[/]");
                }

                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to save API settings: {Markup.Escape(apiException?.Message ?? "Unknown error")}[/]");
                }
            }
        }

        /// <summary>
        /// Enumerates all installations and prompts the user to select one if multiple exist.
        /// </summary>
        private VersionInfoModel? SelectInstallation()
        {
            List<VersionInfoModel> installations = _VersionService.GetAllInstallations();
            VersionInfoModel? selected = null;

            if (installations.Count == 1)
            {
                selected = installations[0];

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Auto-selected single installation: {selected.ServerName}.");
            }

            else if (installations.Count > 1)
            {
                string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Multiple installations found. Which installation would you like to configure?")
                    .AddChoices(installations.Select(i => $"{i.ServerName} (v{i.ToolVersion} at {i.InstallPath})")));

                int selectedIndex = installations.FindIndex(i => choice.StartsWith(
                    $"{i.ServerName} (v{i.ToolVersion}",
                    StringComparison.Ordinal));

                if (selectedIndex >= 0)
                {
                    selected = installations[selectedIndex];
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"User selected installation: {selected?.ServerName}.");
            }

            return selected;
        }
    }
}
