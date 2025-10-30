// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;
using System.Reflection;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class TimerServiceTest
    {
        // Checks whether the SetTimers method creates the timers without the heartbeat timer.
        [TestMethod]
        public void TestSetTimers()
        {
            SBTSection serverBackupSection = new();
            ServerModel server = new(new());

            Mock<ILoggerService> _mockLogger = new();
            Mock<ApplicationService> _mockApplicationService = new(serverBackupSection);
            Mock<ServerService> _mockServerService = new(_mockLogger.Object, serverBackupSection, server);

            var timerDurations = new[]
            {
                new TimeSpan(2, 0, 0),
                new TimeSpan(1, 0, 0)
            };

            TimerCollection timers = new();

            MethodInfo baseAdd = timers.GetType().BaseType!
                .GetMethod("BaseAdd", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(System.Configuration.ConfigurationElement) }, null)!;

            baseAdd.Invoke(timers, new object[] { new TimerElement()
            {
                Name = "Warning One",
                Time = "01:00:00",
                Message = "Server will shutdown for a backup in an hour."
            } });

            TimerService _timerService = new(_mockApplicationService.Object, _mockServerService.Object, _mockLogger.Object, serverBackupSection);

            string expected = "Completed";

            string actual = _timerService.SetTimers(timers, timerDurations);

            Assert.AreEqual(expected, actual);
            _mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Failed to set up")),
                It.IsAny<bool>()),
                Times.Never);
        }

        // Checks whether the SetTimers method creates the timers with the heartbeat timer.
        [TestMethod]
        public void TestSetTimersHeartbeat()
        {
            SBTSection serverBackupSection = new();
            ServerModel server = new(new());

            Mock<ILoggerService> _mockLogger = new();
            Mock<ApplicationService> _mockApplicationService = new(serverBackupSection);
            Mock<ServerService> _mockServerService = new(_mockLogger.Object, serverBackupSection, server);

            var timerDurations = new[]
            {
                new TimeSpan(2, 0, 0),
                new TimeSpan(1, 0, 0)
            };

            TimerCollection timers = new();

            MethodInfo baseAdd = timers.GetType().BaseType!
                .GetMethod("BaseAdd", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(System.Configuration.ConfigurationElement) }, null)!;

            baseAdd.Invoke(timers, new object[] { new TimerElement()
            {
                Name = "Warning One",
                Time = "01:00:00",
                Message = "Server will shutdown for a backup in an hour."
            } });

            NotificationElement notifications = new()
            {
                Enabled = true
            };

            baseAdd = notifications.Emails.GetType().BaseType!
                .GetMethod("BaseAdd", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(System.Configuration.ConfigurationElement) }, null)!;

            baseAdd.Invoke(notifications.Emails, new object[] { new EmailElement()
            {
                Trigger = "Heartbeat",
                SystemEmail = true
            } });

            serverBackupSection.Notifications = notifications;

            TimerService _timerService = new(_mockApplicationService.Object, _mockServerService.Object, _mockLogger.Object, serverBackupSection);

            string expected = "Completed";

            string actual = _timerService.SetTimers(timers, timerDurations);

            Assert.AreEqual(expected, actual);
            _mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Failed to set up")),
                It.IsAny<bool>()),
                Times.Never);
        }

        // Checks whether the SetTimers method creates only the system timers.
        [TestMethod]
        public void TestSetTimersOnlySystem()
        {
            SBTSection serverBackupSection = new();
            ServerModel server = new(new());

            Mock<ILoggerService> _mockLogger = new();
            Mock<ApplicationService> _mockApplicationService = new(serverBackupSection);
            Mock<ServerService> _mockServerService = new(_mockLogger.Object, serverBackupSection, server);

            var timerDurations = new[]
            {
                new TimeSpan(2, 0, 0)
            };

            NotificationElement notifications = new()
            {
                Enabled = true
            };

            MethodInfo baseAdd = notifications.Emails.GetType().BaseType!
                .GetMethod("BaseAdd", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(System.Configuration.ConfigurationElement) }, null)!;

            baseAdd.Invoke(notifications.Emails, new object[] { new EmailElement()
            {
                Trigger = "Heartbeat",
                SystemEmail = true
            } });

            serverBackupSection.Notifications = notifications;

            TimerService _timerService = new(_mockApplicationService.Object, _mockServerService.Object, _mockLogger.Object, serverBackupSection);

            string expected = "Completed";

            string actual = _timerService.SetTimers(new(), timerDurations);

            Assert.AreEqual(expected, actual);
            _mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Failed to set up")),
                It.IsAny<bool>()),
                Times.Never);
        }
    }
}