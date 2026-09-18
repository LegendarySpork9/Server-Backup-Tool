// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;

namespace ServerBackupTool.Installer.Steps
{
    public class ScheduledTaskStep
    {
        private readonly ILoggerService _Logger;
        private readonly ITaskSchedulerService _TaskSchedulerService;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public ScheduledTaskStep(
            ILoggerService logger,
            ITaskSchedulerService taskSchedulerService,
            InstallOptionsModel options)
        {
            _Logger = logger;
            _TaskSchedulerService = taskSchedulerService;
            _Options = options;
        }

        /// <summary>
        /// Creates the Windows scheduled task for the Server Backup Tool.
        /// </summary>
        public void Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Creating scheduled task.");

            string executablePath = Path.Combine(
                _Options.InstallPath,
                "ServerBackupTool.exe");

            (bool success, Exception? exception) = _TaskSchedulerService.CreateScheduledTask(
                InstallerValues.ScheduledTask.TaskName,
                executablePath);

            if (!success)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to create scheduled task: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to create scheduled task.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Scheduled task created.");
        }
    }
}
