// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Tests.Functions;

namespace ServerBackupTool.Tests.Converters
{
    [TestClass]
    public class ServerConverterTest
    {
        // Checks whether the GetMessageCommand method returns the expected command.
        [TestMethod]
        public void TestMessageCommand()
        {
            Mock<ServerConverter> _mockServerConverter = new();
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            string command = _mockServerConverter.Object.GetMessageCommand(serverBackupSection.ServerDetails.Game, "This is a test.");

            Assert.IsTrue(!string.IsNullOrWhiteSpace(command));
        }

        // Checks whether the GetStopCommand method returns the expected command.
        [TestMethod]
        public void TestStopCommand()
        {
            Mock<ServerConverter> _mockServerConverter = new();
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            string command = _mockServerConverter.Object.GetStopCommand(serverBackupSection.ServerDetails.Game);

            Assert.IsTrue(!string.IsNullOrWhiteSpace(command));
        }

        // Checks whether the GetFinalMessage method returns the expected message.
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