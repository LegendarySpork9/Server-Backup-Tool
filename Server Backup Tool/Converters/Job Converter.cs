// Copyright © - unpublished - Toby Hunter
namespace ServerBackupTool.Converters
{
    internal class JobConverter
    {
        public (string, string) GetBackPaths(string? game, string filePath)
        {
            return game switch
            {
                "Minecraft" => (@$"{filePath}\world", @$"{filePath}\Backups\world {DateTime.Now:dd-MM-yyyy}.zip"),
                _ => ("", "")
            };
        }
    }
}
