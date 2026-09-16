// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Implementations;
using ServerBackupTool.Installer.Models;

namespace ServerBackupTool.IntegrationTests.Installer.Services
{
    [TestClass]
    public class VersionServiceTest
    {
        private Mock<ILoggerService> _MockLogger = null!;
        private Mock<IRegistryService> _MockRegistry = null!;
        private Mock<IResourceService> _MockResourceService = null!;
        private Mock<IFileSystem> _MockFileSystem = null!;
        private VersionService _VersionService = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
            _MockRegistry = new Mock<IRegistryService>();
            _MockResourceService = new Mock<IResourceService>();
            _MockFileSystem = new Mock<IFileSystem>();
            _VersionService = new VersionService(
                _MockLogger.Object,
                _MockRegistry.Object,
                _MockResourceService.Object,
                _MockFileSystem.Object);
        }

        /// <summary>
        /// Checks that GetInstalledVersion returns null when no version is installed.
        /// </summary>
        [TestMethod]
        public void GetInstalledVersion_ReturnsNull_WhenNotInstalled()
        {
            _MockRegistry.Setup(r => r.ReadUninstallEntry("TestServer"))
                .Returns((VersionInfoModel?)null);

            VersionInfoModel? result = _VersionService.GetInstalledVersion("TestServer");

            Assert.IsNull(result);
        }

        /// <summary>
        /// Checks that GetInstalledVersion returns a full VersionInfoModel when installed.
        /// </summary>
        [TestMethod]
        public void GetInstalledVersion_ReturnsVersionInfo_WhenInstalled()
        {
            _MockRegistry.Setup(r => r.ReadUninstallEntry("TestServer"))
                .Returns(new VersionInfoModel
                {
                    ServerName = "TestServer",
                    ToolVersion = "1.0.0",
                    ApiVersion = "1.0.0",
                    InstallPath = @"C:\Test\SBT",
                    ToolTaskName = "Server Backup Tool - TestServer",
                    ApiTaskName = "Server Backup Tool API - TestServer"
                });

            VersionInfoModel? result = _VersionService.GetInstalledVersion("TestServer");

            Assert.IsNotNull(result);
            Assert.AreEqual(
                "TestServer",
                result.ServerName);
            Assert.AreEqual(
                "1.0.0",
                result.ToolVersion);
            Assert.AreEqual(
                "1.0.0",
                result.ApiVersion);
            Assert.AreEqual(
                @"C:\Test\SBT",
                result.InstallPath);
            Assert.AreEqual(
                "Server Backup Tool - TestServer",
                result.ToolTaskName);
            Assert.AreEqual(
                "Server Backup Tool API - TestServer",
                result.ApiTaskName);
        }

        /// <summary>
        /// Checks that IsToolUpdateAvailable returns true when the bundled version is higher.
        /// </summary>
        [TestMethod]
        public void IsToolUpdateAvailable_ReturnsTrue_WhenBundledVersionHigher()
        {
            bool result = _VersionService.IsToolUpdateAvailable(
                "0.9.0",
                "1.0.0");

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Checks that IsToolUpdateAvailable returns false when the versions match.
        /// </summary>
        [TestMethod]
        public void IsToolUpdateAvailable_ReturnsFalse_WhenVersionsMatch()
        {
            bool result = _VersionService.IsToolUpdateAvailable(
                "1.0.0",
                "1.0.0");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks that IsToolUpdateAvailable returns false when the installed version is higher.
        /// </summary>
        [TestMethod]
        public void IsToolUpdateAvailable_ReturnsFalse_WhenInstalledVersionHigher()
        {
            bool result = _VersionService.IsToolUpdateAvailable(
                "99.99.99",
                "1.0.0");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks that IsApiUpdateAvailable returns true when the bundled version is higher.
        /// </summary>
        [TestMethod]
        public void IsApiUpdateAvailable_ReturnsTrue_WhenBundledVersionHigher()
        {
            bool result = _VersionService.IsApiUpdateAvailable(
                "0.9.0",
                "1.0.0");

            Assert.IsTrue(result);
        }

        /// <summary>
        /// Checks that IsApiUpdateAvailable returns false when the versions match.
        /// </summary>
        [TestMethod]
        public void IsApiUpdateAvailable_ReturnsFalse_WhenVersionsMatch()
        {
            bool result = _VersionService.IsApiUpdateAvailable(
                "1.0.0",
                "1.0.0");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks that IsApiUpdateAvailable returns false when the installed version is higher.
        /// </summary>
        [TestMethod]
        public void IsApiUpdateAvailable_ReturnsFalse_WhenInstalledVersionHigher()
        {
            bool result = _VersionService.IsApiUpdateAvailable(
                "99.99.99",
                "1.0.0");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks that GetBundledToolVersion returns the default version when the assembly does not exist.
        /// </summary>
        [TestMethod]
        public void GetBundledToolVersion_ReturnsDefault_WhenAssemblyNotFound()
        {
            _MockFileSystem.Setup(f => f.FileExists(It.IsAny<string>()))
                .Returns(false);

            string result = _VersionService.GetBundledToolVersion(@"C:\Test\SBT");

            Assert.AreEqual(
                "0.0.0",
                result);
        }

        /// <summary>
        /// Checks that GetBundledApiVersion returns the default version when the assembly does not exist.
        /// </summary>
        [TestMethod]
        public void GetBundledApiVersion_ReturnsDefault_WhenAssemblyNotFound()
        {
            _MockFileSystem.Setup(f => f.FileExists(It.IsAny<string>()))
                .Returns(false);

            string result = _VersionService.GetBundledApiVersion(@"C:\Test\SBT\API");

            Assert.AreEqual(
                "0.0.0",
                result);
        }

        /// <summary>
        /// Checks that IsToolUpdateAvailable returns false when the installed version is not a valid version string.
        /// </summary>
        [TestMethod]
        public void IsToolUpdateAvailable_ReturnsFalse_WhenInstalledVersionInvalid()
        {
            bool result = _VersionService.IsToolUpdateAvailable(
                "invalid",
                "1.0.0");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks that IsApiUpdateAvailable returns false when the bundled version is not a valid version string.
        /// </summary>
        [TestMethod]
        public void IsApiUpdateAvailable_ReturnsFalse_WhenBundledVersionInvalid()
        {
            bool result = _VersionService.IsApiUpdateAvailable(
                "1.0.0",
                "invalid");

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Checks that GetEmbeddedToolVersion parses the version from the resource filename.
        /// </summary>
        [TestMethod]
        public void GetEmbeddedToolVersion_ParsesVersionFromResourceName()
        {
            _MockResourceService.Setup(r => r.FindResource("Tool_"))
                .Returns("Server_Backup_Tool.Installer.Tool_2.0.2.zip");

            string version = _VersionService.GetEmbeddedToolVersion();

            Assert.AreEqual(
                "2.0.2",
                version);
        }

        /// <summary>
        /// Checks that GetEmbeddedToolVersion returns the default when no resource is found.
        /// </summary>
        [TestMethod]
        public void GetEmbeddedToolVersion_ReturnsDefault_WhenResourceNotFound()
        {
            _MockResourceService.Setup(r => r.FindResource("Tool_"))
                .Returns((string?)null);

            string version = _VersionService.GetEmbeddedToolVersion();

            Assert.AreEqual(
                "0.0.0",
                version);
        }

        /// <summary>
        /// Checks that GetEmbeddedApiVersion parses the version from the resource filename.
        /// </summary>
        [TestMethod]
        public void GetEmbeddedApiVersion_ParsesVersionFromResourceName()
        {
            _MockResourceService.Setup(r => r.FindResource("API_"))
                .Returns("Server_Backup_Tool.Installer.API_1.5.0.zip");

            string version = _VersionService.GetEmbeddedApiVersion();

            Assert.AreEqual(
                "1.5.0",
                version);
        }

        /// <summary>
        /// Checks that GetEmbeddedApiVersion returns the default when no resource is found.
        /// </summary>
        [TestMethod]
        public void GetEmbeddedApiVersion_ReturnsDefault_WhenResourceNotFound()
        {
            _MockResourceService.Setup(r => r.FindResource("API_"))
                .Returns((string?)null);

            string version = _VersionService.GetEmbeddedApiVersion();

            Assert.AreEqual(
                "0.0.0",
                version);
        }

        /// <summary>
        /// Checks that GetAllInstallations delegates to the registry service.
        /// </summary>
        [TestMethod]
        public void GetAllInstallations_ReturnsRegistryInstallations()
        {
            List<VersionInfoModel> expected =
            [
                new VersionInfoModel { ServerName = "Server1", ToolVersion = "1.0.0" },
                new VersionInfoModel { ServerName = "Server2", ToolVersion = "2.0.0" }
            ];

            _MockRegistry.Setup(r => r.GetAllInstallations())
                .Returns(expected);

            List<VersionInfoModel> result = _VersionService.GetAllInstallations();

            Assert.AreEqual(
                2,
                result.Count);
            Assert.AreEqual(
                "Server1",
                result[0].ServerName);
            Assert.AreEqual(
                "Server2",
                result[1].ServerName);
        }

        /// <summary>
        /// Checks that GetAllInstallations returns an empty list when no installations exist.
        /// </summary>
        [TestMethod]
        public void GetAllInstallations_ReturnsEmptyList_WhenNoneExist()
        {
            _MockRegistry.Setup(r => r.GetAllInstallations())
                .Returns([]);

            List<VersionInfoModel> result = _VersionService.GetAllInstallations();

            Assert.AreEqual(
                0,
                result.Count);
        }
    }
}
