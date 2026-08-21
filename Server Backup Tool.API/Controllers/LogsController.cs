// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Entities;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Models.Responses;
using ServerBackupTool.API.Models.Responses.Related;
using ServerBackupTool.API.Services;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;

namespace ServerBackupTool.API.Controllers
{
    [ApiController]
    [Authorize]
    public class LogsController : ControllerBase
    {
        private readonly ILoggerService _Logger;
        private readonly IDatabase _Database;
        private readonly IExtendedFileSystem _FileSystem;
        private readonly DatabaseOptionsModel Options;
        private readonly ArchiveSettingsModel Archive;

        // Set's the class's global variables.
        public LogsController(
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
        /// Gets the live logs from the database.
        /// </summary>
        [HttpGet("logs", Name = "GetLogs")]
        [EndpointDescription("Fetches a list of live logs from the database that match the given parameters.")]
        [ProducesResponseType(typeof(LogsResponseModel), 200)]
        [ProducesResponseType(typeof(SuccessModel), 204)]
        [ProducesResponseType(typeof(FailureModel), 400)]
        [ProducesResponseType(typeof(FailureModel), 401)]
        [ProducesResponseType(typeof(FailureModel), 500)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string level = "All",
            [FromQuery] string type = "All",
            [FromQuery] int limit = 100,
            [FromQuery] int afterId = 0)
        {
            LogService _logService = new(
                _Logger,
                _Database,
                _FileSystem,
                Options,
                Archive);

            if (!Enum.TryParse<Entities.LogLevel>(
                level,
                true,
                out Entities.LogLevel logLevel))
            {
                return StatusCode(
                    400,
                    new FailureModel()
                    {
                        Error = $"\"{level}\" is not a valid log level."
                    });
            }

            if (!Enum.TryParse<LogType>(
                type,
                true,
                out LogType logType))
            {
                return StatusCode(
                    400,
                    new FailureModel()
                    {
                        Error = $"\"{type}\" is not a valid log type."
                    });
            }

            (List<LogModel>? logs, Exception? ex) = await _logService.GetLogs(
                logLevel,
                logType,
                limit,
                afterId);

            if (ex != null)
            {
                return StatusCode(
                    500,
                    new FailureModel()
                    {
                        Error = $"Something went wrong during an operation. Please see log files for details quoting {_Logger.RequestId}."
                    });
            }

            if (logs == null)
            {
                return StatusCode(
                    204,
                    new SuccessModel()
                    {
                        Information = "No live logs found in the database that match the given parameters."
                    });
            }

            LogsResponseModel response = new()
            {
                ServerName = Options.ServerName,
                Logs = logs,
                NextAfter = logs.Count == limit ? logs.First().Id : null
            };

            return StatusCode(
                200,
                response);
        }

        /// <summary>
        /// Gets the log archives from the archive store.
        /// </summary>
        [HttpGet("logs/archived", Name = "GetLogArchives")]
        [EndpointDescription("Fetches a list of available log archives from the archive directory.")]
        [ProducesResponseType(typeof(LogArchivesResponseModel), 200)]
        [ProducesResponseType(typeof(SuccessModel), 204)]
        [ProducesResponseType(typeof(FailureModel), 401)]
        [ProducesResponseType(typeof(FailureModel), 500)]
        public async Task<IActionResult> GetLogArchives()
        {
            LogService _logService = new(
                _Logger,
                _Database,
                _FileSystem,
                Options,
                Archive);

            (List<ArchivedLogModel>? logArchives, Exception? ex) = _logService.GetLogArchives();

            if (ex != null)
            {
                return StatusCode(
                    500,
                    new FailureModel()
                    {
                        Error = $"Something went wrong during an operation. Please see log files for details quoting {_Logger.RequestId}."
                    });
            }

            if (logArchives == null)
            {
                return StatusCode(
                    204,
                    new SuccessModel()
                    {
                        Information = "No log archives found in the archive directory."
                    });
            }

            LogArchivesResponseModel response = new()
            {
                ServerName = Options.ServerName,
                Archives = logArchives
            };

            return StatusCode(
                200,
                response);
        }

        /// <summary>
        /// Gets thes logs from the given archive.
        /// </summary>
        [HttpGet("logs/archived/{file}", Name = "GetArchivedLogs")]
        [EndpointDescription("Fetches the all the logs in the given log archive.")]
        [ProducesResponseType(typeof(ArchivedLogsResponseModel), 200)]
        [ProducesResponseType(typeof(SuccessModel), 204)]
        [ProducesResponseType(typeof(FailureModel), 400)]
        [ProducesResponseType(typeof(FailureModel), 401)]
        [ProducesResponseType(typeof(FailureModel), 500)]
        public async Task<IActionResult> GetArchivedLogs([FromRoute] string file)
        {
            LogService _logService = new(
                _Logger,
                _Database,
                _FileSystem,
                Options,
                Archive);

            if (!file.Contains(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                return StatusCode(
                    400,
                    new FailureModel()
                    {
                        Error = "The provided file must include the \".zip\" extension."
                    });
            }

            if (file.Contains(Path.DirectorySeparatorChar))
            {
                return StatusCode(
                    400,
                    new FailureModel()
                    {
                        Error = "The provided file must only include the name of the file with the \".zip\" extension."
                    });
            }

            (List<FileLogModel>? archivedLogs, Exception? ex) = await _logService.GetArchivedLogs(file);

            if (ex != null)
            {
                return StatusCode(
                    500,
                    new FailureModel()
                    {
                        Error = $"Something went wrong during an operation. Please see log files for details quoting {_Logger.RequestId}."
                    });
            }

            if (archivedLogs == null)
            {
                return StatusCode(
                    204,
                    new SuccessModel()
                    {
                        Information = $"No logs found in the archive {file}."
                    });
            }

            ArchivedLogsResponseModel response = new()
            {
                ServerName = Options.ServerName,
                ArchiveName = file,
                Logs = archivedLogs
            };

            return StatusCode(
                200,
                response);
        }
    }
}
