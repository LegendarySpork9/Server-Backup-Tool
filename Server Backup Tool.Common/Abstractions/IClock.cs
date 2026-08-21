// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Common.Abstractions
{
    /// <summary>
    /// Interface for the DateTime object.
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
