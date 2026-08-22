// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.API.Services;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Common.Models.Requests;

namespace ServerBackupTool.PersistenceTests.API.Services
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
            string dbName = $"CommandServiceTest_{Guid.NewGuid():N}";
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
        /// Checks that LogCommand returns a valid ID when the insert succeeds.
        /// </summary>
        [TestMethod]
        public async Task LogCommand_ReturnsValidId_WhenSuccessful()
        {
            CommandRequestModel command = new()
            {
                Target = "Tool",
                Command = "stop"
            };

            (int? commandId, DateTime createdAt, Exception? ex) = await _CommandService.LogCommand(command);

            Assert.IsNotNull(commandId);
            Assert.AreEqual(
                1,
                commandId.Value);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that LogCommand uses IClock.UtcNow for the CreatedAt timestamp.
        /// </summary>
        [TestMethod]
        public async Task LogCommand_UsesClockUtcNow_ForCreatedAt()
        {
            DateTime fixedDateTime = new(2025, 1, 1, 10, 30, 0, DateTimeKind.Utc);
            _MockClock.Setup(c => c.UtcNow).Returns(fixedDateTime);

            CommandRequestModel command = new()
            {
                Target = "Server",
                Command = "say Hello"
            };

            (int? commandId, DateTime createdAt, Exception? ex) = await _CommandService.LogCommand(command);

            Assert.AreEqual(
                fixedDateTime,
                createdAt);
            _MockClock.Verify(
                c => c.UtcNow,
                Times.Once());
        }

        /// <summary>
        /// Checks that LogCommand returns a null ID when the database operation fails.
        /// </summary>
        [TestMethod]
        public async Task LogCommand_ReturnsNullId_WhenDatabaseFails()
        {
            string failDbName = $"CommandServiceFailTest_{Guid.NewGuid():N}";
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
                failMockLogger.Setup(l => l.RequestId).Returns(Guid.NewGuid());

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

                (int? commandId, DateTime createdAt, Exception? ex) = await failService.LogCommand(command);

                Assert.IsNull(commandId);
                Assert.IsNotNull(ex);
            }
            finally
            {
                await failKeepAlive.CloseAsync();
                await failKeepAlive.DisposeAsync();
            }
        }
    }
}
