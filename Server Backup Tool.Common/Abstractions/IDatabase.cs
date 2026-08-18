// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;

namespace ServerBackupTool.Common.Abstractions
{
    /// <summary>
    /// Interface for the database.
    /// </summary>
    public interface IDatabase
    {
        Task<(List<T>, Exception?)> Query<T>(string sql, Func<SqliteDataReader, T> map, params SqliteParameter[] parameters);
        Task<(T?, Exception?)> QuerySingle<T>(string sql, Func<SqliteDataReader, T> map, params SqliteParameter[] parameters);
        Task<(int, Exception?)> Execute(string sql, params SqliteParameter[] parameters);
    }
}
