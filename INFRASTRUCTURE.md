# Server Backup Tool - Infrastructure Document

## Overview

Server Backup Tool is a self-hosted console application for managing game server processes. It provides scheduled backups, log archival, email notifications, and health monitoring via ICMP heartbeat pings. Currently supports Minecraft servers. A companion Web API provides HTTP access to log data and a command queue.

- **Author:** Hunter Industries / Toby Hunter
- **Version:** 3.0.0
- **Repository:** https://github.com/LegendarySpork9/Server-Backup-Tool

## Technology Stack

| Component | Technology | Version |
|---|---|---|
| Framework | .NET | 10.0 |
| Language | C# | Latest |
| Application Type | Console Application + Web API + TUI Installer | - |
| Logging | log4net | 3.3.2 |
| Database | Microsoft.Data.Sqlite | 10.0.11 |
| API Documentation | Scalar.AspNetCore | 2.16.18 |
| OpenAPI | Microsoft.AspNetCore.OpenApi | 10.0.10 |
| OpenAPI Models | Microsoft.OpenApi | 2.11.0 |
| Authentication | Basic (custom handler) | - |
| Configuration | System.Configuration (App.config) | - |
| Installer TUI | Spectre.Console | 0.57.2 |
| Integration Test TUI | Spectre.Console.Testing | 0.57.2 |
| Task Scheduler | TaskScheduler | 2.12.2 |
| Integration Test HTTP | Microsoft.AspNetCore.Mvc.Testing | 10.0.0 |
| Testing | MSTest | 3.6.3 |
| Test SDK | Microsoft.NET.Test.Sdk | 17.12.0 |
| Mocking | Moq | 4.20.72 |
| Code Coverage | coverlet.collector | 6.0.2 |

## Solution Structure

```
Server-Backup-Tool/
├── Server Backup Tool/                     # Console application (game server management)
│   ├── Abstractions/                       # ILoggerService, IExtendedDatabase, IEmailSender, IExtendedFileSystem
│   ├── Converters/                         # ServerConverter, JobConverter, TimeConverter
│   ├── Functions/                          # ConsoleFunction
│   ├── Implementations/                    # LoggerServiceWrapper, ExtendedDatabaseWrapper, SMTPEmailSender, ExtendedFileSystemWrapper
│   ├── Models/
│   │   └── Configuration/                  # App.config section models
│   ├── Properties/                         # Publish profiles
│   ├── Services/                           # ApplicationService, TimerService, ServerService, CommandService, LogService, etc.
│   └── Content/                            # Static assets (Logo.ico)
├── Server Backup Tool.API/                 # REST API (log access + command queue)
│   ├── Abstractions/                       # ILoggerService, IExtendedDatabase, IExtendedFileSystem (API-specific)
│   ├── Controllers/                        # LogsController, CommandsController, WebhooksController
│   ├── Entities/                           # LogLevel, LogType enums
│   ├── Filters/                            # RequestLoggingFilter, ResponseLoggingFilter
│   ├── Functions/                          # IPAddressFunction
│   ├── Implementations/                    # ClientAuthHandler, ExtendedDatabaseWrapper, LoggerServiceWrapper, WebhookRegistrationService, WebhookDispatchService
│   ├── Models/
│   │   ├── Requests/                       # WebhookRegistrationRequestModel
│   │   └── Responses/                      # CommandResponseModel, LogsResponseModel, LogArchivesResponseModel, ArchivedLogsResponseModel, FailureModel, SuccessModel, WebhookRegistrationResponseModel
│   │       └── Related/                    # LogModel, ArchivedLogModel, FileLogModel
│   ├── Services/                           # LogService, CommandService, LoggerService, LogPollingService
├── Server Backup Tool.Installer/            # TUI installer (install, update, configure, uninstall)
│   ├── Abstractions/                       # ILoggerService, IConfigWriter, IDatabaseInitialiser, ITaskSchedulerService, IVersionService, IFileService, IRegistryService
│   ├── Functions/                          # (none — shared functions live in Common)
│   ├── Implementations/                    # LoggerServiceWrapper, ConfigWriter, DatabaseInitialiser, TaskSchedulerService, VersionService, FileService, RegistryService
│   ├── Models/                             # InstallOptionsModel, VersionInfoModel, CustomTimerModel
│   │   └── Related/                       # ServerConfigModel, TimerConfigModel, EmailConfigModel, ApiConfigModel, etc.
│   ├── Modes/                              # InstallMode, UpdateMode, ConfigureMode, UninstallMode
│   ├── Steps/                              # ComponentSelectionStep, LocationStep, ServerConfigStep, TimerConfigStep, etc.
│   └── Values/                             # InstallerValues
├── Server Backup Tool.Common/              # Shared library
│   ├── Abstractions/                       # IClock, IDatabase, IFileSystem
│   ├── Entities/                           # TargetType
│   ├── Functions/                          # ParameterFunction
│   ├── Implementations/                    # SystemClockProvider, DatabaseWrapper, FileSystemWrapper
│   ├── Models/                             # DatabaseOptionsModel
│   │   └── Requests/                       # CommandRequestModel
│   └── Values/                             # StandardValues
├── Tests/
│   ├── Server Backup Tool.UnitTests/       # Unit tests (no I/O, no HTTP)
│   │   ├── API/Functions/                  # IPAddressFunctionTest
│   │   ├── Common/Functions/               # ParameterFunctionTest, HashFunctionTest
│   │   └── Tool/
│   │       ├── Converters/                 # JobConverterTest, ServerConverterTest, TimeConverterTest
│   │       └── Services/                   # TimerServiceTest
│   ├── Server Backup Tool.IntegrationTests/ # Integration tests (HTTP + file system)
│   │   ├── API/
│   │   │   ├── Controllers/                # GetLogsTest, PostCommandsTest, WebhooksTest, GetLogArchivesTest, GetArchivedLogsTest
│   │   │   ├── Fixtures/                   # CustomWebApplicationFactory
│   │   │   ├── Helpers/                    # AuthHelper, TestDataSeeder
│   │   │   └── Implementations/            # ClientAuthHandlerTest
│   │   ├── Tool/
│   │   │   ├── Helpers/                    # ConfigurationHelper, DirectoryHelper
│   │   │   ├── Mocks/                      # Mock data (Configs/, Server/)
│   │   │   └── Services/                   # JobServiceTest, EmailServiceTest, PidFileServiceTest, ServerServiceTest
│   │   └── Installer/
│   │       ├── Services/                   # ConfigWriterTest, FileServiceTest, VersionServiceTest, ResourceServiceTest, RegistryServiceTest, TaskSchedulerServiceTest
│   │       ├── Steps/                      # ComponentSelectionStepTest, TimerConfigStepTest
│   │       └── ConfigGenerationTest        # End-to-end config roundtrip tests
│   └── Server Backup Tool.PersistenceTests/ # Database persistence tests (in-memory SQLite)
│       ├── API/
│       │   ├── Implementations/            # ExtendedDatabaseWrapperTest
│       │   └── Services/                   # LogServiceTest, CommandServiceTest, WebhookRegistrationServiceTest
│       ├── Tool/
│       │   ├── Implementations/            # ExtendedDatabaseWrapperTest
│       │   └── Services/                   # CommandServiceTest, LogServiceTest
│       └── Installer/
│           └── Services/                   # DatabaseInitialiserTest
└── .github/workflows/                      # CI/CD pipeline definitions
```

## Application Architecture

### Application Type

The application is a **.NET 10.0 console application** that runs as a long-lived process alongside a game server. It launches the game server as a child process with redirected I/O, monitors its output, and manages scheduled operations.

The solution also includes a .NET 10.0 Web API (Server Backup Tool.API) that provides HTTP access to the tool's log data and a command queue. It uses SQLite for persistence, Basic authentication, and Scalar for API documentation. A TUI installer (Server Backup Tool.Installer) handles installation, configuration, updating, and uninstallation via Spectre.Console. A shared library (Server Backup Tool.Common) contains abstractions and implementations used by the console app, the API, and the installer.

### Dependency Injection

External dependencies are wrapped behind interfaces to support testability. Services are instantiated manually rather than through a DI container.

**Console App (Server Backup Tool):**

| Abstraction | Implementation | Purpose |
|---|---|---|
| `ILoggerService` | `LoggerServiceWrapper` | Application and server logging via log4net |
| `IExtendedDatabase` | `ExtendedDatabaseWrapper` | SQLite database operations (QuerySingle) — extends Common `IDatabase` |
| `IExtendedFileSystem` | `ExtendedFileSystemWrapper` | File system and ZIP archive operations |
| `IEmailSender` | `SMTPEmailSender` | SMTP email delivery |
| `ICommandReader` | `ConsoleCommandReader` | Console input abstraction for testability |
| `IEmailService` | `EmailService` | Email trigger matching and dispatch |
| `IJobService` | `JobService` | Backup, archive, and cleanup job execution |
| `IPingProvider` | `PingProvider` | ICMP ping for heartbeat monitoring |

**Common (Server Backup Tool.Common):**

| Abstraction | Implementation | Purpose |
|---|---|---|
| `IClock` | `SystemClockProvider` | UTC time operations |
| `IDatabase` | `DatabaseWrapper` | SQLite database operations (ExecuteNonQuery) |
| `IFileSystem` | `FileSystemWrapper` | Basic file system operations |

**API (Server Backup Tool.API):**

| Abstraction | Implementation | Purpose |
|---|---|---|
| `ILoggerService` | `LoggerServiceWrapper` | Request-scoped API logging via log4net |
| `IExtendedDatabase` | `ExtendedDatabaseWrapper` | SQLite database operations (Query, ExecuteScalar) — extends Common `IDatabase` |
| `IExtendedFileSystem` | `ExtendedFileSystemWrapper` | Archive file access |
| `IWebhookRegistrationService` | `WebhookRegistrationService` | Webhook CRUD operations |
| `IWebhookDispatchService` | `WebhookDispatchService` | Webhook payload delivery with HMAC signing |

**Installer (Server Backup Tool.Installer):**

| Abstraction | Implementation | Purpose |
|---|---|---|
| `ILoggerService` | `LoggerServiceWrapper` | Installer logging via log4net |
| `IConfigWriter` | `ConfigWriter` | App.config and appsettings.json generation |
| `IDatabaseInitialiser` | `DatabaseInitialiser` | SQLite database creation and schema setup |
| `ITaskSchedulerService` | `TaskSchedulerService` | Windows Task Scheduler registration |
| `IVersionService` | `VersionService` | Version detection and comparison |
| `IFileService` | `FileService` | File copy, backup, and permission validation |
| `IResourceService` | `ResourceService` | Embedded ZIP resource extraction for binary deployment |
| `IExtendedFileSystem` | `ExtendedFileSystemWrapper` | File copy and file system operations — extends Common `IFileSystem` |
| `IRegistryService` | `RegistryService` | Windows Registry Add/Remove Programs integration |

All Steps and Modes accept `IAnsiConsole` as the first constructor parameter for testability. Steps and Modes can be tested using `Spectre.Console.Testing.TestConsole`.

### Services

| Service | Responsibility |
|---|---|
| `ApplicationService` | Top-level orchestrator for server lifecycle, backup workflow, command processing, and user input |
| `TimerService` | Manages heartbeat, backup, wait, queued command check, and custom timers |
| `ServerService` | Game server process management and output monitoring |
| `JobService` | Backup creation, log archival, and old file cleanup |
| `EmailService` | Email construction, trigger matching, and SMTP delivery |
| `LoggerService` | Internal log4net adapter with dual loggers (tool and server) and database persistence |
| `CommandService` | Command queue operations (get, log, delete) via SQLite |
| `LogService` | Log message persistence and clearing via SQLite |
| `PidFileService` | Process ID file management for server instance tracking |

### API Services

| Service | Responsibility |
|---|---|
| `LogService` | Log retrieval with filtering, pagination, and archive access |
| `CommandService` | Command queue insertion |
| `LoggerService` | log4net adapter with request-scoped log file management |
| `WebhookRegistrationService` | Webhook registration CRUD (register, unregister, get all, update cursor) |
| `WebhookDispatchService` | Sends webhook payloads via HTTP POST with HMAC-SHA256 signing and retry |
| `LogPollingService` | Background service that polls for new logs and dispatches to registered webhooks |

### Converters

| Converter | Responsibility |
|---|---|
| `ServerConverter` | Game-specific server commands (stop, message, final message detection) |
| `JobConverter` | Game-specific backup source and destination paths |
| `TimeConverter` | Calculates duration between current time and a scheduled trigger time |

### Functions

| Function | Responsibility |
|---|---|
| `ConsoleFunction` | TextWriter wrapper that intercepts console output, ensuring all messages pass through log4net |

### API Controllers

| Controller | Route | Methods | Description |
|---|---|---|---|
| `LogsController` | `/logs` | GET | Retrieve live and archived logs with filtering |
| `CommandsController` | `/commands` | POST | Queue commands for the tool or server |
| `WebhooksController` | `/webhooks` | POST, DELETE | Register and unregister webhook endpoints |

### API Filters

| Filter | Type | Purpose |
|---|---|---|
| `RequestLoggingFilter` | `IAsyncResourceFilter` | Logs incoming request method, path, body, and query parameters |
| `ResponseLoggingFilter` | `IAsyncResultFilter` | Logs outgoing response status codes |

### API Functions

| Function | Purpose |
|---|---|
| `IPAddressFunction` | Extracts client IP from CF-Connecting-IP, X-Forwarded-For, or RemoteIpAddress |

### Common Functions

| Function | Purpose |
|---|---|
| `ParameterFunction` | Formats model properties into log-friendly strings via reflection |
| `HashFunction` | SHA-512 hashing utility used by the API and installer |

### Common Values

| Class | Purpose |
|---|---|
| `StandardValues` | Shared constant values (logger levels) used across projects |

### Common IFileSystem Methods

| Method | Purpose |
|---|---|
| `GetFiles(path)` | List files in a directory |
| `GetFiles(path, pattern, searchOption)` | List files matching a pattern with search option |
| `DirectoryExists(path)` | Check if a directory exists |
| `CreateDirectory(path)` | Create a directory |
| `DeleteDirectory(path, recursive)` | Delete a directory, optionally recursive |
| `GetCreationTime(file)` | Get file creation timestamp |
| `FileExists(path)` | Check if a file exists |
| `DeleteFile(file)` | Delete a file |
| `ReadAllText(file)` | Read all text from a file (async) |
| `WriteAllText(path, content)` | Write text to a file (async) |

### Installer Modes

| Mode | CLI Argument | Description |
|---|---|---|
| `InstallMode` | `--install` | Full installation wizard with 9 interactive steps |
| `UpdateMode` | `--update` | Detects existing install, compares versions, backs up configs, replaces binaries |
| `ConfigureMode` | `--configure` | Edits existing App.config via interactive menus |
| `UninstallMode` | `--uninstall` | Component-level uninstall with optional database and log cleanup |

ConfigureMode captures a snapshot of both the App.config and API settings before editing begins. On save, it compares the current values against the originals and displays a diff table showing each changed setting with its old and new value.

### Installer Steps

| Step | Responsibility |
|---|---|
| `ComponentSelectionStep` | Select SBT (required) and optional API component |
| `LocationStep` | Choose install directory with write permission validation |
| `ServerConfigStep` | Server name, game type, directory, start file, IP address |
| `TimerConfigStep` | Backup time, time zone, custom timers |
| `EmailConfigStep` | Optional SMTP and email template configuration |
| `ApiConfigStep` | API port, credentials generation (SHA-512 hashed), webhook secret |
| `ConfirmationStep` | Summary table and user confirmation |
| `FileDeployStep` | File copy, config generation, database creation, task registration |
| `ConfigGenerationStep` | Standalone App.config and appsettings.json generation |
| `DatabaseSetupStep` | SQLite database creation with schema |
| `ScheduledTaskStep` | Windows Task Scheduler registration |
| `ValidationStep` | Post-install checks with pass/fail report |

### Installer Values

| Class | Purpose |
|---|---|
| `InstallerValues` | Constants for registry paths, scheduled task settings, database SQL, and defaults |

### API Authentication

- Basic authentication via `ClientAuthHandler`
- Credentials hashed with SHA512 and compared against configured values

### Application Lifecycle

#### Startup

1. Initialise `EmailService` and configure console output filter
2. Configure log4net from `App.config`
3. Register process exit handler
4. Load `serverBackup` configuration section
5. Send "Open" notification email (if configured)
6. Create `ApplicationService` (initialises database connection, `LogService`, `CommandService`, and sets `LogService` on the logger for database persistence)
7. Begin execution

#### Runtime

1. Calculate timer durations from configured trigger times
2. Set and start all timers (heartbeat, backup, queued command check, custom)
3. Launch game server process with redirected I/O
4. Write PID file to `%PROGRAMDATA%`
5. Enter user input loop — commands are queued to the database and processed asynchronously via the queued command check timer

#### Backup Workflow

1. Send stop command to the game server
2. Wait 30 seconds via the Wait timer
3. Wait for server process to fully close
4. Create ZIP backup of the game world directory
5. Archive log files into a dated ZIP
6. Delete backups and archived logs older than 10 days
7. Restart the full application cycle

#### Shutdown

1. User enters `exit app` command (queued to database)
2. Queued command check timer picks up the exit command
3. Stop command sent to the server (with 30-second wait if running)
4. Process exit handler sends "Close" notification email, clears tool logs from database
5. PID file deleted

### Timer System

| Timer | Interval | Purpose |
|---|---|---|
| Heartbeat | 5 seconds | Pings the server IP; sends heartbeat email and stops if unreachable |
| Wait | 30 seconds | Delay before backup completion (activated on demand) |
| Backup | Calculated from config | Triggers the backup workflow at the configured time |
| QueuedCommandCheck | Configured (`PollingIntervalMs`) | Polls database for commands queued by the API or console input |
| Custom Timers | Calculated from config | Sends configured messages to the server at scheduled times |

### Console Commands

| Command | Action |
|---|---|
| `exit app` | Queued to database; processed by timer to gracefully stop the server and exit |
| `start server` | Queued to database; processed by timer to start the server if not running |
| `reset heartbeat` | Queued to database; processed by timer to restart the heartbeat timer |
| Any other input | Queued to database as a server command; processed by timer and sent to the server process |

## Supported Games

| Game | Stop Command | Message Command | Final Message | Backup Source |
|---|---|---|---|---|
| Minecraft | `stop` | `/say {message}` | `{WorkingDirectory}>PAUSE` | `{ServerPath}\world` |

## Data Persistence

Both the console app and the API use **SQLite** for structured data persistence (logs and commands). The console app also uses file-based persistence for backups, archived logs, and PID files.

### SQLite Tables

| Table | Columns | Purpose |
|---|---|---|
| `Logs` | Id, ServerName, Timestamp, Level, Type, Message | Stores tool and server log entries |
| `Commands` | Id, ServerName, Target, Command, CreatedAt | Command queue for tool/server actions |
| `Webhooks` | Id, URL, ServerName, LogType, LogLevel, AfterId, CreatedAt | Registered webhook endpoints for log notifications |

### File-Based Persistence

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
    <section name="serverBackup" type="ServerBackupTool.Models.Configuration.SBTSection, ServerBackupTool" />
  </configSections>

  <serverBackup>
    <serverDetails name="<server name>"
                   game="<Minecraft>"
                   location="<path to server directory>"
                   startFile="<server executable>"
                   ipAddress="<server IP for heartbeat>" />
    <databaseDetails path="<path to SQLite database>"
                     pollingInterval="<milliseconds between command queue polls>" />
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
                username="<SMTP auth username, defaults to from email if empty>"
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

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Authentication": {
    "ClientId": "<SHA-512 hash of the client ID>",
    "ClientSecret": "<SHA-512 hash of the client secret>"
  },
  "Database": {
    "Path": "<Location and name of the database>",
    "ServerName": "<Name of the server the API is for>",
    "PollingIntervalMs": 1000
  },
  "ArchiveSettings": {
    "ArchiveDirectory": "<Location of .zip log files>"
  },
  "Webhook": {
    "Secret": "<HMAC-SHA256 secret key>",
    "TimeoutSeconds": 10,
    "MaxRetries": 3
  }
}
```

## Logging

- **Framework:** log4net 3.3.2
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

Console output is intercepted by `ConsoleFunction`, a custom `TextWriter` wrapper. It strips the `log4net - ` prefix from messages that pass through log4net, and flags any output that bypasses the logging pipeline as an error.

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

All workflows run on `windows-latest` using .NET 10.0 SDK.

| Workflow | Trigger | Steps |
|---|---|---|
| **CI on Commit** (`Commit.yml`) | Push to any branch | Checkout, Restore, Build (Release) |
| **CI on Pull Request** (`Pull Request.yml`) | PR to any branch | Download and start Papercut SMTP, Checkout, Restore, Build (Release), Run Tests with Coverage (Coverlet), Generate Coverage Report (ReportGenerator), Post Coverage Status to GitHub, Output Coverage to Job Summary, Stop Papercut SMTP |
| **Check for Linked Issue** (`PR Linked Issue.yml`) | PR opened/edited/reopened/synchronised | Verifies PR has linked GitHub issues via description, comments, or Development section |

### Pull Request Test Infrastructure

The Pull Request workflow downloads and starts [Papercut SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP), a local mock SMTP server, to enable integration testing of email functionality without requiring an external mail server.

### Build Configuration

- **SDK:** .NET 10.0
- **Configuration:** Release
- **Test Runner:** `dotnet test` (MSTest)

## Installer

### Overview

The Server Backup Tool Installer is a .NET 10.0 TUI console application built with Spectre.Console. It provides four modes: Install, Update, Configure, and Uninstall. The installer can be run interactively (mode selection menu) or via CLI arguments (`--install`, `--update`, `--configure`, `--uninstall`). Multiple installations can coexist on the same machine — each installation uses per-server registry keys (`ServerBackupTool_{ServerName}`) and configurable scheduled task names to avoid conflicts. Uninstall supports component-level removal: "Everything (Tool and API)" or "API only". When an API component is selected, it installs to a separate directory (`{InstallPath}.API`).

### Installation Workflow

1. Component selection (SBT + optional API)
2. Install location with write permission validation
3. Server configuration (name, game, directory, start file, IP) and scheduled task naming
4. Backup and timer configuration
5. Optional email notification setup
6. Optional API configuration with credential generation
7. Confirmation summary
8. File deployment with progress display (binaries, config, database, scheduled tasks, registry)
9. Post-install validation

### Distribution

Published as a self-contained single-file executable with the tool and API binaries embedded as ZIP resources. The build script (`build-installer.ps1`) orchestrates the publish order:

1. Publish `Server Backup Tool` as self-contained for `win-x64`
2. Publish `Server Backup Tool.API` as self-contained for `win-x64`
3. Package both publish outputs as versioned ZIPs (`Tool_X.Y.Z.zip` and `API_X.Y.Z.zip`) in the installer's `Resources/` directory
4. Publish the installer with embedded resources as a single-file executable

```powershell
.\build-installer.ps1
```

At install time, the `ResourceService` extracts the embedded ZIPs to the install directory using `System.IO.Compression.ZipArchive`. Versions are parsed from the resource filenames (e.g., `Tool_2.0.2.zip` yields version `2.0.2`). The embedded resources are conditionally included in the csproj — they are only present after running the build script.

### Installer Logging

- **Framework:** log4net 3.3.2
- **Configuration:** Programmatic (no config file)
- **Appender:** RollingFileAppender writing to `Logs\Installer.log`
- **Format:** `{ISO8601 Timestamp} {LEVEL} - {Message}`
- **Max File Size:** 10 MB, 10 rolling backups

### API HTTPS / Kestrel Configuration

The installer generates a `Kestrel` section in `appsettings.json` for HTTP/HTTPS binding. If HTTPS is enabled during install, the user selects a certificate format (PFX or PEM) and the generated config includes the appropriate HTTPS endpoint configuration. The installer validates that the certificate contains a private key before proceeding.

- **PFX:** `"Certificate": { "Path": "...", "Password": "..." }`
- **PEM:** `"Certificate": { "Path": "...", "KeyPath": "..." }`

The API uses Kestrel's endpoint configuration to bind to the specified addresses and ports.

### Registry Integration

The installer writes to Add/Remove Programs using per-server registry keys:

| Key | Value |
|---|---|
| Path | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ServerBackupTool_{ServerName}` |
| DisplayName | Server Backup Tool - {ServerName} |
| Publisher | Hunter Industries |
| UninstallString | `"{InstallPath}\ServerBackupToolInstaller.exe" --uninstall` |
| ServerName | Name of the server this installation manages |
| InstallLocation | Tool install directory |
| ApiInstallLocation | API install directory (empty if API not installed) |
| ToolVersion | Installed tool version |
| ApiVersion | Installed API version (empty if API not installed) |
| ToolTaskName | User-configured scheduled task name for the backup tool |
| ApiTaskName | User-configured scheduled task name for the API |

### Scheduled Tasks

The installer registers one or two Windows scheduled tasks depending on the selected components. Task names are configurable during install (defaults: `Server Backup Tool - {ServerName}` and `Server Backup Tool API - {ServerName}`) to support multiple installations on the same machine.

**Tool Task:**

| Setting | Value |
|---|---|
| Name | Configurable (default: `Server Backup Tool - {ServerName}`) |
| Trigger | At system startup (1-minute delay) |
| Action | Run `ServerBackupTool.exe` |
| Principal | Current user, S4U logon (run whether logged on or not), least privilege |
| Restart on failure | Every 1 minute, up to 3 times |
| Execution time limit | Disabled |
| Start on AC power only | Yes |
| Stop on battery | Yes |
| Allow hard terminate | Yes |
| Multiple instances | Ignore new |
| Start when available | Yes |

**API Task (if API component selected):**

| Setting | Value |
|---|---|
| Name | Configurable (default: `Server Backup Tool API - {ServerName}`) |
| Trigger | At system startup (1-minute delay) |
| Action | Run `ServerBackupTool.API.exe` |
| Principal | Current user, S4U logon (run whether logged on or not), least privilege |
| Restart on failure | Every 1 minute, up to 3 times |
| Execution time limit | Disabled |
| Start on AC power only | Yes |
| Stop on battery | Yes |
| Allow hard terminate | Yes |
| Multiple instances | Ignore new |
| Start when available | Yes |

### Uninstall Paths

The uninstaller supports two removal paths when an API component is installed:

**Everything (Tool and API):**

1. Remove tool and API scheduled tasks
2. Prompt to keep or delete the database file
3. Prompt to keep or delete log files (application logs and archived logs)
4. Remove registry entry
5. Delete tool install directory and API install directory

**API only:**

1. Remove API scheduled task
2. Delete API install directory
3. Update registry entry to clear API fields (`ApiInstallLocation`, `ApiVersion`, `ApiTaskName`) while preserving the tool installation

## Hosting Requirements

### Runtime Prerequisites

- .NET 10.0 Runtime
- Windows (required for game server process management)

### Network Requirements

- ICMP access to the game server IP address (for heartbeat pings)
- Outbound SMTP (configurable port, default 587) for email notifications
- HTTP port access for the API
- Outbound HTTPS to registered webhook URLs (for log notifications)

### File System Requirements

- Read/write access to the game server directory (for process management and backups)
- Read/write access to the `Logs/` directory
- Read/write access to the `Archived Logs/` directory
- Read/write access to `%PROGRAMDATA%\Hunter Industries\Server Backup Tool` (for PID files)
- SQLite file read/write access
