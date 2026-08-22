// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Services;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the logger service.
    /// </summary>
    public interface ILoggerService
    {
        void SetLogService(LogService logService);
        void LogToolMessage(string level, string message, bool serverRunning = false);
        void LogServerMessage(string message);
    }
}
