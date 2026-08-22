// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Entities;

namespace ServerBackupTool.Models
{
    /// <summary>
    /// Stores the command data.
    /// </summary>
    public class CommandModel
    {
        public required int Id { get; set; }
        public required TargetType Target { get; set; }
        public required string Command { get; set; }
    }
}
