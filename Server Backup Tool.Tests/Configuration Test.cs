// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Tests.Functions;
using ServerBackupTool.Models.Configuration;
using System.Configuration;

namespace ServerBackupTool.Tests
{
    [TestClass]
    public class ConfigurationTest
    {
        /// <summary>
        /// Checks whether the full configuration file loads successfully.
        /// </summary>
        [TestMethod]
        public void ParseFullConfiguration()
        {
            SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            Assert.IsNotNull(serverBackupSection);
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingNameTag()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Name.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'name' not found."));
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingGameTag()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Game.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'game' not found."));
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingLocationTag()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Location.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'location' not found."));
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingStartFileTag()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Start File.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'startFile' not found."));
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingBackupTimeTag()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Backup Time.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'backupTime' not found."));
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingEnabledTag()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Notification Enabled.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'enabled' not found."));
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingFAEmailTag()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing From Address Email.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'email' not found."));
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingTAEmailTag()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing To Address Email.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'email' not found."));
        }

        /// <summary>
        /// Checks whether the configuration file fails to load if missing a tag.
        /// </summary>
        [TestMethod]
        public void ParseConfigurationMissingIPAddress()
        {
            ConfigurationErrorsException exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing IP Address.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'ipAddress' not found."));
        }
    }
}
