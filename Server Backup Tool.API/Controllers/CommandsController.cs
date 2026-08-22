// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Models.Requests;
using ServerBackupTool.API.Models.Responses;
using ServerBackupTool.API.Services;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Entities;
using ServerBackupTool.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace ServerBackupTool.API.Controllers
{
    [ApiController]
    [Route("commands")]
    [Authorize]
    public class CommandsController : ControllerBase
    {
        private readonly ILoggerService _Logger;
        private readonly IDatabase _Database;
        private readonly IClock _Clock;
        private readonly DatabaseOptionsModel Options;

        // Set's the class's global variables.
        public CommandsController(
            ILoggerService _logger,
            IDatabase _database,
            IClock _clock,
            DatabaseOptionsModel options)
        {
            _Logger = _logger;
            _Database = _database;
            _Clock = _clock;
            Options = options;
        }

        /// <summary>
        /// Adds the command to the command table.
        /// </summary>
        [HttpPost]
        [EndpointDescription("Logs the specified command to the command queue.")]
        [ProducesResponseType(typeof(CommandResponseModel), 200)]
        [ProducesResponseType(typeof(FailureModel), 400)]
        [ProducesResponseType(typeof(FailureModel), 401)]
        [ProducesResponseType(415)]
        [ProducesResponseType(typeof(FailureModel), 500)]
        public async Task<IActionResult> Post([FromBody, Required] CommandRequestModel command)
        {
            CommandService _commandService = new(
                _Logger,
                _Database,
                _Clock,
                Options);

            if (!Enum.TryParse<TargetType>(
                command.Target,
                true,
                out TargetType target))
            {
                return StatusCode(
                    400,
                    new FailureModel()
                    {
                        Error = $"\"{command.Target}\" is not a valid target. Allowed values: Tool, Server"
                    });
            }

            (int? commandId, DateTime createdAt, Exception? ex) = await _commandService.LogCommand(command);

            if (!commandId.HasValue || ex != null)
            {
                return StatusCode(
                    500,
                    new FailureModel()
                    {
                        Error = $"Something went wrong during an operation. Please see log files for details quoting {_Logger.RequestId}."
                    });
            }

            CommandResponseModel response = new()
            {
                Id = commandId.Value,
                ServerName = Options.ServerName,
                Target = target,
                Command = command.Command!,
                CreatedAt = createdAt,
            };

            return StatusCode(
                200,
                response);
        }
    }
}
