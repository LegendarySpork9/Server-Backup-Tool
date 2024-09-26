// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models.Configuration;
using System.IO.Compression;

namespace ServerBackupTool.Services
{
    internal class JobService
    {
        readonly string ServerFilePath;
        readonly string Game;

        public JobService(SBTSection _configurationSection)
        {
            ServerFilePath = _configurationSection.ServerDetails.Location;
            Game = _configurationSection.ServerDetails.Game;
        }

        public string RunJobs(string? job)
        {
            string result = "Complete";

            if (string.IsNullOrWhiteSpace(job))
            {
                result = BackupServer();
                result = ArchiveLogs();
                result = RemoveOldFiles();
            }

            else
            {
                switch (job)
                {
                    case "backup": result = BackupServer(); break;
                    case "archive": result = ArchiveLogs(); break;
                    case "clean": result = RemoveOldFiles(); break;
                    default: break;
                }
            }

            return result;
        }

        private void CheckDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

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

        private string ArchiveLogs()
        {
            LoggerService _logger = new();

            string result = "Complete";
            string[] files = Directory.GetFiles(@".\Logs");

            CheckDirectory(@$".\Archived Logs");

            try
            {
                var zip = ZipFile.Open(@$".\Archived Logs\Server {DateTime.Now:dd-MM-yyyy}.zip", ZipArchiveMode.Create);
                
                foreach (var logFile in files)
                {
                    if (!logFile.Contains("Backup"))
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
                    if (File.GetCreationTime(archivedLog) < DateTime.Now.AddDays(-10))
                    {
                        File.Delete(archivedLog);
                    }
                }

                string[] backups = Directory.GetFiles(@$"{ServerFilePath}\Backups");

                foreach (var backup in backups)
                {
                    if (File.GetCreationTime(backup) < DateTime.Now.AddDays(-10))
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
