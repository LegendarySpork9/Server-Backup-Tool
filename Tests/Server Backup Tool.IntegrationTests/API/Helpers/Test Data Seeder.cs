// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using System.IO.Compression;

namespace ServerBackupTool.IntegrationTests.API.Helpers
{
    public static class TestDataSeeder
    {
        /// <summary>
        /// Inserts the given number of log rows into the Logs table.
        /// </summary>
        public static void SeedLogs(
            string dbConnStr,
            int count,
            string serverName = "TestServer",
            string level = "Info",
            string logger = "Tool")
        {
            using SqliteConnection connection = new(dbConnStr);
            connection.Open();

            for (int i = 0; i < count; i++)
            {
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO Logs (ServerName, Timestamp, Level, Logger, Message)
                    VALUES (@serverName, @timestamp, @level, @logger, @message)";

                cmd.Parameters.AddWithValue(
                    "@serverName",
                    serverName);
                cmd.Parameters.AddWithValue(
                    "@timestamp",
                    DateTime.UtcNow.AddMinutes(-count + i).ToString("o"));
                cmd.Parameters.AddWithValue(
                    "@level",
                    level);
                cmd.Parameters.AddWithValue(
                    "@logger",
                    logger);
                cmd.Parameters.AddWithValue(
                    "@message",
                    $"Test log message {i + 1}");

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Deletes all rows from the Logs table.
        /// </summary>
        public static void ClearLogs(string dbConnStr)
        {
            using SqliteConnection connection = new(dbConnStr);
            connection.Open();

            using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Logs";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates a test archive ZIP file containing the given log files.
        /// </summary>
        public static void CreateTestArchive(
            string archiveDir,
            string archiveName,
            Dictionary<string, string[]> logFiles)
        {
            string tempDir = Path.Combine(
                Path.GetTempPath(),
                $"SBT_TempArchive_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDir);

            try
            {
                foreach (KeyValuePair<string, string[]> logFile in logFiles)
                {
                    string filePath = Path.Combine(
                        tempDir,
                        logFile.Key);
                    File.WriteAllLines(
                        filePath,
                        logFile.Value);
                }

                string zipPath = Path.Combine(
                    archiveDir,
                    archiveName);

                ZipFile.CreateFromDirectory(
                    tempDir,
                    zipPath);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(
                        tempDir,
                        true);
                }
            }
        }
    }
}
