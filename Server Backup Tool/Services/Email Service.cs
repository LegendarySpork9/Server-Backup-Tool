// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Net.Mail;
using System.Net;
using ServerBackupTool.Converters;

namespace ServerBackupTool.Services
{
    public class EmailService
    {
        // Configures and emails out a given email configuration.
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
                        Port = notifications.Port,
                        EnableSsl = notifications.EnableSSL,
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

        // Checks if the body is a .HTML file.
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

        // Checks if the given trigger word or message exists in the email configurations.
        public void CheckForEmail(NotificationElement notifications, string? trigger = null, string? message = null)
        {
            if (notifications.Emails.Count != 0)
            {
                foreach (EmailElement email in notifications.Emails)
                {
                    if (!string.IsNullOrWhiteSpace(trigger))
                    {
                        if (email.Trigger == trigger)
                        {
                            SendEmail(notifications, email);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        if (message.Contains(email.Trigger))
                        {
                            SendEmail(notifications, email);
                        }
                    }
                }
            }
        }
    }
}
