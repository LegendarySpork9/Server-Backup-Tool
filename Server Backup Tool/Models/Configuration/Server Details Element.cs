// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    // Stores the information about the server in the configuration file.
    public class ServerDetailsElement : ConfigurationElement
    {
        [ConfigurationProperty("game", IsRequired = true)]
        public string Game
        {
            get { return (string)this["game"]; }
            set { this["game"] = value; }
        }

        [ConfigurationProperty("location", IsRequired = true)]
        public string Location
        {
            get { return (string)this["location"]; }
            set { this["location"] = value; }
        }

        [ConfigurationProperty("startFile", IsRequired = true)]
        public string StartFile
        {
            get { return (string)this["startFile"]; }
            set { this["startFile"] = value; }
        }

        [ConfigurationProperty("ipAddress", IsRequired = true)]
        public string IPAddress
        {
            get { return (string)this["ipAddress"]; }
            set { this["ipAddress"] = value; }
        }
    }
}
