// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Services;

namespace ServerBackupTool.Implementations
{
    public class LoggerServiceWrapper : ILoggerService
    {
        private readonly LoggerService _Logger;

        // Set's the class's global variables.
        public LoggerServiceWrapper()
        {
            _Logger = new();
        }

        /// <summary>
        /// Sets the log service for database persistence.
        /// </summary>
        public void SetLogService(LogService logService)
        {
            _Logger.SetLogService(logService);
        }

        /// <summary>
        /// Logs the given message to the tool logs.
        /// </summary>
        public void LogToolMessage(
            string level,
            string message,
            bool serverRunning = false)
        {
            _Logger.LogToolMessage(
                level,
                message,
                serverRunning);
        }

        /// <summary>
        /// Logs the given message to the server logs.
        /// </summary>
        public void LogServerMessage(string message)
        {
            _Logger.LogServerMessage(message);
        }
    }
}
