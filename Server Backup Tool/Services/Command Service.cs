// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Entities;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Models;

namespace ServerBackupTool.Services
{
    public class CommandService
    {
        private readonly ILoggerService _Logger;
        private readonly IDatabase _Database;
        private readonly DatabaseOptionsModel Options;

        // Sets the class's global variables
        public CommandService(
            ILoggerService _logger,
            IDatabase _database,
            DatabaseOptionsModel options)
        {
            _Logger = _logger;
            _Database = _database;
            Options = options;
        }

        /// <summary>
        /// Fetches a command from database.
        /// </summary>
        public async Task<(CommandModel?, Exception?)> GetCommand()
        {
            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"CommandService.GetCommand called.");

            CommandModel? command = null;
            Exception? ex = null;

            try
            {
                string sql = @"select
    Id,
    Target,
    Command
from Commands
where ServerName = @serverName
order by CreatedAt
limit 1";
                SqliteParameter[] parameters =
                [
                    new("@serverName", SqliteType.Text) { Value = Options.ServerName }
                ];

                (CommandModel? result, Exception? qex) = await _Database.QuerySingle(
                    sql,
                    reader =>
                    {
                        return new CommandModel()
                        {
                            Id = reader.GetInt32(0),
                            Target = Enum.Parse<TargetType>(reader.GetString(1)),
                            Command = reader.GetString(2)
                        };
                    },
                    parameters);

                if (qex != null)
                {
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run CommandService.GetCommand.");
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                if (result != null)
                {
                    command = result;
                }
            }

            catch (Exception cex)
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run CommandService.GetCommand.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            int commands = command != null ? 1 : 0;

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"CommandService.GetCommand returned {commands} command(s).");
            return (
                command,
                ex);
        }

        /// <summary>
        /// Deletes a command from database.
        /// </summary>
        public async Task<(bool, Exception?)> DeleteCommand(int id)
        {
            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"CommandService.DeleteCommand called with the parameter {id}.");

            bool success = false;
            Exception? ex = null;

            try
            {
                string sql = @"delete from Commands
where Id = @id";
                SqliteParameter[] parameters =
                [
                    new("@id", SqliteType.Integer) { Value = id }
                ];

                (int result, Exception? qex) = await _Database.Execute(
                    sql,
                    parameters);

                if (qex != null)
                {
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run CommandService.DeleteCommand.");
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
                    "An error occured when trying to run CommandService.DeleteCommand.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"CommandService.DeleteCommand returned {success}.");
            return (
                success,
                ex);
        }
    }
}
