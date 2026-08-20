// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Abstractions
{
    /// <summary>
    /// Interface for the file system operations.
    /// </summary>
    public interface IExtendedFileSystem : Common.Abstractions.IFileSystem
    {
        // Directory Operations
        void DeleteDirectory(string path);

        // File Operations

        long GetFileSize(string file);
        DateTime GetLastWriteTime(string file);
        Task<string[]> ReadAllLines(string file);

        // ZIP Operations
        void ExtractZIPToDirectory(string zip, string destination);
    }
}
