// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool.Models.Configuration
{
    public class SubjectElement : ConfigurationElement
    {
        [ConfigurationProperty("value")]
        public string Value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }
    }
}
