// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Implementations;
using System.IO.Compression;

namespace ServerBackupTool.Implementations
{
    public class ExtendedFileSystemWrapper : FileSystem, IExtendedFileSystem
    {
        // Directory Operations

        /// <summary>
        /// Returns whether the directory exists for a given path.
        /// </summary>
        public bool DirectoryExists(string path) => Directory.Exists(path);

        /// <summary>
        /// Creates the directory for a given path.
        /// </summary>
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        // File Operations

        /// <summary>
        /// Deletes the given file.
        /// </summary>
        public void DeleteFile(string file) => File.Delete(file);

        /// <summary>
        /// Returns all the text in a given file.
        /// </summary>
        public Task<string> ReadAllText(string file) => File.ReadAllTextAsync(file);

        /// <summary>
        /// Writes text to a given file asynchronously.
        /// </summary>
        public Task WriteAllText(
            string path,
            string content) => File.WriteAllTextAsync(
                path,
                content);

        // ZIP Operations

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
    }
}
