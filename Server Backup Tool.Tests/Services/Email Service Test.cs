// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Tests.Functions;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class EmailServiceTest
    {
        [TestMethod]
        public void GetEmailBodyHTMLString()
        {
            var exception = Assert.ThrowsException<DirectoryNotFoundException>(() =>
            {
                string emailBody = EmailFunction.LoadHTMLFile(@"&lt;html&gt;&lt;body&gt;&lt;p&gt;Hello,&lt;/p&gt;
&lt;p&gt;The Server Backup Tool has opened.&lt;/p&gt;
&lt;p&gt;For any urgent issues, please message Toby on discord.&lt;/p&gt;
&lt;p&gt;Thanks,&lt;/p&gt;
&lt;p&gt;Hunter NAS&lt;/p&gt;
&lt;img src=&quot;cid:footerImage&quot; width=&quot;200&quot; height=&quot;200&quot;&gt;&lt;/body&gt;&lt;/html&gt;");
            });

            Assert.IsTrue(exception.Message.Contains("The filename, directory name, or volume label syntax is incorrect."));
        }

        [TestMethod]
        public void GetEmailBodyHTMLFile()
        {
            string emailBody = EmailFunction.LoadHTMLFile(@"D:\System Folders\Documents\GitHub\Server-Backup-Tool\Server Backup Tool.Tests\Mocks\Open Email Body.html");

            Assert.IsNotNull(emailBody);
        }

        [TestMethod]
        public void TestEmail()
        {
            Mock<EmailService> _mockEmailService = new();
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            try
            {
                _mockEmailService.Object.SendEmail(serverBackupSection.Notifications, serverBackupSection.Notifications.Emails.Cast<EmailElement>().SingleOrDefault(email => email.Trigger.Equals("Open")));

                Assert.IsTrue(true);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to send email. Exception: {ex.Message}");
            }
        }
    }
}