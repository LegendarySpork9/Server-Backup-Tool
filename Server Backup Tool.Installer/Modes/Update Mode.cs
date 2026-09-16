// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Models.Related;
using ServerBackupTool.Installer.Values;
using Spectre.Console;
using System.Text.Json;
using System.Xml.Linq;

namespace ServerBackupTool.Installer.Modes
{
    public class UpdateMode
    {
        private readonly ILoggerService _Logger;
        private readonly IFileService _FileService;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly IConfigWriter _ConfigWriter;
        private readonly IDatabaseInitialiser _DatabaseInitialiser;
        private readonly IRegistryService _RegistryService;
        private readonly IVersionService _VersionService;
        private readonly IResourceService _ResourceService;

        // Sets the class's global variables.
        public UpdateMode(
            ILoggerService logger,
            IFileService fileService,
            IExtendedFileSystem fileSystem,
            IConfigWriter configWriter,
            IDatabaseInitialiser databaseInitialiser,
            IRegistryService registryService,
            IVersionService versionService,
            IResourceService resourceService)
        {
            _Logger = logger;
            _FileService = fileService;
            _FileSystem = fileSystem;
            _ConfigWriter = configWriter;
            _DatabaseInitialiser = databaseInitialiser;
            _RegistryService = registryService;
            _VersionService = versionService;
            _ResourceService = resourceService;
        }

        /// <summary>
        /// Runs the update process.
        /// </summary>
        public async Task Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Starting update mode.");

            VersionInfoModel? installed = SelectInstallation();

            if (installed == null)
            {
                AnsiConsole.MarkupLine("[red]No existing installation found. Please run the installer first.[/]");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    "No existing installation found.");
            }

            else
            {
                string embeddedToolVersion = _VersionService.GetEmbeddedToolVersion();
                string embeddedApiVersion = _VersionService.GetEmbeddedApiVersion();

                Table versionTable = new();
                versionTable.Border(TableBorder.Rounded);
                versionTable.Title("[bold]Version Summary[/]");
                versionTable.AddColumn("[bold]Component[/]");
                versionTable.AddColumn("[bold]Installed[/]");
                versionTable.AddColumn("[bold]Bundled[/]");

                versionTable.AddRow(
                    "Server Backup Tool",
                    Markup.Escape(installed.ToolVersion),
                    Markup.Escape(embeddedToolVersion));

                if (!string.IsNullOrEmpty(installed.ApiVersion) || embeddedApiVersion != "0.0.0")
                {
                    versionTable.AddRow(
                        "Server Backup Tool API",
                        Markup.Escape(!string.IsNullOrEmpty(installed.ApiVersion) ? installed.ApiVersion : "Not installed"),
                        Markup.Escape(embeddedApiVersion != "0.0.0" ? embeddedApiVersion : "Not bundled"));
                }

                AnsiConsole.Write(versionTable);
                AnsiConsole.WriteLine();

                if (!AnsiConsole.Prompt(new ConfirmationPrompt("Proceed with update?")
                {
                    ShowDefaultValue = false
                }))
                {
                    AnsiConsole.MarkupLine("[yellow]Update cancelled.[/]");
                }

                else
                {
                    string backupPath = Path.Combine(
                        installed.InstallPath,
                        $"Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}");

                    try
                    {
                        AnsiConsole.MarkupLine("Backing up existing installation...");

                        (bool backupSuccess, Exception? backupEx) = _FileService.BackupDirectory(
                            installed.InstallPath,
                            backupPath);

                        if (!backupSuccess)
                        {
                            throw backupEx ?? new InvalidOperationException("Backup failed.");
                        }

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            $"Backup created at {backupPath}.");

                        ExtractUpdatedBinaries(installed);

                        (bool dbSuccess, Exception? dbEx) = await _DatabaseInitialiser.ValidateDatabase(Path.Combine(
                            installed.InstallPath,
                            InstallerValues.Defaults.DatabaseFileName));

                        if (!dbSuccess)
                        {
                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Warning,
                                "Database validation failed, running migration.");

                            string dbPath = Path.Combine(
                                installed.InstallPath,
                                InstallerValues.Defaults.DatabaseFileName);

                            (bool initSuccess, Exception? initEx) = await _DatabaseInitialiser.InitialiseDatabase(dbPath);

                            if (!initSuccess)
                            {
                                throw initEx ?? new InvalidOperationException("Database migration failed.");
                            }
                        }

                        await MigrateConfigsAsync(installed.InstallPath);

                        string newToolVersion = _VersionService.GetBundledToolVersion(installed.InstallPath);
                        string newApiVersion = !string.IsNullOrEmpty(installed.ApiInstallPath) ? _VersionService.GetBundledApiVersion(installed.ApiInstallPath) : string.Empty;

                        (bool regSuccess, Exception? regEx) = _RegistryService.WriteUninstallEntry(
                            installed.ServerName,
                            installed.InstallPath,
                            installed.ApiInstallPath,
                            newToolVersion,
                            newApiVersion,
                            installed.ToolTaskName,
                            installed.ApiTaskName);

                        if (!regSuccess)
                        {
                            throw regEx ?? new InvalidOperationException("Registry update failed.");
                        }

                        AnsiConsole.MarkupLine($"[green]Update completed successfully.[/]");
                        AnsiConsole.MarkupLine($"Tool version: [blue]{Markup.Escape(installed.ToolVersion)}[/] -> [green]{Markup.Escape(newToolVersion)}[/]");

                        if (!string.IsNullOrEmpty(installed.ApiVersion) || !string.IsNullOrEmpty(newApiVersion))
                        {
                            AnsiConsole.MarkupLine($"API version:  [blue]{Markup.Escape(installed.ApiVersion)}[/] -> [green]{Markup.Escape(newApiVersion)}[/]");
                        }

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Info,
                            "Update mode completed.");
                    }

                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Update failed: {Markup.Escape(ex.Message)}[/]");
                        AnsiConsole.MarkupLine("[yellow]Rolling back...[/]");

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Error,
                            $"Update failed: {ex.Message}. Rolling back.");

                        _FileService.CopyFiles(
                            backupPath,
                            installed.InstallPath);
                        _FileService.DeleteDirectory(backupPath);

                        AnsiConsole.MarkupLine("[yellow]Rollback complete.[/]");
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the updated tool and API binaries from embedded resources.
        /// </summary>
        private void ExtractUpdatedBinaries(VersionInfoModel installed)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Extracting updated binaries.");

            string? toolResourceName = _ResourceService.FindResource(InstallerValues.Resources.ToolPrefix);

            if (toolResourceName != null)
            {
                (bool toolExtracted, Exception? toolEx) = _ResourceService.ExtractResource(
                    toolResourceName,
                    installed.InstallPath);

                if (!toolExtracted)
                {
                    throw new InvalidOperationException(
                        "Failed to extract updated tool binaries.",
                        toolEx);
                }
            }

            string? apiResourceName = _ResourceService.FindResource(InstallerValues.Resources.ApiPrefix);

            if (!string.IsNullOrEmpty(installed.ApiInstallPath) && apiResourceName != null)
            {
                (bool apiExtracted, Exception? apiEx) = _ResourceService.ExtractResource(
                    apiResourceName,
                    installed.ApiInstallPath);

                if (!apiExtracted)
                {
                    throw new InvalidOperationException(
                        "Failed to extract updated API binaries.",
                        apiEx);
                }
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Updated binaries extracted.");
        }

        /// <summary>
        /// Migrates App.config and appsettings.json by adding missing properties from the current version's schema.
        /// </summary>
        private async Task MigrateConfigsAsync(string installPath)
        {
            string appConfigPath = Path.Combine(
                installPath,
                InstallerValues.Defaults.ToolConfigFileName);

            if (_FileSystem.FileExists(appConfigPath))
            {
                AnsiConsole.MarkupLine("Checking App.config for missing properties...");

                XDocument existingConfig = XDocument.Load(appConfigPath);

                InstallOptionsModel defaultOptions = new()
                {
                    ServerConfig = new ServerConfigModel(),
                    TimerConfig = new TimerConfigModel()
                };

                XDocument referenceConfig = _ConfigWriter.GenerateAppConfig(defaultOptions);

                (bool migrated, List<string> additions) = _ConfigWriter.MigrateAppConfig(
                    existingConfig,
                    referenceConfig);

                if (migrated)
                {
                    _FileSystem.CopyFile(
                        appConfigPath,
                        appConfigPath + ".bak",
                        true);

                    await _ConfigWriter.WriteConfig(
                        appConfigPath,
                        existingConfig);

                    AnsiConsole.MarkupLine($"[green]App.config updated with {additions.Count} new propert(ies):[/]");

                    foreach (string addition in additions)
                    {
                        AnsiConsole.MarkupLine($"  [blue]+[/] {Markup.Escape(addition)}");
                    }
                }

                else
                {
                    AnsiConsole.MarkupLine("[green]App.config is up to date.[/]");
                }
            }

            string apiSettingsPath = Path.Combine(
                installPath,
                "appsettings.json");

            if (_FileSystem.FileExists(apiSettingsPath))
            {
                AnsiConsole.MarkupLine("Checking appsettings.json for missing properties...");

                string existingJson = await _FileSystem.ReadAllText(apiSettingsPath);

                ApiConfigModel defaultApiConfig = new();
                ServerConfigModel defaultServerConfig = new();

                string referenceJson = _ConfigWriter.GenerateApiAppSettings(
                    defaultApiConfig,
                    defaultServerConfig);

                (bool apiMigrated, List<string> apiAdditions) = _ConfigWriter.MigrateApiAppSettings(
                    existingJson,
                    referenceJson);

                if (apiMigrated)
                {
                    _FileSystem.CopyFile(
                        apiSettingsPath,
                        apiSettingsPath + ".bak",
                        true);

                    Dictionary<string, object?> merged = JsonSerializer.Deserialize<Dictionary<string, object?>>(existingJson) ?? [];

                    using JsonDocument referenceDoc = JsonDocument.Parse(referenceJson);
                    using JsonDocument existingDoc = JsonDocument.Parse(existingJson);

                    foreach (JsonProperty property in referenceDoc.RootElement.EnumerateObject())
                    {
                        if (!existingDoc.RootElement.TryGetProperty(
                            property.Name,
                            out _))
                        {
                            merged[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
                        }

                        else if (property.Value.ValueKind == JsonValueKind.Object)
                        {
                            JsonElement existingSection = existingDoc.RootElement.GetProperty(property.Name);
                            Dictionary<string, object?> sectionDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(existingSection.GetRawText()) ?? [];

                            foreach (JsonProperty subProperty in property.Value.EnumerateObject())
                            {
                                if (!existingSection.TryGetProperty(
                                    subProperty.Name,
                                    out _))
                                {
                                    sectionDict[subProperty.Name] = JsonSerializer.Deserialize<object>(subProperty.Value.GetRawText());
                                }
                            }

                            merged[property.Name] = sectionDict;
                        }
                    }

                    string updatedJson = JsonSerializer.Serialize(
                        merged,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                    await _ConfigWriter.WriteApiSettings(
                        apiSettingsPath,
                        updatedJson);

                    AnsiConsole.MarkupLine($"[green]appsettings.json updated with {apiAdditions.Count} new propert(ies):[/]");

                    foreach (string addition in apiAdditions)
                    {
                        AnsiConsole.MarkupLine($"  [blue]+[/] {Markup.Escape(addition)}");
                    }
                }

                else
                {
                    AnsiConsole.MarkupLine("[green]appsettings.json is up to date.[/]");
                }
            }
        }

        /// <summary>
        /// Enumerates all installations and prompts the user to select one if multiple exist.
        /// </summary>
        private VersionInfoModel? SelectInstallation()
        {
            List<VersionInfoModel> installations = _VersionService.GetAllInstallations();
            VersionInfoModel? selected = null;

            if (installations.Count == 1)
            {
                selected = installations[0];

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Auto-selected single installation: {selected.ServerName}.");
            }

            else if (installations.Count > 1)
            {
                string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Multiple installations found. Which installation would you like to update?")
                    .AddChoices(installations.Select(i => $"{i.ServerName} (v{i.ToolVersion} at {i.InstallPath})")));

                int selectedIndex = installations.FindIndex(i => choice.StartsWith(
                    $"{i.ServerName} (v{i.ToolVersion}",
                    StringComparison.Ordinal));

                if (selectedIndex >= 0)
                {
                    selected = installations[selectedIndex];
                }

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"User selected installation: {selected?.ServerName}.");
            }

            return selected;
        }
    }
}
