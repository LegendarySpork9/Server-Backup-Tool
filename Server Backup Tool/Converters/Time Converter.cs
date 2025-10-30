// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;

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

        // Returns the time between now and when the timer should be triggered.
        public TimeSpan GetDuration(string triggerTime)
        {
            DateTime currentTime = _Clock.UtcNow;
            string elapsedTime = GetElapsedTime(currentTime, DateTime.Parse(triggerTime).ToUniversalTime(), triggerTime);

            TimeSpan timerDuration = DateTime.Parse(elapsedTime).ToUniversalTime().Subtract(currentTime);

            return timerDuration;
        }

        // Returns the date and time the timer should be triggered.
        private string GetElapsedTime(DateTime currentTime, DateTime triggerDateTime, string triggerTime)
        {
            string? elapsedTime;

            if (triggerDateTime <= currentTime)
            {
                elapsedTime = $"{_Clock.UtcNow.AddDays(1):dd/MM/yyyy} {triggerTime}";
            }

            else
            {
                elapsedTime = $"{_Clock.UtcNow:dd/MM/yyyy} {triggerTime}";
            }

            return elapsedTime;
        }
    }
}