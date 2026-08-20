// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Models.Responses.Related;

namespace ServerBackupTool.API.Models.Responses
{
    /// <summary>
    /// Stores the log archive response data.
    /// </summary>
    public class LogArchivesResponseModel
    {
        public required string ServerName { get; set; }
        public required List<ArchivedLogModel> Archives { get; set; }
    }
}
