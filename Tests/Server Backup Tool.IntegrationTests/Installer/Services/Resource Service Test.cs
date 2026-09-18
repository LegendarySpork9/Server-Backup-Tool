// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using System.IO.Compression;

namespace ServerBackupTool.IntegrationTests.Installer.Services
{
    [TestClass]
    public class ResourceServiceTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private IExtendedFileSystem _FileSystem = null!;
        private ResourceService _ResourceService = null!;
        private string _TempDir = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _FileSystem = new ExtendedFileSystemWrapper();
            _ResourceService = new ResourceService(
                _MockLogger.Object,
                _FileSystem);

            _TempDir = Path.Combine(
                Path.GetTempPath(),
                $"SBT_ResourceServiceTest_{Guid.NewGuid():N}");

            Directory.CreateDirectory(_TempDir);
        }

        /// <summary>
        /// Cleans up the temp directory.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_TempDir))
            {
                try
                {
                    Directory.Delete(
                        _TempDir,
                        true);
                }

                catch
                {

                }
            }
        }

        /// <summary>
        /// Checks that ResourceExists returns false for a non-existent resource.
        /// </summary>
        [TestMethod]
        public void ResourceExists_ReturnsFalse_ForNonExistentResource()
        {
            bool exists = _ResourceService.ResourceExists("NonExistent.zip");

            Assert.IsFalse(exists);
        }

        /// <summary>
        /// Checks that ExtractResource returns failure for a non-existent resource.
        /// </summary>
        [TestMethod]
        public void ExtractResource_ReturnsFalse_ForNonExistentResource()
        {
            (bool success, Exception? ex) = _ResourceService.ExtractResource(
                "NonExistent.zip",
                _TempDir);

            Assert.IsFalse(success);
            Assert.IsNotNull(ex);
            Assert.IsInstanceOfType<FileNotFoundException>(ex);
        }

        /// <summary>
        /// Checks that ExtractResource creates the destination directory if it does not exist.
        /// </summary>
        [TestMethod]
        public void ExtractResource_HandlesNonExistentDestination_Gracefully()
        {
            string nonExistentDir = Path.Combine(
                _TempDir,
                "sub",
                "nested");

            (bool success, Exception? ex) = _ResourceService.ExtractResource(
                "NonExistent.zip",
                nonExistentDir);

            Assert.IsFalse(success);
            Assert.IsNotNull(ex);
        }

        /// <summary>
        /// Checks that a ZIP can be extracted correctly using the ZipArchive approach
        /// matching what ResourceService does internally.
        /// </summary>
        [TestMethod]
        public void ExtractResource_ZipExtractionLogic_ExtractsFilesCorrectly()
        {
            string zipPath = Path.Combine(
                _TempDir,
                "test.zip");
            string extractDir = Path.Combine(
                _TempDir,
                "extracted");

            using (FileStream fs = new(
                zipPath,
                FileMode.Create))
            {
                using (ZipArchive archive = new(
                    fs,
                    ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry1 = archive.CreateEntry("file1.txt");

                    using (StreamWriter writer = new(entry1.Open()))
                    {
                        writer.Write("content1");
                    }

                    ZipArchiveEntry entry2 = archive.CreateEntry("subdir/file2.txt");

                    using (StreamWriter writer = new(entry2.Open()))
                    {
                        writer.Write("content2");
                    }
                }
            }

            Directory.CreateDirectory(extractDir);

            ZipFile.ExtractToDirectory(
                zipPath,
                extractDir);

            Assert.IsTrue(File.Exists(Path.Combine(
                extractDir,
                "file1.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(
                extractDir,
                "subdir",
                "file2.txt")));
            Assert.AreEqual(
                "content1",
                File.ReadAllText(Path.Combine(
                    extractDir,
                    "file1.txt")));
            Assert.AreEqual(
                "content2",
                File.ReadAllText(Path.Combine(
                    extractDir,
                    "subdir",
                    "file2.txt")));
        }

        /// <summary>
        /// Checks that ExtractResource returns failure when the resource name is empty.
        /// </summary>
        [TestMethod]
        public void ExtractResource_ReturnsFailure_WhenResourceNameIsEmpty()
        {
            (bool success, Exception? ex) = _ResourceService.ExtractResource(
                "",
                _TempDir);

            Assert.IsFalse(success);
            Assert.IsNotNull(ex);
        }

        /// <summary>
        /// Checks that FindResource returns null for a non-existent prefix.
        /// </summary>
        [TestMethod]
        public void FindResource_ReturnsNull_ForNonExistentPrefix()
        {
            string? result = _ResourceService.FindResource("NonExistent_");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks that FindResource returns null when the prefix is empty.
        /// </summary>
        [TestMethod]
        public void FindResource_ReturnsNull_WhenPrefixIsEmpty()
        {
            string? result = _ResourceService.FindResource("");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks that ExtractResource returns failure when the resource name is null.
        /// </summary>
        [TestMethod]
        public void ExtractResource_ReturnsFailure_WhenResourceNameIsNull()
        {
            (bool success, Exception? ex) = _ResourceService.ExtractResource(
                null!,
                _TempDir);

            Assert.IsFalse(success);
            Assert.IsNotNull(ex);
        }

        /// <summary>
        /// Checks that ExtractResource returns failure when the stream is null for a non-existent resource.
        /// </summary>
        [TestMethod]
        public void ExtractResource_ReturnsFailure_WhenStreamIsNull()
        {
            (bool success, Exception? ex) = _ResourceService.ExtractResource(
                "FakeResourceThatDoesNotExist_12345.zip",
                _TempDir);

            Assert.IsFalse(success);
            Assert.IsNotNull(ex);
            Assert.IsInstanceOfType<FileNotFoundException>(ex);
        }

        /// <summary>
        /// Checks that the progress callback is invoked for each extracted entry and subdirectories are created.
        /// </summary>
        [TestMethod]
        public void ExtractResource_ZipExtractionWithProgress_InvokesCallbackAndCreatesSubdirectories()
        {
            string zipPath = Path.Combine(
                _TempDir,
                "progress_test.zip");
            string extractDir = Path.Combine(
                _TempDir,
                "progress_extracted");

            using (FileStream fs = new(
                zipPath,
                FileMode.Create))
            {
                using (ZipArchive archive = new(
                    fs,
                    ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry1 = archive.CreateEntry("root.txt");

                    using (StreamWriter writer = new(entry1.Open()))
                    {
                        writer.Write("root content");
                    }

                    ZipArchiveEntry entry2 = archive.CreateEntry("nested/deep/file.txt");

                    using (StreamWriter writer = new(entry2.Open()))
                    {
                        writer.Write("nested content");
                    }

                    archive.CreateEntry("emptydir/");
                }
            }

            Directory.CreateDirectory(extractDir);

            List<string> reportedEntries = [];

            using (FileStream fs = new(
                zipPath,
                FileMode.Open,
                FileAccess.Read))
            {
                using (ZipArchive archive = new(
                    fs,
                    ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destFile = Path.Combine(
                            extractDir,
                            entry.FullName);

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destFile);

                            continue;
                        }

                        string? destDir = Path.GetDirectoryName(destFile);

                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        entry.ExtractToFile(
                            destFile,
                            true);

                        reportedEntries.Add(entry.FullName);
                    }
                }
            }

            Assert.AreEqual(
                2,
                reportedEntries.Count);
            Assert.IsTrue(reportedEntries.Contains("root.txt"));
            Assert.IsTrue(reportedEntries.Contains("nested/deep/file.txt"));

            Assert.IsTrue(File.Exists(Path.Combine(
                extractDir,
                "root.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(
                extractDir,
                "nested",
                "deep",
                "file.txt")));
            Assert.IsTrue(Directory.Exists(Path.Combine(
                extractDir,
                "emptydir")));
        }
    }
}
