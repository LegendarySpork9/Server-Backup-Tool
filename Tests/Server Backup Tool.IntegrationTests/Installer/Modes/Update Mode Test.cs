// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Modes;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Modes
{
    [TestClass]
    public class UpdateModeTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IFileService> _MockFileService = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;
        private Mock<IConfigWriter> _MockConfigWriter = null!;
        private Mock<IDatabaseInitialiser> _MockDatabaseInitialiser = null!;
        private Mock<IRegistryService> _MockRegistry = null!;
        private Mock<IVersionService> _MockVersionService = null!;
        private Mock<IResourceService> _MockResourceService = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockFileService = new Mock<IFileService>();
            _MockFileSystem = new Mock<IExtendedFileSystem>();
            _MockConfigWriter = new Mock<IConfigWriter>();
            _MockDatabaseInitialiser = new Mock<IDatabaseInitialiser>();
            _MockRegistry = new Mock<IRegistryService>();
            _MockVersionService = new Mock<IVersionService>();
            _MockResourceService = new Mock<IResourceService>();
        }

        /// <summary>
        /// Checks that Execute shows an error when no installation is found.
        /// </summary>
        [TestMethod]
        public async Task Execute_ShowsError_WhenNoInstallationFound()
        {
            _MockVersionService
                .Setup(v => v.GetAllInstallations())
                .Returns([]);

            TestConsole console = new();
            console.Interactive();

            UpdateMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileService.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockDatabaseInitialiser.Object,
                _MockRegistry.Object,
                _MockVersionService.Object,
                _MockResourceService.Object);

            await mode.Execute();

            Assert.IsTrue(
                console.Output.Contains("No existing installation found"),
                $"Expected output to contain 'No existing installation found' but got: {console.Output}");
        }
    }
}
