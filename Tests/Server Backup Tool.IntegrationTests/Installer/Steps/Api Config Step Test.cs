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

        /// <summary>
        /// Checks that Execute populates HTTPS settings when the user enables HTTPS with PFX format.
        /// </summary>
        [TestMethod]
        public void Execute_ConfiguresHttps_WhenUserEnablesHttpsWithPfx()
        {
            TestConsole console = new();
            console.Interactive();

            // Bind address (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // HTTP port (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // Enable HTTPS? Yes
            console.Input.PushTextWithEnter("y");
            // HTTPS port
            console.Input.PushTextWithEnter("5001");
            // Certificate format (PFX is first, press Enter)
            console.Input.PushKey(ConsoleKey.Enter);
            // Certificate path
            console.Input.PushTextWithEnter(@"C:\certs\server.pfx");
            // Certificate password
            console.Input.PushTextWithEnter("certpass123");
            // API database path (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // Archive directory (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // Webhook secret
            console.Input.PushTextWithEnter("webhooksecret");
            // Press Enter to continue after credentials display
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
            Assert.IsTrue(options.ApiConfig.EnableHttps);
            Assert.AreEqual(
                "PFX",
                options.ApiConfig.CertificateFormat);
            Assert.AreEqual(
                5001,
                options.ApiConfig.HttpsPort);
            Assert.AreEqual(
                @"C:\certs\server.pfx",
                options.ApiConfig.CertificatePath);
            Assert.AreEqual(
                "certpass123",
                options.ApiConfig.CertificatePassword);
        }

        /// <summary>
        /// Checks that Execute populates HTTPS settings when the user enables HTTPS with PEM format.
        /// </summary>
        [TestMethod]
        public void Execute_ConfiguresHttps_WhenUserEnablesHttpsWithPem()
        {
            TestConsole console = new();
            console.Interactive();

            // Bind address (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // HTTP port (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // Enable HTTPS? Yes
            console.Input.PushTextWithEnter("y");
            // HTTPS port
            console.Input.PushTextWithEnter("5001");
            // Certificate format (PEM is second, press Down then Enter)
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);
            // Certificate path
            console.Input.PushTextWithEnter(@"C:\certs\server.pem");
            // Key path
            console.Input.PushTextWithEnter(@"C:\certs\server-key.pem");
            // API database path (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // Archive directory (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // Webhook secret
            console.Input.PushTextWithEnter("webhooksecret");
            // Press Enter to continue after credentials display
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
            Assert.IsTrue(options.ApiConfig.EnableHttps);
            Assert.AreEqual(
                "PEM",
                options.ApiConfig.CertificateFormat);
            Assert.AreEqual(
                5001,
                options.ApiConfig.HttpsPort);
            Assert.AreEqual(
                @"C:\certs\server.pem",
                options.ApiConfig.CertificatePath);
            Assert.AreEqual(
                @"C:\certs\server-key.pem",
                options.ApiConfig.CertificateKeyPath);
            Assert.AreEqual(
                string.Empty,
                options.ApiConfig.CertificatePassword);
        }
    }
}
