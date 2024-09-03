// Copyright © - unpublished - Toby Hunter
using log4net;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using System.Net.NetworkInformation;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerBackupTool.Services
{
    internal class TimerService
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");
        readonly List<TimerModel> Timers = new();
        readonly SBTSection ServerBackupSection;
        readonly ServerService _ServerService;
        readonly bool DoHeartbeat = false;

        public TimerService (SBTSection _configurationSection, ServerService _serverService)
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

            ServerBackupSection = _configurationSection;
            _ServerService = _serverService;
        }

        public string SetTimers(TimerCollection timerDetails, TimeSpan[] timerDurations)
        {
            string result = "Completed";
            int timerNumber = 0;

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
                Console.WriteLine(ex.ToString());
                Log.Error(ex.ToString());
                result = "Errored";
            }

            return result;
        }

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

        private void SystemTimers(TimerModel timer)
        {
            timer.TimerData.Stop();

            Console.WriteLine("\n{0} Triggered", timer.TimerName);
            Log.Info($"{timer.TimerName} Triggered");

            timer.TimerData.Dispose();
            timer.Triggered = true;

            if (timer.TimerName == "Backup")
            {
                Program.TakeBackup(this);
            }

            else
            {
                Program.WaitForServerClose.Set();
            }
        }

        private void ServerWarning(TimerModel timer)
        {
            timer.TimerData.Stop();

            Console.WriteLine("\n{0} Triggered", timer.TimerName);
            Log.Info($"{timer.TimerName} Triggered");
            Log.Debug($"Warning Message: {timer.ElapsedMessage}");

            Console.WriteLine("\n----Server Commands----");
            Log.Info("----Server Commands----");

            timer.TimerData.Dispose();
            timer.Triggered = true;

            _ServerService.SendCommand(timer.ElapsedMessage, true);
        }

        private void Heartbeat(Timer heartbeatTimer)
        {
            EmailService _emailService = new();

            Ping pingSender = new();
            PingReply reply = pingSender.Send("25.35.45.248");

            if (reply.Status != IPStatus.Success)
            {
                heartbeatTimer.Stop();

                foreach (EmailElement email in ServerBackupSection.Notifications.Emails)
                {
                    if (email.Trigger == "Heartbeat")
                    {
                        _emailService.SendEmail(ServerBackupSection.Notifications, email);
                    }
                }
            }
        }
    }
}
