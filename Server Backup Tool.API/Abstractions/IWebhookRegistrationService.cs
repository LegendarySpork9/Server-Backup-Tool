// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Models.Requests;

namespace ServerBackupTool.API.Abstractions
{
    /// <summary>
    /// Interface for the webhook registration service operations.
    /// </summary>
    public interface IWebhookRegistrationService
    {
        Task<(string?, Exception?)> Register(WebhookRegistrationRequestModel registration);
        Task<(bool, Exception?)> Unregister(string webhookId);
        Task<(List<WebhookRegistrationModel>?, Exception?)> GetAll(string serverName);
        Task<(bool, Exception?)> UpdateAfterId(string webhookId, int afterId);
    }
}
