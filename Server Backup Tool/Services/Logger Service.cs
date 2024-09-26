// Copyright © - unpublished - Toby Hunter
using log4net;

namespace ServerBackupTool.Services
{
    internal class LoggerService
    {
        private readonly ILog ToolLogger = LogManager.GetLogger("ToolLogs");
        private readonly ILog ServerLogger = LogManager.GetLogger("ServerLogs");

        public void LogToolMessage(string level, string message)
        {
            switch (level)
            {
                case "Info": ToolLogger.Info(message); break;
                case "Debug": ToolLogger.Debug(message); break;
                case "Warn": ToolLogger.Warn(message); break;
                case "Error": ToolLogger.Error(message); break;
            }
        }

        public void LogServerMessage(string logEntry)
        {
            switch (logEntry)
            {
                case String when logEntry.Contains("/INFO]"): ServerLogger.Info(logEntry); break;
                case String when logEntry.Contains("/WARN]"): ServerLogger.Warn(logEntry); break;
                case String when logEntry.Contains("/ERROR]"): ServerLogger.Error(logEntry); break;
                case String when logEntry.Contains("/DEBUG]"): ServerLogger.Debug(logEntry); break;
                default: ServerLogger.Info(logEntry); break;
            }
        }
    }
}
