// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
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

        /// <summary>
        /// Checks that Execute populates a custom trigger template with multiple recipients and an image.
        /// </summary>
        [TestMethod]
        public void Execute_ConfiguresEmail_WithCustomTriggerMultipleRecipientsAndImage()
        {
            TestConsole console = new();
            console.Interactive();

            // Enable email
            console.Input.PushTextWithEnter("y");
            // SMTP host
            console.Input.PushTextWithEnter("mail.example.com");
            // SMTP password
            console.Input.PushTextWithEnter("secret");
            // SMTP port (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // Enable SSL
            console.Input.PushTextWithEnter("y");
            // From email
            console.Input.PushTextWithEnter("noreply@example.com");
            // From name (default)
            console.Input.PushKey(ConsoleKey.Enter);
            // Add an email template? Yes
            console.Input.PushTextWithEnter("y");
            // Select trigger type: "Custom" is the 4th option
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.DownArrow);
            console.Input.PushKey(ConsoleKey.Enter);
            // Enter the server output text to match
            console.Input.PushTextWithEnter("Server stopped");
            // Subject
            console.Input.PushTextWithEnter("Server Down Alert");
            // Content
            console.Input.PushTextWithEnter("<p>The server has stopped.</p>");
            // First recipient (required)
            console.Input.PushTextWithEnter("admin@example.com");
            console.Input.PushTextWithEnter("Admin");
            // Add another recipient? Yes
            console.Input.PushTextWithEnter("y");
            // Second recipient
            console.Input.PushTextWithEnter("ops@example.com");
            console.Input.PushTextWithEnter("Ops Team");
            // Add another recipient? No
            console.Input.PushTextWithEnter("n");
            // Add an inline image? Yes
            console.Input.PushTextWithEnter("y");
            // Image content ID
            console.Input.PushTextWithEnter("logo");
            // Image file path
            console.Input.PushTextWithEnter(@"C:\images\logo.png");
            // Add another inline image? No
            console.Input.PushTextWithEnter("n");
            // Add another email template? No
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
                "mail.example.com",
                options.EmailConfig.SmtpHost);
            Assert.AreEqual(
                1,
                options.EmailConfig.Emails.Count);

            EmailTemplateModel template = options.EmailConfig.Emails[0];

            Assert.AreEqual(
                "Server stopped",
                template.Trigger);
            Assert.IsFalse(template.IsSystem);
            Assert.AreEqual(
                "Server Down Alert",
                template.Subject);
            Assert.AreEqual(
                "<p>The server has stopped.</p>",
                template.Content);
            Assert.AreEqual(
                2,
                template.Recipients.Count);
            Assert.AreEqual(
                "admin@example.com",
                template.Recipients[0].Email);
            Assert.AreEqual(
                "ops@example.com",
                template.Recipients[1].Email);
            Assert.AreEqual(
                "Ops Team",
                template.Recipients[1].Name);
            Assert.AreEqual(
                1,
                template.Images.Count);
            Assert.AreEqual(
                "logo",
                template.Images[0].Key);
            Assert.AreEqual(
                @"C:\images\logo.png",
                template.Images[0].Path);
        }
    }
}
