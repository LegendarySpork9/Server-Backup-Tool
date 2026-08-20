// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Converters;
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
        private readonly ApplicationService _ApplicationService;
        private readonly ServerService _ServerService;
        private readonly ILoggerService _Logger;
        private readonly SBTSection ServerBackupSection;
        private readonly bool DoHeartbeat = false;
        private readonly List<TimerModel> Timers = [];

        // Sets the class's global variables.
        public TimerService(
            ApplicationService _applicationService,
            ServerService _serverService,
            ILoggerService _logger,
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

            _ApplicationService = _applicationService;
            _ServerService = _serverService;
            _Logger = _logger;
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

                Timers[Timers.Count() - 1].TimerData.Interval = timerDurations[0].TotalMilliseconds;

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
                if (timer.TimerName != "Wait")
                {
                    timer.TimerData.Start();
                }
            }
        }

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
    }
}
