// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.Implementations
{
    public class ExtendedDatabaseWrapper : Common.Implementations.DatabaseWrapper, IExtendedDatabase
    {
        private readonly DatabaseOptionsModel _Options;

        // Sets the class's global variables.
        public ExtendedDatabaseWrapper(
            DatabaseOptionsModel _options) : base(_options)
        {
            _Options = _options;
        }

        /// <summary>
        /// Returns the given model from the database.
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
    }
}
