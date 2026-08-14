// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Tests.Tool.Functions
{
    public static class DirectoryFunction
    {
        /// <summary>
        /// Returns the base directory of the running application.
        /// </summary>
        /// <returns></returns>
        public static string GetBaseDirectory()
        {
            string directory = Directory.GetCurrentDirectory();
            int binIndex = directory.IndexOf(@"\bin\");

            if (binIndex >= 0)
            {
                directory = directory[..binIndex] + @"\";
            }

            return directory;
        }
    }
}
