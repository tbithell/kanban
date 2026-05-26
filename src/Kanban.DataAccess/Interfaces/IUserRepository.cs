using System.Data;
using Kanban.Domain.Entities;

namespace Kanban.DataAccess.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByGoogleSubAsync(string googleSub, IDbTransaction? tx = null);
    Task<User?> FindByEmailAsync(string email, IDbTransaction? tx = null);
    Task<User?> FindByIdAsync(Guid id, IDbTransaction? tx = null);
    Task InsertAsync(User user, IDbTransaction tx);
    Task LinkGoogleSubAsync(Guid userId, string googleSub, IDbTransaction tx);
    Task UpdateLastSignInAsync(Guid userId, DateTimeOffset signedInAt, IDbTransaction tx);
}
