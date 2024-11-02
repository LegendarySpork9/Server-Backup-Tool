// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Converters
{
    public class TimeConverter
    {
        // Returns the time between now and when the timer should be triggered.
        public TimeSpan GetDuration(string triggerTime)
        {
            string currentTime = DateTime.Now.ToString();
            string elapsedTime = GetElapsedTime(DateTime.Now, DateTime.Parse(triggerTime), triggerTime);

            TimeSpan timerDuration = DateTime.Parse(elapsedTime).Subtract(DateTime.Parse(currentTime));

            return timerDuration;
        }

        // Returns the date and time the timer should be triggered.
        private string GetElapsedTime(DateTime currentTime, DateTime triggerDateTime, string triggerTime)
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