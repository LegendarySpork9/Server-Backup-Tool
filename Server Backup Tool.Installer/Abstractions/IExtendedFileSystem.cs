// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for the extended file system operations.
    /// </summary>
    public interface IExtendedFileSystem : Common.Abstractions.IFileSystem
    {
        void CopyFile(string source, string destination, bool overwrite);
    }
}
