// Copyright © - 31/10/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    // Stores a list of images from the configuration file.
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
