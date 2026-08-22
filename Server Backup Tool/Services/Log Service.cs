// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Common.Values;

namespace ServerBackupTool.Services
{
    public class LogService
    {
        private readonly ILoggerService _Logger;
        private readonly IDatabase _Database;
        private readonly IClock _Clock;
        private readonly DatabaseOptionsModel Options;

        // Sets the class's global variables
        public LogService(
            ILoggerService _logger,
            IDatabase _database,
            IClock _clock,
            DatabaseOptionsModel options)
        {
            _Logger = _logger;
            _Database = _database;
            _Clock = _clock;
            Options = options;
        }

        /// <summary>
        /// Logs the message to the database.
        /// </summary>
        public async Task LogMessage(
            string level,
            string type,
            string message)
        {
            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.LogMessage called with the parameters \"{level}\", \"{type}\", \"{message}\".");

            try
            {
                string sql = @"insert into Logs (
    ServerName,
    Timestamp,
    Level,
    Logger,
    Message
)
values (
    @serverName,
    @timestamp,
    @level,
    @logger,
    @message
)";
                SqliteParameter[] parameters =
                [
                    new("@serverName", SqliteType.Text) { Value = Options.ServerName },
                    new("@timestamp", SqliteType.Text) { Value = _Clock.UtcNow },
                    new("@level", SqliteType.Text) { Value = level },
                    new("@logger", SqliteType.Text) { Value = type },
                    new("@message", SqliteType.Text) { Value = message },
                ];

                (int result, Exception? ex) = await _Database.Execute(
                    sql,
                    parameters);

                if (ex != null)
                {
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run LogService.LogMessage.");
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Error,
                        ex.ToString());
                }
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run LogService.LogMessage.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());
            }

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.LogMessage completed.");
        }

        /// <summary>
        /// Logs the message to the database.
        /// </summary>
        public async Task<(bool, Exception?)> ClearLogs(string type)
        {
            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.ClearLogs called with the parameter \"{type}\".");

            bool success = false;
            Exception? ex = null;

            try
            {
                string sql = @"delete from Logs
where ServerName = @serverName
and Logger = @logger";
                SqliteParameter[] parameters =
                [
                    new("@serverName", SqliteType.Text) { Value = Options.ServerName },
                    new("@logger", SqliteType.Text) { Value = type }
                ];

                (int result, Exception? qex) = await _Database.Execute(
                    sql,
                    parameters);

                if (qex != null)
                {
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run LogService.ClearLogs.");
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                if (result > 0)
                {
                    success = true;
                }
            }

            catch (Exception cex)
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run LogService.ClearLogs.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.ClearLogs returned {success}.");
            return (
                success,
                ex);
        }
    }
}
