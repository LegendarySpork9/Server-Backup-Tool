// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Net.Mail;
using System.Net;
using log4net;

namespace ServerBackupTool.Services
{
    internal class EmailService
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");

        public void SendEmail(NotificationElement notifications, EmailElement email)
        {
            if (notifications.Enabled)
            {
                SmtpClient smtp = new()
                {
                    Host = notifications.Provider.Name,
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(notifications.FromAddress.Email, notifications.Provider.Password)
                };

                MailAddress fromAddress = new(notifications.FromAddress.Email, notifications.FromAddress.Name);

                MailMessage message = new()
                {
                    From = fromAddress,
                    Subject = email.Subject.Value,
                    Body = GetEmailBody(email.Content.Value),
                    IsBodyHtml = true
                };

                foreach (ToAddressElement toAddressElement in email.Addresses)
                {
                    MailAddress toAddress = new(toAddressElement.Email, toAddressElement.Name);
                    message.To.Add(toAddress);
                }

                if (email.Images != null)
                {
                    foreach (ImageElement imageElement in email.Images)
                    {
                        Attachment image = new(imageElement.Path)
                        {
                            ContentId = imageElement.Key
                        };
                        image.ContentDisposition.Inline = true;
                        message.Attachments.Add(image);
                    }
                }

                smtp.Send(message);
            }
        }

        private string GetEmailBody(string configurationValue)
        {
            string emailBody = configurationValue;

            try
            {
                emailBody = File.ReadAllText(configurationValue);
            }

            catch
            {
                Log.Warn("Unable to read body from file, using value specified in configuration element.");
            }

            return emailBody;
        }
    }
}
