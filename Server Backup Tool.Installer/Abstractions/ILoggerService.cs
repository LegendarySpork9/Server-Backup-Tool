// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for the installer logger service.
    /// </summary>
    public interface ILoggerService
    {
        void LogMessage(string level, string message);
    }
}
