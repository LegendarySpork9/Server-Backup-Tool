// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Converters;
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Tests.Functions;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class ServerServiceTest
    {
        [TestMethod]
        public void TestStart()
        {
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");
            serverBackupSection.ServerDetails.Location = Path.Combine(DirectoryFunction.GetBaseDirectory(), "Mocks", "Server");
            Mock<ServerModel> mockServer = new(serverBackupSection.ServerDetails);
            mockServer.Object.Game = serverBackupSection.ServerDetails.Game;

            try
            {
                mockServer.Object.ServerProcess.Start();
                mockServer.Object.ServerProcess.BeginOutputReadLine();
                mockServer.Object.ServerRunning = true;
                mockServer.Object.ServerProcess.Kill();

                Assert.IsTrue(mockServer.Object.ServerRunning);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to start server. Exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestCommand()
        {
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");
            Mock<ServerModel> mockServer = ServerFunction.RunServer(serverBackupSection);
            Mock<ServerConverter> _mockServerConverter = new();

            try
            {
                mockServer.Object.ServerProcess.StandardInput.WriteLine(_mockServerConverter.Object.GetStopCommand(serverBackupSection.ServerDetails.Game));
                mockServer.Object.ServerProcess.StandardInput.Flush();

                Assert.IsTrue(true);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to send the server a command. Exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestStop()
        {
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");
            Mock<ServerModel> mockServer = ServerFunction.RunServer(serverBackupSection);
            Mock<ServerConverter> _mockServerConverter = new();

            try
            {
                mockServer.Object.ServerProcess.StandardInput.WriteLine(_mockServerConverter.Object.GetStopCommand(serverBackupSection.ServerDetails.Game));
                mockServer.Object.ServerProcess.StandardInput.Flush();
                mockServer.Object.ServerProcess.StandardInput.WriteLine();
                mockServer.Object.ServerProcess.StandardInput.WriteLine();
                mockServer.Object.ServerProcess.CancelOutputRead();
                mockServer.Object.ServerProcess.Close();
                mockServer.Object.ServerRunning = false;

                Assert.IsTrue(true);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to stop the server. Exception: {ex.Message}");
            }
        }
    }
}