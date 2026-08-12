// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;

namespace ServerBackupTool.Implementations
{
    public class SystemClock : IClock
    {
        /// <summary>
        /// Returns the current UTC date and time.
        /// </summary>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
