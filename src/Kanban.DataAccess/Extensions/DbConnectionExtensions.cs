using System.Data;
using Microsoft.Data.Sqlite;

namespace Kanban.DataAccess.Extensions;

public static class DbConnectionExtensions
{
    public static IDbTransaction BeginDeferredTransaction(this IDbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
            return sqlite.BeginTransaction(deferred: true);
        return connection.BeginTransaction();
    }
}
