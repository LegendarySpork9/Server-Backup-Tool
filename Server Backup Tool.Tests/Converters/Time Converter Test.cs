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
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");
            Mock<TimeConverter> _mockTimeConverter = new(serverBackupSection.TimerDetails.TimeZone);

            TimeSpan duration = _mockTimeConverter.Object.GetDuration(serverBackupSection.TimerDetails.BackupTime);

            Assert.IsNotNull(duration);
        }
    }
}