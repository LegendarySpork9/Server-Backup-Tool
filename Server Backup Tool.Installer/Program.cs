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

                string mode = DetermineMode(args);

                switch (mode)
                {
                    case "install":
                        await new InstallMode(
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
                            logger,
                            fileSystem,
                            configWriter,
                            versionService).Execute();
                        break;

                    case "uninstall":
                        new UninstallMode(
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
                AnsiConsole.MarkupLine($"[red]An unexpected error occurred: {Markup.Escape(ex.Message)}[/]");

                logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    $"Unexpected Error: {ex}");

                return 1;
            }
        }

        /// <summary>
        /// Determines the installer mode from command line arguments or user selection.
        /// </summary>
        private static string DetermineMode(string[] args)
        {
            string mode = args.Length > 0 ? args[0].TrimStart('-')
                .ToLowerInvariant() switch
                {
                    "install" => "install",
                    "update" => "update",
                    "configure" => "configure",
                    "uninstall" => "uninstall",
                    _ => PromptForMode()
                } : PromptForMode();

            return mode;
        }

        /// <summary>
        /// Prompts the user to select an installer mode.
        /// </summary>
        private static string PromptForMode()
        {
            string selection = AnsiConsole.Prompt(new SelectionPrompt<string>()
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
