// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Tests.Functions;

namespace ServerBackupTool.Tests.Converters
{
    [TestClass]
    public class ServerConverterTest
    {
        [TestMethod]
        public void TestMessageCommand()
        {
            Mock<ServerConverter> _mockServerConverter = new();
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            string command = _mockServerConverter.Object.GetMessageCommand(serverBackupSection.ServerDetails.Game, "This is a test.");

            Assert.IsTrue(!string.IsNullOrWhiteSpace(command));
        }

        [TestMethod]
        public void TestStopCommand()
        {
            Mock<ServerConverter> _mockServerConverter = new();
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            string command = _mockServerConverter.Object.GetStopCommand(serverBackupSection.ServerDetails.Game);

            Assert.IsTrue(!string.IsNullOrWhiteSpace(command));
        }

        [TestMethod]
        public void TestFinalMessage()
        {
            Mock<ServerConverter> _mockServerConverter = new();
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            string message = _mockServerConverter.Object.GetFinalMessage(serverBackupSection.ServerDetails.Game, serverBackupSection.ServerDetails.Location);

            Assert.IsTrue(!string.IsNullOrWhiteSpace(message));
        }
    }
}