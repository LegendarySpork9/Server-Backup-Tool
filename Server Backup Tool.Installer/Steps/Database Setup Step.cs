// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;

namespace ServerBackupTool.Installer.Steps
{
    public class DatabaseSetupStep
    {
        private readonly ILoggerService _Logger;
        private readonly IDatabaseInitialiser _DatabaseInitialiser;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public DatabaseSetupStep(
            ILoggerService logger,
            IDatabaseInitialiser databaseInitialiser,
            InstallOptionsModel options)
        {
            _Logger = logger;
            _DatabaseInitialiser = databaseInitialiser;
            _Options = options;
        }

        /// <summary>
        /// Initialises the SQLite database at the configured path.
        /// </summary>
        public async Task Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Initialising database at {_Options.ServerConfig.DatabasePath}.");

            (bool success, Exception? exception) = await _DatabaseInitialiser.InitialiseDatabase(_Options.ServerConfig.DatabasePath);

            if (!success)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to initialise database: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to initialise database.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Database initialised.");
        }
    }
}
