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
    public class InstallModeTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IFileService> _MockFileService = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;
        private Mock<IResourceService> _MockResourceService = null!;
        private Mock<IConfigWriter> _MockConfigWriter = null!;
        private Mock<IDatabaseInitialiser> _MockDatabaseInitialiser = null!;
        private Mock<ITaskSchedulerService> _MockTaskScheduler = null!;
        private Mock<IRegistryService> _MockRegistry = null!;
        private Mock<IVersionService> _MockVersionService = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockFileService = new Mock<IFileService>();
            _MockFileSystem = new Mock<IExtendedFileSystem>();
            _MockResourceService = new Mock<IResourceService>();
            _MockConfigWriter = new Mock<IConfigWriter>();
            _MockDatabaseInitialiser = new Mock<IDatabaseInitialiser>();
            _MockTaskScheduler = new Mock<ITaskSchedulerService>();
            _MockRegistry = new Mock<IRegistryService>();
            _MockVersionService = new Mock<IVersionService>();
        }

        /// <summary>
        /// Checks that Execute completes without throwing when the user declines confirmation.
        /// </summary>
        [TestMethod]
        public async Task Execute_CancelsWhenUserDeclinesConfirmation()
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns([]);
            _MockVersionService
                .Setup(v => v.GetEmbeddedToolVersion())
                .Returns("1.0.0");
            _MockVersionService
                .Setup(v => v.GetEmbeddedApiVersion())
                .Returns("0.0.0");
            _MockFileService
                .Setup(f => f.ValidateWritePermissions(It.IsAny<string>()))
                .ReturnsAsync(true);
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(true);

            TestConsole console = new();
            console.Interactive();

            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter(@"C:\TestInstall");
            console.Input.PushTextWithEnter("TestServer");
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter(@"C:\GameServer");
            console.Input.PushTextWithEnter("server.jar");
            console.Input.PushTextWithEnter("192.168.1.100");
            console.Input.PushTextWithEnter(@"C:\ProgramData\Data.db");
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("03:00:00");
            console.Input.PushTextWithEnter("n");
            console.Input.PushTextWithEnter("n");
            console.Input.PushTextWithEnter("n");
            console.Input.PushTextWithEnter("n");

            InstallMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockResourceService.Object,
                _MockConfigWriter.Object,
                _MockDatabaseInitialiser.Object,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object);

            await mode.Execute();

            _MockRegistry.Verify(
                r => r.WriteUninstallEntry(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that Execute completes a full installation with the tool only component selected.
        /// </summary>
        [TestMethod]
        public async Task Execute_CompletesFullInstallation_WithToolOnly()
        {
            string tempDir = Path.Combine(
                Path.GetTempPath(),
                $"InstallModeTest_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDir);

            string dbName = $"InstallModeTest_{Guid.NewGuid():N}";
            string connectionString = $"{dbName};Mode=Memory;Cache=Shared";

            SqliteConnection keepAlive = new($"Data Source={connectionString}");

            await keepAlive.OpenAsync();

            try
            {
                Mock<ILoggerService> mockLogger = new();
                Mock<IFileService> mockFileService = new();
                Mock<IExtendedFileSystem> mockFileSystem = new();
                Mock<IResourceService> mockResourceService = new();
                Mock<ITaskSchedulerService> mockTaskScheduler = new();
                Mock<IRegistryService> mockRegistry = new();
                Mock<IVersionService> mockVersionService = new();

                mockVersionService
                    .Setup(v => v.GetAllInstallations())
                    .Returns([]);
                mockVersionService
                    .Setup(v => v.GetEmbeddedToolVersion())
                    .Returns("1.0.0");
                mockVersionService
                    .Setup(v => v.GetEmbeddedApiVersion())
                    .Returns("0.0.0");
                mockFileService
                    .Setup(f => f.ValidateWritePermissions(It.IsAny<string>()))
                    .ReturnsAsync(true);
                mockFileSystem
                    .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                    .Returns(true);
                mockResourceService
                    .Setup(r => r.FindResource(It.IsAny<string>()))
                    .Returns((string?)null);
                mockResourceService
                    .Setup(r => r.ResourceExists(It.IsAny<string>()))
                    .Returns(false);
                mockTaskScheduler
                    .Setup(t => t.CreateScheduledTask(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((true, (Exception?)null));
                mockTaskScheduler
                    .Setup(t => t.TaskExists(It.IsAny<string>()))
                    .Returns(true);
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
                    .Returns("1.0.0");

                ExtendedFileSystemWrapper realFileSystem = new();

                ConfigWriter realConfigWriter = new(
                    mockLogger.Object,
                    realFileSystem);

                DatabaseInitialiser realDatabaseInitialiser = new(
                    mockLogger.Object,
                    mockFileSystem.Object);

                TestConsole console = new();
                console.Interactive();

                console.Input.PushKey(ConsoleKey.Enter);
                console.Input.PushTextWithEnter(tempDir);
                console.Input.PushTextWithEnter("TestServer");
                console.Input.PushKey(ConsoleKey.Enter);
                console.Input.PushKey(ConsoleKey.Enter);
                console.Input.PushTextWithEnter(tempDir);
                console.Input.PushTextWithEnter("start.bat");
                console.Input.PushTextWithEnter("127.0.0.1");
                console.Input.PushTextWithEnter(connectionString);
                console.Input.PushKey(ConsoleKey.Enter);
                console.Input.PushTextWithEnter("03:00:00");
                console.Input.PushTextWithEnter("n");
                console.Input.PushTextWithEnter("n");
                console.Input.PushTextWithEnter("y");

                InstallMode mode = new(
                    console,
                    mockLogger.Object,
                    mockFileService.Object,
                    mockFileSystem.Object,
                    mockResourceService.Object,
                    realConfigWriter,
                    realDatabaseInitialiser,
                    mockTaskScheduler.Object,
                    mockRegistry.Object,
                    mockVersionService.Object);

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

                mockTaskScheduler.Verify(
                    t => t.CreateScheduledTask(
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                    Times.Once());

                Assert.IsTrue(
                    console.Output.Contains("Installation completed successfully"),
                    $"Expected output to contain 'Installation completed successfully' but got: {console.Output}");
            }

            finally
            {
                await keepAlive.CloseAsync();
                await keepAlive.DisposeAsync();

                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(
                        tempDir,
                        true);
                }
            }
        }
    }
}
