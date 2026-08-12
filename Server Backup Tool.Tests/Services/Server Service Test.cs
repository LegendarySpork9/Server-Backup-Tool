// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Models;
using ServerBackupTool.Services;
using ServerBackupTool.Tests.Functions;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class ServerServiceTest
    {
        /// <summary>
        /// Checks whether the StartServer starts the server as expected.
        /// </summary>
        [TestMethod]
        public async Task TestStartServer()
        {
            Mock<ILoggerService> _mockLogger = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);

            ServerModel server = new(new()
            {
                Name = "Test Server",
                Game = "Minecraft",
                Location = Path.Combine(
                    DirectoryFunction.GetBaseDirectory(),
                    @"Mocks\Server"),
                StartFile = "Start.bat"
            })
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            ServerService _serverService = new(
                _mockLogger.Object,
                _pidFileService, new(),
                server);

            string expected = "Completed";

            string actual = await _serverService.StartServer();

            Assert.AreEqual(
                expected,
                actual);

            _mockFileSystem.Verify(fs => fs.WriteAllText(
                It.Is<string>(path => path.EndsWith("Test Server.pid")),
                It.IsAny<string>()),
                Times.Once);
        }
    }
}