// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
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
