// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;
using ServerBackupTool.Functions;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;

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
                new FileSystem());

            Console.SetOut(new FilterConsoleFunction(Console.Out));

            log4net.Config.XmlConfigurator.Configure();

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            ServerBackupSection = ConfigurationManager.GetSection("serverBackup") as SBTSection;

            if (ServerBackupSection == null)
            {
                Environment.Exit(0);
            }

            _emailService.CheckForEmail(
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
                new FileSystem());
            PidFileService _pidFileService = new(
                new LoggerServiceWrapper(),
                new FileSystem());

            if (ServerBackupSection != null)
            {
                _emailService.CheckForEmail(
                    ServerBackupSection.Notifications,
                    "Close");

                _pidFileService.Delete(ServerBackupSection.ServerDetails.Name);
            }
        }
    }
}