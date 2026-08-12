// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using System.IO.Compression;

namespace ServerBackupTool.Implementations
{
    public class FileSystem : IFileSystem
    {
        /// <summary>
        /// Returns whether the directory exists for a given path.
        /// </summary>
        public bool DirectoryExists(string path) => Directory.Exists(path);

        /// <summary>
        /// Creates the directory for a given path.
        /// </summary>
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        /// <summary>
        /// Returns all the files in a given path.
        /// </summary>
        public IEnumerable<string> GetFiles(string path) => Directory.GetFiles(path);

        /// <summary>
        /// Deletes the given file.
        /// </summary>
        public void DeleteFile(string file) => File.Delete(file);

        /// <summary>
        /// Returns the UTC date and time for when the given file was created.
        /// </summary>
        public DateTime GetCreationTime(string file) => File.GetCreationTimeUtc(file);

        /// <summary>
        /// Creates a ZIP file from the given directory.
        /// </summary>
        public void CreateZIPFromDirectory(
            string sourceDirectory,
            string destinationFile) =>
            ZipFile.CreateFromDirectory(
                sourceDirectory,
                destinationFile);

        /// <summary>
        /// Creates a ZIP file in the given directory.
        /// </summary>
        public void CreateZIPFile(string path)
        {
            using ZipArchive zip = ZipFile.Open(
                path,
                ZipArchiveMode.Create);
        }

        /// <summary>
        /// Adds the given file to the given ZIP file.
        /// </summary>
        public void CreateZIPEntryFromFile(
            string zipFilePath,
            string sourceFilePath,
            string entryName)
        {
            using (ZipArchive zip = ZipFile.Open(
                zipFilePath,
                ZipArchiveMode.Update))
            {
                zip.CreateEntryFromFile(
                    sourceFilePath,
                    entryName,
                    CompressionLevel.Optimal);
            }
        }

        /// <summary>
        /// Returns all the text in a given file.
        /// </summary>
        public string ReadAllText(string file) => File.ReadAllText(file);

        /// <summary>
        /// Writes text to a given file asynchronously.
        /// </summary>
        public Task WriteAllText(
            string path,
            string content) => File.WriteAllTextAsync(
                path,
                content);

        /// <summary>
        /// Returns whether the file exists for a given path.
        /// </summary>
        public bool FileExists(string path) => File.Exists(path);
    }
}
