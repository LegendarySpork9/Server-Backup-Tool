// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;
using System.Net;
using System.Net.Mail;
using System.Reflection;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class EmailServiceTest
    {
        // Checks whether the SendEmail method sends the email as expected.
        [TestMethod]
        public void TestSendEmailMock()
        {
            string testEmail = @"&lt;html&gt;&lt;body&gt;&lt;p&gt;Hello,&lt;/p&gt;
&lt;p&gt;The Server Backup Tool has opened.&lt;/p&gt;
&lt;p&gt;For any urgent issues, please message Toby on discord.&lt;/p&gt;
&lt;p&gt;Thanks,&lt;/p&gt;
&lt;p&gt;Hunter NAS&lt;/p&gt;
&lt;img src=&quot;cid:footerImage&quot; width=&quot;200&quot; height=&quot;200&quot;&gt;&lt;/body&gt;&lt;/html&gt;";

            Mock<ILoggerService> _mockLogger = new();
            Mock<IEmailSender> _mockEmailSender = new();
            _mockEmailSender.Setup(es => es.Send(It.IsAny<MailMessage>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<NetworkCredential>())).Throws(new InvalidOperationException("Failed to send email."));
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns(testEmail);

            EmailService _emailService = new(_mockLogger.Object, _mockEmailSender.Object, _mockFileSystem.Object);

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
                .GetMethod("BaseAdd", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(System.Configuration.ConfigurationElement) }, null)!;

            baseAdd.Invoke(email.Addresses, new object[] { new ToAddressElement()
            {
                Email = "unittests@hunter-industries.co.uk",
                Name = "Test Recipient"
            } });

            _emailService.SendEmail(notifications, email);

            _mockLogger.Verify(l => l.LogToolMessage(
                It.Is<string>(lvl => lvl.Contains("Warn") || lvl.Contains("Warning")),
                It.Is<string>(msg => msg.Contains("Failed to send")),
                It.IsAny<bool>()),
                Times.Once);

            _mockLogger.Verify(l => l.LogToolMessage(
                It.Is<string>(lvl => lvl.Contains("Error")),
                It.Is<string>(msg => msg.Contains("Failed to send email")),
                It.IsAny<bool>()),
                Times.Once);
        }

        // Checks whether the SendEmail method sends the email as expected.
        [TestCategory("Integration")]
        [TestMethod]
        public void TestSendEmail()
        {
            string testEmail = @"&lt;html&gt;&lt;body&gt;&lt;p&gt;Hello,&lt;/p&gt;
&lt;p&gt;The Server Backup Tool has opened.&lt;/p&gt;
&lt;p&gt;For any urgent issues, please message Toby on discord.&lt;/p&gt;
&lt;p&gt;Thanks,&lt;/p&gt;
&lt;p&gt;Hunter NAS&lt;/p&gt;
&lt;img src=&quot;cid:footerImage&quot; width=&quot;200&quot; height=&quot;200&quot;&gt;&lt;/body&gt;&lt;/html&gt;";

            Mock<ILoggerService> _mockLogger = new();
            SMTPEmailSender _smtpEmailSender = new();
            Mock<IFileSystem> _mockFileSystem = new();
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns(testEmail);

            EmailService _emailService = new(_mockLogger.Object, _smtpEmailSender, _mockFileSystem.Object);

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
                .GetMethod("BaseAdd", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(System.Configuration.ConfigurationElement) }, null)!;

            baseAdd.Invoke(email.Addresses, new object[] { new ToAddressElement()
            {
                Email = "unittests@hunter-industries.co.uk",
                Name = "Test Recipient"
            } });

            _emailService.SendEmail(notifications, email);

            _mockLogger.Verify(l => l.LogToolMessage(
                It.Is<string>(lvl => lvl.Contains("Info")),
                It.Is<string>(msg => msg.Contains("email sent successfully")),
                It.IsAny<bool>()),
                Times.Once);
        }
    }
}