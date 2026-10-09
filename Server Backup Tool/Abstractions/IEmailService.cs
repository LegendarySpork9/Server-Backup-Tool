// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Models.Configuration;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the email service operations.
    /// </summary>
    public interface IEmailService
    {
        Task SendEmail(NotificationElement notifications, EmailElement email);
        Task CheckForEmail(NotificationElement notifications, string? trigger = null, string? message = null);
    }
}
