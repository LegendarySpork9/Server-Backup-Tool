// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Steps;
using ServerBackupTool.Installer.Values;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class FileDeployStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IResourceService> _MockResourceService = null!;
        private Mock<ITaskSchedulerService> _MockTaskScheduler = null!;
        private Mock<IRegistryService> _MockRegistry = null!;
        private Mock<IVersionService> _MockVersionService = null!;
        private ExtendedFileSystemWrapper _RealFileSystem = null!;
        private ConfigWriter _RealConfigWriter = null!;
        private DatabaseInitialiser _RealDatabaseInitialiser = null!;
        private SqliteConnection _KeepAlive = null!;
        private string _ConnectionString = null!;
        private string _TempDir = null!;

        /// <summary>
        /// Initialises the test dependencies, temp directory, and in-memory database.
        /// </summary>
        [TestInitialize]
        public async Task TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockResourceService = new Mock<IResourceService>();
            _MockTaskScheduler = new Mock<ITaskSchedulerService>();
            _MockRegistry = new Mock<IRegistryService>();
            _MockVersionService = new Mock<IVersionService>();

            _RealFileSystem = new ExtendedFileSystemWrapper();

            _RealConfigWriter = new ConfigWriter(
                _MockLogger.Object,
                _RealFileSystem);

            Mock<IExtendedFileSystem> mockDbFileSystem = new();

            _RealDatabaseInitialiser = new DatabaseInitialiser(
                _MockLogger.Object,
                mockDbFileSystem.Object);

            string dbName = $"FileDeployStepTest_{Guid.NewGuid():N}";
            _ConnectionString = $"{dbName};Mode=Memory;Cache=Shared";

            _KeepAlive = new SqliteConnection($"Data Source={_ConnectionString}");

            await _KeepAlive.OpenAsync();

            _TempDir = Path.Combine(
                Path.GetTempPath(),
                $"SBT_FileDeployStepTest_{Guid.NewGuid():N}");

            Directory.CreateDirectory(_TempDir);
        }

        /// <summary>
        /// Cleans up the in-memory database and temp directory.
        /// </summary>
        [TestCleanup]
        public async Task TestCleanup()
        {
            await _KeepAlive.CloseAsync();
            await _KeepAlive.DisposeAsync();

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
        /// Checks that Execute completes deployment, registers scheduled task, writes registry entry,
        /// and creates a config file on disk.
        /// </summary>
        [TestMethod]
        public async Task Execute_CompletesDeployment_WithMockedServices()
        {
            _MockResourceService
                .Setup(r => r.FindResource(It.IsAny<string>()))
                .Returns((string?)null);
            _MockResourceService
                .Setup(r => r.ResourceExists(It.IsAny<string>()))
                .Returns(false);
            _MockTaskScheduler
                .Setup(t => t.CreateScheduledTask(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockRegistry
                .Setup(r => r.WriteUninstallEntry(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockVersionService
                .Setup(v => v.GetBundledToolVersion(It.IsAny<string>()))
                .Returns("1.0.0");
            _MockVersionService
                .Setup(v => v.GetBundledApiVersion(It.IsAny<string>()))
                .Returns("1.0.0");

            InstallOptionsModel options = new()
            {
                InstallPath = _TempDir,
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = string.Empty,
                ServerConfig = new ServerConfigModel
                {
                    ServerName = "TestServer",
                    Game = "Minecraft",
                    ServerDirectory = @"C:\GameServer",
                    StartFile = "start.bat",
                    IPAddress = "192.168.1.100",
                    DatabasePath = _ConnectionString
                },
                TimerConfig = new TimerConfigModel
                {
                    BackupTime = "03:00:00",
                    CustomTimers = []
                }
            };

            TestConsole console = new();
            console.Interactive();

            FileDeployStep step = new(
                console,
                _MockLogger.Object,
                _RealFileSystem,
                _MockResourceService.Object,
                _RealConfigWriter,
                _RealDatabaseInitialiser,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object,
                options);

            await step.Execute();

            _MockTaskScheduler.Verify(
                t => t.CreateScheduledTask(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once());

            _MockRegistry.Verify(
                r => r.WriteUninstallEntry(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once());

            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            Assert.IsTrue(
                File.Exists(configPath),
                $"Expected config file to exist at '{configPath}' after Execute.");
        }

        /// <summary>
        /// Checks that Execute handles resource not found by logging a warning and skipping extraction.
        /// </summary>
        [TestMethod]
        public async Task Execute_HandlesExtractionFailure_WhenResourceNotFound()
        {
            _MockResourceService
                .Setup(r => r.ResourceExists(It.IsAny<string>()))
                .Returns(false);
            _MockResourceService
                .Setup(r => r.FindResource(It.IsAny<string>()))
                .Returns((string?)null);
            _MockTaskScheduler
                .Setup(t => t.CreateScheduledTask(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockRegistry
                .Setup(r => r.WriteUninstallEntry(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockVersionService
                .Setup(v => v.GetBundledToolVersion(It.IsAny<string>()))
                .Returns("1.0.0");

            InstallOptionsModel options = new()
            {
                InstallPath = _TempDir,
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = string.Empty,
                ServerConfig = new ServerConfigModel
                {
                    ServerName = "TestServer",
                    Game = "Minecraft",
                    ServerDirectory = @"C:\GameServer",
                    StartFile = "start.bat",
                    IPAddress = "192.168.1.100",
                    DatabasePath = _ConnectionString
                },
                TimerConfig = new TimerConfigModel
                {
                    BackupTime = "03:00:00",
                    CustomTimers = []
                }
            };

            TestConsole console = new();
            console.Interactive();

            FileDeployStep step = new(
                console,
                _MockLogger.Object,
                _RealFileSystem,
                _MockResourceService.Object,
                _RealConfigWriter,
                _RealDatabaseInitialiser,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object,
                options);

            await step.Execute();

            _MockResourceService.Verify(
                r => r.ResourceExists(It.IsAny<string>()),
                Times.Once());
            _MockResourceService.Verify(
                r => r.ExtractResource(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<string>?>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that Execute throws when database initialisation fails.
        /// </summary>
        [TestMethod]
        public async Task Execute_HandlesDatabaseFailure_WhenInitialisationFails()
        {
            Mock<IDatabaseInitialiser> mockDatabaseInitialiser = new();
            mockDatabaseInitialiser
                .Setup(d => d.InitialiseDatabase(It.IsAny<string>()))
                .Returns(Task.FromResult((false, (Exception?)new IOException("Database error"))));

            _MockResourceService
                .Setup(r => r.ResourceExists(It.IsAny<string>()))
                .Returns(false);
            _MockResourceService
                .Setup(r => r.FindResource(It.IsAny<string>()))
                .Returns((string?)null);
            _MockTaskScheduler
                .Setup(t => t.CreateScheduledTask(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockRegistry
                .Setup(r => r.WriteUninstallEntry(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockVersionService
                .Setup(v => v.GetBundledToolVersion(It.IsAny<string>()))
                .Returns("1.0.0");

            InstallOptionsModel options = new()
            {
                InstallPath = _TempDir,
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = string.Empty,
                ServerConfig = new ServerConfigModel
                {
                    ServerName = "TestServer",
                    Game = "Minecraft",
                    ServerDirectory = @"C:\GameServer",
                    StartFile = "start.bat",
                    IPAddress = "192.168.1.100",
                    DatabasePath = _ConnectionString
                },
                TimerConfig = new TimerConfigModel
                {
                    BackupTime = "03:00:00",
                    CustomTimers = []
                }
            };

            TestConsole console = new();
            console.Interactive();

            FileDeployStep step = new(
                console,
                _MockLogger.Object,
                _RealFileSystem,
                _MockResourceService.Object,
                _RealConfigWriter,
                mockDatabaseInitialiser.Object,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object,
                options);

            InvalidOperationException thrown = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => step.Execute());

            Assert.AreEqual(
                "Failed to create database.",
                thrown.Message);
            Assert.IsNotNull(thrown.InnerException);
            Assert.IsInstanceOfType<IOException>(thrown.InnerException);
        }

        /// <summary>
        /// Checks that Execute throws when scheduled task creation fails.
        /// </summary>
        [TestMethod]
        public async Task Execute_HandlesScheduledTaskFailure_WhenCreationFails()
        {
            _MockResourceService
                .Setup(r => r.ResourceExists(It.IsAny<string>()))
                .Returns(false);
            _MockResourceService
                .Setup(r => r.FindResource(It.IsAny<string>()))
                .Returns((string?)null);
            _MockTaskScheduler
                .Setup(t => t.CreateScheduledTask(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((false, (Exception?)new UnauthorizedAccessException("Access denied")));

            InstallOptionsModel options = new()
            {
                InstallPath = _TempDir,
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = string.Empty,
                ServerConfig = new ServerConfigModel
                {
                    ServerName = "TestServer",
                    Game = "Minecraft",
                    ServerDirectory = @"C:\GameServer",
                    StartFile = "start.bat",
                    IPAddress = "192.168.1.100",
                    DatabasePath = _ConnectionString
                },
                TimerConfig = new TimerConfigModel
                {
                    BackupTime = "03:00:00",
                    CustomTimers = []
                }
            };

            TestConsole console = new();
            console.Interactive();

            FileDeployStep step = new(
                console,
                _MockLogger.Object,
                _RealFileSystem,
                _MockResourceService.Object,
                _RealConfigWriter,
                _RealDatabaseInitialiser,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object,
                options);

            InvalidOperationException thrown = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => step.Execute());

            Assert.AreEqual(
                "Failed to register scheduled task.",
                thrown.Message);
            Assert.IsNotNull(thrown.InnerException);
            Assert.IsInstanceOfType<UnauthorizedAccessException>(thrown.InnerException);
        }

        /// <summary>
        /// Checks that Execute with API config creates API directories, generates API config, and registers API scheduled task.
        /// </summary>
        [TestMethod]
        public async Task Execute_WithApiConfig_CreatesApiDirectoriesAndRegistersApiTask()
        {
            string apiDir = Path.Combine(
                _TempDir,
                "API");

            _MockResourceService
                .Setup(r => r.FindResource(It.IsAny<string>()))
                .Returns((string?)null);
            _MockResourceService
                .Setup(r => r.ResourceExists(It.IsAny<string>()))
                .Returns(false);
            _MockTaskScheduler
                .Setup(t => t.CreateScheduledTask(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockRegistry
                .Setup(r => r.WriteUninstallEntry(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockVersionService
                .Setup(v => v.GetBundledToolVersion(It.IsAny<string>()))
                .Returns("1.0.0");
            _MockVersionService
                .Setup(v => v.GetBundledApiVersion(It.IsAny<string>()))
                .Returns("1.0.0");

            InstallOptionsModel options = new()
            {
                InstallPath = _TempDir,
                ApiInstallPath = apiDir,
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = "Server Backup Tool API - TestServer",
                ServerConfig = new ServerConfigModel
                {
                    ServerName = "TestServer",
                    Game = "Minecraft",
                    ServerDirectory = @"C:\GameServer",
                    StartFile = "start.bat",
                    IPAddress = "192.168.1.100",
                    DatabasePath = _ConnectionString
                },
                TimerConfig = new TimerConfigModel
                {
                    BackupTime = "03:00:00",
                    CustomTimers = []
                },
                ApiConfig = new ApiConfigModel
                {
                    DatabasePath = _ConnectionString,
                    ClientIdHash = "hashedId",
                    ClientSecretHash = "hashedSecret",
                    WebhookSecret = "webhookSecret"
                }
            };

            TestConsole console = new();
            console.Interactive();

            FileDeployStep step = new(
                console,
                _MockLogger.Object,
                _RealFileSystem,
                _MockResourceService.Object,
                _RealConfigWriter,
                _RealDatabaseInitialiser,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object,
                options);

            await step.Execute();

            Assert.IsTrue(
                Directory.Exists(apiDir),
                $"Expected API directory to exist at '{apiDir}' after Execute.");

            _MockTaskScheduler.Verify(
                t => t.CreateScheduledTask(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Exactly(2));

            _MockTaskScheduler.Verify(
                t => t.CreateScheduledTask(
                    "Server Backup Tool API - TestServer",
                    It.Is<string>(s => s.Contains("ServerBackupTool.API.exe"))),
                Times.Once());

            string apiSettingsPath = Path.Combine(
                apiDir,
                "appsettings.json");

            Assert.IsTrue(
                File.Exists(apiSettingsPath),
                $"Expected API settings file to exist at '{apiSettingsPath}' after Execute.");

            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            Assert.IsTrue(
                File.Exists(configPath),
                $"Expected config file to exist at '{configPath}' after Execute.");
        }
    }
}
