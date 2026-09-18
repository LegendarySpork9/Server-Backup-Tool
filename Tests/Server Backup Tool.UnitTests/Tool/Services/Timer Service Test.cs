// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Entities;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;
using System.Net.NetworkInformation;
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

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

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

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

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

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

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
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
        /// <summary>
        /// Checks whether Heartbeat stops the timer and sends an email when the ping fails.
        /// </summary>
        [TestMethod]
        public async Task Heartbeat_StopsTimer_WhenPingFails()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            PingReply failedReply = CreatePingReply(IPStatus.TimedOut);

            _mockPingProvider.Setup(p => p.SendPingAsync(
                It.IsAny<string>(),
                It.IsAny<int>()))
                .ReturnsAsync(failedReply);

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0)
            ];

            _timerService.SetTimers(
                new(),
                timerDurations);

            await _timerService.Heartbeat();

            _mockEmailService.Verify(e => e.CheckForEmail(
                It.IsAny<NotificationElement>(),
                "Heartbeat",
                null),
                Times.Once);
        }

        /// <summary>
        /// Checks whether Heartbeat does nothing when the ping succeeds.
        /// </summary>
        [TestMethod]
        public async Task Heartbeat_DoesNothing_WhenPingSucceeds()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            PingReply successReply = CreatePingReply(IPStatus.Success);

            _mockPingProvider.Setup(p => p.SendPingAsync(
                It.IsAny<string>(),
                It.IsAny<int>()))
                .ReturnsAsync(successReply);

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0)
            ];

            _timerService.SetTimers(
                new(),
                timerDurations);

            await _timerService.Heartbeat();

            _mockEmailService.Verify(e => e.CheckForEmail(
                It.IsAny<NotificationElement>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether ProcessQueuedCommands processes a command when one exists.
        /// </summary>
        [TestMethod]
        public async Task ProcessQueuedCommands_ProcessesCommand_WhenCommandExists()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            CommandModel command = new()
            {
                Id = 1,
                Target = TargetType.Server,
                Command = "say hello"
            };

            _mockCommandService.Setup(c => c.GetCommand())
                .ReturnsAsync((command, (Exception?)null));
            _mockCommandService.Setup(c => c.DeleteCommand(1))
                .ReturnsAsync((true, (Exception?)null));

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0)
            ];

            _timerService.SetTimers(
                new(),
                timerDurations);

            await _timerService.ProcessQueuedCommands(
                null,
                null!);

            _mockApplicationService.Verify(a => a.ProcessCommand(command),
                Times.Once);
            _mockCommandService.Verify(c => c.DeleteCommand(1),
                Times.Once);
        }

        /// <summary>
        /// Checks whether ProcessQueuedCommands does nothing when no command exists.
        /// </summary>
        [TestMethod]
        public async Task ProcessQueuedCommands_DoesNothing_WhenNoCommand()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            _mockCommandService.Setup(c => c.GetCommand())
                .ReturnsAsync(((CommandModel?)null, (Exception?)null));

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0)
            ];

            _timerService.SetTimers(
                new(),
                timerDurations);

            await _timerService.ProcessQueuedCommands(
                null,
                null!);

            _mockApplicationService.Verify(a => a.ProcessCommand(
                It.IsAny<CommandModel>()),
                Times.Never);
            _mockCommandService.Verify(c => c.DeleteCommand(
                It.IsAny<int>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether StartTimers starts all timers except Wait and QueuedCommandCheck.
        /// </summary>
        [TestMethod]
        public void StartTimers_StartsAllTimersExceptWaitAndQueueCheck()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimerCollection timers = new();

            baseAdd = timers.GetType().BaseType!
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

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0),
                new TimeSpan(1, 0, 0)
            ];

            _timerService.SetTimers(
                timers,
                timerDurations);

            _timerService.StartTimers();

            List<TimerModel> timerList = (List<TimerModel>)typeof(TimerService)
                .GetField(
                    "Timers",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_timerService)!;

            foreach (TimerModel timer in timerList)
            {
                if (timer.TimerName == "Wait" || timer.TimerName == "QueuedCommandCheck")
                {
                    Assert.IsFalse(
                        timer.TimerData.Enabled,
                        $"{timer.TimerName} should not be started.");
                }

                else
                {
                    Assert.IsTrue(
                        timer.TimerData.Enabled,
                        $"{timer.TimerName} should be started.");
                }
            }
        }

        /// <summary>
        /// Checks whether RestartHeartbeat stops and restarts the heartbeat timer.
        /// </summary>
        [TestMethod]
        public void RestartHeartbeat_StopsAndRestartsHeartbeatTimer()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0)
            ];

            _timerService.SetTimers(
                new(),
                timerDurations);

            List<TimerModel> timerList = (List<TimerModel>)typeof(TimerService)
                .GetField(
                    "Timers",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_timerService)!;

            TimerModel heartbeatTimer = timerList.First(t => t.TimerName == "Heartbeat");
            heartbeatTimer.TimerData.Stop();

            Assert.IsFalse(
                heartbeatTimer.TimerData.Enabled,
                "Heartbeat timer should be stopped before restart.");

            _timerService.RestartHeartbeat();

            Assert.IsTrue(
                heartbeatTimer.TimerData.Enabled,
                "Heartbeat timer should be running after restart.");
        }

        /// <summary>
        /// Checks whether WaitForClose starts the Wait timer.
        /// </summary>
        [TestMethod]
        public void WaitForClose_StartsWaitTimer()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0)
            ];

            _timerService.SetTimers(
                new(),
                timerDurations);

            _timerService.WaitForClose();

            List<TimerModel> timerList = (List<TimerModel>)typeof(TimerService)
                .GetField(
                    "Timers",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_timerService)!;

            TimerModel waitTimer = timerList.First(t => t.TimerName == "Wait");

            Assert.IsTrue(
                waitTimer.TimerData.Enabled,
                "Wait timer should be started after WaitForClose.");
        }

        /// <summary>
        /// Checks whether ServerWarning logs and sends a command to the server.
        /// </summary>
        [TestMethod]
        public async Task ServerWarning_LogsAndSendsCommand()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
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

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimerCollection timers = new();

            baseAdd = timers.GetType().BaseType!
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

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0),
                new TimeSpan(1, 0, 0)
            ];

            _timerService.SetTimers(
                timers,
                timerDurations);

            List<TimerModel> timerList = (List<TimerModel>)typeof(TimerService)
                .GetField(
                    "Timers",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_timerService)!;

            int warningIndex = timerList.FindIndex(t => t.TimerName == "Warning One");

            await _timerService.ServerWarning(warningIndex);

            _mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Warning One Triggered")),
                It.IsAny<bool>()),
                Times.Once);
            _mockServerService.Verify(s => s.SendCommand(
                "Server will shutdown for a backup in an hour.",
                true),
                Times.Once);
        }

        /// <summary>
        /// Checks whether SystemTimers triggers backup and stops queued commands.
        /// </summary>
        [TestMethod]
        public async Task SystemTimers_Backup_StopsQueuedCommandsAndRunsBackup()
        {
            SBTSection serverBackupSection = new()
            {
                DatabaseDetails = new() { PollingInterval = 1000 }
            };

            Mock<ILoggerService> _mockLogger = new();
            Mock<IApplicationService> _mockApplicationService = new();
            Mock<IServerService> _mockServerService = new();
            Mock<ICommandService> _mockCommandService = new();
            Mock<IEmailService> _mockEmailService = new();
            Mock<IPingProvider> _mockPingProvider = new();

            TimerService _timerService = new(
                _mockLogger.Object,
                _mockApplicationService.Object,
                _mockServerService.Object,
                _mockCommandService.Object,
                _mockEmailService.Object,
                _mockPingProvider.Object,
                serverBackupSection);

            TimeSpan[] timerDurations =
            [
                new TimeSpan(2, 0, 0)
            ];

            _timerService.SetTimers(
                new(),
                timerDurations);

            List<TimerModel> timerList = (List<TimerModel>)typeof(TimerService)
                .GetField(
                    "Timers",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(_timerService)!;

            int backupIndex = timerList.FindIndex(t => t.TimerName == "Backup");

            await _timerService.SystemTimers(backupIndex);

            _mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Backup Triggered")),
                It.IsAny<bool>()),
                Times.Once);
            _mockApplicationService.Verify(a => a.RunBackup(
                _timerService,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Creates a PingReply instance with the specified status using reflection.
        /// </summary>
        private static PingReply CreatePingReply(IPStatus status)
        {
            ConstructorInfo constructor = typeof(PingReply).GetConstructors(
                BindingFlags.NonPublic | BindingFlags.Instance)
                .First(c => c.GetParameters().Length == 5);

            PingReply reply = (PingReply)constructor.Invoke(
                [System.Net.IPAddress.Loopback, null, status, 0L, Array.Empty<byte>()]);

            return reply;
        }
    }
}