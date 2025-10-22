// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Converters
{
    public class TimeConverter
    {
        // Returns the time between now and when the timer should be triggered.
        public TimeSpan GetDuration(string triggerTime)
        {
            DateTime currentTime = DateTime.UtcNow;
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
                elapsedTime = $"{DateTime.UtcNow.AddDays(1):dd/MM/yyyy} {triggerTime}";
            }

            else
            {
                elapsedTime = $"{DateTime.UtcNow:dd/MM/yyyy} {triggerTime}";
            }

            return elapsedTime;
        }
    }
}