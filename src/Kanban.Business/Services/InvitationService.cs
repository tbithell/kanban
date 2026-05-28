using System.Data;
using Kanban.Business.Interfaces;
using Kanban.Business.Transforms;
using Kanban.Contracts;
using Kanban.DataAccess.Extensions;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.Events;
using Kanban.Domain.Exceptions;
using Kanban.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Kanban.Business.Services;

public sealed class InvitationService : IInvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IAuthEventRepository _authEventRepository;
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<InvitationService> _logger;

    private static readonly ResiliencePipeline RetryPolicy =
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex.Message.Contains("SQLITE_BUSY", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase)),
            })
            .Build();

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

    public async Task<(IssueInviteResponse Response, bool IsNew)> IssueAsync(
        string email,
        Guid issuedByUserId,
        SystemRole callerRole,
        string frontendBaseUrl,
        CancellationToken cancellationToken = default)
    {
        Verify.That(email).IsNotNull().IsNotEmpty();
        Verify.That(issuedByUserId).IsNotDefault();
        Verify.That(frontendBaseUrl).IsNotNull().IsNotEmpty();

        if (callerRole != SystemRole.Admin)
            throw new ForbiddenException("invite.forbidden", "Only admins can issue invitations.");

        return await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _dbConnection.BeginDeferredTransaction();
            try
            {
                var existing = await _invitationRepository.FindActiveByEmailAsync(email, tx);
                if (existing is not null)
                {
                    var refreshToken = InvitationToken.Generate();
                    var newExpiry = DateTimeOffset.UtcNow.AddDays(7);
                    await _invitationRepository.RefreshTokenAsync(
                        existing.Id, refreshToken.Hash, newExpiry, tx);
                    tx.Commit();
                    _logger.LogInformation(
                        "Active invitation for {Email} refreshed by {IssuedByUserId}", email, issuedByUserId);
                    var refreshedResponse = new IssueInviteResponse
                    {
                        Token = refreshToken.Raw,
                        RedemptionLink = $"{frontendBaseUrl}/accept/{refreshToken.Raw}",
                        ExpiresAt = newExpiry,
                    };
                    return (refreshedResponse, false);
                }

                var registeredUser = await _userRepository.FindByEmailAsync(email, tx);
                if (registeredUser is not null)
                    throw new ConflictException(
                        "invite.already_registered", "This email address is already registered.");

                var token = InvitationToken.Generate();
                var issuedAt = DateTimeOffset.UtcNow;
                var invitation = new Invitation(
                    id: Guid.NewGuid(),
                    email: email,
                    issuedByUserId: issuedByUserId,
                    tokenHash: token.Hash,
                    issuedAt: issuedAt,
                    expiresAt: issuedAt.AddDays(7),
                    consumedAt: null,
                    consumedByUserId: null);

                await _invitationRepository.InsertAsync(invitation, tx);

                var authEvent = new AuthEvent(
                    Id: Guid.NewGuid(),
                    OccurredAt: issuedAt,
                    EventType: AuthEventType.InvitationIssued,
                    UserId: issuedByUserId,
                    Outcome: "success");
                await _authEventRepository.RecordAsync(authEvent, tx);

                tx.Commit();

                _logger.LogInformation(
                    "Invitation issued by {IssuedByUserId} for {Email}", issuedByUserId, email);

                return (InvitationTransforms.ToResponse(invitation, token.Raw, frontendBaseUrl), true);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }, cancellationToken);
    }
}
