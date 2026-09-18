// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;
using System.Net;
using System.Net.Mail;
using System.Reflection;

namespace ServerBackupTool.IntegrationTests.Tool.Services
{
    [TestClass]
    public class EmailServiceTest
    {
        /// <summary>
        /// Adds an EmailElement to a NotificationElement's Emails collection via reflection.
        /// </summary>
        private static void AddEmailToNotification(
            NotificationElement notifications,
            EmailElement email)
        {
            MethodInfo baseAdd = notifications.Emails.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                notifications.Emails,
                [email]);
        }

        /// <summary>
        /// Checks whether the SendEmail method sends the email as expected.
        /// </summary>
        [TestMethod]
        public async Task TestSendEmail()
        {
            string testEmail = @"<html>
    <body>
        <p>Hello,</p>
        <p>The Server Backup Tool has opened.</p>
        <p>For any urgent issues, please message Toby on discord.</p>
        <p>Thanks,</p>
        <p>Hunter NAS</p>
        <img src=""https://raw.githubusercontent.com/LegendarySpork9/Server-Backup-Tool/refs/heads/main/Server%20Backup%20Tool/Content/HI%20Tech%20Logo.png"" width=""200"" height=""200"">
    </body>
</html>";

            Mock<ILoggerService> mockLogger = new();
            SMTPEmailSender smtpEmailSender = new();
            Mock<IExtendedFileSystem> mockFileSystem = new();
            mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ReturnsAsync(testEmail);

            EmailService emailService = new(
                mockLogger.Object,
                smtpEmailSender,
                mockFileSystem.Object);

            NotificationElement notifications = new()
            {
                Enabled = true,
                Port = 25,
                EnableSSL = false,
                Provider = new()
                {
                    Name = "localhost",
                    Password = ""
                },
                FromAddress = new()
                {
                    Email = "unittests@hunter-industries.co.uk",
                    Name = "Test Sender"
                }
            };
            EmailElement email = new()
            {
                Subject = new() { Value = "SBT Open Notification (Testing)" },
                Content = new() { Value = testEmail }
            };

            MethodInfo baseAdd = email.Addresses.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                email.Addresses,
                [ new ToAddressElement()
            {
                Email = "unittests@hunter-industries.co.uk",
                Name = "Test Recipient"
            } ]);

            await emailService.SendEmail(
                notifications,
                email);

            mockLogger.Verify(l => l.LogToolMessage(
                It.Is<string>(lvl => lvl.Contains("Info")),
                It.Is<string>(msg => msg.Contains("email sent successfully")),
                It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// Checks that CheckForEmail sends the email when the trigger matches.
        /// </summary>
        [TestMethod]
        public async Task CheckForEmail_ReturnsMatchingEmail_WhenTriggerMatches()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IEmailSender> mockEmailSender = new();
            Mock<IExtendedFileSystem> mockFileSystem = new();
            mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ReturnsAsync("body");

            EmailService emailService = new(
                mockLogger.Object,
                mockEmailSender.Object,
                mockFileSystem.Object);

            NotificationElement notifications = new()
            {
                Enabled = true,
                Port = 25,
                EnableSSL = false,
                Provider = new()
                {
                    Name = "localhost",
                    Password = ""
                },
                FromAddress = new()
                {
                    Email = "unittests@hunter-industries.co.uk",
                    Name = "Test Sender"
                }
            };

            EmailElement email = new()
            {
                Trigger = "Open",
                SystemEmail = true,
                Subject = new() { Value = "Open Notification" },
                Content = new() { Value = "body" }
            };

            MethodInfo baseAdd = email.Addresses.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                email.Addresses,
                [new ToAddressElement()
            {
                Email = "unittests@hunter-industries.co.uk",
                Name = "Test Recipient"
            }]);

            AddEmailToNotification(
                notifications,
                email);

            await emailService.CheckForEmail(
                notifications,
                trigger: "Open");

            mockEmailSender.Verify(s => s.Send(
                It.IsAny<MailMessage>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<NetworkCredential>()),
                Times.Once);
        }

        /// <summary>
        /// Checks that CheckForEmail does not send an email when the trigger does not match.
        /// </summary>
        [TestMethod]
        public async Task CheckForEmail_ReturnsNull_WhenTriggerDoesNotMatch()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IEmailSender> mockEmailSender = new();
            Mock<IExtendedFileSystem> mockFileSystem = new();

            EmailService emailService = new(
                mockLogger.Object,
                mockEmailSender.Object,
                mockFileSystem.Object);

            NotificationElement notifications = new()
            {
                Enabled = true,
                Port = 25,
                EnableSSL = false,
                Provider = new()
                {
                    Name = "localhost",
                    Password = ""
                },
                FromAddress = new()
                {
                    Email = "unittests@hunter-industries.co.uk",
                    Name = "Test Sender"
                }
            };

            EmailElement email = new()
            {
                Trigger = "Open",
                SystemEmail = true,
                Subject = new() { Value = "Open Notification" },
                Content = new() { Value = "body" }
            };

            AddEmailToNotification(
                notifications,
                email);

            await emailService.CheckForEmail(
                notifications,
                trigger: "NonExistent");

            mockEmailSender.Verify(s => s.Send(
                It.IsAny<MailMessage>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<NetworkCredential>()),
                Times.Never);
        }

        /// <summary>
        /// Checks that GetEmailBody returns the content as-is when ReadAllText throws (i.e., value is not a file path).
        /// </summary>
        [TestMethod]
        public async Task GetEmailBody_ReturnsContent_WhenValueIsNotFilePath()
        {
            string htmlContent = "<html><body><p>Plain HTML content.</p></body></html>";

            Mock<ILoggerService> mockLogger = new();
            Mock<IEmailSender> mockEmailSender = new();
            Mock<IExtendedFileSystem> mockFileSystem = new();
            mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ThrowsAsync(new FileNotFoundException());

            EmailService emailService = new(
                mockLogger.Object,
                mockEmailSender.Object,
                mockFileSystem.Object);

            MailMessage? capturedMessage = null;
            mockEmailSender.Setup(s => s.Send(
                It.IsAny<MailMessage>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<NetworkCredential>()))
                .Callback<MailMessage, string, int, bool, NetworkCredential>(
                    (msg, host, port, ssl, cred) => capturedMessage = msg);

            NotificationElement notifications = new()
            {
                Enabled = true,
                Port = 25,
                EnableSSL = false,
                Provider = new()
                {
                    Name = "localhost",
                    Password = ""
                },
                FromAddress = new()
                {
                    Email = "unittests@hunter-industries.co.uk",
                    Name = "Test Sender"
                }
            };

            EmailElement email = new()
            {
                Subject = new() { Value = "Plain Content Test" },
                Content = new() { Value = htmlContent }
            };

            MethodInfo baseAdd = email.Addresses.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                email.Addresses,
                [new ToAddressElement()
            {
                Email = "unittests@hunter-industries.co.uk",
                Name = "Test Recipient"
            }]);

            await emailService.SendEmail(
                notifications,
                email);

            Assert.IsNotNull(capturedMessage);
            Assert.AreEqual(
                htmlContent,
                capturedMessage.Body);
        }

        /// <summary>
        /// Checks that GetEmailBody reads the file content when the file exists.
        /// </summary>
        [TestMethod]
        public async Task GetEmailBody_ReadsFile_WhenFileExists()
        {
            string fileContent = "<html><body><p>Content from file.</p></body></html>";
            string tempFile = Path.Combine(
                Path.GetTempPath(),
                $"email_test_{Guid.NewGuid()}.html");

            try
            {
                await File.WriteAllTextAsync(
                    tempFile,
                    fileContent);

                Mock<ILoggerService> mockLogger = new();
                Mock<IEmailSender> mockEmailSender = new();
                Mock<IExtendedFileSystem> mockFileSystem = new();
                mockFileSystem.Setup(fs => fs.ReadAllText(tempFile)).ReturnsAsync(fileContent);

                EmailService emailService = new(
                    mockLogger.Object,
                    mockEmailSender.Object,
                    mockFileSystem.Object);

                MailMessage? capturedMessage = null;
                mockEmailSender.Setup(s => s.Send(
                    It.IsAny<MailMessage>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<NetworkCredential>()))
                    .Callback<MailMessage, string, int, bool, NetworkCredential>(
                        (msg, host, port, ssl, cred) => capturedMessage = msg);

                NotificationElement notifications = new()
                {
                    Enabled = true,
                    Port = 25,
                    EnableSSL = false,
                    Provider = new()
                    {
                        Name = "localhost",
                        Password = ""
                    },
                    FromAddress = new()
                    {
                        Email = "unittests@hunter-industries.co.uk",
                        Name = "Test Sender"
                    }
                };

                EmailElement email = new()
                {
                    Subject = new() { Value = "File Content Test" },
                    Content = new() { Value = tempFile }
                };

                MethodInfo baseAdd = email.Addresses.GetType().BaseType!
                    .GetMethod(
                        "BaseAdd",
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null,
                        [typeof(System.Configuration.ConfigurationElement)],
                        null)!;

                baseAdd.Invoke(
                    email.Addresses,
                    [new ToAddressElement()
                {
                    Email = "unittests@hunter-industries.co.uk",
                    Name = "Test Recipient"
                }]);

                await emailService.SendEmail(
                    notifications,
                    email);

                Assert.IsNotNull(capturedMessage);
                Assert.AreEqual(
                    fileContent,
                    capturedMessage.Body);
            }

            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Checks that SendEmail skips sending when notifications are disabled.
        /// </summary>
        [TestMethod]
        public async Task SendEmail_SkipsWhenNotificationsDisabled()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IEmailSender> mockEmailSender = new();
            Mock<IExtendedFileSystem> mockFileSystem = new();

            EmailService emailService = new(
                mockLogger.Object,
                mockEmailSender.Object,
                mockFileSystem.Object);

            NotificationElement notifications = new()
            {
                Enabled = false,
                Port = 25,
                EnableSSL = false,
                Provider = new()
                {
                    Name = "localhost",
                    Password = ""
                },
                FromAddress = new()
                {
                    Email = "unittests@hunter-industries.co.uk",
                    Name = "Test Sender"
                }
            };

            EmailElement email = new()
            {
                Subject = new() { Value = "Should Not Send" },
                Content = new() { Value = "body" }
            };

            await emailService.SendEmail(
                notifications,
                email);

            mockEmailSender.Verify(s => s.Send(
                It.IsAny<MailMessage>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<NetworkCredential>()),
                Times.Never);
        }

        /// <summary>
        /// Checks that SendEmail sends an email with the correct subject.
        /// </summary>
        [TestMethod]
        public async Task SendEmail_SendsEmailWithCorrectSubject()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IEmailSender> mockEmailSender = new();
            Mock<IExtendedFileSystem> mockFileSystem = new();
            mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ReturnsAsync("body");

            EmailService emailService = new(
                mockLogger.Object,
                mockEmailSender.Object,
                mockFileSystem.Object);

            MailMessage? capturedMessage = null;
            mockEmailSender.Setup(s => s.Send(
                It.IsAny<MailMessage>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<NetworkCredential>()))
                .Callback<MailMessage, string, int, bool, NetworkCredential>(
                    (msg, host, port, ssl, cred) => capturedMessage = msg);

            NotificationElement notifications = new()
            {
                Enabled = true,
                Port = 25,
                EnableSSL = false,
                Provider = new()
                {
                    Name = "localhost",
                    Password = ""
                },
                FromAddress = new()
                {
                    Email = "unittests@hunter-industries.co.uk",
                    Name = "Test Sender"
                }
            };

            EmailElement email = new()
            {
                Trigger = "Open",
                SystemEmail = true,
                Subject = new() { Value = "SBT Open Notification" },
                Content = new() { Value = "body" }
            };

            MethodInfo baseAdd = email.Addresses.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                email.Addresses,
                [new ToAddressElement()
            {
                Email = "unittests@hunter-industries.co.uk",
                Name = "Test Recipient"
            }]);

            await emailService.SendEmail(
                notifications,
                email);

            Assert.IsNotNull(capturedMessage);
            Assert.AreEqual(
                "SBT Open Notification",
                capturedMessage.Subject);
        }

        /// <summary>
        /// Checks that CheckForEmail sends the email when the message contains the trigger for a non-system email.
        /// </summary>
        [TestMethod]
        public async Task CheckForEmail_ReturnsMatchingEmail_WhenMessageContainsTrigger()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IEmailSender> mockEmailSender = new();
            Mock<IExtendedFileSystem> mockFileSystem = new();
            mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).ReturnsAsync("body");

            EmailService emailService = new(
                mockLogger.Object,
                mockEmailSender.Object,
                mockFileSystem.Object);

            NotificationElement notifications = new()
            {
                Enabled = true,
                Port = 25,
                EnableSSL = false,
                Provider = new()
                {
                    Name = "localhost",
                    Password = ""
                },
                FromAddress = new()
                {
                    Email = "unittests@hunter-industries.co.uk",
                    Name = "Test Sender"
                }
            };

            EmailElement email = new()
            {
                Trigger = "warning",
                SystemEmail = false,
                Subject = new() { Value = "Warning Notification" },
                Content = new() { Value = "body" }
            };

            MethodInfo baseAdd = email.Addresses.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                email.Addresses,
                [new ToAddressElement()
            {
                Email = "unittests@hunter-industries.co.uk",
                Name = "Test Recipient"
            }]);

            AddEmailToNotification(
                notifications,
                email);

            await emailService.CheckForEmail(
                notifications,
                message: "A warning occurred in the server.");

            mockEmailSender.Verify(s => s.Send(
                It.IsAny<MailMessage>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<NetworkCredential>()),
                Times.Once);
        }
    }
}
