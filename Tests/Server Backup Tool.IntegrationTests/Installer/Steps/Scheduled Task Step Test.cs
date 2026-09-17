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
    }
}
