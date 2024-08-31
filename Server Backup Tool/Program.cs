// Copyright © - 17/01/2024 - Toby Hunter
using log4net;
using System.Configuration;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Converters;
using ServerBackupTool.Services;

namespace ServerBackupTool
{
    internal class Program
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");
        static SBTSection ServerBackupSection;
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
            
            RunProgram(ServerBackupSection);

            while (true)
            {
                string Command = Console.ReadLine();

                if (!string.IsNullOrEmpty(Command))
                {
                    if (Command == "Exit App")
                    {
                        Console.WriteLine("Exit Command Triggered");
                        Log.Info("Exit Command Triggered");
                        break;
                    }

                    else if (Command == "Start Server")
                    {
                        if (!Server_Information.IsRunning)
                        {
                            Console.WriteLine("Starting Server");
                            Log.Info("Starting Server");

                            Server.StartServer();

                            Console.WriteLine("\n----Server Commands----");
                            Log.Info("----Server Commands----");
                        }
                    }

                    else if (Command == "stop")
                    {
                        if (Server_Information.IsRunning)
                        {
                            Log.Debug($"Command Sent to Server: {Command}");

                            Console.WriteLine("\nStopping Server");
                            Log.Info("Stopping Server");

                            Server.StopServer();
                        }
                    }

                    else
                    {
                        Server.SendCommand(Command);
                        Log.Debug($"Command Sent to Server: {Command}");
                    }
                }
            }
        }

        static void RunProgram(SBTSection serverBackupSection)
        {
            TimeConverter _timeConverter = new();
            TimerService _timerService = new(serverBackupSection);

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

            result = Server.StartServer();

            Console.WriteLine("Starting Server: {0}", result);
            Log.Info($"Starting Server: {result}");

            Console.WriteLine("\n----Server Commands----");
            Log.Info("----Server Commands----");
        }

        public static void TakeBackup(TimerService _timerService)
        {
            Console.WriteLine("Stopping Server");
            Log.Info("Stopping Server");

            Server.StopServer();

            Console.WriteLine("Waiting for 30 Seconds");
            Log.Info("Waiting for 30 Seconds");

            _timerService.WaitForClose();
            WaitForServerClose.WaitOne();
            WaitForServerClose.Reset();

            Console.WriteLine("Creating Backup");
            Log.Info("Creating Backup");

            Archive_Jobs.BackupServer();

            Console.WriteLine("Archiving Logs");
            Log.Info("Archiving Logs");

            Archive_Jobs.ArchiveLogs();

            Console.WriteLine("Removing Old Backups and Logs");
            Log.Info("Removing Old Backups and Logs");

            Archive_Jobs.RemoveOldFiles();

            Console.WriteLine("Restarting Process");
            Log.Info("Restarting Process");

            RunProgram(new SBTSection());
        }

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