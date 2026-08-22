// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using System.Net.NetworkInformation;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerBackupTool.Services
{
    public class TimerService
    {
        private readonly ILoggerService _Logger;
        private readonly ApplicationService _ApplicationService;
        private readonly ServerService _ServerService;
        private readonly CommandService _CommandService;
        private readonly SBTSection ServerBackupSection;
        private readonly bool DoHeartbeat = false;
        private readonly List<TimerModel> Timers = [];
        private bool DoProcessQueuedCommands = false;

        // Sets the class's global variables.
        public TimerService(
            ILoggerService _logger,
            ApplicationService _applicationService,
            ServerService _serverService,
            CommandService _commandService,
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
                    await Heartbeat(Timers[0].TimerData);
                    break;
                case 1:
                    SystemTimers(Timers[1]);
                    break;
                case 2:
                    SystemTimers(Timers[2]);
                    break;
                default:
                    ServerWarning(Timers[timerNumber]);
                    break;
            }
        }

        /// <summary>
        /// Runs code related to built in timers.
        /// </summary>
        private async void SystemTimers(TimerModel timer)
        {
            timer.TimerData.Stop();

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                $"{timer.TimerName} Triggered");

            timer.TimerData.Dispose();
            timer.Triggered = true;

            if (timer.TimerName == "Backup")
            {
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
        private async void ServerWarning(TimerModel timer)
        {
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
        /// Runs when the Heartbeat timer finishes.
        /// </summary>
        private async Task Heartbeat(Timer heartbeatTimer)
        {
            EmailService _emailService = new(
                _Logger,
                new SMTPEmailSender(),
                new ExtendedFileSystemWrapper(),
                true);

            Ping pingSender = new();
            PingReply reply = await pingSender.SendPingAsync(ServerBackupSection.ServerDetails.IPAddress);

            if (reply.Status != IPStatus.Success)
            {
                heartbeatTimer.Stop();

                await _emailService.CheckForEmail(
                    ServerBackupSection.Notifications,
                    "Heartbeat");
            }
        }

        /// <summary>
        /// Runs when the QueuedCommandCheck timer has finished.
        /// </summary>
        private async Task ProcessQueuedCommands(
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

                if (DoProcessQueuedCommands)
                {
                    timer.Start();
                }
            }
        }
    }
}
