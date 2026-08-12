// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Converters;
using ServerBackupTool.Implementations;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using System.Diagnostics;

namespace ServerBackupTool.Services
{
    public class ServerService
    {
        private readonly ILoggerService _Logger;
        private readonly PidFileService _PidFileService;
        private readonly SBTSection ServerBackupSection;
        private readonly ServerModel Server;

        // Sets the class's global variables.
        public ServerService(
            ILoggerService _logger,
            PidFileService pidFileService,
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

            Server.ServerProcess.OutputDataReceived += ServerResponseData;

            try
            {
                Server.ServerProcess.Start();
                Server.ServerProcess.BeginOutputReadLine();
                Server.ServerRunning = true;

                await _PidFileService.Write(
                    Server.Name,
                    Server.ServerProcess.Id,
                    Server.ServerProcess.StartTime.ToUniversalTime());
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
        public void SendCommand(
            string command,
            bool isTimer = false)
        {
            if (isTimer)
            {
                Server.ServerProcess.StandardInput.WriteLine(ServerConverter.GetMessageCommand(
                    Server.Game,
                    command));
                Server.ServerProcess.StandardInput.Flush();
            }

            else
            {
                Server.ServerProcess.StandardInput.WriteLine(command);
                Server.ServerProcess.StandardInput.Flush();
            }
        }

        /// <summary>
        /// Logs the output from the server.
        /// </summary>
        private void ServerResponseData(
            object sender,
            DataReceivedEventArgs e)
        {
            EmailService _emailService = new(
                _Logger,
                new SMTPEmailSender(),
                new FileSystem(),
                true);

            if (!string.IsNullOrEmpty(e.Data))
            {
                try
                {
                    _Logger.LogServerMessage(e.Data);

                    _emailService.CheckForEmail(
                        ServerBackupSection.Notifications,
                        null,
                        e.Data);

                    if (e.Data.IndexOf(
                        ServerConverter.GetFinalMessage(
                            Server.Game,
                            Server.ServerProcess.StartInfo.WorkingDirectory),
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        StopServer();
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
        private void StopServer()
        {
            Server.ServerProcess.StandardInput.WriteLine();
            Server.ServerProcess.StandardInput.WriteLine();
            Server.ServerProcess.CancelOutputRead();
            Server.ServerProcess.Close();
            Server.ServerProcess.OutputDataReceived -= ServerResponseData;
            Server.ServerRunning = false;

            _PidFileService.Delete(Server.Name);
        }
    }
}
