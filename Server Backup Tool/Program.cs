// Copyright © - 17/01/2024 - Toby Hunter
using log4net;
using System.Configuration;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Converters;
using ServerBackupTool.Services;
using ServerBackupTool.Models;

namespace ServerBackupTool
{
    internal class Program
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");
        static SBTSection? ServerBackupSection;
        static ServerModel? Server;
        public static ManualResetEvent WaitForServerClose = new(false);

        static void Main()
        {
            log4net.Config.XmlConfigurator.Configure();

            EmailService _emailService = new();

            // Development Settings
            string configFilePath = Path.Combine("D:\\System Folders\\Documents\\GitHub\\Server-Backup-Tool\\Server Backup Tool\\", "App - Development.config");

            ExeConfigurationFileMap configMap = new()
            {
                ExeConfigFilename = configFilePath
            };
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            ServerBackupSection = (SBTSection)config.GetSection("serverBackup");

            //SBTSection? serverBackupSection = ConfigurationManager.GetSection("serverBackup") as SBTSection;

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            Server = new(ServerBackupSection.ServerDetails)
            {
                Game = ServerBackupSection.ServerDetails.Game
            };

            /* Move this to a new method start. */
            if (ServerBackupSection.Notifications.Emails.Count != 0)
            {
                foreach (EmailElement email in ServerBackupSection.Notifications.Emails)
                {
                    if (email.Trigger == "Open")
                    {
                        _emailService.SendEmail(ServerBackupSection.Notifications, email);
                    }
                }
            }

            ServerService _serverService = new(ServerBackupSection, Server);

            RunProgram(ServerBackupSection, Server);

            while (true)
            {
                string command = Console.ReadLine();

                if (!string.IsNullOrEmpty(command))
                {
                    if (command.ToLower() == "exit app")
                    {
                        Console.WriteLine("Exit Command Triggered");
                        Log.Info("Exit Command Triggered");
                        break;
                    }

                    else if (command.ToLower() == "start server")
                    {
                        if (!Server.ServerRunning)
                        {
                            Console.WriteLine("Starting Server");
                            Log.Info("Starting Server");

                            _serverService.StartServer();

                            Console.WriteLine("\n----Server Commands----");
                            Log.Info("----Server Commands----");
                        }
                    }

                    else
                    {
                        _serverService.SendCommand(command);
                        Log.Debug($"Command Sent to Server: {command}");
                    }
                }
            }
            /* Move this to a new method end. */
        }

        /* Move this to a new method start. */
        static void RunProgram(SBTSection serverBackupSection, ServerModel server)
        {
            TimeConverter _timeConverter = new();
            ServerService _serverService = new(serverBackupSection, server);
            TimerService _timerService = new(serverBackupSection, _serverService);

            Console.WriteLine("Current Time: {0}", DateTime.Now);
            Log.Info($"Current Time: {DateTime.Now}");

            TimeSpan[] timerDurations = Array.Empty<TimeSpan>();

            TimeSpan duration = _timeConverter.GetDuration(serverBackupSection.TimerDetails.BackupTime);

            Console.WriteLine("Time before backup: {0}", duration);
            Log.Debug($"Time before backup: {duration}");
            
            timerDurations = timerDurations.Append(duration).ToArray();

            foreach (TimerElement timer in serverBackupSection.TimerDetails.Timers)
            {
                duration = _timeConverter.GetDuration(timer.Time);

                Console.WriteLine("Time before {0}: {1}", timer.Name.ToLower(), duration);
                Log.Debug($"Time before {timer.Name.ToLower()}: {duration}");

                timerDurations = timerDurations.Append(duration).ToArray();
            }

            string result = _timerService.SetTimers(serverBackupSection.TimerDetails.Timers, timerDurations);

            Console.WriteLine("Setting Timers: {0}", result);
            Log.Info($"Setting Timers: {result}");

            Console.WriteLine($"Starting Timers");
            Log.Info("Starting Timers");

            _timerService.StartTimers();

            result = _serverService.StartServer();

            Console.WriteLine("Starting Server: {0}", result);
            Log.Info($"Starting Server: {result}");

            Console.WriteLine("\n----Server Commands----");
            Log.Info("----Server Commands----");
        }

        public static void TakeBackup(TimerService _timerService)
        {
            ServerService _serverService = new(ServerBackupSection, Server);
            ServerConverter _serverConverter = new();
            JobService _jobService = new(ServerBackupSection);

            Console.WriteLine("Stopping Server");
            Log.Info("Stopping Server");

            _serverService.SendCommand(_serverConverter.GetStopCommand(Server.Game));

            Console.WriteLine("Waiting for 30 Seconds");
            Log.Info("Waiting for 30 Seconds");

            _timerService.WaitForClose();
            WaitForServerClose.WaitOne();
            WaitForServerClose.Reset();

            Console.WriteLine("Creating Backup");
            Log.Info("Creating Backup");

            _jobService.RunJobs("backup");

            Console.WriteLine("Archiving Logs");
            Log.Info("Archiving Logs");

            _jobService.RunJobs("archive");

            Console.WriteLine("Removing Old Backups and Logs");
            Log.Info("Removing Old Backups and Logs");

            _jobService.RunJobs("clean");

            Console.WriteLine("Restarting Process");
            Log.Info("Restarting Process");

            RunProgram(new SBTSection(), null);
        }
        /* Move this to a new method end. */

        static void OnProcessExit(object? sender, EventArgs e)
        {
            EmailService _emailService = new();

            if (ServerBackupSection.Notifications.Emails.Count != 0)
            {
                foreach (EmailElement email in ServerBackupSection.Notifications.Emails)
                {
                    if (email.Trigger == "Close")
                    {
                        _emailService.SendEmail(ServerBackupSection.Notifications, email);
                    }
                }
            }
        }
    }
}