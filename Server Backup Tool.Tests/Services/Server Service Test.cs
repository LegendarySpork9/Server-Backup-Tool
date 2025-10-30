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
        // Checks whether the StartServer starts the server as expected.
        [TestMethod]
        public void TestStartServer()
        {
            Mock<ILoggerService> _mockLogger = new();

            ServerModel server = new(new()
            {
                Game = "Minecraft",
                Location = Path.Combine(DirectoryFunction.GetBaseDirectory(), @"Mocks\Server"),
                StartFile = "Start.bat"
            });

            ServerService _serverService = new(_mockLogger.Object, new(), server);

            string expected = "Completed";

            string actual = _serverService.StartServer();

            Assert.AreEqual(expected, actual);
        }
    }
}