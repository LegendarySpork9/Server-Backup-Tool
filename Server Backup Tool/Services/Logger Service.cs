// Copyright © - 31/10/2024 - Toby Hunter
using log4net;

namespace ServerBackupTool.Services
{
    internal class LoggerService
    {
        private readonly ILog ToolLogger = LogManager.GetLogger("ToolLogs");
        private readonly ILog ServerLogger = LogManager.GetLogger("ServerLogs");

        // Outputs a message to the tool logs.
        public void LogToolMessage(string level, string message, bool serverRunning = false)
        {
            switch (level)
            {
                case "Info": ToolLogger.Info(message); DisplayCommandsMessage(serverRunning); break;
                case "Debug": ToolLogger.Debug(message); DisplayCommandsMessage(serverRunning); break;
                case "Warn": ToolLogger.Warn(message); DisplayCommandsMessage(serverRunning); break;
                case "Error": ToolLogger.Error(message); break;
            }
        }

        // Outputs a message to the server logs.
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

        // Displays the server commands message on the console.
        private void DisplayCommandsMessage(bool serverRunning)
        {
            switch (serverRunning)
            {
                case true: Console.WriteLine("\n----Server Commands----"); break;
            }
        }
    }
}
