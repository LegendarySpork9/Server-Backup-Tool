// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Converters
{
    public class JobConverter
    {
        // Returns the paths of the world data and where the data should be backed up to.
        public (string, string) GetBackPaths(string? game, string filePath)
        {
            return game switch
            {
                "Minecraft" => (@$"{filePath}\world", @$"{filePath}\Backups\world {DateTime.UtcNow:dd-MM-yyyy}.zip"),
                _ => ("", "")
            };
        }
    }
}
