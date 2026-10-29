// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Common.Abstractions;

namespace ServerBackupTool.Converters
{
    public class TimeConverter
    {
        private readonly IClock _Clock;

        // Sets the class's global variables.
        public TimeConverter(IClock _clock)
        {
            _Clock = _clock;
        }

        /// <summary>
        /// Returns the time between now and when the timer should be triggered.
        /// </summary>
        public TimeSpan GetDuration(string triggerTime)
        {
            DateTime currentTime = _Clock.UtcNow;
            TimeSpan parsedTime = TimeSpan.Parse(triggerTime);
            DateTime triggerDateTime = DateTime.SpecifyKind(
                currentTime.Date.Add(parsedTime),
                DateTimeKind.Utc);

            DateTime targetDateTime = GetElapsedTime(
                currentTime,
                triggerDateTime);

            TimeSpan timerDuration = targetDateTime.Subtract(currentTime);

            return timerDuration;
        }

        /// <summary>
        /// Returns the UTC date and time the timer should next trigger.
        /// </summary>
        private static DateTime GetElapsedTime(
            DateTime currentTime,
            DateTime triggerDateTime)
        {
            if (triggerDateTime <= currentTime)
            {
                triggerDateTime = triggerDateTime.AddDays(1);
            }

            return triggerDateTime;
        }
    }
}