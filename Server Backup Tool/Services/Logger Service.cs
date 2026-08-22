// Copyright © - 31/10/2024 - Toby Hunter
using log4net;
using ServerBackupTool.Common.Values;

namespace ServerBackupTool.Services
{
    public class LoggerService
    {
        private readonly ILog ToolLogger = LogManager.GetLogger("ToolLogs");
        private readonly ILog ServerLogger = LogManager.GetLogger("ServerLogs");
        private readonly LogService _LogService;

        // Set's the class's global variables.
        public LoggerService(LogService _logService)
        {
            _LogService = _logService;
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
                    _LogService.LogMessage(
                            level,
                            "Tool",
                            message)
                        .GetAwaiter()
                        .GetResult();
                    DisplayCommandsMessage(serverRunning);
                    break;
                case "Debug":
                    ToolLogger.Debug(message);
                    _LogService.LogMessage(
                            level,
                            "Tool",
                            message)
                        .GetAwaiter()
                        .GetResult();
                    DisplayCommandsMessage(serverRunning);
                    break;
                case "Warn":
                    ToolLogger.Warn(message);
                    _LogService.LogMessage(
                            level,
                            "Tool",
                            message)
                        .GetAwaiter()
                        .GetResult();
                    DisplayCommandsMessage(serverRunning);
                    break;
                case "Error":
                    ToolLogger.Error(message);
                    _LogService.LogMessage(
                            level,
                            "Tool",
                            message)
                        .GetAwaiter()
                        .GetResult();
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
                    _LogService.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Server",
                        logEntry)
                    .GetAwaiter()
                    .GetResult();
                    break;
                case String when logEntry.Contains("/WARN]"):
                    ServerLogger.Warn(logEntry);
                    _LogService.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "Server",
                        logEntry)
                    .GetAwaiter()
                    .GetResult();
                    break;
                case String when logEntry.Contains("/ERROR]"):
                    ServerLogger.Error(logEntry);
                    _LogService.LogMessage(
                        StandardValues.LoggerValues.Error,
                        "Server",
                        logEntry)
                    .GetAwaiter()
                    .GetResult();
                    break;
                case String when logEntry.Contains("/DEBUG]"):
                    ServerLogger.Debug(logEntry);
                    _LogService.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        "Server",
                        logEntry)
                    .GetAwaiter()
                    .GetResult();
                    break;
                default: ServerLogger.Info(logEntry); break;
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
