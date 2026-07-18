# Server Backup Tool - Infrastructure Document

## Overview

Server Backup Tool is a self-hosted console application for managing game server processes. It provides scheduled backups, log archival, email notifications, and health monitoring via ICMP heartbeat pings. Currently supports Minecraft servers.

- **Author:** Hunter Industries / Toby Hunter
- **Version:** 2.0.2
- **Repository:** https://github.com/LegendarySpork9/Server-Backup-Tool

## Technology Stack

| Component | Technology | Version |
|---|---|---|
| Framework | .NET | 6.0 |
| Language | C# | Latest |
| Application Type | Console Application | - |
| Logging | log4net | 3.3.1 |
| Configuration | System.Configuration (App.config) | - |
| Testing | MSTest | 3.6.3 |
| Test SDK | Microsoft.NET.Test.Sdk | 17.12.0 |
| Mocking | Moq | 4.20.72 |
| Code Coverage | coverlet.collector | 6.0.2 |

## Solution Structure

```
Server-Backup-Tool/
+-- Server Backup Tool/                 # Main console application
|   +-- Abstractions/                   # Interface definitions
|   +-- Converters/                     # Value formatting and game-specific logic
|   +-- Functions/                      # Utility functions
|   +-- Implementations/               # Interface implementations (wrappers)
|   +-- Models/                         # Data models
|   |   +-- Configuration/              # App.config section models
|   +-- Properties/                     # Publish profiles
|   +-- Services/                       # Business logic services
|   +-- Content/                        # Static assets (Logo.ico)
+-- Server Backup Tool.Tests/           # Unit test project
|   +-- Converters/                     # Converter tests
|   +-- Functions/                      # Test helper functions
|   +-- Mocks/                          # Mock data
|   |   +-- Configs/                    # Mock configuration files
|   |   +-- Server/                     # Mock server files
|   +-- Services/                       # Service tests
+-- .github/workflows/                  # CI/CD pipeline definitions
```

## Application Architecture

### Application Type

The application is a **.NET 6.0 console application** that runs as a long-lived process alongside a game server. It launches the game server as a child process with redirected I/O, monitors its output, and manages scheduled operations.

### Dependency Injection

External dependencies are wrapped behind interfaces to support testability. Services are instantiated manually rather than through a DI container.

| Abstraction | Implementation | Purpose |
|---|---|---|
| `ILoggerService` | `LoggerServiceWrapper` | Application and server logging via log4net |
| `IFileSystem` | `FileSystem` | File system and ZIP archive operations |
| `IEmailSender` | `SMTPEmailSender` | SMTP email delivery |
| `IClock` | `SystemClock` | UTC time operations |

### Services

| Service | Responsibility |
|---|---|
| `ApplicationService` | Top-level orchestrator for server lifecycle, backup workflow, and user input |
| `TimerService` | Manages heartbeat, backup, wait, and custom timers |
| `ServerService` | Game server process management and output monitoring |
| `JobService` | Backup creation, log archival, and old file cleanup |
| `EmailService` | Email construction, trigger matching, and SMTP delivery |
| `LoggerService` | Internal log4net adapter with dual loggers (tool and server) |
| `PidFileService` | Process ID file management for server instance tracking |

### Converters

| Converter | Responsibility |
|---|---|
| `ServerConverter` | Game-specific server commands (stop, message, final message detection) |
| `JobConverter` | Game-specific backup source and destination paths |
| `TimeConverter` | Calculates duration between current time and a scheduled trigger time |

### Functions

| Function | Responsibility |
|---|---|
| `FilterConsoleFunction` | TextWriter wrapper that intercepts console output, ensuring all messages pass through log4net |

### Application Lifecycle

#### Startup

1. Initialise `EmailService` and configure console output filter
2. Configure log4net from `App.config`
3. Register process exit handler
4. Load `serverBackup` configuration section
5. Send "Open" notification email (if configured)
6. Create `ApplicationService` and begin execution

#### Runtime

1. Calculate timer durations from configured trigger times
2. Set and start all timers (heartbeat, backup, custom)
3. Launch game server process with redirected I/O
4. Write PID file to `%PROGRAMDATA%`
5. Enter user input loop for console commands

#### Backup Workflow

1. Send stop command to the game server
2. Wait 30 seconds via the Wait timer
3. Wait for server process to fully close
4. Create ZIP backup of the game world directory
5. Archive log files into a dated ZIP
6. Delete backups and archived logs older than 10 days
7. Restart the full application cycle

#### Shutdown

1. User enters `exit app` command
2. Stop command sent to the server (with 30-second wait if running)
3. Process exit handler sends "Close" notification email
4. PID file deleted

### Timer System

| Timer | Interval | Purpose |
|---|---|---|
| Heartbeat | 5 seconds | Pings the server IP; sends heartbeat email and stops if unreachable |
| Wait | 30 seconds | Delay before backup completion (activated on demand) |
| Backup | Calculated from config | Triggers the backup workflow at the configured time |
| Custom Timers | Calculated from config | Sends configured messages to the server at scheduled times |

### Console Commands

| Command | Action |
|---|---|
| `exit app` | Gracefully stops the server and exits the application |
| `start server` | Starts the server if it is not currently running |
| `reset heartbeat` | Restarts the heartbeat timer |
| Any other input | Sent directly as a command to the server process |

## Supported Games

| Game | Stop Command | Message Command | Final Message | Backup Source |
|---|---|---|---|---|
| Minecraft | `stop` | `/say {message}` | `{WorkingDirectory}>PAUSE` | `{ServerPath}\world` |

## Data Persistence

The application uses **file-based persistence** with no database dependency.

| Data | Storage Format | Location |
|---|---|---|
| Game World Backups | ZIP files (one per day) | `{ServerPath}\Backups` |
| Archived Logs | ZIP files (one per day) | `.\Archived Logs` |
| Application Logs | Rolling text files | `.\Logs\Server Backup.log` |
| Server Output Logs | Rolling text files | `.\Logs\Server.log` |
| PID Files | Text file (process ID + UTC start time) | `%PROGRAMDATA%\Hunter Industries\Server Backup Tool` |

### File Retention

| Data | Retention Period |
|---|---|
| Game World Backups | 10 days |
| Archived Logs | 10 days |
| Application Logs | 10 rolling files, 10 MB each |
| Server Output Logs | 10 rolling files, 10 MB each |

## Email Notification System

### Trigger Types

| Trigger | Type | When Sent |
|---|---|---|
| `Open` | System | Application startup |
| `Close` | System | Application shutdown |
| `Heartbeat` | System | Server heartbeat ping fails |
| Custom text | Server | Server output contains the trigger text |

### Email Features

- HTML email bodies (inline or from file)
- Inline image attachments with content IDs for HTML references
- Multiple recipients per email template
- Configurable SMTP provider with SSL/TLS support
- Notifications can be globally enabled or disabled

## Configuration

### App.config Structure

```xml
<configuration>
  <configSections>
    <section name="log4net" type="..." />
    <section name="serverBackup" type="ServerBackupTool.Models.Configuration.SBTSection, Server Backup Tool" />
  </configSections>

  <serverBackup>
    <serverDetails name="<server name>"
                   game="<Minecraft>"
                   location="<path to server directory>"
                   startFile="<server executable>"
                   ipAddress="<server IP for heartbeat>" />
    <timerDetails count="<number of custom timers>"
                  backupTime="<HH:mm:ss>">
      <timers>
        <timer name="<timer display name>"
               time="<HH:mm:ss>"
               message="<command to send to server>" />
      </timers>
    </timerDetails>
    <notifications enabled="<true|false>"
                   port="<SMTP port, default: 587>"
                   enableSSL="<true|false, default: true>">
      <provider name="<SMTP server hostname>"
                password="<SMTP password>" />
      <fromAddress email="<sender email>"
                   name="<sender display name, default: Server Backup Tool>" />
      <emails>
        <email trigger="<trigger keyword>"
               system="<true|false, default: false>">
          <addresses>
            <toAddress email="<recipient email>"
                       name="<recipient display name>" />
          </addresses>
          <subject value="<email subject>" />
          <content value="<HTML content or path to .html file>" />
          <images>
            <image key="<content ID>" path="<path to image file>" />
          </images>
        </email>
      </emails>
    </notifications>
  </serverBackup>

  <log4net>
    <!-- Appender and logger configuration -->
  </log4net>
</configuration>
```

## Logging

- **Framework:** log4net 3.3.1
- **Configuration:** Embedded in App.config

### Appenders

| Appender | Type | File | Purpose |
|---|---|---|---|
| ConsoleAppender | Console | - | Display application messages (up to WARN level) |
| BackupLogAppender | RollingFile | `Logs\Server Backup.log` | Application operation logs |
| ServerLogAppender | RollingFile | `Logs\Server.log` | Game server output logs |

### Log File Settings

- **Max File Size:** 10 MB
- **Backup Count:** 10 rolling files
- **Format:** `{ISO8601 Timestamp} {LEVEL} - {Message}`
- **Lock Model:** MinimalLock (concurrent access safe)

### Loggers

| Logger | Appenders | Purpose |
|---|---|---|
| ToolLogs | BackupLogAppender, ConsoleAppender | Application operation logging |
| ServerLogs | ServerLogAppender | Game server output logging |

### Console Output Filtering

Console output is intercepted by `FilterConsoleFunction`, a custom `TextWriter` wrapper. It strips the `log4net - ` prefix from messages that pass through log4net, and flags any output that bypasses the logging pipeline as an error.

### Server Log Level Detection

Server output log levels are parsed from the message content:

| Pattern | Log Level |
|---|---|
| `/INFO]` | Info |
| `/WARN]` | Warn |
| `/ERROR]` | Error |
| `/DEBUG]` | Debug |
| Default | Info |

## CI/CD

### GitHub Actions Workflows

All workflows run on `windows-latest` using .NET 6.0.x SDK.

| Workflow | Trigger | Steps |
|---|---|---|
| **CI on Commit** (`Commit.yml`) | Push to any branch | Checkout, Restore, Build (Release) |
| **CI on Pull Request** (`Pull Request.yml`) | PR to any branch | Download and start Papercut SMTP, Checkout, Restore, Build (Release), Run Tests, Stop Papercut SMTP |
| **Check for Linked Issue** (`PR Linked Issue.yml`) | PR opened/edited/reopened/synchronised | Verifies PR has linked GitHub issues via description, comments, or Development section |

### Pull Request Test Infrastructure

The Pull Request workflow downloads and starts [Papercut SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP), a local mock SMTP server, to enable integration testing of email functionality without requiring an external mail server.

### Build Configuration

- **SDK:** .NET 6.0.x
- **Configuration:** Release
- **Test Runner:** `dotnet test` (MSTest)

## Hosting Requirements

### Runtime Prerequisites

- .NET 6.0 Runtime
- Windows (required for game server process management)

### Network Requirements

- ICMP access to the game server IP address (for heartbeat pings)
- Outbound SMTP (configurable port, default 587) for email notifications

### File System Requirements

- Read/write access to the game server directory (for process management and backups)
- Read/write access to the `Logs/` directory
- Read/write access to the `Archived Logs/` directory
- Read/write access to `%PROGRAMDATA%\Hunter Industries\Server Backup Tool` (for PID files)
