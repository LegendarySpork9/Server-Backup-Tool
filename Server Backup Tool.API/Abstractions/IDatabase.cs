// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;

namespace ServerBackupTool.API.Abstractions
{
    /// <summary>
    /// Interface for the database.
    /// </summary>
    public interface IDatabase
    {
        Task<(List<T>, Exception?)> Query<T>(string sql, Func<SqliteDataReader, T> map, params SqliteParameter[] parameters);
        Task<(object?, Exception?)> ExecuteScalar(string sql, params SqliteParameter[] parameters);
    }
}
