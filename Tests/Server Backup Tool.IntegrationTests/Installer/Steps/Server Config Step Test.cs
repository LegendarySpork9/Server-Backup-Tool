// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Steps;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class ServerConfigStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockFileSystem = new Mock<IExtendedFileSystem>();
        }

        /// <summary>
        /// Checks that Execute populates server configuration when all prompts are answered.
        /// </summary>
        [TestMethod]
        public void Execute_SetsServerConfig_WhenAllPromptsAnswered()
        {
            _MockFileSystem
                .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
                .Returns(true);

            TestConsole console = new();
            console.Interactive();
            console.Input.PushTextWithEnter("TestServer");
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("C:\\GameServer");
            console.Input.PushTextWithEnter("start.bat");
            console.Input.PushTextWithEnter("192.168.1.1");
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushKey(ConsoleKey.Enter);

            InstallOptionsModel options = new()
            {
                Components = ["Server Backup Tool"]
            };

            ServerConfigStep step = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                options);

            step.Execute();

            Assert.AreEqual(
                "TestServer",
                options.ServerConfig.ServerName);
            Assert.AreEqual(
                "Minecraft",
                options.ServerConfig.Game);
            Assert.AreEqual(
                "C:\\GameServer",
                options.ServerConfig.ServerDirectory);
            Assert.AreEqual(
                "start.bat",
                options.ServerConfig.StartFile);
            Assert.AreEqual(
                "192.168.1.1",
                options.ServerConfig.IPAddress);
            Assert.IsTrue(
                options.ServerConfig.DatabasePath.Length > 0);
            Assert.AreEqual(
                1000,
                options.ServerConfig.PollingIntervalMs);
            Assert.AreEqual(
                "Server Backup Tool - TestServer",
                options.ToolTaskName);
        }
    }
}
