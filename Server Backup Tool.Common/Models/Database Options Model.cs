// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Common.Models
{
    public class DatabaseOptionsModel
    {
        public required string Path { get; set; }
        public required string ServerName { get; set; }
        public required int PollingIntervalMs { get; set; }
    }
}
