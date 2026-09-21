// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Modes;
using ServerBackupTool.Installer.Values;
using Spectre.Console.Testing;
using System.Text.Json;
using System.Xml.Linq;

namespace ServerBackupTool.IntegrationTests.Installer.Modes
{
    [TestClass]
    public class ConfigureModeTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;
        private Mock<IConfigWriter> _MockConfigWriter = null!;
        private Mock<IVersionService> _MockVersionService = null!;
        private string _TempDir = null!;

        private const string TestConfigXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <section name=""serverBackup"" type=""ServerBackupTool.Models.Configuration.SBTSection, ServerBackupTool"" />
  </configSections>
  <serverBackup>
    <serverDetails name=""TestServer"" game=""Minecraft"" location=""C:\GameServer"" startFile=""start.bat"" ipAddress=""127.0.0.1"" />
    <databaseDetails path=""C:\ProgramData\Data.db"" pollingInterval=""1000"" />
    <timerDetails count=""1"" backupTime=""03:00:00"">
      <timers />
    </timerDetails>
    <notifications enabled=""false"">
      <provider name="""" password="""" />
      <fromAddress email="""" name="""" />
      <emails />
    </notifications>
  </serverBackup>
</configuration>";

        private const string TestApiSettingsJson = @"{
  ""Database"": {
    ""Path"": ""C:\\ProgramData\\Data.db"",
    ""ServerName"": ""TestServer"",
    ""PollingIntervalMs"": 1000
  },
  ""ArchiveSettings"": {
    ""ArchiveDirectory"": ""Archived Logs""
  },
  ""Authentication"": {
    ""ClientId"": ""abc123"",
    ""ClientSecret"": ""secret456""
  },
  ""Webhook"": {
    ""Secret"": ""webhooksecret"",
    ""TimeoutSeconds"": 10,
    ""MaxRetries"": 3
  },
  ""ConnectionStrings"": {},
  ""Kestrel"": {}
}";

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockFileSystem = new Mock<IExtendedFileSystem>();
            _MockConfigWriter = new Mock<IConfigWriter>();
            _MockVersionService = new Mock<IVersionService>();

            _TempDir = Path.Combine(
                Path.GetTempPath(),
                $"SBT_ConfigureModeTest_{Guid.NewGuid():N}");

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

        private const string RichConfigXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <section name=""serverBackup"" type=""ServerBackupTool.Models.Configuration.SBTSection, ServerBackupTool"" />
  </configSections>
  <serverBackup>
    <serverDetails name=""TestServer"" game=""Minecraft"" location=""C:\GameServer"" startFile=""start.bat"" ipAddress=""127.0.0.1"" />
    <databaseDetails path=""C:\ProgramData\Data.db"" pollingInterval=""1000"" />
    <timerDetails count=""2"" backupTime=""03:00:00"">
      <timers>
        <timer name=""Restart"" time=""12:00:00"" message=""Server restarting"" />
      </timers>
    </timerDetails>
    <notifications enabled=""true"">
      <provider name=""smtp.test.com"" password=""pass123"" />
      <fromAddress email=""admin@test.com"" name=""Server Backup Tool"" />
      <emails>
        <email trigger=""Open"" system=""true"">
          <addresses>
            <toAddress email=""admin@test.com"" name=""Admin"" />
          </addresses>
          <subject value=""Server Started"" />
          <content value=""The server has started."" />
          <images>
            <image key=""logo"" path=""C:\logo.png"" />
          </images>
        </email>
      </emails>
    </notifications>
  </serverBackup>
</configuration>";

        private VersionInfoModel SingleInstallation => new()
        {
            ServerName = "TestServer",
            ToolVersion = "1.0.0",
            InstallPath = _TempDir,
            ToolTaskName = "Server Backup Tool - TestServer"
        };

        private async Task<string> SetupConfigFile(string xml = "")
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                string.IsNullOrEmpty(xml) ? TestConfigXml : xml);

            return configPath;
        }

        private void SetupSingleInstallation(VersionInfoModel? model = null)
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns([model ?? SingleInstallation]);

            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
        }

        private ConfigureMode CreateMode(TestConsole console)
        {
            return new ConfigureMode(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);
        }

        private static void NavigateMainMenu(
            TestConsole console,
            int index)
        {
            for (int i = 0; i < index; i++)
            {
                console.Input.PushKey(ConsoleKey.DownArrow);
            }

            console.Input.PushKey(ConsoleKey.Enter);
        }

        /// <summary>
        /// Checks that Execute shows an error when no installation is found.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsError_WhenNoInstallationFound()
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns([]);

            TestConsole console = new();
            console.Interactive();

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            Assert.IsTrue(
                console.Output.Contains("No existing installation found"),
                $"Expected output to contain 'No existing installation found' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Exit Without Saving does not write any configuration files.
        /// </summary>
        [TestMethod]
        public async Task Execute_ExitWithoutSaving_DoesNotWriteConfig()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                TestConfigXml);

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);

            TestConsole console = new();
            console.Interactive();

            // Menu choices (no API): Server Details, Backup and Timers, Email Notifications,
            // Database Settings, Save and Exit, Exit Without Saving.
            // Navigate to "Exit Without Saving" (index 5) and press Enter.
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            _MockConfigWriter.Verify(
                w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()),
                Times.Never());

            _MockConfigWriter.Verify(
                w => w.WriteApiSettings(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that editing server details and saving calls the config writer.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditServerDetailsAndSave_WritesConfig()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                TestConfigXml);

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            _MockConfigWriter
                .Setup(w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()))
                .ReturnsAsync((true, (Exception?)null));

            TestConsole console = new();
            console.Interactive();

            // Menu: Select "Server Details" (first item — Enter).
            console.Input.PushKey(ConsoleKey.Enter);

            // EditServerDetails prompts for 5 text values with defaults.
            console.Input.PushTextWithEnter("MyNewServer");
            console.Input.PushTextWithEnter("Minecraft");
            console.Input.PushTextWithEnter(@"C:\GameServer");
            console.Input.PushTextWithEnter("start.bat");
            console.Input.PushTextWithEnter("192.168.1.50");

            // Menu: Select "Save and Exit" (index 4).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // Confirm save.
            console.Input.PushTextWithEnter("y");

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            _MockConfigWriter.Verify(
                w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()),
                Times.Once());
        }

        /// <summary>
        /// Checks that editing timer details and exiting without saving does not write config.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditTimerDetailsAndExitWithoutSaving_DoesNotWriteConfig()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                TestConfigXml);

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);

            TestConsole console = new();
            console.Interactive();

            // Menu: Select "Backup and Timers" (index 1).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // Timer sub-menu: Select "Edit Backup Time" (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Enter new backup time.
            console.Input.PushTextWithEnter("04:00:00");

            // Timer sub-menu: Select "Back" (index 3).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // Main menu: Select "Exit Without Saving" (index 5).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            _MockConfigWriter.Verify(
                w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()),
                Times.Never());

            _MockConfigWriter.Verify(
                w => w.WriteApiSettings(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that editing email notifications and saving calls the config writer.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditEmailNotificationsAndSave_WritesConfig()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                TestConfigXml);

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            _MockConfigWriter
                .Setup(w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()))
                .ReturnsAsync((true, (Exception?)null));

            TestConsole console = new();
            console.Interactive();

            // Main menu: Select "Email Notifications" (index 2).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // Email sub-menu: Select "Toggle Enabled" (index 0).
            console.Input.PushKey(ConsoleKey.Enter);

            // ConfirmationPrompt "Enable email notifications?".
            console.Input.PushTextWithEnter("y");

            // Email sub-menu: Select "Edit SMTP Settings" (index 1).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // TextPrompt "SMTP host:".
            console.Input.PushTextWithEnter("smtp.test.com");

            // TextPrompt "SMTP authentication username:".
            console.Input.PushTextWithEnter("auth@test.com");

            // TextPrompt "SMTP password:" (secret).
            console.Input.PushTextWithEnter("password123");

            // Email sub-menu: Select "Add Email Templates" (index 3).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // Trigger type selection: "Custom (triggered by server output text)" (index 3).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // TextPrompt "Enter the server output text to match:".
            console.Input.PushTextWithEnter("crash detected");

            // TextPrompt "Enter the subject:".
            console.Input.PushTextWithEnter("Server Crashed");

            // TextPrompt "Enter the content (HTML or path to .html file):".
            console.Input.PushTextWithEnter("<h1>Server has crashed</h1>");

            // TextPrompt "Enter the recipient email:".
            console.Input.PushTextWithEnter("admin@test.com");

            // TextPrompt "Enter the recipient name:".
            console.Input.PushTextWithEnter("Admin");

            // ConfirmationPrompt "Add another recipient?".
            console.Input.PushTextWithEnter("n");

            // ConfirmationPrompt "Add an inline image?".
            console.Input.PushTextWithEnter("n");

            // Email sub-menu: Select "Back" (index 6).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // Main menu: Select "Save and Exit" (index 4).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // ConfirmationPrompt "Save these changes?".
            console.Input.PushTextWithEnter("y");

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            _MockConfigWriter.Verify(
                w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()),
                Times.Once());
        }

        /// <summary>
        /// Checks that editing database settings and exiting without saving does not write config.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditDatabaseSettingsAndExitWithoutSaving_DoesNotWriteConfig()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                TestConfigXml);

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);

            TestConsole console = new();
            console.Interactive();

            // Main menu: Select "Database Settings" (index 3).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // TextPrompt "Database path:".
            console.Input.PushTextWithEnter(@"D:\NewData.db");

            // TextPrompt "Polling interval (ms):".
            console.Input.PushTextWithEnter("2000");

            // Main menu: Select "Exit Without Saving" (index 5).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            _MockConfigWriter.Verify(
                w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()),
                Times.Never());

            _MockConfigWriter.Verify(
                w => w.WriteApiSettings(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that editing API database settings navigates the API sub-menu correctly.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditApiDatabaseSettings_NavigatesApiSubMenu()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            string apiDir = Path.Combine(
                _TempDir,
                "Api");

            Directory.CreateDirectory(apiDir);

            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");

            await File.WriteAllTextAsync(
                configPath,
                TestConfigXml);
            await File.WriteAllTextAsync(
                apiSettingsPath,
                TestApiSettingsJson);

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ApiInstallPath = apiDir,
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync(TestApiSettingsJson);

            TestConsole console = new();
            console.Interactive();

            // Main menu (with API): Server Details, Backup and Timers, Email Notifications,
            // Database Settings, API Settings, Save and Exit, Exit Without Saving.
            // Select "API Settings" (index 4).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // API sub-menu: Select "Database" (index 0).
            console.Input.PushKey(ConsoleKey.Enter);

            // TextPrompt "Database path:".
            console.Input.PushTextWithEnter(@"D:\ApiData.db");

            // TextPrompt "Server name:".
            console.Input.PushTextWithEnter("NewApiServer");

            // TextPrompt "Polling interval (ms):".
            console.Input.PushTextWithEnter("2000");

            // Main menu (with API): Select "Exit Without Saving" (index 6).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            _MockConfigWriter.Verify(
                w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()),
                Times.Never());

            _MockConfigWriter.Verify(
                w => w.WriteApiSettings(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that saving after editing server details displays the change summary table.
        /// </summary>
        [TestMethod]
        public async Task Execute_SaveAndExitWithChanges_DisplaysChangeSummary()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                TestConfigXml);

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            _MockConfigWriter
                .Setup(w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()))
                .ReturnsAsync((true, (Exception?)null));

            TestConsole console = new();
            console.Interactive();

            // Main menu: Select "Server Details" (index 0).
            console.Input.PushKey(ConsoleKey.Enter);

            // EditServerDetails: Change server name, keep other defaults.
            console.Input.PushTextWithEnter("RenamedServer");
            console.Input.PushTextWithEnter("Minecraft");
            console.Input.PushTextWithEnter(@"C:\GameServer");
            console.Input.PushTextWithEnter("start.bat");
            console.Input.PushTextWithEnter("127.0.0.1");

            // Main menu: Select "Save and Exit" (index 4).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // ConfirmationPrompt "Save these changes?".
            console.Input.PushTextWithEnter("y");

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            string output = console.Output;

            Assert.IsTrue(
                output.Contains("Changes to Save"),
                $"Expected output to contain 'Changes to Save' but got: {output}");

            Assert.IsTrue(
                output.Contains("TestServer"),
                $"Expected output to contain 'TestServer' (original value) but got: {output}");

            Assert.IsTrue(
                output.Contains("RenamedServer"),
                $"Expected output to contain 'RenamedServer' (new value) but got: {output}");

            _MockConfigWriter.Verify(
                w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()),
                Times.Once());
        }

        /// <summary>
        /// Checks that editing timer details allows adding a custom timer and editing backup time.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditTimerDetailsAddCustomTimer_UpdatesTimerConfig()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                TestConfigXml);

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);

            TestConsole console = new();
            console.Interactive();

            // Main menu: Select "Backup and Timers" (index 1).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // Timer sub-menu: Select "Edit Backup Time" (index 0).
            console.Input.PushKey(ConsoleKey.Enter);

            // TextPrompt "Backup time (HH:mm:ss):".
            console.Input.PushTextWithEnter("05:30:00");

            // Timer sub-menu: Select "Add Custom Timer" (index 1).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // TextPrompt "Enter the timer name:".
            console.Input.PushTextWithEnter("Restart");

            // TextPrompt "Enter the timer time (HH:mm:ss):".
            console.Input.PushTextWithEnter("12:00:00");

            // TextPrompt "Enter the timer message:".
            console.Input.PushTextWithEnter("Server restarting");

            // Timer sub-menu: Select "Back" (index 3).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            // Main menu: Select "Exit Without Saving" (index 5).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            string output = console.Output;

            Assert.IsTrue(
                output.Contains("Timer 'Restart' added"),
                $"Expected output to contain timer added confirmation but got: {output}");
        }

        /// <summary>
        /// Checks that Execute shows an error when the config file is not found.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsError_WhenConfigFileNotFound()
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns([SingleInstallation]);

            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(false);

            TestConsole console = new();
            console.Interactive();

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Configuration file not found"),
                $"Expected 'Configuration file not found' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute handles a corrupt configuration file gracefully.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsError_WhenConfigFileIsCorrupt()
        {
            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            await File.WriteAllTextAsync(
                configPath,
                "NOT VALID XML <><>");

            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Failed to load configuration"),
                $"Expected 'Failed to load configuration' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute prompts for selection when multiple installations exist.
        /// </summary>
        [TestMethod]
        public async Task Execute_PromptsForSelection_WhenMultipleInstallationsExist()
        {
            await SetupConfigFile();

            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "Server1",
                        ToolVersion = "1.0.0",
                        InstallPath = _TempDir,
                        ToolTaskName = "Server Backup Tool - Server1"
                    },
                    new VersionInfoModel
                    {
                        ServerName = "Server2",
                        ToolVersion = "2.0.0",
                        InstallPath = @"C:\Other",
                        ToolTaskName = "Server Backup Tool - Server2"
                    }
                ]);

            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);

            TestConsole console = new();
            console.Interactive();

            // Select first installation.
            console.Input.PushKey(ConsoleKey.Enter);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Multiple installations found"),
                $"Expected 'Multiple installations found' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that removing a custom timer updates the timer config.
        /// </summary>
        [TestMethod]
        public async Task Execute_RemoveCustomTimer_ShowsTimerRemoved()
        {
            await SetupConfigFile(RichConfigXml);
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Backup and Timers (index 1).
            NavigateMainMenu(
                console,
                1);

            // Remove Custom Timer (index 2).
            NavigateMainMenu(
                console,
                2);

            // Select the timer to remove (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Back (index 3).
            NavigateMainMenu(
                console,
                3);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Timer removed"),
                $"Expected 'Timer removed' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that removing a custom timer when none exist shows a warning.
        /// </summary>
        [TestMethod]
        public async Task Execute_RemoveCustomTimer_ShowsWarning_WhenNoneExist()
        {
            await SetupConfigFile();
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Backup and Timers (index 1).
            NavigateMainMenu(
                console,
                1);

            // Remove Custom Timer (index 2).
            NavigateMainMenu(
                console,
                2);

            // Back (index 3).
            NavigateMainMenu(
                console,
                3);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("No custom timers to remove"),
                $"Expected 'No custom timers to remove' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Edit From Address updates the from address values.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditFromAddress_UpdatesValues()
        {
            await SetupConfigFile();
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Edit From Address (index 2).
            NavigateMainMenu(
                console,
                2);

            // From email.
            console.Input.PushTextWithEnter("noreply@test.com");

            // From name.
            console.Input.PushTextWithEnter("Backup Service");

            // Back (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            _MockLogger.Verify(
                l => l.LogMessage(It.IsAny<string>(), "Email notification settings updated."),
                Times.Once());
        }

        /// <summary>
        /// Checks that editing an existing email template exercises EditEmailTemplate.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditEmailTemplate_EditsSubjectAndContent()
        {
            await SetupConfigFile(RichConfigXml);
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Edit Email Templates (index 4).
            NavigateMainMenu(
                console,
                4);

            // Select the template (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Edit Subject (index 0).
            console.Input.PushKey(ConsoleKey.Enter);

            // New subject.
            console.Input.PushTextWithEnter("Updated Subject");

            // Edit Content (index 1).
            NavigateMainMenu(console, 1);

            // New content.
            console.Input.PushTextWithEnter("<h1>Updated</h1>");

            // Back (index 5).
            NavigateMainMenu(
                console,
                5);

            // Back from email menu (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            _MockLogger.Verify(
                l => l.LogMessage(It.IsAny<string>(), "Email template updated."),
                Times.Once());
        }

        /// <summary>
        /// Checks that Edit Email Templates shows a warning when no templates exist.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditEmailTemplates_ShowsWarning_WhenNoneExist()
        {
            await SetupConfigFile();
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Edit Email Templates (index 4).
            NavigateMainMenu(
                console,
                4);

            // Back (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("No email templates to edit"),
                $"Expected 'No email templates to edit' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that removing an email template shows the removed confirmation.
        /// </summary>
        [TestMethod]
        public async Task Execute_RemoveEmailTemplate_ShowsRemoved()
        {
            await SetupConfigFile(RichConfigXml);
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Remove Email Templates (index 5).
            NavigateMainMenu(
                console,
                5);

            // Select template to remove (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Back (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Email template removed"),
                $"Expected 'Email template removed' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that removing email templates shows a warning when none exist.
        /// </summary>
        [TestMethod]
        public async Task Execute_RemoveEmailTemplates_ShowsWarning_WhenNoneExist()
        {
            await SetupConfigFile();
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Remove Email Templates (index 5).
            NavigateMainMenu(
                console,
                5);

            // Back (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("No email templates to remove"),
                $"Expected 'No email templates to remove' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that adding a recipient to an existing template works.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditEmailTemplate_AddRecipient()
        {
            await SetupConfigFile(RichConfigXml);
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Edit Email Templates (index 4).
            NavigateMainMenu(
                console,
                4);

            // Select template (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Add Recipient (index 2).
            NavigateMainMenu(
                console,
                2);

            // Recipient email.
            console.Input.PushTextWithEnter("dev@test.com");

            // Recipient name.
            console.Input.PushTextWithEnter("Developer");

            // Back (index 5).
            NavigateMainMenu(
                console,
                5);

            // Back from email menu (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Recipient 'dev@test.com' added"),
                $"Expected recipient added but got: {console.Output}");
        }

        /// <summary>
        /// Checks that removing a recipient from a template works.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditEmailTemplate_RemoveRecipient()
        {
            await SetupConfigFile(RichConfigXml);
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Edit Email Templates (index 4).
            NavigateMainMenu(
                console,
                4);

            // Select template (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Remove Recipient (index 3).
            NavigateMainMenu(
                console,
                3);

            // Select recipient (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Back (index 5).
            NavigateMainMenu(
                console,
                5);

            // Back from email menu (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Recipient removed"),
                $"Expected 'Recipient removed' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Manage Images allows adding and removing images.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditEmailTemplate_ManageImages_AddAndRemove()
        {
            await SetupConfigFile(RichConfigXml);
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Edit Email Templates (index 4).
            NavigateMainMenu(
                console,
                4);

            // Select template (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Manage Images (index 4).
            NavigateMainMenu(
                console,
                4);

            // Add Image (index 0).
            console.Input.PushKey(ConsoleKey.Enter);

            // Image key.
            console.Input.PushTextWithEnter("banner");

            // Image path.
            console.Input.PushTextWithEnter(@"C:\banner.png");

            // Remove Image (index 1).
            NavigateMainMenu(
                console,
                1);

            // Select image to remove (first item — the original "logo").
            console.Input.PushKey(ConsoleKey.Enter);

            // Back from images (index 2).
            NavigateMainMenu(
                console,
                2);

            // Back from template edit (index 5).
            NavigateMainMenu(
                console,
                5);

            // Back from email menu (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            string output = console.Output;

            Assert.IsTrue(
                output.Contains("Image 'banner' added"),
                $"Expected 'Image banner added' but got: {output}");

            Assert.IsTrue(
                output.Contains("Image removed"),
                $"Expected 'Image removed' but got: {output}");
        }

        /// <summary>
        /// Checks that Remove Image shows a warning when no images exist.
        /// </summary>
        [TestMethod]
        public async Task Execute_ManageImages_RemoveShowsWarning_WhenNoneExist()
        {
            string noImagesConfig = RichConfigXml.Replace(
                @"<images>
            <image key=""logo"" path=""C:\logo.png"" />
          </images>",
                "<images />");

            await SetupConfigFile(noImagesConfig);
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Edit Email Templates (index 4).
            NavigateMainMenu(
                console,
                4);

            // Select template (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Manage Images (index 4).
            NavigateMainMenu(
                console,
                4);

            // Remove Image (index 1).
            NavigateMainMenu(
                console,
                1);

            // Back from images (index 2).
            NavigateMainMenu(
                console,
                2);

            // Back from template edit (index 5).
            NavigateMainMenu(
                console,
                5);

            // Back from email menu (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("No images to remove"),
                $"Expected 'No images to remove' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that editing API archive settings works.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditApiArchiveSettings_UpdatesValues()
        {
            await SetupConfigFile();

            string apiDir = Path.Combine(
                _TempDir,
                "Api");
            Directory.CreateDirectory(apiDir);
            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");
            await File.WriteAllTextAsync(
                apiSettingsPath,
                TestApiSettingsJson);

            SetupSingleInstallation(new VersionInfoModel
            {
                ServerName = "TestServer",
                ToolVersion = "1.0.0",
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer"
            });

            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync(TestApiSettingsJson);

            TestConsole console = new();
            console.Interactive();

            // API Settings (index 4, with API menu).
            NavigateMainMenu(
                console,
                4);

            // Archive Settings (index 1).
            NavigateMainMenu(
                console,
                1);

            // Archive directory.
            console.Input.PushTextWithEnter("New Archives");

            // Exit Without Saving (index 6, with API menu).
            NavigateMainMenu(
                console,
                6);

            await CreateMode(console).Execute();

            _MockLogger.Verify(
                l => l.LogMessage(It.IsAny<string>(), "API archive settings updated."),
                Times.Once());
        }

        /// <summary>
        /// Checks that regenerating API authentication credentials works.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditApiAuthentication_RegeneratesCredentials()
        {
            await SetupConfigFile();

            string apiDir = Path.Combine(
                _TempDir,
                "Api");
            Directory.CreateDirectory(apiDir);
            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");
            await File.WriteAllTextAsync(
                apiSettingsPath,
                TestApiSettingsJson);

            SetupSingleInstallation(new VersionInfoModel
            {
                ServerName = "TestServer",
                ToolVersion = "1.0.0",
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer"
            });

            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync(TestApiSettingsJson);

            TestConsole console = new();
            console.Interactive();

            // API Settings (index 4).
            NavigateMainMenu(
                console,
                4);

            // Authentication (index 2).
            NavigateMainMenu(
                console,
                2);

            // Regenerate credentials? Yes.
            console.Input.PushTextWithEnter("y");

            // Press Enter to continue after credentials display.
            console.Input.PushTextWithEnter("");

            // Exit Without Saving (index 6).
            NavigateMainMenu(
                console,
                6);

            await CreateMode(console).Execute();

            string output = console.Output;

            Assert.IsTrue(
                output.Contains("API Credentials"),
                $"Expected 'API Credentials' but got: {output}");

            _MockLogger.Verify(
                l => l.LogMessage(It.IsAny<string>(), "API authentication credentials regenerated."),
                Times.Once());
        }

        /// <summary>
        /// Checks that declining API credential regeneration does nothing.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditApiAuthentication_DeclinesRegeneration()
        {
            await SetupConfigFile();

            string apiDir = Path.Combine(
                _TempDir,
                "Api");
            Directory.CreateDirectory(apiDir);
            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");
            await File.WriteAllTextAsync(
                apiSettingsPath,
                TestApiSettingsJson);

            SetupSingleInstallation(new VersionInfoModel
            {
                ServerName = "TestServer",
                ToolVersion = "1.0.0",
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer"
            });

            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync(TestApiSettingsJson);

            TestConsole console = new();
            console.Interactive();

            // API Settings (index 4).
            NavigateMainMenu(
                console,
                4);

            // Authentication (index 2).
            NavigateMainMenu(
                console,
                2);

            // Regenerate credentials? No.
            console.Input.PushTextWithEnter("n");

            // Exit Without Saving (index 6).
            NavigateMainMenu(
                console,
                6);

            await CreateMode(console).Execute();

            _MockLogger.Verify(
                l => l.LogMessage(It.IsAny<string>(), "API authentication credentials regenerated."),
                Times.Never());
        }

        /// <summary>
        /// Checks that editing API webhook settings works and changes the secret.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditApiWebhook_ChangesSecret()
        {
            await SetupConfigFile();

            string apiDir = Path.Combine(
                _TempDir,
                "Api");
            Directory.CreateDirectory(apiDir);
            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");
            await File.WriteAllTextAsync(
                apiSettingsPath,
                TestApiSettingsJson);

            SetupSingleInstallation(new VersionInfoModel
            {
                ServerName = "TestServer",
                ToolVersion = "1.0.0",
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer"
            });

            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync(TestApiSettingsJson);

            TestConsole console = new();
            console.Interactive();

            // API Settings (index 4).
            NavigateMainMenu(
                console,
                4);

            // Webhook (index 3).
            NavigateMainMenu(
                console,
                3);

            // Change webhook secret? Yes.
            console.Input.PushTextWithEnter("y");

            // New secret (secret prompt).
            console.Input.PushTextWithEnter("new-webhook-secret");

            // Timeout (accept default).
            console.Input.PushTextWithEnter("10");

            // Max retries (accept default).
            console.Input.PushTextWithEnter("3");

            // Exit Without Saving (index 6).
            NavigateMainMenu(
                console,
                6);

            await CreateMode(console).Execute();

            _MockLogger.Verify(
                l => l.LogMessage(It.IsAny<string>(), "API webhook settings updated."),
                Times.Once());
        }

        /// <summary>
        /// Checks that saving with API changes calls WriteApiSettings.
        /// </summary>
        [TestMethod]
        public async Task Execute_SaveWithApiChanges_WritesApiSettings()
        {
            await SetupConfigFile();

            string apiDir = Path.Combine(
                _TempDir,
                "Api");
            Directory.CreateDirectory(apiDir);
            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");
            await File.WriteAllTextAsync(
                apiSettingsPath,
                TestApiSettingsJson);

            SetupSingleInstallation(new VersionInfoModel
            {
                ServerName = "TestServer",
                ToolVersion = "1.0.0",
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer"
            });

            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync(TestApiSettingsJson);

            _MockConfigWriter
                .Setup(w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()))
                .ReturnsAsync((true, (Exception?)null));

            _MockConfigWriter
                .Setup(w => w.WriteApiSettings(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((true, (Exception?)null));

            TestConsole console = new();
            console.Interactive();

            // API Settings (index 4).
            NavigateMainMenu(
                console,
                4);

            // Archive Settings (index 1).
            NavigateMainMenu(
                console,
                1);

            // New archive directory.
            console.Input.PushTextWithEnter("Changed Archives");

            // Save and Exit (index 5, with API menu).
            NavigateMainMenu(
                console,
                5);

            // Confirm save.
            console.Input.PushTextWithEnter("y");

            await CreateMode(console).Execute();

            _MockConfigWriter.Verify(
                w => w.WriteApiSettings(It.IsAny<string>(), It.IsAny<string>()),
                Times.Once());

            Assert.IsTrue(
                console.Output.Contains("API settings saved"),
                $"Expected 'API settings saved' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that saving with no changes shows a warning.
        /// </summary>
        [TestMethod]
        public async Task Execute_SaveWithNoChanges_ShowsNoChangesMessage()
        {
            await SetupConfigFile();
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Save and Exit (index 4).
            NavigateMainMenu(
                console,
                4);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("No changes were made"),
                $"Expected 'No changes were made' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that cancelling the save confirmation does not write config.
        /// </summary>
        [TestMethod]
        public async Task Execute_SaveCancelled_DoesNotWriteConfig()
        {
            await SetupConfigFile();
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Edit server details to create a change.
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("Changed");
            console.Input.PushTextWithEnter("Minecraft");
            console.Input.PushTextWithEnter(@"C:\GameServer");
            console.Input.PushTextWithEnter("start.bat");
            console.Input.PushTextWithEnter("127.0.0.1");

            // Save and Exit (index 4).
            NavigateMainMenu(
                console,
                4);

            // Decline save.
            console.Input.PushTextWithEnter("n");

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Save cancelled"),
                $"Expected 'Save cancelled' but got: {console.Output}");

            _MockConfigWriter.Verify(
                w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that a config write failure shows an error message.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsError_WhenConfigWriteFails()
        {
            await SetupConfigFile();
            SetupSingleInstallation();

            _MockConfigWriter
                .Setup(w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()))
                .ReturnsAsync((false, new IOException("Disk full")));

            TestConsole console = new();
            console.Interactive();

            // Edit server name to create a change.
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("Changed");
            console.Input.PushTextWithEnter("Minecraft");
            console.Input.PushTextWithEnter(@"C:\GameServer");
            console.Input.PushTextWithEnter("start.bat");
            console.Input.PushTextWithEnter("127.0.0.1");

            // Save and Exit (index 4).
            NavigateMainMenu(
                console,
                4);

            // Confirm save.
            console.Input.PushTextWithEnter("y");

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Failed to save App.config"),
                $"Expected 'Failed to save App.config' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that an API settings write failure shows an error message.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsError_WhenApiSettingsWriteFails()
        {
            await SetupConfigFile();

            string apiDir = Path.Combine(
                _TempDir,
                "Api");
            Directory.CreateDirectory(apiDir);
            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");
            await File.WriteAllTextAsync(
                apiSettingsPath,
                TestApiSettingsJson);

            SetupSingleInstallation(new VersionInfoModel
            {
                ServerName = "TestServer",
                ToolVersion = "1.0.0",
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer"
            });

            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync(TestApiSettingsJson);

            _MockConfigWriter
                .Setup(w => w.WriteConfig(It.IsAny<string>(), It.IsAny<XDocument>()))
                .ReturnsAsync((true, (Exception?)null));

            _MockConfigWriter
                .Setup(w => w.WriteApiSettings(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((false, new IOException("Permission denied")));

            TestConsole console = new();
            console.Interactive();

            // API Settings (index 4).
            NavigateMainMenu(
                console,
                4);

            // Archive Settings (index 1).
            NavigateMainMenu(
                console,
                1);

            // Changed value.
            console.Input.PushTextWithEnter("Different Archives");

            // Save and Exit (index 5).
            NavigateMainMenu(
                console,
                5);

            // Confirm save.
            console.Input.PushTextWithEnter("y");

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Failed to save API settings"),
                $"Expected 'Failed to save API settings' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that API settings load failure shows a warning and disables API menu.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsWarning_WhenApiSettingsLoadFails()
        {
            await SetupConfigFile();

            string apiDir = Path.Combine(
                _TempDir,
                "Api");
            Directory.CreateDirectory(apiDir);
            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");
            await File.WriteAllTextAsync(
                apiSettingsPath,
                "INVALID JSON {{{");

            SetupSingleInstallation(new VersionInfoModel
            {
                ServerName = "TestServer",
                ToolVersion = "1.0.0",
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer"
            });

            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync("INVALID JSON {{{");

            TestConsole console = new();
            console.Interactive();

            // Exit Without Saving (index 5 — no API in menu since load failed).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("Warning: Failed to load API settings"),
                $"Expected API load warning but got: {console.Output}");
        }

        /// <summary>
        /// Checks that the API Back option returns to the main menu without changes.
        /// </summary>
        [TestMethod]
        public async Task Execute_ApiSettingsBack_ReturnsToMainMenu()
        {
            await SetupConfigFile();

            string apiDir = Path.Combine(
                _TempDir,
                "Api");
            Directory.CreateDirectory(apiDir);
            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");
            await File.WriteAllTextAsync(
                apiSettingsPath,
                TestApiSettingsJson);

            SetupSingleInstallation(new VersionInfoModel
            {
                ServerName = "TestServer",
                ToolVersion = "1.0.0",
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer"
            });

            _MockFileSystem
                .Setup(fs => fs.ReadAllText(apiSettingsPath))
                .ReturnsAsync(TestApiSettingsJson);

            TestConsole console = new();
            console.Interactive();

            // API Settings (index 4).
            NavigateMainMenu(
                console,
                4);

            // Back (index 4).
            NavigateMainMenu(
                console,
                4);

            // Exit Without Saving (index 6).
            NavigateMainMenu(
                console,
                6);

            await CreateMode(console).Execute();

            _MockConfigWriter.Verify(
                w => w.WriteApiSettings(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that Remove Recipient shows a warning when no recipients exist.
        /// </summary>
        [TestMethod]
        public async Task Execute_EditEmailTemplate_RemoveRecipient_ShowsWarning_WhenNoneExist()
        {
            string noRecipientsConfig = RichConfigXml.Replace(
                @"<addresses>
            <toAddress email=""admin@test.com"" name=""Admin"" />
          </addresses>",
                "<addresses />");

            await SetupConfigFile(noRecipientsConfig);
            SetupSingleInstallation();

            TestConsole console = new();
            console.Interactive();

            // Email Notifications (index 2).
            NavigateMainMenu(
                console,
                2);

            // Edit Email Templates (index 4).
            NavigateMainMenu(
                console,
                4);

            // Select template (first item).
            console.Input.PushKey(ConsoleKey.Enter);

            // Remove Recipient (index 3).
            NavigateMainMenu(
                console,
                3);

            // Back (index 5).
            NavigateMainMenu(
                console,
                5);

            // Back from email menu (index 6).
            NavigateMainMenu(
                console,
                6);

            // Exit Without Saving (index 5).
            NavigateMainMenu(
                console,
                5);

            await CreateMode(console).Execute();

            Assert.IsTrue(
                console.Output.Contains("No recipients to remove"),
                $"Expected 'No recipients to remove' but got: {console.Output}");
        }
    }
}
