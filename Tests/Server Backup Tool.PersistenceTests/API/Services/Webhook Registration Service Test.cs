// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.API.Models.Requests;
using ServerBackupTool.API.Services;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.PersistenceTests.API.Services
{
    [TestClass]
    public class WebhookRegistrationServiceTest
    {
        private SqliteConnection _KeepAlive = null!;
        private WebhookRegistrationService _WebhookRegistrationService = null!;
        private Mock<IClock> _MockClock = null!;
        private string ServerName = null!;

        private const string CreateLogTableSql = @"
            CREATE TABLE Logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ServerName TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Level TEXT NOT NULL,
                Logger TEXT NOT NULL,
                Message TEXT NOT NULL
            );";

        private const string CreateWebhookTableSql = @"
            CREATE TABLE Webhooks (
                Id TEXT PRIMARY KEY,
                Url TEXT NOT NULL,
                LogType TEXT NOT NULL,
                LogLevel TEXT NOT NULL,
                AfterId INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            );";

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public async Task Setup()
        {
            string dbName = $"WebhookRegistrationServiceTest_{Guid.NewGuid():N}";
            string connectionString = $"{dbName};Mode=Memory;Cache=Shared";
            ServerName = "TestServer";

            _KeepAlive = new SqliteConnection($"Data Source={connectionString}");
            await _KeepAlive.OpenAsync();

            using (SqliteCommand command = new(
                CreateLogTableSql,
                _KeepAlive))
            {
                await command.ExecuteNonQueryAsync();
            }

            using (SqliteCommand command = new(
                CreateWebhookTableSql,
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

            ExtendedDatabaseWrapper database = new(dbOptions);

            Mock<ILoggerService> mockLogger = new();
            mockLogger.Setup(l => l.RequestId).Returns(Guid.NewGuid());

            _MockClock = new Mock<IClock>();
            _MockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));

            _WebhookRegistrationService = new WebhookRegistrationService(
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
        /// Checks that Register returns a valid ID when the insert succeeds.
        /// </summary>
        [TestMethod]
        public async Task Register_ReturnsValidId_WhenSuccessful()
        {
            WebhookRegistrationRequestModel registration = new()
            {
                Url = "https://example.com/webhook",
                LogType = "All",
                LogLevel = "All",
                AfterId = 1
            };

            (string? webhookId, Exception? ex) = await _WebhookRegistrationService.Register(registration);

            Assert.IsNotNull(webhookId);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that Register sets AfterId to the max log ID when AfterId is zero.
        /// </summary>
        [TestMethod]
        public async Task Register_SetsAfterIdToMaxLogId_WhenZero()
        {
            using (SqliteCommand command = new(
                @"INSERT INTO Logs (ServerName, Timestamp, Level, Logger, Message) VALUES
                    ('TestServer', '2025-06-15T12:00:00', 'Info', 'Tool', 'Message 1'),
                    ('TestServer', '2025-06-15T12:01:00', 'Info', 'Tool', 'Message 2'),
                    ('TestServer', '2025-06-15T12:02:00', 'Info', 'Tool', 'Message 3');",
                _KeepAlive))
            {
                await command.ExecuteNonQueryAsync();
            }

            WebhookRegistrationRequestModel registration = new()
            {
                Url = "https://example.com/webhook",
                LogType = "All",
                LogLevel = "All",
                AfterId = 0
            };

            (string? webhookId, Exception? ex) = await _WebhookRegistrationService.Register(registration);

            Assert.IsNotNull(webhookId);
            Assert.IsNull(ex);

            using (SqliteCommand query = new(
                @"SELECT AfterId FROM Webhooks WHERE Id = @id",
                _KeepAlive))
            {
                query.Parameters.Add(new SqliteParameter("@id", SqliteType.Text) { Value = webhookId });

                object? result = await query.ExecuteScalarAsync();

                Assert.IsNotNull(result);
                Assert.AreEqual(
                    3,
                    Convert.ToInt32(result));
            }
        }

        /// <summary>
        /// Checks that Unregister returns true when the webhook exists.
        /// </summary>
        [TestMethod]
        public async Task Unregister_ReturnsTrue_WhenExists()
        {
            WebhookRegistrationRequestModel registration = new()
            {
                Url = "https://example.com/webhook",
                LogType = "All",
                LogLevel = "All",
                AfterId = 1
            };

            (string? webhookId, Exception? _) = await _WebhookRegistrationService.Register(registration);

            Assert.IsNotNull(webhookId);

            (bool removed, Exception? ex) = await _WebhookRegistrationService.Unregister(webhookId);

            Assert.IsTrue(removed);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that Unregister returns false when the webhook does not exist.
        /// </summary>
        [TestMethod]
        public async Task Unregister_ReturnsFalse_WhenNotExists()
        {
            (bool removed, Exception? ex) = await _WebhookRegistrationService.Unregister(Guid.NewGuid().ToString());

            Assert.IsFalse(removed);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that GetAll returns all registered webhooks.
        /// </summary>
        [TestMethod]
        public async Task GetAll_ReturnsRegistrations()
        {
            WebhookRegistrationRequestModel registration1 = new()
            {
                Url = "https://example.com/webhook1",
                LogType = "Tool",
                LogLevel = "Info",
                AfterId = 1
            };

            WebhookRegistrationRequestModel registration2 = new()
            {
                Url = "https://example.com/webhook2",
                LogType = "Server",
                LogLevel = "Warn",
                AfterId = 2
            };

            await _WebhookRegistrationService.Register(registration1);
            await _WebhookRegistrationService.Register(registration2);

            (var registrations, Exception? ex) = await _WebhookRegistrationService.GetAll();

            Assert.IsNotNull(registrations);
            Assert.AreEqual(
                2,
                registrations.Count);
            Assert.IsNull(ex);
        }

        /// <summary>
        /// Checks that UpdateAfterId returns true when the webhook exists.
        /// </summary>
        [TestMethod]
        public async Task UpdateAfterId_ReturnsTrue_WhenExists()
        {
            WebhookRegistrationRequestModel registration = new()
            {
                Url = "https://example.com/webhook",
                LogType = "All",
                LogLevel = "All",
                AfterId = 1
            };

            (string? webhookId, Exception? _) = await _WebhookRegistrationService.Register(registration);

            Assert.IsNotNull(webhookId);

            (bool updated, Exception? ex) = await _WebhookRegistrationService.UpdateAfterId(webhookId, 10);

            Assert.IsTrue(updated);
            Assert.IsNull(ex);
        }
    }
}
