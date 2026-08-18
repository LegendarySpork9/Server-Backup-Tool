// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Models.Responses.Related;

namespace ServerBackupTool.API.Models.Responses
{
    /// <summary>
    /// Stores the logs response data.
    /// </summary>
    public class LogsResponseModel
    {
        public required string ServerName { get; set; }
        public required List<LogModel> Logs { get; set; }
        public required int NextAfter { get; set; }
    }
}
