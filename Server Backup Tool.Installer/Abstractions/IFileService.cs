// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for the file service.
    /// </summary>
    public interface IFileService
    {
        (bool, Exception?) CopyFiles(string sourcePath, string destinationPath, Action<string>? progressCallback = null);
        (bool, Exception?) BackupDirectory(string sourcePath, string backupPath);
        (bool, Exception?) DeleteDirectory(string path);
        Task<bool> ValidateWritePermissions(string path);
        string[] GetDeployableFiles(string sourcePath);
    }
}
