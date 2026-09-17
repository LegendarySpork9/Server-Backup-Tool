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
    }
}
