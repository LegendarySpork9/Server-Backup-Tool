// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Models.Responses
{
    /// <summary>
    /// Stores the webhook registration response data.
    /// </summary>
    public class WebhookRegistrationResponseModel
    {
        public required string Id { get; set; }
        public required string ServerName { get; set; }
    }
}
