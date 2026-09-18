// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;

namespace ServerBackupTool.Installer.Implementations
{
    public class FileService : IFileService
    {
        private readonly ILoggerService _Logger;
        private readonly IExtendedFileSystem _FileSystem;

        // Sets the class's global variables.
        public FileService(
            ILoggerService logger,
            IExtendedFileSystem fileSystem)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
        }

        /// <summary>
        /// Copies all files from the source to the destination directory.
        /// </summary>
        public (bool, Exception?) CopyFiles(
            string sourcePath,
            string destinationPath,
            Action<string>? progressCallback = null)
        {
            bool copied = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Copying files from {sourcePath} to {destinationPath}.");

                if (!_FileSystem.DirectoryExists(destinationPath))
                {
                    _FileSystem.CreateDirectory(destinationPath);
                }

                string[] files = GetDeployableFiles(sourcePath);

                foreach (string file in files)
                {
                    string relativePath = Path.GetRelativePath(
                        sourcePath,
                        file);
                    string destFile = Path.Combine(
                        destinationPath,
                        relativePath);
                    string? destDir = Path.GetDirectoryName(destFile);

                    if (!string.IsNullOrEmpty(destDir) && !_FileSystem.DirectoryExists(destDir))
                    {
                        _FileSystem.CreateDirectory(destDir);
                    }

                    _FileSystem.CopyFile(
                        file,
                        destFile,
                        true);

                    progressCallback?.Invoke(relativePath);
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Copied {files.Length} file(s).");

                copied = true;
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Copy Files: {ex.Message}");

                exception = ex;
            }

            return (
                copied,
                exception);
        }

        /// <summary>
        /// Creates a backup of the specified directory.
        /// </summary>
        public (bool, Exception?) BackupDirectory(
            string sourcePath,
            string backupPath)
        {
            bool backedUp = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Backing up {sourcePath} to {backupPath}.");

                string timestampedPath = Path.Combine(
                    backupPath,
                    $"Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}");

                CopyFiles(
                    sourcePath,
                    timestampedPath);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Backup created.");

                backedUp = true;
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Create Backup: {ex.Message}");

                exception = ex;
            }

            return (
                backedUp,
                exception);
        }

        /// <summary>
        /// Deletes the specified directory and all its contents.
        /// </summary>
        public (bool, Exception?) DeleteDirectory(string path)
        {
            bool deleted = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Deleting directory {path}.");

                if (_FileSystem.DirectoryExists(path))
                {
                    _FileSystem.DeleteDirectory(
                        path,
                        true);
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Directory deleted.");

                deleted = true;
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Delete Directory: {ex.Message}");

                exception = ex;
            }

            return (
                deleted,
                exception);
        }

        /// <summary>
        /// Validates that the specified path is writable.
        /// </summary>
        public async Task<bool> ValidateWritePermissions(string path)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Validating write permissions for {path}.");

            bool writable = false;
            bool createdDirectory = false;

            try
            {
                if (!_FileSystem.DirectoryExists(path))
                {
                    _FileSystem.CreateDirectory(path);

                    createdDirectory = true;
                }

                string testFile = Path.Combine(
                    path,
                    $".sbt_write_test_{Guid.NewGuid():N}");

                await _FileSystem.WriteAllText(
                    testFile,
                    "test");
                _FileSystem.DeleteFile(testFile);

                writable = true;
            }

            catch
            {

            }

            if (createdDirectory && _FileSystem.DirectoryExists(path))
            {
                try
                {
                    _FileSystem.DeleteDirectory(path);
                }

                catch
                {

                }
            }

            _Logger.LogMessage(
                writable ? StandardValues.LoggerValues.Info : StandardValues.LoggerValues.Warning,
                $"Write permission validation for {path}: {(writable ? "Passed" : "Failed")}.");

            return writable;
        }

        /// <summary>
        /// Gets all deployable files from the source directory.
        /// </summary>
        public string[] GetDeployableFiles(string sourcePath)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Getting deployable files from {sourcePath}.");

            string[] files = [];

            if (_FileSystem.DirectoryExists(sourcePath))
            {
                files = [.. _FileSystem.GetFiles(
                    sourcePath,
                    "*",
                    SearchOption.AllDirectories)];
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Found {files.Length} deployable file(s).");

            return files;
        }
    }
}
