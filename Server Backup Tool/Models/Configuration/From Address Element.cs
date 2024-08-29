// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    public class FromAddressElement : ConfigurationElement
    {
        [ConfigurationProperty("email")]
        public string Email
        {
            get { return (string)this["email"]; }
            set { this["email"] = value; }
        }

        [ConfigurationProperty("name", DefaultValue = "Server Backup Tool")]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }
    }
}
