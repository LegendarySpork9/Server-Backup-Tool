// Copyright © - 31/10/2024 - Toby Hunter
using ServerBackupTool.Abstractions;

namespace ServerBackupTool.Converters
{
    public class JobConverter
    {
        private readonly IClock _Clock;

        // Sets the class's global variables.
        public JobConverter(IClock _clock)
        {
            _Clock = _clock;
        }

        // Returns the paths of the world data and where the data should be backed up to.
        public (string, string) GetBackPaths(string? game, string filePath)
        {
            return game switch
            {
                "Minecraft" => (@$"{filePath}\world", @$"{filePath}\Backups\world {_Clock.UtcNow:dd-MM-yyyy}.zip"),
                _ => ("", "")
            };
        }
    }
}
