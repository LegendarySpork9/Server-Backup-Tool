// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Models
{
    /// <summary>
    /// Stores the webhook configuration settings.
    /// </summary>
    public class WebhookSettingsModel
    {
        public required string Secret { get; set; }
        public int TimeoutSeconds { get; set; } = 10;
        public int MaxRetries { get; set; } = 3;
    }
}
