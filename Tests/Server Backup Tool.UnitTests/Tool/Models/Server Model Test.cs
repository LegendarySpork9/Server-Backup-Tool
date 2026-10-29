// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Models;

namespace ServerBackupTool.UnitTests.Tool.Models
{
    [TestClass]
    public class ServerModelTest
    {
        /// <summary>
        /// Checks that ResetProcess creates a new Process instance.
        /// </summary>
        [TestMethod]
        public void TestResetProcessCreatesNewInstance()
        {
            ServerModel server = new(new()
            {
                Name = "Test",
                Game = "Minecraft",
                Location = "C:\\Temp",
                StartFile = "Start.bat",
                IPAddress = "127.0.0.1"
            })
            {
                Name = "Test",
                Game = "Minecraft"
            };

            var firstProcess = server.ServerProcess;

            server.ResetProcess();

            var secondProcess = server.ServerProcess;

            Assert.AreNotSame(
                firstProcess,
                secondProcess,
                "Expected a new Process instance after ResetProcess.");
        }
    }
}
