// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Steps;
using ServerBackupTool.Installer.Values;
using System.Xml.Linq;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class ConfigGenerationStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private ConfigWriter _ConfigWriter = null!;
        private string _TempDir = null!;

        /// <summary>
        /// Initialises the test dependencies and temp directory.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();

            IExtendedFileSystem fileSystem = new ExtendedFileSystemWrapper();

            _ConfigWriter = new ConfigWriter(
                _MockLogger.Object,
                fileSystem);

            _TempDir = Path.Combine(
                Path.GetTempPath(),
                $"SBT_ConfigGenStepTest_{Guid.NewGuid():N}");

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
        /// Checks that Execute completes and writes a valid XML config file to disk.
        /// </summary>
        [TestMethod]
        public async Task Execute_Completes_AndWritesValidConfigToDisk()
        {
            InstallOptionsModel options = new()
            {
                InstallPath = _TempDir,
                ServerConfig = new ServerConfigModel
                {
                    ServerName = "TestServer",
                    Game = "Minecraft",
                    ServerDirectory = @"C:\GameServer",
                    StartFile = "server.jar",
                    IPAddress = "192.168.1.100",
                    DatabasePath = @"C:\ProgramData\Data.db",
                    PollingIntervalMs = 1000
                },
                TimerConfig = new TimerConfigModel
                {
                    BackupTime = "03:00:00",
                    CustomTimers = []
                }
            };

            ConfigGenerationStep step = new(
                _MockLogger.Object,
                _ConfigWriter,
                options);

            await step.Execute();

            string configPath = Path.Combine(
                _TempDir,
                InstallerValues.Defaults.ToolConfigFileName);

            Assert.IsTrue(
                File.Exists(configPath),
                "Expected config file to exist on disk after Execute.");

            XDocument loaded = XDocument.Load(configPath);

            Assert.IsNotNull(loaded.Root);
            Assert.AreEqual(
                "configuration",
                loaded.Root.Name.LocalName);

            XElement? serverBackup = loaded.Root.Element("serverBackup");

            Assert.IsNotNull(
                serverBackup,
                "Expected serverBackup element in generated config.");
        }
    }
}
