// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the job service operations.
    /// </summary>
    public interface IJobService
    {
        Task<string> RunJobs(string job);
    }
}