// Copyright © - Unpublished - Toby Hunter
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using ServerBackupTool.Common.Values;

namespace ServerBackupTool.Installer.Implementations
{
    public class LoggerServiceWrapper : Abstractions.ILoggerService
    {
        private readonly ILog _Logger;

        // Sets the class's global variables.
        public LoggerServiceWrapper()
        {
            ConfigureLog4Net();
            _Logger = LogManager.GetLogger("InstallerLogs");
        }

        /// <summary>
        /// Logs a message at the specified level.
        /// </summary>
        public void LogMessage(
            string level,
            string message)
        {
            switch (level)
            {
                case StandardValues.LoggerValues.Info:
                    _Logger.Info(message);
                    break;
                case StandardValues.LoggerValues.Debug:
                    _Logger.Debug(message);
                    break;
                case StandardValues.LoggerValues.Warning:
                    _Logger.Warn(message);
                    break;
                case StandardValues.LoggerValues.Error:
                    _Logger.Error(message);
                    break;
            }
        }

        /// <summary>
        /// Configures log4net programmatically with a rolling file appender.
        /// </summary>
        private static void ConfigureLog4Net()
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

            PatternLayout layout = new()
            {
                ConversionPattern = "%d{ISO8601} %level - %message%newline"
            };
            layout.ActivateOptions();

            RollingFileAppender appender = new()
            {
                Name = "InstallerLogAppender",
                File = @"Logs\Installer.log",
                AppendToFile = true,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                MaxSizeRollBackups = 10,
                MaximumFileSize = "10MB",
                StaticLogFileName = true,
                LockingModel = new FileAppender.MinimalLock(),
                Layout = layout
            };
            appender.ActivateOptions();

            Logger logger = hierarchy.GetLogger("InstallerLogs") as Logger ?? throw new InvalidOperationException("Failed to get InstallerLogs logger.");
            logger.Additivity = false;
            logger.AddAppender(appender);
            logger.Level = Level.All;

            hierarchy.Configured = true;
        }
    }
}
