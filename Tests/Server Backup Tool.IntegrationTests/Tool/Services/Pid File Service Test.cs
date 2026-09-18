// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Implementations;
using ServerBackupTool.Services;

namespace ServerBackupTool.IntegrationTests.Tool.Services
{
    [TestClass]
    public class PidFileServiceTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private IExtendedFileSystem _FileSystem = null!;
        private PidFileService _PidService = null!;
        private string ServerName = null!;

        private static readonly string PidDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Hunter Industries",
            "Server Backup Tool");

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _FileSystem = new ExtendedFileSystemWrapper();
            _PidService = new PidFileService(
                _MockLogger.Object,
                _FileSystem);

            ServerName = $"PidTest_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Cleans up the test environment.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            string filePath = Path.Combine(
                PidDirectory,
                $"{ServerName}.pid");

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }

                catch
                {

                }
            }
        }

        /// <summary>
        /// Checks that Write creates the directory and PID file.
        /// </summary>
        [TestMethod]
        public async Task Write_CreatesDirectoryAndFile()
        {
            await _PidService.Write(
                ServerName,
                12345,
                new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));

            string filePath = Path.Combine(
                PidDirectory,
                $"{ServerName}.pid");

            Assert.IsTrue(Directory.Exists(PidDirectory));
            Assert.IsTrue(File.Exists(filePath));
        }

        /// <summary>
        /// Checks that Write creates a file with the correct content format.
        /// </summary>
        [TestMethod]
        public async Task Write_FileContainsCorrectFormat()
        {
            int processId = 54321;
            DateTime startTime = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            await _PidService.Write(
                ServerName,
                processId,
                startTime);

            string filePath = Path.Combine(
                PidDirectory,
                $"{ServerName}.pid");
            string content = await File.ReadAllTextAsync(filePath);

            Assert.IsTrue(content.Contains(processId.ToString()));
            Assert.IsTrue(content.Contains(startTime.ToString("O")));
        }

        /// <summary>
        /// Checks that Write overwrites an existing PID file.
        /// </summary>
        [TestMethod]
        public async Task Write_OverwritesExistingFile()
        {
            DateTime startTime = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            await _PidService.Write(
                ServerName,
                11111,
                startTime);

            await _PidService.Write(
                ServerName,
                22222,
                startTime);

            string filePath = Path.Combine(
                PidDirectory,
                $"{ServerName}.pid");
            string content = await File.ReadAllTextAsync(filePath);

            Assert.IsTrue(content.Contains("22222"));
            Assert.IsFalse(content.Contains("11111"));
        }

        /// <summary>
        /// Checks that Delete removes the PID file.
        /// </summary>
        [TestMethod]
        public async Task Delete_RemovesFile()
        {
            await _PidService.Write(
                ServerName,
                12345,
                new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));

            string filePath = Path.Combine(
                PidDirectory,
                $"{ServerName}.pid");
            Assert.IsTrue(File.Exists(filePath));

            _PidService.Delete(ServerName);

            Assert.IsFalse(File.Exists(filePath));
        }

        /// <summary>
        /// Checks that Delete does not throw when the file does not exist.
        /// </summary>
        [TestMethod]
        public void Delete_NoErrorWhenFileMissing()
        {
            string uniqueName = $"PidTest_Missing_{Guid.NewGuid():N}";

            _PidService.Delete(uniqueName);
        }

        /// <summary>
        /// Checks that Write does not throw when WriteAllText throws an exception.
        /// </summary>
        [TestMethod]
        public async Task Write_LogsWarning_WhenWriteAllTextThrows()
        {
            Mock<IExtendedFileSystem> mockFileSystem = new();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(true);
            mockFileSystem.Setup(fs => fs.WriteAllText(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new IOException("Disk full"));

            PidFileService pidService = new(
                _MockLogger.Object,
                mockFileSystem.Object);

            await pidService.Write(
                "TestServer",
                12345,
                new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));

            _MockLogger.Verify(
                l => l.LogToolMessage(
                    "Warn",
                    "Failed to write PID file for TestServer.",
                    false),
                Times.Once);
            _MockLogger.Verify(
                l => l.LogToolMessage(
                    "Error",
                    It.Is<string>(m => m.Contains("Disk full")),
                    false),
                Times.Once);
        }

        /// <summary>
        /// Checks that Delete does not throw when DeleteFile throws an exception.
        /// </summary>
        [TestMethod]
        public void Delete_LogsWarning_WhenDeleteFileThrows()
        {
            Mock<IExtendedFileSystem> mockFileSystem = new();
            mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(true);
            mockFileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException("Access denied"));

            PidFileService pidService = new(
                _MockLogger.Object,
                mockFileSystem.Object);

            pidService.Delete("TestServer");

            _MockLogger.Verify(
                l => l.LogToolMessage(
                    "Warn",
                    "Failed to delete PID file for TestServer.",
                    false),
                Times.Once);
            _MockLogger.Verify(
                l => l.LogToolMessage(
                    "Error",
                    It.Is<string>(m => m.Contains("Access denied")),
                    false),
                Times.Once);
        }
    }
}
