// Copyright © - Unpublished - Toby Hunter
using Microsoft.Win32.TaskScheduler;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Values;
using System.Runtime.Versioning;

namespace ServerBackupTool.Installer.Implementations
{
    [SupportedOSPlatform("windows")]
    public class TaskSchedulerService : ITaskSchedulerService
    {
        private readonly ILoggerService _Logger;

        // Sets the class's global variables.
        public TaskSchedulerService(ILoggerService logger)
        {
            _Logger = logger;
        }

        /// <summary>
        /// Creates a scheduled task to run the executable at system startup.
        /// </summary>
        public (bool, Exception?) CreateScheduledTask(
            string taskName,
            string executablePath)
        {
            bool created = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Creating scheduled task '{taskName}'.");

                using (TaskService taskService = new())
                {
                    TaskDefinition definition = taskService.NewTask();
                    definition.RegistrationInfo.Description = "Runs the Server Backup Tool at system startup.";

                    definition.Triggers.Add(new BootTrigger
                    {
                        Delay = TimeSpan.FromMinutes(InstallerValues.ScheduledTask.BootDelayMinutes),
                        Enabled = true
                    });

                    string workingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty;

                    definition.Actions.Add(new ExecAction(
                        executablePath,
                        null,
                        workingDirectory));

                    definition.Principal.LogonType = TaskLogonType.S4U;
                    definition.Principal.RunLevel = TaskRunLevel.LUA;

                    definition.Settings.RestartCount = InstallerValues.ScheduledTask.MaxRestartAttempts;
                    definition.Settings.RestartInterval = TimeSpan.FromMinutes(InstallerValues.ScheduledTask.RestartDelayMinutes);
                    definition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                    definition.Settings.DisallowStartIfOnBatteries = false;
                    definition.Settings.StopIfGoingOnBatteries = false;
                    definition.Settings.AllowHardTerminate = true;
                    definition.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                    definition.Settings.StartWhenAvailable = true;

                    taskService.RootFolder.RegisterTaskDefinition(
                        taskName,
                        definition,
                        TaskCreation.CreateOrUpdate,
                        System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                        null,
                        TaskLogonType.S4U);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Scheduled task created.");

                    created = true;
                }
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Create Scheduled Task: {ex.Message}");

                exception = ex;
            }

            return (
                created,
                exception);
        }

        /// <summary>
        /// Removes the specified scheduled task.
        /// </summary>
        public (bool, Exception?) RemoveScheduledTask(string taskName)
        {
            bool removed = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Removing scheduled task '{taskName}'.");

                using (TaskService taskService = new())
                {
                    taskService.RootFolder.DeleteTask(
                        taskName,
                        false);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Scheduled task removed.");

                    removed = true;
                }
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Remove Scheduled Task: {ex.Message}");

                exception = ex;
            }

            return (
                removed,
                exception);
        }

        /// <summary>
        /// Checks whether the specified scheduled task exists.
        /// </summary>
        public bool TaskExists(string taskName)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Checking if scheduled task '{taskName}' exists.");

            bool exists = false;

            using (TaskService taskService = new())
            {
                exists = taskService.GetTask(taskName) != null;
            }

            return exists;
        }
    }
}
