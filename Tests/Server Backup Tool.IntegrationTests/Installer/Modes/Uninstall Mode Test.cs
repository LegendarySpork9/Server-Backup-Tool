// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Modes;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Modes
{
    [TestClass]
    public class UninstallModeTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IFileService> _MockFileService = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;
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
            _MockTaskScheduler = new Mock<ITaskSchedulerService>();
            _MockRegistry = new Mock<IRegistryService>();
            _MockVersionService = new Mock<IVersionService>();
        }

        /// <summary>
        /// Checks that Execute shows an error when no installation is found.
        /// </summary>
        [TestMethod]
        public void Execute_ShowsError_WhenNoInstallationFound()
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns([]);

            TestConsole console = new();
            console.Interactive();

            UninstallMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object);

            mode.Execute();

            Assert.IsTrue(
                console.Output.Contains("No existing installation found"),
                $"Expected output to contain 'No existing installation found' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute does not remove the registry entry when the user declines confirmation.
        /// </summary>
        [TestMethod]
        public void Execute_CancelsWhenUserDeclinesConfirmation()
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

            TestConsole console = new();
            console.Interactive();

            // Confirmation: n.
            console.Input.PushTextWithEnter("n");

            UninstallMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object);

            mode.Execute();

            _MockRegistry.Verify(
                r => r.RemoveUninstallEntry(It.IsAny<string>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that Execute completes a full uninstall when the user confirms.
        /// </summary>
        [TestMethod]
        public void Execute_CompletesFullUninstall_WhenConfirmed()
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
            _MockTaskScheduler
                .Setup(t => t.TaskExists(It.IsAny<string>()))
                .Returns(true);
            _MockTaskScheduler
                .Setup(t => t.RemoveScheduledTask(It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockRegistry
                .Setup(r => r.RemoveUninstallEntry(It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            _MockFileService
                .Setup(f => f.DeleteDirectory(It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(false);

            TestConsole console = new();
            console.Interactive();

            console.Input.PushTextWithEnter("y");
            console.Input.PushTextWithEnter("n");
            console.Input.PushTextWithEnter("n");

            UninstallMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object);

            mode.Execute();

            _MockRegistry.Verify(
                r => r.RemoveUninstallEntry(It.IsAny<string>()),
                Times.Once());

            _MockTaskScheduler.Verify(
                t => t.RemoveScheduledTask(It.IsAny<string>()),
                Times.Once());

            Assert.IsTrue(
                console.Output.Contains("Uninstall completed"),
                $"Expected output to contain 'Uninstall completed' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute completes a full uninstall with API installed, database deletion, and log deletion.
        /// </summary>
        [TestMethod]
        public void Execute_CompletesFullUninstall_WithDatabaseAndLogDeletion()
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        ApiVersion = "1.0.0",
                        InstallPath = @"C:\Server Backup Tool",
                        ApiInstallPath = @"C:\Server Backup Tool.API",
                        ToolTaskName = "Server Backup Tool - TestServer",
                        ApiTaskName = "Server Backup Tool API - TestServer"
                    }
                ]);
            _MockTaskScheduler
                .Setup(t => t.TaskExists(It.IsAny<string>()))
                .Returns(true);
            _MockTaskScheduler
                .Setup(t => t.RemoveScheduledTask(It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockRegistry
                .Setup(r => r.RemoveUninstallEntry(It.IsAny<string>()))
                .Returns((true, (Exception?)null));
            _MockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(@"C:\Server Backup Tool.API"))
                .Returns(true);
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(It.Is<string>(s =>
                    s.Contains("Logs") || s.Contains("Archived Logs"))))
                .Returns(false);
            _MockFileService
                .Setup(f => f.DeleteDirectory(It.IsAny<string>()))
                .Returns((true, (Exception?)null));

            TestConsole console = new();
            console.Interactive();

            // Confirm uninstall.
            console.Input.PushTextWithEnter("y");
            // Select "Everything (Tool and API)" — first choice.
            console.Input.PushKey(ConsoleKey.Enter);
            // Don't keep database.
            console.Input.PushTextWithEnter("n");
            // Don't keep logs.
            console.Input.PushTextWithEnter("n");

            UninstallMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object);

            mode.Execute();

            _MockTaskScheduler.Verify(
                t => t.RemoveScheduledTask(It.IsAny<string>()),
                Times.Exactly(2));

            _MockRegistry.Verify(
                r => r.RemoveUninstallEntry(It.IsAny<string>()),
                Times.Once());

            _MockFileService.Verify(
                f => f.DeleteDirectory(It.IsAny<string>()),
                Times.AtLeastOnce());

            Assert.IsTrue(
                console.Output.Contains("Uninstall completed"),
                $"Expected output to contain 'Uninstall completed' but got: {console.Output}");
        }

        /// <summary>
        /// Checks that Execute removes only the API task and updates registry when API only is selected.
        /// </summary>
        [TestMethod]
        public void Execute_UninstallsApiOnly_WhenApiOnlySelected()
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns(
                [
                    new VersionInfoModel
                    {
                        ServerName = "TestServer",
                        ToolVersion = "1.0.0",
                        ApiVersion = "1.0.0",
                        InstallPath = @"C:\Server Backup Tool",
                        ApiInstallPath = @"C:\Server Backup Tool.API",
                        ToolTaskName = "Server Backup Tool - TestServer",
                        ApiTaskName = "Server Backup Tool API - TestServer"
                    }
                ]);
            _MockTaskScheduler
                .Setup(t => t.TaskExists("Server Backup Tool API - TestServer"))
                .Returns(true);
            _MockTaskScheduler
                .Setup(t => t.RemoveScheduledTask("Server Backup Tool API - TestServer"))
                .Returns((true, (Exception?)null));
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(@"C:\Server Backup Tool.API"))
                .Returns(true);
            _MockFileService
                .Setup(f => f.DeleteDirectory(@"C:\Server Backup Tool.API"))
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

            TestConsole console = new();
            console.Interactive();

            // Confirm uninstall.
            console.Input.PushTextWithEnter("y");
            // Select "API only" (second choice — down arrow then Enter).
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);

            UninstallMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockTaskScheduler.Object,
                _MockRegistry.Object,
                _MockVersionService.Object);

            mode.Execute();

            _MockTaskScheduler.Verify(
                t => t.RemoveScheduledTask("Server Backup Tool API - TestServer"),
                Times.Once());

            _MockRegistry.Verify(
                r => r.RemoveUninstallEntry(It.IsAny<string>()),
                Times.Never());

            _MockRegistry.Verify(
                r => r.WriteUninstallEntry(
                    "TestServer",
                    @"C:\Server Backup Tool",
                    string.Empty,
                    "1.0.0",
                    string.Empty,
                    "Server Backup Tool - TestServer",
                    string.Empty),
                Times.Once());

            _MockFileService.Verify(
                f => f.DeleteDirectory(@"C:\Server Backup Tool.API"),
                Times.Once());

            Assert.IsTrue(
                console.Output.Contains("Uninstall completed"),
                $"Expected output to contain 'Uninstall completed' but got: {console.Output}");
        }
    }
}
