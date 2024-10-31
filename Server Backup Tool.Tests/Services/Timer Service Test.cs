// Copyright © - unpublished - Toby Hunter
using ServerBackupTool.Models;
using ServerBackupTool.Models.Configuration;
using ServerBackupTool.Tests.Functions;
using System.Net.NetworkInformation;

namespace ServerBackupTool.Tests.Services
{
    [TestClass]
    public class TimerServiceTest
    {
        [TestMethod]
        public void TestTimerSetupNoHeartbeat()
        {
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");
            List<Mock<TimerModel>> timers = TimerFunction.ConfigureTimers(serverBackupSection);

            try
            {
                bool heartbeatAdded = false;

                foreach (Mock<TimerModel> timer in timers)
                {
                    if (timer.Object.TimerName == "Heartbeat")
                    {
                        heartbeatAdded = true;
                    }
                }

                Assert.IsTrue(timers.Count != 0 && !heartbeatAdded);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to setup timers without heartbeat. Exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestTimerSetupHeartbeat()
        {
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");
            List<Mock<TimerModel>> timers = TimerFunction.ConfigureTimers(serverBackupSection, true);

            try
            {
                bool heartbeatAdded = false;

                foreach (Mock<TimerModel> timer in timers)
                {
                    if (timer.Object.TimerName == "Heartbeat")
                    {
                        heartbeatAdded = true;
                    }
                }

                Assert.IsTrue(timers.Count != 0 && heartbeatAdded);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to setup timers with heartbeat. Exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestStart()
        {
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");
            List<Mock<TimerModel>> timers = TimerFunction.ConfigureTimers(serverBackupSection);

            try
            {
                foreach (Mock<TimerModel> timer in timers)
                {
                    if (timer.Object.TimerName != "Wait")
                    {
                        timer.Object.TimerData.Start();
                        timer.Object.TimerData.Stop();
                    }
                }

                Assert.IsTrue(true);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to start timers. Exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestWaitForClose()
        {
            SBTSection serverBackupSection = ConfigurationLoaderFunction.LoadConfig("Full Configuration.config");
            List<Mock<TimerModel>> timers = TimerFunction.ConfigureTimers(serverBackupSection);

            try
            {
                foreach (Mock<TimerModel> timer in timers)
                {
                    if (timer.Object.TimerName == "Wait")
                    {
                        timer.Object.TimerData.Start();
                        timer.Object.TimerData.Stop();
                    }
                }

                Assert.IsTrue(true);
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to start the wait timer. Exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestHeartbeat()
        {
            try
            {
                Ping pingSender = new();
                PingReply reply = pingSender.Send("localhost");

                if (reply.Status != IPStatus.Success)
                {
                    Assert.IsTrue(true);
                }
            }

            catch (Exception ex)
            {
                Assert.Fail($"Failed to ping ip address. Exception: {ex.Message}");
            }
        }
    }
}