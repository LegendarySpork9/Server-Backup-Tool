// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Entities;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Models.Requests;
using ServerBackupTool.Common.Functions;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.API.Services
{
    public class WebhookRegistrationService
    {
        private readonly ILoggerService _Logger;
        private readonly IExtendedDatabase _Database;
        private readonly IClock _Clock;
        private readonly DatabaseOptionsModel Options;

        // Sets the class's global variables
        public WebhookRegistrationService(
            ILoggerService _logger,
            IExtendedDatabase _database,
            IClock _clock,
            DatabaseOptionsModel options)
        {
            _Logger = _logger;
            _Database = _database;
            _Clock = _clock;
            Options = options;
        }

        /// <summary>
        /// Registers a new webhook in the database.
        /// </summary>
        public async Task<(string?, Exception?)> Register(WebhookRegistrationRequestModel registration)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookRegistrationService.Register called with the parameters {ParameterFunction.FormatParameters(registration)}.");

            string? webhookId = null;
            Exception? ex = null;

            try
            {
                string id = Guid.NewGuid().ToString();
                int afterId = registration.AfterId;

                if (afterId == 0)
                {
                    string maxIdSql = @"select
    COALESCE(MAX(Id), 0)
from [Logs]
where ServerName = @serverName";

                    (object? maxResult, Exception? maxEx) = await _Database.ExecuteScalar(
                        maxIdSql,
                        new SqliteParameter("@serverName", SqliteType.Text) { Value = Options.ServerName });

                    if (maxEx != null)
                    {
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            "An error occured when trying to run WebhookRegistrationService.Register.");
                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Error,
                            maxEx.ToString());

                        ex = maxEx;

                        return (
                            null,
                            ex);
                    }

                    if (maxResult != null)
                    {
                        afterId = int.Parse(maxResult.ToString() ?? "0");
                    }
                }

                DateTime createdAt = _Clock.UtcNow;

                string sql = @"insert into Webhooks (
    Id,
    Url,
    LogType,
    LogLevel,
    AfterId,
    CreatedAt
)
values (
    @id,
    @url,
    @logType,
    @logLevel,
    @afterId,
    @createdAt
)";
                List<SqliteParameter> parameterList =
                [
                    new("@id", SqliteType.Text) { Value = id },
                    new("@url", SqliteType.Text) { Value = registration.Url },
                    new("@logType", SqliteType.Text) { Value = registration.LogType },
                    new("@logLevel", SqliteType.Text) { Value = registration.LogLevel },
                    new("@afterId", SqliteType.Integer) { Value = afterId },
                    new("@createdAt", SqliteType.Text) { Value = createdAt }
                ];

                (int rowsAffected, Exception? qex) = await _Database.Execute(
                    sql,
                    [.. parameterList]);

                if (qex != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run WebhookRegistrationService.Register.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                if (rowsAffected > 0)
                {
                    webhookId = id;
                }
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run WebhookRegistrationService.Register.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookRegistrationService.Register returned {webhookId ?? "null"}.");
            return (
                webhookId,
                ex);
        }

        /// <summary>
        /// Removes a webhook registration from the database.
        /// </summary>
        public async Task<(bool, Exception?)> Unregister(string webhookId)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookRegistrationService.Unregister called with the parameter \"{webhookId}\".");

            bool removed = false;
            Exception? ex = null;

            try
            {
                string sql = @"delete from Webhooks
where Id = @id";

                (int rowsAffected, Exception? qex) = await _Database.Execute(
                    sql,
                    new SqliteParameter("@id", SqliteType.Text) { Value = webhookId });

                if (qex != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run WebhookRegistrationService.Unregister.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                removed = rowsAffected > 0;
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run WebhookRegistrationService.Unregister.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookRegistrationService.Unregister returned {removed}.");
            return (
                removed,
                ex);
        }

        /// <summary>
        /// Returns all active webhook registrations.
        /// </summary>
        public async Task<(List<WebhookRegistrationModel>?, Exception?)> GetAll()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "WebhookRegistrationService.GetAll called.");

            List<WebhookRegistrationModel>? registrations = null;
            Exception? ex = null;

            try
            {
                string sql = @"select
    Id,
    Url,
    LogType,
    LogLevel,
    AfterId
from [Webhooks]";

                (List<WebhookRegistrationModel> results, Exception? qex) = await _Database.Query(
                    sql,
                    reader =>
                    {
                        return new WebhookRegistrationModel()
                        {
                            Id = reader.GetString(0),
                            Url = reader.GetString(1),
                            LogType = Enum.Parse<LogType>(reader.GetString(2), true),
                            LogLevel = Enum.Parse<Entities.LogLevel>(reader.GetString(3), true),
                            AfterId = reader.GetInt32(4)
                        };
                    });

                if (qex != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run WebhookRegistrationService.GetAll.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                if (results.Count > 0)
                {
                    registrations = results;
                }
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run WebhookRegistrationService.GetAll.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookRegistrationService.GetAll returned {registrations?.Count ?? 0} registration(s).");
            return (
                registrations,
                ex);
        }

        /// <summary>
        /// Updates the AfterId for a given webhook.
        /// </summary>
        public async Task<(bool, Exception?)> UpdateAfterId(
            string webhookId,
            int afterId)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookRegistrationService.UpdateAfterId called with the parameters \"{webhookId}\", \"{afterId}\".");

            bool updated = false;
            Exception? ex = null;

            try
            {
                string sql = @"update Webhooks set AfterId = @afterId
where Id = @id";

                List<SqliteParameter> parameterList =
                [
                    new("@afterId", SqliteType.Integer) { Value = afterId },
                    new("@id", SqliteType.Text) { Value = webhookId }
                ];

                (int rowsAffected, Exception? qex) = await _Database.Execute(
                    sql,
                    [.. parameterList]);

                if (qex != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run WebhookRegistrationService.UpdateAfterId.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                updated = rowsAffected > 0;
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run WebhookRegistrationService.UpdateAfterId.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookRegistrationService.UpdateAfterId returned {updated}.");
            return (
                updated,
                ex);
        }
    }
}
