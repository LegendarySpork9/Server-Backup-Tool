// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Tests.Functions;
using System.IO.Compression;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class JobServiceTest
    {
        // Checks whether the CreateDirectory method returns the created directory information.
        [TestMethod]
        public void TestDirectory()
        {
            DirectoryInfo directoryObject = Directory.CreateDirectory(Path.Combine(DirectoryFunction.GetBaseDirectory(), "Output"));

            Assert.IsNotNull(directoryObject);
        }

        // Checks whether the CreateFromDirectory method creates a backup of the world folder.
        [TestMethod]
        public void TestBackup()
        {
            string directory = DirectoryFunction.GetBaseDirectory();

            try
            {
                ZipFile.CreateFromDirectory(Path.Combine(directory, @"Mocks\Jobs\world"), Path.Combine(directory, @$"Output\world {DateTime.Now:dd-MM-yyyy}.zip"));

                Assert.IsTrue(true);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to create backup. Exception: {ex.Message}");
            }
        }

        // Checks whether the logs can be archived correctly.
        [TestMethod]
        public void TestArchive()
        {
            string directory = DirectoryFunction.GetBaseDirectory();

            try
            {
                string[] files = Directory.GetFiles(Path.Combine(directory, @"Mocks\Jobs\Logs"));
                string[] zippedFiles = Array.Empty<string>();

                var zip = ZipFile.Open(Path.Combine(directory, @$"Output\Server {DateTime.Now:dd-MM-yyyy}.zip"), ZipArchiveMode.Create);

                foreach (var logFile in files)
                {
                    if (!logFile.Contains("Backup.log"))
                    {
                        zip.CreateEntryFromFile(logFile, Path.GetFileName(logFile), CompressionLevel.Optimal);
                        zippedFiles = zippedFiles.Append(logFile).ToArray();
                    }
                }

                zip.Dispose();

                Assert.IsTrue(!zippedFiles.Contains(Path.Combine(directory, @"Mocks\Jobs\Logs\Server Backup.log")));
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to archive logs. Exception: {ex.Message}");
            }
        }

        // Checks whether the old files can be removed.
        [TestMethod]
        public void TestCleanUp()
        {
            string directory = DirectoryFunction.GetBaseDirectory();

            try
            {
                string[] files = Directory.GetFiles(Path.Combine(directory, "Output"));

                foreach (var file in files)
                {
                    File.Delete(file);
                }

                files = Directory.GetFiles(Path.Combine(directory, "Output"));

                Assert.IsTrue(files.Length == 0);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to delete archives. Exception: {ex.Message}");
            }
        }
    }
}