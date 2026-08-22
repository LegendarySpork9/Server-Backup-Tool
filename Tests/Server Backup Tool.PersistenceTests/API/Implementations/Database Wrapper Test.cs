// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.PersistenceTests.API.Implementations
{
    [TestClass]
    public class DatabaseWrapperTest
    {
        private record LogRecord(
            int Id,
            string ServerName,
            string Timestamp,
            string Level,
            string Logger,
            string Message);

        private SqliteConnection _KeepAlive = null!;
        private DatabaseWrapper _Wrapper = null!;

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
            string dbName = $"ApiDatabaseWrapperTest_{Guid.NewGuid():N}";
            string connectionString = $"{dbName};Mode=Memory;Cache=Shared";

            _KeepAlive = new SqliteConnection($"Data Source={connectionString}");
            await _KeepAlive.OpenAsync();

            using (SqliteCommand command = new(
                CreateTableSql,
                _KeepAlive))
            {
                await command.ExecuteNonQueryAsync();
            }

            DatabaseOptionsModel options = new()
            {
                Path = connectionString,
                ServerName = "TestServer",
                PollingIntervalMs = 1000
            };

            _Wrapper = new DatabaseWrapper(options);
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
        /// Seeds the database with the given number of rows.
        /// </summary>
        private async Task SeedRows(
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
        /// Checks that querying inserted rows returns the correct count.
        /// </summary>
        [TestMethod]
        public async Task Query_ReturnsInsertedRows()
        {
            await SeedRows(3);

            string querySql = "SELECT Id, ServerName, Timestamp, Level, Logger, Message FROM Logs";

            (List<LogRecord> results, Exception? exception) = await _Wrapper.Query(
                querySql,
                reader => new LogRecord(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)));

            Assert.AreEqual(
                3,
                results.Count);
            Assert.IsNull(exception);
        }

        /// <summary>
        /// Checks that querying an empty table returns an empty list.
        /// </summary>
        [TestMethod]
        public async Task Query_EmptyTable_ReturnsEmptyList()
        {
            string querySql = "SELECT Id, ServerName, Timestamp, Level, Logger, Message FROM Logs";

            (List<LogRecord> results, Exception? exception) = await _Wrapper.Query(
                querySql,
                reader => new LogRecord(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)));

            Assert.AreEqual(
                0,
                results.Count);
            Assert.IsNull(exception);
        }

        /// <summary>
        /// Checks that querying with parameters filters correctly.
        /// </summary>
        [TestMethod]
        public async Task Query_WithParameters_FiltersCorrectly()
        {
            await SeedRows(
                3,
                serverName: "Server1");
            await SeedRows(
                2,
                serverName: "Server2",
                level: "Debug",
                logger: "Server");

            string querySql = "SELECT Id, ServerName, Timestamp, Level, Logger, Message FROM Logs WHERE ServerName = @name";

            SqliteParameter[] queryParameters =
            [
                new("@name", SqliteType.Text) { Value = "Server1" }
            ];

            (List<LogRecord> results, Exception? exception) = await _Wrapper.Query(
                querySql,
                reader => new LogRecord(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)),
                queryParameters);

            Assert.AreEqual(
                3,
                results.Count);
            Assert.IsNull(exception);
            Assert.IsTrue(results.TrueForAll(r => r.ServerName == "Server1"));
        }

        /// <summary>
        /// Checks that querying with invalid SQL returns an exception.
        /// </summary>
        [TestMethod]
        public async Task Query_InvalidSql_ReturnsException()
        {
            string querySql = "SELECT * FROM NonExistentTable";

            (List<LogRecord> results, Exception? exception) = await _Wrapper.Query(
                querySql,
                reader => new LogRecord(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)));

            Assert.IsNotNull(exception);
        }
    }
}
