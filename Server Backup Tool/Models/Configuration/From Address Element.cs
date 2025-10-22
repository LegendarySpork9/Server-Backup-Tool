// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    // Stores the information about the email sender.
    public class FromAddressElement : ConfigurationElement
    {
        [ConfigurationProperty("email", IsRequired = true)]
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
