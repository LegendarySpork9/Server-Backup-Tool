// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for extracting embedded binary resources.
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        /// Finds an embedded resource whose name matches the given prefix and returns the full resource name, or null if not found.
        /// </summary>
        string? FindResource(string prefix);

        (bool, Exception?) ExtractResource(string resourceName, string destinationPath, Action<string>? progressCallback = null);
        bool ResourceExists(string resourceName);
    }
}
