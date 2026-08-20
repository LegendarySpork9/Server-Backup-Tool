// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.API.Abstractions
{
    /// <summary>
    /// Interface for the logger service.
    /// </summary>
    public interface ILoggerService
    {
        Guid RequestId { get; }
        void ChangeIdentifier(string value);
        void LogMessage(string level, string message);
    }
}
