// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    // Interface for the logger service.
    public interface ILoggerService
    {
        void LogToolMessage(string level, string message, bool verbose = false);

        void LogServerMessage(string message);
    }
}
