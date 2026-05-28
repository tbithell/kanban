using System.Data;
using Kanban.Business.Interfaces;
using Kanban.Contracts;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Kanban.Business.Services;

public sealed class InvitationService : IInvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IAuthEventRepository _authEventRepository;
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(
        IUserRepository userRepository,
        IInvitationRepository invitationRepository,
        IAuthEventRepository authEventRepository,
        IDbConnection dbConnection,
        ILogger<InvitationService> logger)
    {
        Verify.That(userRepository).IsNotNull();
        Verify.That(invitationRepository).IsNotNull();
        Verify.That(authEventRepository).IsNotNull();
        Verify.That(dbConnection).IsNotNull();
        Verify.That(logger).IsNotNull();
        _userRepository = userRepository;
        _invitationRepository = invitationRepository;
        _authEventRepository = authEventRepository;
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public Task<(IssueInviteResponse Response, bool IsNew)> IssueAsync(
        string email,
        Guid issuedByUserId,
        SystemRole callerRole,
        string frontendBaseUrl,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
