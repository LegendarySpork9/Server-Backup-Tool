// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Models
{
    /// <summary>
    /// Stores the outgoing webhook payload data.
    /// </summary>
    public class WebhookPayloadModel
    {
        public required string ServerName { get; set; }
        public required List<WebhookLogEntryModel> Logs { get; set; }
    }
}
