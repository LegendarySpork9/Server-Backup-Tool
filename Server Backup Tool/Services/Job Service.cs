// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Converters;
using ServerBackupTool.Models.Configuration;

namespace ServerBackupTool.Services
{
    public class JobService
    {
        readonly ILoggerService _Logger;
        readonly IFileSystem _FileSystem;
        readonly IClock _Clock;
        readonly string ServerFilePath;
        readonly string Game;

        // Sets the class's global variables.
        public JobService(ILoggerService _logger, IFileSystem _fileSystem, IClock _clock, SBTSection _configurationSection)
        {
            _Logger = _logger;
            _FileSystem = _fileSystem;
            _Clock = _clock;
            ServerFilePath = _configurationSection.ServerDetails.Location;
            Game = _configurationSection.ServerDetails.Game;
        }

        // Executes the given method.
        public string RunJobs(string job)
        {
            string result = "Complete";

            switch (job)
            {
                case "backup": result = BackupServer(); break;
                case "archive": result = ArchiveLogs(); break;
                case "clean": result = RemoveOldFiles(); break;
                default: break;
            }

            return result;
        }

        // Creates the directory if it does not exist.
        private void CheckDirectory(string path)
        {
            if (!_FileSystem.DirectoryExists(path))
            {
                _FileSystem.CreateDirectory(path);
            }
        }

        // Creates a ZIP file of the world data.
        private string BackupServer()
        {
            JobConverter _jobConverter = new(_Clock);

            string result = "Complete";
            (string source, string destination) = _jobConverter.GetBackPaths(Game, ServerFilePath);

            CheckDirectory(@$"{ServerFilePath}\Backups");

            try
            {
                _FileSystem.CreateZIPFromDirectory(source, destination);
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to create a backup of the game data.");
                _Logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Failed";
            }

            return result;
        }

        // Creates a ZIP of the log files.
        private string ArchiveLogs()
        {
            string result = "Complete";
            string[] files = _FileSystem.GetFiles(@".\Logs").ToArray();

            CheckDirectory(@$".\Archived Logs");

            try
            {
                string zipPath = @$".\Archived Logs\Server {_Clock.UtcNow:dd-MM-yyyy}.zip";

                _FileSystem.CreateZIPFile(zipPath);
                
                foreach (string logFile in files)
                {
                    if (!logFile.Contains("Backup.log"))
                    {
                        _FileSystem.CreateZIPEntryFromFile(zipPath, logFile, Path.GetFileName(logFile));
                        _FileSystem.DeleteFile(logFile);
                    }
                }
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to archive the logs into a ZIP file.");
                _Logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Failed";
            }

            return result;
        }

        // Deletes logs older than a given time.
        private string RemoveOldFiles()
        {
            string result = "Complete";

            CheckDirectory(@$".\Archived Logs");

            string[] archivedLogs = _FileSystem.GetFiles(@".\Archived Logs").ToArray();

            try
            {
                foreach (string archivedLog in archivedLogs)
                {
                    if (_FileSystem.GetCreationTime(archivedLog) < _Clock.UtcNow.AddDays(-10))
                    {
                        _FileSystem.DeleteFile(archivedLog);
                    }
                }

                string[] backups = _FileSystem.GetFiles(@$"{ServerFilePath}\Backups").ToArray();

                foreach (string backup in backups)
                {
                    if (_FileSystem.GetCreationTime(backup) < _Clock.UtcNow.AddDays(-10))
                    {
                        _FileSystem.DeleteFile(backup);
                    }
                }
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to delete logs and/or data backups over 10 days old.");
                _Logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Failed";
            }

            return result;
        }
    }
}
