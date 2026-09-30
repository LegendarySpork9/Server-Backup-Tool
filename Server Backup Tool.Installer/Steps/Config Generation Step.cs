// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Steps
{
    public class ConfigGenerationStep
    {
        private readonly ILoggerService _Logger;
        private readonly IConfigWriter _ConfigWriter;
        private readonly InstallOptionsModel _Options;

        // Sets the class's global variables.
        public ConfigGenerationStep(
            ILoggerService logger,
            IConfigWriter configWriter,
            InstallOptionsModel options)
        {
            _Logger = logger;
            _ConfigWriter = configWriter;
            _Options = options;
        }

        /// <summary>
        /// Generates and writes configuration files for the installation.
        /// </summary>
        public async Task Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Starting configuration generation.");

            await GenerateAppConfigAsync();

            if (_Options.ApiConfig != null)
            {
                await GenerateApiAppSettingsAsync();
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Configuration generation completed.");
        }

        /// <summary>
        /// Generates and writes the App.config file.
        /// </summary>
        private async Task GenerateAppConfigAsync()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Generating App.config.");

            XDocument config = _ConfigWriter.GenerateAppConfig(_Options);
            string configPath = Path.Combine(
                _Options.InstallPath,
                InstallerValues.Defaults.ToolConfigFileName);

            (bool success, Exception? exception) = await _ConfigWriter.WriteConfig(
                configPath,
                config);

            if (!success)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to write App.config: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to write App.config.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "App.config written.");
        }

        /// <summary>
        /// Generates and writes the appsettings.json file.
        /// </summary>
        private async Task GenerateApiAppSettingsAsync()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Generating appsettings.json.");

            string json = _ConfigWriter.GenerateApiAppSettings(
                _Options.ApiConfig!,
                _Options.ServerConfig);
            string apiSettingsPath = Path.Combine(
                _Options.InstallPath,
                "appsettings.json");

            (bool success, Exception? exception) = await _ConfigWriter.WriteApiSettings(
                apiSettingsPath,
                json);

            if (!success)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Failed to write appsettings.json: {exception?.Message}");

                throw new InvalidOperationException(
                    "Failed to write appsettings.json.",
                    exception);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "appsettings.json written.");
        }
    }
}
