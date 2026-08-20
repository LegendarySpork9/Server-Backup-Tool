// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Configuration;

namespace ServerBackupTool.IntegrationTests.Tool.Helpers
{
    public static class ConfigurationLoaderHelper
    {
        /// <summary>
        /// Returns the SBTSection for the given configuration file.
        /// </summary>
        public static SBTSection? LoadConfig(string file)
        {
            ExeConfigurationFileMap configMap = new()
            {
                ExeConfigFilename = Path.Combine(
                    DirectoryHelper.GetBaseDirectory(),
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
