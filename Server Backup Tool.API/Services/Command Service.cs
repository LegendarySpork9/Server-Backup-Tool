// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.Common.Functions;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Common.Models.Requests;

namespace ServerBackupTool.API.Services
{
    public class CommandService
    {
        private readonly ILoggerService _Logger;
        private readonly IDatabase _Database;
        private readonly IClock _Clock;
        private readonly DatabaseOptionsModel Options;

        // Sets the class's global variables
        public CommandService(
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
        /// Logs the given command to the database.
        /// </summary>
        public async Task<(int?, DateTime, Exception?)> LogCommand(CommandRequestModel command)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"CommandService.LogCommand called with the parameters {ParameterFunction.FormatParameters(command)}.");

            int? commandId = null;
            DateTime createdAt = _Clock.UtcNow;
            Exception? ex = null;

            try
            {
                string sql = @"insert into Commands (
    ServerName,
    Target,
    Command,
    CreatedAt
)
values (
    @serverName,
    @target,
    @command,
    @createdAt
)
RETURNING Id";
                List<SqliteParameter> parameterList =
                [
                    new("@serverName", SqliteType.Text) { Value = Options.ServerName },
                    new("@target", SqliteType.Text) { Value = command.Target },
                    new("@command", SqliteType.Text) { Value = command.Command },
                    new("@createdAt", SqliteType.Text) { Value = createdAt }
                ];

                (object? result, Exception? qex) = await _Database.ExecuteScalar(
                    sql,
                    [.. parameterList]);

                if (qex != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run CommandService.LogCommand.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                if (result != null)
                {
                    commandId = int.Parse(result.ToString() ?? "0");
                }
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run CommandService.LogCommand.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"CommandService.LogCommand returned {commandId} | {createdAt}.");
            return (
                commandId,
                createdAt,
                ex);
        }
    }
}
