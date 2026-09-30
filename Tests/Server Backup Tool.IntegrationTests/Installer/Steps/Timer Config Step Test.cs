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

        /// <summary>
        /// Checks that Execute adds one custom timer when the user provides timer details then declines adding another.
        /// </summary>
        [TestMethod]
        public void Execute_AddsOneCustomTimer_WhenUserProvidesDetails()
        {
            TestConsole console = new();
            console.Interactive();
            console.Input.PushTextWithEnter("03:00:00");
            console.Input.PushTextWithEnter("y");
            console.Input.PushTextWithEnter("Restart Warning");
            console.Input.PushTextWithEnter("02:55:00");
            console.Input.PushTextWithEnter("Server restarting in 5 minutes");
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
                1,
                options.TimerConfig.CustomTimers.Count);
            Assert.AreEqual(
                "Restart Warning",
                options.TimerConfig.CustomTimers[0].Name);
            Assert.AreEqual(
                "02:55:00",
                options.TimerConfig.CustomTimers[0].Time);
            Assert.AreEqual(
                "Server restarting in 5 minutes",
                options.TimerConfig.CustomTimers[0].Message);
        }
    }
}
