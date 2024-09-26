// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Net.Mail;
using System.Net;
using ServerBackupTool.Converters;

namespace ServerBackupTool.Services
{
    internal class EmailService
    {
        public void SendEmail(NotificationElement notifications, EmailElement email)
        {
            if (notifications.Enabled)
            {
                LoggerService _logger = new();

                _logger.LogToolMessage(StandardValues.LoggerValues.Info, $"Trying to send \"{email.Subject.Value}\" email.");

                try
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

                catch (Exception ex)
                {
                    _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to send \"{email.Subject.Value}\" email.");
                    _logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                }
            }
        }

        private string GetEmailBody(string configurationValue)
        {
            LoggerService _logger = new();

            string emailBody = configurationValue;

            try
            {
                emailBody = File.ReadAllText(configurationValue);
            }

            catch
            {
                _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Unable to read body from file, using value specified in configuration element.");
            }

            return emailBody;
        }
    }
}
