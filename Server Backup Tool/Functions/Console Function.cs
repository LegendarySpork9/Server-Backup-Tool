// Copyright © - 17/01/2024 - Toby Hunter
using ServerBackupTool.Common.Values;
using ServerBackupTool.Services;
using System.Text;

namespace ServerBackupTool.Functions
{
    public class ConsoleFunction : TextWriter
    {
        private readonly TextWriter Console;
        private readonly LoggerService _Logger = new();

        // Sets the class's global variables.
        public ConsoleFunction(TextWriter console)
        {
            Console = console;
        }

        public override Encoding Encoding => Console.Encoding;

        /// <summary>
        /// Captures the console output and checks it has gone through Log4Net.
        /// </summary>
        public override void WriteLine(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message) && message != "\n----Server Commands----")
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    "An unknown error has occured that hasn't passed through the LoggerService.cs class.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    message);
            }

            else
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Captures the console output and checks it has gone through Log4Net.
        /// </summary>
        public override void Write(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message) && !message.Contains("log4net - "))
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    "An unknown error has occured that hasn't passed through the LoggerService.cs class.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    message);
            }

            if (!string.IsNullOrWhiteSpace(message) && message.Contains("log4net - "))
            {
                Console.Write(message.Replace("log4net - ", ""));
            }

            else
            {
                Console.Write(message);
            }
        }
    }
}
