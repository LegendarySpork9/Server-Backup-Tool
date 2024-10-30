// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Diagnostics;

namespace ServerBackupTool.Models
{
    public class ServerModel
    {
        public string? Game { get; set; }
        public bool ServerRunning { get; set; } = false;
        public Process ServerProcess { get; }

        public ServerModel(ServerDetailsElement serverDetails)
        {
            ProcessStartInfo psi = new()
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = serverDetails.Location,
                FileName = $@"{serverDetails.Location}\{serverDetails.StartFile}"
            };

            ServerProcess = new() { StartInfo = psi };
        }
    }
}
