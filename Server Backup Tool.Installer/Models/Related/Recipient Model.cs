// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Models.Related
{
    /// <summary>
    /// Stores the configuration for an email recipient.
    /// </summary>
    public class RecipientModel
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
