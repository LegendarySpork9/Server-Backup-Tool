// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Modes;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Modes
{
    [TestClass]
    public class ConfigureModeTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;
        private Mock<IConfigWriter> _MockConfigWriter = null!;
        private Mock<IVersionService> _MockVersionService = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockFileSystem = new Mock<IExtendedFileSystem>();
            _MockConfigWriter = new Mock<IConfigWriter>();
            _MockVersionService = new Mock<IVersionService>();
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

            ConfigureMode mode = new(
                console,
                _MockLogger.Object,
                _MockFileSystem.Object,
                _MockConfigWriter.Object,
                _MockVersionService.Object);

            await mode.Execute();

            Assert.IsTrue(
                console.Output.Contains("No existing installation found"),
                $"Expected output to contain 'No existing installation found' but got: {console.Output}");
        }
    }
}
