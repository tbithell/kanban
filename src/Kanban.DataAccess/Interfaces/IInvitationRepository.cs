using System.Data;
using Kanban.Domain.Entities;

namespace Kanban.DataAccess.Interfaces;

public interface IInvitationRepository
{
    Task<Invitation?> FindByTokenHashAsync(string tokenHash, IDbTransaction? tx = null);
    Task<Invitation?> FindActiveByEmailAsync(string email, IDbTransaction? tx = null);
    Task InsertAsync(Invitation invitation, IDbTransaction tx);
    Task<bool> TryConsumeAsync(string tokenHash, DateTimeOffset consumedAt, IDbTransaction tx);
    Task RecordConsumerAsync(string tokenHash, Guid consumedByUserId, IDbTransaction tx);
    Task RefreshTokenAsync(Guid id, string newTokenHash, DateTimeOffset newExpiresAt,
                           IDbTransaction tx);
}
