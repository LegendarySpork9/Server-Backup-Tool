// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;

namespace ServerBackupTool.IntegrationTests.Tool.Services
{
    [TestClass]
    public class JobServiceTest
    {
        private string TempBaseDir = null!;
        private string OriginalDir = null!;
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IClock> _MockClock = null!;
        private IExtendedFileSystem _FileSystem = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            TempBaseDir = Path.Combine(
                Path.GetTempPath(),
                $"SBT_JobTest_{Guid.NewGuid():N}");

            Directory.CreateDirectory(TempBaseDir);

            OriginalDir = Directory.GetCurrentDirectory();
            _MockLogger = new Mock<ILoggerService>();
            _MockClock = new Mock<IClock>();
            _MockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));
            _FileSystem = new ExtendedFileSystemWrapper();
        }

        /// <summary>
        /// Cleans up the test environment.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            Directory.SetCurrentDirectory(OriginalDir);

            if (Directory.Exists(TempBaseDir))
            {
                try
                {
                    Directory.Delete(TempBaseDir, true);
                }

                catch
                {

                }
            }
        }

        /// <summary>
        /// Creates a test configuration section with the given server location.
        /// </summary>
        private static SBTSection CreateSection(string location)
        {
            SBTSection section = new()
            {
                ServerDetails = new()
                {
                    Name = "TestServer",
                    Game = "Minecraft",
                    Location = location,
                    StartFile = "Start.bat",
                    IPAddress = "127.0.0.1"
                }
            };

            return section;
        }

        /// <summary>
        /// Checks that a backup job creates a ZIP file.
        /// </summary>
        [TestMethod]
        public void RunJobs_Backup_CreatesZipFile()
        {
            string serverLocation = Path.Combine(
                TempBaseDir,
                "Server");
            string worldDir = Path.Combine(
                serverLocation,
                "world");
            string backupsDir = Path.Combine(
                serverLocation,
                "Backups");

            Directory.CreateDirectory(worldDir);
            File.WriteAllText(
                Path.Combine(
                    worldDir,
                    "level.dat"),
                "test data");

            SBTSection section = CreateSection(serverLocation);

            JobService jobService = new(
                _MockLogger.Object,
                _FileSystem,
                _MockClock.Object,
                section);

            string result = jobService.RunJobs("backup");

            Assert.AreEqual(
                "Complete",
                result);
            Assert.IsTrue(Directory.Exists(backupsDir));

            string[] zipFiles = Directory.GetFiles(
                backupsDir,
                "*.zip");
            Assert.AreEqual(
                1,
                zipFiles.Length);
        }

        /// <summary>
        /// Checks that an archive job creates a ZIP and deletes the originals.
        /// </summary>
        [TestMethod]
        public void RunJobs_Archive_CreatesArchiveAndDeletesOriginals()
        {
            Directory.SetCurrentDirectory(TempBaseDir);

            string logsDir = Path.Combine(
                TempBaseDir,
                "Logs");
            Directory.CreateDirectory(logsDir);
            File.WriteAllText(
                Path.Combine(
                    logsDir,
                    "server.log"),
                "test log content");
            File.WriteAllText(
                Path.Combine(
                    logsDir,
                    "tool.log"),
                "test tool content");

            string serverLocation = Path.Combine(
                TempBaseDir,
                "Server");
            Directory.CreateDirectory(serverLocation);

            SBTSection section = CreateSection(serverLocation);

            JobService jobService = new(
                _MockLogger.Object,
                _FileSystem,
                _MockClock.Object,
                section);

            string result = jobService.RunJobs("archive");

            Assert.AreEqual(
                "Complete",
                result);

            string archivedLogsDir = Path.Combine(
                TempBaseDir,
                "Archived Logs");
            Assert.IsTrue(Directory.Exists(archivedLogsDir));

            string[] zipFiles = Directory.GetFiles(
                archivedLogsDir,
                "*.zip");
            Assert.AreEqual(
                1,
                zipFiles.Length);

            Assert.IsFalse(File.Exists(Path.Combine(
                logsDir,
                "server.log")));
            Assert.IsFalse(File.Exists(Path.Combine(
                logsDir,
                "tool.log")));
        }

        /// <summary>
        /// Checks that a cleanup job deletes old files.
        /// </summary>
        [TestMethod]
        public void RunJobs_CleanUp_DeletesOldFiles()
        {
            Directory.SetCurrentDirectory(TempBaseDir);

            string archivedLogsDir = Path.Combine(
                TempBaseDir,
                "Archived Logs");
            Directory.CreateDirectory(archivedLogsDir);

            string oldArchive = Path.Combine(
                archivedLogsDir,
                "old-archive.zip");
            File.WriteAllText(
                oldArchive,
                "old data");
            File.SetCreationTimeUtc(
                oldArchive,
                DateTime.UtcNow.AddDays(-15));

            string serverLocation = Path.Combine(
                TempBaseDir,
                "Server");
            string backupsDir = Path.Combine(
                serverLocation,
                "Backups");
            Directory.CreateDirectory(backupsDir);

            string oldBackup = Path.Combine(
                backupsDir,
                "old-backup.zip");
            File.WriteAllText(
                oldBackup,
                "old backup data");
            File.SetCreationTimeUtc(
                oldBackup,
                DateTime.UtcNow.AddDays(-15));

            SBTSection section = CreateSection(serverLocation);

            _MockClock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

            JobService jobService = new(
                _MockLogger.Object,
                _FileSystem,
                _MockClock.Object,
                section);

            string result = jobService.RunJobs("clean");

            Assert.AreEqual(
                "Complete",
                result);
            Assert.IsFalse(File.Exists(oldArchive));
            Assert.IsFalse(File.Exists(oldBackup));
        }

        /// <summary>
        /// Checks that a cleanup job keeps recent files.
        /// </summary>
        [TestMethod]
        public void RunJobs_CleanUp_KeepsRecentFiles()
        {
            Directory.SetCurrentDirectory(TempBaseDir);

            string archivedLogsDir = Path.Combine(
                TempBaseDir,
                "Archived Logs");
            Directory.CreateDirectory(archivedLogsDir);

            string recentArchive = Path.Combine(
                archivedLogsDir,
                "recent-archive.zip");
            File.WriteAllText(
                recentArchive,
                "recent data");
            File.SetCreationTimeUtc(
                recentArchive,
                DateTime.UtcNow.AddDays(-2));

            string serverLocation = Path.Combine(
                TempBaseDir,
                "Server");
            string backupsDir = Path.Combine(
                serverLocation,
                "Backups");
            Directory.CreateDirectory(backupsDir);

            string recentBackup = Path.Combine(
                backupsDir,
                "recent-backup.zip");
            File.WriteAllText(
                recentBackup,
                "recent backup data");
            File.SetCreationTimeUtc(
                recentBackup,
                DateTime.UtcNow.AddDays(-2));

            SBTSection section = CreateSection(serverLocation);

            _MockClock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

            JobService jobService = new(
                _MockLogger.Object,
                _FileSystem,
                _MockClock.Object,
                section);

            string result = jobService.RunJobs("clean");

            Assert.AreEqual(
                "Complete",
                result);
            Assert.IsTrue(File.Exists(recentArchive));
            Assert.IsTrue(File.Exists(recentBackup));
        }

        /// <summary>
        /// Checks that an unknown job returns Complete without error.
        /// </summary>
        [TestMethod]
        public void RunJobs_Unknown_ReturnsComplete()
        {
            string serverLocation = Path.Combine(
                TempBaseDir,
                "Server");
            Directory.CreateDirectory(serverLocation);

            SBTSection section = CreateSection(serverLocation);

            JobService jobService = new(
                _MockLogger.Object,
                _FileSystem,
                _MockClock.Object,
                section);

            string result = jobService.RunJobs("unknown");

            Assert.AreEqual(
                "Complete",
                result);
        }
    }
}
