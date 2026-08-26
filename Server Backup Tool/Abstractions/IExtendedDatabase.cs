// Copyright © - Unpublished - Toby Hunter
using Microsoft.Data.Sqlite;

namespace ServerBackupTool.Abstractions
{
    /// <summary>
    /// Interface for the database.
    /// </summary>
    public interface IExtendedDatabase : Common.Abstractions.IDatabase
    {
        Task<(T?, Exception?)> QuerySingle<T>(string sql, Func<SqliteDataReader, T> map, params SqliteParameter[] parameters);
    }
}
