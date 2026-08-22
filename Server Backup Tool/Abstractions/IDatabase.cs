// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the database.
    /// </summary>
    public interface IDatabase
    {
        Task<(T?, Exception?)> QuerySingle<T>(string sql, Func<SqliteDataReader, T> map, params SqliteParameter[] parameters);
        Task<(int, Exception?)> Execute(string sql, params SqliteParameter[] parameters);
    }
}
