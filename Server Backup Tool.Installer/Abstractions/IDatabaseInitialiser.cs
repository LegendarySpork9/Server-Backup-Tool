// Copyright © - Unpublished - Toby Hunter
namespace ServerBackupTool.Installer.Abstractions
{
    /// <summary>
    /// Interface for the database initialiser.
    /// </summary>
    public interface IDatabaseInitialiser
    {
        Task<(bool, Exception?)> InitialiseDatabase(string databasePath);
        Task<(bool, Exception?)> ValidateDatabase(string databasePath);
    }
}
