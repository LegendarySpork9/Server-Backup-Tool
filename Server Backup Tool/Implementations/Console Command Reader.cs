// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;

namespace ServerBackupTool.Implementations
{
    public class ConsoleCommandReader : ICommandReader
    {
        /// <summary>
        /// Reads a command from the console.
        /// </summary>
        public string? ReadCommand()
        {
            return Console.ReadLine();
        }
    }
}
