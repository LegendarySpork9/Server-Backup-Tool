// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Installer.Abstractions;
using ServerBackupTool.Installer.Models;
using ServerBackupTool.Installer.Steps;
using Spectre.Console.Testing;

namespace ServerBackupTool.IntegrationTests.Installer.Steps
{
    [TestClass]
    public class ComponentSelectionStepTest
    {
        private Mock<ILoggerService> _MockLogger = null!;

        /// <summary>
        /// Initialises the test dependencies.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            _MockLogger = new Mock<ILoggerService>();
        }

        /// <summary>
        /// Checks that Execute sets the selected components when the default selection is confirmed.
        /// </summary>
        [TestMethod]
        public void Execute_ConfirmsDefault_SetsServerBackupTool()
        {
            TestConsole console = new();
            console.Interactive();
            console.Input.PushKey(ConsoleKey.Enter);

            InstallOptionsModel options = new();
            ComponentSelectionStep step = new(
                console,
                _MockLogger.Object,
                options);

            step.Execute();

            Assert.IsTrue(options.Components.Contains("Server Backup Tool"));
        }
    }
}
