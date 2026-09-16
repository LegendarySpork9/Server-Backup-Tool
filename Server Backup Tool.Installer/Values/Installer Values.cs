// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Values
{
    public static class InstallerValues
    {
        public static class Registry
        {
            public const string UninstallKeyBase = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            public const string UninstallKeyPrefix = "ServerBackupTool";
            public const string DisplayName = "Server Backup Tool";
            public const string Publisher = "Hunter Industries";
        }

        public static class ScheduledTask
        {
            public const string TaskName = "Server Backup Tool";
            public const int RestartDelayMinutes = 1;
            public const int MaxRestartAttempts = 3;
        }

        public static class Database
        {
            public const string CreateTablesSql = @"
PRAGMA journal_mode=WAL;

CREATE TABLE IF NOT EXISTS Logs (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ServerName  TEXT    NOT NULL,
    Timestamp   TEXT    NOT NULL,
    Level       TEXT    NOT NULL,
    Type        TEXT    NOT NULL,
    Message     TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS Commands (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ServerName  TEXT    NOT NULL,
    Target      TEXT    NOT NULL,
    Command     TEXT    NOT NULL,
    CreatedAt   TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Logs_Server ON Logs (ServerName, Id);

CREATE INDEX IF NOT EXISTS IX_Commands_Server ON Commands (ServerName);

CREATE TABLE IF NOT EXISTS Webhooks (
    Id          TEXT    PRIMARY KEY,
    URL         TEXT    NOT NULL,
    ServerName  TEXT    NOT NULL,
    LogType     TEXT    NOT NULL,
    LogLevel    TEXT    NOT NULL,
    AfterId     INTEGER NOT NULL DEFAULT 0,
    CreatedAt   TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Webhooks_Id ON Webhooks (Id);

CREATE INDEX IF NOT EXISTS IX_Webhooks_Server ON Webhooks (ServerName);";
        }

        public static class Resources
        {
            public const string ToolPrefix = "Tool_";
            public const string ApiPrefix = "API_";
            public const string ZipExtension = ".zip";
        }

        public static class Defaults
        {
            public const string InstallPath = @"C:\Server Backup Tool";
            public const string ToolConfigFileName = "Server Backup Tool.dll.config";
            public const string DatabaseFileName = "Data.db";
            public const string ArchiveDirectory = "Archived Logs";
            public const string FromName = "Server Backup Tool";
            public const int SmtpPort = 587;
            public const bool EnableSSL = true;
            public const int PollingIntervalMs = 1000;
            public const string ApiBindAddress = "0.0.0.0";
            public const int ApiHttpPort = 5000;
            public const int ApiHttpsPort = 5001;
            public const int WebhookTimeoutSeconds = 10;
            public const int WebhookMaxRetries = 3;
            public static readonly string ProgramDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Hunter Industries",
                "Server Backup Tool");
        }

        public static readonly string[] Games =
        [
            "Minecraft"
        ];
    }
}
