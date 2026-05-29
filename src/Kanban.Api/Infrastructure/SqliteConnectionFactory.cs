using System.Data;
using Kanban.DataAccess.Interfaces;
using Microsoft.Data.Sqlite;

namespace Kanban.Api.Infrastructure;

internal sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    public IDbTransaction BeginDeferredTransaction(IDbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
            return sqlite.BeginTransaction(deferred: true);
        return connection.BeginTransaction();
    }
}
