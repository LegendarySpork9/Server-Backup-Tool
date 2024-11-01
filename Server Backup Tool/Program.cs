// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;

namespace ServerBackupTool
{
    internal class Program
    {
        static SBTSection? ServerBackupSection;

        // Configures the application.
        static void Main()
        {
            EmailService _emailService = new();

            log4net.Config.XmlConfigurator.Configure();

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            ServerBackupSection = ConfigurationManager.GetSection("serverBackup") as SBTSection;

            _emailService.CheckForEmail(ServerBackupSection.Notifications, "Open");

            ApplicationService _applicationService = new(ServerBackupSection);

            _applicationService.RunApplication();
        }

        // Runs code when the application closes.
        static void OnProcessExit(object? sender, EventArgs e)
        {
            EmailService _emailService = new();

            _emailService.CheckForEmail(ServerBackupSection.Notifications, "Close");
        }
    }
}