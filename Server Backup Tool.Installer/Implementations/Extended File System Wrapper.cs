// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Common.Implementations;

namespace ServerBackupTool.Installer.Implementations
{
    public class ExtendedFileSystemWrapper : FileSystem, IExtendedFileSystem
    {
        /// <summary>
        /// Copies a file to a destination, optionally overwriting.
        /// </summary>
        public void CopyFile(string source, string destination, bool overwrite) => File.Copy(
            source,
            destination,
            overwrite);
    }
}
