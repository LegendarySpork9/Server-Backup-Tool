// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Common.Values;
using ServerBackupTool.Converters;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using System.Diagnostics;

namespace ServerBackupTool.Implementations
{
    public class ServerServiceWrapper : IServerService
    {
        private readonly ILoggerService _Logger;
        private readonly IPidFileService _PidFileService;
        private readonly SBTSection ServerBackupSection;
        private readonly ServerModel Server;

        // Sets the class's global variables.
        public ServerServiceWrapper(
            ILoggerService _logger,
            IPidFileService pidFileService,
            SBTSection serverBackupSection,
            ServerModel _server)
        {
            _Logger = _logger;
            _PidFileService = pidFileService;
            ServerBackupSection = serverBackupSection;
            Server = _server;
        }

        /// <summary>
        /// Activates the server.
        /// </summary>
        public async Task<string> StartServer()
        {
            string result = "Completed";

            Server.ResetProcess();
            Server.ServerProcess.OutputDataReceived += ServerResponseData;
            Server.ServerProcess.EnableRaisingEvents = true;
            Server.ServerProcess.Exited += OnProcessExited;

            try
            {
                Server.ServerProcess.Start();
                Server.ServerProcess.BeginOutputReadLine();
                Server.ServerRunning = true;

                await _PidFileService.Write(
                    Server.Name,
                    Server.ServerProcess.Id,
                    Server.ServerProcess.StartTime);
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Warning,
                    "Failed to start the server.");
                _Logger.LogToolMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());

                result = "Errored";
            }

            return result;
        }

        /// <summary>
        /// Executes a command through the server.
        /// </summary>
        public async Task SendCommand(
            string command,
            bool isTimer = false)
        {
            if (isTimer)
            {
                await Server.ServerProcess.StandardInput.WriteLineAsync(ServerConverter.GetMessageCommand(
                    Server.Game,
                    command));
                await Server.ServerProcess.StandardInput.FlushAsync();
            }

            else
            {
                await Server.ServerProcess.StandardInput.WriteLineAsync(command);
                await Server.ServerProcess.StandardInput.FlushAsync();
            }
        }

        /// <summary>
        /// Logs the output from the server.
        /// </summary>
        private async void ServerResponseData(
            object sender,
            DataReceivedEventArgs e)
        {
            EmailServiceWrapper _emailService = new(
                _Logger,
                new SMTPEmailSender(),
                new ExtendedFileSystemWrapper(),
                true);

            if (!string.IsNullOrEmpty(e.Data))
            {
                try
                {
                    _Logger.LogServerMessage(e.Data);

                    await _emailService.CheckForEmail(
                        ServerBackupSection.Notifications,
                        null,
                        e.Data);

                    if (e.Data.IndexOf(
                        ServerConverter.GetFinalMessage(
                            Server.Game,
                            Server.ServerProcess.StartInfo.WorkingDirectory),
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        await StopServer();
                    }
                }

                catch (Exception ex)
                {
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Warning,
                        "Failed to capture server output or the server produced an error.",
                        true);
                    _Logger.LogToolMessage(
                        StandardValues.LoggerValues.Error,
                        ex.ToString());
                }
            }
        }

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        private async Task StopServer()
        {
            if (!Server.ServerRunning)
            {
                return;
            }

            Server.ServerRunning = false;

            if (!Server.ServerProcess.HasExited)
            {
                await Server.ServerProcess.StandardInput.WriteLineAsync();
                await Server.ServerProcess.StandardInput.WriteLineAsync();
                Server.ServerProcess.CancelOutputRead();
            }

            Server.ServerProcess.Exited -= OnProcessExited;
            Server.ServerProcess.Close();
            Server.ServerProcess.OutputDataReceived -= ServerResponseData;

            _PidFileService.Delete(Server.Name);
        }

        /// <summary>
        /// Handles the server process exiting unexpectedly.
        /// </summary>
        private void OnProcessExited(
            object? sender,
            EventArgs e)
        {
            if (!Server.ServerRunning)
            {
                return;
            }

            Server.ServerRunning = false;

            _Logger.LogToolMessage(
                StandardValues.LoggerValues.Info,
                "Server process exited unexpectedly, cleaning up");

            Server.ServerProcess.OutputDataReceived -= ServerResponseData;
            Server.ServerProcess.Exited -= OnProcessExited;

            _PidFileService.Delete(Server.Name);
        }
    }
}
