// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Common.Models.Requests;
using ServerBackupTool.Models;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the command service operations.
    /// </summary>
    public interface ICommandService
    {
        Task<(CommandModel?, Exception?)> GetCommand();
        Task<(bool, Exception?)> LogCommand(CommandRequestModel command);
        Task<(bool, Exception?)> DeleteCommand(int id);
    }
}
