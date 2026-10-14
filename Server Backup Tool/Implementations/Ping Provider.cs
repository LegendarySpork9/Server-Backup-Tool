// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;
using System.Net.NetworkInformation;

namespace ServerBackupTool.Implementations
{
    /// <summary>
    /// Wraps the System.Net.NetworkInformation.Ping class.
    /// </summary>
    public class PingProvider : IPingProvider
    {
        /// <summary>
        /// Sends an asynchronous ping to the specified host.
        /// </summary>
        public async Task<PingReply> SendPingAsync(
            string hostNameOrAddress,
            int timeout)
        {
            Ping pingSender = new();
            PingReply reply = await pingSender.SendPingAsync(
                hostNameOrAddress,
                timeout);

            return reply;
        }
    }
}
