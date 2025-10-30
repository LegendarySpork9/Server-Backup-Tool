// Copyright © - 17/01/2024 - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Net.Mail;
using System.Net;
using ServerBackupTool.Converters;
using ServerBackupTool.Abstractions;

namespace ServerBackupTool.Services
{
    public class EmailService
    {
        readonly ILoggerService _Logger;
        readonly IEmailSender _EmailSender;
        readonly IFileSystem _FileSystem;
        readonly bool ServerRunning = false;

        // Sets the class's global variables.
        public EmailService(ILoggerService _logger, IEmailSender _emailSender, IFileSystem _fileSystem, bool serverRunning = false)
        {
            _Logger = _logger;
            _EmailSender = _emailSender;
            _FileSystem = _fileSystem;
            ServerRunning = serverRunning;
        }

        // Configures and emails out a given email configuration.
        public void SendEmail(NotificationElement notifications, EmailElement email)
        {
            if (notifications.Enabled)
            {
                _Logger.LogToolMessage(StandardValues.LoggerValues.Info, $"Trying to send \"{email.Subject.Value}\" email.");

                try
                {
                    NetworkCredential credentials = new(notifications.FromAddress.Email, notifications.Provider.Password);
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

                    _EmailSender.Send(message, notifications.Provider.Name, notifications.Port, notifications.EnableSSL, credentials);
                    _Logger.LogToolMessage(StandardValues.LoggerValues.Info, $"\"{email.Subject.Value}\" email sent successfully.", ServerRunning);
                }

                catch (Exception ex)
                {
                    _Logger.LogToolMessage(StandardValues.LoggerValues.Warning, $"Failed to send \"{email.Subject.Value}\" email.", ServerRunning);
                    _Logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                }
            }
        }

        // Checks if the body is a .HTML file.
        private string GetEmailBody(string configurationValue)
        {
            string emailBody = configurationValue;

            try
            {
                emailBody = _FileSystem.ReadAllText(configurationValue);
            }

            catch
            {
                _Logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Unable to read body from file, using value specified in configuration element.");
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
                        if (message.Contains(email.Trigger) && !email.SystemEmail)
                        {
                            SendEmail(notifications, email);
                        }
                    }
                }
            }
        }
    }
}
