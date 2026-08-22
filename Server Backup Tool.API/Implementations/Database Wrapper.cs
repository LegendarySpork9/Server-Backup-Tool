// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.API.Implementations
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
        /// Returns a list of the given model from the database.
        /// </summary>
        public async Task<(List<T>, Exception?)> Query<T>(
            string sql,
            Func<SqliteDataReader, T> map,
            params SqliteParameter[] parameters)
        {
            List<T> results = [];
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

                        using (SqliteDataReader dataReader = await command.ExecuteReaderAsync())
                        {
                            while (await dataReader.ReadAsync())
                            {
                                results.Add(map(dataReader));
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                exception = ex;
            }

            return (
                results,
                exception);
        }

        /// <summary>
        /// Returns the result of the execution for given query.
        /// </summary>
        public async Task<(object?, Exception?)> ExecuteScalar(
            string sql,
            params SqliteParameter[] parameters)
        {
            object? result = null;
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

                        result = await command.ExecuteScalarAsync();
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
