// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    /// <summary>
    /// Stores the information about the server in the configuration file.
    /// </summary>
    public class ServerDetailsElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

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
