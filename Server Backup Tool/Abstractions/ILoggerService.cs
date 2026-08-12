// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the logger service.
    /// </summary>
    public interface ILoggerService
    {
        void LogToolMessage(string level, string message, bool verbose = false);

        void LogServerMessage(string message);
    }
}
