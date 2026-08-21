// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Entities;

namespace ServerBackupTool.API.Models.Responses
{
    /// <summary>
    /// Stores the command response data.
    /// </summary>
    public class CommandResponseModel
    {
        public required int Id { get; set; }
        public required string ServerName { get; set; }
        public required TargetType Target { get; set; }
        public required string Command { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
