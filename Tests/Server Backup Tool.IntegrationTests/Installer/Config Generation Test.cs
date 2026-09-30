// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using System.Xml.Linq;

namespace ServerBackupTool.IntegrationTests.Installer
{
    [TestClass]
    public class ConfigGenerationTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private IExtendedFileSystem _FileSystem = null!;
        private ConfigWriter _ConfigWriter = null!;
        private string _TempDir = null!;
        private string _ConfigPath = null!;

        /// <summary>
        /// Initialises the test dependencies, generates a config, and writes it to a temp file.
        /// </summary>
        [TestInitialize]
        public async Task TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _FileSystem = new ExtendedFileSystemWrapper();
            _ConfigWriter = new ConfigWriter(
                _MockLogger.Object,
                _FileSystem);

            _TempDir = Path.Combine(
                Path.GetTempPath(),
                $"SBT_ConfigGenTest_{Guid.NewGuid():N}");

            Directory.CreateDirectory(_TempDir);

            _ConfigPath = Path.Combine(
                _TempDir,
                "App.config");

            InstallOptionsModel options = CreateTestOptions();
            XDocument config = _ConfigWriter.GenerateAppConfig(options);

            await _ConfigWriter.WriteConfig(
                _ConfigPath,
                config);
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
        /// Checks that the generated config file loads successfully with an SBT section.
        /// </summary>
        [TestMethod]
        public void GeneratedConfig_LoadsSuccessfully_WithSBTSection()
        {
            XDocument loaded = XDocument.Load(_ConfigPath);

            Assert.IsNotNull(loaded);
            Assert.IsNotNull(loaded.Root);
            Assert.AreEqual(
                "configuration",
                loaded.Root.Name.LocalName);

            XElement? serverBackup = loaded.Root.Element("serverBackup");

            Assert.IsNotNull(serverBackup);
        }

        /// <summary>
        /// Checks that the generated config contains the correct server details.
        /// </summary>
        [TestMethod]
        public void GeneratedConfig_ContainsCorrectServerDetails()
        {
            XDocument loaded = XDocument.Load(_ConfigPath);
            XElement? serverDetails = loaded.Root?.Element("serverBackup")?
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
        /// Checks that the generated config contains the correct timer details.
        /// </summary>
        [TestMethod]
        public void GeneratedConfig_ContainsCorrectTimerDetails()
        {
            XDocument loaded = XDocument.Load(_ConfigPath);
            XElement? timerDetails = loaded.Root?.Element("serverBackup")?
                .Element("timerDetails");

            Assert.IsNotNull(timerDetails);
            Assert.AreEqual(
                "03:00:00",
                timerDetails.Attribute("backupTime")?.Value);
            Assert.AreEqual(
                "2",
                timerDetails.Attribute("count")?.Value);

            List<XElement> timers = timerDetails.Element("timers")?
                .Elements("timer")
                .ToList() ?? [];

            Assert.AreEqual(1, timers.Count);
            Assert.AreEqual(
                "Warning",
                timers[0].Attribute("name")?.Value);
            Assert.AreEqual(
                "02:30:00",
                timers[0].Attribute("time")?.Value);
        }

        /// <summary>
        /// Checks that the generated config contains the correct notification details.
        /// </summary>
        [TestMethod]
        public void GeneratedConfig_ContainsCorrectNotificationDetails()
        {
            XDocument loaded = XDocument.Load(_ConfigPath);
            XElement? notifications = loaded.Root?.Element("serverBackup")?
                .Element("notifications");

            Assert.IsNotNull(notifications);

            string? enabled = notifications.Attribute("enabled")?.Value;

            Assert.IsTrue(
                string.Equals(
                    enabled, "true",
                    StringComparison.OrdinalIgnoreCase),
                $"Expected 'true' but got '{enabled}'.");

            XElement? provider = notifications.Element("provider");

            Assert.IsNotNull(provider);
            Assert.AreEqual(
                "smtp.test.com",
                provider.Attribute("name")?.Value);

            XElement? fromAddress = notifications.Element("fromAddress");

            Assert.IsNotNull(fromAddress);
            Assert.AreEqual(
                "noreply@test.com",
                fromAddress.Attribute("email")?.Value);

            List<XElement> emails = notifications.Element("emails")?
                .Elements("email")
                .ToList() ?? [];

            Assert.IsTrue(emails.Count > 0);
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
