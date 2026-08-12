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
        private readonly ILoggerService _Logger = new LoggerServiceWrapper();
        private readonly PidFileService _PidFileService;
        private readonly ServerService _ServerService;
        private readonly TimerService _TimerService;
        private readonly SBTSection ServerBackupSection;
        private readonly ServerModel Server;
        private readonly SystemClock Clock = new();

        public static ManualResetEvent WaitForServerClose = new(false);

        // Sets the class's global variables.
        public ApplicationService(SBTSection serverBackupSection)
        {
            ServerBackupSection = serverBackupSection;
            Server = new(serverBackupSection.ServerDetails)
            {
                Name = serverBackupSection.ServerDetails.Name,
                Game = serverBackupSection.ServerDetails.Game
            };
            _PidFileService = new(
                _Logger,
                new FileSystem());
            _PidFileService.Delete(Server.Name);
            _ServerService = new(
                _Logger,
                _PidFileService,
                ServerBackupSection,
                Server);
            _TimerService = new(
                this,
                _ServerService,
                _Logger,
                ServerBackupSection);
        }

        /// <summary>
        /// Executes the methods to run the application.
        /// </summary>
        public async Task RunApplication()
        {
            TimeConverter _timeConverter = new(Clock);

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"Current Time: {Clock.UtcNow}");

            TimeSpan[] timerDurations = Array.Empty<TimeSpan>();
            TimeSpan duration = _timeConverter.GetDuration(ServerBackupSection.TimerDetails.BackupTime);

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"Time before backup: {duration:hh\\:mm\\:ss}");

            timerDurations = timerDurations.Append(duration)
                .ToArray();

            foreach (TimerElement timer in ServerBackupSection.TimerDetails.Timers)
            {
                duration = _timeConverter.GetDuration(timer.Time);

                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Time before {timer.Name.ToLower()}: {duration:hh\\:mm\\:ss}");

                timerDurations = timerDurations.Append(duration)
                    .ToArray();
            }

            string result = _TimerService.SetTimers(
                ServerBackupSection.TimerDetails.Timers,
                timerDurations);

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"Setting Timers: {result}");
            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Starting Timers");

            _TimerService.StartTimers();

            result = await _ServerService.StartServer();

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"Starting Server: {result}",
                true);

            await UserInput();
        }

        /// <summary>
        /// Executes the methods to take a backup of the server and log data.
        /// </summary>
        public async Task RunBackup(TimerService _timerService)
        {
            JobService _jobService = new(
                _Logger,
                new FileSystem(),
                Clock,
                ServerBackupSection);

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Stopping Server");

            _ServerService.SendCommand(ServerConverter.GetStopCommand(Server.Game));

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Waiting for 30 Seconds");

            _timerService.WaitForClose();

            WaitForServerClose.WaitOne();
            WaitForServerClose.Reset();

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Creating Backup");

            _jobService.RunJobs("backup");

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Archiving Logs");

            _jobService.RunJobs("archive");

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Removing Old Backups and Logs");

            _jobService.RunJobs("clean");

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Restarting Process");

            await RunApplication();
        }

        /// <summary>
        /// Handles inputs from the user.
        /// </summary>
        private async Task UserInput()
        {
            while (true)
            {
                string? command = Console.ReadLine();

                if (!string.IsNullOrEmpty(command))
                {
                    if (command.ToLower() == "exit app")
                    {
                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Info,
                            "Exit Command Triggered");

                        if (Server.ServerRunning)
                        {
                            _ServerService.SendCommand(ServerConverter.GetStopCommand(Server.Game));

                            _Logger.LogToolMessage(
                                StandardValues.LoggerValues.Debug,
                                "Stop Command Sent to Server");
                            _Logger.LogToolMessage(
                                StandardValues.LoggerValues.Debug,
                                "Waiting for 30 seconds");

                            Thread.Sleep(30000);
                        }

                        break;
                    }

                    else if (command.ToLower() == "start server")
                    {
                        if (!Server.ServerRunning)
                        {
                            _Logger.LogToolMessage(
                                StandardValues.LoggerValues.Info,
                                "Starting Server");
                            
                            await _ServerService.StartServer();

                            Console.WriteLine("\n----Server Commands----");
                        }
                    }

                    else if (command.ToLower() == "reset heartbeat")
                    {
                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Info,
                            "Restarting Heartbeat Timer");

                        _TimerService.RestartHeartbeat();
                    }

                    else
                    {
                        _ServerService.SendCommand(command);

                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Debug,
                            $"Command Sent to Server: {command}");
                    }
                }
            }
        }
    }
}
