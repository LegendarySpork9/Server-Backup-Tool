// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.Common.Implementations
{
    public class DatabaseWrapper : IDatabase
    {
        private readonly DatabaseOptionsModel _Options;

        /// <summary>
        /// </summary>
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
        /// Returns the given field from the database.
        /// </summary>
        public async Task<(T?, Exception?)> QuerySingle<T>(
            string sql,
            Func<SqliteDataReader, T> map,
            params SqliteParameter[] parameters)
        {
            T? result = default;
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
                            if (await dataReader.ReadAsync())
                            {
                                result = map(dataReader);
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
                result,
                exception);
        }

        /// <summary>
        /// Returns the result of the execution for given query.
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
