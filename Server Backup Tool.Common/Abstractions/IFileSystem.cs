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
        IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption);
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive = false);

        // File Operations
        DateTime GetCreationTime(string file);
        bool FileExists(string path);
        void DeleteFile(string file);
        Task<string> ReadAllText(string file);
        Task WriteAllText(string path, string content);
    }
}
