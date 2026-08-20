// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;
using System.Reflection;

namespace ServerBackupTool.IntegrationTests.Tool.Services
{
    [TestClass]
    public class EmailServiceTest
    {
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
    }
}
