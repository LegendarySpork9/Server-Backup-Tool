// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Models.Related
{
    /// <summary>
    /// Stores the configuration for an image used in email templates.
    /// </summary>
    public class ImageModel
    {
        public string Key { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
