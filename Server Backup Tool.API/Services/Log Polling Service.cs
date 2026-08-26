// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Entities;
using ServerBackupTool.API.Implementations;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Models.Responses.Related;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;
using System.Text.Json;

namespace ServerBackupTool.API.Services
{
    public class LogPollingService : BackgroundService
    {
        private readonly ILoggerService _Logger = new LoggerServiceWrapper("LogPollingService");
        private readonly IExtendedDatabase _Database;
        private readonly IHttpClientFactory _HttpClientFactory;
        private readonly IClock _Clock;
        private readonly DatabaseOptionsModel Options;
        private readonly WebhookSettingsModel WebhookSettings;
        private readonly JsonSerializerOptions JsonOptions;

        public LogPollingService(
            IExtendedDatabase _database,
            IHttpClientFactory _httpClientFactory,
            IClock _clock,
            DatabaseOptionsModel options,
            WebhookSettingsModel webhookSettings,
            IOptions<JsonOptions> jsonOptions)
        {
            _Database = _database;
            _HttpClientFactory = _httpClientFactory;
            _Clock = _clock;
            Options = options;
            WebhookSettings = webhookSettings;
            JsonOptions = jsonOptions.Value.JsonSerializerOptions;
        }

        /// <summary>
        /// Polls for new logs and dispatches them to registered webhooks on a loop.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Log Polling Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAndDispatch(stoppingToken);
                }

                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                catch (Exception ex)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run LogPollingService.PollAndDispatch.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        ex.ToString());
                }

                try
                {
                    await Task.Delay(
                        Options.PollingIntervalMs,
                        stoppingToken);
                }

                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Log Polling Service stopped.");
        }

        /// <summary>
        /// Retrieves new logs from the database and sends them to all registered webhooks.
        /// </summary>
        private async Task PollAndDispatch(CancellationToken stoppingToken)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "LogPollingService.PollAndDispatch called.");

            WebhookRegistrationService registrationService = new(
                _Logger,
                _Database,
                _Clock,
                Options);

            (List<WebhookRegistrationModel>? registrations, Exception? regEx) = await registrationService.GetAll();

            if (regEx != null)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to retrieve webhook registrations.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    regEx.ToString());

                return;
            }

            if (registrations == null || registrations.Count == 0)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "No webhook registrations found.");

                return;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Found {registrations.Count} webhook registration(s).");

            int minAfterId = registrations.Min(r => r.AfterId);

            string sql = @"select
    Id,
    Timestamp,
    Level,
    Logger,
    Message
from [Logs]
where ServerName = @serverName
and Id > @afterId
order by Id asc";

            (List<LogModel> allLogs, Exception? logEx) = await _Database.Query(
                sql,
                reader =>
                {
                    return new LogModel()
                    {
                        Id = reader.GetInt32(0),
                        Timestamp = DateTime.SpecifyKind(
                            reader.GetDateTime(1),
                            DateTimeKind.Utc),
                        Level = Enum.Parse<Entities.LogLevel>(
                            reader.GetString(2),
                            true),
                        Logger = Enum.Parse<LogType>(
                            reader.GetString(3),
                            true),
                        Message = reader.GetString(4)
                    };
                },
                new SqliteParameter("@serverName", SqliteType.Text) { Value = Options.ServerName },
                new SqliteParameter("@afterId", SqliteType.Integer) { Value = minAfterId });

            if (logEx != null)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to retrieve logs for webhook dispatch.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    logEx.ToString());

                return;
            }

            if (allLogs.Count == 0)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "No new logs found for webhook dispatch.");

                return;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Found {allLogs.Count} new log(s) for webhook dispatch.");

            foreach (WebhookRegistrationModel registration in registrations)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                List<LogModel> filteredLogs = [.. allLogs.Where(l => l.Id > registration.AfterId)
                    .Where(l => registration.LogType == LogType.All || l.Logger == registration.LogType)
                    .Where(l => registration.LogLevel == Entities.LogLevel.All || l.Level == registration.LogLevel)];

                if (filteredLogs.Count == 0)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"No matching logs for webhook \"{registration.Id}\".");

                    continue;
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Dispatching {filteredLogs.Count} log(s) to webhook \"{registration.Id}\".");

                List<WebhookLogEntryModel> entries = [.. filteredLogs.Select(l => new WebhookLogEntryModel()
                    {
                        Id = l.Id,
                        Timestamp = l.Timestamp,
                        Level = l.Level.ToString(),
                        Type = l.Logger.ToString(),
                        Message = l.Message
                    })];

                WebhookPayloadModel payload = new()
                {
                    ServerName = Options.ServerName,
                    Logs = entries
                };

                HttpClient httpClient = _HttpClientFactory.CreateClient();

                WebhookDispatchService dispatchService = new(
                    _Logger,
                    httpClient,
                    WebhookSettings,
                    JsonOptions);

                (bool sent, Exception? sendEx) = await dispatchService.Send(
                    registration.Url,
                    payload);

                if (sent)
                {
                    int maxLogId = filteredLogs.Max(l => l.Id);

                    await registrationService.UpdateAfterId(
                        registration.Id,
                        maxLogId);

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Debug,
                        $"Webhook \"{registration.Id}\" cursor updated to {maxLogId}.");
                }

                else
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Failed to dispatch logs to webhook \"{registration.Id}\".");

                    if (sendEx != null)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Error,
                            sendEx.ToString());
                    }
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "LogPollingService.PollAndDispatch completed.");
        }
    }
}
