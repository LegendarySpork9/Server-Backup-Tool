// Copyright © - Unpublished - Toby Hunter
using System.ComponentModel.DataAnnotations;

namespace ServerBackupTool.API.Models.Requests
{
    /// <summary>
    /// Stores the webhook registration request data.
    /// </summary>
    public class WebhookRegistrationRequestModel
    {
        [Required(ErrorMessage = "The url field is required.")]
        public string? Url { get; set; }
        [Required(ErrorMessage = "The serverName field is required.")]
        public string? ServerName { get; set; }
        [Required(ErrorMessage = "The logType field is required.")]
        public string? LogType { get; set; }
        [Required(ErrorMessage = "The logLevel field is required.")]
        public string? LogLevel { get; set; }
        public int AfterId { get; set; } = 0;
    }
}
