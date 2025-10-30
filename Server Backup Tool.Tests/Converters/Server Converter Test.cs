// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Converters;

namespace ServerBackupTool.Tests.Converters
{
    [TestClass]
    public class ServerConverterTest
    {
        #region GetMessageCommand

        // Checks whether the GetMessageCommand method returns the expected command for Minecraft.
        [TestMethod]
        public void TestGetMessageCommandMinecraft()
        {
            ServerConverter _serverConverter = new();

            string game = "Minecraft";

            string expected = "/say Hello! This is a test!";

            string actual = _serverConverter.GetMessageCommand(game, "Hello! This is a test!");

            Assert.AreEqual(expected, actual);
        }

        // Checks whether the GetMessageCommand method returns the empty strings when the game isn't registered.
        [TestMethod]
        public void TestGetMessageCommandUnregisteredGame()
        {
            ServerConverter _serverConverter = new();

            string game = "UnknownGame";

            string actual = _serverConverter.GetMessageCommand(game, "Hello! This is a test!");

            Assert.AreEqual("", actual);
        }

        // Checks whether the GetMessageCommand method returns the empty strings when the game is null.
        [TestMethod]
        public void TestGetMessageCommandNoGame()
        {
            ServerConverter _serverConverter = new();

            string actual = _serverConverter.GetMessageCommand(null, "Hello! This is a test!");

            Assert.AreEqual("", actual);
        }

        #endregion

        #region GetStopCommand

        // Checks whether the GetStopCommand method returns the expected command for Minecraft.
        [TestMethod]
        public void TestGetStopCommandMinecraft()
        {
            ServerConverter _serverConverter = new();

            string game = "Minecraft";

            string expected = "stop";

            string actual = _serverConverter.GetStopCommand(game);

            Assert.AreEqual(expected, actual);
        }

        // Checks whether the GetStopCommand method returns the empty strings when the game isn't registered.
        [TestMethod]
        public void TestGetStopCommandUnregisteredGame()
        {
            ServerConverter _serverConverter = new();

            string game = "UnknownGame";

            string actual = _serverConverter.GetStopCommand(game);

            Assert.AreEqual("", actual);
        }

        // Checks whether the GetStopCommand method returns the empty strings when the game is null.
        [TestMethod]
        public void TestGetStopCommandNoGame()
        {
            ServerConverter _serverConverter = new();

            string actual = _serverConverter.GetStopCommand(null);

            Assert.AreEqual("", actual);
        }

        #endregion

        #region GetFinalMessage

        // Checks whether the GetFinalMessage method returns the expected command for Minecraft.
        [TestMethod]
        public void TestGetFinalMessageMinecraft()
        {
            ServerConverter _serverConverter = new();

            string game = "Minecraft";
            string location = @"C:\GameServer";

            string expected = @"C:\GameServer>PAUSE";

            string actual = _serverConverter.GetFinalMessage(game, location);

            Assert.AreEqual(expected, actual);
        }

        // Checks whether the GetFinalMessage method returns the empty strings when the game isn't registered.
        [TestMethod]
        public void TestGetFinalMessageUnregisteredGame()
        {
            ServerConverter _serverConverter = new();

            string game = "UnknownGame";
            string location = @"C:\GameServer";

            string actual = _serverConverter.GetFinalMessage(game, location);

            Assert.AreEqual("", actual);
        }

        // Checks whether the GetFinalMessage method returns the empty strings when the game is null.
        [TestMethod]
        public void TestGetFinalMessageNoGame()
        {
            ServerConverter _serverConverter = new();

            string location = @"C:\GameServer";

            string actual = _serverConverter.GetFinalMessage(null, location);

            Assert.AreEqual("", actual);
        }

        #endregion
    }
}