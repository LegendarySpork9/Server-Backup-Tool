# Server-Backup-Tool [![Commit Status](https://github.com/LegendarySpork9/Server-Backup-Tool/actions/workflows/Commit.yml/badge.svg?event=push)](https://github.com/LegendarySpork9/Server-Backup-Tool/actions/workflows/Commit.yml) [![PR Status](https://github.com/LegendarySpork9/Server-Backup-Tool/actions/workflows/Pull%20Request.yml/badge.svg)](https://github.com/LegendarySpork9/Server-Backup-Tool/actions/workflows/Pull%20Request.yml)

Manages game server backups with scheduled archiving, log rotation, email notifications, and ICMP heartbeat monitoring. Includes a REST API for remote log access and command queuing, and a TUI installer for setup and configuration.

## Components

| Component | Type | Description |
|-----------|------|-------------|
| Server Backup Tool | Console App (.NET 10.0) | Long-lived process managing server lifecycle, timers, and backups |
| Server Backup Tool.API | Web API (.NET 10.0) | REST endpoints for logs, commands, and webhooks |
| Server Backup Tool.Installer | Console TUI (.NET 10.0) | Install, update, configure, and uninstall via Spectre.Console |
| Server Backup Tool.Common | Class Library (.NET 10.0) | Shared abstractions |

## Tech Stack

| Technology | Version |
|------------|---------|
| .NET | 10.0 |
| Language | C# |
| Database | SQLite |
| Installer TUI | Spectre.Console |
| API Documentation | Scalar |

## Status

Active development

## Documentation

Setup, configuration, and usage documentation is available on the [LegendarySpork9 Wiki](https://wiki.legendaryspork9.co.uk).

---

Copyright © 2024 Toby Hunter, All rights reserved.
