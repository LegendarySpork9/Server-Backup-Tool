// Copyright © - 17/01/2024 - Toby Hunter
using log4net;

namespace Server_Backup_Tool
{
    internal class Program
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");
        public static ManualResetEvent WaitForServerClose = new ManualResetEvent(false);

        static void Main()
        {
            log4net.Config.XmlConfigurator.Configure();
            RunProgram();

            while (true)
            {
                string Command = Console.ReadLine();

                if (!string.IsNullOrEmpty(Command))
                {
                    if (Command == "Exit App")
                    {
                        break;
                    }

                    else
                    {
                        Server.SendCommand(Command);
                        Log.Debug($"Command Sent to Server: {Command}");
                    }
                }
            }
        }

        static void RunProgram()
        {
            Console.WriteLine($"Current Time: {DateTime.Now}");
            Log.Info($"Current Time: {DateTime.Now}");

            TimeSpan BackupTimer = Times.GetTimer();

            Console.WriteLine($"Time before backup: {BackupTimer}");
            Log.Debug($"Time before backup: {BackupTimer}");

            TimeSpan WarningOne = Times.GetWarningOne();

            Console.WriteLine($"Time before first warning: {WarningOne}");
            Log.Debug($"Time before first warning: {WarningOne}");

            TimeSpan WarningTwo = Times.GetWarningTwo();

            Console.WriteLine($"Time before second warning: {WarningTwo}");
            Log.Debug($"Time before second warning: {WarningTwo}");

            TimeSpan WarningThree = Times.GetWarningThree();

            Console.WriteLine($"Time before third warning: {WarningThree}");
            Log.Debug($"Time before third warning: {WarningThree}");

            string Result = Timers.SetTimers(BackupTimer, WarningOne, WarningTwo, WarningThree);

            Console.WriteLine($"Setting Timers: {Result}");
            Log.Debug($"Setting Timers: {Result}");

            Console.WriteLine($"Starting Timers");
            Log.Info("Starting Timers");

            Timers.StartTimers();

            Result = Server.StartServer();

            Console.WriteLine($"Starting Server: {Result}");
            Log.Info($"Starting Server: {Result}");

            Console.WriteLine("\n----Server Commands----");
            Log.Info("----Server Commands----");
        }

        public static void TakeBackup()
        {
            Console.WriteLine("Stopping Server");
            Log.Info("Stopping Server");

            Server.StopServer();

            Console.WriteLine("Waiting for 30 Seconds");
            Log.Info("Waiting for 30 Seconds");

            Timers.WaitForClose();
            WaitForServerClose.WaitOne();
            WaitForServerClose.Reset();

            Console.WriteLine("Creating Backup");
            Log.Info("Creating Backup");

            Archive_Jobs.BackupServer();

            Console.WriteLine("Archiving Logs");
            Log.Info("Archiving Logs");

            Archive_Jobs.ArchiveLogs();

            Console.WriteLine("Removing Old Backups and Logs");
            Log.Info("Removing Old Backups and Logs");

            Archive_Jobs.RemoveOldFiles();

            Console.WriteLine("Restarting Process");
            Log.Info("Restarting Process");

            RunProgram();
        }
    }
}