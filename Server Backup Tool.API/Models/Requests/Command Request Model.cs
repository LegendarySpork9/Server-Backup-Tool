// Copyright © - Unpublished - Toby Hunter
using System.ComponentModel.DataAnnotations;

namespace ServerBackupTool.API.Models.Requests
{
    /// <summary>
    /// Stores the command request data.
    /// </summary>
    public class CommandRequestModel
    {
        [Required(ErrorMessage = "The target field is required.")]
        public string? Target { get; set; }

        [Required(ErrorMessage = "The command field is required.")]
        public string? Command { get; set; }
    }
}
