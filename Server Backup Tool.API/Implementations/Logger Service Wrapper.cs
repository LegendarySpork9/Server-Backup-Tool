// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Services;
using ServerBackupTool.API.Abstractions;

namespace ServerBackupTool.API.Implementations
{
    public class LoggerServiceWrapper : ILoggerService
    {
        private string IPAddress;

        public LoggerServiceWrapper(string ipAddress)
        {
            IPAddress = ipAddress;
        }

        /// <summary>
        /// Changes the identifier of the logger.
        /// </summary>
        public void ChangeIdentifier(string value) => IPAddress = value;

        /// <summary>
        /// Logs the given message to the log file.
        /// </summary>
        public void LogMessage(
            string level,
            string message)
        {
            LoggerService _logger = new(
                IPAddress,
                "Logs");
            _logger.LogMessage(
                level,
                message);
        }
    }
}
