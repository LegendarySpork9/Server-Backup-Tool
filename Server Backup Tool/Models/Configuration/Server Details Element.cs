// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
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
    }
}
