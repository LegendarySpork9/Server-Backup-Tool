// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Configuration;

namespace ServerBackupTool.Tests.Tool.Functions
{
    public static class ConfigurationLoaderFunction
    {
        /// <summary>
        /// Returns the SBTSection for the given configuration file.
        /// </summary>
        public static SBTSection? LoadConfig(string file)
        {
            ExeConfigurationFileMap configMap = new()
            {
                ExeConfigFilename = Path.Combine(
                    DirectoryFunction.GetBaseDirectory(),
                    @"Tool\Mocks\Configs",
                    file)
            };

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(
                configMap,
                ConfigurationUserLevel.None);

            return config.GetSection("serverBackup") as SBTSection;
        }
    }
}
