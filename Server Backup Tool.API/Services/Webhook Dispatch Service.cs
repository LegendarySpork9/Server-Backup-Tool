// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Models;
using ServerBackupTool.Common.Values;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ServerBackupTool.API.Services
{
    public class WebhookDispatchService
    {
        private readonly ILoggerService _Logger;
        private readonly HttpClient _HttpClient;
        private readonly WebhookSettingsModel Settings;
        private readonly JsonSerializerOptions JsonOptions;

        // Sets the class's global variables
        public WebhookDispatchService(
            ILoggerService _logger,
            HttpClient httpClient,
            WebhookSettingsModel settings,
            JsonSerializerOptions jsonOptions)
        {
            _Logger = _logger;
            _HttpClient = httpClient;
            Settings = settings;
            JsonOptions = jsonOptions;
        }

        /// <summary>
        /// Sends the webhook payload to the given URL with HMAC-SHA256 signing and retry logic.
        /// </summary>
        public async Task<(bool, Exception?)> Send(
            string url,
            WebhookPayloadModel payload)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookDispatchService.Send called for URL \"{url}\".");

            bool success = false;
            Exception? lastException = null;

            try
            {
                string json = JsonSerializer.Serialize(
                    payload,
                    JsonOptions);

                string signature = ComputeSignature(json);

                for (int attempt = 0; attempt <= Settings.MaxRetries; attempt++)
                {
                    try
                    {
                        if (attempt > 0)
                        {
                            int delayMs = (int)(2000 * Math.Pow(2, attempt - 1));

                            _Logger.LogMessage(
                                StandardValues.LoggerValues.Debug,
                                $"Retrying webhook dispatch (attempt {attempt + 1}) after {delayMs}ms.");

                            await Task.Delay(delayMs);
                        }

                        using (HttpRequestMessage request = new(
                            HttpMethod.Post,
                            url))
                        {
                            request.Content = new StringContent(
                                json,
                                Encoding.UTF8,
                                "application/json");
                            request.Headers.Add(
                                "X-Webhook-Secret",
                                signature);

                            using (HttpResponseMessage response = await _HttpClient.SendAsync(request))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    success = true;

                                    _Logger.LogMessage(
                                        StandardValues.LoggerValues.Debug,
                                        $"Webhook dispatched successfully to \"{url}\".");

                                    break;
                                }

                                _Logger.LogMessage(
                                    StandardValues.LoggerValues.Warning,
                                    $"Webhook dispatch to \"{url}\" returned {(int)response.StatusCode}.");
                            }
                        }
                    }

                    catch (Exception rex)
                    {
                        lastException = rex;

                        _Logger.LogMessage(
                            StandardValues.LoggerValues.Warning,
                            $"Webhook dispatch to \"{url}\" failed: {rex.Message}");
                    }
                }

                if (!success)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Webhook dispatch to \"{url}\" failed after {Settings.MaxRetries + 1} attempt(s).");
                }
            }

            catch (Exception cex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "An error occured when trying to run WebhookDispatchService.Send.");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    cex.ToString());

                lastException = cex;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"WebhookDispatchService.Send returned {success}.");
            return (
                success,
                lastException);
        }

        /// <summary>
        /// Computes the HMAC-SHA256 signature for the given body.
        /// </summary>
        private string ComputeSignature(string body)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(Settings.Secret);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            using (HMACSHA256 hmac = new(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(bodyBytes);

                StringBuilder hex = new(hashBytes.Length * 2);

                foreach (byte b in hashBytes)
                {
                    hex.Append(b.ToString("x2"));
                }

                return hex.ToString();
            }
        }
    }
}
