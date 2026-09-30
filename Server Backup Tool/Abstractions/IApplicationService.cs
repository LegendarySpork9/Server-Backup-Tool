// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.Models;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the application service operations.
    /// </summary>
    public interface IApplicationService
    {
        Task RunApplication(CancellationToken cancellationToken = default);
        Task RunBackup(ITimerService timerService, CancellationToken cancellationToken = default);
        Task ProcessCommand(CommandModel command);
    }
}