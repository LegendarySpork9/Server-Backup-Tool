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
    public class ApplicationService : IApplicationService
    {
        private readonly ILoggerService _Logger;
        private readonly IClock _Clock;
        private readonly ICommandReader _CommandReader;
        private readonly LogService _LogService;
        private readonly ICommandService _CommandService;
        private readonly IPidFileService _PidFileService;
        private readonly IServerService _ServerService;
        private readonly ITimerService _TimerService;
        private readonly IJobService _JobService;
        private readonly SBTSection ServerBackupSection;
        private readonly ServerModel Server;
        internal readonly TimeSpan ExitDelay;
        internal readonly Action ExitAction;

        public static ManualResetEvent WaitForServerClose = new(false);

        // Sets the class's global variables.
        public ApplicationService(
            SBTSection serverBackupSection,
            ICommandReader commandReader)
        {
            _Logger = new LoggerServiceWrapper();
            _Clock = new SystemClockProvider();

            DatabaseOptionsModel options = new()
            {
                Path = serverBackupSection.DatabaseDetails.Path,
                ServerName = serverBackupSection.ServerDetails.Name,
                PollingIntervalMs = serverBackupSection.DatabaseDetails.PollingInterval
            };

            _CommandReader = commandReader;
            ServerBackupSection = serverBackupSection;
            Server = new(serverBackupSection.ServerDetails)
            {
                Name = serverBackupSection.ServerDetails.Name,
                Game = serverBackupSection.ServerDetails.Game
            };
            IExtendedDatabase _database = new ExtendedDatabaseWrapper(options);
            _LogService = new(
                _Logger,
                _database,
                _Clock,
                options
                );
            _Logger.SetLogService(_LogService);
            _CommandService = new CommandService(
                _Logger,
                _database,
                _Clock,
                options);
            _PidFileService = new PidFileService(
                _Logger,
                new ExtendedFileSystemWrapper());
            _PidFileService.Delete(Server.Name);
            _ServerService = new ServerService(
                _Logger,
                _PidFileService,
                ServerBackupSection,
                Server);
            _TimerService = new TimerService(
                _Logger,
                this,
                _ServerService,
                _CommandService,
                new EmailService(
                    _Logger,
                    new SMTPEmailSender(),
                    new ExtendedFileSystemWrapper(),
                    true),
                new PingProvider(),
                ServerBackupSection);
            _JobService = new JobService(
                _Logger,
                new ExtendedFileSystemWrapper(),
                _Clock,
                _LogService,
                ServerBackupSection);
            ExitDelay = TimeSpan.FromSeconds(30);
            ExitAction = () => Environment.Exit(0);
        }

        // Sets the class's global variables via dependency injection.
        internal ApplicationService(
            ILoggerService logger,
            IClock clock,
            ICommandReader commandReader,
            ICommandService commandService,
            IPidFileService pidFileService,
            IServerService serverService,
            ITimerService timerService,
            IJobService jobService,
            SBTSection serverBackupSection,
            ServerModel server,
            TimeSpan? exitDelay = null,
            Action? exitAction = null)
        {
            _Logger = logger;
            _Clock = clock;
            _CommandReader = commandReader;
            _LogService = null!;
            _CommandService = commandService;
            _PidFileService = pidFileService;
            _ServerService = serverService;
            _TimerService = timerService;
            _JobService = jobService;
            ServerBackupSection = serverBackupSection;
            Server = server;
            ExitDelay = exitDelay ?? TimeSpan.FromSeconds(30);
            ExitAction = exitAction ?? (() => Environment.Exit(0));
        }

        /// <summary>
        /// Executes the methods to run the application.
        /// </summary>
        public async Task RunApplication(CancellationToken cancellationToken = default)
        {
            TimeConverter _timeConverter = new(_Clock);

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"Current Time: {_Clock.UtcNow}");

            TimeSpan[] timerDurations = Array.Empty<TimeSpan>();
            TimeSpan duration = _timeConverter.GetDuration(ServerBackupSection.TimerDetails.BackupTime);

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"Time before backup: {duration:hh\\:mm\\:ss}");

            timerDurations = timerDurations.Append(duration)
                .ToArray();

            foreach (TimerElement timer in ServerBackupSection.TimerDetails.Timers)
            {
                duration = _timeConverter.GetDuration(timer.Time);

                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Info,
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

            _TimerService.StartQueuedCommandCheckTimer();

            await UserInput(cancellationToken);
        }

        /// <summary>
        /// Executes the methods to take a backup of the server and log data.
        /// </summary>
        public async Task RunBackup(ITimerService _timerService, CancellationToken cancellationToken = default)
        {
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

            await _JobService.RunJobs("backup");

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Archiving Logs");

            await _JobService.RunJobs("archive");

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Removing Old Backups and Logs");

            await _JobService.RunJobs("clean");

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Restarting Process");

            await RunApplication(cancellationToken);
        }

        /// <summary>
        /// Handles inputs from the user.
        /// </summary>
        private async Task UserInput(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? command = _CommandReader.ReadCommand();

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
                    StandardValues.LoggerValues.Info,
                    $"Command Sent to Server: {command.Command}");
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
                            StandardValues.LoggerValues.Info,
                            "Stop Command Sent to Server");
                        _Logger.LogToolMessage(
                            StandardValues.LoggerValues.Info,
                            "Waiting for 30 seconds");

                        Thread.Sleep(ExitDelay);
                    }

                    await _CommandService.DeleteCommand(command.Id);
                    _TimerService.StopQueuedCommandCheckTimer();
                    ExitAction();
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