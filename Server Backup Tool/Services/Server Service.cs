// Copyright © - unpublished - Toby Hunter
using log4net;
using ServerBackupTool.Converters;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using System.Diagnostics;

namespace ServerBackupTool.Services
{
    internal class ServerService
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");
        static readonly ILog ServerLog = LogManager.GetLogger("ServerLog");
        readonly SBTSection ServerBackupSection;
        private ServerModel Server;

        public ServerService(SBTSection _serverBackupSection, ServerModel _server)
        {
            ServerBackupSection = _serverBackupSection;
            Server = _server;
        }

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
                Console.WriteLine("\n{0}", ex.ToString());
                Log.Error(ex.ToString());
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
            ServerConverter _serverConverter = new();
            EmailService _emailService = new();

            if (!string.IsNullOrEmpty(e.Data))
            {
                try
                {
                    switch (e.Data)
                    {
                        /* Move this when the change to logging is made start. */
                        case String when e.Data.Contains("/INFO]"): ServerLog.Info(e.Data); break;
                        case String when e.Data.Contains("/WARN]"): ServerLog.Warn(e.Data); break;
                        case String when e.Data.Contains("/ERROR]"): ServerLog.Error(e.Data); break;
                        case String when e.Data.Contains("/DEBUG]"): ServerLog.Debug(e.Data); break;
                        default: ServerLog.Info(e.Data); break;
                        /* Move this when the change to logging is made end. */
                    }

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
                    Console.WriteLine("\n{0}", ex.ToString());
                    Log.Error(ex.ToString());

                    Console.WriteLine("\n----Server Commands----");
                    Log.Info("----Server Commands----");
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
