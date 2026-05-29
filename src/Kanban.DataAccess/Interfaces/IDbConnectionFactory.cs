using System.Data;

namespace Kanban.DataAccess.Interfaces;

public interface IDbConnectionFactory
{
    IDbTransaction BeginDeferredTransaction(IDbConnection connection);
}
