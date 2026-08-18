// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Entities;
using ServerBackupTool.API.Helpers;
using ServerBackupTool.API.Models.Responses.Related;
using ServerBackupTool.API.Values;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.API.Services
{
    public class LogService
    {
        private readonly ILoggerService _Logger;
        private readonly IDatabase _Database;
        private readonly DatabaseOptionsModel Options;

        // Sets the class's global variables
        public LogService(
            ILoggerService _logger,
            IDatabase _database,
            DatabaseOptionsModel options)
        {
            _Logger = _logger;
            _Database = _database;
            Options = options;
        }

        /// <summary>
        /// Loads the logs from the database that match the parameters.
        /// </summary>
        public async Task<(List<LogModel>?, Exception?)> GetLogs(
            Entities.LogLevel level = Entities.LogLevel.All,
            LogType type = LogType.All,
            int limit = 100,
            int afterId = 0)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.GetLogs called with the parameters \"{nameof(level)}\", \"{nameof(type)}\", \"{limit}\", \"{afterId}\".");

            List<LogModel>? logs = null;
            Exception? ex = null;

            try
            {
                string sql = @"select top @limit
    Id,
    Timestamp,
    Level,
    Logger,
    Message
from [Logs]
where ServerName = @serverName";
                List<SqliteParameter> parameterList =
                [
                    new("@limit", SqliteType.Integer) { Value = limit },
                    new("@serverName", SqliteType.Text) { Value = Options.ServerName }
                ];

                if (level != Entities.LogLevel.All)
                {
                    sql += @"
and Level = @level";
                    parameterList.Add(new("@level", SqliteType.Text) { Value = nameof(level) });
                }

                if (type != LogType.All)
                {
                    sql += @"
and Logger = @type";
                    parameterList.Add(new("@type", SqliteType.Text) { Value = nameof(type) });
                }

                if (afterId > 0)
                {
                    sql += @"
and Id < @afterId";
                    parameterList.Add(new("@afterId", SqliteType.Integer) { Value = afterId });
                }

                sql += @"
order by Id desc";

                (List<LogModel> results, Exception? qex) = await _Database.Query(
                    sql,
                    reader =>
                    {
                        return new LogModel()
                        {
                            Id = reader.GetInt32(0),
                            Timestamp = DateTime.SpecifyKind(
                                reader.GetDateTime(1),
                                DateTimeKind.Utc),
                            Level = EnumHelper.ParseEnum<Entities.LogLevel>(reader.GetString(2)),
                            Logger = EnumHelper.ParseEnum<LogType>(reader.GetString(3)),
                            Message = reader.GetString(4)
                        };
                    },
                    [.. parameterList]);

                if (qex != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run LogService.GetLogs.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                if (results.Count > 0)
                {
                    logs = [.. results.OrderBy(r => r.Id)];
                }
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run LogService.GetLogs.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            return (
                logs,
                ex);
        }
    }
}
