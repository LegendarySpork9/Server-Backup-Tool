// Copyright © - Unpublished - Toby Hunter
using Microsoft.Win32;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Values;
using System.Runtime.Versioning;

namespace ServerBackupTool.IntegrationTests.Installer.Services
{
    [TestClass]
    [SupportedOSPlatform("windows")]
    public class RegistryServiceTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private RegistryService _RegistryService = null!;
        private string _TestServerName = null!;
        private string _TestKeyPath = null!;

        /// <summary>
        /// Initialises the test dependencies and a unique server name per test run.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _RegistryService = new RegistryService(
                _MockLogger.Object,
                Registry.CurrentUser);

            _TestServerName = $"TestServer_{Guid.NewGuid():N}";
            _TestKeyPath = $@"{InstallerValues.Registry.UninstallKeyBase}\{InstallerValues.Registry.UninstallKeyPrefix}_{_TestServerName}";
        }

        /// <summary>
        /// Cleans up test registry keys.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(
                    _TestKeyPath,
                    false);
            }

            catch
            {

            }
        }

        /// <summary>
        /// Checks that WriteUninstallEntry writes all expected values to the registry.
        /// </summary>
        [TestMethod]
        public void WriteUninstallEntry_WritesAllValues()
        {
            _RegistryService.WriteUninstallEntry(
                _TestServerName,
                @"C:\Test\SBT",
                @"C:\Test\SBT\API",
                "1.0.0",
                "1.0.0",
                "SBT_Tool_Task",
                "SBT_Api_Task");

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_TestKeyPath);

            Assert.IsNotNull(key);
            Assert.AreEqual(
                $"{InstallerValues.Registry.DisplayName} - {_TestServerName}",
                key.GetValue("DisplayName"));
            Assert.AreEqual(
                "1.0.0",
                key.GetValue("ToolVersion"));
            Assert.AreEqual(
                "1.0.0",
                key.GetValue("ApiVersion"));
            Assert.AreEqual(
                InstallerValues.Registry.Publisher,
                key.GetValue("Publisher"));
            Assert.AreEqual(
                @"C:\Test\SBT",
                key.GetValue("InstallLocation"));
            Assert.AreEqual(
                @"C:\Test\SBT\API",
                key.GetValue("ApiInstallLocation"));
            Assert.AreEqual(
                "SBT_Tool_Task",
                key.GetValue("ToolTaskName"));
            Assert.AreEqual(
                "SBT_Api_Task",
                key.GetValue("ApiTaskName"));
            Assert.AreEqual(
                _TestServerName,
                key.GetValue("ServerName"));
        }

        /// <summary>
        /// Checks that WriteUninstallEntry returns true on success.
        /// </summary>
        [TestMethod]
        public void WriteUninstallEntry_ReturnsTrue_OnSuccess()
        {
            (bool success, Exception? error) = _RegistryService.WriteUninstallEntry(
                _TestServerName,
                @"C:\Test\SBT",
                @"C:\Test\SBT\API",
                "1.0.0",
                "1.0.0",
                "SBT_Tool_Task",
                "SBT_Api_Task");

            Assert.IsTrue(success);
            Assert.IsNull(error);
        }

        /// <summary>
        /// Checks that ReadUninstallEntry returns null when the key does not exist.
        /// </summary>
        [TestMethod]
        public void ReadUninstallEntry_ReturnsNull_WhenKeyDoesNotExist()
        {
            VersionInfoModel? result = _RegistryService.ReadUninstallEntry(_TestServerName);

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks that ReadUninstallEntry returns version info when the key exists.
        /// </summary>
        [TestMethod]
        public void ReadUninstallEntry_ReturnsVersionInfo_WhenKeyExists()
        {
            _RegistryService.WriteUninstallEntry(
                _TestServerName,
                @"C:\Test\SBT",
                @"C:\Test\SBT\API",
                "2.0.0",
                "1.5.0",
                "SBT_Tool_Task",
                "SBT_Api_Task");

            VersionInfoModel? result = _RegistryService.ReadUninstallEntry(_TestServerName);

            Assert.IsNotNull(result);
            Assert.AreEqual(
                _TestServerName,
                result.ServerName);
            Assert.AreEqual(
                "2.0.0",
                result.ToolVersion);
            Assert.AreEqual(
                "1.5.0",
                result.ApiVersion);
            Assert.AreEqual(
                @"C:\Test\SBT",
                result.InstallPath);
            Assert.AreEqual(
                @"C:\Test\SBT\API",
                result.ApiInstallPath);
        }

        /// <summary>
        /// Checks that RemoveUninstallEntry deletes the registry key.
        /// </summary>
        [TestMethod]
        public void RemoveUninstallEntry_DeletesKey()
        {
            _RegistryService.WriteUninstallEntry(
                _TestServerName,
                @"C:\Test\SBT",
                @"C:\Test\SBT\API",
                "1.0.0",
                "1.0.0",
                "SBT_Tool_Task",
                "SBT_Api_Task");

            (bool removed, Exception? error) = _RegistryService.RemoveUninstallEntry(_TestServerName);

            Assert.IsTrue(removed);
            Assert.IsNull(error);

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_TestKeyPath);

            Assert.IsNull(key);
        }

        /// <summary>
        /// Checks that RemoveUninstallEntry returns true when the key does not exist.
        /// </summary>
        [TestMethod]
        public void RemoveUninstallEntry_ReturnsTrue_WhenKeyDoesNotExist()
        {
            (bool removed, Exception? error) = _RegistryService.RemoveUninstallEntry(_TestServerName);

            Assert.IsTrue(removed);
            Assert.IsNull(error);
        }

        /// <summary>
        /// Checks that GetAllInstallations returns an empty list when no installations exist.
        /// </summary>
        [TestMethod]
        public void GetAllInstallations_ReturnsEmpty_WhenNoInstallations()
        {
            List<VersionInfoModel> installations = _RegistryService.GetAllInstallations();

            int matchCount = installations.Count(i => i.ServerName == _TestServerName);

            Assert.AreEqual(
                0,
                matchCount);
        }

        /// <summary>
        /// Checks that GetAllInstallations returns all installations.
        /// </summary>
        [TestMethod]
        public void GetAllInstallations_ReturnsAllInstallations()
        {
            string secondServerName = $"TestServer_{Guid.NewGuid():N}";
            string secondKeyPath = $@"{InstallerValues.Registry.UninstallKeyBase}\{InstallerValues.Registry.UninstallKeyPrefix}_{secondServerName}";

            try
            {
                _RegistryService.WriteUninstallEntry(
                    _TestServerName,
                    @"C:\Test\SBT1",
                    @"C:\Test\SBT1\API",
                    "1.0.0",
                    "1.0.0",
                    "SBT_Tool_Task_1",
                    "SBT_Api_Task_1");

                _RegistryService.WriteUninstallEntry(
                    secondServerName,
                    @"C:\Test\SBT2",
                    @"C:\Test\SBT2\API",
                    "2.0.0",
                    "2.0.0",
                    "SBT_Tool_Task_2",
                    "SBT_Api_Task_2");

                List<VersionInfoModel> installations = _RegistryService.GetAllInstallations();

                int matchCount = installations.Count(i => i.ServerName == _TestServerName || i.ServerName == secondServerName);

                Assert.AreEqual(
                    2,
                    matchCount);
            }

            finally
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(
                        secondKeyPath,
                        false);
                }

                catch
                {

                }
            }
        }
    }
}
