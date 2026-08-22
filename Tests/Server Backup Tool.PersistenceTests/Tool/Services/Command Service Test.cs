// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Entities;
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Common.Models.Requests;
using ServerBackupTool.Implementations;
using ServerBackupTool.Services;

namespace ServerBackupTool.PersistenceTests.Tool.Services
{
    [TestClass]
    public class CommandServiceTest
    {
        private SqliteConnection _KeepAlive = null!;
        private CommandService _CommandService = null!;
        private Mock<IClock> _MockClock = null!;
        private string ServerName = null!;

        private const string CreateTableSql = @"
            CREATE TABLE Commands (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ServerName TEXT NOT NULL,
                Target TEXT NOT NULL,
                Command TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );";

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public async Task Setup()
        {
            string dbName = $"ToolCommandServiceTest_{Guid.NewGuid():N}";
            string connectionString = $"{dbName};Mode=Memory;Cache=Shared";
            ServerName = "TestServer";

            _KeepAlive = new SqliteConnection($"Data Source={connectionString}");
            await _KeepAlive.OpenAsync();

            using (SqliteCommand command = new(
                CreateTableSql,
                _KeepAlive))
            {
                await command.ExecuteNonQueryAsync();
            }

            DatabaseOptionsModel dbOptions = new()
            {
                Path = connectionString,
                ServerName = ServerName,
                PollingIntervalMs = 1000
            };

            DatabaseWrapper database = new(dbOptions);

            Mock<ILoggerService> mockLogger = new();

            _MockClock = new Mock<IClock>();
            _MockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));

            _CommandService = new CommandService(
                mockLogger.Object,
                database,
                _MockClock.Object,
                dbOptions);
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
        /// Seeds a command into the database.
        /// </summary>
        private async Task SeedCommand(
            string serverName = "TestServer",
            string target = "Tool",
            string command = "stop",
            string? createdAt = null)
        {
            string insertSql = "INSERT INTO Commands (ServerName, Target, Command, CreatedAt) VALUES (@serverName, @target, @command, @createdAt)";

            using SqliteCommand cmd = new(
                insertSql,
                _KeepAlive);
            cmd.Parameters.AddWithValue(
                "@serverName",
                serverName);
            cmd.Parameters.AddWithValue(
                "@target",
                target);
            cmd.Parameters.AddWithValue(
                "@command",
                command);
            cmd.Parameters.AddWithValue(
                "@createdAt",
                createdAt ?? DateTime.UtcNow.ToString("O"));

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Checks that GetCommand returns a command when the queue has an entry.
        /// </summary>
        [TestMethod]
        public async Task GetCommand_ReturnsCommand_WhenQueueHasEntry()
        {
            await SeedCommand();

            (Models.CommandModel? command, Exception? ex) = await _CommandService.GetCommand();

            Assert.IsNotNull(command);
            Assert.AreEqual(
                TargetType.Tool,
                command.Target);
            Assert.AreEqual(
                "stop",
                command.Command);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that GetCommand returns null when the queue is empty.
        /// </summary>
        [TestMethod]
        public async Task GetCommand_ReturnsNull_WhenQueueIsEmpty()
        {
            (Models.CommandModel? command, Exception? ex) = await _CommandService.GetCommand();

            Assert.IsNull(command);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that GetCommand only returns commands for the configured server name.
        /// </summary>
        [TestMethod]
        public async Task GetCommand_FiltersCorrectly_ByServerName()
        {
            await SeedCommand(serverName: "OtherServer");

            (Models.CommandModel? command, Exception? ex) = await _CommandService.GetCommand();

            Assert.IsNull(command);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that GetCommand returns the oldest command first.
        /// </summary>
        [TestMethod]
        public async Task GetCommand_ReturnsOldestFirst()
        {
            await SeedCommand(
                command: "first",
                createdAt: "2025-01-01T00:00:00.0000000Z");
            await SeedCommand(
                command: "second",
                createdAt: "2025-01-02T00:00:00.0000000Z");

            (Models.CommandModel? command, Exception? ex) = await _CommandService.GetCommand();

            Assert.IsNotNull(command);
            Assert.AreEqual(
                "first",
                command.Command);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that LogCommand returns true when the insert succeeds.
        /// </summary>
        [TestMethod]
        public async Task LogCommand_ReturnsTrue_WhenSuccessful()
        {
            CommandRequestModel command = new()
            {
                Target = "Tool",
                Command = "stop"
            };

            (bool success, Exception? ex) = await _CommandService.LogCommand(command);

            Assert.IsTrue(success);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that LogCommand returns false when the database operation fails.
        /// </summary>
        [TestMethod]
        public async Task LogCommand_ReturnsFalse_WhenDatabaseFails()
        {
            string failDbName = $"ToolCommandServiceFailTest_{Guid.NewGuid():N}";
            string failConnectionString = $"{failDbName};Mode=Memory;Cache=Shared";

            SqliteConnection failKeepAlive = new($"Data Source={failConnectionString}");
            await failKeepAlive.OpenAsync();

            try
            {
                DatabaseOptionsModel failDbOptions = new()
                {
                    Path = failConnectionString,
                    ServerName = ServerName,
                    PollingIntervalMs = 1000
                };

                DatabaseWrapper failDatabase = new(failDbOptions);

                Mock<ILoggerService> failMockLogger = new();

                CommandService failService = new(
                    failMockLogger.Object,
                    failDatabase,
                    _MockClock.Object,
                    failDbOptions);

                CommandRequestModel command = new()
                {
                    Target = "Tool",
                    Command = "stop"
                };

                (bool success, Exception? ex) = await failService.LogCommand(command);

                Assert.IsFalse(success);
                Assert.IsNotNull(ex);
            }
            finally
            {
                await failKeepAlive.CloseAsync();
                await failKeepAlive.DisposeAsync();
            }
        }

        /// <summary>
        /// Checks that DeleteCommand returns true when the ID exists.
        /// </summary>
        [TestMethod]
        public async Task DeleteCommand_ReturnsTrue_WhenIdExists()
        {
            await SeedCommand();

            (bool success, Exception? ex) = await _CommandService.DeleteCommand(1);

            Assert.IsTrue(success);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that DeleteCommand returns false when the ID does not exist.
        /// </summary>
        [TestMethod]
        public async Task DeleteCommand_ReturnsFalse_WhenIdDoesNotExist()
        {
            (bool success, Exception? ex) = await _CommandService.DeleteCommand(999);

            Assert.IsFalse(success);
            Assert.IsNull(ex);
        }
    }
}
