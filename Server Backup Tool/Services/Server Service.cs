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
        readonly ILoggerService _Logger;
        readonly SBTSection ServerBackupSection;
        private ServerModel Server;

        // Sets the class's global variables.
        public ServerService(ILoggerService _logger, SBTSection _serverBackupSection, ServerModel _server)
        {
            _Logger = _logger;
            ServerBackupSection = _serverBackupSection;
            Server = _server;
        }

        // Activates the server.
        public string StartServer()
        {
            string result = "Completed";

            Server.ServerProcess.OutputDataReceived += ServerResponseData;

            try
            {
                Server.ServerProcess.Start();
                Server.ServerProcess.BeginOutputReadLine();
                Server.ServerRunning = true;
            }

            catch (Exception ex)
            {
                _Logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to start the server.");
                _Logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Errored";
            }

            return result;
        }

        // Executes a command through the server.
        public void SendCommand(string command, bool isTimer = false)
        {
            ServerConverter _serverConverter = new();

            if (isTimer)
            {
                Server.ServerProcess.StandardInput.WriteLine(_serverConverter.GetMessageCommand(Server.Game, command));
                Server.ServerProcess.StandardInput.Flush();
            }

            else
            {
                Server.ServerProcess.StandardInput.WriteLine(command);
                Server.ServerProcess.StandardInput.Flush();
            }
        }

        // Logs the output from the server.
        private void ServerResponseData(object sender, DataReceivedEventArgs e)
        {
            ServerConverter _serverConverter = new();
            EmailService _emailService = new(_Logger, new SMTPEmailSender(), new FileSystem(), true);

            if (!string.IsNullOrEmpty(e.Data))
            {
                try
                {
                    _Logger.LogServerMessage(e.Data);

                    _emailService.CheckForEmail(ServerBackupSection.Notifications, null, e.Data);

                    if (e.Data.IndexOf(_serverConverter.GetFinalMessage(Server.Game, Server.ServerProcess.StartInfo.WorkingDirectory), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        StopServer();
                    }
                }

                catch (Exception ex)
                {
                    _Logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to capture server output or the server produced an error.", true);
                    _Logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                }
            }
        }

        // Shuts down the server.
        private void StopServer()
        {
            Server.ServerProcess.StandardInput.WriteLine();
            Server.ServerProcess.StandardInput.WriteLine();
            Server.ServerProcess.CancelOutputRead();
            Server.ServerProcess.Close();
            Server.ServerProcess.OutputDataReceived -= ServerResponseData;
            Server.ServerRunning = false;
        }
    }
}
