// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    // Interface for the DateTime object.
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
