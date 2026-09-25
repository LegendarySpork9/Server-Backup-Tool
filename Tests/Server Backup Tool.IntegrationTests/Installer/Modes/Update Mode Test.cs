// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Modes;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Modes
{
    [TestClass]
    public class UpdateModeTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IFileService> _MockFileService = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;
        private Mock<IConfigWriter> _MockConfigWriter = null!;
        private Mock<IDatabaseInitialiser> _MockDatabaseInitialiser = null!;
        private Mock<IRegistryService> _MockRegistry = null!;
        private Mock<IVersionService> _MockVersionService = null!;
        private Mock<IResourceService> _MockResourceService = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockFileService = new Mock<IFileService>();
            _MockFileSystem = new Mock<IExtendedFileSystem>();
            _MockConfigWriter = new Mock<IConfigWriter>();
            _MockDatabaseInitialiser = new Mock<IDatabaseInitialiser>();
            _MockRegistry = new Mock<IRegistryService>();
            _MockVersionService = new Mock<IVersionService>();
            _MockResourceService = new Mock<IResourceService>();
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

            UpdateMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockDatabaseInitialiser.Object,
                _MockRegistry.Object,
                _MockVersionService.Object,
                _MockResourceService.Object);

            await mode.Execute();

            Assert.IsTrue(
                console.Output.Contains("No existing installation found"),
                $"Expected output to contain 'No existing installation found' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute completes an update when the user confirms.
        /// </summary>
        [TestMethod]
        public async Task Execute_CompletesUpdate_WhenConfirmed()
        {
            string tempDir = Path.Combine(
                Path.GetTempPath(),
                $"UpdateModeTest_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDir);

            try
            {
                string configPath = Path.Combine(
                    tempDir,
                    "ServerBackupTool.dll.config");

                string configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <section name=""log4net"" type=""log4net.Config.Log4NetConfigurationSectionHandler,log4net"" />
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

                await File.WriteAllTextAsync(
                    configPath,
                    configXml);

                Mock<ILoggerService> mockLogger = new();
                Mock<IFileService> mockFileService = new();
                Mock<IExtendedFileSystem> mockFileSystem = new();
                Mock<IResourceService> mockResourceService = new();
                Mock<IRegistryService> mockRegistry = new();
                Mock<IVersionService> mockVersionService = new();

                mockVersionService
                    .Setup(v => v.GetAllInstallations())
                    .Returns(
                    [
                        new VersionInfoModel
                        {
                            ServerName = "TestServer",
                            ToolVersion = "1.0.0",
                            InstallPath = tempDir,
                            ToolTaskName = "Server Backup Tool - TestServer"
                        }
                    ]);
                mockVersionService
                    .Setup(v => v.GetEmbeddedToolVersion())
                    .Returns("2.0.0");
                mockVersionService
                    .Setup(v => v.GetEmbeddedApiVersion())
                    .Returns("0.0.0");
                mockFileService
                    .Setup(f => f.BackupDirectory(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((true, (Exception?)null));
                mockResourceService
                    .Setup(r => r.FindResource(It.IsAny<string>()))
                    .Returns((string?)null);
                mockResourceService
                    .Setup(r => r.ResourceExists(It.IsAny<string>()))
                    .Returns(false);
                mockRegistry
                    .Setup(r => r.WriteUninstallEntry(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Returns((true, (Exception?)null));
                mockVersionService
                    .Setup(v => v.GetBundledToolVersion(It.IsAny<string>()))
                    .Returns("2.0.0");
                mockFileSystem
                    .Setup(fs => fs.FileExists(It.IsAny<string>()))
                    .Returns((string path) => !path.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));

                ExtendedFileSystemWrapper realFileSystem = new();

                ConfigWriter realConfigWriter = new(
                    mockLogger.Object,
                    realFileSystem);

                DatabaseInitialiser realDatabaseInitialiser = new(
                    mockLogger.Object,
                    mockFileSystem.Object);

                TestConsole console = new();
                console.Interactive();

                console.Input.PushTextWithEnter("y");

                UpdateMode mode = new(
                    console,
                    mockLogger.Object,
                    mockFileService.Object,
                    mockFileSystem.Object,
                    realConfigWriter,
                    realDatabaseInitialiser,
                    mockRegistry.Object,
                    mockVersionService.Object,
                    mockResourceService.Object);

                await mode.Execute();

                mockRegistry.Verify(
                    r => r.WriteUninstallEntry(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                    Times.Once());

                Assert.IsTrue(
                    console.Output.Contains("Update completed successfully"),
                    $"Expected output to contain 'Update completed successfully' but got: {console.Output}");
            }

            finally
            {
                SqliteConnection.ClearAllPools();

                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(
                        tempDir,
                        true);
                }
            }
        }

        /// <summary>
        /// Checks that Execute cancels the update when the user declines confirmation.
        /// </summary>
        [TestMethod]
        public async Task Execute_CancelsUpdate_WhenUserDeclinesConfirmation()
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        InstallPath = @"C:\Server Backup Tool",
                        ToolTaskName = "Server Backup Tool - TestServer"
                    }
                ]);
            _MockVersionService
                .Setup(v => v.GetEmbeddedToolVersion())
                .Returns("2.0.0");
            _MockVersionService
                .Setup(v => v.GetEmbeddedApiVersion())
                .Returns("0.0.0");

            TestConsole console = new();
            console.Interactive();

            // Decline update.
            console.Input.PushTextWithEnter("n");

            UpdateMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockDatabaseInitialiser.Object,
                _MockRegistry.Object,
                _MockVersionService.Object,
                _MockResourceService.Object);

            await mode.Execute();

            _MockResourceService.Verify(
                r => r.ExtractResource(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<string>?>()),
                Times.Never());

            _MockFileService.Verify(
                f => f.BackupDirectory(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());

            Assert.IsTrue(
                console.Output.Contains("Update cancelled"),
                $"Expected output to contain 'Update cancelled' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute extracts both tool and API binaries when API is installed.
        /// </summary>
        [TestMethod]
        public async Task Execute_ExtractsBothToolAndApi_WhenApiInstalled()
        {
            string tempDir = Path.Combine(
                Path.GetTempPath(),
                $"UpdateModeTest_{Guid.NewGuid():N}");
            string apiDir = Path.Combine(
                Path.GetTempPath(),
                $"UpdateModeApiTest_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(apiDir);

            try
            {
                string configPath = Path.Combine(
                    tempDir,
                    "ServerBackupTool.dll.config");

                string configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
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

                await File.WriteAllTextAsync(
                    configPath,
                    configXml);

                Mock<ILoggerService> mockLogger = new();
                Mock<IFileService> mockFileService = new();
                Mock<IExtendedFileSystem> mockFileSystem = new();
                Mock<IResourceService> mockResourceService = new();
                Mock<IRegistryService> mockRegistry = new();
                Mock<IVersionService> mockVersionService = new();

                mockVersionService
                    .Setup(v => v.GetAllInstallations())
                    .Returns(
                    [
                        new VersionInfoModel
                        {
                            ServerName = "TestServer",
                            ToolVersion = "1.0.0",
                            ApiVersion = "1.0.0",
                            InstallPath = tempDir,
                            ApiInstallPath = apiDir,
                            ToolTaskName = "Server Backup Tool - TestServer",
                            ApiTaskName = "Server Backup Tool API - TestServer"
                        }
                    ]);
                mockVersionService
                    .Setup(v => v.GetEmbeddedToolVersion())
                    .Returns("2.0.0");
                mockVersionService
                    .Setup(v => v.GetEmbeddedApiVersion())
                    .Returns("2.0.0");
                mockFileService
                    .Setup(f => f.BackupDirectory(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((true, (Exception?)null));
                mockResourceService
                    .Setup(r => r.FindResource("Tool_"))
                    .Returns("Tool_2.0.0.zip");
                mockResourceService
                    .Setup(r => r.FindResource("API_"))
                    .Returns("API_2.0.0.zip");
                mockResourceService
                    .Setup(r => r.ExtractResource(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<string>?>()))
                    .Returns((true, (Exception?)null));
                mockRegistry
                    .Setup(r => r.WriteUninstallEntry(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Returns((true, (Exception?)null));
                mockVersionService
                    .Setup(v => v.GetBundledToolVersion(It.IsAny<string>()))
                    .Returns("2.0.0");
                mockVersionService
                    .Setup(v => v.GetBundledApiVersion(It.IsAny<string>()))
                    .Returns("2.0.0");
                mockFileSystem
                    .Setup(fs => fs.FileExists(It.IsAny<string>()))
                    .Returns((string path) => !path.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));

                ExtendedFileSystemWrapper realFileSystem = new();

                ConfigWriter realConfigWriter = new(
                    mockLogger.Object,
                    realFileSystem);

                DatabaseInitialiser realDatabaseInitialiser = new(
                    mockLogger.Object,
                    mockFileSystem.Object);

                TestConsole console = new();
                console.Interactive();

                // Confirm update.
                console.Input.PushTextWithEnter("y");

                UpdateMode mode = new(
                    console,
                    mockLogger.Object,
                    mockFileService.Object,
                    mockFileSystem.Object,
                    realConfigWriter,
                    realDatabaseInitialiser,
                    mockRegistry.Object,
                    mockVersionService.Object,
                    mockResourceService.Object);

                await mode.Execute();

                mockResourceService.Verify(
                    r => r.ExtractResource(
                        "Tool_2.0.0.zip",
                        tempDir,
                        It.IsAny<Action<string>?>()),
                    Times.Once());

                mockResourceService.Verify(
                    r => r.ExtractResource(
                        "API_2.0.0.zip",
                        apiDir,
                        It.IsAny<Action<string>?>()),
                    Times.Once());

                Assert.IsTrue(
                    console.Output.Contains("Update completed successfully"),
                    $"Expected output to contain 'Update completed successfully' but got: {console.Output}");
            }

            finally
            {
                SqliteConnection.ClearAllPools();

                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(
                        tempDir,
                        true);
                }

                if (Directory.Exists(apiDir))
                {
                    Directory.Delete(
                        apiDir,
                        true);
                }
            }
        }
    }
}
