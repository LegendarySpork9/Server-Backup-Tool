// Copyright � - 31/10/2024 - Toby Hunter
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Converters;

namespace ServerBackupTool.UnitTests.Tool.Converters
{
    [TestClass]
    public class TimeConverterTest
    {
        /// <summary>
        /// Checks whether the method GetDuration returns the expected duration.
        /// </summary>
        [TestMethod]
        public void TestGetDuration()
        {
            Mock<IClock> mockClock = new();
            mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 01, 01));

            TimeConverter _timeConverter = new(mockClock.Object);

            string triggerTime = "02:00:00";

            TimeSpan expected = new(02, 00, 00);

            TimeSpan actual = _timeConverter.GetDuration(triggerTime);

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the method GetDuration calculates correctly when the trigger is the next day.
        /// </summary>
        [TestMethod]
        public void GetDuration_CalculatesCorrectly_WhenTriggerIsNextDay()
        {
            Mock<IClock> mockClock = new();
            mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 01, 01, 23, 00, 00, DateTimeKind.Utc));

            TimeConverter _timeConverter = new(mockClock.Object);

            string triggerTime = "02:00:00";

            TimeSpan expected = new(03, 00, 00);

            TimeSpan actual = _timeConverter.GetDuration(triggerTime);

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the method GetDuration calculates correctly when the trigger is today.
        /// </summary>
        [TestMethod]
        public void GetDuration_CalculatesCorrectly_WhenTriggerIsToday()
        {
            Mock<IClock> mockClock = new();
            mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc));

            TimeConverter _timeConverter = new(mockClock.Object);

            string triggerTime = "15:00:00";

            TimeSpan expected = new(07, 00, 00);

            TimeSpan actual = _timeConverter.GetDuration(triggerTime);

            Assert.AreEqual(
                expected,
                actual);
        }
    }
}