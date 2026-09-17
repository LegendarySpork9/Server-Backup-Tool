// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Steps;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class TimerConfigStepTest
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
        /// Checks that Execute sets the backup time and leaves custom timers empty when declined.
        /// </summary>
        [TestMethod]
        public void Execute_SetsBackupTime_AndNoCustomTimers()
        {
            TestConsole console = new();
            console.Interactive();
            console.Input.PushTextWithEnter("03:00:00");
            console.Input.PushTextWithEnter("n");

            InstallOptionsModel options = new();
            TimerConfigStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();

            Assert.AreEqual(
                "03:00:00",
                options.TimerConfig.BackupTime);
            Assert.AreEqual(
                0,
                options.TimerConfig.CustomTimers.Count);
        }
    }
}
