// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    // Stores a list of emails from the configuration file.
    [ConfigurationCollection(typeof(EmailElement), AddItemName = "email")]
    public class EmailCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new EmailElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((EmailElement)element).Trigger;
        }

        public EmailElement this[int idx]
        {
            get { return (EmailElement)BaseGet(idx); }
        }
    }
}
