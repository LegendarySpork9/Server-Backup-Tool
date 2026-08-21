// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Abstractions;

namespace ServerBackupTool.Common.Implementations
{
    public class SystemClockProvider : IClock
    {
        /// <summary>
        /// Returns the current UTC date and time.
        /// </summary>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
