// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the command reader operations.
    /// </summary>
    public interface ICommandReader
    {
        string? ReadCommand();
    }
}
