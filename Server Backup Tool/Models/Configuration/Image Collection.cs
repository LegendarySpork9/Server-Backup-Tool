// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    [ConfigurationCollection(typeof(ImageElement), AddItemName = "image")]
    public class ImageCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ImageElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ImageElement)element).Key;
        }

        public ImageElement this[int idx]
        {
            get { return (ImageElement)BaseGet(idx); }
        }
    }
}
