// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;

namespace ServerBackupTool.Tests.Functions
{
    internal static class ServerFunction
    {
        // Runs a mock server for testing some commands.
        public static Mock<ServerModel> RunServer(SBTSection serverBackupSection)
        {
            serverBackupSection.ServerDetails.Location = Path.Combine(DirectoryFunction.GetBaseDirectory(), @"Mocks\Server");
            Mock<ServerModel> mockServer = new(serverBackupSection.ServerDetails);
            mockServer.Object.Game = serverBackupSection.ServerDetails.Game;

            try
            {
                mockServer.Object.ServerProcess.Start();
                mockServer.Object.ServerProcess.BeginOutputReadLine();
                mockServer.Object.ServerRunning = true;
            }

            catch
            {

            }

            return mockServer;
        }
    }
}
