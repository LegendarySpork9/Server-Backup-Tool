// Copyright © - 17/01/2024 - Toby Hunter
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Functions;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;
using System.Configuration;

namespace ServerBackupTool
{
    internal class Program
    {
        static SBTSection? ServerBackupSection;

        /// <summary>
        /// Configures the application.
        /// </summary>
        static async Task Main()
        {
            EmailService _emailService = new(
                new LoggerServiceWrapper(),
                new SMTPEmailSender(),
                new ExtendedFileSystemWrapper());

            Console.SetOut(new ConsoleFunction(Console.Out));

            log4net.Config.XmlConfigurator.Configure();

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            ServerBackupSection = ConfigurationManager.GetSection("serverBackup") as SBTSection;

            if (ServerBackupSection == null)
            {
                Environment.Exit(0);
            }

            await _emailService.CheckForEmail(
                ServerBackupSection.Notifications,
                "Open");

            ApplicationService _applicationService = new(ServerBackupSection);

            await _applicationService.RunApplication();
        }

        /// <summary>
        /// Runs code when the application closes.
        /// </summary>
        static void OnProcessExit(
            object? sender,
            EventArgs e)
        {
            EmailService _emailService = new(
                new LoggerServiceWrapper(),
                new SMTPEmailSender(),
                new ExtendedFileSystemWrapper());
            PidFileService _pidFileService = new(
                new LoggerServiceWrapper(),
                new ExtendedFileSystemWrapper());

            if (ServerBackupSection != null)
            {
                DatabaseOptionsModel options = new()
                {
                    Path = ServerBackupSection.DatabaseDetails.Path,
                    ServerName = ServerBackupSection.ServerDetails.Name,
                    PollingIntervalMs = ServerBackupSection.DatabaseDetails.PollingInterval
                };

                LogService _logService = new(
                    new LoggerServiceWrapper(),
                    new DatabaseWrapper(options),
                    new SystemClockProvider(),
                    options);

                _emailService.CheckForEmail(
                    ServerBackupSection.Notifications,
                    "Close").GetAwaiter()
                    .GetResult();
                _pidFileService.Delete(ServerBackupSection.ServerDetails.Name);
                _logService.ClearLogs("Tool")
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}