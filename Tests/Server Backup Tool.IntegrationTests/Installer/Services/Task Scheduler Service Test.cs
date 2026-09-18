// Copyright © - Unpublished - Toby Hunter
using Microsoft.Win32.TaskScheduler;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using System.Runtime.Versioning;

namespace ServerBackupTool.IntegrationTests.Installer.Services
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class TaskSchedulerServiceTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private TaskSchedulerService _TaskSchedulerService = null!;
        private string _TestPrefix = null!;
        private readonly List<string> _CreatedTasks = [];

        /// <summary>
        /// Initialises the test dependencies and a unique task prefix per test run.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _TaskSchedulerService = new TaskSchedulerService(_MockLogger.Object);
            _TestPrefix = $"SBT_Test_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Cleans up any scheduled tasks created during the test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            using TaskService taskService = new();

            foreach (string taskName in _CreatedTasks)
            {
                try
                {
                    taskService.RootFolder.DeleteTask(
                        taskName,
                        false);
                }

                catch
                {

                }
            }
        }

        /// <summary>
        /// Checks that CreateScheduledTask returns true on success.
        /// </summary>
        [TestMethod]
        public void CreateScheduledTask_ReturnsTrue_OnSuccess()
        {
            string taskName = $"{_TestPrefix}_Create";
            _CreatedTasks.Add(taskName);

            (bool created, Exception? error) = _TaskSchedulerService.CreateScheduledTask(
                taskName,
                @"C:\Windows\System32\cmd.exe");

            if (!created && error is UnauthorizedAccessException)
            {
                Assert.Inconclusive("Test requires elevated privileges.");
            }

            Assert.IsTrue(created);
            Assert.IsNull(error);
        }

        /// <summary>
        /// Checks that CreateScheduledTask creates a task that exists in the scheduler.
        /// </summary>
        [TestMethod]
        public void CreateScheduledTask_CreatesTaskThatExists()
        {
            string taskName = $"{_TestPrefix}_Exists";
            _CreatedTasks.Add(taskName);

            (bool created, Exception? error) = _TaskSchedulerService.CreateScheduledTask(
                taskName,
                @"C:\Windows\System32\cmd.exe");

            if (!created && error is UnauthorizedAccessException)
            {
                Assert.Inconclusive("Test requires elevated privileges.");
            }

            bool exists = _TaskSchedulerService.TaskExists(taskName);

            Assert.IsTrue(exists);
        }

        /// <summary>
        /// Checks that TaskExists returns false for a non-existent task.
        /// </summary>
        [TestMethod]
        public void TaskExists_ReturnsFalse_ForNonExistentTask()
        {
            string taskName = $"{_TestPrefix}_NonExistent";

            bool exists = _TaskSchedulerService.TaskExists(taskName);

            Assert.IsFalse(exists);
        }

        /// <summary>
        /// Checks that RemoveScheduledTask returns true on success.
        /// </summary>
        [TestMethod]
        public void RemoveScheduledTask_ReturnsTrue_OnSuccess()
        {
            string taskName = $"{_TestPrefix}_Remove";
            _CreatedTasks.Add(taskName);

            (bool created, Exception? createError) = _TaskSchedulerService.CreateScheduledTask(
                taskName,
                @"C:\Windows\System32\cmd.exe");

            if (!created && createError is UnauthorizedAccessException)
            {
                Assert.Inconclusive("Test requires elevated privileges.");
            }

            (bool removed, Exception? error) = _TaskSchedulerService.RemoveScheduledTask(taskName);

            Assert.IsTrue(removed);
            Assert.IsNull(error);
            Assert.IsFalse(_TaskSchedulerService.TaskExists(taskName));
        }

        /// <summary>
        /// Checks that RemoveScheduledTask returns true for a non-existent task.
        /// </summary>
        [TestMethod]
        public void RemoveScheduledTask_ReturnsTrue_ForNonExistentTask()
        {
            string taskName = $"{_TestPrefix}_NonExistentRemove";

            (bool removed, Exception? error) = _TaskSchedulerService.RemoveScheduledTask(taskName);

            Assert.IsTrue(removed);
            Assert.IsNull(error);
        }
    }
}
