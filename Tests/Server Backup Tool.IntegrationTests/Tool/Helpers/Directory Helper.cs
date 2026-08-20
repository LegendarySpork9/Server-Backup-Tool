// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.IntegrationTests.Tool.Helpers
{
    public static class DirectoryHelper
    {
        /// <summary>
        /// Returns the base directory of the running application.
        /// </summary>
        public static string GetBaseDirectory()
        {
            string directory = Directory.GetCurrentDirectory();
            int binIndex = directory.IndexOf(@"\bin\", StringComparison.OrdinalIgnoreCase);

            if (binIndex >= 0)
            {
                directory = directory[..binIndex];
            }

            return directory;
        }
    }
}
