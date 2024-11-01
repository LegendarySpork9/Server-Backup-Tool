// Copyright © - 17/01/2024 - Toby Hunter
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Models;
using ServerBackupTool.Converters;

namespace ServerBackupTool.Services
{
    internal class ApplicationService
    {
        readonly LoggerService Logger = new();
        readonly ServerService _ServerService;
        TimerService _TimerService;
        static SBTSection? ServerBackupSection;
        static ServerModel? Server;
        public static ManualResetEvent WaitForServerClose = new(false);

        // Sets the class's global variables.
        public ApplicationService(SBTSection serverBackupSection)
        {
            ServerBackupSection = serverBackupSection;
            Server = new(serverBackupSection.ServerDetails)
            {
                Game = serverBackupSection.ServerDetails.Game
            };
            _ServerService = new(ServerBackupSection, Server);
            _TimerService = new(this, _ServerService, ServerBackupSection);
        }

        // Executes the methods to run the application.
        public void RunApplication()
        {
            TimeConverter _timeConverter = new();

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, $"Current Time: {DateTime.Now}");

            TimeSpan[] timerDurations = Array.Empty<TimeSpan>();
            TimeSpan duration = _timeConverter.GetDuration(ServerBackupSection.TimerDetails.BackupTime);

            Logger.LogToolMessage(StandardValues.LoggerValues.Debug, $"Time before backup: {duration}");

            timerDurations = timerDurations.Append(duration).ToArray();

            foreach (TimerElement timer in ServerBackupSection.TimerDetails.Timers)
            {
                duration = _timeConverter.GetDuration(timer.Time);

                Logger.LogToolMessage(StandardValues.LoggerValues.Debug, $"Time before {timer.Name.ToLower()}: {duration}");

                timerDurations = timerDurations.Append(duration).ToArray();
            }

            string result = _TimerService.SetTimers(ServerBackupSection.TimerDetails.Timers, timerDurations);

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, $"Setting Timers: {result}");
            Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Starting Timers");

            _TimerService.StartTimers();
            result = _ServerService.StartServer();

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, $"Starting Server: {result}");
            Console.WriteLine("\n----Server Commands----");

            UserInput();
        }

        // Executes the methods to take a backup of the server and log data.
        public void RunBackup(TimerService _timerService)
        {
            ServerConverter _serverConverter = new();
            JobService _jobService = new(ServerBackupSection);

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Stopping Server");

            _ServerService.SendCommand(_serverConverter.GetStopCommand(Server.Game));

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Waiting for 30 Seconds");

            _timerService.WaitForClose();
            WaitForServerClose.WaitOne();
            WaitForServerClose.Reset();

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Creating Backup");

            _jobService.RunJobs("backup");

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Archiving Logs");

            _jobService.RunJobs("archive");

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Removing Old Backups and Logs");

            _jobService.RunJobs("clean");

            Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Restarting Process");

            RunApplication();
        }

        // Handles inputs from the user.
        private void UserInput()
        {
            while (true)
            {
                string command = Console.ReadLine();

                if (!string.IsNullOrEmpty(command))
                {
                    if (command.ToLower() == "exit app")
                    {
                        Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Exit Command Triggered");

                        if (Server.ServerRunning)
                        {
                            ServerConverter _serverConverter = new();

                            _ServerService.SendCommand(_serverConverter.GetStopCommand(Server.Game));
                        }

                        break;
                    }

                    else if (command.ToLower() == "start server")
                    {
                        if (!Server.ServerRunning)
                        {
                            Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Starting Server");
                            _ServerService.StartServer();

                            Console.WriteLine("\n----Server Commands----");
                        }
                    }

                    else
                    {
                        _ServerService.SendCommand(command);
                        Logger.LogToolMessage(StandardValues.LoggerValues.Debug, $"Command Sent to Server: {command}");
                    }
                }
            }
        }
    }
}
