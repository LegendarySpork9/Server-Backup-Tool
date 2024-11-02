// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Tests.Functions;
using ServerBackupTool.Models.Configuration;
using System.Configuration;

namespace ServerBackupTool.Tests
{
    [TestClass]
    public class ConfigurationTest
    {
        [TestMethod]
        public void ParseFullConfiguration()
        {
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            Assert.IsNotNull(serverBackupSection);
        }

        [TestMethod]
        public void ParseConfigurationMissingGameTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Game.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'game' not found."));
        }

        [TestMethod]
        public void ParseConfigurationMissingLocationTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Location.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'location' not found."));
        }

        [TestMethod]
        public void ParseConfigurationMissingStartFileTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Start File.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'startFile' not found."));
        }

        [TestMethod]
        public void ParseConfigurationMissingBackupTimeTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Backup Time.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'backupTime' not found."));
        }

        [TestMethod]
        public void ParseConfigurationMissingEnabledTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing Notification Enabled.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'enabled' not found."));
        }

        [TestMethod]
        public void ParseConfigurationMissingFAEmailTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing From Address Email.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'email' not found."));
        }

        [TestMethod]
        public void ParseConfigurationMissingTAEmailTag()
        {
            var exception = Assert.ThrowsException<ConfigurationErrorsException>(() =>
            {
                SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Configuration Missing To Address Email.config");
            });

            Assert.IsTrue(exception.Message.Contains("Required attribute 'email' not found."));
        }
    }
}
