// Copyright © - Unpublished - Toby Hunter
using System.Net.NetworkInformation;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the ping send operation.
    /// </summary>
    public interface IPingProvider
    {
        Task<PingReply> SendPingAsync(string hostNameOrAddress, int timeout);
    }
}
