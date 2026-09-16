// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;

namespace ServerBackupTool.IntegrationTests.Installer.Services
{
    [TestClass]
    public class FileServiceTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private IExtendedFileSystem _FileSystem = null!;
        private FileService _FileService = null!;
        private string _TempBase = null!;
        private string _SourceDir = null!;
        private string _DestDir = null!;

        /// <summary>
        /// Initialises the test dependencies and temp directories with test files.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _FileSystem = new ExtendedFileSystemWrapper();
            _FileService = new FileService(
                _MockLogger.Object,
                _FileSystem);

            _TempBase = Path.Combine(
                Path.GetTempPath(),
                $"SBT_FileServiceTest_{Guid.NewGuid():N}");

            _SourceDir = Path.Combine(
                _TempBase,
                "source");
            _DestDir = Path.Combine(
                _TempBase,
                "dest");

            Directory.CreateDirectory(_SourceDir);
            Directory.CreateDirectory(_DestDir);

            File.WriteAllText(
                Path.Combine(
                    _SourceDir,
                    "file1.txt"),
                "content1");
            File.WriteAllText(
                Path.Combine(
                    _SourceDir,
                    "file2.txt"),
                "content2");
            File.WriteAllText(
                Path.Combine(
                    _SourceDir,
                    "file3.txt"),
                "content3");
        }

        /// <summary>
        /// Cleans up the temp directories.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_TempBase))
            {
                try
                {
                    Directory.Delete(
                        _TempBase,
                        true);
                }

                catch
                {

                }
            }
        }

        /// <summary>
        /// Checks that ValidateWritePermissions returns true for a writable path.
        /// </summary>
        [TestMethod]
        public async Task ValidateWritePermissions_ReturnsTrue_ForWritablePath()
        {
            bool result = await _FileService.ValidateWritePermissions(_TempBase);

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Checks that ValidateWritePermissions returns false for an invalid path.
        /// </summary>
        [TestMethod]
        public async Task ValidateWritePermissions_ReturnsFalse_ForNonExistentPath()
        {
            bool result = await _FileService.ValidateWritePermissions(@"\\?\invalid:path<>");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks that CopyFiles copies all files to the destination directory.
        /// </summary>
        [TestMethod]
        public void CopyFiles_CopiesAllFilesToDestination()
        {
            (bool success, Exception? error) = _FileService.CopyFiles(
                _SourceDir,
                _DestDir);

            Assert.IsTrue(success);
            Assert.IsNull(error);

            int copiedCount = Directory.GetFiles(_DestDir).Length;

            Assert.AreEqual(
                3,
                copiedCount);
        }

        /// <summary>
        /// Checks that CopyFiles invokes the progress callback for each file.
        /// </summary>
        [TestMethod]
        public void CopyFiles_InvokesProgressCallback_ForEachFile()
        {
            int callbackCount = 0;

            _FileService.CopyFiles(
                _SourceDir,
                _DestDir,
                _ => callbackCount++);

            Assert.AreEqual(
                3,
                callbackCount);
        }

        /// <summary>
        /// Checks that BackupDirectory creates a timestamped backup containing the source files.
        /// </summary>
        [TestMethod]
        public void BackupDirectory_CreatesTimestampedBackup()
        {
            string backupRoot = Path.Combine(_TempBase, "backups");
            Directory.CreateDirectory(backupRoot);

            (bool success, Exception? error) = _FileService.BackupDirectory(
                _SourceDir,
                backupRoot);

            Assert.IsTrue(success);
            Assert.IsNull(error);

            string[] backupDirs = Directory.GetDirectories(backupRoot);

            Assert.AreEqual(
                1,
                backupDirs.Length);
            Assert.IsTrue(Path.GetFileName(backupDirs[0])
                .StartsWith("Backup_"));

            int fileCount = Directory.GetFiles(backupDirs[0]).Length;

            Assert.AreEqual(
                3,
                fileCount);
        }

        /// <summary>
        /// Checks that ValidateWritePermissions cleans up a directory it created.
        /// </summary>
        [TestMethod]
        public async Task ValidateWritePermissions_CleansUpCreatedDirectory()
        {
            string testPath = Path.Combine(
                _TempBase,
                $"writePerm_{Guid.NewGuid():N}");

            Assert.IsFalse(Directory.Exists(testPath));

            await _FileService.ValidateWritePermissions(testPath);

            Assert.IsFalse(Directory.Exists(testPath));
        }

        /// <summary>
        /// Checks that DeleteDirectory returns true when the directory does not exist.
        /// </summary>
        [TestMethod]
        public void DeleteDirectory_ReturnsTrue_WhenDirectoryDoesNotExist()
        {
            string nonExistentPath = Path.Combine(
                _TempBase,
                $"nonExistent_{Guid.NewGuid():N}");

            (bool success, Exception? error) = _FileService.DeleteDirectory(nonExistentPath);

            Assert.IsTrue(success);
            Assert.IsNull(error);
        }

        /// <summary>
        /// Checks that DeleteDirectory removes the directory and all its contents.
        /// </summary>
        [TestMethod]
        public void DeleteDirectory_RemovesDirectoryAndContents()
        {
            string dirToDelete = Path.Combine(
                _TempBase,
                "toDelete");
            Directory.CreateDirectory(dirToDelete);
            File.WriteAllText(
                Path.Combine(
                    dirToDelete,
                    "file.txt"),
                "data");

            (bool success, Exception? error) = _FileService.DeleteDirectory(dirToDelete);

            Assert.IsTrue(success);
            Assert.IsNull(error);
            Assert.IsFalse(Directory.Exists(dirToDelete));
        }
    }
}
