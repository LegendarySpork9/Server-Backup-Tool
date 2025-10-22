// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Tests.Functions;

namespace ServerBackupTool.Tests.Converters
{
    [TestClass]
    public class JobConverterTest
    {

        // Checks whether the GetBackPaths method returns the expected values.
        [TestMethod]
        public void TestBackupPaths()
        {
            Mock<JobConverter> _mockJobConverter = new();
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            (string source, string destination) = _mockJobConverter.Object.GetBackPaths(serverBackupSection.ServerDetails.Game, serverBackupSection.ServerDetails.Location);

            Assert.IsTrue(!string.IsNullOrWhiteSpace(source));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(destination));
        }
    }
}