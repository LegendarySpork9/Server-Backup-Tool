// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for the Windows Task Scheduler service.
    /// </summary>
    public interface ITaskSchedulerService
    {
        (bool, Exception?) CreateScheduledTask(string taskName, string executablePath);
        (bool, Exception?) RemoveScheduledTask(string taskName);
        bool TaskExists(string taskName);
    }
}
