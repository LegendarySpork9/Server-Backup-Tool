// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    // Stores the information about a timer in the configuration file.
    public class TimerElement : ConfigurationElement
    {
        [ConfigurationProperty("name")]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("time")]
        public string Time
        {
            get { return (string)this["time"]; }
            set { this["time"] = value; }
        }

        [ConfigurationProperty("message")]
        public string Message
        {
            get { return (string)this["message"]; }
            set { this["message"] = value; }
        }
    }
}
