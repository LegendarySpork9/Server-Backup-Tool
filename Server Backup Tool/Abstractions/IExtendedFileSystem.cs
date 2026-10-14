// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the file system operations.
    /// </summary>
    public interface IExtendedFileSystem : Common.Abstractions.IFileSystem
    {
        // ZIP Operations
        void CreateZIPFromDirectory(string sourceDirectory, string destinationFile);
        void CreateZIPFile(string path);
        void CreateZIPEntryFromFile(string zipFilePath, string sourceFilePath, string entryName);
    }
}
