// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Steps;
using System.Runtime.Versioning;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class ScheduledTaskStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<ITaskSchedulerService> _MockTaskScheduler = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockTaskScheduler = new Mock<ITaskSchedulerService>();
        }

        /// <summary>
        /// Checks that Execute completes without exception when the scheduled task is created successfully.
        /// </summary>
        [TestMethod]
        public void Execute_Completes_WhenTaskCreatedSuccessfully()
        {
            _MockTaskScheduler
                .Setup(ts => ts.CreateScheduledTask(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((true, (Exception?)null));

            InstallOptionsModel options = new()
            {
                InstallPath = @"C:\Server Backup Tool"
            };

            ScheduledTaskStep step = new(
                _MockLogger.Object,
                _MockTaskScheduler.Object,
                options);

            step.Execute();
        }

        /// <summary>
        /// Checks that Execute throws InvalidOperationException when the scheduled task creation fails.
        /// </summary>
        [TestMethod]
        public void Execute_ThrowsInvalidOperationException_WhenTaskCreationFails()
        {
            _MockTaskScheduler
                .Setup(ts => ts.CreateScheduledTask(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((false, new Exception("Access denied")));

            InstallOptionsModel options = new()
            {
                InstallPath = @"C:\Server Backup Tool"
            };

            ScheduledTaskStep step = new(
                _MockLogger.Object,
                _MockTaskScheduler.Object,
                options);

            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(
                () => step.Execute());

            Assert.AreEqual(
                "Failed to create scheduled task.",
                exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual(
                "Access denied",
                exception.InnerException.Message);
        }
    }
}
