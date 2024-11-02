﻿// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Services;
using System.Text;

namespace ServerBackupTool.Functions
{
    public class FilterConsoleFunction : TextWriter
    {
        private readonly TextWriter Console;

        public FilterConsoleFunction(TextWriter console)
        {
            Console = console;
        }

        public override Encoding Encoding => Console.Encoding;

        public override void WriteLine(string? message)
        {
            LoggerService _logger = new();

            if (!string.IsNullOrWhiteSpace(message) && message != "\n----Server Commands----")
            {
                _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "An unknown error has occured that hasn't passed through the LoggerService.cs class.");
                _logger.LogToolMessage(StandardValues.LoggerValues.Error, message);
            }

            else
            {
                Console.WriteLine(message);
            }
        }

        public override void Write(string? message)
        {
            LoggerService _logger = new();

            if (!string.IsNullOrWhiteSpace(message) && !message.Contains("log4net - "))
            {
                _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "An unknown error has occured that hasn't passed through the LoggerService.cs class.");
                _logger.LogToolMessage(StandardValues.LoggerValues.Error, message);
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
