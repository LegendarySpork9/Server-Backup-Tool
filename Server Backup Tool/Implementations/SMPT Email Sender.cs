// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using System.Net;
using System.Net.Mail;

namespace ServerBackupTool.Implementations
{
    public class SMTPEmailSender : IEmailSender
    {
        // Sends the given email.
        public void Send(MailMessage message, string host, int port, bool enableSsl, NetworkCredential credentials)
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

            smtp.Send(message);
        }
    }
}
