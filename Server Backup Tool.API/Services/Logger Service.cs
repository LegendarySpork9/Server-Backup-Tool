// Copyright © - Unpublished - Toby Hunter
using log4net;

namespace ServerBackupTool.API.Services
{
    public class LoggerService
    {
        private readonly string Identifier;
        private readonly ILog Logger;

        // Sets the class's global variables.
        public LoggerService(
            string id,
            string logAppender)
        {
            Identifier = id;
            Logger = LogManager.GetLogger(logAppender);
        }

        /// <summary>
        /// Adds the message to the log file and SQL table.
        /// </summary>
        public void LogMessage(
            string level,
            string message)
        {
            switch (level)
            {
                case "Info":
                    Logger.Info($"{Identifier} - {message.Trim()}");
                    break;
                case "Debug":
                    Logger.Debug($"{Identifier} - {message.Trim()}");
                    break;
                case "Warn":
                    Logger.Warn($"{Identifier} - {message.Trim()}");
                    break;
                case "Error":
                    Logger.Error($"{Identifier} - {message.Trim()}");
                    break;
            }
        }
    }
}
