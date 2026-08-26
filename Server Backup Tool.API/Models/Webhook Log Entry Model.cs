// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Models
{
    /// <summary>
    /// Stores the log entry data for webhook payloads.
    /// </summary>
    public class WebhookLogEntryModel
    {
        public required int Id { get; set; }
        public required DateTime Timestamp { get; set; }
        public required string Level { get; set; }
        public required string Type { get; set; }
        public required string Message { get; set; }
    }
}
