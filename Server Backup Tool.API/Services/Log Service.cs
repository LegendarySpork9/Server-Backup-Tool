// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Entities;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Models.Responses.Related;
using ServerBackupTool.API.Values;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.API.Services
{
    public class LogService
    {
        private readonly ILoggerService _Logger;
        private readonly IDatabase _Database;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly DatabaseOptionsModel Options;
        private readonly ArchiveSettingsModel Archive;

        // Sets the class's global variables
        public LogService(
            ILoggerService _logger,
            IDatabase _database,
            IExtendedFileSystem _fileSystem,
            DatabaseOptionsModel options,
            ArchiveSettingsModel archive)
        {
            _Logger = _logger;
            _Database = _database;
            _FileSystem = _fileSystem;
            Options = options;
            Archive = archive;
        }

        /// <summary>
        /// Loads the logs from the database that match the parameters.
        /// </summary>
        public async Task<(List<LogModel>?, Exception?)> GetLogs(
            Entities.LogLevel level = Entities.LogLevel.All,
            LogType type = LogType.All,
            int limit = 100,
            int afterId = 0)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.GetLogs called with the parameters \"{level}\", \"{type}\", \"{limit}\", \"{afterId}\".");
            
            List<LogModel>? logs = null;
            Exception? ex = null;

            try
            {
                string sql = @"select
    Id,
    Timestamp,
    Level,
    Logger,
    Message
from [Logs]
where ServerName = @serverName";
                List<SqliteParameter> parameterList =
                [
                    new("@limit", SqliteType.Integer) { Value = limit },
                    new("@serverName", SqliteType.Text) { Value = Options.ServerName }
                ];

                if (level != Entities.LogLevel.All)
                {
                    sql += @"
and Level = @level";
                    parameterList.Add(new("@level", SqliteType.Text) { Value = level.ToString() });
                }

                if (type != LogType.All)
                {
                    sql += @"
and Logger = @type";
                    parameterList.Add(new("@type", SqliteType.Text) { Value = type.ToString() });
                }

                if (afterId > 0)
                {
                    sql += @"
and Id < @afterId";
                    parameterList.Add(new("@afterId", SqliteType.Integer) { Value = afterId });
                }

                sql += @"
order by Id desc
limit @limit";

                (List<LogModel> results, Exception? qex) = await _Database.Query(
                    sql,
                    reader =>
                    {
                        return new LogModel()
                        {
                            Id = reader.GetInt32(0),
                            Timestamp = DateTime.SpecifyKind(
                                reader.GetDateTime(1),
                                DateTimeKind.Utc),
                            Level = Enum.Parse<Entities.LogLevel>(reader.GetString(2), true),
                            Logger = Enum.Parse<LogType>(reader.GetString(3), true),
                            Message = reader.GetString(4)
                        };
                    },
                    [.. parameterList]);

                if (qex != null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "An error occured when trying to run LogService.GetLogs.");
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Error,
                        qex.ToString());

                    ex = qex;
                }

                if (results.Count > 0)
                {
                    logs = [.. results.OrderBy(r => r.Id)];
                }
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run LogService.GetLogs.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.GetLogs returned {logs?.Count ?? 0} log(s).");
            return (
                logs,
                ex);
        }

        /// <summary>
        /// Returns the archived logs from the archive store.
        /// </summary>
        public (List<ArchivedLogModel>?, Exception?) GetLogArchives()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.GetLogArchives called.");

            List<ArchivedLogModel>? archives = null;
            Exception? ex = null;

            try
            {
                IEnumerable<string> files = _FileSystem.GetFiles(Archive.ArchiveDirectory);

                if (files.Any())
                {
                    archives = [];

                    foreach (string file in files)
                    {
                        DateTime createdAt = _FileSystem.GetCreationTime(file);
                        long sizeInBytes = _FileSystem.GetFileSize(file);

                        archives.Add(new()
                        {
                            FileName = Path.GetFileName(file),
                            CreatedAt = createdAt,
                            SizeBytes = sizeInBytes
                        });
                    }
                }
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run LogService.GetLogArchives.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.GetLogArchives returned {archives?.Count ?? 0} archive(s).");
            return (
                archives,
                ex);
        }

        /// <summary>
        /// Loads the logs from the given archive.
        /// </summary>
        public async Task<(List<FileLogModel>?, Exception?)> GetArchivedLogs(string file)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.GetArchivedLogs called with the parameter \"{file}\".");

            List<FileLogModel>? logs = null;
            Exception? ex = null;
            string? extractPath = null;

            try
            {
                string archivePath = Path.Combine(
                    Archive.ArchiveDirectory,
                    file);
                extractPath = Path.Combine(
                    Archive.ArchiveDirectory,
                    Path.GetFileNameWithoutExtension(file));

                if (_FileSystem.FileExists(archivePath))
                {
                    _FileSystem.ExtractZIPToDirectory(
                        archivePath,
                        extractPath);

                    IEnumerable<string> logFiles = _FileSystem.GetFiles(extractPath)
                        .OrderBy(f => _FileSystem.GetLastWriteTime(f));

                    if (logFiles.Any())
                    {
                        logs = [];
                        int logId = 0;

                        foreach (string logFile in logFiles)
                        {
                            FileLogModel log = new()
                            {
                                FileName = Path.GetFileName(logFile),
                                Content = []
                            };

                            string[] logLines = await _FileSystem.ReadAllLines(logFile);

                            foreach (string logLine in logLines)
                            {
                                int firstDash = logLine.IndexOf(" - ");

                                if (firstDash > 0)
                                {
                                    string prefix = logLine[..firstDash];
                                    string message = logLine[(firstDash + 3)..];

                                    int lastSpace = prefix.LastIndexOf(' ');
                                    string timestamp = prefix[..lastSpace];
                                    string level = prefix[(lastSpace + 1)..];

                                    logId++;

                                    log.Content.Add(new()
                                    {
                                        Id = logId,
                                        Timestamp = DateTime.Parse(timestamp),
                                        Level = Enum.Parse<Entities.LogLevel>(level, true),
                                        Logger = LogType.Server,
                                        Message = message
                                    });
                                }
                            }

                            logs.Add(log);
                        }
                    }
                }
            }

            catch (Exception cex)
            {

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run LogService.GetArchivedLogs.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                ex = cex;
            }

            if (!string.IsNullOrWhiteSpace(extractPath) && _FileSystem.DirectoryExists(extractPath))
            {
                _FileSystem.DeleteDirectory(extractPath);
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"LogService.GetArchivedLogs returned {logs?.Sum(l => l.Content.Count) ?? 0} archived log(s).");
            return (
                logs,
                ex);
        }
    }
}
