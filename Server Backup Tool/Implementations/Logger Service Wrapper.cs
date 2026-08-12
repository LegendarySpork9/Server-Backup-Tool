// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Services;

namespace ServerBackupTool.Implementations
{
    public class LoggerServiceWrapper : ILoggerService
    {
        private readonly LoggerService _Logger = new();

        /// <summary>
        /// Logs the given message to the tool logs.
        /// </summary>
        public void LogToolMessage(
            string level,
            string message,
            bool verbose = false)
        {
            _Logger.LogToolMessage(
                level,
                message,
                verbose);
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
