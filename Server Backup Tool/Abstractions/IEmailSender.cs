// Copyright © - Unpublished - Toby Hunter
using System.Net;
using System.Net.Mail;

namespace ServerBackupTool.Abstractions
{
    // Interface for the email send operation.
    public interface IEmailSender
    {
        void Send(MailMessage message, string host, int port, bool enableSsl, NetworkCredential credentials);
    }
}
