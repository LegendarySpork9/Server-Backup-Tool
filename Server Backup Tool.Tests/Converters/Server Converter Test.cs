// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Converters;

namespace ServerBackupTool.Tests.Converters
{
    [TestClass]
    public class ServerConverterTest
    {
        #region GetMessageCommand

        /// <summary>
        /// Checks whether the GetMessageCommand method returns the expected command for Minecraft.
        /// </summary>
        [TestMethod]
        public void TestGetMessageCommandMinecraft()
        {
            string game = "Minecraft";
            string expected = "/say Hello! This is a test!";

            string actual = ServerConverter.GetMessageCommand(
                game,
                "Hello! This is a test!");

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the GetMessageCommand method returns the empty strings when the game isn't registered.
        /// </summary>
        [TestMethod]
        public void TestGetMessageCommandUnregisteredGame()
        {
            string game = "UnknownGame";

            string actual = ServerConverter.GetMessageCommand(
                game,
                "Hello! This is a test!");

            Assert.AreEqual(
                "",
                actual);
        }

        /// <summary>
        /// Checks whether the GetMessageCommand method returns the empty strings when the game is null.
        /// </summary>
        [TestMethod]
        public void TestGetMessageCommandNoGame()
        {
            string actual = ServerConverter.GetMessageCommand(
                null,
                "Hello! This is a test!");

            Assert.AreEqual(
                "",
                actual);
        }

        #endregion

        #region GetStopCommand

        /// <summary>
        /// Checks whether the GetStopCommand method returns the expected command for Minecraft.
        /// </summary>
        [TestMethod]
        public void TestGetStopCommandMinecraft()
        {
            string game = "Minecraft";
            string expected = "stop";

            string actual = ServerConverter.GetStopCommand(game);

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the GetStopCommand method returns the empty strings when the game isn't registered.
        /// </summary>
        [TestMethod]
        public void TestGetStopCommandUnregisteredGame()
        {
            string game = "UnknownGame";

            string actual = ServerConverter.GetStopCommand(game);

            Assert.AreEqual(
                "",
                actual);
        }

        /// <summary>
        /// Checks whether the GetStopCommand method returns the empty strings when the game is null.
        /// </summary>
        [TestMethod]
        public void TestGetStopCommandNoGame()
        {
            string actual = ServerConverter.GetStopCommand(null);

            Assert.AreEqual(
                "",
                actual);
        }

        #endregion

        #region GetFinalMessage

        /// <summary>
        /// Checks whether the GetFinalMessage method returns the expected command for Minecraft.
        /// </summary>
        [TestMethod]
        public void TestGetFinalMessageMinecraft()
        {
            string game = "Minecraft";
            string filePath = @"C:\GameServer";

            string expected = @"C:\GameServer>PAUSE";

            string actual = ServerConverter.GetFinalMessage(
                game,
                filePath);

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the GetFinalMessage method returns the empty strings when the game isn't registered.
        /// </summary>
        [TestMethod]
        public void TestGetFinalMessageUnregisteredGame()
        {
            string game = "UnknownGame";
            string filePath = @"C:\GameServer";

            string actual = ServerConverter.GetFinalMessage(
                game,
                filePath);

            Assert.AreEqual(
                "",
                actual);
        }

        /// <summary>
        /// Checks whether the GetFinalMessage method returns the empty strings when the game is null.
        /// </summary>
        [TestMethod]
        public void TestGetFinalMessageNoGame()
        {
            string filePath = @"C:\GameServer";

            string actual = ServerConverter.GetFinalMessage(
                null,
                filePath);

            Assert.AreEqual(
                "",
                actual);
        }

        #endregion
    }
}