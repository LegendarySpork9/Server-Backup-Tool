// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Values;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Functions
{
    /// <summary>
    /// Provides helper methods for reading values from installed configuration files.
    /// </summary>
    public static class ConfigurationFunction
    {
        /// <summary>
        /// Reads the database path from the installed App.config, falling back to the default ProgramData location.
        /// </summary>
        public static string GetDatabasePath(
            string installPath,
            IExtendedFileSystem fileSystem,
            ILoggerService logger)
        {
            string configPath = Path.Combine(
                installPath,
                InstallerValues.Defaults.ToolConfigFileName);
            string defaultPath = Path.Combine(
                InstallerValues.Defaults.ProgramDataPath,
                InstallerValues.Defaults.DatabaseFileName);

            string dbPath = defaultPath;

            if (fileSystem.FileExists(configPath))
            {
                try
                {
                    XDocument config = XDocument.Load(configPath);
                    string? configuredPath = config.Root?.Element("serverBackup")?
                        .Element("databaseDetails")?
                        .Attribute("path")?.Value;

                    if (!string.IsNullOrEmpty(configuredPath))
                    {
                        dbPath = configuredPath;
                    }
                }

                catch (Exception ex)
                {
                    logger.LogMessage(
                        Common.Values.StandardValues.LoggerValues.Warning,
                        $"Failed to read database path from config: {ex.Message}. Using default.");
                }
            }

            return dbPath;
        }
    }
}
