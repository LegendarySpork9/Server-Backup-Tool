PRAGMA journal_mode=WAL;

CREATE TABLE IF NOT EXISTS Logs (
    Id  INTEGER PRIMARY KEY AUTOINCREMENT,
    ServerName  TEXT    NOT NULL,
    Timestamp   TEXT    NOT NULL,
    Level   TEXT    NOT NULL,
    Type  TEXT    NOT NULL,
    Message TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS Commands (
    Id  INTEGER PRIMARY KEY AUTOINCREMENT,
    ServerName  TEXT    NOT NULL,
    Target  TEXT    NOT NULL,
    Command TEXT    NOT NULL,
    CreatedAt   TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Logs_Server ON Logs (ServerName, Id);

CREATE INDEX IF NOT EXISTS IX_Commands_Server ON Commands (ServerName);

CREATE TABLE IF NOT EXISTS Webhooks (
    Id          TEXT    PRIMARY KEY,
    Url         TEXT    NOT NULL,
    LogType     TEXT    NOT NULL,
    LogLevel    TEXT    NOT NULL,
    AfterId     INTEGER NOT NULL DEFAULT 0,
    CreatedAt   TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Webhooks_Id ON Webhooks (Id);