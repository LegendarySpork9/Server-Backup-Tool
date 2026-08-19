// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Mvc.Filters;
using ServerBackupTool.API.Abstractions;
using ServerBackupTool.API.Values;

namespace ServerBackupTool.API.Filters
{
    public class RequestLoggingFilter : IAsyncActionFilter
    {
        /// <summary>
        /// Adds a log message to the logs with the query details.
        /// </summary>
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            ILoggerService logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerService>();

            string method = context.HttpContext.Request.Method;
            string path = context.HttpContext.Request.Path;

            if (context.ActionArguments.Count > 0)
            {
                string parameters = string.Join(
                    ", ",
                    context.ActionArguments.Select(p => $"\"{p.Key}: {p.Value}\""));

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
