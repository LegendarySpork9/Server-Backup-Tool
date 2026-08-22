// Copyright © - 31/10/2024 - Toby Hunter
using log4net;
using ServerBackupTool.Common.Values;

namespace ServerBackupTool.Services
{
    public class LoggerService
    {
        private readonly ILog ToolLogger = LogManager.GetLogger("ToolLogs");
        private readonly ILog ServerLogger = LogManager.GetLogger("ServerLogs");
        private LogService? _LogService;
        private bool _IsPersisting;

        /// <summary>
        /// Sets the log service for database persistence.
        /// </summary>
        public void SetLogService(LogService logService)
        {
            _LogService = logService;
        }

        /// <summary>
        /// Outputs a message to the tool logs.
        /// </summary>
        public void LogToolMessage(
            string level,
            string message,
            bool serverRunning = false)
        {
            switch (level)
            {
                case "Info":
                    ToolLogger.Info(message);
                    PersistMessage(
                        level,
                        "Tool",
                        message);
                    DisplayCommandsMessage(serverRunning);
                    break;
                case "Debug":
                    ToolLogger.Debug(message);
                    PersistMessage(
                        level,
                        "Tool",
                        message);
                    DisplayCommandsMessage(serverRunning);
                    break;
                case "Warn":
                    ToolLogger.Warn(message);
                    PersistMessage(
                        level,
                        "Tool",
                        message);
                    DisplayCommandsMessage(serverRunning);
                    break;
                case "Error":
                    ToolLogger.Error(message);
                    PersistMessage(
                        level,
                        "Tool",
                        message);
                    break;
            }
        }

        /// <summary>
        /// Outputs a message to the server logs.
        /// </summary>
        public void LogServerMessage(string logEntry)
        {
            switch (logEntry)
            {
                case String when logEntry.Contains("/INFO]"):
                    ServerLogger.Info(logEntry);
                    PersistMessage(
                        StandardValues.LoggerValues.Info,
                        "Server",
                        logEntry);
                    break;
                case String when logEntry.Contains("/WARN]"):
                    ServerLogger.Warn(logEntry);
                    PersistMessage(
                        StandardValues.LoggerValues.Warning,
                        "Server",
                        logEntry);
                    break;
                case String when logEntry.Contains("/ERROR]"):
                    ServerLogger.Error(logEntry);
                    PersistMessage(
                        StandardValues.LoggerValues.Error,
                        "Server",
                        logEntry);
                    break;
                case String when logEntry.Contains("/DEBUG]"):
                    ServerLogger.Debug(logEntry);
                    PersistMessage(
                        StandardValues.LoggerValues.Debug,
                        "Server",
                        logEntry);
                    break;
                default:
                    ServerLogger.Info(logEntry);
                    PersistMessage(
                        StandardValues.LoggerValues.Info,
                        "Server",
                        logEntry);
                    break;
            }
        }

        /// <summary>
        /// Persists the message to the database via the log service.
        /// </summary>
        private void PersistMessage(
            string level,
            string type,
            string message)
        {
            if (_LogService != null && !_IsPersisting)
            {
                _IsPersisting = true;

                try
                {
                    _LogService.LogMessage(
                            level,
                            type,
                            message)
                        .GetAwaiter()
                        .GetResult();
                }

                finally
                {
                    _IsPersisting = false;
                }
            }
        }

        /// <summary>
        /// Displays the server commands message on the console.
        /// </summary>
        private void DisplayCommandsMessage(bool serverRunning)
        {
            switch (serverRunning)
            {
                case true: Console.WriteLine("\n----Server Commands----"); break;
            }
        }
    }
}
