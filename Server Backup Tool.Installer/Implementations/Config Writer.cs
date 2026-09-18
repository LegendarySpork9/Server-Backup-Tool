// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Values;
using System.Text.Json;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Implementations
{
    public class ConfigWriter : IConfigWriter
    {
        private readonly ILoggerService _Logger;
        private readonly IExtendedFileSystem _FileSystem;

        // Sets the class's global variables.
        public ConfigWriter(
            ILoggerService logger,
            IExtendedFileSystem fileSystem)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
        }

        /// <summary>
        /// Generates the App.config XML document for the Server Backup Tool.
        /// </summary>
        public XDocument GenerateAppConfig(InstallOptionsModel options)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Generating App.config.");

            XElement serverBackup = new("serverBackup",
                new XElement("serverDetails",
                    new XAttribute("name", options.ServerConfig.ServerName),
                    new XAttribute("game", options.ServerConfig.Game),
                    new XAttribute("location", options.ServerConfig.ServerDirectory),
                    new XAttribute("startFile", options.ServerConfig.StartFile),
                    new XAttribute("ipAddress", options.ServerConfig.IPAddress)),
                new XElement("databaseDetails",
                    new XAttribute("path", options.ServerConfig.DatabasePath),
                    new XAttribute("pollingInterval", options.ServerConfig.PollingIntervalMs)),
                BuildTimerDetails(options.TimerConfig),
                BuildNotifications(options.EmailConfig));

            XElement log4Net = BuildLog4NetSection();

            XDocument config = new(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("configuration",
                    new XElement("configSections",
                        new XElement("section",
                            new XAttribute("name", "log4net"),
                            new XAttribute("type", "log4net.Config.Log4NetConfigurationSectionHandler,log4net")),
                        new XElement("section",
                            new XAttribute("name", "serverBackup"),
                            new XAttribute("type", "ServerBackupTool.Models.Configuration.SBTSection, ServerBackupTool"))),
                    serverBackup,
                    log4Net));

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "App.config generated.");

            return config;
        }

        /// <summary>
        /// Generates the appsettings.json content for the SBT API.
        /// </summary>
        public string GenerateApiAppSettings(
            ApiConfigModel apiConfig,
            ServerConfigModel serverConfig)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Generating appsettings.json.");

            Dictionary<string, object> kestrelEndpoints = new()
            {
                ["Http"] = new
                {
                    Url = $"http://{apiConfig.BindAddress}:{apiConfig.HttpPort}"
                }
            };

            if (apiConfig.EnableHttps)
            {
                kestrelEndpoints["Https"] = new
                {
                    Url = $"https://{apiConfig.BindAddress}:{apiConfig.HttpsPort}",
                    Certificate = new
                    {
                        Path = apiConfig.CertificatePath,
                        Password = apiConfig.CertificatePassword
                    }
                };
            }

            Dictionary<string, object> settings = new()
            {
                ["Logging"] = new
                {
                    LogLevel = new
                    {
                        Default = "Information",
                        Microsoft_AspNetCore = "Warning"
                    }
                },
                ["AllowedHosts"] = "*",
                ["Kestrel"] = new
                {
                    Endpoints = kestrelEndpoints
                },
                ["Authentication"] = new
                {
                    ClientId = apiConfig.ClientIdHash,
                    ClientSecret = apiConfig.ClientSecretHash
                },
                ["Database"] = new
                {
                    Path = apiConfig.DatabasePath,
                    ServerName = serverConfig.ServerName,
                    PollingIntervalMs = serverConfig.PollingIntervalMs
                },
                ["ArchiveSettings"] = new
                {
                    ArchiveDirectory = apiConfig.ArchiveDirectory
                },
                ["Webhook"] = new
                {
                    Secret = apiConfig.WebhookSecret,
                    TimeoutSeconds = InstallerValues.Defaults.WebhookTimeoutSeconds,
                    MaxRetries = InstallerValues.Defaults.WebhookMaxRetries
                }
            };

            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                { 
                    WriteIndented = true 
                });

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "appsettings.json generated.");

            return json;
        }

        /// <summary>
        /// Writes the App.config XML document to the specified path.
        /// </summary>
        public async Task<(bool, Exception?)> WriteConfig(
            string path,
            XDocument config)
        {
            bool written = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Writing App.config to {path}.");

                await using (FileStream stream = new(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true))
                {
                    await config.SaveAsync(
                        stream,
                        SaveOptions.None,
                        CancellationToken.None);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "App.config written.");

                    written = true;
                }
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Write App.config: {ex.Message}");

                exception = ex;
            }

            return (
                written,
                exception);
        }

        /// <summary>
        /// Writes the appsettings.json content to the specified path.
        /// </summary>
        public async Task<(bool, Exception?)> WriteApiSettings(
            string path,
            string json)
        {
            bool written = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Writing appsettings.json to {path}.");

                await _FileSystem.WriteAllText(
                    path,
                    json);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "appsettings.json written.");

                written = true;
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Write appsettings.json: {ex.Message}");

                exception = ex;
            }

            return (
                written,
                exception);
        }

        /// <summary>
        /// Migrates an existing App.config by adding missing elements and attributes from the reference.
        /// </summary>
        public (bool, List<string>) MigrateAppConfig(
            XDocument existingConfig,
            XDocument referenceConfig)
        {
            List<string> additions = [];

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Migrating App.config.");

            if (existingConfig.Root != null && referenceConfig.Root != null)
            {
                MergeElements(
                    existingConfig.Root,
                    referenceConfig.Root,
                    string.Empty,
                    additions);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"App.config migration complete. {additions.Count} addition(s) made.");

            return (
                additions.Count > 0,
                additions);
        }

        /// <summary>
        /// Migrates an existing appsettings.json by adding missing properties from the reference.
        /// </summary>
        public (bool, List<string>) MigrateApiAppSettings(
            string existingJson,
            string referenceJson)
        {
            List<string> additions = [];

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Migrating appsettings.json.");

            using JsonDocument existingDoc = JsonDocument.Parse(existingJson);
            using JsonDocument referenceDoc = JsonDocument.Parse(referenceJson);

            Dictionary<string, object?> merged = JsonSerializer.Deserialize<Dictionary<string, object?>>(existingJson) ?? [];

            foreach (JsonProperty property in referenceDoc.RootElement.EnumerateObject())
            {
                if (!existingDoc.RootElement.TryGetProperty(
                    property.Name,
                    out _))
                {
                    merged[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());

                    additions.Add(property.Name);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        $"Added missing section '{property.Name}' to appsettings.json.");
                }

                else if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    JsonElement existingSection = existingDoc.RootElement.GetProperty(property.Name);

                    Dictionary<string, object?> existingSectionDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(existingSection.GetRawText()) ?? [];

                    foreach (JsonProperty subProperty in property.Value.EnumerateObject())
                    {
                        if (!existingSection.TryGetProperty(
                            subProperty.Name,
                            out _))
                        {
                            existingSectionDict[subProperty.Name] = JsonSerializer.Deserialize<object>(subProperty.Value.GetRawText());

                            string path = $"{property.Name}.{subProperty.Name}";
                            additions.Add(path);

                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Info,
                                $"Added missing property '{path}' to appsettings.json.");
                        }
                    }

                    merged[property.Name] = existingSectionDict;
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"appsettings.json migration complete. {additions.Count} addition(s) made.");

            return (
                additions.Count > 0,
                additions);
        }

        /// <summary>
        /// Recursively merges missing elements and attributes from a reference element into an existing element.
        /// </summary>
        private static void MergeElements(
            XElement existing,
            XElement reference,
            string path,
            List<string> additions)
        {
            foreach (XAttribute refAttribute in reference.Attributes())
            {
                if (existing.Attribute(refAttribute.Name) == null)
                {
                    existing.SetAttributeValue(
                        refAttribute.Name,
                        refAttribute.Value);

                    string attributePath = string.IsNullOrEmpty(path) ? $"{existing.Name}@{refAttribute.Name}" : $"{path}/{existing.Name}@{refAttribute.Name}";

                    additions.Add(attributePath);
                }
            }

            foreach (XElement refChild in reference.Elements())
            {
                XElement? existingChild = FindMatchingElement(
                    existing,
                    refChild);
                string childPath = string.IsNullOrEmpty(path) ? refChild.Name.LocalName : $"{path}/{refChild.Name.LocalName}";

                if (existingChild == null)
                {
                    existing.Add(new XElement(refChild));
                    additions.Add(childPath);
                }

                else
                {
                    MergeElements(
                        existingChild,
                        refChild,
                        childPath,
                        additions);
                }
            }
        }

        /// <summary>
        /// Finds a matching element in the parent by name and key attribute.
        /// </summary>
        private static XElement? FindMatchingElement(
            XElement parent,
            XElement reference)
        {
            List<XElement> siblings = [.. parent.Elements(reference.Name)];

            XElement? match = null;

            if (siblings.Count <= 1)
            {
                match = siblings.FirstOrDefault();
            }
            else
            {
                string[] keyAttributes =
                [
                    "name",
                    "ref",
                    "trigger"
                ];

                bool keyFound = false;

                foreach (string key in keyAttributes)
                {
                    XAttribute? refAttr = reference.Attribute(key);

                    if (refAttr != null)
                    {
                        match = siblings.FirstOrDefault(e => e.Attribute(key)?.Value == refAttr.Value);
                        keyFound = true;

                        break;
                    }
                }

                if (!keyFound)
                {
                    match = siblings.FirstOrDefault();
                }
            }

            return match;
        }

        /// <summary>
        /// Builds the timerDetails XML element.
        /// </summary>
        private static XElement BuildTimerDetails(TimerConfigModel timerConfig)
        {
            XElement timerDetails = new("timerDetails",
                new XAttribute("count", timerConfig.CustomTimers.Count + 1),
                new XAttribute("backupTime", timerConfig.BackupTime));

            XElement timers = new("timers");

            foreach (CustomTimerModel timer in timerConfig.CustomTimers)
            {
                timers.Add(new XElement("timer",
                    new XAttribute("name", timer.Name),
                    new XAttribute("time", timer.Time),
                    new XAttribute("message", timer.Message)));
            }

            timerDetails.Add(timers);

            return timerDetails;
        }

        /// <summary>
        /// Builds the notifications XML element.
        /// </summary>
        private static XElement BuildNotifications(EmailConfigModel? emailConfig)
        {
            XElement notifications;

            if (emailConfig == null)
            {
                notifications = new XElement("notifications",
                    new XAttribute("enabled", false),
                    new XElement("provider",
                        new XAttribute("name", string.Empty),
                        new XAttribute("password", string.Empty)),
                    new XElement("fromAddress",
                        new XAttribute("email", string.Empty),
                        new XAttribute("name", string.Empty)),
                    new XElement("emails"));
            }

            else
            {
                notifications = new("notifications",
                new XAttribute("enabled", emailConfig.Enabled),
                new XAttribute("port", emailConfig.Port),
                new XAttribute("enableSSL", emailConfig.EnableSSL));

                notifications.Add(new XElement("provider",
                    new XAttribute("name", emailConfig.SmtpHost),
                    new XAttribute("password", emailConfig.SmtpPassword)));

                notifications.Add(new XElement("fromAddress",
                    new XAttribute("email", emailConfig.FromEmail),
                    new XAttribute("name", emailConfig.FromName)));

                XElement emails = new("emails");

                foreach (EmailTemplateModel template in emailConfig.Emails)
                {
                    XElement email = new("email",
                        new XAttribute("trigger", template.Trigger),
                        new XAttribute("system", template.IsSystem));

                    XElement addresses = new("addresses");

                    foreach (RecipientModel recipient in template.Recipients)
                    {
                        addresses.Add(new XElement("toAddress",
                            new XAttribute("email", recipient.Email),
                            new XAttribute("name", recipient.Name)));
                    }

                    email.Add(addresses);
                    email.Add(new XElement("subject", new XAttribute("value", template.Subject)));
                    email.Add(new XElement("content", new XAttribute("value", template.Content)));

                    XElement images = new("images");

                    foreach (ImageModel image in template.Images)
                    {
                        images.Add(new XElement("image",
                            new XAttribute("key", image.Key),
                            new XAttribute("path", image.Path)));
                    }

                    email.Add(images);

                    emails.Add(email);
                }

                notifications.Add(emails);
            }

            return notifications;
        }

        /// <summary>
        /// Builds the log4net configuration XML element.
        /// </summary>
        private static XElement BuildLog4NetSection()
        {
            return new XElement("log4net",
                new XElement("appender",
                    new XAttribute("name", "ConsoleAppender"),
                    new XAttribute("type", "log4net.Appender.ConsoleAppender"),
                    new XElement("layout",
                        new XAttribute("type", "log4net.Layout.PatternLayout"),
                        new XElement("conversionPattern",
                            new XAttribute("value", "log4net - %message%newline"))),
                    new XElement("filter",
                        new XAttribute("type", "log4net.Filter.LevelRangeFilter"),
                        new XElement("levelMin", new XAttribute("value", "INFO")),
                        new XElement("levelMax", new XAttribute("value", "WARN"))),
                    new XElement("filter",
                        new XAttribute("type", "log4net.Filter.DenyAllFilter"))),
                new XElement("appender",
                    new XAttribute("name", "BackupLogAppender"),
                    new XAttribute("type", "log4net.Appender.RollingFileAppender"),
                    new XElement("file", new XAttribute("value", @"Logs\Server Backup.log")),
                    new XElement("lockingModel", new XAttribute("type", "log4net.Appender.FileAppender+MinimalLock")),
                    new XElement("appendToFile", new XAttribute("value", "true")),
                    new XElement("rollingStyle", new XAttribute("value", "Size")),
                    new XElement("maxSizeRollBackups", new XAttribute("value", "10")),
                    new XElement("maximumFileSize", new XAttribute("value", "10MB")),
                    new XElement("staticLogFileName", new XAttribute("value", "true")),
                    new XElement("layout",
                        new XAttribute("type", "log4net.Layout.PatternLayout"),
                        new XElement("conversionPattern",
                            new XAttribute("value", "%d{ISO8601} %level - %message%newline")))),
                new XElement("appender",
                    new XAttribute("name", "ServerLogAppender"),
                    new XAttribute("type", "log4net.Appender.RollingFileAppender"),
                    new XElement("file", new XAttribute("value", @"Logs\Server.log")),
                    new XElement("lockingModel", new XAttribute("type", "log4net.Appender.FileAppender+MinimalLock")),
                    new XElement("appendToFile", new XAttribute("value", "true")),
                    new XElement("rollingStyle", new XAttribute("value", "Size")),
                    new XElement("maxSizeRollBackups", new XAttribute("value", "10")),
                    new XElement("maximumFileSize", new XAttribute("value", "10MB")),
                    new XElement("staticLogFileName", new XAttribute("value", "true")),
                    new XElement("layout",
                        new XAttribute("type", "log4net.Layout.PatternLayout"),
                        new XElement("conversionPattern",
                            new XAttribute("value", "%d{ISO8601} %level - %message%newline")))),
                new XElement("logger",
                    new XAttribute("name", "ToolLogs"),
                    new XElement("appender-ref", new XAttribute("ref", "BackupLogAppender")),
                    new XElement("appender-ref", new XAttribute("ref", "ConsoleAppender"))),
                new XElement("logger",
                    new XAttribute("name", "ServerLogs"),
                    new XElement("appender-ref", new XAttribute("ref", "ServerLogAppender"))));
        }
    }
}
