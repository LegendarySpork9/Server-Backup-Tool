// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Implementations;
using ServerBackupTool.Services;

namespace ServerBackupTool.PersistenceTests.Tool.Services
{
    [TestClass]
    public class LogServiceTest
    {
        private SqliteConnection _KeepAlive = null!;
        private LogService _LogService = null!;
        private Mock<IClock> _MockClock = null!;

        private const string CreateTableSql = @"
            CREATE TABLE Logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ServerName TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Level TEXT NOT NULL,
                Logger TEXT NOT NULL,
                Message TEXT NOT NULL
            );";

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public async Task Setup()
        {
            string dbName = $"ToolLogServiceTest_{Guid.NewGuid():N}";
            string connectionString = $"{dbName};Mode=Memory;Cache=Shared";

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
                ServerName = "TestServer",
                PollingIntervalMs = 1000
            };

            DatabaseWrapper database = new(dbOptions);

            Mock<ILoggerService> mockLogger = new();

            _MockClock = new Mock<IClock>();
            _MockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));

            _LogService = new LogService(
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
        /// Seeds log rows into the database.
        /// </summary>
        private async Task SeedLogs(
            int count,
            string serverName = "TestServer",
            string level = "Info",
            string logger = "Tool")
        {
            string insertSql = "INSERT INTO Logs (ServerName, Timestamp, Level, Logger, Message) VALUES (@serverName, @timestamp, @level, @logger, @message)";

            for (int i = 1; i <= count; i++)
            {
                using SqliteCommand command = new(
                    insertSql,
                    _KeepAlive);
                command.Parameters.AddWithValue(
                    "@serverName",
                    serverName);
                command.Parameters.AddWithValue(
                    "@timestamp",
                    DateTime.UtcNow.ToString("O"));
                command.Parameters.AddWithValue(
                    "@level",
                    level);
                command.Parameters.AddWithValue(
                    "@logger",
                    logger);
                command.Parameters.AddWithValue(
                    "@message",
                    $"Message {i}");

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Gets the count of log rows in the database.
        /// </summary>
        private async Task<long> GetLogCount(string? logger = null)
        {
            string sql = logger != null
                ? "SELECT COUNT(*) FROM Logs WHERE Logger = @logger"
                : "SELECT COUNT(*) FROM Logs";

            using SqliteCommand command = new(
                sql,
                _KeepAlive);

            if (logger != null)
            {
                command.Parameters.AddWithValue(
                    "@logger",
                    logger);
            }

            object? result = await command.ExecuteScalarAsync();
            return (long)(result ?? 0);
        }

        /// <summary>
        /// Checks that LogMessage inserts a record when successful.
        /// </summary>
        [TestMethod]
        public async Task LogMessage_InsertsRecord_WhenSuccessful()
        {
            await _LogService.LogMessage(
                "Info",
                "Tool",
                "Test message");

            long count = await GetLogCount();

            Assert.AreEqual(
                1,
                count);
        }

        /// <summary>
        /// Checks that LogMessage handles an exception when the database fails.
        /// </summary>
        [TestMethod]
        public async Task LogMessage_HandlesException_WhenDatabaseFails()
        {
            string failDbName = $"ToolLogServiceFailTest_{Guid.NewGuid():N}";
            string failConnectionString = $"{failDbName};Mode=Memory;Cache=Shared";

            SqliteConnection failKeepAlive = new($"Data Source={failConnectionString}");
            await failKeepAlive.OpenAsync();

            try
            {
                DatabaseOptionsModel failDbOptions = new()
                {
                    Path = failConnectionString,
                    ServerName = "TestServer",
                    PollingIntervalMs = 1000
                };

                DatabaseWrapper failDatabase = new(failDbOptions);

                Mock<ILoggerService> failMockLogger = new();

                Mock<IClock> failMockClock = new();
                failMockClock.Setup(c => c.UtcNow)
                    .Returns(DateTime.UtcNow);

                LogService failService = new(
                    failMockLogger.Object,
                    failDatabase,
                    failMockClock.Object,
                    failDbOptions);

                await failService.LogMessage(
                    "Info",
                    "Tool",
                    "Test message");

                failMockLogger.Verify(
                    l => l.LogToolMessage(
                        "Warn",
                        It.IsAny<string>(),
                        false),
                    Times.Once());
            }
            finally
            {
                await failKeepAlive.CloseAsync();
                await failKeepAlive.DisposeAsync();
            }
        }

        /// <summary>
        /// Checks that ClearLogs returns true when logs exist for the given type.
        /// </summary>
        [TestMethod]
        public async Task ClearLogs_ReturnsTrue_WhenLogsExist()
        {
            await SeedLogs(
                3,
                logger: "Server");

            (bool success, Exception? ex) = await _LogService.ClearLogs("Server");

            Assert.IsTrue(success);
            Assert.IsNull(ex);

            long count = await GetLogCount("Server");
            Assert.AreEqual(
                0,
                count);
        }

        /// <summary>
        /// Checks that ClearLogs returns false when no logs exist for the given type.
        /// </summary>
        [TestMethod]
        public async Task ClearLogs_ReturnsFalse_WhenNoLogsExist()
        {
            (bool success, Exception? ex) = await _LogService.ClearLogs("Server");

            Assert.IsFalse(success);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that ClearLogs only deletes logs for the specified type.
        /// </summary>
        [TestMethod]
        public async Task ClearLogs_DeletesCorrectType_WhenMultipleTypesExist()
        {
            await SeedLogs(
                3,
                logger: "Tool");
            await SeedLogs(
                2,
                logger: "Server");

            (bool success, Exception? ex) = await _LogService.ClearLogs("Server");

            Assert.IsTrue(success);
            Assert.IsNull(ex);

            long toolCount = await GetLogCount("Tool");
            long serverCount = await GetLogCount("Server");

            Assert.AreEqual(
                3,
                toolCount);
            Assert.AreEqual(
                0,
                serverCount);
        }
    }
}
