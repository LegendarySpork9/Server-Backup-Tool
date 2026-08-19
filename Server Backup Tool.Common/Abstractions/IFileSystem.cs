// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Common.Abstractions
{
    /// <summary>
    /// Interface for the file system operations.
    /// </summary>
    public interface IFileSystem
    {
        // Directory Operations
        IEnumerable<string> GetFiles(string path);

        // File Operations
        DateTime GetCreationTime(string file);
        bool FileExists(string path);
    }
}
