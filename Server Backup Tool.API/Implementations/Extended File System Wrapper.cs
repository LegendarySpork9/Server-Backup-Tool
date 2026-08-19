// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.Common.Implementations;
using System.IO.Compression;

namespace ServerBackupTool.API.Implementations
{
    public class ExtendedFileSystemWrapper : FileSystem, IExtendedFileSystem
    {
        // Directory Operations

        /// <summary>
        /// Deletes the given directory.
        /// </summary>
        public void DeleteDirectory(string path) => Directory.Delete(
            path,
            true);

        // File Operations

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public long GetFileSize(string file) => new FileInfo(file).Length;

        /// <summary>
        /// Gets the last write time of the file.
        /// </summary>
        public DateTime GetLastWriteTime(string file) => File.GetLastWriteTimeUtc(file);

        /// <summary>
        /// Returns all the lines in a given file.
        /// </summary>
        public async Task<string[]> ReadAllLines(string file) => await File.ReadAllLinesAsync(file);

        // ZIP Operations

        /// <summary>
        /// Extracts the given ZIP file to the given directory.
        /// </summary>
        public void ExtractZIPToDirectory(
            string zip,
            string destination) =>
            ZipFile.ExtractToDirectory(
                zip,
                destination);
    }
}
