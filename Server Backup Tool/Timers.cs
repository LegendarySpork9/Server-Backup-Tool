// Copyright © - 17/01/2024 - Toby Hunter
using log4net;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Server_Backup_Tool
{
    internal class Timers
    {
        static readonly ILog Log = LogManager.GetLogger("BackupLog");
        static Timer BackupTimer;
        static Timer WarningOne;
        static Timer WarningTwo;
        static Timer WarningThree;
        static Timer Wait;

        public static string SetTimers(TimeSpan Backup, TimeSpan One, TimeSpan Two, TimeSpan Three)
        {
            try
            {
                BackupTimer = new Timer
                {
                    Interval = Backup.TotalMilliseconds
                };
                BackupTimer.Elapsed += BackupServer;

                WarningOne = new Timer
                {
                    Interval = One.TotalMilliseconds
                };
                WarningOne.Elapsed += WarnOne;

                WarningTwo = new Timer
                {
                    Interval = Two.TotalMilliseconds
                };
                WarningTwo.Elapsed += WarnTwo;

                WarningThree = new Timer
                {
                    Interval = Three.TotalMilliseconds
                };
                WarningThree.Elapsed += WarnThree;

                return "Completed";
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Log.Error(ex.ToString());
                return "Errored";
            }
        }

        public static void StartTimers()
        {
            BackupTimer.Start();
            WarningOne.Start();
            WarningTwo.Start();
            WarningThree.Start();
        }

        public static void WaitForClose()
        {
            Wait = new Timer
            {
                Interval = TimeSpan.FromSeconds(30).TotalMilliseconds
            };
            Wait.Elapsed += WaitServer;

            Wait.Start();
        }

        private static void BackupServer(object sender, ElapsedEventArgs e)
        {
            BackupTimer.Stop();

            Console.WriteLine("\nBackup Triggered");
            Log.Info("Backup Triggered");

            BackupTimer.Dispose();
            Program.TakeBackup();
        }

        private static void WarnOne(object sender, ElapsedEventArgs e)
        {
            WarningOne.Stop();

            Console.WriteLine("\nWarning One Triggered");
            Log.Info("Warning One Triggered");
            Log.Debug("Warning Message: Server will shutdown for a backup in an hour.");

            Console.WriteLine("\n----Server Commands----");
            Log.Info("----Server Commands----");

            WarningOne.Dispose();
            Server.SendWarning("Server will shutdown for a backup in an hour.");
        }

        private static void WarnTwo(object sender, ElapsedEventArgs e)
        {
            WarningTwo.Stop();

            Console.WriteLine("\nWarning Two Triggered");
            Log.Info("Warning Two Triggered");
            Log.Debug("Warning Message: Server will shutdown for a backup in 30 minutes.");

            Console.WriteLine("\n----Server Commands----");
            Log.Info("----Server Commands----");

            WarningOne.Dispose();
            Server.SendWarning("Server will shutdown for a backup in 30 minutes.");
        }

        private static void WarnThree(object sender, ElapsedEventArgs e)
        {
            WarningThree.Stop();

            Console.WriteLine("\nWarning Three Triggered");
            Log.Info("Warning Three Triggered");
            Log.Debug("Warning Message: Server will shutdown for a backup in 5 minutes.");

            Console.WriteLine("\n----Server Commands----");
            Log.Info("----Server Commands----");

            WarningOne.Dispose();
            Server.SendWarning("Server will shutdown for a backup in 5 minutes.");
        }

        private static void WaitServer(object sender, ElapsedEventArgs e)
        {
            Wait.Stop();

            Console.WriteLine("Wait Server Triggered");
            Log.Info("Wait Server Triggered");
            
            Wait.Dispose();
            Program.WaitForServerClose.Set();
        }
    }
}
