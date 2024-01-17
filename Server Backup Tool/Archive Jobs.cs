// Copyright © - 17/01/2024 - Toby Hunter
using log4net;
using System.IO.Compression;

namespace Server_Backup_Tool
{
    internal class Archive_Jobs
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");

        public static void BackupServer()
        {
            try
            {
                ZipFile.CreateFromDirectory(@$"{Server_Information.FilePath}\world", @$"{Server_Information.FilePath}\Backups\world {DateTime.Now:dd-MM-yyyy}.zip");
                //ZipFile.CreateFromDirectory(@$"{Server_Information.FilePath}\world", @$"{Server_Information.FilePath}\Backups\world {DateTime.Now:dd-MM-yyyy HH-mm}.zip");
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Log.Error(ex.ToString());
            }
        }

        public static void ArchiveLogs()
        {
            string[] Files = Directory.GetFiles(@".\Logs");

            try
            {
                var ZIP = ZipFile.Open(@$".\Archived Logs\Server {DateTime.Now:dd-MM-yyyy}.zip", ZipArchiveMode.Create);
                //var ZIP = ZipFile.Open(@$".\Archived Logs\Server {DateTime.Now:dd-MM-yyyy HH-mm}.zip", ZipArchiveMode.Create);

                foreach (var LogFile in Files)
                {
                    if (!LogFile.Contains("Backup"))
                    {
                        ZIP.CreateEntryFromFile(LogFile, Path.GetFileName(LogFile), CompressionLevel.Optimal);
                        File.Delete(LogFile);
                    }
                }

                ZIP.Dispose();
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Log.Error(ex.ToString());
            }
        }

        public static void RemoveOldFiles()
        {
            string[] ArchivedLogs = Directory.GetFiles(@".\Archived Logs");

            foreach (var ArchivedLog in ArchivedLogs)
            {
                if (File.GetCreationTime(ArchivedLog) < DateTime.Now.AddDays(-10))
                {
                    File.Delete(ArchivedLog);
                }
            }

            string[] Backups = Directory.GetFiles(@$"{Server_Information.FilePath}\Backups");

            foreach (var Backup in Backups)
            {
                if (File.GetCreationTime(Backup) < DateTime.Now.AddDays(-10))
                {
                    File.Delete(Backup);
                }
            }
        }
    }
}
