// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Tests.Functions;
using ServerBackupTool.Models.Configuration;
using System.Configuration;

namespace ServerBackupTool.Tests
{
    [TestClass]
    public class ConfigurationTest
    {
        // Checks whether the full configuration file loads successfully.
        [TestMethod]
        public void ParseFullConfiguration()
        {
            SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            Assert.IsNotNull(serverBackupSection);
        }

        // Checks whether the configuration file fails to load if missing a tag.
        [TestMethod]
        public void ParseConfigurationMissingGameTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Game.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'game' not found."));
        }

        // Checks whether the configuration file fails to load if missing a tag.
        [TestMethod]
        public void ParseConfigurationMissingLocationTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Location.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'location' not found."));
        }

        // Checks whether the configuration file fails to load if missing a tag.
        [TestMethod]
        public void ParseConfigurationMissingStartFileTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Start File.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'startFile' not found."));
        }

        // Checks whether the configuration file fails to load if missing a tag.
        [TestMethod]
        public void ParseConfigurationMissingBackupTimeTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Backup Time.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'backupTime' not found."));
        }

        // Checks whether the configuration file fails to load if missing a tag.
        [TestMethod]
        public void ParseConfigurationMissingEnabledTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Notification Enabled.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'enabled' not found."));
        }

        // Checks whether the configuration file fails to load if missing a tag.
        [TestMethod]
        public void ParseConfigurationMissingFAEmailTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing From Address Email.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'email' not found."));
        }

        // Checks whether the configuration file fails to load if missing a tag.
        [TestMethod]
        public void ParseConfigurationMissingTAEmailTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing To Address Email.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'email' not found."));
        }

        // Checks whether the configuration file fails to load if missing a tag.
        [TestMethod]
        public void ParseConfigurationMissingIPAddress()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection? serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing IP Address.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'ipAddress' not found."));
        }
    }
}
