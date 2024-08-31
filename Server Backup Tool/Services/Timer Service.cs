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
        readonly List<TimerModel> timers = new();

        public string SetTimers(TimerCollection timerDetails, TimeSpan[] timerDurations)
        {
            string result = "Completed";
            int timerNumber = 0;

            try
            {
                for (int x = 0; x < SystemTimerModel.Names.Length; x++)
                {
                    Timer timerData = new()
                    {
                        Interval = SystemTimerModel.Durations[x]
                    };
                    int currentTimerNumber = timerNumber;
                    timerData.Elapsed += (sender, e) => TimerElapsed(sender, e, currentTimerNumber);
                    timerNumber++;

                    timers.Add(new TimerModel
                    {
                        TimerName = SystemTimerModel.Names[x],
                        TimerData = timerData
                    });
                }

                timers[2].TimerData.Interval = timerDurations[0].TotalMilliseconds;

                for (int x = 0; x < timerDetails.Count; x++)
                {
                    Timer timerData = new()
                    {
                        Interval = timerDurations[x + 1].TotalMilliseconds,
                    };
                    int currentTimerNumber = timerNumber;
                    timerData.Elapsed += (sender, e) => TimerElapsed(sender, e, currentTimerNumber);
                    timerNumber++;

                    timers.Add(new TimerModel
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
            foreach (TimerModel timer in timers)
            {
                if (timer.TimerName != "Wait")
                {
                    timer.TimerData.Start();
                }
            }
        }

        public void WaitForClose()
        {
            foreach (TimerModel timer in timers)
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
                    Heartbeat(timers[0].TimerData);
                    break;
                case 1:
                    SystemTimers(timers[1]);
                    break;
                case 2:
                    SystemTimers(timers[2]);
                    break;
                default:
                    ServerWarning(timers[timerNumber]);
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
            Server.SendWarning(timer.ElapsedMessage);
        }

        private void Heartbeat(Timer heartbeatTimer)
        {
            Ping pingSender = new();
            PingReply reply = pingSender.Send("25.35.45.248");

            if (reply.Status != IPStatus.Success)
            {
                heartbeatTimer.Stop();
                Email.ConnectionEmail();
            }
        }
    }
}
