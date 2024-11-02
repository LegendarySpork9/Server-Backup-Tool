// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    public class SBTSection : ConfigurationSection
    {
        [ConfigurationProperty("serverDetails")]
        public ServerDetailsElement ServerDetails
        {
            get { return (ServerDetailsElement)this["serverDetails"]; }
            set { this["serverDetails"] = value; }
        }

        [ConfigurationProperty("timerDetails")]
        public TimerDetailsElement TimerDetails
        {
            get { return (TimerDetailsElement)this["timerDetails"]; }
            set { this["timerDetails"] = value; }
        }

        [ConfigurationProperty("notifications")]
        public NotificationElement Notifications
        {
            get { return (NotificationElement)this["notifications"]; }
            set { this["notifications"] = value; }
        }
    }
}
