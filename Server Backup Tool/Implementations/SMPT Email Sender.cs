// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using System.Net;
using System.Net.Mail;

namespace ServerBackupTool.Implementations
{
    public class SMTPEmailSender : IEmailSender
    {
        /// <summary>
        /// Sends the given email.
        /// </summary>
        public async Task Send(
            MailMessage message,
            string host,
            int port,
            bool enableSsl,
            NetworkCredential credentials)
        {
            SmtpClient smtp = new()
            {
                Host = host,
                Port = port,
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = credentials
            };

            await smtp.SendMailAsync(message);
        }
    }
}
