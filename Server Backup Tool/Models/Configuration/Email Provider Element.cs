// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    public class EmailProviderElement : ConfigurationElement
    {
        [ConfigurationProperty("name")]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("password")]
        public string Password
        {
            get { return (string)this["password"]; }
            set { this["password"] = value; }
        }
    }
}
