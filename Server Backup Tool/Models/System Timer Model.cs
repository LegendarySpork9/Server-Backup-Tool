// Copyright © - unpublished - Toby Hunter
namespace ServerBackupTool.Models
{
    public static class SystemTimerModel
    {
        public static readonly string[] Names = { "Heartbeat", "Wait", "Backup" };
        public static readonly int[] Durations = { 5000, 30000, 1 };
    }
}
