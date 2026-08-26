// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;

namespace ServerBackupTool.Common.Abstractions
{
    /// <summary>
    /// Interface for the database.
    /// </summary>
    public interface IDatabase
    {
        Task<(int, Exception?)> Execute(string sql, params SqliteParameter[] parameters);
    }
}
