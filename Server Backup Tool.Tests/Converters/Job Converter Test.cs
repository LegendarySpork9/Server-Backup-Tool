// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;
using ServerBackupTool.Converters;
using ServerBackupTool.Implementations;

namespace ServerBackupTool.Tests.Converters
{
    [TestClass]
    public class JobConverterTest
    {
        // Checks whether the GetBackPaths method returns the expected values for Minecraft.
        [TestMethod]
        public void TestGetBackupPathsMinecraft()
        {
            Mock<IClock> mockClock = new();
            mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 01, 01));

            JobConverter _jobConverter = new(mockClock.Object);

            string game = "Minecraft";
            string filePath = @"C:\GameServer";

            string expectedSource = @"C:\GameServer\world";
            string expectedDestination = @$"C:\GameServer\Backups\world 01-01-2025.zip";

            (string actualSource, string actualDestination) = _jobConverter.GetBackPaths(game, filePath);

            Assert.AreEqual(expectedSource, actualSource);
            Assert.AreEqual(expectedDestination, actualDestination);
        }

        // Checks whether the GetBackPaths method returns the empty strings when the game isn't registered.
        [TestMethod]
        public void TestGetBackupPathsUnregisteredGame()
        {
            JobConverter _jobConverter = new(new SystemClock());

            string game = "UnknownGame";
            string filePath = @"C:\GameServer";

            (string actualSource, string actualDestination) = _jobConverter.GetBackPaths(game, filePath);

            Assert.AreEqual("", actualSource);
            Assert.AreEqual("", actualDestination);
        }

        // Checks whether the GetBackPaths method returns the empty strings when the game is null.
        [TestMethod]
        public void TestGetBackupPathsNoGame()
        {
            JobConverter _jobConverter = new(new SystemClock());

            string filePath = @"C:\GameServer";

            (string actualSource, string actualDestination) = _jobConverter.GetBackPaths(null, filePath);

            Assert.AreEqual("", actualSource);
            Assert.AreEqual("", actualDestination);
        }
    }
}