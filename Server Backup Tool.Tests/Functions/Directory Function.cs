// Copyright © - unpublished - Toby Hunter
namespace ServerBackupTool.Tests.Functions
{
    internal static class DirectoryFunction
    {
        public static string GetBaseDirectory()
        {
            return Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(AppContext.BaseDirectory).FullName).FullName).FullName).FullName;
        }
    }
}
