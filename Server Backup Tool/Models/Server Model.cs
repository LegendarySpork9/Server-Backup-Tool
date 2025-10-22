// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Models.Configuration;
using System.Diagnostics;

namespace ServerBackupTool.Models
{
    // Stores the information about the server.
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
                FileName = Path.Combine(serverDetails.Location, serverDetails.StartFile)
            };

            ServerProcess = new() { StartInfo = psi };
        }
    }
}
