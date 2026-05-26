using System.Data;
using Kanban.Domain.Events;

namespace Kanban.DataAccess.Interfaces;

public interface IAuthEventRepository
{
    Task RecordAsync(AuthEvent authEvent, IDbTransaction tx);
}
