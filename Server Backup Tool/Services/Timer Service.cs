// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using System.Net.NetworkInformation;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerBackupTool.Services
{
    internal class TimerService
    {
        readonly ApplicationService _ApplicationService;
        readonly ServerService _ServerService;
        readonly SBTSection ServerBackupSection;
        readonly bool DoHeartbeat = false;
        readonly List<TimerModel> Timers = new();

        // Sets the class's global variables.
        public TimerService (ApplicationService _applicationService, ServerService _serverService, SBTSection _configurationSection)
        {
            if (_configurationSection.Notifications.Emails.Count != 0)
            {
                foreach (EmailElement email in _configurationSection.Notifications.Emails)
                {
                    if (email.Trigger == "Heartbeat")
                    {
                        DoHeartbeat = true;
                    }
                }
            }

            _ApplicationService = _applicationService;
            _ServerService = _serverService;
            ServerBackupSection = _configurationSection;
        }

        // Configures the timers.
        public string SetTimers(TimerCollection timerDetails, TimeSpan[] timerDurations)
        {
            LoggerService _logger = new();

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
                    timerData.Elapsed += (sender, e) => TimerElapsed(sender, e, currentTimerNumber);
                    timerNumber++;

                    Timers.Add(new TimerModel
                    {
                        TimerName = SystemTimerModel.Names[x],
                        TimerData = timerData
                    });
                }

                Timers[2].TimerData.Interval = timerDurations[0].TotalMilliseconds;

                for (int x = 0; x < timerDetails.Count; x++)
                {
                    Timer timerData = new()
                    {
                        Interval = timerDurations[x + 1].TotalMilliseconds,
                    };
                    int currentTimerNumber = timerNumber;
                    timerData.Elapsed += (sender, e) => TimerElapsed(sender, e, currentTimerNumber);
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
                _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to set up the timers.");
                _logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Errored";
            }

            return result;
        }

        // Activates the timers.
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

        // Activates the server closing delay timer.
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

        // Runs when a timer has finished.
        private void TimerElapsed(object sender, ElapsedEventArgs e, int timerNumber)
        {
            switch (timerNumber)
            {
                case 0:
                    Heartbeat(Timers[0].TimerData);
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

        // Runs code related to built in timers.
        private void SystemTimers(TimerModel timer)
        {
            LoggerService _logger = new();

            timer.TimerData.Stop();

            _logger.LogToolMessage(StandardValues.LoggerValues.Info, $"{timer.TimerName} Triggered");

            timer.TimerData.Dispose();
            timer.Triggered = true;

            if (timer.TimerName == "Backup")
            {
                _ApplicationService.RunBackup(this);
            }

            else
            {
                ApplicationService.WaitForServerClose.Set();
            }
        }

        // Runs code related to the server timers.
        private void ServerWarning(TimerModel timer)
        {
            LoggerService _logger = new();

            timer.TimerData.Stop();

            _logger.LogToolMessage(StandardValues.LoggerValues.Info, $"{timer.TimerName} Triggered");
            _logger.LogToolMessage(StandardValues.LoggerValues.Debug, $"Warning Message: {timer.ElapsedMessage}");

            Console.WriteLine("\n----Server Commands----");

            timer.TimerData.Dispose();
            timer.Triggered = true;

            _ServerService.SendCommand(timer.ElapsedMessage, true);
        }

        // Runs when the Heartbeat timer finishes.
        private void Heartbeat(Timer heartbeatTimer)
        {
            EmailService _emailService = new();

            Ping pingSender = new();
            PingReply reply = pingSender.Send("25.35.45.248");

            if (reply.Status != IPStatus.Success)
            {
                heartbeatTimer.Stop();

                _emailService.CheckForEmail(ServerBackupSection.Notifications, "Heartbeat");
            }
        }
    }
}
