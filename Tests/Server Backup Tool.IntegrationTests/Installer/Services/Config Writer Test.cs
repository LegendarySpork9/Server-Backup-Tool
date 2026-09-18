// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using System.Text.Json;
using System.Xml.Linq;

namespace ServerBackupTool.IntegrationTests.Installer.Services
{
    [TestClass]
    public class ConfigWriterTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private IExtendedFileSystem _FileSystem = null!;
        private ConfigWriter _ConfigWriter = null!;
        private string _TempDir = null!;

        /// <summary>
        /// Initialises the test dependencies and temp directory.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _FileSystem = new ExtendedFileSystemWrapper();
            _ConfigWriter = new ConfigWriter(
                _MockLogger.Object,
                _FileSystem);
            _TempDir = Path.Combine(
                Path.GetTempPath(),
                $"SBT_ConfigWriterTest_{Guid.NewGuid():N}");

            Directory.CreateDirectory(_TempDir);
        }

        /// <summary>
        /// Cleans up the temp directory.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_TempDir))
            {
                try
                {
                    Directory.Delete(
                        _TempDir,
                        true);
                }

                catch
                {

                }
            }
        }

        /// <summary>
        /// Checks that GenerateAppConfig returns a valid XDocument with a configuration root element.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_ReturnsValidXml_WithFullOptions()
        {
            InstallOptionsModel options = CreateTestOptions();

            XDocument config = _ConfigWriter.GenerateAppConfig(options);

            Assert.IsNotNull(config);
            Assert.IsNotNull(config.Root);
            Assert.AreEqual(
                "configuration",
                config.Root.Name.LocalName);
        }

        /// <summary>
        /// Checks that GenerateAppConfig includes the correct server details attributes.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_IncludesServerDetailsAttributes()
        {
            InstallOptionsModel options = CreateTestOptions();

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? serverDetails = config.Root?.Element("serverBackup")?
                .Element("serverDetails");

            Assert.IsNotNull(serverDetails);
            Assert.AreEqual(
                "TestServer",
                serverDetails.Attribute("name")?.Value);
            Assert.AreEqual(
                "Minecraft",
                serverDetails.Attribute("game")?.Value);
            Assert.AreEqual(
                @"C:\GameServer",
                serverDetails.Attribute("location")?.Value);
            Assert.AreEqual(
                "server.jar",
                serverDetails.Attribute("startFile")?.Value);
            Assert.AreEqual(
                "192.168.1.100",
                serverDetails.Attribute("ipAddress")?.Value);
        }

        /// <summary>
        /// Checks that GenerateAppConfig includes timer details with custom timers.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_IncludesTimerDetailsWithCustomTimers()
        {
            InstallOptionsModel options = CreateTestOptions();

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? timerDetails = config.Root?.Element("serverBackup")?
                .Element("timerDetails");

            Assert.IsNotNull(timerDetails);
            Assert.AreEqual(
                "03:00:00",
                timerDetails.Attribute("backupTime")?.Value);

            List<XElement> timers = timerDetails.Element("timers")?
                .Elements("timer")
                .ToList() ?? [];

            Assert.AreEqual(
                1,
                timers.Count);
        }

        /// <summary>
        /// Checks that GenerateAppConfig includes the log4net section with appender elements.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_IncludesLog4NetSection()
        {
            InstallOptionsModel options = CreateTestOptions();

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? log4Net = config.Root?.Element("log4net");

            Assert.IsNotNull(log4Net);

            List<XElement> appenders = [.. log4Net.Elements("appender")];

            Assert.IsTrue(appenders.Count > 0);
        }

        /// <summary>
        /// Checks that GenerateAppConfig omits email elements when EmailConfig is null.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_OmitsEmailElements_WhenEmailConfigNull()
        {
            InstallOptionsModel options = CreateTestOptions();
            options.EmailConfig = null;

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? notifications = config.Root?.Element("serverBackup")?
                .Element("notifications");

            Assert.IsNotNull(notifications);

            string? enabled = notifications.Attribute("enabled")?.Value;

            Assert.IsTrue(
                string.Equals(
                    enabled, "false",
                    StringComparison.OrdinalIgnoreCase),
                $"Expected 'false' but got '{enabled}'.");
        }

        /// <summary>
        /// Checks that GenerateAppConfig includes email elements when EmailConfig is provided.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_IncludesEmailElements_WhenEmailConfigProvided()
        {
            InstallOptionsModel options = CreateTestOptions();

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? notifications = config.Root?
                .Element("serverBackup")?
                .Element("notifications");

            Assert.IsNotNull(notifications);

            string? enabled = notifications.Attribute("enabled")?.Value;

            Assert.IsTrue(
                string.Equals(
                    enabled, "true",
                    StringComparison.OrdinalIgnoreCase),
                $"Expected 'true' but got '{enabled}'.");

            List<XElement> emails = notifications.Element("emails")?
                .Elements("email")
                .ToList() ?? [];

            Assert.IsTrue(emails.Count > 0);
        }

        /// <summary>
        /// Checks that GenerateApiAppSettings returns valid JSON with all expected sections.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_ReturnsValidJson_WithAllSettings()
        {
            InstallOptionsModel options = CreateTestOptions();

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig!,
                options.ServerConfig);

            Assert.IsFalse(string.IsNullOrWhiteSpace(json));

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            Assert.IsTrue(root.TryGetProperty(
                "Authentication",
                out _));
            Assert.IsTrue(root.TryGetProperty(
                "Database",
                out _));
            Assert.IsTrue(root.TryGetProperty(
                "Webhook",
                out _));
        }

        /// <summary>
        /// Checks that WriteConfig writes a valid XML file to disk.
        /// </summary>
        [TestMethod]
        public async Task WriteConfig_WritesFileToDisk_AndParsesAsValidXml()
        {
            InstallOptionsModel options = CreateTestOptions();
            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            string filePath = Path.Combine(
                _TempDir,
                "App.config");

            (bool success, Exception? error) = await _ConfigWriter.WriteConfig(
                filePath,
                config);

            Assert.IsTrue(success);
            Assert.IsNull(error);
            Assert.IsTrue(File.Exists(filePath));

            XDocument loaded = XDocument.Load(filePath);

            Assert.IsNotNull(loaded.Root);
            Assert.AreEqual(
                "configuration",
                loaded.Root.Name.LocalName);
        }

        /// <summary>
        /// Checks that WriteApiSettings writes a valid JSON file to disk.
        /// </summary>
        [TestMethod]
        public async Task WriteApiSettings_WritesFileToDisk_AndParsesAsValidJson()
        {
            InstallOptionsModel options = CreateTestOptions();
            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig!,
                options.ServerConfig);
            string filePath = Path.Combine(
                _TempDir,
                "appsettings.json");

            (bool written, Exception? error) = await _ConfigWriter.WriteApiSettings(
                filePath,
                json);

            Assert.IsTrue(written);
            Assert.IsNull(error);
            Assert.IsTrue(File.Exists(filePath));

            string loadedJson = File.ReadAllText(filePath);
            using JsonDocument document = JsonDocument.Parse(loadedJson);

            Assert.IsTrue(document.RootElement.TryGetProperty(
                "Authentication",
                out _));
            Assert.IsTrue(document.RootElement.TryGetProperty(
                "Database",
                out _));
            Assert.IsTrue(document.RootElement.TryGetProperty(
                "Webhook",
                out _));
            Assert.IsTrue(document.RootElement.TryGetProperty(
                "ArchiveSettings",
                out _));
        }

        /// <summary>
        /// Checks that the generated API settings contain the correct database values.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_ContainsCorrectDatabaseValues()
        {
            InstallOptionsModel options = CreateTestOptions();

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig!,
                options.ServerConfig);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement database = document.RootElement.GetProperty("Database");

            Assert.AreEqual(
                options.ApiConfig!.DatabasePath,
                database.GetProperty("Path")
                    .GetString());
            Assert.AreEqual(
                options.ServerConfig.ServerName,
                database.GetProperty("ServerName")
                    .GetString());
            Assert.AreEqual(
                options.ServerConfig.PollingIntervalMs,
                database.GetProperty("PollingIntervalMs")
                    .GetInt32());
        }

        /// <summary>
        /// Checks that the generated API settings contain the correct archive directory.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_ContainsCorrectArchiveDirectory()
        {
            InstallOptionsModel options = CreateTestOptions();
            options.ApiConfig!.ArchiveDirectory = "Custom Archives";

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig,
                options.ServerConfig);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement archiveSettings = document.RootElement.GetProperty("ArchiveSettings");

            Assert.AreEqual(
                "Custom Archives",
                archiveSettings.GetProperty("ArchiveDirectory")
                    .GetString());
        }

        /// <summary>
        /// Checks that the generated API settings contain the correct webhook values.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_ContainsCorrectWebhookValues()
        {
            InstallOptionsModel options = CreateTestOptions();

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig!,
                options.ServerConfig);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement webhook = document.RootElement.GetProperty("Webhook");

            Assert.AreEqual(
                options.ApiConfig!.WebhookSecret,
                webhook.GetProperty("Secret")
                    .GetString());
            Assert.AreEqual(
                10,
                webhook.GetProperty("TimeoutSeconds")
                    .GetInt32());
            Assert.AreEqual(
                3,
                webhook.GetProperty("MaxRetries")
                    .GetInt32());
        }

        /// <summary>
        /// Checks that the generated API settings contain the hashed credentials.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_ContainsHashedCredentials()
        {
            InstallOptionsModel options = CreateTestOptions();

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig!,
                options.ServerConfig);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement authentication = document.RootElement.GetProperty("Authentication");

            Assert.AreEqual(
                options.ApiConfig!.ClientIdHash,
                authentication.GetProperty("ClientId")
                    .GetString());
            Assert.AreEqual(
                options.ApiConfig.ClientSecretHash,
                authentication.GetProperty("ClientSecret")
                    .GetString());
        }

        /// <summary>
        /// Checks that API settings can be written and read back with values preserved.
        /// </summary>
        [TestMethod]
        public async Task WriteApiSettings_RoundTrip_PreservesAllValues()
        {
            InstallOptionsModel options = CreateTestOptions();
            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig!,
                options.ServerConfig);
            string filePath = Path.Combine(
                _TempDir,
                "appsettings.json");

            await _ConfigWriter.WriteApiSettings(
                filePath,
                json);

            string loadedJson = File.ReadAllText(filePath);
            using JsonDocument document = JsonDocument.Parse(loadedJson);
            JsonElement root = document.RootElement;

            Assert.AreEqual(
                "hashedId",
                root.GetProperty("Authentication")
                    .GetProperty("ClientId")
                    .GetString());
            Assert.AreEqual(
                "TestServer",
                root.GetProperty("Database")
                    .GetProperty("ServerName")
                    .GetString());
            Assert.AreEqual(
                "webhookKey",
                root.GetProperty("Webhook")
                    .GetProperty("Secret")
                    .GetString());
        }

        /// <summary>
        /// Checks that MigrateAppConfig adds a missing element from the reference config.
        /// </summary>
        [TestMethod]
        public void MigrateAppConfig_AddsMissingElement()
        {
            XDocument existing = new(
                new XElement("configuration",
                    new XElement("serverBackup",
                        new XElement("serverDetails",
                            new XAttribute("name", "MyServer"),
                            new XAttribute("game", "Minecraft")))));

            XDocument reference = new(
                new XElement("configuration",
                    new XElement("serverBackup",
                        new XElement("serverDetails",
                            new XAttribute("name", ""),
                            new XAttribute("game", "")),
                        new XElement("databaseDetails",
                            new XAttribute("path", ""),
                            new XAttribute("pollingInterval", "1000")))));

            (bool migrated, List<string> additions) = _ConfigWriter.MigrateAppConfig(
                existing,
                reference);

            Assert.IsTrue(migrated);
            Assert.IsTrue(additions.Count > 0);

            XElement? databaseDetails = existing.Root?.Element("serverBackup")?
                .Element("databaseDetails");

            Assert.IsNotNull(databaseDetails);
            Assert.AreEqual(
                "1000",
                databaseDetails.Attribute("pollingInterval")?.Value);
        }

        /// <summary>
        /// Checks that MigrateAppConfig adds a missing attribute to an existing element.
        /// </summary>
        [TestMethod]
        public void MigrateAppConfig_AddsMissingAttribute()
        {
            XDocument existing = new(
                new XElement("configuration",
                    new XElement("serverBackup",
                        new XElement("serverDetails",
                            new XAttribute("name", "MyServer")))));

            XDocument reference = new(
                new XElement("configuration",
                    new XElement("serverBackup",
                        new XElement("serverDetails",
                            new XAttribute("name", ""),
                            new XAttribute("game", ""),
                            new XAttribute("ipAddress", "")))));

            (bool migrated, List<string> additions) = _ConfigWriter.MigrateAppConfig(
                existing,
                reference);

            Assert.IsTrue(migrated);

            XElement? serverDetails = existing.Root?.Element("serverBackup")?
                .Element("serverDetails");

            Assert.IsNotNull(serverDetails);
            Assert.AreEqual(
                "MyServer",
                serverDetails.Attribute("name")?.Value);
            Assert.IsNotNull(serverDetails.Attribute("game"));
            Assert.IsNotNull(serverDetails.Attribute("ipAddress"));
        }

        /// <summary>
        /// Checks that MigrateAppConfig preserves existing values and does not overwrite them.
        /// </summary>
        [TestMethod]
        public void MigrateAppConfig_PreservesExistingValues()
        {
            InstallOptionsModel options = CreateTestOptions();
            XDocument reference = _ConfigWriter.GenerateAppConfig(options);

            XDocument existing = new(
                new XElement("configuration",
                    new XElement("serverBackup",
                        new XElement("serverDetails",
                            new XAttribute("name", "UserServer"),
                            new XAttribute("game", "Minecraft"),
                            new XAttribute("location", @"D:\Games\MC"),
                            new XAttribute("startFile", "start.bat"),
                            new XAttribute("ipAddress", "10.0.0.1")))));

            _ConfigWriter.MigrateAppConfig(
                existing,
                reference);

            XElement? serverDetails = existing.Root?.Element("serverBackup")?
                .Element("serverDetails");

            Assert.IsNotNull(serverDetails);
            Assert.AreEqual(
                "UserServer",
                serverDetails.Attribute("name")?.Value);
            Assert.AreEqual(
                "10.0.0.1",
                serverDetails.Attribute("ipAddress")?.Value);
        }

        /// <summary>
        /// Checks that MigrateAppConfig returns false when no changes are needed.
        /// </summary>
        [TestMethod]
        public void MigrateAppConfig_ReturnsFalse_WhenNoChangeNeeded()
        {
            InstallOptionsModel options = CreateTestOptions();
            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XDocument reference = XDocument.Parse(config.ToString());

            (bool migrated, List<string> additions) = _ConfigWriter.MigrateAppConfig(
                config,
                reference);

            Assert.IsFalse(migrated);
            Assert.AreEqual(
                0,
                additions.Count);
        }

        /// <summary>
        /// Checks that MigrateApiAppSettings adds a missing top-level section.
        /// </summary>
        [TestMethod]
        public void MigrateApiAppSettings_AddsMissingSection()
        {
            string existing = """{"Authentication":{"ClientId":"abc","ClientSecret":"def"}}""";
            string reference = """{"Authentication":{"ClientId":"","ClientSecret":""},"Webhook":{"Secret":"","TimeoutSeconds":10,"MaxRetries":3}}""";

            (bool migrated, List<string> additions) = _ConfigWriter.MigrateApiAppSettings(
                existing,
                reference);

            Assert.IsTrue(migrated);
            Assert.IsTrue(additions.Contains("Webhook"));
        }

        /// <summary>
        /// Checks that MigrateApiAppSettings adds a missing property within an existing section.
        /// </summary>
        [TestMethod]
        public void MigrateApiAppSettings_AddsMissingProperty()
        {
            string existing = """{"Database":{"Path":"test.db","ServerName":"TestServer"}}""";
            string reference = """{"Database":{"Path":"","ServerName":"","PollingIntervalMs":1000}}""";

            (bool migrated, List<string> additions) = _ConfigWriter.MigrateApiAppSettings(
                existing,
                reference);

            Assert.IsTrue(migrated);
            Assert.IsTrue(additions.Contains("Database.PollingIntervalMs"));
        }

        /// <summary>
        /// Checks that MigrateApiAppSettings preserves existing values.
        /// </summary>
        [TestMethod]
        public void MigrateApiAppSettings_PreservesExistingValues()
        {
            string existing = """{"Authentication":{"ClientId":"myHash","ClientSecret":"mySecret"},"Database":{"Path":"mydb.db","ServerName":"Production"}}""";
            string reference = """{"Authentication":{"ClientId":"","ClientSecret":""},"Database":{"Path":"","ServerName":"","PollingIntervalMs":1000}}""";

            (bool migrated, List<string> additions) = _ConfigWriter.MigrateApiAppSettings(
                existing,
                reference);

            Assert.IsTrue(migrated);

            using JsonDocument existingDoc = JsonDocument.Parse(existing);

            Assert.AreEqual(
                "myHash",
                existingDoc.RootElement.GetProperty("Authentication")
                    .GetProperty("ClientId")
                    .GetString());
            Assert.AreEqual(
                "Production",
                existingDoc.RootElement.GetProperty("Database")
                    .GetProperty("ServerName")
                    .GetString());
        }

        /// <summary>
        /// Checks that MigrateApiAppSettings returns false when no changes are needed.
        /// </summary>
        [TestMethod]
        public void MigrateApiAppSettings_ReturnsFalse_WhenNoChangeNeeded()
        {
            InstallOptionsModel options = CreateTestOptions();
            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig!,
                options.ServerConfig);

            (bool migrated, List<string> additions) = _ConfigWriter.MigrateApiAppSettings(
                json,
                json);

            Assert.IsFalse(migrated);
            Assert.AreEqual(
                0,
                additions.Count);
        }

        /// <summary>
        /// Checks that the generated API settings contain Kestrel HTTP endpoint configuration.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_ContainsKestrelHttpEndpoint()
        {
            InstallOptionsModel options = CreateTestOptions();

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig!,
                options.ServerConfig);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement kestrel = document.RootElement.GetProperty("Kestrel");
            JsonElement endpoints = kestrel.GetProperty("Endpoints");
            JsonElement http = endpoints.GetProperty("Http");

            Assert.AreEqual(
                "http://0.0.0.0:5000",
                http.GetProperty("Url")
                    .GetString());
        }

        /// <summary>
        /// Checks that the generated API settings contain Kestrel HTTPS endpoint with PFX certificate when enabled.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_ContainsKestrelHttpsEndpoint_WhenEnabledWithPfx()
        {
            InstallOptionsModel options = CreateTestOptions();
            options.ApiConfig!.EnableHttps = true;
            options.ApiConfig.HttpsPort = 5001;
            options.ApiConfig.CertificateFormat = "PFX";
            options.ApiConfig.CertificatePath = @"C:\certs\api.pfx";
            options.ApiConfig.CertificatePassword = "certpass";

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig,
                options.ServerConfig);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement endpoints = document.RootElement.GetProperty("Kestrel")
                .GetProperty("Endpoints");

            Assert.IsTrue(endpoints.TryGetProperty(
                "Https",
                out JsonElement https));
            Assert.AreEqual(
                "https://0.0.0.0:5001",
                https.GetProperty("Url")
                    .GetString());
            Assert.AreEqual(
                @"C:\certs\api.pfx",
                https.GetProperty("Certificate")
                    .GetProperty("Path")
                    .GetString());
            Assert.AreEqual(
                "certpass",
                https.GetProperty("Certificate")
                    .GetProperty("Password")
                    .GetString());
        }

        /// <summary>
        /// Checks that the generated API settings contain Kestrel HTTPS endpoint with PEM certificate when enabled.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_ContainsKestrelHttpsEndpoint_WhenEnabledWithPem()
        {
            InstallOptionsModel options = CreateTestOptions();
            options.ApiConfig!.EnableHttps = true;
            options.ApiConfig.HttpsPort = 5001;
            options.ApiConfig.CertificateFormat = "PEM";
            options.ApiConfig.CertificatePath = @"C:\certs\api.pem";
            options.ApiConfig.CertificateKeyPath = @"C:\certs\api-key.pem";

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig,
                options.ServerConfig);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement endpoints = document.RootElement.GetProperty("Kestrel")
                .GetProperty("Endpoints");

            Assert.IsTrue(endpoints.TryGetProperty(
                "Https",
                out JsonElement https));
            Assert.AreEqual(
                "https://0.0.0.0:5001",
                https.GetProperty("Url")
                    .GetString());
            Assert.AreEqual(
                @"C:\certs\api.pem",
                https.GetProperty("Certificate")
                    .GetProperty("Path")
                    .GetString());
            Assert.AreEqual(
                @"C:\certs\api-key.pem",
                https.GetProperty("Certificate")
                    .GetProperty("KeyPath")
                    .GetString());
            Assert.IsFalse(https.GetProperty("Certificate")
                .TryGetProperty(
                    "Password",
                    out _));
        }

        /// <summary>
        /// Checks that the generated API settings do not contain HTTPS endpoint when disabled.
        /// </summary>
        [TestMethod]
        public void GenerateApiAppSettings_OmitsHttpsEndpoint_WhenDisabled()
        {
            InstallOptionsModel options = CreateTestOptions();
            options.ApiConfig!.EnableHttps = false;

            string json = _ConfigWriter.GenerateApiAppSettings(
                options.ApiConfig,
                options.ServerConfig);

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement endpoints = document.RootElement.GetProperty("Kestrel")
                .GetProperty("Endpoints");

            Assert.IsFalse(endpoints.TryGetProperty(
                "Https",
                out _));
        }

        /// <summary>
        /// Checks that GenerateAppConfig includes the username attribute on the provider element.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_IncludesUsernameAttributeOnProvider()
        {
            InstallOptionsModel options = CreateTestOptions();

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? provider = config.Root?.Element("serverBackup")?
                .Element("notifications")?
                .Element("provider");

            Assert.IsNotNull(provider);
            Assert.AreEqual(
                "auth@test.com",
                provider.Attribute("username")?.Value);
        }

        /// <summary>
        /// Checks that GenerateAppConfig writes empty username attribute when EmailConfig is null.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_WritesEmptyUsernameAttribute_WhenEmailConfigNull()
        {
            InstallOptionsModel options = CreateTestOptions();
            options.EmailConfig = null;

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? provider = config.Root?.Element("serverBackup")?
                .Element("notifications")?
                .Element("provider");

            Assert.IsNotNull(provider);
            Assert.AreEqual(
                string.Empty,
                provider.Attribute("username")?.Value);
        }

        /// <summary>
        /// Checks that GenerateAppConfig includes image elements in the email template.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_IncludesImagesInEmailTemplate()
        {
            InstallOptionsModel options = CreateTestOptions();
            options.EmailConfig!.Emails[0].Images =
            [
                new ImageModel { Key = "logo", Path = @"C:\images\logo.png" },
                new ImageModel { Key = "banner", Path = @"C:\images\banner.png" }
            ];

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? email = config.Root?.Element("serverBackup")?
                .Element("notifications")?
                .Element("emails")?
                .Element("email");

            Assert.IsNotNull(email);

            List<XElement> images = email.Element("images")?
                .Elements("image")
                .ToList() ?? [];

            Assert.AreEqual(
                2,
                images.Count);
            Assert.AreEqual(
                "logo",
                images[0].Attribute("key")?.Value);
            Assert.AreEqual(
                @"C:\images\logo.png",
                images[0].Attribute("path")?.Value);
            Assert.AreEqual(
                "banner",
                images[1].Attribute("key")?.Value);
            Assert.AreEqual(
                @"C:\images\banner.png",
                images[1].Attribute("path")?.Value);
        }

        /// <summary>
        /// Checks that GenerateAppConfig includes the correct timer count attribute.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_IncludesCorrectTimerCount()
        {
            InstallOptionsModel options = CreateTestOptions();

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? timerDetails = config.Root?.Element("serverBackup")?
                .Element("timerDetails");

            Assert.IsNotNull(timerDetails);

            int expectedCount = options.TimerConfig.CustomTimers.Count + 1;

            Assert.AreEqual(
                expectedCount.ToString(),
                timerDetails.Attribute("count")?.Value);
        }

        /// <summary>
        /// Checks that GenerateAppConfig includes email recipients with correct attributes.
        /// </summary>
        [TestMethod]
        public void GenerateAppConfig_IncludesEmailRecipients()
        {
            InstallOptionsModel options = CreateTestOptions();
            options.EmailConfig!.Emails[0].Recipients =
            [
                new RecipientModel { Email = "admin@test.com", Name = "Admin" },
                new RecipientModel { Email = "ops@test.com", Name = "Ops" }
            ];

            XDocument config = _ConfigWriter.GenerateAppConfig(options);
            XElement? email = config.Root?.Element("serverBackup")?
                .Element("notifications")?
                .Element("emails")?
                .Element("email");

            Assert.IsNotNull(email);

            List<XElement> addresses = email.Element("addresses")?
                .Elements("toAddress")
                .ToList() ?? [];

            Assert.AreEqual(
                2,
                addresses.Count);
            Assert.AreEqual(
                "admin@test.com",
                addresses[0].Attribute("email")?.Value);
            Assert.AreEqual(
                "Admin",
                addresses[0].Attribute("name")?.Value);
            Assert.AreEqual(
                "ops@test.com",
                addresses[1].Attribute("email")?.Value);
            Assert.AreEqual(
                "Ops",
                addresses[1].Attribute("name")?.Value);
        }

        /// <summary>
        /// Checks that MigrateAppConfig handles multiple siblings with the same tag name correctly.
        /// </summary>
        [TestMethod]
        public void MigrateAppConfig_HandlesMultipleSiblingsWithSameTagName()
        {
            XDocument existing = new(
                new XElement("configuration",
                    new XElement("configSections",
                        new XElement("section",
                            new XAttribute("name", "log4net"),
                            new XAttribute("type", "log4net.Config.Log4NetConfigurationSectionHandler,log4net")),
                        new XElement("section",
                            new XAttribute("name", "serverBackup"),
                            new XAttribute("type", "ServerBackupTool.Models.Configuration.SBTSection, ServerBackupTool")))));

            XDocument reference = new(
                new XElement("configuration",
                    new XElement("configSections",
                        new XElement("section",
                            new XAttribute("name", "log4net"),
                            new XAttribute("type", "log4net.Config.Log4NetConfigurationSectionHandler,log4net")),
                        new XElement("section",
                            new XAttribute("name", "serverBackup"),
                            new XAttribute("type", "ServerBackupTool.Models.Configuration.SBTSection, ServerBackupTool")),
                        new XElement("section",
                            new XAttribute("name", "newSection"),
                            new XAttribute("type", "Some.New.Handler,SomeAssembly")))));

            (bool migrated, List<string> additions) = _ConfigWriter.MigrateAppConfig(
                existing,
                reference);

            Assert.IsTrue(migrated);
            Assert.IsTrue(additions.Count > 0);

            List<XElement> sections = existing.Root?.Element("configSections")?
                .Elements("section")
                .ToList() ?? [];

            Assert.AreEqual(
                3,
                sections.Count);

            XElement? log4NetSection = sections.FirstOrDefault(s =>
                s.Attribute("name")?.Value == "log4net");

            Assert.IsNotNull(log4NetSection);
            Assert.AreEqual(
                "log4net.Config.Log4NetConfigurationSectionHandler,log4net",
                log4NetSection.Attribute("type")?.Value);

            XElement? serverBackupSection = sections.FirstOrDefault(s =>
                s.Attribute("name")?.Value == "serverBackup");

            Assert.IsNotNull(serverBackupSection);
            Assert.AreEqual(
                "ServerBackupTool.Models.Configuration.SBTSection, ServerBackupTool",
                serverBackupSection.Attribute("type")?.Value);

            XElement? newSection = sections.FirstOrDefault(s =>
                s.Attribute("name")?.Value == "newSection");

            Assert.IsNotNull(newSection);
            Assert.AreEqual(
                "Some.New.Handler,SomeAssembly",
                newSection.Attribute("type")?.Value);
        }

        /// <summary>
        /// Creates a fully populated InstallOptionsModel for testing.
        /// </summary>
        private static InstallOptionsModel CreateTestOptions()
        {
            return new InstallOptionsModel
            {
                Components = ["Server Backup Tool", "Server Backup Tool API"],
                InstallPath = @"C:\Test\SBT",
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = "Server Backup Tool API - TestServer",
                ServerConfig = new ServerConfigModel
                {
                    ServerName = "TestServer",
                    Game = "Minecraft",
                    ServerDirectory = @"C:\GameServer",
                    StartFile = "server.jar",
                    IPAddress = "192.168.1.100",
                    DatabasePath = @"C:\Test\SBT\ServerBackupTool.db",
                    PollingIntervalMs = 1000
                },
                TimerConfig = new TimerConfigModel
                {
                    BackupTime = "03:00:00",
                    CustomTimers = [new CustomTimerModel { Name = "Warning", Time = "02:30:00", Message = "Backup in 30 minutes" }]
                },
                EmailConfig = new EmailConfigModel
                {
                    Enabled = true,
                    Port = 587,
                    EnableSSL = true,
                    SmtpHost = "smtp.test.com",
                    SmtpUsername = "auth@test.com",
                    SmtpPassword = "password",
                    FromEmail = "noreply@test.com",
                    FromName = "SBT",
                    Emails = [new EmailTemplateModel
                    {
                        Trigger = "Open",
                        IsSystem = true,
                        Recipients = [new RecipientModel { Email = "admin@test.com", Name = "Admin" }],
                        Subject = "Server Started",
                        Content = "The server has started."
                    }]
                },
                ApiConfig = new ApiConfigModel
                {
                    BindAddress = "0.0.0.0",
                    HttpPort = 5000,
                    DatabasePath = @"C:\Test\SBT\ServerBackupTool.db",
                    ClientId = "plainId",
                    ClientSecret = "plainSecret",
                    ClientIdHash = "hashedId",
                    ClientSecretHash = "hashedSecret",
                    WebhookSecret = "webhookKey"
                }
            };
        }
    }
}
