// Copyright © - unpublished - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    public class ToAddressElement : ConfigurationElement
    {
        [ConfigurationProperty("email")]
        public string Email
        {
            get { return (string)this["email"]; }
            set { this["email"] = value; }
        }

        [ConfigurationProperty("name")]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }
    }
}
