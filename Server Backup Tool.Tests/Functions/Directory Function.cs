// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Tests.Functions
{
    internal static class DirectoryFunction
    {
        public static string GetBaseDirectory()
        {
            return Directory.GetCurrentDirectory().Replace(@"bin\Debug\net6.0", "");
        }
    }
}
