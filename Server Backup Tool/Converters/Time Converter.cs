// Copyright © - 31/10/2024 - Toby Hunter
using System.Globalization;

namespace ServerBackupTool.Converters
{
    public class TimeConverter
    {
        readonly TimeZoneInfo TimeZone = TimeZoneInfo.Local;

        // Sets the class's global variables.
        public TimeConverter(string configurationTimeZone)
        {
            foreach (TimeZoneInfo timeZone in TimeZoneInfo.GetSystemTimeZones())
            {
                if (timeZone.DisplayName == configurationTimeZone)
                {
                    TimeZone = timeZone;
                }
            }
        }

        // Returns the time between now and when the timer should be triggered.
        public TimeSpan GetDuration(string triggerTime)
        {
            string currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZone).ToString();
            string elapsedTime = GetElapsedTime(TimeZoneInfo.ConvertTime(DateTime.Now, TimeZone), TimeZoneInfo.ConvertTime(DateTime.Parse(triggerTime), TimeZone), triggerTime);

            TimeSpan timerDuration = DateTime.Parse(elapsedTime).Subtract(DateTime.Parse(currentTime));

            return timerDuration;
        }

        // Returns the date and time the timer should be triggered.
        private string GetElapsedTime(DateTime currentTime, DateTime triggerDateTime, string triggerTime)
        {
            string? elapsedTime;

            if (triggerDateTime <= currentTime)
            {
                elapsedTime = $"{TimeZoneInfo.ConvertTime(DateTime.Now.AddDays(1), TimeZone):dd/MM/yyyy} {triggerTime}";
            }

            else
            {
                elapsedTime = $"{TimeZoneInfo.ConvertTime(DateTime.Now, TimeZone):dd/MM/yyyy} {triggerTime}";
            }

            return elapsedTime;
        }
    }
}