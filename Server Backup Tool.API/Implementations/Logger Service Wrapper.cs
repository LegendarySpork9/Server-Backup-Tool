// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Http;
using ServerBackupTool.API.Functions;
using ServerBackupTool.API.Services;
using ServerBackupTool.API.Abstractions;

namespace ServerBackupTool.API.Implementations
{
    public class LoggerServiceWrapper : ILoggerService
    {
        private readonly IHttpContextAccessor? _ContextAccessor;
        private string _Identifier;

        public Guid RequestId { get; }

        public LoggerServiceWrapper(string identifier)
        {
            _Identifier = identifier;
            RequestId = Guid.Empty;
        }

        public LoggerServiceWrapper(IHttpContextAccessor contextAccessor)
        {
            _ContextAccessor = contextAccessor;
            _Identifier = "Unknown";
            RequestId = Guid.NewGuid();
        }

        /// <summary>
        /// Changes the identifier of the logger.
        /// </summary>
        public void ChangeIdentifier(string value) => _Identifier = value;

        /// <summary>
        /// Logs the given message to the log file.
        /// </summary>
        public void LogMessage(
            string level,
            string message)
        {
            string id = _Identifier;

            if (_ContextAccessor?.HttpContext != null)
            {
                string ip = IPAddressFunction.FetchIpAddress(_ContextAccessor.HttpContext);

                if (!string.IsNullOrEmpty(ip))
                {
                    id = ip;
                }
            }

            if (RequestId != Guid.Empty)
            {
                id = $"{id} [{RequestId}]";
            }

            LoggerService _logger = new(
                id,
                "Logs");
            _logger.LogMessage(
                level,
                message);
        }
    }
}
