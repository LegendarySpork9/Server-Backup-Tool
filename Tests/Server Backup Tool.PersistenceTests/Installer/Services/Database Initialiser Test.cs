// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;

namespace ServerBackupTool.PersistenceTests.Installer.Services
{
    [TestClass]
    public class DatabaseInitialiserTest
    {
        private SqliteConnection _KeepAlive = null!;
        private Mock<IExtendedFileSystem> _MockFileSystem = null!;
        private DatabaseInitialiser _DatabaseInitialiser = null!;
        private string _ConnectionString = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public async Task Setup()
        {
            string dbName = $"DatabaseInitialiserTest_{Guid.NewGuid():N}";
            _ConnectionString = $"{dbName};Mode=Memory;Cache=Shared";

            _KeepAlive = new SqliteConnection($"Data Source={_ConnectionString}");
            await _KeepAlive.OpenAsync();

            Mock<ILoggerService> mockLogger = new();
            _MockFileSystem = new Mock<IExtendedFileSystem>();
            _DatabaseInitialiser = new DatabaseInitialiser(
                mockLogger.Object,
                _MockFileSystem.Object);
        }

        /// <summary>
        /// Cleans up the test environment.
        /// </summary>
        [TestCleanup]
        public async Task Cleanup()
        {
            await _KeepAlive.CloseAsync();
            await _KeepAlive.DisposeAsync();
        }

        /// <summary>
        /// Checks that InitialiseDatabase creates the Logs table.
        /// </summary>
        [TestMethod]
        public async Task InitialiseDatabase_CreatesLogsTable()
        {
            await _DatabaseInitialiser.InitialiseDatabase(_ConnectionString);

            using (SqliteCommand command = new(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Logs'",
                _KeepAlive))
            {
                long count = (long)(await command.ExecuteScalarAsync())!;

                Assert.AreEqual(
                    1,
                    count);
            }
        }

        /// <summary>
        /// Checks that InitialiseDatabase creates the Commands table.
        /// </summary>
        [TestMethod]
        public async Task InitialiseDatabase_CreatesCommandsTable()
        {
            await _DatabaseInitialiser.InitialiseDatabase(_ConnectionString);

            using (SqliteCommand command = new(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Commands'",
                _KeepAlive))
            {
                long count = (long)(await command.ExecuteScalarAsync())!;

                Assert.AreEqual(
                    1,
                    count);
            }
        }

        /// <summary>
        /// Checks that InitialiseDatabase creates the Webhooks table.
        /// </summary>
        [TestMethod]
        public async Task InitialiseDatabase_CreatesWebhooksTable()
        {
            await _DatabaseInitialiser.InitialiseDatabase(_ConnectionString);

            using (SqliteCommand command = new(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Webhooks'",
                _KeepAlive))
            {
                long count = (long)(await command.ExecuteScalarAsync())!;

                Assert.AreEqual(
                    1,
                    count);
            }
        }

        /// <summary>
        /// Checks that InitialiseDatabase creates at least 4 indexes.
        /// </summary>
        [TestMethod]
        public async Task InitialiseDatabase_CreatesIndexes()
        {
            await _DatabaseInitialiser.InitialiseDatabase(_ConnectionString);

            using (SqliteCommand command = new(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name LIKE 'IX_%'",
                _KeepAlive))
            {
                long count = (long)(await command.ExecuteScalarAsync())!;

                Assert.IsTrue(
                    count >= 4,
                    $"Expected at least 4 index(es) but found {count}.");
            }
        }

        /// <summary>
        /// Checks that InitialiseDatabase executes the WAL pragma without error.
        /// </summary>
        [TestMethod]
        public async Task InitialiseDatabase_ExecutesWalPragmaWithoutError()
        {
            (bool success, Exception? ex) = await _DatabaseInitialiser.InitialiseDatabase(_ConnectionString);

            Assert.IsTrue(success);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that ValidateDatabase returns false when the required tables are missing.
        /// </summary>
        [TestMethod]
        public async Task ValidateDatabase_ReturnsFalse_WhenTablesAreMissing()
        {
            string emptyDbName = $"EmptyDb_{Guid.NewGuid():N}";
            string emptyConnectionString = $"{emptyDbName};Mode=Memory;Cache=Shared";

            SqliteConnection emptyKeepAlive = new($"Data Source={emptyConnectionString}");
            await emptyKeepAlive.OpenAsync();

            try
            {
                (bool result, Exception? ex) = await _DatabaseInitialiser.ValidateDatabase(emptyConnectionString);

                Assert.IsFalse(
                    result,
                    "Expected ValidateDatabase to return false for an empty database.");
                Assert.IsNull(ex);
            }

            finally
            {
                await emptyKeepAlive.CloseAsync();
                await emptyKeepAlive.DisposeAsync();
            }
        }
    }
}
