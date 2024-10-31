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
