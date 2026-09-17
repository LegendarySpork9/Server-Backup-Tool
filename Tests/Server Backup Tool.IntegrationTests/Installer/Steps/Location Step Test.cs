// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Steps;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class LocationStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IFileService> _MockFileService = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockFileService = new Mock<IFileService>();
        }

        /// <summary>
        /// Checks that Execute sets the install path and API install path when the default is accepted.
        /// </summary>
        [TestMethod]
        public async Task Execute_SetsInstallPathAndApiPath_WhenDefaultAccepted()
        {
            _MockFileService
                .Setup(fs => fs.ValidateWritePermissions(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            TestConsole console = new();
            console.Interactive();
            console.Input.PushKey(ConsoleKey.Enter);

            InstallOptionsModel options = new();
            LocationStep step = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                options);

            await step.Execute();

            Assert.AreEqual(
                @"C:\Server Backup Tool",
                options.InstallPath);
            Assert.AreEqual(
                @"C:\Server Backup Tool.API",
                options.ApiInstallPath);
        }
    }
}
