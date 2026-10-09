// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Implementations;
using ServerBackupTool.IntegrationTests.Tool.Helpers;
using ServerBackupTool.Models;

namespace ServerBackupTool.IntegrationTests.Tool.Implementations
{
    [TestClass]
    public class ServerServiceWrapperTest
    {
        private static readonly string PidDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Hunter Industries",
            "Server Backup Tool");

        /// <summary>
        /// Checks whether the StartServer starts the server as expected.
        /// </summary>
        [TestMethod]
        public async Task TestStartServer()
        {
            Mock<ILoggerService> mockLogger = new();
            ExtendedFileSystemWrapper fileSystem = new();

            PidFileServiceWrapper pidFileService = new(
                mockLogger.Object,
                fileSystem);

            ServerModel server = new(new()
            {
                Name = "Test Server",
                Game = "Minecraft",
                Location = Path.Combine(
                    DirectoryHelper.GetBaseDirectory(),
                    @"Tool\Mocks\Server"),
                StartFile = "Start.bat",
                IPAddress = "127.0.0.1"
            })
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            ServerServiceWrapper serverService = new(
                mockLogger.Object,
                pidFileService,
                new(),
                server);

            string expected = "Completed";

            try
            {
                string actual = await serverService.StartServer();

                Assert.AreEqual(
                    expected,
                    actual);

                string pidFilePath = Path.Combine(
                    PidDirectory,
                    "Test Server.pid");

                Assert.IsTrue(
                    File.Exists(pidFilePath),
                    "Expected PID file to be created on disk.");
            }

            finally
            {
                string pidFilePath = Path.Combine(
                    PidDirectory,
                    "Test Server.pid");

                if (File.Exists(pidFilePath))
                {
                    File.Delete(pidFilePath);
                }
            }
        }
        /// <summary>
        /// Checks that the PID file is deleted exactly once when the process exits.
        /// </summary>
        [TestMethod]
        public async Task TestProcessExitDeletesPidFileOnce()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IPidFileService> mockPidFileService = new();

            mockPidFileService.Setup(p => p.Write(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            ServerModel server = new(new()
            {
                Name = "Test Server",
                Game = "Minecraft",
                Location = Path.Combine(
                    DirectoryHelper.GetBaseDirectory(),
                    @"Tool\Mocks\Server"),
                StartFile = "Start.bat",
                IPAddress = "127.0.0.1"
            })
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            ServerServiceWrapper serverService = new(
                mockLogger.Object,
                mockPidFileService.Object,
                new(),
                server);

            await serverService.StartServer();

            await Task.Delay(500);

            mockPidFileService.Verify(p => p.Delete("Test Server"),
                Times.Once);
        }

        /// <summary>
        /// Checks that ServerRunning is set to false after the process exits.
        /// </summary>
        [TestMethod]
        public async Task TestProcessExitSetsServerRunningToFalse()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IPidFileService> mockPidFileService = new();

            mockPidFileService.Setup(p => p.Write(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            ServerModel server = new(new()
            {
                Name = "Test Server",
                Game = "Minecraft",
                Location = Path.Combine(
                    DirectoryHelper.GetBaseDirectory(),
                    @"Tool\Mocks\Server"),
                StartFile = "Start.bat",
                IPAddress = "127.0.0.1"
            })
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            ServerServiceWrapper serverService = new(
                mockLogger.Object,
                mockPidFileService.Object,
                new(),
                server);

            await serverService.StartServer();

            Assert.IsTrue(
                server.ServerRunning,
                "Expected ServerRunning to be true after start.");

            await Task.Delay(500);

            Assert.IsFalse(
                server.ServerRunning,
                "Expected ServerRunning to be false after process exit.");
        }
    }
}
