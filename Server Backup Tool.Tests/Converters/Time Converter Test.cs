// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Tests.Functions;

namespace ServerBackupTool.Tests.Converters
{
    [TestClass]
    public class TimeConverterTest
    {
        [TestMethod]
        public void TestDuration()
        {
            Mock<TimeConverter> _mockTimeConverter = new();
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");

            TimeSpan duration = _mockTimeConverter.Object.GetDuration(serverBackupSection.TimerDetails.BackupTime);

            Assert.IsNotNull(duration);
        }
    }
}