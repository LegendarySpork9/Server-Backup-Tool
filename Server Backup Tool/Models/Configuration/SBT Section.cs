// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    /// <summary>
    /// Stores all the information in the configuration file.
    /// </summary>
    public class SBTSection : ConfigurationSection
    {
        [ConfigurationProperty("serverDetails")]
        public ServerDetailsElement ServerDetails
        {
            get { return (ServerDetailsElement)this["serverDetails"]; }
            set { this["serverDetails"] = value; }
        }

        [ConfigurationProperty("databaseDetails")]
        public DatabaseDetailsElement DatabaseDetails
        {
            get { return (DatabaseDetailsElement)this["databaseDetails"]; }
            set { this["databaseDetails"] = value; }
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
