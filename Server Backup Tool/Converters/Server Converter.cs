// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Converters
{
    public class ServerConverter
    {
        // Returns the command to display messages to all users on the server.
        public string GetMessageCommand(string? game, string command)
        {
            return game switch
            {
                "Minecraft" => $"/say {command}",
                _ => ""
            };
        }

        // Returns the command to trigger the server shutdown.
        public string GetStopCommand(string? game)
        {
            return game switch
            {
                "Minecraft" => "stop",
                _ => ""
            };
        }

        // Returns the final message the server outputs.
        public string GetFinalMessage(string? game, string filePath)
        {
            return game switch
            {
                "Minecraft" => $"{filePath}>PAUSE",
                _ => ""
            };
        }
    }
}
