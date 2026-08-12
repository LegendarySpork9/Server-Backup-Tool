// Copyright © - Unpublished - Toby Hunter
using System.Net;
using System.Net.Mail;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the email send operation.
    /// </summary>
    public interface IEmailSender
    {
        Task Send(MailMessage message, string host, int port, bool enableSsl, NetworkCredential credentials);
    }
}
