// Copyright © - Unpublished - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    /// <summary>
    /// Stores the information about the server in the configuration file.
    /// </summary>
    public class DatabaseDetailsElement : ConfigurationElement
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }

        [ConfigurationProperty("pollingInterval", IsRequired = true)]
        public int PollingInterval
        {
            get { return (int)this["pollingInterval"]; }
            set { this["pollingInterval"] = value; }
        }
    }
}
