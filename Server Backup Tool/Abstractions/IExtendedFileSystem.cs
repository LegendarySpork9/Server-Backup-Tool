// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the file system operations.
    /// </summary>
    public interface IExtendedFileSystem : Common.Abstractions.IFileSystem
    {
        // Directory Operations
        void CreateDirectory(string path);

        // File Operations
        void DeleteFile(string file);
        Task<string> ReadAllText(string file);
        Task WriteAllText(string path, string content);

        // ZIP Operations
        void CreateZIPFromDirectory(string sourceDirectory, string destinationFile);
        void CreateZIPFile(string path);
        void CreateZIPEntryFromFile(string zipFilePath, string sourceFilePath, string entryName);
    }
}
