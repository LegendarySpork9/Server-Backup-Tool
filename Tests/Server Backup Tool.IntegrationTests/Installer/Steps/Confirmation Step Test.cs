// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Steps;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class ConfirmationStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
        }

        /// <summary>
        /// Creates a fully populated InstallOptionsModel for testing.
        /// </summary>
        private static InstallOptionsModel CreatePopulatedOptions()
        {
            return new InstallOptionsModel
            {
                Components = ["Server Backup Tool"],
                InstallPath = @"C:\Server Backup Tool",
                ApiInstallPath = @"C:\Server Backup Tool.API",
                ToolTaskName = "Server Backup Tool - TestServer",
                ApiTaskName = string.Empty,
                ServerConfig = new ServerConfigModel
                {
                    ServerName = "TestServer",
                    Game = "Minecraft",
                    ServerDirectory = @"C:\GameServer",
                    StartFile = "start.bat",
                    IPAddress = "192.168.1.1",
                    DatabasePath = @"C:\ProgramData\Data.db"
                },
                TimerConfig = new TimerConfigModel
                {
                    BackupTime = "03:00:00",
                    CustomTimers = []
                }
            };
        }

        /// <summary>
        /// Checks that Execute completes without exception when the user confirms.
        /// </summary>
        [TestMethod]
        public void Execute_Completes_WhenUserConfirms()
        {
            TestConsole console = new();
            console.Interactive();
            console.Input.PushTextWithEnter("y");

            InstallOptionsModel options = CreatePopulatedOptions();
            ConfirmationStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();
        }

        /// <summary>
        /// Checks that Execute throws OperationCanceledException when the user cancels.
        /// </summary>
        [TestMethod]
        public void Execute_ThrowsOperationCanceledException_WhenUserCancels()
        {
            TestConsole console = new();
            console.Interactive();
            console.Input.PushTextWithEnter("n");

            InstallOptionsModel options = CreatePopulatedOptions();
            ConfirmationStep step = new(
                console,
                _MockLogger.Object,
                options);

            Assert.ThrowsException<OperationCanceledException>(
                () => step.Execute());
        }
    }
}
