// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Entities;
using ServerBackupTool.API.Models.Requests;
using ServerBackupTool.API.Models.Responses;
using ServerBackupTool.API.Services;
using ServerBackupTool.Common.Abstractions;
using ServerBackupTool.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace ServerBackupTool.API.Controllers
{
    [ApiController]
    [Route("webhooks")]
    [Authorize]
    public class WebhooksController : ControllerBase
    {
        private readonly ILoggerService _Logger;
        private readonly IExtendedDatabase _Database;
        private readonly IClock _Clock;
        private readonly DatabaseOptionsModel Options;

        // Set's the class's global variables.
        public WebhooksController(
            ILoggerService _logger,
            IExtendedDatabase _database,
            IClock _clock,
            DatabaseOptionsModel options)
        {
            _Logger = _logger;
            _Database = _database;
            _Clock = _clock;
            Options = options;
        }

        /// <summary>
        /// Registers a new webhook.
        /// </summary>
        [HttpPost]
        [EndpointDescription("Registers a webhook to receive realtime logs.")]
        [ProducesResponseType(typeof(WebhookRegistrationResponseModel), 201)]
        [ProducesResponseType(typeof(FailureModel), 400)]
        [ProducesResponseType(typeof(FailureModel), 401)]
        [ProducesResponseType(415)]
        [ProducesResponseType(typeof(FailureModel), 500)]
        public async Task<IActionResult> Register([FromBody, Required] WebhookRegistrationRequestModel registration)
        {
            WebhookRegistrationService _webhookService = new(
                _Logger,
                _Database,
                _Clock,
                Options);

            if (!Enum.TryParse<LogType>(
                registration.LogType,
                true,
                out LogType logType))
            {
                return StatusCode(
                    400,
                    new FailureModel()
                    {
                        Error = $"\"{registration.LogType}\" is not a valid log type."
                    });
            }

            if (!Enum.TryParse<Entities.LogLevel>(
                registration.LogLevel,
                true,
                out Entities.LogLevel logLevel))
            {
                return StatusCode(
                    400,
                    new FailureModel()
                    {
                        Error = $"\"{registration.LogLevel}\" is not a valid log level."
                    });
            }

            if (!Uri.TryCreate(
                registration.Url,
                UriKind.Absolute,
                out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return StatusCode(
                    400,
                    new FailureModel()
                    {
                        Error = "The provided URL is not a valid HTTP or HTTPS URL."
                    });
            }

            (string? webhookId, Exception? ex) = await _webhookService.Register(registration);

            if (webhookId == null || ex != null)
            {
                return StatusCode(
                    500,
                    new FailureModel()
                    {
                        Error = $"Something went wrong during an operation. Please see log files for details quoting {_Logger.RequestId}."
                    });
            }

            WebhookRegistrationResponseModel response = new()
            {
                Id = webhookId,
                ServerName = registration.ServerName!
            };

            return StatusCode(
                201,
                response);
        }

        /// <summary>
        /// Unregisters an existing webhook.
        /// </summary>
        [HttpDelete("{webhookId}")]
        [EndpointDescription("Removes a registered webhook from receiving realtime logs.")]
        [ProducesResponseType(typeof(SuccessModel), 200)]
        [ProducesResponseType(typeof(FailureModel), 401)]
        [ProducesResponseType(typeof(FailureModel), 404)]
        [ProducesResponseType(typeof(FailureModel), 500)]
        public async Task<IActionResult> Unregister([FromRoute] string webhookId)
        {
            WebhookRegistrationService _webhookService = new(
                _Logger,
                _Database,
                _Clock,
                Options);

            (bool removed, Exception? ex) = await _webhookService.Unregister(webhookId);

            if (ex != null)
            {
                return StatusCode(
                    500,
                    new FailureModel()
                    {
                        Error = $"Something went wrong during an operation. Please see log files for details quoting {_Logger.RequestId}."
                    });
            }

            if (!removed)
            {
                return StatusCode(
                    404,
                    new FailureModel()
                    {
                        Error = $"No webhook found with the ID \"{webhookId}\"."
                    });
            }

            return StatusCode(
                200,
                new SuccessModel()
                {
                    Information = $"Webhook \"{webhookId}\" has been unregistered."
                });
        }
    }
}
