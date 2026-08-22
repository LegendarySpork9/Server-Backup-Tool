// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Models;
using ServerBackupTool.API.Models.Responses;
using ServerBackupTool.Common.Values;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ServerBackupTool.API.Implementations
{
    public class ClientAuthHandler : AuthenticationHandler<ClientAuthOptions>
    {
        private readonly AuthenticationModel AuthModel;
        private readonly ILoggerService _Logger;

        // Sets the class's global variables.
        public ClientAuthHandler(
            IOptionsMonitor<ClientAuthOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            AuthenticationModel authModel,
            ILoggerService _logger)
            : base(options, loggerFactory, encoder)
        {
            AuthModel = authModel;
            _Logger = _logger;
        }

        /// <summary>
        /// Checks whether the client authorisation details are correct.
        /// </summary>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Endpoint? endpoint = Context.GetEndpoint();

            if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>() == null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Authenticating {Request.Method} {Request.Path}{Request.QueryString}");

            if (!Request.Headers.ContainsKey("Authorization"))
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "Missing Authorization header");

                return Task.FromResult(AuthenticateResult.NoResult());
            }

            string? authHeader = Request.Headers.Authorization;

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(
                "Basic ",
                StringComparison.OrdinalIgnoreCase))
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "Invalid Authorization scheme");

                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization scheme. Only Basic is accepted."));
            }

            string encodedCredentials = authHeader["Basic ".Length..].Trim();
            string decodedCredentials;

            try
            {
                decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            }

            catch (FormatException)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "Malformed Base64 in Authorization header");

                return Task.FromResult(AuthenticateResult.Fail("Malformed credentials."));
            }

            int separatorIndex = decodedCredentials.IndexOf(':');

            if (separatorIndex < 0)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "Invalid credential format");

                return Task.FromResult(AuthenticateResult.Fail("Invalid credential format."));
            }

            string clientId = decodedCredentials[..separatorIndex];
            string clientSecret = decodedCredentials[(separatorIndex + 1)..];

            string clientIdHash = HashValue(clientId);
            string clientSecretHash = HashValue(clientSecret);

            byte[] expectedIdBytes = Encoding.UTF8.GetBytes(AuthModel.ClientId);
            byte[] expectedSecretBytes = Encoding.UTF8.GetBytes(AuthModel.ClientSecret);

            byte[] actualIdBytes = Encoding.UTF8.GetBytes(clientIdHash);
            byte[] actualSecretBytes = Encoding.UTF8.GetBytes(clientSecretHash);

            bool idMatch = CryptographicOperations.FixedTimeEquals(
                actualIdBytes,
                expectedIdBytes);
            bool secretMatch = CryptographicOperations.FixedTimeEquals(
                actualSecretBytes,
                expectedSecretBytes);

            if (!idMatch || !secretMatch)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "Invalid client credentials");

                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials."));
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Authentication Successful");

            ClaimsIdentity identity = new(Scheme.Name);
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        /// <summary>
        /// Returns a 401 response with a JSON body explaining the failure.
        /// </summary>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.ContentType = "application/json";

            string message = "Basic authentication is required for this call.";

            AuthenticateResult result = await HandleAuthenticateOnceAsync();

            if (result.Failure != null)
            {
                message = result.Failure.Message;
            }

            await Response.WriteAsync(JsonSerializer.Serialize(new FailureModel()
            {
                Error = message
            }));
        }

        /// <summary>
        /// Takes the given value and hashes it.
        /// </summary>
        private static string HashValue(string value)
        {
            byte[] hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(value));

            StringBuilder hex = new(hashBytes.Length * 2);

            foreach (byte b in hashBytes)
            {
                hex.Append(b.ToString("x2"));
            }

            return hex.ToString();
        }
    }
}
