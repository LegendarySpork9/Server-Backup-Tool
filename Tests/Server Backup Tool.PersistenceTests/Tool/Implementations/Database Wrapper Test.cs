// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Implementations;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.PersistenceTests.Tool.Implementations
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
            string dbName = $"ToolDatabaseWrapperTest_{Guid.NewGuid():N}";
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
        /// Checks that inserting a row returns the number of rows affected.
        /// </summary>
        [TestMethod]
        public async Task Execute_InsertRow_ReturnsRowsAffected()
        {
            string sql = "INSERT INTO Logs (ServerName, Timestamp, Level, Logger, Message) VALUES (@serverName, @timestamp, @level, @logger, @message)";

            SqliteParameter[] parameters =
            [
                new("@serverName", SqliteType.Text) { Value = "TestServer" },
                new("@timestamp", SqliteType.Text) { Value = DateTime.UtcNow.ToString("O") },
                new("@level", SqliteType.Text) { Value = "Info" },
                new("@logger", SqliteType.Text) { Value = "Tool" },
                new("@message", SqliteType.Text) { Value = "Test message" }
            ];

            (int result, Exception? exception) = await _Wrapper.Execute(
                sql,
                parameters);

            Assert.AreEqual(
                1,
                result);
            Assert.IsNull(exception);
        }

        /// <summary>
        /// Checks that updating a row returns the number of rows affected.
        /// </summary>
        [TestMethod]
        public async Task Execute_UpdateRow_ReturnsRowsAffected()
        {
            string insertSql = "INSERT INTO Logs (ServerName, Timestamp, Level, Logger, Message) VALUES (@serverName, @timestamp, @level, @logger, @message)";

            SqliteParameter[] insertParameters =
            [
                new("@serverName", SqliteType.Text) { Value = "TestServer" },
                new("@timestamp", SqliteType.Text) { Value = DateTime.UtcNow.ToString("O") },
                new("@level", SqliteType.Text) { Value = "Info" },
                new("@logger", SqliteType.Text) { Value = "Tool" },
                new("@message", SqliteType.Text) { Value = "Original message" }
            ];

            await _Wrapper.Execute(
                insertSql,
                insertParameters);

            string updateSql = "UPDATE Logs SET Message = @message WHERE ServerName = @serverName";

            SqliteParameter[] updateParameters =
            [
                new("@message", SqliteType.Text) { Value = "Updated message" },
                new("@serverName", SqliteType.Text) { Value = "TestServer" }
            ];

            (int result, Exception? exception) = await _Wrapper.Execute(
                updateSql,
                updateParameters);

            Assert.AreEqual(
                1,
                result);
            Assert.IsNull(exception);
        }

        /// <summary>
        /// Checks that deleting a row returns the number of rows affected.
        /// </summary>
        [TestMethod]
        public async Task Execute_DeleteRow_ReturnsRowsAffected()
        {
            string insertSql = "INSERT INTO Logs (ServerName, Timestamp, Level, Logger, Message) VALUES (@serverName, @timestamp, @level, @logger, @message)";

            SqliteParameter[] insertParameters =
            [
                new("@serverName", SqliteType.Text) { Value = "TestServer" },
                new("@timestamp", SqliteType.Text) { Value = DateTime.UtcNow.ToString("O") },
                new("@level", SqliteType.Text) { Value = "Info" },
                new("@logger", SqliteType.Text) { Value = "Tool" },
                new("@message", SqliteType.Text) { Value = "To be deleted" }
            ];

            await _Wrapper.Execute(
                insertSql,
                insertParameters);

            string deleteSql = "DELETE FROM Logs WHERE ServerName = @serverName";

            SqliteParameter[] deleteParameters =
            [
                new("@serverName", SqliteType.Text) { Value = "TestServer" }
            ];

            (int result, Exception? exception) = await _Wrapper.Execute(
                deleteSql,
                deleteParameters);

            Assert.AreEqual(
                1,
                result);
            Assert.IsNull(exception);
        }

        /// <summary>
        /// Checks that invalid SQL returns an exception.
        /// </summary>
        [TestMethod]
        public async Task Execute_InvalidSql_ReturnsException()
        {
            string sql = "INSERT INTO NonExistent (Column1) VALUES ('test')";

            (int result, Exception? exception) = await _Wrapper.Execute(sql);

            Assert.AreEqual(
                -1,
                result);
            Assert.IsNotNull(exception);
        }

        /// <summary>
        /// Checks that querying a single row returns the correct mapped result.
        /// </summary>
        [TestMethod]
        public async Task QuerySingle_ReturnsMatchingRow()
        {
            string insertSql = "INSERT INTO Logs (ServerName, Timestamp, Level, Logger, Message) VALUES (@serverName, @timestamp, @level, @logger, @message)";

            SqliteParameter[] insertParameters =
            [
                new("@serverName", SqliteType.Text) { Value = "TestServer" },
                new("@timestamp", SqliteType.Text) { Value = "2025-01-01T00:00:00.0000000Z" },
                new("@level", SqliteType.Text) { Value = "Info" },
                new("@logger", SqliteType.Text) { Value = "Tool" },
                new("@message", SqliteType.Text) { Value = "Single row test" }
            ];

            await _Wrapper.Execute(
                insertSql,
                insertParameters);

            string querySql = "SELECT Id, ServerName, Timestamp, Level, Logger, Message FROM Logs WHERE Id = @id";

            SqliteParameter[] queryParameters =
            [
                new("@id", SqliteType.Integer) { Value = 1 }
            ];

            (LogRecord? result, Exception? exception) = await _Wrapper.QuerySingle(
                querySql,
                reader => new LogRecord(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)),
                queryParameters);

            Assert.IsNotNull(result);
            Assert.AreEqual(
                "TestServer",
                result.ServerName);
            Assert.AreEqual(
                "Single row test",
                result.Message);
            Assert.IsNull(exception);
        }

        /// <summary>
        /// Checks that querying a single row with no match returns default.
        /// </summary>
        [TestMethod]
        public async Task QuerySingle_NoMatch_ReturnsDefault()
        {
            string querySql = "SELECT Id, ServerName, Timestamp, Level, Logger, Message FROM Logs WHERE Id = @id";

            SqliteParameter[] queryParameters =
            [
                new("@id", SqliteType.Integer) { Value = 999 }
            ];

            (LogRecord? result, Exception? exception) = await _Wrapper.QuerySingle(
                querySql,
                reader => new LogRecord(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)),
                queryParameters);

            Assert.IsNull(result);
            Assert.IsNull(exception);
        }
    }
}
