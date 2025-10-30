// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Abstractions;

namespace ServerBackupTool.Implementations
{
    public class SystemClock : IClock
    {
        // Returns the current UTC date and time.
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
