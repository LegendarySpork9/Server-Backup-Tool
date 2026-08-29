// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.Common.Implementations
{
    public class DatabaseWrapper : IDatabase
    {
        private readonly DatabaseOptionsModel _Options;

        // Sets the class's global variables.
        public DatabaseWrapper(
            DatabaseOptionsModel _options)
        {
            _Options = _options;
        }

        /// <summary>
        /// Returns the number of rows affected for the given query.
        /// </summary>
        public async Task<(int, Exception?)> Execute(
            string sql,
            params SqliteParameter[] parameters)
        {
            int result = -1;
            Exception? exception = null;

            try
            {
                using (SqliteConnection connection = new($"Data Source={_Options.Path}"))
                {
                    await connection.OpenAsync();

                    using (SqliteCommand command = new(
                        sql,
                        connection))
                    {
                        command.Parameters.AddRange(parameters);

                        result = await command.ExecuteNonQueryAsync();
                    }
                }
            }

            catch (Exception ex)
            {
                exception = ex;
            }

            return (
                result,
                exception);
        }
    }
}
