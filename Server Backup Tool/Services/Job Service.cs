// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models.Configuration;
using System.IO.Compression;

namespace ServerBackupTool.Services
{
    internal class JobService
    {
        readonly string ServerFilePath;
        readonly string Game;

        // Sets the class's global variables.
        public JobService(SBTSection _configurationSection)
        {
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
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        // Creates a ZIP file of the world data.
        private string BackupServer()
        {
            JobConverter _jobConverter = new();
            LoggerService _logger = new();

            string result = "Complete";
            (string source, string destination) = _jobConverter.GetBackPaths(Game, ServerFilePath);

            CheckDirectory(@$"{ServerFilePath}\Backups");

            try
            {
                ZipFile.CreateFromDirectory(source, destination);
            }

            catch (Exception ex)
            {
                _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to create a backup of the game data.");
                _logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Failed";
            }

            return result;
        }

        // Creates a ZIP of the log files.
        private string ArchiveLogs()
        {
            LoggerService _logger = new();

            string result = "Complete";
            string[] files = Directory.GetFiles(@".\Logs");

            CheckDirectory(@$".\Archived Logs");

            try
            {
                var zip = ZipFile.Open(@$".\Archived Logs\Server {DateTime.UtcNow:dd-MM-yyyy}.zip", ZipArchiveMode.Create);
                
                foreach (var logFile in files)
                {
                    if (!logFile.Contains("Backup.log"))
                    {
                        zip.CreateEntryFromFile(logFile, Path.GetFileName(logFile), CompressionLevel.Optimal);
                        File.Delete(logFile);
                    }
                }

                zip.Dispose();
            }

            catch (Exception ex)
            {
                _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to archive the logs into a ZIP file.");
                _logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Failed";
            }

            return result;
        }

        // Deletes logs older than a given time.
        private string RemoveOldFiles()
        {
            LoggerService _logger = new();

            string result = "Complete";

            CheckDirectory(@$".\Archived Logs");

            string[] archivedLogs = Directory.GetFiles(@".\Archived Logs");

            try
            {
                foreach (var archivedLog in archivedLogs)
                {
                    if (File.GetCreationTime(archivedLog) < DateTime.UtcNow.AddDays(-10))
                    {
                        File.Delete(archivedLog);
                    }
                }

                string[] backups = Directory.GetFiles(@$"{ServerFilePath}\Backups");

                foreach (var backup in backups)
                {
                    if (File.GetCreationTime(backup) < DateTime.UtcNow.AddDays(-10))
                    {
                        File.Delete(backup);
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to delete logs and/or data backups over 10 days old.");
                _logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Failed";
            }

            return result;
        }
    }
}
