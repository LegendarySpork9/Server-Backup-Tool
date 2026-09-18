// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Services;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.PersistenceTests.API.Services
{
    [TestClass]
    public class LogPollingServiceTest
    {
        private SqliteConnection _KeepAlive = null!;
        private ExtendedDatabaseWrapper _Database = null!;
        private DatabaseOptionsModel _Options = null!;
        private Mock<IWebhookDispatchService> _MockDispatchService = null!;
        private IServiceScopeFactory _ScopeFactory = null!;

        private const string CreateLogTableSql = @"
            CREATE TABLE Logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ServerName TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Level TEXT NOT NULL,
                Type TEXT NOT NULL,
                Message TEXT NOT NULL
            );";

        private const string CreateWebhookTableSql = @"
            CREATE TABLE Webhooks (
                Id TEXT PRIMARY KEY,
                Url TEXT NOT NULL,
                ServerName TEXT NOT NULL,
                LogType TEXT NOT NULL,
                LogLevel TEXT NOT NULL,
                AfterId INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            );";

        /// <summary>
        /// Initialises the in-memory database, real services, and DI scope factory.
        /// </summary>
        [TestInitialize]
        public async Task Setup()
        {
            string dbName = $"LogPollingServiceTest_{Guid.NewGuid():N}";
            string connectionString = $"{dbName};Mode=Memory;Cache=Shared";

            _Options = new DatabaseOptionsModel
            {
                Path = connectionString,
                ServerName = "TestServer",
                PollingIntervalMs = 50
            };

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

            _Database = new ExtendedDatabaseWrapper(_Options);

            Mock<ILoggerService> mockLogger = new();
            mockLogger.Setup(l => l.RequestId).Returns(Guid.NewGuid());

            Mock<IClock> mockClock = new();
            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            _MockDispatchService = new Mock<IWebhookDispatchService>();

            ServiceCollection services = new();
            services.AddSingleton<ILoggerService>(mockLogger.Object);
            services.AddSingleton<IExtendedDatabase>(_Database);
            services.AddSingleton<IClock>(mockClock.Object);
            services.AddSingleton(_Options);
            services.AddScoped<IWebhookRegistrationService, WebhookRegistrationService>();
            services.AddScoped<IWebhookDispatchService>(_ => _MockDispatchService.Object);

            ServiceProvider provider = services.BuildServiceProvider();
            _ScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        }

        /// <summary>
        /// Cleans up the in-memory database.
        /// </summary>
        [TestCleanup]
        public async Task Cleanup()
        {
            await _KeepAlive.CloseAsync();
            await _KeepAlive.DisposeAsync();
        }

        /// <summary>
        /// Inserts a log entry into the in-memory database.
        /// </summary>
        private async Task InsertLog(
            string message,
            string level = "Info",
            string type = "Tool")
        {
            string sql = @"INSERT INTO Logs (ServerName, Timestamp, Level, Type, Message)
                VALUES (@serverName, @timestamp, @level, @type, @message)";

            using (SqliteCommand command = new(
                sql,
                _KeepAlive))
            {
                command.Parameters.AddWithValue(
                    "@serverName",
                    "TestServer");
                command.Parameters.AddWithValue(
                    "@timestamp",
                    DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue(
                    "@level",
                    level);
                command.Parameters.AddWithValue(
                    "@type",
                    type);
                command.Parameters.AddWithValue(
                    "@message",
                    message);

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserts a webhook registration into the in-memory database.
        /// </summary>
        private async Task InsertWebhook(
            string id,
            string url,
            int afterId = 0,
            string logType = "All",
            string logLevel = "All")
        {
            string sql = @"INSERT INTO Webhooks (Id, Url, ServerName, LogType, LogLevel, AfterId, CreatedAt)
                VALUES (@id, @url, @serverName, @logType, @logLevel, @afterId, @createdAt)";

            using (SqliteCommand command = new(
                sql,
                _KeepAlive))
            {
                command.Parameters.AddWithValue(
                    "@id",
                    id);
                command.Parameters.AddWithValue(
                    "@url",
                    url);
                command.Parameters.AddWithValue(
                    "@serverName",
                    "TestServer");
                command.Parameters.AddWithValue(
                    "@logType",
                    logType);
                command.Parameters.AddWithValue(
                    "@logLevel",
                    logLevel);
                command.Parameters.AddWithValue(
                    "@afterId",
                    afterId);
                command.Parameters.AddWithValue(
                    "@createdAt",
                    DateTime.UtcNow.ToString("o"));

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Reads the current AfterId for a webhook from the in-memory database.
        /// </summary>
        private async Task<int> GetWebhookAfterId(string webhookId)
        {
            string sql = "SELECT AfterId FROM Webhooks WHERE Id = @id";

            object? result = 0;

            using (SqliteCommand command = new(
                sql,
                _KeepAlive))
            {
                command.Parameters.AddWithValue(
                    "@id",
                    webhookId);

                result = await command.ExecuteScalarAsync();
            }
            

            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Checks that PollAndDispatch does not dispatch when no webhooks are registered.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_DoesNotDispatch_WhenNoWebhooksRegistered()
        {
            await InsertLog("Test log entry");

            LogPollingService service = new(
                _Database,
                _ScopeFactory,
                _Options);

            using CancellationTokenSource cts = new();

            await service.StartAsync(cts.Token);
            await Task.Delay(150);

            cts.Cancel();

            await service.StopAsync(CancellationToken.None);

            _MockDispatchService.Verify(
                d => d.Send(It.IsAny<string>(), It.IsAny<WebhookPayloadModel>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that PollAndDispatch does not dispatch when no new logs exist.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_DoesNotDispatch_WhenNoNewLogs()
        {
            await InsertWebhook(
                "wh-1",
                "https://example.com/hook",
                afterId: 999);

            LogPollingService service = new(
                _Database,
                _ScopeFactory,
                _Options);

            using CancellationTokenSource cts = new();

            await service.StartAsync(cts.Token);
            await Task.Delay(150);

            cts.Cancel();

            await service.StopAsync(CancellationToken.None);

            _MockDispatchService.Verify(
                d => d.Send(It.IsAny<string>(), It.IsAny<WebhookPayloadModel>()),
                Times.Never());
        }

        /// <summary>
        /// Checks that PollAndDispatch dispatches new logs and updates the webhook cursor.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_DispatchesNewLogs_AndUpdatesCursor()
        {
            await InsertWebhook(
                "wh-1",
                "https://example.com/hook",
                afterId: 0);
            await InsertLog("Server started");

            _MockDispatchService
                .Setup(d => d.Send(It.IsAny<string>(), It.IsAny<WebhookPayloadModel>()))
                .ReturnsAsync((true, (Exception?)null));

            LogPollingService service = new(
                _Database,
                _ScopeFactory,
                _Options);

            using CancellationTokenSource cts = new();

            await service.StartAsync(cts.Token);
            await Task.Delay(200);

            cts.Cancel();

            await service.StopAsync(CancellationToken.None);

            _MockDispatchService.Verify(
                d => d.Send("https://example.com/hook", It.Is<WebhookPayloadModel>(p =>
                    p.ServerName == "TestServer" &&
                    p.Logs.Count == 1 &&
                    p.Logs[0].Message == "Server started")),
                Times.AtLeastOnce());

            int updatedAfterId = await GetWebhookAfterId("wh-1");

            Assert.IsTrue(
                updatedAfterId > 0,
                $"Expected AfterId to be updated but got: {updatedAfterId}");
        }

        /// <summary>
        /// Checks that PollAndDispatch does not update the cursor when dispatch fails.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_DoesNotUpdateCursor_WhenDispatchFails()
        {
            await InsertWebhook(
                "wh-1",
                "https://example.com/hook",
                afterId: 0);
            await InsertLog("Server started");

            _MockDispatchService
                .Setup(d => d.Send(It.IsAny<string>(), It.IsAny<WebhookPayloadModel>()))
                .ReturnsAsync((false, new HttpRequestException("Connection refused")));

            LogPollingService service = new(
                _Database,
                _ScopeFactory,
                _Options);

            using CancellationTokenSource cts = new();

            await service.StartAsync(cts.Token);
            await Task.Delay(200);

            cts.Cancel();

            await service.StopAsync(CancellationToken.None);

            int afterId = await GetWebhookAfterId("wh-1");

            Assert.AreEqual(
                0,
                afterId,
                $"Expected AfterId to remain 0 after failed dispatch but got: {afterId}");
        }

        /// <summary>
        /// Checks that PollAndDispatch only sends logs after the webhook's AfterId.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_FiltersLogs_ByWebhookAfterId()
        {
            await InsertLog("Old log 1");
            await InsertLog("Old log 2");
            await InsertLog("New log");

            await InsertWebhook(
                "wh-1",
                "https://example.com/hook",
                afterId: 2);

            _MockDispatchService
                .Setup(d => d.Send(It.IsAny<string>(), It.IsAny<WebhookPayloadModel>()))
                .ReturnsAsync((true, (Exception?)null));

            LogPollingService service = new(
                _Database,
                _ScopeFactory,
                _Options);

            using CancellationTokenSource cts = new();

            await service.StartAsync(cts.Token);
            await Task.Delay(200);

            cts.Cancel();

            await service.StopAsync(CancellationToken.None);

            _MockDispatchService.Verify(
                d => d.Send("https://example.com/hook", It.Is<WebhookPayloadModel>(p =>
                    p.Logs.Count == 1 &&
                    p.Logs[0].Message == "New log")),
                Times.AtLeastOnce());

            int updatedAfterId = await GetWebhookAfterId("wh-1");

            Assert.AreEqual(
                3,
                updatedAfterId,
                $"Expected AfterId to be 3 but got: {updatedAfterId}");
        }

        /// <summary>
        /// Checks that PollAndDispatch skips a webhook when no logs match its filter criteria.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_SkipsWebhook_WhenNoLogsMatchFilter()
        {
            await InsertLog(
                "Tool log",
                level: "Info",
                type: "Tool");
            await InsertWebhook(
                "wh-1",
                "https://example.com/hook",
                afterId: 0,
                logType: "API");

            LogPollingService service = new(
                _Database,
                _ScopeFactory,
                _Options);

            using CancellationTokenSource cts = new();

            await service.StartAsync(cts.Token);
            await Task.Delay(200);

            cts.Cancel();

            await service.StopAsync(CancellationToken.None);

            _MockDispatchService.Verify(
                d => d.Send(It.IsAny<string>(), It.IsAny<WebhookPayloadModel>()),
                Times.Never());
        }
    }
}
