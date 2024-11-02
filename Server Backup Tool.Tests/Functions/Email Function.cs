// Copyright © - 31/10/2024 - Toby Hunter
namespace ServerBackupTool.Tests.Functions
{
    internal class EmailFunction
    {
        public static string LoadHTMLFile(string file)
        {
            return File.ReadAllText(file);
        }
    }
}
