// Copyright © - 17/01/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Entities;
using ServerBackupTool.Common.Implementations;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Common.Models.Requests;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Converters;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;

namespace ServerBackupTool.Services
{
    public class ApplicationService
    {
        private readonly ILoggerService _Logger = new LoggerServiceWrapper();
        private readonly IClock _Clock = new SystemClockProvider();
        private readonly LogService _LogService;
        private readonly CommandService _CommandService;
        private readonly PidFileService _PidFileService;
        private readonly ServerService _ServerService;
        private readonly TimerService _TimerService;
        private readonly SBTSection ServerBackupSection;
        private readonly ServerModel Server;

        public static ManualResetEvent WaitForServerClose = new(false);

        // Sets the class's global variables.
        public ApplicationService(SBTSection serverBackupSection)
        {
            DatabaseOptionsModel options = new()
            {
                Path = serverBackupSection.DatabaseDetails.Path,
                ServerName = serverBackupSection.ServerDetails.Name,
                PollingIntervalMs = serverBackupSection.DatabaseDetails.PollingInterval
            };

            ServerBackupSection = serverBackupSection;
            Server = new(serverBackupSection.ServerDetails)
            {
                Name = serverBackupSection.ServerDetails.Name,
                Game = serverBackupSection.ServerDetails.Game
            };
            IDatabase _database = new DatabaseWrapper(options);
            _LogService = new(
                _Logger,
                _database,
                _Clock,
                options
                );
            _Logger.SetLogService(_LogService);
            _CommandService = new(
                _Logger,
                _database,
                _Clock,
                options);
            _PidFileService = new(
                _Logger,
                new ExtendedFileSystemWrapper());
            _PidFileService.Delete(Server.Name);
            _ServerService = new(
                _Logger,
                _PidFileService,
                ServerBackupSection,
                Server);
            _TimerService = new(
                _Logger,
                this,
                _ServerService,
                _CommandService,
                ServerBackupSection);
        }

        /// <summary>
        /// Executes the methods to run the application.
        /// </summary>
        public async Task RunApplication()
        {
            TimeConverter _timeConverter = new(_Clock);

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"Current Time: {_Clock.UtcNow}");

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
                new ExtendedFileSystemWrapper(),
                _Clock,
                _LogService,
                ServerBackupSection);

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Stopping Server");

            await _ServerService.SendCommand(ServerConverter.GetStopCommand(Server.Game));

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Waiting for 30 Seconds");

            _timerService.WaitForClose();

            WaitForServerClose.WaitOne();
            WaitForServerClose.Reset();

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Creating Backup");

            await _jobService.RunJobs("backup");

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Archiving Logs");

            await _jobService.RunJobs("archive");

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Removing Old Backups and Logs");

            await _jobService.RunJobs("clean");

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
                        CommandRequestModel commandRequest = new()
                        {
                            Target = "Tool",
                            Command = command
                        };

                        await _CommandService.LogCommand(commandRequest);

                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Info,
                            "Exit Command Queued");

                        break;
                    }

                    else if (command.ToLower() == "start server")
                    {
                        CommandRequestModel commandRequest = new()
                        {
                            Target = "Tool",
                            Command = command
                        };

                        await _CommandService.LogCommand(commandRequest);

                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Info,
                            "Start Server Queued");

                        break;
                    }

                    else if (command.ToLower() == "reset heartbeat")
                    {
                        CommandRequestModel commandRequest = new()
                        {
                            Target = "Tool",
                            Command = command
                        };

                        await _CommandService.LogCommand(commandRequest);

                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Info,
                            "Heartbeat Reset Queued");
                    }

                    else
                    {
                        CommandRequestModel commandRequest = new()
                        {
                            Target = "Server",
                            Command = command
                        };

                        await _CommandService.LogCommand(commandRequest);

                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Info,
                            "Command Queued");
                    }
                }
            }
        }

        /// <summary>
        /// Processes the given command.
        /// </summary>
        public async Task ProcessCommand(CommandModel command)
        {
            if (command.Target == TargetType.Server)
            {
                await _ServerService.SendCommand(command.Command);

                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Command Sent to Server: {command}");
            }

            else
            {
                if (command.Command.ToLower() == "exit app")
                {
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Info,
                        "Exit Command Triggered");

                    if (Server.ServerRunning)
                    {
                        await _ServerService.SendCommand(ServerConverter.GetStopCommand(Server.Game));

                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Debug,
                            "Stop Command Sent to Server");
                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Debug,
                            "Waiting for 30 seconds");

                        Thread.Sleep(30000);
                    }
                }

                else if (command.Command.ToLower() == "start server")
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

                else if (command.Command.ToLower() == "reset heartbeat")
                {
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Info,
                        "Restarting Heartbeat Timer");

                    _TimerService.RestartHeartbeat();
                }
            }
        }
    }
}
