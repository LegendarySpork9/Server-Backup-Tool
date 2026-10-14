// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the server service operations.
    /// </summary>
    public interface IServerService
    {
        Task<string> StartServer();
        Task SendCommand(string command, bool isTimer = false);
    }
}
