// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Implementations;
using ServerBackupTool.IntegrationTests.Tool.Helpers;
using ServerBackupTool.Models;
using ServerBackupTool.Services;

namespace ServerBackupTool.IntegrationTests.Tool.Services
{
    [TestClass]
    public class ServerServiceTest
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

            PidFileService pidFileService = new(
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

            ServerService serverService = new(
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
    }
}
