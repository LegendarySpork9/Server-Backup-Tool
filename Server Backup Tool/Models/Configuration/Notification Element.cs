// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    public class NotificationElement : ConfigurationElement
    {
        [ConfigurationProperty("enabled", IsRequired = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        [ConfigurationProperty("port", DefaultValue = 587)]
        public int Port
        {
            get { return (int)this["port"]; }
            set { this["port"] = value; }
        }

        [ConfigurationProperty("enableSSL", DefaultValue = true)]
        public bool EnableSSL
        {
            get { return (bool)this["enableSSL"]; }
            set { this["enableSSL"] = value; }
        }

        [ConfigurationProperty("provider")]
        public EmailProviderElement Provider
        {
            get { return (EmailProviderElement)this["provider"]; }
            set { this["provider"] = value; }
        }

        [ConfigurationProperty("fromAddress")]
        public FromAddressElement FromAddress
        {
            get { return (FromAddressElement)this["fromAddress"]; }
            set { this["fromAddress"] = value; }
        }

        [ConfigurationProperty("emails")]
        public EmailCollection Emails
        {
            get { return (EmailCollection)this["emails"]; }
            set { this["emails"] = value; }
        }
    }
}
