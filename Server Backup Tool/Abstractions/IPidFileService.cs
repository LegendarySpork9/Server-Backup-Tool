// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the PID file service operations.
    /// </summary>
    public interface IPidFileService
    {
        Task Write(string serverName, int processId, DateTime startTimeUtc);
        void Delete(string serverName);
    }
}
