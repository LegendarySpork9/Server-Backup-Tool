// Copyright © - 17/01/2024 - Toby Hunter
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Models;
using ServerBackupTool.Converters;
using ServerBackupTool.Implementations;
using ServerBackupTool.Abstractions;

namespace ServerBackupTool.Services
{
    public class ApplicationService
    {
        readonly ILoggerService _Logger = new LoggerServiceWrapper();
        readonly ServerService _ServerService;
        readonly TimerService _TimerService;
        readonly SBTSection ServerBackupSection;
        readonly ServerModel Server;
        readonly SystemClock Clock = new();

        public static ManualResetEvent WaitForServerClose = new(false);

        // Sets the class's global variables.
        public ApplicationService(SBTSection serverBackupSection)
        {
            ServerBackupSection = serverBackupSection;
            Server = new(serverBackupSection.ServerDetails)
            {
                Game = serverBackupSection.ServerDetails.Game
            };
            _ServerService = new(_Logger, ServerBackupSection, Server);
            _TimerService = new(this, _ServerService, _Logger, ServerBackupSection);
        }

        // Executes the methods to run the application.
        public void RunApplication()
        {
            TimeConverter _timeConverter = new(Clock);

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, $"Current Time: {Clock.UtcNow}");

            TimeSpan[] timerDurations = Array.Empty<TimeSpan>();
            TimeSpan duration = _timeConverter.GetDuration(ServerBackupSection.TimerDetails.BackupTime);

            _Logger.LogToolMessage(StandardValues.LoggerValues.Debug, $"Time before backup: {duration:hh\\:mm\\:ss}");

            timerDurations = timerDurations.Append(duration).ToArray();

            foreach (TimerElement timer in ServerBackupSection.TimerDetails.Timers)
            {
                duration = _timeConverter.GetDuration(timer.Time);

                _Logger.LogToolMessage(StandardValues.LoggerValues.Debug, $"Time before {timer.Name.ToLower()}: {duration:hh\\:mm\\:ss}");

                timerDurations = timerDurations.Append(duration).ToArray();
            }

            string result = _TimerService.SetTimers(ServerBackupSection.TimerDetails.Timers, timerDurations);

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, $"Setting Timers: {result}");
            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Starting Timers");

            _TimerService.StartTimers();

            result = _ServerService.StartServer();

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, $"Starting Server: {result}", true);

            UserInput();
        }

        // Executes the methods to take a backup of the server and log data.
        public void RunBackup(TimerService _timerService)
        {
            ServerConverter _serverConverter = new();
            JobService _jobService = new(_Logger, new FileSystem(), Clock, ServerBackupSection);

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Stopping Server");

            _ServerService.SendCommand(_serverConverter.GetStopCommand(Server.Game));

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Waiting for 30 Seconds");

            _timerService.WaitForClose();

            WaitForServerClose.WaitOne();
            WaitForServerClose.Reset();

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Creating Backup");

            _jobService.RunJobs("backup");

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Archiving Logs");

            _jobService.RunJobs("archive");

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Removing Old Backups and Logs");

            _jobService.RunJobs("clean");

            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Restarting Process");

            RunApplication();
        }

        // Handles inputs from the user.
        private void UserInput()
        {
            while (true)
            {
                string? command = Console.ReadLine();

                if (!string.IsNullOrEmpty(command))
                {
                    if (command.ToLower() == "exit app")
                    {
                        _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Exit Command Triggered");

                        if (Server.ServerRunning)
                        {
                            ServerConverter _serverConverter = new();

                            _ServerService.SendCommand(_serverConverter.GetStopCommand(Server.Game));

                            _Logger.LogToolMessage(StandardValues.LoggerValues.Debug, "Stop Command Sent to Server");
                            _Logger.LogToolMessage(StandardValues.LoggerValues.Debug, "Waiting for 30 seconds");

                            Thread.Sleep(30000);
                        }

                        break;
                    }

                    else if (command.ToLower() == "start server")
                    {
                        if (!Server.ServerRunning)
                        {
                            _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Starting Server");
                            _ServerService.StartServer();

                            Console.WriteLine("\n----Server Commands----");
                        }
                    }

                    else if (command.ToLower() == "reset heartbeat")
                    {
                        _Logger.LogToolMessage(StandardValues.LoggerValues.Info, "Restarting Heartbeat Timer");

                        _TimerService.RestartHeartbeat();
                    }

                    else
                    {
                        _ServerService.SendCommand(command);
                        _Logger.LogToolMessage(StandardValues.LoggerValues.Debug, $"Command Sent to Server: {command}");
                    }
                }
            }
        }
    }
}
