// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.Common.Values;
using System.Text.Json;

namespace ServerBackupTool.API.Filters
{
    public class ResponseLoggingFilter : IAsyncResultFilter
    {
        /// <summary>
        /// Adds a log message to the logs with the return details.
        /// </summary>
        public async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next)
        {
            await next();

            if (context.Result is ObjectResult objectResult)
            {
                ILoggerService logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerService>();

                JsonSerializerOptions jsonOptions = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<JsonOptions>>()
                    .Value
                    .JsonSerializerOptions;

                string json = JsonSerializer.Serialize(
                    objectResult.Value,
                    jsonOptions);

                string method = context.HttpContext.Request.Method;
                string path = context.HttpContext.Request.Path;

                logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"{method} {path} returned a {objectResult.StatusCode} with the data {json}.");
            }
        }
    }
}
