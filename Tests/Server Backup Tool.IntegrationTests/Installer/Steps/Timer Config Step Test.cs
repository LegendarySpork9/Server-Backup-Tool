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

            // Backup time
            console.Input.PushTextWithEnter("03:00:00");
            // Add custom timer? No
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

            // Backup time
            console.Input.PushTextWithEnter("03:00:00");
            // Add custom timer? Yes
            console.Input.PushTextWithEnter("y");
            // Timer name
            console.Input.PushTextWithEnter("Restart Warning");
            // Timer time
            console.Input.PushTextWithEnter("02:55:00");
            // Timer message
            console.Input.PushTextWithEnter("Server restarting in 5 minutes");
            // Add another custom timer? No
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
        /// <summary>
        /// Checks that Execute rejects a duplicate custom timer name and accepts the corrected name.
        /// </summary>
        [TestMethod]
        public void Execute_RejectsDuplicateTimerName_AndAcceptsCorrectedName()
        {
            TestConsole console = new();
            console.Interactive();

            // Backup time
            console.Input.PushTextWithEnter("03:00:00");
            // Add custom timer? Yes
            console.Input.PushTextWithEnter("y");
            // First timer name
            console.Input.PushTextWithEnter("Warning One");
            // First timer time
            console.Input.PushTextWithEnter("02:00:00");
            // First timer message
            console.Input.PushTextWithEnter("Two hours left");
            // Add another custom timer? Yes
            console.Input.PushTextWithEnter("y");
            // Duplicate timer name (rejected)
            console.Input.PushTextWithEnter("Warning One");
            // Corrected timer name
            console.Input.PushTextWithEnter("Warning Two");
            // Second timer time
            console.Input.PushTextWithEnter("01:00:00");
            // Second timer message
            console.Input.PushTextWithEnter("One hour left");
            // Add another custom timer? No
            console.Input.PushTextWithEnter("n");

            InstallOptionsModel options = new();
            TimerConfigStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();

            Assert.AreEqual(
                2,
                options.TimerConfig.CustomTimers.Count);
            Assert.AreEqual(
                "Warning One",
                options.TimerConfig.CustomTimers[0].Name);
            Assert.AreEqual(
                "Warning Two",
                options.TimerConfig.CustomTimers[1].Name);
        }

        /// <summary>
        /// Checks that Execute rejects a reserved system timer name and accepts the corrected name.
        /// </summary>
        [TestMethod]
        public void Execute_RejectsReservedTimerName_AndAcceptsCorrectedName()
        {
            TestConsole console = new();
            console.Interactive();

            // Backup time
            console.Input.PushTextWithEnter("03:00:00");
            // Add custom timer? Yes
            console.Input.PushTextWithEnter("y");
            // Reserved timer name (rejected)
            console.Input.PushTextWithEnter("Backup");
            // Corrected timer name
            console.Input.PushTextWithEnter("Backup Warning");
            // Timer time
            console.Input.PushTextWithEnter("02:00:00");
            // Timer message
            console.Input.PushTextWithEnter("Backup starting soon");
            // Add another custom timer? No
            console.Input.PushTextWithEnter("n");

            InstallOptionsModel options = new();
            TimerConfigStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();

            Assert.AreEqual(
                1,
                options.TimerConfig.CustomTimers.Count);
            Assert.AreEqual(
                "Backup Warning",
                options.TimerConfig.CustomTimers[0].Name);
        }
    }
}
