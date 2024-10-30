// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using Timer = System.Timers.Timer;

namespace ServerBackupTool.Tests.Functions
{
    internal static class TimerFunction
    {
        public static List<Mock<TimerModel>> ConfigureTimers(SBTSection serverBackupSection, bool doHeartbeat = false)
        {
            List<Mock<TimerModel>> timers = new();
            TimeSpan[] timerDurations = TimerFunction.GetDurations(serverBackupSection.TimerDetails);
            int timerNumber = 0;

            try
            {
                for (int x = 0; x < SystemTimerModel.Names.Length; x++)
                {
                    if (SystemTimerModel.Names[x] == "Heartbeat" && !doHeartbeat)
                    {
                        continue;
                    }

                    Timer timerData = new()
                    {
                        Interval = SystemTimerModel.Durations[x]
                    };
                    int currentTimerNumber = timerNumber;
                    timerNumber++;

                    Mock<TimerModel> timer = new();
                    timer.Object.TimerName = SystemTimerModel.Names[x];
                    timer.Object.TimerData = timerData;

                    timers.Add(timer);
                }

                timers[timers.Count() - 1].Object.TimerData.Interval = timerDurations[0].TotalMilliseconds;

                for (int x = 0; x < serverBackupSection.TimerDetails.Timers.Count; x++)
                {
                    Timer timerData = new()
                    {
                        Interval = timerDurations[x + 1].TotalMilliseconds,
                    };
                    int currentTimerNumber = timerNumber;
                    timerNumber++;

                    Mock<TimerModel> timer = new();
                    timer.Object.TimerName = serverBackupSection.TimerDetails.Timers[x].Name;
                    timer.Object.ElapsedMessage = serverBackupSection.TimerDetails.Timers[x].Message;
                    timer.Object.TimerData = timerData;

                    timers.Add(timer);
                }
            }

            catch (Exception ex)
            {

            }

            return timers;
        }

        private static TimeSpan[] GetDurations(TimerDetailsElement timerDetails)
        {
            TimeSpan[] durations = Array.Empty<TimeSpan>();

            string currentTime = DateTime.Now.ToString();
            string elapsedTime = GetElapsedTime(DateTime.Now, DateTime.Parse(timerDetails.BackupTime), timerDetails.BackupTime);

            durations = durations.Append(DateTime.Parse(elapsedTime).Subtract(DateTime.Parse(currentTime))).ToArray();

            foreach (TimerElement timer in timerDetails.Timers)
            {
                currentTime = DateTime.Now.ToString();
                elapsedTime = GetElapsedTime(DateTime.Now, DateTime.Parse(timer.Time), timer.Time);

                durations = durations.Append(DateTime.Parse(elapsedTime).Subtract(DateTime.Parse(currentTime))).ToArray();
            }

            return durations;
        }

        private static string GetElapsedTime(DateTime currentTime, DateTime triggerDateTime, string triggerTime)
        {
            string? elapsedTime;

            if (triggerDateTime <= currentTime)
            {
                elapsedTime = $"{DateTime.Now.AddDays(1):dd/MM/yyyy} {triggerTime}";
            }

            else
            {
                elapsedTime = $"{DateTime.Now:dd/MM/yyyy} {triggerTime}";
            }

            return elapsedTime;
        }
    }
}
