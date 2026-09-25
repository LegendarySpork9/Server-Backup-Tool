// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Models;

namespace ServerBackupTool.API.Abstractions
{
    /// <summary>
    /// Interface for the webhook dispatch service operations.
    /// </summary>
    public interface IWebhookDispatchService
    {
        Task<(bool, Exception?)> Send(string url, string webhookId, WebhookPayloadModel payload);
    }
}
