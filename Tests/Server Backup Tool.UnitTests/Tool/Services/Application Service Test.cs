// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Entities;
using ServerBackupTool.Common.Models.Requests;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Services;

namespace ServerBackupTool.UnitTests.Tool.Services
{
    [TestClass]
    public class ApplicationServiceTest
    {
        /// <summary>
        /// Checks whether ProcessCommand logs the exit app command.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommand_ExitApp_LogsExitCommand()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            SBTSection serverBackupSection = new();
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            CommandModel command = new()
            {
                Id = 1,
                Target = TargetType.Tool,
                Command = "exit app"
            };

            await applicationService.ProcessCommand(command);

            mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Exit Command Triggered")),
                It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// Checks whether ProcessCommand sends a server command to the server service.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommand_ServerCommand_SendsToServer()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            SBTSection serverBackupSection = new();
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            CommandModel command = new()
            {
                Id = 2,
                Target = TargetType.Server,
                Command = "say hello"
            };

            await applicationService.ProcessCommand(command);

            mockServerService.Verify(s => s.SendCommand(
                "say hello",
                false),
                Times.Once);
        }

        /// <summary>
        /// Checks whether ProcessCommand starts the server when it is not running.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommand_StartServer_CallsServerServiceStart()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            SBTSection serverBackupSection = new();
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft",
                ServerRunning = false
            };

            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            CommandModel command = new()
            {
                Id = 3,
                Target = TargetType.Tool,
                Command = "start server"
            };

            await applicationService.ProcessCommand(command);

            mockServerService.Verify(s => s.StartServer(),
                Times.Once);
        }

        /// <summary>
        /// Checks whether ProcessCommand restarts the heartbeat timer.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommand_ResetHeartbeat_CallsTimerServiceRestart()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            SBTSection serverBackupSection = new();
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            CommandModel command = new()
            {
                Id = 4,
                Target = TargetType.Tool,
                Command = "reset heartbeat"
            };

            await applicationService.ProcessCommand(command);

            mockTimerService.Verify(t => t.RestartHeartbeat(),
                Times.Once);
        }

        /// <summary>
        /// Checks whether RunApplication sets timers and starts the server.
        /// </summary>
        [TestMethod]
        public async Task RunApplication_SetsTimersAndStartsServer()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            mockTimerService.Setup(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()))
                .Returns("Completed");

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            mockCommandReader.Setup(r => r.ReadCommand())
                .Returns("exit app");

            mockCommandService.Setup(c => c.LogCommand(
                It.IsAny<CommandRequestModel>()))
                .ReturnsAsync((true, (Exception?)null));

            SBTSection serverBackupSection = new()
            {
                TimerDetails = new() { BackupTime = "04:00:00" }
            };

            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            CancellationTokenSource cts = new();
            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            await applicationService.RunApplication(cts.Token);

            mockTimerService.Verify(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()),
                Times.Once);
            mockTimerService.Verify(t => t.StartTimers(),
                Times.Once);
            mockServerService.Verify(s => s.StartServer(),
                Times.Once);
            mockTimerService.Verify(t => t.StartQueuedCommandCheckTimer(),
                Times.Once);
        }

        /// <summary>
        /// Checks whether RunBackup calls the job service methods.
        /// </summary>
        [TestMethod]
        public async Task RunBackup_CallsJobServiceMethods()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            mockJobService.Setup(j => j.RunJobs("backup"))
                .ReturnsAsync("Complete");
            mockJobService.Setup(j => j.RunJobs("archive"))
                .ReturnsAsync("Complete");
            mockJobService.Setup(j => j.RunJobs("clean"))
                .ReturnsAsync("Complete");

            mockTimerService.Setup(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()))
                .Returns("Completed");

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            mockCommandReader.Setup(r => r.ReadCommand())
                .Returns("exit app");

            mockCommandService.Setup(c => c.LogCommand(
                It.IsAny<CommandRequestModel>()))
                .ReturnsAsync((true, (Exception?)null));

            SBTSection serverBackupSection = new()
            {
                TimerDetails = new() { BackupTime = "04:00:00" }
            };

            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            CancellationTokenSource cts = new();
            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            // Signal WaitForServerClose immediately so RunBackup does not block.
            ApplicationService.WaitForServerClose.Set();

            await applicationService.RunBackup(mockTimerService.Object, cts.Token);

            mockJobService.Verify(j => j.RunJobs("backup"),
                Times.Once);
            mockJobService.Verify(j => j.RunJobs("archive"),
                Times.Once);
            mockJobService.Verify(j => j.RunJobs("clean"),
                Times.Once);
        }

        /// <summary>
        /// Checks whether UserInput with exit app stops the loop and logs the command.
        /// </summary>
        [TestMethod]
        public async Task UserInput_ExitApp_LogsExitCommand()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            mockCommandReader.Setup(r => r.ReadCommand())
                .Returns("exit app");

            mockCommandService.Setup(c => c.LogCommand(
                It.IsAny<CommandRequestModel>()))
                .ReturnsAsync((true, (Exception?)null));

            mockTimerService.Setup(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()))
                .Returns("Completed");

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            SBTSection serverBackupSection = new()
            {
                TimerDetails = new() { BackupTime = "04:00:00" }
            };

            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            CancellationTokenSource cts = new();
            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            await applicationService.RunApplication(cts.Token);

            mockCommandService.Verify(c => c.LogCommand(
                It.Is<CommandRequestModel>(r =>
                    r.Target == "Tool" &&
                    r.Command == "exit app")),
                Times.Once);
            mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Exit Command Queued")),
                It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// Checks whether UserInput sends a generic command to the server via the command service.
        /// </summary>
        [TestMethod]
        public async Task UserInput_GenericCommand_SendsToServer()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            Queue<string?> commands = new();
            commands.Enqueue("say hello");
            commands.Enqueue("exit app");

            mockCommandReader.Setup(r => r.ReadCommand())
                .Returns(() => commands.Dequeue());

            mockCommandService.Setup(c => c.LogCommand(
                It.IsAny<CommandRequestModel>()))
                .ReturnsAsync((true, (Exception?)null));

            mockTimerService.Setup(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()))
                .Returns("Completed");

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            SBTSection serverBackupSection = new()
            {
                TimerDetails = new() { BackupTime = "04:00:00" }
            };

            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            CancellationTokenSource cts = new();
            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            await applicationService.RunApplication(cts.Token);

            mockCommandService.Verify(c => c.LogCommand(
                It.Is<CommandRequestModel>(r =>
                    r.Target == "Server" &&
                    r.Command == "say hello")),
                Times.Once);
            mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s == "Command Queued"),
                It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// Checks whether ProcessCommand does not start the server when it is already running.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommand_StartServer_DoesNothing_WhenServerAlreadyRunning()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            SBTSection serverBackupSection = new();
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft",
                ServerRunning = true
            };

            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            CommandModel command = new()
            {
                Id = 5,
                Target = TargetType.Tool,
                Command = "start server"
            };

            await applicationService.ProcessCommand(command);

            mockServerService.Verify(s => s.StartServer(),
                Times.Never);
            mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Starting Server")),
                It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// Checks whether ProcessCommand sends stop command and waits when server is running on exit.
        /// </summary>
        [TestMethod]
        public async Task ProcessCommand_ExitApp_StopsServerWhenRunning()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            SBTSection serverBackupSection = new();
            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft",
                ServerRunning = true
            };

            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server,
                TimeSpan.Zero);

            CommandModel command = new()
            {
                Id = 6,
                Target = TargetType.Tool,
                Command = "exit app"
            };

            await applicationService.ProcessCommand(command);

            mockServerService.Verify(s => s.SendCommand(
                "stop",
                false),
                Times.Once);
            mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Stop Command Sent to Server")),
                It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// Checks whether UserInput handles the start server command correctly.
        /// </summary>
        [TestMethod]
        public async Task UserInput_StartServer_LogsStartServerQueued()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            mockCommandReader.Setup(r => r.ReadCommand())
                .Returns("start server");

            mockCommandService.Setup(c => c.LogCommand(
                It.IsAny<CommandRequestModel>()))
                .ReturnsAsync((true, (Exception?)null));

            mockTimerService.Setup(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()))
                .Returns("Completed");

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            SBTSection serverBackupSection = new()
            {
                TimerDetails = new() { BackupTime = "04:00:00" }
            };

            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            CancellationTokenSource cts = new();
            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            await applicationService.RunApplication(cts.Token);

            mockCommandService.Verify(c => c.LogCommand(
                It.Is<CommandRequestModel>(r =>
                    r.Target == "Tool" &&
                    r.Command == "start server")),
                Times.Once);
            mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Start Server Queued")),
                It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// Checks whether UserInput handles the reset heartbeat command correctly.
        /// </summary>
        [TestMethod]
        public async Task UserInput_ResetHeartbeat_LogsHeartbeatResetQueued()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            Queue<string?> commands = new();
            commands.Enqueue("reset heartbeat");
            commands.Enqueue("exit app");

            mockCommandReader.Setup(r => r.ReadCommand())
                .Returns(() => commands.Dequeue());

            mockCommandService.Setup(c => c.LogCommand(
                It.IsAny<CommandRequestModel>()))
                .ReturnsAsync((true, (Exception?)null));

            mockTimerService.Setup(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()))
                .Returns("Completed");

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            SBTSection serverBackupSection = new()
            {
                TimerDetails = new() { BackupTime = "04:00:00" }
            };

            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            CancellationTokenSource cts = new();
            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            await applicationService.RunApplication(cts.Token);

            mockCommandService.Verify(c => c.LogCommand(
                It.Is<CommandRequestModel>(r =>
                    r.Target == "Tool" &&
                    r.Command == "reset heartbeat")),
                Times.Once);
            mockLogger.Verify(l => l.LogToolMessage(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("Heartbeat Reset Queued")),
                It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// Checks whether UserInput skips null or empty commands.
        /// </summary>
        [TestMethod]
        public async Task UserInput_NullCommand_SkipsAndContinues()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            Queue<string?> commands = new();
            commands.Enqueue(null);
            commands.Enqueue("");
            commands.Enqueue("exit app");

            mockCommandReader.Setup(r => r.ReadCommand())
                .Returns(() => commands.Dequeue());

            mockCommandService.Setup(c => c.LogCommand(
                It.IsAny<CommandRequestModel>()))
                .ReturnsAsync((true, (Exception?)null));

            mockTimerService.Setup(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()))
                .Returns("Completed");

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            SBTSection serverBackupSection = new()
            {
                TimerDetails = new() { BackupTime = "04:00:00" }
            };

            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            CancellationTokenSource cts = new();
            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            await applicationService.RunApplication(cts.Token);

            mockCommandService.Verify(c => c.LogCommand(
                It.Is<CommandRequestModel>(r =>
                    r.Target == "Tool" &&
                    r.Command == "exit app")),
                Times.Once);
        }

        /// <summary>
        /// Checks whether UserInput exits via cancellation token.
        /// </summary>
        [TestMethod]
        public async Task UserInput_CancellationToken_ExitsLoop()
        {
            Mock<ILoggerService> mockLogger = new();
            Mock<IClock> mockClock = new();
            Mock<ICommandReader> mockCommandReader = new();
            Mock<ICommandService> mockCommandService = new();
            Mock<IPidFileService> mockPidFileService = new();
            Mock<IServerService> mockServerService = new();
            Mock<ITimerService> mockTimerService = new();
            Mock<IJobService> mockJobService = new();

            mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));

            CancellationTokenSource cts = new();

            mockCommandReader.Setup(r => r.ReadCommand())
                .Returns(() =>
                {
                    cts.Cancel();
                    return null;
                });

            mockTimerService.Setup(t => t.SetTimers(
                It.IsAny<TimerCollection>(),
                It.IsAny<TimeSpan[]>()))
                .Returns("Completed");

            mockServerService.Setup(s => s.StartServer())
                .ReturnsAsync("Completed");

            SBTSection serverBackupSection = new()
            {
                TimerDetails = new() { BackupTime = "04:00:00" }
            };

            ServerModel server = new(new())
            {
                Name = "Test Server",
                Game = "Minecraft"
            };

            ApplicationService applicationService = new(
                mockLogger.Object,
                mockClock.Object,
                mockCommandReader.Object,
                mockCommandService.Object,
                mockPidFileService.Object,
                mockServerService.Object,
                mockTimerService.Object,
                mockJobService.Object,
                serverBackupSection,
                server);

            await applicationService.RunApplication(cts.Token);

            mockCommandService.Verify(c => c.LogCommand(
                It.IsAny<CommandRequestModel>()),
                Times.Never);
        }
    }
}