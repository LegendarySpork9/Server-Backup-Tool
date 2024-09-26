// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using System.Diagnostics;

namespace ServerBackupTool.Services
{
    internal class ServerService
    {
        readonly SBTSection ServerBackupSection;
        private ServerModel Server;

        public ServerService(SBTSection _serverBackupSection, ServerModel _server)
        {
            ServerBackupSection = _serverBackupSection;
            Server = _server;
        }

        public string StartServer()
        {
            LoggerService _logger = new();

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
                _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to start the server.");
                _logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());
                result = "Errored";
            }

            return result;
        }

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

                if (command == _serverConverter.GetStopCommand(Server.Game))
                {
                    Server.ServerRunning = false;
                }
            }
        }

        private void ServerResponseData(object sender, DataReceivedEventArgs e)
        {
            LoggerService _logger = new();
            ServerConverter _serverConverter = new();
            EmailService _emailService = new();

            if (!string.IsNullOrEmpty(e.Data))
            {
                try
                {
                    _logger.LogServerMessage(e.Data);

                    if (ServerBackupSection.Notifications.Emails.Count != 0)
                    {
                        foreach (EmailElement email in ServerBackupSection.Notifications.Emails)
                        {
                            if (e.Data.Contains(email.Trigger))
                            {
                                _emailService.SendEmail(ServerBackupSection.Notifications, email);
                            }
                        }
                    }

                    if (e.Data.Contains(_serverConverter.GetFinalMessage(Server.Game, Server.ServerProcess.StartInfo.WorkingDirectory)))
                    {
                        StopServer();
                    }
                }

                catch (Exception ex)
                {
                    _logger.LogToolMessage(StandardValues.LoggerValues.Warning, "Failed to capture server output or the server produced an error.");
                    _logger.LogToolMessage(StandardValues.LoggerValues.Error, ex.ToString());

                    Console.WriteLine("\n----Server Commands----");
                }
            }
        }

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
