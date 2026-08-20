// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.IntegrationTests.Tool.Helpers;
using ServerBackupTool.Models;
using ServerBackupTool.Services;

namespace ServerBackupTool.IntegrationTests.Tool.Services
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
            Mock<ILoggerService> mockLogger = new();
            Mock<IExtendedFileSystem> mockFileSystem = new();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(true);
            mockFileSystem.Setup(fs => fs.WriteAllText(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            PidFileService pidFileService = new(
                mockLogger.Object,
                mockFileSystem.Object);

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

            ServerService serverService = new(
                mockLogger.Object,
                pidFileService,
                new(),
                server);

            string expected = "Completed";

            string actual = await serverService.StartServer();

            Assert.AreEqual(
                expected,
                actual);

            mockFileSystem.Verify(fs => fs.WriteAllText(
                It.Is<string>(path => path.EndsWith("Test Server.pid")),
                It.IsAny<string>()),
                Times.Once);
        }
    }
}
