// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Steps;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class DatabaseSetupStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private IExtendedFileSystem _FileSystem = null!;
        private DatabaseInitialiser _DatabaseInitialiser = null!;
        private SqliteConnection _KeepAlive = null!;
        private string _ConnectionString = null!;

        /// <summary>
        /// Initialises the test dependencies and in-memory database.
        /// </summary>
        [TestInitialize]
        public async Task TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _FileSystem = new ExtendedFileSystemWrapper();

            string dbName = $"DatabaseSetupStepTest_{Guid.NewGuid():N}";
            _ConnectionString = $"{dbName};Mode=Memory;Cache=Shared";

            _KeepAlive = new SqliteConnection($"Data Source={_ConnectionString}");

            await _KeepAlive.OpenAsync();

            _DatabaseInitialiser = new DatabaseInitialiser(
                _MockLogger.Object,
                _FileSystem);
        }

        /// <summary>
        /// Cleans up the in-memory database.
        /// </summary>
        [TestCleanup]
        public async Task TestCleanup()
        {
            await _KeepAlive.CloseAsync();
            await _KeepAlive.DisposeAsync();
        }

        /// <summary>
        /// Checks that Execute completes and creates the expected tables in the database.
        /// </summary>
        [TestMethod]
        public async Task Execute_Completes_AndCreatesExpectedTables()
        {
            InstallOptionsModel options = new()
            {
                ServerConfig = new ServerConfigModel
                {
                    DatabasePath = _ConnectionString
                }
            };

            DatabaseSetupStep step = new(
                _MockLogger.Object,
                _DatabaseInitialiser,
                options);

            await step.Execute();

            string[] expectedTables =
            [
                "Logs",
                "Commands",
                "Webhooks"
            ];

            foreach (string table in expectedTables)
            {
                using (SqliteCommand command = new(
                    $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{table}'",
                    _KeepAlive))
                {
                    long count = (long)(await command.ExecuteScalarAsync())!;

                    Assert.AreEqual(
                        1,
                        count,
                        $"Expected table '{table}' to exist after Execute.");
                }
            }
        }
    }
}
