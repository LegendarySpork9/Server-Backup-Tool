// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Tests.Functions
{
    public static class DirectoryFunction
    {
        /// <summary>
        /// Returns the base directory of the running application.
        /// </summary>
        /// <returns></returns>
        public static string GetBaseDirectory()
        {
            return Directory.GetCurrentDirectory()
                .Replace(
                    @"bin\Debug\net6.0",
                    "");
        }
    }
}
