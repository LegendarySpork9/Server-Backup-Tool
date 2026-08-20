// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Models.Responses.Related
{
    /// <summary>
    /// Stores the logs data per file.
    /// </summary>
    public class FileLogModel
    {
        public required string FileName { get; set; }
        public required List<LogModel> Content { get; set; }
    }
}
