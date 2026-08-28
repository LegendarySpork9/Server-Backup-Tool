// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Entities;

namespace ServerBackupTool.API.Models
{
    /// <summary>
    /// Stores the webhook registration data.
    /// </summary>
    public class WebhookRegistrationModel
    {
        public required string Id { get; set; }
        public required string Url { get; set; }
        public required string ServerName { get; set; }
        public required LogType LogType { get; set; }
        public required Entities.LogLevel LogLevel { get; set; }
        public required int AfterId { get; set; }
    }
}
