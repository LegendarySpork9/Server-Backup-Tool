// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Steps;
using Spectre.Console;

namespace ServerBackupTool.Installer.Modes
{
    public class InstallMode
    {
        private readonly IAnsiConsole _Console;
        private readonly ILoggerService _Logger;
        private readonly IFileService _FileService;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly IResourceService _ResourceService;
        private readonly IConfigWriter _ConfigWriter;
        private readonly IDatabaseInitialiser _DatabaseInitialiser;
        private readonly ITaskSchedulerService _TaskSchedulerService;
        private readonly IRegistryService _RegistryService;
        private readonly IVersionService _VersionService;

        // Sets the class's global variables.
        public InstallMode(
            IAnsiConsole console,
            ILoggerService logger,
            IFileService fileService,
            IExtendedFileSystem fileSystem,
            IResourceService resourceService,
            IConfigWriter configWriter,
            IDatabaseInitialiser databaseInitialiser,
            ITaskSchedulerService taskSchedulerService,
            IRegistryService registryService,
            IVersionService versionService)
        {
            _Console = console;
            _Logger = logger;
            _FileService = fileService;
            _FileSystem = fileSystem;
            _ResourceService = resourceService;
            _ConfigWriter = configWriter;
            _DatabaseInitialiser = databaseInitialiser;
            _TaskSchedulerService = taskSchedulerService;
            _RegistryService = registryService;
            _VersionService = versionService;
        }

        /// <summary>
        /// Runs the install wizard.
        /// </summary>
        public async Task Execute()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Starting install mode.");

            InstallOptionsModel options = new();

            try
            {
                string embeddedToolVersion = _VersionService.GetEmbeddedToolVersion();
                string embeddedApiVersion = _VersionService.GetEmbeddedApiVersion();

                _Console.MarkupLine($"Bundled tool version: [blue]{Markup.Escape(embeddedToolVersion)}[/]");

                if (embeddedApiVersion != "0.0.0")
                {
                    _Console.MarkupLine($"Bundled API version:  [blue]{Markup.Escape(embeddedApiVersion)}[/]");
                }

                _Console.WriteLine();

                List<VersionInfoModel> existingInstallations = _VersionService.GetAllInstallations();

                if (existingInstallations.Count > 0)
                {
                    foreach (VersionInfoModel existing in existingInstallations)
                    {
                        _Console.MarkupLine($"[yellow]Existing installation found: {Markup.Escape(existing.ServerName)} at {Markup.Escape(existing.InstallPath)} (v{Markup.Escape(existing.ToolVersion)})[/]");
                    }

                    if (!_Console.Prompt(new ConfirmationPrompt("Existing installation(s) found. Proceeding will create a new installation. Continue?") { DefaultValue = false, ShowDefaultValue = false }))
                    {
                        throw new OperationCanceledException("Installation cancelled — existing installation detected.");
                    }

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Info,
                        "User confirmed installation alongside existing version(s).");
                }

                new ComponentSelectionStep(
                    _Console,
                    _Logger,
                    options).Execute();

                await new LocationStep(
                    _Console,
                    _Logger,
                    _FileService,
                    options).Execute();

                new ServerConfigStep(
                    _Console,
                    _Logger,
                    _FileSystem,
                    options).Execute();

                new TimerConfigStep(
                    _Console,
                    _Logger,
                    options).Execute();

                new EmailConfigStep(
                    _Console,
                    _Logger,
                    options).Execute();

                new ApiConfigStep(
                    _Console,
                    _Logger,
                    options).Execute();

                new ConfirmationStep(
                    _Console,
                    _Logger,
                    options).Execute();

                await new FileDeployStep(
                    _Console,
                    _Logger,
                    _FileSystem,
                    _ResourceService,
                    _ConfigWriter,
                    _DatabaseInitialiser,
                    _TaskSchedulerService,
                    _RegistryService,
                    _VersionService,
                    options).Execute();

                await new ValidationStep(
                    _Console,
                    _Logger,
                    _FileSystem,
                    _DatabaseInitialiser,
                    _TaskSchedulerService,
                    options).Execute();

                _Console.MarkupLine("[green]Installation completed successfully.[/]");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Install mode completed.");
            }

            catch (OperationCanceledException)
            {
                _Console.MarkupLine("[yellow]Installation cancelled by user.[/]");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Installation cancelled by user.");
            }

            catch (Exception ex)
            {
                _Console.MarkupLine($"[red]Installation failed: {Markup.Escape(ex.Message)}[/]");

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Installation failed: {ex.Message}");

                if (!string.IsNullOrEmpty(options.InstallPath) && _FileSystem.DirectoryExists(options.InstallPath))
                {
                    _Console.MarkupLine("[yellow]Cleaning up partial installation...[/]");

                    _FileService.DeleteDirectory(options.InstallPath);
                }
            }
        }
    }
}
