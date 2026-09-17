// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Steps;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class ApiConfigStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
        }

        /// <summary>
        /// Checks that Execute sets ApiConfig to null when the API component is not selected.
        /// </summary>
        [TestMethod]
        public void Execute_SkipsApiConfig_WhenComponentNotSelected()
        {
            TestConsole console = new();
            console.Interactive();

            InstallOptionsModel options = new()
            {
                Components = ["Server Backup Tool"]
            };

            ApiConfigStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();

            Assert.IsNull(options.ApiConfig);
        }

        /// <summary>
        /// Checks that Execute populates ApiConfig when the API component is selected and prompts are answered.
        /// </summary>
        [TestMethod]
        public void Execute_ConfiguresApi_WhenComponentSelected()
        {
            TestConsole console = new();
            console.Interactive();

            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("n");
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("mysecret");
            console.Input.PushKey(ConsoleKey.Enter);

            InstallOptionsModel options = new()
            {
                Components = ["Server Backup Tool", "Server Backup Tool API"],
                InstallPath = @"C:\Test"
            };

            options.ServerConfig = new ServerConfigModel
            {
                DatabasePath = "test.db"
            };

            ApiConfigStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();

            Assert.IsNotNull(options.ApiConfig);
            Assert.AreEqual(
                "0.0.0.0",
                options.ApiConfig.BindAddress);
            Assert.AreEqual(
                5000,
                options.ApiConfig.HttpPort);
            Assert.IsFalse(options.ApiConfig.EnableHttps);
            Assert.AreEqual(
                "test.db",
                options.ApiConfig.DatabasePath);
            Assert.IsTrue(
                options.ApiConfig.ClientId.Length > 0);
            Assert.IsTrue(
                options.ApiConfig.ClientSecret.Length > 0);
            Assert.IsTrue(
                options.ApiConfig.WebhookSecret.Length > 0);
        }
    }
}
