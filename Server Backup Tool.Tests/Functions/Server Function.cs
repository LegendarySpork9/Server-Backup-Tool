﻿// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;

namespace ServerBackupTool.Tests.Functions
{
    internal static class ServerFunction
    {
        public static Mock<ServerModel> RunServer(SBTSection serverBackupSection)
        {
            serverBackupSection.ServerDetails.Location = Directory.GetCurrentDirectory().Replace(@"bin\Debug\net6.0", @"Mocks\Server");
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