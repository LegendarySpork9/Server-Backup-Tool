// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Abstractions;

namespace ServerBackupTool.Common.Implementations
{
    public class FileSystem : IFileSystem
    {
        // Directory Operations

        /// <summary>
        /// Returns whether the directory exists for a given path.
        /// </summary>
        public bool DirectoryExists(string path) => Directory.Exists(path);

        /// <summary>
        /// Returns all the files in a given path.
        /// </summary>
        public IEnumerable<string> GetFiles(string path) => Directory.GetFiles(path);

        /// <summary>
        /// Returns all the files in a given path matching the search pattern and option.
        /// </summary>
        public IEnumerable<string> GetFiles(
            string path,
            string searchPattern,
            SearchOption searchOption) => Directory.GetFiles(
                path,
                searchPattern,
                searchOption);

        /// <summary>
        /// Creates the directory for a given path.
        /// </summary>
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        /// <summary>
        /// Deletes the directory at the given path.
        /// </summary>
        public void DeleteDirectory(string path, bool recursive = false) => Directory.Delete(
            path,
            recursive);

        // File Operations

        /// <summary>
        /// Returns the UTC date and time for when the given file was created.
        /// </summary>
        public DateTime GetCreationTime(string file) => File.GetCreationTimeUtc(file);

        /// <summary>
        /// Returns whether the file exists for a given path.
        /// </summary>
        public bool FileExists(string path) => File.Exists(path);

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
    }
}
