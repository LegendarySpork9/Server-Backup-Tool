// Copyright © - 17/01/2024 - Toby Hunter
using System.Configuration;

namespace ServerBackupTool
{
    internal class Server_Information
    {
        public static string FilePath = ConfigurationManager.AppSettings["ServerLocation"];
        public static bool IsRunning = false;
    }
}
