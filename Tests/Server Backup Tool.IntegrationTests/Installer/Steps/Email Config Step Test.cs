// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Steps;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class EmailConfigStepTest
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
        /// Checks that Execute sets EmailConfig to null when the user declines email configuration.
        /// </summary>
        [TestMethod]
        public void Execute_SkipsEmail_WhenUserDeclinesConfiguration()
        {
            TestConsole console = new();
            console.Interactive();
            console.Input.PushTextWithEnter("n");

            InstallOptionsModel options = new();
            EmailConfigStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();

            Assert.IsNull(options.EmailConfig);
        }

        /// <summary>
        /// Checks that Execute populates EmailConfig with a template and recipient when configured.
        /// </summary>
        [TestMethod]
        public void Execute_ConfiguresEmail_WithOneTemplateAndRecipient()
        {
            TestConsole console = new();
            console.Interactive();

            console.Input.PushTextWithEnter("y");
            console.Input.PushTextWithEnter("smtp.test.com");
            console.Input.PushTextWithEnter("password123");
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("y");
            console.Input.PushTextWithEnter("test@test.com");
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("y");
            console.Input.PushKey(ConsoleKey.Enter);
            console.Input.PushTextWithEnter("Server Started");
            console.Input.PushTextWithEnter("<p>Server is online</p>");
            console.Input.PushTextWithEnter("admin@test.com");
            console.Input.PushTextWithEnter("Admin");
            console.Input.PushTextWithEnter("n");
            console.Input.PushTextWithEnter("n");
            console.Input.PushTextWithEnter("n");

            InstallOptionsModel options = new();
            EmailConfigStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();

            Assert.IsNotNull(options.EmailConfig);
            Assert.IsTrue(options.EmailConfig.Enabled);
            Assert.AreEqual(
                "smtp.test.com",
                options.EmailConfig.SmtpHost);
            Assert.AreEqual(
                "password123",
                options.EmailConfig.SmtpPassword);
            Assert.AreEqual(
                587,
                options.EmailConfig.Port);
            Assert.IsTrue(options.EmailConfig.EnableSSL);
            Assert.AreEqual(
                "test@test.com",
                options.EmailConfig.FromEmail);
            Assert.AreEqual(
                1,
                options.EmailConfig.Emails.Count);
            Assert.AreEqual(
                "Open",
                options.EmailConfig.Emails[0].Trigger);
            Assert.IsTrue(options.EmailConfig.Emails[0].IsSystem);
            Assert.AreEqual(
                "Server Started",
                options.EmailConfig.Emails[0].Subject);
            Assert.AreEqual(
                1,
                options.EmailConfig.Emails[0].Recipients.Count);
            Assert.AreEqual(
                "admin@test.com",
                options.EmailConfig.Emails[0].Recipients[0].Email);
            Assert.AreEqual(
                "Admin",
                options.EmailConfig.Emails[0].Recipients[0].Name);
            Assert.AreEqual(
                0,
                options.EmailConfig.Emails[0].Images.Count);
        }
    }
}
