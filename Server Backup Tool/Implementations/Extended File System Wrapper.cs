// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Implementations;
using System.IO.Compression;

namespace ServerBackupTool.Implementations
{
    public class ExtendedFileSystemWrapper : FileSystem, IExtendedFileSystem
    {
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
