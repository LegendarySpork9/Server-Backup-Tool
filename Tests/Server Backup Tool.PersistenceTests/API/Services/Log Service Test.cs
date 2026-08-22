// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Entities;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Models.Responses.Related;
using ServerBackupTool.API.Services;
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.PersistenceTests.API.Services
{
    [TestClass]
    public class LogServiceTest
    {
        private SqliteConnection _KeepAlive = null!;
        private LogService _LogService = null!;
        private string ServerName = null!;

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
            string dbName = $"LogServiceTest_{Guid.NewGuid():N}";
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
            mockLogger.Setup(l => l.RequestId).Returns(Guid.NewGuid());

            Mock<IExtendedFileSystem> mockFileSystem = new();

            ArchiveSettingsModel archive = new() { ArchiveDirectory = "." };

            _LogService = new LogService(
                mockLogger.Object,
                database,
                mockFileSystem.Object,
                dbOptions,
                archive);
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
        /// Seeds the database with the given number of log rows.
        /// </summary>
        private async Task SeedLogs(
            int count,
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
                    ServerName);
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
                    $"Test message {i}");

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Checks that GetLogs returns logs when data exists.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_ReturnsLogs_WhenDataExists()
        {
            await SeedLogs(5);

            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs();

            Assert.IsNotNull(logs);
            Assert.AreEqual(
                5,
                logs.Count);
        }

        /// <summary>
        /// Checks that GetLogs returns null when no matching data exists.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_ReturnsNull_WhenNoMatchingData()
        {
            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs();

            Assert.IsNull(logs);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that GetLogs filters by log level.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_FiltersByLevel()
        {
            await SeedLogs(
                3,
                level: "Info");
            await SeedLogs(
                2,
                level: "Debug");

            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs(level: LogLevel.Info);

            Assert.IsNotNull(logs);
            Assert.AreEqual(
                3,
                logs.Count);
        }

        /// <summary>
        /// Checks that GetLogs filters by log type.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_FiltersByType()
        {
            await SeedLogs(
                3,
                logger: "Tool");
            await SeedLogs(
                2,
                logger: "Server");

            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs(type: LogType.Tool);

            Assert.IsNotNull(logs);
            Assert.AreEqual(
                3,
                logs.Count);
        }

        /// <summary>
        /// Checks that GetLogs respects the limit parameter.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_RespectsLimit()
        {
            await SeedLogs(10);

            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs(limit: 3);

            Assert.IsNotNull(logs);
            Assert.AreEqual(
                3,
                logs.Count);
        }

        /// <summary>
        /// Checks that GetLogs respects the afterId parameter.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_RespectsAfterId()
        {
            await SeedLogs(5);

            int maxId;

            using (SqliteCommand command = new(
                "SELECT MAX(Id) FROM Logs",
                _KeepAlive))
            {
                maxId = Convert.ToInt32(await command.ExecuteScalarAsync());
            }

            int afterId = maxId - 2;

            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs(afterId: afterId);

            Assert.IsNotNull(logs);
            Assert.IsTrue(logs.TrueForAll(l => l.Id < afterId));
        }

        /// <summary>
        /// Checks that GetLogs returns logs ordered by Id ascending.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_OrdersByIdAscending()
        {
            await SeedLogs(5);

            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs();

            Assert.IsNotNull(logs);

            for (int i = 1; i < logs.Count; i++)
            {
                Assert.IsTrue(
                    logs[i].Id > logs[i - 1].Id,
                    $"Log at index {i} (Id={logs[i].Id}) should be greater than log at index {i - 1} (Id={logs[i - 1].Id}).");
            }
        }

        /// <summary>
        /// Checks that GetLogs filters by both level and type simultaneously.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_FiltersByLevelAndType()
        {
            await SeedLogs(
                3,
                level: "Info",
                logger: "Tool");
            await SeedLogs(
                2,
                level: "Info",
                logger: "Server");
            await SeedLogs(
                2,
                level: "Debug",
                logger: "Tool");

            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs(
                level: LogLevel.Info,
                type: LogType.Tool);

            Assert.IsNotNull(logs);
            Assert.AreEqual(
                3,
                logs.Count);
        }

        /// <summary>
        /// Checks that GetLogs applies a limit to filtered results.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_FiltersByLevelWithLimit()
        {
            await SeedLogs(
                5,
                level: "Info");
            await SeedLogs(
                3,
                level: "Debug");

            (List<LogModel>? logs, Exception? ex) = await _LogService.GetLogs(
                level: LogLevel.Info,
                limit: 2);

            Assert.IsNotNull(logs);
            Assert.AreEqual(
                2,
                logs.Count);
        }

        /// <summary>
        /// Checks that pagination with afterId returns no overlapping results.
        /// </summary>
        [TestMethod]
        public async Task GetLogs_Pagination_NoOverlap()
        {
            await SeedLogs(10);

            (List<LogModel>? firstPage, Exception? ex1) = await _LogService.GetLogs(limit: 5);

            Assert.IsNotNull(firstPage);
            Assert.AreEqual(
                5,
                firstPage.Count);

            int nextAfter = firstPage.First().Id;

            (List<LogModel>? secondPage, Exception? ex2) = await _LogService.GetLogs(
                limit: 5,
                afterId: nextAfter);

            Assert.IsNotNull(secondPage);

            List<int> firstPageIds = firstPage.Select(l => l.Id).ToList();

            foreach (LogModel log in secondPage)
            {
                Assert.IsFalse(
                    firstPageIds.Contains(log.Id),
                    $"Log Id {log.Id} appeared on both pages.");
            }
        }
    }
}
