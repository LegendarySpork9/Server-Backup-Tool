// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Modes;
using Spectre.Console;

namespace ServerBackupTool.Installer
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            ILoggerService logger = new LoggerServiceWrapper();

            logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Server Backup Tool Installer started.");

            IAnsiConsole console = AnsiConsole.Console;

            try
            {
                IExtendedFileSystem fileSystem = new ExtendedFileSystemWrapper();
                IFileService fileService = new FileService(
                    logger,
                    fileSystem);
                IResourceService resourceService = new ResourceService(
                    logger,
                    fileSystem);
                IConfigWriter configWriter = new ConfigWriter(
                    logger,
                    fileSystem);
                IDatabaseInitialiser databaseInitialiser = new DatabaseInitialiser(
                    logger,
                    fileSystem);
                IRegistryService registryService = new RegistryService(logger);
                ITaskSchedulerService taskSchedulerService = new TaskSchedulerService(logger);
                IVersionService versionService = new VersionService(
                    logger,
                    registryService,
                    resourceService,
                    fileSystem);

                string mode = DetermineMode(
                    console,
                    args);

                switch (mode)
                {
                    case "install":
                        await new InstallMode(
                            console,
                            logger,
                            fileService,
                            fileSystem,
                            resourceService,
                            configWriter,
                            databaseInitialiser,
                            taskSchedulerService,
                            registryService,
                            versionService).Execute();
                        break;

                    case "update":
                        await new UpdateMode(
                            console,
                            logger,
                            fileService,
                            fileSystem,
                            configWriter,
                            databaseInitialiser,
                            registryService,
                            versionService,
                            resourceService).Execute();
                        break;

                    case "configure":
                        await new ConfigureMode(
                            console,
                            logger,
                            fileSystem,
                            configWriter,
                            versionService).Execute();
                        break;

                    case "uninstall":
                        new UninstallMode(
                            console,
                            logger,
                            fileService,
                            fileSystem,
                            taskSchedulerService,
                            registryService,
                            versionService).Execute();
                        break;
                }

                logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Server Backup Tool Installer finished.");

                return 0;
            }

            catch (Exception ex)
            {
                console.MarkupLine($"[red]An unexpected error occurred: {Markup.Escape(ex.Message)}[/]");

                logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Unexpected Error: {ex}");

                return 1;
            }
        }

        /// <summary>
        /// Determines the installer mode from command line arguments or user selection.
        /// </summary>
        private static string DetermineMode(
            IAnsiConsole console,
            string[] args)
        {
            string mode = args.Length > 0 ? args[0].TrimStart('-')
                .ToLowerInvariant() switch
                {
                    "install" => "install",
                    "update" => "update",
                    "configure" => "configure",
                    "uninstall" => "uninstall",
                    _ => PromptForMode(console)
                } : PromptForMode(console);

            return mode;
        }

        /// <summary>
        /// Prompts the user to select an installer mode.
        /// </summary>
        private static string PromptForMode(IAnsiConsole console)
        {
            string selection = console.Prompt(new SelectionPrompt<string>()
                .Title(@"Hunter Industries Server Backup Tool Installer
What operation would you like to perform?")
                .AddChoices(
                    "Install",
                    "Update",
                    "Configure",
                    "Uninstall"));

            return selection.ToLowerInvariant();
        }
    }
}
