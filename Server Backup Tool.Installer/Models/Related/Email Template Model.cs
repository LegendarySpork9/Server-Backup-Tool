// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Models.Related
{
    /// <summary>
    /// Stores the configuration for an email template.
    /// </summary>
    public class EmailTemplateModel
    {
        public string Trigger { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public List<RecipientModel> Recipients { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<ImageModel> Images { get; set; } = [];
    }
}
