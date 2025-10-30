// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    // Interface for the file system operations.
    public interface IFileSystem
    {
        // Directory operations
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        IEnumerable<string> GetFiles(string path);

        // File operations
        bool FileExists(string file);
        void DeleteFile(string file);
        DateTime GetCreationTime(string file);

        // Zip operations
        void CreateZIPFromDirectory(string sourceDirectory, string destinationFile);
        void CreateZIPFile(string path);
        void CreateZIPEntryFromFile(string zipFilePath, string sourceFilePath, string entryName);

        // Optional: reading/writing files
        string ReadAllText(string file);
        void WriteAllText(string file, string content);
    }
}
