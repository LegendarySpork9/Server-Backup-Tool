// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class JobServiceTest
    {
        // Checks whether the RunJobs method creates a backup of the world folder when the backup type is specified.
        [TestMethod]
        public void TestRunJobsBackup()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.CreateZIPFromDirectory(It.IsAny<string>(), It.IsAny<string>()));
            Mock<IClock> _mockClock = new();
            _mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 01, 01));

            SBTSection serverBackupSection = new()
            {
                ServerDetails = new ServerDetailsElement
                {
                    Location = @"C:\Server",
                    Game = "Minecraft"
                }
            };

            JobService _jobService = new(_mockLogger.Object, _mockFileSystem.Object, _mockClock.Object, serverBackupSection);

            string expected = "Complete";

            string actual = _jobService.RunJobs("backup");

            Assert.AreEqual(expected, actual);
            _mockFileSystem.Verify(fs => fs.CreateZIPFromDirectory(
                It.Is<string>(s => s.Contains("world")),
                It.Is<string>(d => d.Contains("Backups"))),
                Times.Once);
        }

        // Checks whether the RunJobs method archives the server logs when the archive type is specified.
        [TestMethod]
        public void TestRunJobsArchive()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>())).Returns(new[] { "Server.log", "Backup.log" });
            _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            Mock<IClock> _mockClock = new();
            _mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 01, 01));

            SBTSection serverBackupSection = new()
            {
                ServerDetails = new ServerDetailsElement
                {
                    Location = @"C:\Server",
                    Game = "Minecraft"
                }
            };

            JobService _jobService = new(_mockLogger.Object, _mockFileSystem.Object, _mockClock.Object, serverBackupSection);

            string expected = "Complete";

            string actual = _jobService.RunJobs("archive");

            Assert.AreEqual(expected, actual);
            _mockFileSystem.Verify(fs => fs.CreateZIPFile(It.IsAny<string>()), Times.Once);
            _mockFileSystem.Verify(fs => fs.CreateZIPEntryFromFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // Checks whether the RunJobs method deletes old log archives when the clean type is specified.
        [TestMethod]
        public void TestRunJobsCleanUp()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.GetFiles(@".\Archived Logs")).Returns(new[] { "old.zip" });
            _mockFileSystem.Setup(fs => fs.GetFiles(@"C:\Server\Backups")).Returns(new[] { "oldbackup.zip" });
            _mockFileSystem.Setup(fs => fs.GetCreationTime(It.IsAny<string>())).Returns(new DateTime(2025, 01, 01).AddDays(-11));
            Mock<IClock> _mockClock = new();
            _mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 01, 01));

            SBTSection serverBackupSection = new()
            {
                ServerDetails = new ServerDetailsElement
                {
                    Location = @"C:\Server",
                    Game = "Minecraft"
                }
            };

            JobService _jobService = new(_mockLogger.Object, _mockFileSystem.Object, _mockClock.Object, serverBackupSection);

            string expected = "Complete";

            string actual = _jobService.RunJobs("clean");

            Assert.AreEqual(expected, actual);
            _mockFileSystem.Verify(fs => fs.DeleteFile("old.zip"), Times.Once);
            _mockFileSystem.Verify(fs => fs.DeleteFile("oldbackup.zip"), Times.Once);
        }

        // Checks whether the RunJobs method deletes old log archives when an unknown type is specified.
        [TestMethod]
        public void TestRunJobsUnknown()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            Mock<IClock> _mockClock = new();

            SBTSection serverBackupSection = new();

            JobService _jobService = new(_mockLogger.Object, _mockFileSystem.Object, _mockClock.Object, serverBackupSection);

            string expected = "Complete";

            string actual = _jobService.RunJobs("unknown");

            Assert.AreEqual(expected, actual);
            _mockFileSystem.VerifyNoOtherCalls();
        }
    }
}