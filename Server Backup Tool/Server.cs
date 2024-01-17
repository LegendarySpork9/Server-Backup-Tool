// Copyright © - 17/01/2024 - Toby Hunter
using log4net;
using System.Diagnostics;

namespace Server_Backup_Tool
{
    internal class Server
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");
        static readonly ILog ServerLog = LogManager.GetLogger("ServerLog");
        static Process? S;

        public static string StartServer()
        {
            ProcessStartInfo PSI = new()
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            S = new Process { StartInfo = PSI };

            S.OutputDataReceived += OutputDataReceived;

            try
            {
                S.StartInfo.FileName = @$"{Server_Information.FilePath}\Start.bat";
                S.StartInfo.WorkingDirectory = Server_Information.FilePath;
                S.Start();
                S.BeginOutputReadLine();
                return "Completed";
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Log.Error(ex.ToString());
                return "Errored";
            }            
        }

        public static void SendWarning(string Message)
        {
            S.StandardInput.WriteLine($"/say {Message}");
            S.StandardInput.Flush();
        }

        public static void SendCommand(string Command)
        {
            S.StandardInput.WriteLine(Command);
            S.StandardInput.Flush();
        }

        public static string StopServer()
        {
            S.StandardInput.WriteLine("stop");
            S.StandardInput.Flush();

            return "Stopped";
        }

        private static void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ServerLog.Info(e.Data);

                    if (e.Data.Contains($"{Server_Information.FilePath}>PAUSE"))
                    {
                        S.StandardInput.WriteLine();
                        S.StandardInput.WriteLine();
                        S.CancelOutputRead();
                        S.Kill();
                    }
                }
            }
            
            catch (Exception ex)
            {
                Console.WriteLine($"\n{ex}");
                Log.Error(ex.ToString());

                Console.WriteLine("\n----Server Commands----");
                Log.Info("----Server Commands----");
            }
        }
    }
}
