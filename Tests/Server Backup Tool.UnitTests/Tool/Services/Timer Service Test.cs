// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;
using System.Reflection;

namespace ServerBackupTool.UnitTests.Tool.Services
{
    [TestClass]
    public class TimerServiceTest
    {
        /// <summary>
        /// Checks whether the SetTimers method creates the timers without the heartbeat timer.
        /// </summary>
        [TestMethod]
        public void TestSetTimers()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
            };
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IExtendedFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);
            Mock<ApplicationService> _mockApplicationService = new(serverBackupSection);
            Mock<ServerService> _mockServerService = new(
                _mockLogger.Object,
                _pidFileService,
                serverBackupSection,
                server);

            TimeSpan[] timerDurations = new[]
            {
                new TimeSpan(2, 0, 0),
                new TimeSpan(1, 0, 0)
            };

            TimerCollection timers = new();

            MethodInfo baseAdd = timers.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                timers,
                [ new TimerElement()
            {
                Name = "Warning One",
                Time = "01:00:00",
                Message = "Server will shutdown for a backup in an hour."
            } ]);

            Mock<IDatabase> _mockDatabase = new();
            Mock<IClock> _mockClock = new();
            DatabaseOptionsModel dbOptions = new()
            {
                Path = "test",
                ServerName = "TestServer",
                PollingIntervalMs = 1000
            };
            CommandService _commandService = new(
                _mockLogger.Object,
                _mockDatabase.Object,
                _mockClock.Object,
                dbOptions);

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _commandService,
                serverBackupSection);

            string expected = "Completed";

            string actual = _timerService.SetTimers(
                timers,
                timerDurations);

            Assert.AreEqual(
                expected,
                actual);
            _mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Failed to set up")),
                It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the SetTimers method creates the timers with the heartbeat timer.
        /// </summary>
        [TestMethod]
        public void TestSetTimersHeartbeat()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
            };
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IExtendedFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);
            Mock<ApplicationService> _mockApplicationService = new(serverBackupSection);
            Mock<ServerService> _mockServerService = new(
                _mockLogger.Object,
                _pidFileService,
                serverBackupSection,
                server);

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0),
                new TimeSpan(1, 0, 0)
            ];

            TimerCollection timers = new();

            MethodInfo baseAdd = timers.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                timers,
                [ new TimerElement()
            {
                Name = "Warning One",
                Time = "01:00:00",
                Message = "Server will shutdown for a backup in an hour."
            } ]);

            NotificationElement notifications = new()
            {
                Enabled = true
            };

            baseAdd = notifications.Emails.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                notifications.Emails,
                [ new EmailElement()
            {
                Trigger = "Heartbeat",
                SystemEmail = true
            } ]);

            serverBackupSection.Notifications = notifications;

            Mock<IDatabase> _mockDatabase = new();
            Mock<IClock> _mockClock = new();
            DatabaseOptionsModel dbOptions = new()
            {
                Path = "test",
                ServerName = "TestServer",
                PollingIntervalMs = 1000
            };
            CommandService _commandService = new(
                _mockLogger.Object,
                _mockDatabase.Object,
                _mockClock.Object,
                dbOptions);

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _commandService,
                serverBackupSection);

            string expected = "Completed";

            string actual = _timerService.SetTimers(
                timers,
                timerDurations);

            Assert.AreEqual(
                expected,
                actual);
            _mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Failed to set up")),
                It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether the SetTimers method creates only the system timers.
        /// </summary>
        [TestMethod]
        public void TestSetTimersOnlySystem()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
            };
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IExtendedFileSystem> _mockFileSystem = new();
            PidFileService _pidFileService = new(
                _mockLogger.Object,
                _mockFileSystem.Object);
            Mock<ApplicationService> _mockApplicationService = new(serverBackupSection);
            Mock<ServerService> _mockServerService = new(
                _mockLogger.Object,
                _pidFileService,
                serverBackupSection,
                server);

            TimeSpan[] timerDurations = new[]
            {
                new TimeSpan(2, 0, 0)
            };

            NotificationElement notifications = new()
            {
                Enabled = true
            };

            MethodInfo baseAdd = notifications.Emails.GetType().BaseType!
                .GetMethod(
                    "BaseAdd",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    [typeof(System.Configuration.ConfigurationElement)],
                    null)!;

            baseAdd.Invoke(
                notifications.Emails,
                [ new EmailElement()
            {
                Trigger = "Heartbeat",
                SystemEmail = true
            } ]);

            serverBackupSection.Notifications = notifications;

            Mock<IDatabase> _mockDatabase = new();
            Mock<IClock> _mockClock = new();
            DatabaseOptionsModel dbOptions = new()
            {
                Path = "test",
                ServerName = "TestServer",
                PollingIntervalMs = 1000
            };
            CommandService _commandService = new(
                _mockLogger.Object,
                _mockDatabase.Object,
                _mockClock.Object,
                dbOptions);

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _commandService,
                serverBackupSection);

            string expected = "Completed";

            string actual = _timerService.SetTimers(
                new(),
                timerDurations);

            Assert.AreEqual(
                expected,
                actual);
            _mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Failed to set up")),
                It.IsAny<bool>()),
                Times.Never);
        }
    }
}