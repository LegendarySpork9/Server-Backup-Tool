// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    public class TimerDetailsElement : ConfigurationElement
    {
        [ConfigurationProperty("count", DefaultValue = 1)]
        public int Count
        {
            get { return (int)this["count"]; }
            set { this["count"] = value; }
        }

        [ConfigurationProperty("timeZone", DefaultValue = "(UTC+00:00) Dublin, Edinburgh, Lisbon, London")]
        public string TimeZone
        {
            get { return (string)this["timeZone"]; }
            set { this["timeZone"] = value; }
        }

        [ConfigurationProperty("backupTime", IsRequired = true)]
        public string BackupTime
        {
            get { return (string)this["backupTime"]; }
            set { this["backupTime"] = value; }
        }

        [ConfigurationProperty("timers")]
        public TimerCollection Timers
        {
            get { return (TimerCollection)this["timers"]; }
            set { this["timers"] = value; }
        }
    }
}
