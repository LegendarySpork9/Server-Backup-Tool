// Copyright © - unpublished - Toby Hunter
using log4net;
using ServerBackupTool.Models.Configuration;
using System.IO.Compression;

namespace ServerBackupTool.Services
{
    internal class JobService
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");
        readonly string ServerFilePath;

        public JobService(SBTSection _configurationSection)
        {
            ServerFilePath = _configurationSection.ServerDetails.Location;
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
            string result = "Complete";

            CheckDirectory(@$"{ServerFilePath}\Backups");

            try
            {
                ZipFile.CreateFromDirectory(@$"{ServerFilePath}\world", @$"{ServerFilePath}\Backups\world {DateTime.Now:dd-MM-yyyy}.zip");
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Log.Error(ex.ToString());
                result = "Failed";
            }

            return result;
        }

        private string ArchiveLogs()
        {
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
                Console.WriteLine(ex.ToString());
                Log.Error(ex.ToString());
                result = "Failed";
            }

            return result;
        }

        private string RemoveOldFiles()
        {
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
                Console.WriteLine(ex.ToString());
                Log.Error(ex.ToString());
                result = "Failed";
            }

            return result;
        }
    }
}
