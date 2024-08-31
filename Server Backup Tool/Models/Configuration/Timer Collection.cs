// Copyright © - unpublished - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    [ConfigurationCollection(typeof(TimerElement), AddItemName = "timer")]
    public class TimerCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new TimerElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TimerElement)element).Name;
        }

        public TimerElement this[int idx]
        {
            get { return (TimerElement)BaseGet(idx); }
        }
    }
}
