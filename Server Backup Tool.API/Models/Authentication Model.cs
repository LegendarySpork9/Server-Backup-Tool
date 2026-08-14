// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Models
{
    public class AuthenticationModel
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
    }
}
