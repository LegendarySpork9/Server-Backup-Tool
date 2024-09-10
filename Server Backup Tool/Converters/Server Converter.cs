// Copyright © - unpublished - Toby Hunter
namespace ServerBackupTool.Converters
{
    internal class ServerConverter
    {
        public string GetMessageCommand(string? game, string command)
        {
            return game switch
            {
                "Minecraft" => $"/say {command}",
                _ => ""
            };
        }

        public string GetStopCommand(string? game)
        {
            return game switch
            {
                "Minecraft" => "stop",
                _ => ""
            };
        }

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
