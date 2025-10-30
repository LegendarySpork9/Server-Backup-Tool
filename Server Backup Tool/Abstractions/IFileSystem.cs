// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    // Interface for the file system operations.
    public interface IFileSystem
    {
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        IEnumerable<string> GetFiles(string path);

        void DeleteFile(string file);
        DateTime GetCreationTime(string file);

        void CreateZIPFromDirectory(string sourceDirectory, string destinationFile);
        void CreateZIPFile(string path);
        void CreateZIPEntryFromFile(string zipFilePath, string sourceFilePath, string entryName);

        string ReadAllText(string file);
    }
}
