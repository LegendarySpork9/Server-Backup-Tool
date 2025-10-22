// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    // Stores the information about an email in the configuration file.
    public class EmailElement : ConfigurationElement
    {
        [ConfigurationProperty("trigger")]
        public string Trigger
        {
            get { return (string)this["trigger"]; }
            set { this["trigger"] = value; }
        }

        [ConfigurationProperty("system", DefaultValue = false)]
        public bool SystemEmail
        {
            get { return (bool)this["system"]; }
            set { this["system"] = value; }
        }

        [ConfigurationProperty("addresses")]
        public ToAddressCollection Addresses
        {
            get { return (ToAddressCollection)this["addresses"]; }
            set { this["addresses"] = value; }
        }

        [ConfigurationProperty("subject")]
        public SubjectElement Subject
        {
            get { return (SubjectElement)this["subject"]; }
            set { this["subject"] = value; }
        }

        [ConfigurationProperty("content")]
        public ContentElement Content
        {
            get { return (ContentElement)this["content"]; }
            set { this["content"] = value; }
        }

        [ConfigurationProperty("images")]
        public ImageCollection Images
        {
            get { return (ImageCollection)this["images"]; }
            set { this["images"] = value; }
        }
    }
}
