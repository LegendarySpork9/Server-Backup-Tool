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

        // File Operations

        /// <summary>
        /// Returns the UTC date and time for when the given file was created.
        /// </summary>
        public DateTime GetCreationTime(string file) => File.GetCreationTimeUtc(file);

        /// <summary>
        /// Returns whether the file exists for a given path.
        /// </summary>
        public bool FileExists(string path) => File.Exists(path);
    }
}
