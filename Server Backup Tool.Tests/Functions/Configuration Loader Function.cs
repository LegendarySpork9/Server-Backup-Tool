// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Configuration;

namespace ServerBackupTool.Tests.Functions
{
    internal static class ConfigurationLoaderFunction
    {
        public static SBTSection LoadConfig(string file)
        {
            ExeConfigurationFileMap configMap = new()
            {
                ExeConfigFilename = Path.Combine(DirectoryFunction.GetBaseDirectory(), "Mocks", "Configs", file)
            };

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

            return (SBTSection)config.GetSection("serverBackup");
        }
    }
}
