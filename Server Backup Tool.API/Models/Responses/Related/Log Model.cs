// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Entities;

namespace ServerBackupTool.API.Models.Responses.Related
{
    /// <summary>
    /// Stores the log data.
    /// </summary>
    public class LogModel
    {
        public required int Id { get; set; }
        public required DateTime Timestamp { get; set; }
        public required Entities.LogLevel Level { get; set; }
        public required LogType Logger { get; set; }
        public required string Message { get; set; }
    }
}
