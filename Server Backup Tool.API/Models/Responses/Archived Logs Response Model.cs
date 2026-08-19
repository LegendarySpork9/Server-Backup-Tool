// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Models.Responses.Related;

namespace ServerBackupTool.API.Models.Responses
{
    /// <summary>
    /// Stores the archived log data.
    /// </summary>
    public class ArchivedLogsResponseModel
    {
        public required string ServerName { get; set; }
        public required string ArchiveName { get; set; }
        public required List<FileLogModel> Logs { get; set; }
    }
}
