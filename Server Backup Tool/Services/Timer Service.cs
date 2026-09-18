// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using System.Net.NetworkInformation;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerBackupTool.Services
{
    public class TimerService : ITimerService
    {
        private readonly ILoggerService _Logger;
        private readonly IApplicationService _ApplicationService;
        private readonly IServerService _ServerService;
        private readonly ICommandService _CommandService;
        private readonly IEmailService _EmailService;
        private readonly IPingProvider _PingProvider;
        private readonly SBTSection ServerBackupSection;
        private readonly bool DoHeartbeat = false;
        private readonly List<TimerModel> Timers = [];
        private bool DoProcessQueuedCommands = false;

        // Sets the class's global variables.
        public TimerService(
            ILoggerService _logger,
            IApplicationService _applicationService,
            IServerService _serverService,
            ICommandService _commandService,
            IEmailService _emailService,
            IPingProvider _pingProvider,
            SBTSection serverBackupSection)
        {
            if (serverBackupSection.Notifications.Emails.Count != 0)
            {
                foreach (EmailElement email in serverBackupSection.Notifications.Emails)
                {
                    if (email.Trigger == "Heartbeat")
                    {
                        DoHeartbeat = true;
                    }
                }
            }

            _Logger = _logger;
            _ApplicationService = _applicationService;
            _ServerService = _serverService;
            _CommandService = _commandService;
            _EmailService = _emailService;
            _PingProvider = _pingProvider;
            ServerBackupSection = serverBackupSection;
        }

        /// <summary>
        /// Configures the timers.
        /// </summary>
        public string SetTimers(
            TimerCollection timerDetails,
            TimeSpan[] timerDurations)
        {
            string result = "Completed";
            int timerNumber = 0;
            Timers.Clear();

            try
            {
                for (int x = 0; x < SystemTimerModel.Names.Length; x++)
                {
                    if (SystemTimerModel.Names[x] == "Heartbeat" && !DoHeartbeat)
                    {
                        continue;
                    }

                    Timer timerData = new()
                    {
                        Interval = SystemTimerModel.Durations[x]
                    };

                    int currentTimerNumber = timerNumber;
                    timerData.Elapsed += async (sender, e) => await TimerElapsed(
                        sender,
                        e,
                        currentTimerNumber);
                    timerNumber++;

                    Timers.Add(new TimerModel
                    {
                        TimerName = SystemTimerModel.Names[x],
                        TimerData = timerData
                    });
                }

                Timers[^1].TimerData.Interval = timerDurations[0].TotalMilliseconds;

                Timer queuedCommandsCheckData = new()
                {
                    Interval = ServerBackupSection.DatabaseDetails.PollingInterval,
                    AutoReset = false
                };
                queuedCommandsCheckData.Elapsed += async (sender, e) => await ProcessQueuedCommands(
                    sender,
                    e);

                Timers.Add(new()
                {
                    TimerName = "QueuedCommandCheck",
                    TimerData = queuedCommandsCheckData
                });

                for (int x = 0; x < timerDetails.Count; x++)
                {
                    Timer timerData = new()
                    {
                        Interval = timerDurations[x + 1].TotalMilliseconds,
                    };

                    int currentTimerNumber = timerNumber;
                    timerData.Elapsed += async (sender, e) => await TimerElapsed(
                        sender,
                        e,
                        currentTimerNumber);
                    timerNumber++;

                    Timers.Add(new TimerModel
                    {
                        TimerName = timerDetails[x].Name,
                        ElapsedMessage = timerDetails[x].Message,
                        TimerData = timerData
                    });
                }
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    "Failed to set up the timers.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());

                result = "Errored";
            }

            return result;
        }

        /// <summary>
        /// Activates the timers.
        /// </summary>
        public void StartTimers()
        {
            foreach (TimerModel timer in Timers)
            {
                if (timer.TimerName != "Wait" && timer.TimerName != "QueuedCommandCheck")
                {
                    timer.TimerData.Start();
                }
            }
        }

        /// <summary>
        /// Activates the QueuedCommandCheck timer.
        /// </summary>
        public void StartQueuedCommandCheckTimer()
        {
            DoProcessQueuedCommands = true;
            Timers.First(t => t.TimerName == "QueuedCommandCheck").TimerData.Start();
        }

        /// <summary>
        /// Stops the processing of queued commands.
        /// </summary>
        public void StopQueuedCommandCheckTimer() => DoProcessQueuedCommands = false;

        /// <summary>
        /// Activates the heartbeat timer.
        /// </summary>
        public void RestartHeartbeat()
        {
            TimerModel? heartbeat = Timers.Find(c => c.TimerName == "Heartbeat");

            if (heartbeat != null)
            {
                heartbeat.TimerData.Start();
            }
        }

        /// <summary>
        /// Activates the server closing delay timer.
        /// </summary>
        public void WaitForClose()
        {
            foreach (TimerModel timer in Timers)
            {
                if (timer.TimerName == "Wait")
                {
                    timer.TimerData.Start();
                }
            }
        }

        /// <summary>
        /// Runs when a timer has finished.
        /// </summary>
        private async Task TimerElapsed(
            object? sender,
            ElapsedEventArgs e,
            int timerNumber)
        {
            switch (timerNumber)
            {
                case 0:
                    await Heartbeat();
                    break;
                case 1:
                    await SystemTimers(1);
                    break;
                case 2:
                    await SystemTimers(2);
                    break;
                default:
                    await ServerWarning(timerNumber);
                    break;
            }
        }

        /// <summary>
        /// Runs code related to built in timers.
        /// </summary>
        internal async Task SystemTimers(int timerIndex)
        {
            TimerModel timer = Timers[timerIndex];
            timer.TimerData.Stop();

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"{timer.TimerName} Triggered");

            timer.TimerData.Dispose();
            timer.Triggered = true;

            if (timer.TimerName == "Backup")
            {
                StopQueuedCommandCheckTimer();
                await _ApplicationService.RunBackup(this);
            }

            else
            {
                ApplicationService.WaitForServerClose.Set();
            }
        }

        /// <summary>
        /// Runs code related to the server timers.
        /// </summary>
        internal async Task ServerWarning(int timerIndex)
        {
            TimerModel timer = Timers[timerIndex];
            timer.TimerData.Stop();

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"{timer.TimerName} Triggered");
            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Debug,
                $"Warning Message: {timer.ElapsedMessage}",
                true);

            timer.TimerData.Dispose();
            timer.Triggered = true;

            await _ServerService.SendCommand(
                timer.ElapsedMessage ?? "No elapsed message configured",
                true);
        }

        /// <summary>
        /// Runs the heartbeat check logic.
        /// </summary>
        internal async Task Heartbeat()
        {
            PingReply reply = await _PingProvider.SendPingAsync(
                ServerBackupSection.ServerDetails.IPAddress,
                5000);

            if (reply.Status != IPStatus.Success)
            {
                Timers[0].TimerData.Stop();

                await _EmailService.CheckForEmail(
                    ServerBackupSection.Notifications,
                    "Heartbeat");
            }
        }

        /// <summary>
        /// Runs the queued command processing logic.
        /// </summary>
        internal async Task ProcessQueuedCommands(
            object? sender,
            ElapsedEventArgs e)
        {
            Timer timer = Timers.First(t => t.TimerName == "QueuedCommandCheck").TimerData;
            timer.Stop();

            (CommandModel? command, Exception? ex) = await _CommandService.GetCommand();

            if (command != null)
            {
                await _ApplicationService.ProcessCommand(command);
                await _CommandService.DeleteCommand(command.Id);
            }

            if (DoProcessQueuedCommands)
            {
                timer.Start();
            }
        }
    }
}
