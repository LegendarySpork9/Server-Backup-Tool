// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Steps;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class ValidationStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;
        private Mock<IDatabaseInitialiser> _MockDatabaseInitialiser = null!;
        private Mock<ITaskSchedulerService> _MockTaskScheduler = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockFileSystem = new Mock<IExtendedFileSystem>();
            _MockDatabaseInitialiser = new Mock<IDatabaseInitialiser>();
            _MockTaskScheduler = new Mock<ITaskSchedulerService>();
        }

        /// <summary>
        /// Checks that Execute shows PASS for all checks when everything is valid.
        /// </summary>
        [TestMethod]
        public async Task Execute_AllChecksPassing()
        {
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(true);
            _MockDatabaseInitialiser
                .Setup(d => d.ValidateDatabase(It.IsAny<string>()))
                .Returns(Task.FromResult((true, (Exception?)null)));
            _MockTaskScheduler
                .Setup(t => t.TaskExists(It.IsAny<string>()))
                .Returns(true);

            InstallOptionsModel options = new()
            {
                InstallPath = @"C:\Server Backup Tool",
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = string.Empty,
                ServerConfig = new ServerConfigModel
                {
                    DatabasePath = @"C:\ProgramData\Data.db"
                }
            };

            TestConsole console = new();
            console.Interactive();

            ValidationStep step = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockDatabaseInitialiser.Object,
                _MockTaskScheduler.Object,
                options);

            await step.Execute();

            Assert.IsTrue(
                console.Output.Contains("PASS"),
                $"Expected output to contain 'PASS' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute shows FAIL when checks do not pass.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsFailures_WhenChecksDoNotPass()
        {
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(false);
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(false);
            _MockDatabaseInitialiser
                .Setup(d => d.ValidateDatabase(It.IsAny<string>()))
                .Returns(Task.FromResult((false, (Exception?)null)));
            _MockTaskScheduler
                .Setup(t => t.TaskExists(It.IsAny<string>()))
                .Returns(false);

            InstallOptionsModel options = new()
            {
                InstallPath = @"C:\Server Backup Tool",
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = string.Empty,
                ServerConfig = new ServerConfigModel
                {
                    DatabasePath = @"C:\ProgramData\Data.db"
                }
            };

            TestConsole console = new();
            console.Interactive();

            ValidationStep step = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockDatabaseInitialiser.Object,
                _MockTaskScheduler.Object,
                options);

            await step.Execute();

            Assert.IsTrue(
                console.Output.Contains("FAIL"),
                $"Expected output to contain 'FAIL' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute validates the API scheduled task when ApiConfig with ApiTaskName is present.
        /// </summary>
        [TestMethod]
        public async Task Execute_ValidatesApiConfig_WhenApiConfigPresent()
        {
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(false);
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(true);
            _MockDatabaseInitialiser
                .Setup(d => d.ValidateDatabase(It.IsAny<string>()))
                .Returns(Task.FromResult((true, (Exception?)null)));
            _MockTaskScheduler
                .Setup(t => t.TaskExists("Server Backup Tool - ApiServer"))
                .Returns(true);
            _MockTaskScheduler
                .Setup(t => t.TaskExists("Server Backup Tool API - ApiServer"))
                .Returns(true);

            InstallOptionsModel options = new()
            {
                InstallPath = @"C:\Server Backup Tool",
                ToolTaskName = "Server Backup Tool - ApiServer",
                ApiTaskName = "Server Backup Tool API - ApiServer",
                ServerConfig = new ServerConfigModel
                {
                    DatabasePath = @"C:\ProgramData\Data.db"
                },
                ApiConfig = new ApiConfigModel
                {
                    BindAddress = "0.0.0.0",
                    HttpPort = 5000,
                    EnableHttps = false
                }
            };

            TestConsole console = new();
            console.Interactive();

            ValidationStep step = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockDatabaseInitialiser.Object,
                _MockTaskScheduler.Object,
                options);

            await step.Execute();

            string output = console.Output;

            Assert.IsTrue(
                output.Contains("API scheduled task exists"),
                $"Expected output to contain 'API scheduled task exists' but got: {output}");

            _MockTaskScheduler.Verify(
                t => t.TaskExists("Server Backup Tool API - ApiServer"),
                Times.Once());
        }

        /// <summary>
        /// Checks that Execute shows both PASS and FAIL when ApiConfig is set and some checks pass while others fail.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsMixedResults_WhenApiConfigSetAndSomeChecksFail()
        {
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(true);
            _MockDatabaseInitialiser
                .Setup(d => d.ValidateDatabase(It.IsAny<string>()))
                .Returns(Task.FromResult((true, (Exception?)null)));
            _MockTaskScheduler
                .Setup(t => t.TaskExists("Server Backup Tool - TestServer"))
                .Returns(true);
            _MockTaskScheduler
                .Setup(t => t.TaskExists("Server Backup Tool API - TestServer"))
                .Returns(false);

            InstallOptionsModel options = new()
            {
                InstallPath = @"C:\Server Backup Tool",
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = "Server Backup Tool API - TestServer",
                ServerConfig = new ServerConfigModel
                {
                    DatabasePath = @"C:\ProgramData\Data.db"
                },
                ApiConfig = new ApiConfigModel
                {
                    BindAddress = "0.0.0.0",
                    HttpPort = 5000,
                    EnableHttps = false
                }
            };

            TestConsole console = new();
            console.Interactive();

            ValidationStep step = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockDatabaseInitialiser.Object,
                _MockTaskScheduler.Object,
                options);

            await step.Execute();

            string output = console.Output;

            Assert.IsTrue(
                output.Contains("PASS"),
                $"Expected output to contain 'PASS' but got: {output}");
            Assert.IsTrue(
                output.Contains("FAIL"),
                $"Expected output to contain 'FAIL' but got: {output}");
            Assert.IsTrue(
                output.Contains("API scheduled task exists"),
                $"Expected output to contain 'API scheduled task exists' but got: {output}");
        }
    }
}
