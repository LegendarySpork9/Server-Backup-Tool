// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    [ConfigurationCollection(typeof(ToAddressElement), AddItemName = "toAddress")]
    public class ToAddressCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ToAddressElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ToAddressElement)element).Email;
        }

        public ToAddressElement this[int idx]
        {
            get { return (ToAddressElement)BaseGet(idx); }
        }
    }
}
