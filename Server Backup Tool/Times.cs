// Copyright © - 17/01/2024 - Toby Hunter
namespace ServerBackupTool
{
    internal class Times
    {
        public static TimeSpan GetTimer()
        {
            string CurrentTime = DateTime.Now.ToString();
            string DesiredTime = $"{DateTime.Now.AddDays(1):dd/MM/yyyy} 02:00:00";
            //string DesiredTime = $"{DateTime.Now.AddMinutes(4)}";

            TimeSpan Duration = DateTime.Parse(DesiredTime).Subtract(DateTime.Parse(CurrentTime));

            return Duration;
        }

        public static TimeSpan GetWarningOne()
        {
            string CurrentTime = DateTime.Now.ToString();
            string DesiredTime = $"{DateTime.Now.AddDays(1):dd/MM/yyyy} 01:00:00";
            //string DesiredTime = $"{DateTime.Now.AddMinutes(1)}";

            TimeSpan Duration = DateTime.Parse(DesiredTime).Subtract(DateTime.Parse(CurrentTime));

            return Duration;
        }

        public static TimeSpan GetWarningTwo()
        {
            string CurrentTime = DateTime.Now.ToString();
            string DesiredTime = $"{DateTime.Now.AddDays(1):dd/MM/yyyy} 01:30:00";
            //string DesiredTime = $"{DateTime.Now.AddMinutes(2)}";

            TimeSpan Duration = DateTime.Parse(DesiredTime).Subtract(DateTime.Parse(CurrentTime));

            return Duration;
        }

        public static TimeSpan GetWarningThree()
        {
            string CurrentTime = DateTime.Now.ToString();
            string DesiredTime = $"{DateTime.Now.AddDays(1):dd/MM/yyyy} 01:55:00";
            //string DesiredTime = $"{DateTime.Now.AddMinutes(3)}";

            TimeSpan Duration = DateTime.Parse(DesiredTime).Subtract(DateTime.Parse(CurrentTime));

            return Duration;
        }
    }
}
