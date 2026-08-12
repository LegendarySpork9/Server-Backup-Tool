// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Converters
{
    public static class ServerConverter
    {
        /// <summary>
        /// Returns the command to display messages to all users on the server.
        /// </summary>
        public static string GetMessageCommand(
            string? game,
            string command)
        {
            return game switch
            {
                "Minecraft" => $"/say {command}",
                _ => ""
            };
        }

        /// <summary>
        /// Returns the command to trigger the server shutdown.
        /// </summary>
        public static string GetStopCommand(string? game)
        {
            return game switch
            {
                "Minecraft" => "stop",
                _ => ""
            };
        }

        /// <summary>
        /// Returns the final message the server outputs.
        /// </summary>
        public static string GetFinalMessage(
            string? game,
            string filePath)
        {
            return game switch
            {
                "Minecraft" => $"{filePath}>PAUSE",
                _ => ""
            };
        }
    }
}
