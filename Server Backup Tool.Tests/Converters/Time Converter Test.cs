// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Converters;

namespace ServerBackupTool.Tests.Converters
{
    [TestClass]
    public class TimeConverterTest
    {
        // Checks whether the method GetDuration returns the expected duration.
        [TestMethod]
        public void TestGetDuration()
        {
            Mock<IClock> mockClock = new();
            mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 01, 01));

            TimeConverter _timeConverter = new(mockClock.Object);

            string triggerTime = "02:00:00";

            TimeSpan expected = new(02, 00, 00);

            TimeSpan actual = _timeConverter.GetDuration(triggerTime);

            Assert.AreEqual(expected, actual);
        }
    }
}