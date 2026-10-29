// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Values;
using System.IO.Compression;
using System.Reflection;

namespace ServerBackupTool.Installer.Implementations
{
    public class ResourceService : IResourceService
    {
        private readonly ILoggerService _Logger;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly Assembly _Assembly;

        // Sets the class's global variables.
        public ResourceService(
            ILoggerService logger,
            IExtendedFileSystem fileSystem)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
            _Assembly = Assembly.GetExecutingAssembly();
        }

        /// <summary>
        /// Finds an embedded resource whose name contains the given prefix followed by the ZIP extension.
        /// </summary>
        public string? FindResource(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "FindResource called with an empty prefix.");

                return null;
            }

            string? matchingResource = _Assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.Contains(
                    prefix,
                    StringComparison.OrdinalIgnoreCase) && n.EndsWith(
                        InstallerValues.Resources.ZipExtension,
                        StringComparison.OrdinalIgnoreCase));

            if (matchingResource != null)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Found resource matching prefix '{prefix}': {matchingResource}.");
            }

            else
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"No resource found matching prefix '{prefix}'.");
            }

            return matchingResource;
        }

        /// <summary>
        /// Extracts an embedded ZIP resource to the specified destination directory.
        /// </summary>
        public (bool, Exception?) ExtractResource(
            string resourceName,
            string destinationPath,
            Action<string>? progressCallback = null)
        {
            bool extracted = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Extracting resource '{resourceName}' to {destinationPath}.");

                string? fullResourceName = null;

                if (!string.IsNullOrEmpty(resourceName))
                {
                    fullResourceName = _Assembly.GetManifestResourceNames()
                        .FirstOrDefault(n => n.EndsWith(
                            resourceName,
                            StringComparison.OrdinalIgnoreCase));
                }

                if (fullResourceName == null)
                {
                    string error = $"Embedded resource '{resourceName}' not found.";

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        error);

                    exception = new FileNotFoundException(error);
                }

                else
                {
                    using (Stream? stream = _Assembly.GetManifestResourceStream(fullResourceName))
                    {
                        if (stream == null)
                        {
                            string error = $"Failed to open resource stream for '{resourceName}'.";

                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Error,
                                error);

                            exception = new InvalidOperationException(error);
                        }

                        else
                        {
                            if (!_FileSystem.DirectoryExists(destinationPath))
                            {
                                _FileSystem.CreateDirectory(destinationPath);
                            }

                            using (ZipArchive archive = new(
                                stream,
                                ZipArchiveMode.Read))
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    string destFile = Path.Combine(
                                        destinationPath,
                                        entry.FullName);

                                    if (string.IsNullOrEmpty(entry.Name))
                                    {
                                        _FileSystem.CreateDirectory(destFile);

                                        continue;
                                    }

                                    string? destDir = Path.GetDirectoryName(destFile);

                                    if (!string.IsNullOrEmpty(destDir) && !_FileSystem.DirectoryExists(destDir))
                                    {
                                        _FileSystem.CreateDirectory(destDir);
                                    }

                                    entry.ExtractToFile(
                                        destFile,
                                        true);

                                    progressCallback?.Invoke(entry.FullName);
                                }

                                _Logger.LogMessage(
                                    StandardValues.LoggerValues.Info,
                                    $"Extracted {archive.Entries.Count} entry(s) from '{resourceName}'.");

                                extracted = true;
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to extract resource '{resourceName}': {ex.Message}");

                exception = ex;
            }

            return (
                extracted,
                exception);
        }

        /// <summary>
        /// Checks whether an embedded resource exists.
        /// </summary>
        public bool ResourceExists(string resourceName)
        {
            return _Assembly.GetManifestResourceNames()
                .Any(n => n.EndsWith(
                    resourceName,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
