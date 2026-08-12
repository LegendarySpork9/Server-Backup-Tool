// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Services;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class PidFileServiceTest
    {
        /// <summary>
        /// Checks whether the WriteAsync method creates the directory and writes the PID file.
        /// </summary>
        [TestMethod]
        public async Task TestWrite()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
            _mockFileSystem.Setup(fs => fs.WriteAllText(
                It.IsAny<string>(),
                It.IsAny<string>())).Returns(Task.CompletedTask);

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            DateTime startTime = new(2026, 06, 16, 14, 30, 0, DateTimeKind.Utc);

            await _pidFileService.Write(
                "Test Server",
                12345,
                startTime);

            _mockFileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Once);
            _mockFileSystem.Verify(fs => fs.WriteAllText(
                It.Is<string>(path => path.EndsWith("Test Server.pid")),
                It.Is<string>(content => content.Contains("12345") && content.Contains(startTime.ToString("O")))),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the WriteAsync method skips creating the directory when it already exists.
        /// </summary>
        [TestMethod]
        public async Task TestWriteDirectoryExists()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.WriteAllText(
                It.IsAny<string>(),
                It.IsAny<string>())).Returns(Task.CompletedTask);

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            DateTime startTime = new(2026, 06, 16, 14, 30, 0, DateTimeKind.Utc);

            await _pidFileService.Write(
                "Test Server",
                12345,
                startTime);

            _mockFileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
            _mockFileSystem.Verify(fs => fs.WriteAllText(
                It.Is<string>(path => path.EndsWith("Test Server.pid")),
                It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Delete method removes the PID file when it exists.
        /// </summary>
        [TestMethod]
        public void TestDelete()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()));

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            _pidFileService.Delete("Test Server");

            _mockFileSystem.Verify(fs => fs.DeleteFile(
                It.Is<string>(path => path.EndsWith("Test Server.pid"))),
                Times.Once);
        }

        /// <summary>
        /// Checks whether the Delete method skips deletion when the PID file does not exist.
        /// </summary>
        [TestMethod]
        public void TestDeleteFileNotFound()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            _pidFileService.Delete("Test Server");

            _mockFileSystem.Verify(fs => fs.DeleteFile(It.IsAny<string>()), Times.Never);
        }
    }
}
