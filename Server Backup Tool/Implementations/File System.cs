// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using System.IO.Compression;

namespace ServerBackupTool.Implementations
{
    public class FileSystem : IFileSystem
    {
        // Returns whether the directory exists for a given path.
        public bool DirectoryExists(string path) => Directory.Exists(path);

        // Creates the directory for a given path.
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        // Returns all the files in a given path.
        public IEnumerable<string> GetFiles(string path) => Directory.GetFiles(path);

        // Deletes the given file.
        public void DeleteFile(string file) => File.Delete(file);

        // Returns the UTC date and time for when the given file was created.
        public DateTime GetCreationTime(string file) => File.GetCreationTimeUtc(file);

        // Creates a ZIP file from the given directory.
        public void CreateZIPFromDirectory(string sourceDirectory, string destinationFile) =>
            ZipFile.CreateFromDirectory(sourceDirectory, destinationFile);

        // Creates a ZIP file in the given directory.
        public void CreateZIPFile(string path)
        {
            using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
            {

            }
        }

        // Adds the given file to the given ZIP file.
        public void CreateZIPEntryFromFile(string zipFilePath, string sourceFilePath, string entryName)
        {
            using (ZipArchive zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Update))
            {
                zip.CreateEntryFromFile(sourceFilePath, entryName, CompressionLevel.Optimal);
            }
        }

        // Returns all the text in a given file.
        public string ReadAllText(string file) => File.ReadAllText(file);
    }
}
