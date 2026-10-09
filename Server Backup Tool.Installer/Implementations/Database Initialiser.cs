// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Values;

namespace ServerBackupTool.Installer.Implementations
{
    public class DatabaseInitialiser : IDatabaseInitialiser
    {
        private readonly ILoggerService _Logger;
        private readonly IExtendedFileSystem _FileSystem;

        // Sets the class's global variables.
        public DatabaseInitialiser(
            ILoggerService logger,
            IExtendedFileSystem fileSystem)
        {
            _Logger = logger;
            _FileSystem = fileSystem;
        }

        /// <summary>
        /// Creates the SQLite database and initialises tables and indexes.
        /// </summary>
        public async Task<(bool, Exception?)> InitialiseDatabase(string databasePath)
        {
            bool initialised = false;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Initialising Database at {databasePath}.");

                string? directory = Path.GetDirectoryName(databasePath);

                if (!string.IsNullOrEmpty(directory) && !_FileSystem.DirectoryExists(directory))
                {
                    _FileSystem.CreateDirectory(directory);
                }

                string connectionString = $"Data Source={databasePath}";

                using (SqliteConnection connection = new(connectionString))
                {
                    await connection.OpenAsync();

                    string[] statements = InstallerValues.Database.CreateTablesSql.Split(
                        ';',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (string statement in statements)
                    {
                        if (!string.IsNullOrWhiteSpace(statement))
                        {
                            using (SqliteCommand command = new(
                                statement,
                                connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Database initialised.");

                initialised = true;
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Initialise Database: {ex.Message}");

                exception = ex;
            }

            return (
                initialised,
                exception);
        }

        /// <summary>
        /// Validates that the database has the expected tables.
        /// </summary>
        public async Task<(bool, Exception?)> ValidateDatabase(string databasePath)
        {
            bool valid = true;
            Exception? exception = null;

            try
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Validating database at {databasePath}.");

                string connectionString = $"Data Source={databasePath}";

                await using (SqliteConnection connection = new(connectionString))
                {
                    await connection.OpenAsync();

                    string[] requiredTables =
                    [
                        "Logs",
                        "Commands",
                        "Webhooks"
                    ];
                    string validationSQL = @"SELECT
    COUNT(*)
FROM sqlite_master
WHERE type='table'
AND name=@name";

                    foreach (string table in requiredTables)
                    {
                        using (SqliteCommand command = new(
                            validationSQL,
                            connection))
                        {
                            command.Parameters.AddWithValue("@name", table);

                            long count = (long)(await command.ExecuteScalarAsync() ?? 0);

                            if (count == 0)
                            {
                                _Logger.LogMessage(
                                    StandardValues.LoggerValues.Error,
                                    $"Required table '{table}' not found in database.");

                                valid = false;

                                break;
                            }
                        }
                    }
                }

                if (valid)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "Database validation passed.");
                }
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to Validate Database: {ex.Message}");

                exception = ex;
                valid = false;
            }

            return (
                valid,
                exception);
        }
    }
}
