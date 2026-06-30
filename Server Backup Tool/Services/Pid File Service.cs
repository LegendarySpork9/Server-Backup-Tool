// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Converters;

namespace ServerBackupTool.Services
{
    public class PidFileService
    {
        readonly ILoggerService _Logger;
        readonly IFileSystem _FileSystem;

        private static readonly string PidDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Hunter Industries",
            "Server Backup Tool");

        // Sets the class's global variables.
        public PidFileService(
            ILoggerService logger,
            IFileSystem fileSystem)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
        }

        /// <summary>
        /// Creates a pid file with the server's process information.
        /// </summary>
        public async Task Write(
            string serverName,
            int processId,
            DateTime startTimeUtc)
        {
            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"Writing PID file for {serverName}.");

            try
            {
                if (!_FileSystem.DirectoryExists(PidDirectory))
                {
                    _FileSystem.CreateDirectory(PidDirectory);
                }

                string filePath = Path.Combine(
                    PidDirectory,
                    $"{serverName}.pid");
                string content = $@"{processId}
{startTimeUtc:O}";

                await _FileSystem.WriteAllText(
                    filePath,
                    content);

                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Info,
                    $"Written PID file for {serverName}.");
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Failed to write PID file for {serverName}.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());
            }
        }

        /// <summary>
        /// Deletes the PID file for the server.
        /// </summary>
        public void Delete(string serverName)
        {
            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"Deleting PID file for {serverName}.");

            try
            {
                string filePath = Path.Combine(
                    PidDirectory,
                    $"{serverName}.pid");

                if (_FileSystem.FileExists(filePath))
                {
                    _FileSystem.DeleteFile(filePath);

                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Info,
                        $"Deleted PID file for {serverName}.");
                }
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Failed to delete PID file for {serverName}.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());
            }
        }
    }
}
