// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc.Filters;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.Common.Values;

namespace ServerBackupTool.API.Filters
{
    public class RequestLoggingFilter : IAsyncResourceFilter
    {
        /// <summary>
        /// Adds a log message to the logs with the query details.
        /// </summary>
        public async Task OnResourceExecutionAsync(
            ResourceExecutingContext context,
            ResourceExecutionDelegate next)
        {
            ILoggerService logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerService>();

            string method = context.HttpContext.Request.Method;
            string path = context.HttpContext.Request.Path;

            if (context.HttpContext.Request.ContentLength > 0)
            {
                context.HttpContext.Request.EnableBuffering();

                using StreamReader reader = new(
                    context.HttpContext.Request.Body,
                    leaveOpen: true);

                string body = await reader.ReadToEndAsync();
                context.HttpContext.Request.Body.Position = 0;

                logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"{method} {path} called with the payload {body}.");
            }

            else if (context.HttpContext.Request.Query.Count > 0)
            {
                string parameters = string.Join(
                    ", ",
                    context.HttpContext.Request.Query.Select(p => $"\"{p.Key}: {p.Value}\""));

                logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"{method} {path} called with the following parameters {parameters}.");
            }

            else
            {
                logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"{method} {path} called.");
            }

            await next();
        }
    }
}
