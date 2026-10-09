// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models;

namespace ServerBackupTool.UnitTests.Tool.Implementations
{
    [TestClass]
    public class ServerServiceWrapperTest
    {
        /// <summary>
        /// Checks that a second StartServer call produces a different PID and start time.
        /// </summary>
        [TestMethod]
        public async Task TestRestartServerUpdatesPidFile()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IPidFileService> mockPidFileService = new();

            List<(int Pid, DateTime StartTime)> writeCalls = [];

            mockPidFileService.Setup(p => p.Write(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>()))
                .Callback<string, int, DateTime>((_, pid, startTime) =>
                    writeCalls.Add((pid, startTime)))
                .Returns(Task.CompletedTask);

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            ServerModel server = new(new()
            {
                Name = "Test Server",
                Game = "Minecraft",
                Location = Path.Combine(
                    baseDirectory,
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

            await Task.Delay(1100);

            await serverService.StartServer();

            Assert.AreEqual(
                2,
                writeCalls.Count,
                "Expected PidFileService.Write to be called twice.");

            Assert.AreNotEqual(
                writeCalls[0].Pid,
                writeCalls[1].Pid,
                "Expected different PID after restart.");

            Assert.AreNotEqual(
                writeCalls[0].StartTime,
                writeCalls[1].StartTime,
                "Expected different start time after restart.");
        }
    }
}
