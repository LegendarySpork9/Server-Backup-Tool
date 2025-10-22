// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Configuration;

namespace ServerBackupTool.Tests.Functions
{
    internal static class ConfigurationLoaderFunction
    {
        // Returns the SBTSection for the given configuration file.
        public static SBTSection LoadConfig(string file)
        {
            ExeConfigurationFileMap configMap = new()
            {
                ExeConfigFilename = Path.Combine(DirectoryFunction.GetBaseDirectory(), @"Mocks\Configs", file)
            };

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

            return (SBTSection)config.GetSection("serverBackup");
        }
    }
}
