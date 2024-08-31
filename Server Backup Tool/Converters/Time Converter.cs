// Copyright © - unpublished - Toby Hunter
namespace ServerBackupTool.Converters
{
    internal class TimeConverter
    {
        public TimeSpan GetDuration(string triggerTime)
        {
            string currentTime = DateTime.Now.ToString();
            string elapsedTime = GetElapsedTime(DateTime.Now, DateTime.Parse(triggerTime), triggerTime);

            TimeSpan timerDuration = DateTime.Parse(elapsedTime).Subtract(DateTime.Parse(currentTime));

            return timerDuration;
        }

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
