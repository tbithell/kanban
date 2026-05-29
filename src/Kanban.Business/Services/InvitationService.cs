using System.Data;
using Kanban.Business.Interfaces;
using Kanban.Business.Transforms;
using Kanban.Contracts;
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
    private readonly IDbConnectionFactory _transactionFactory;
    private readonly ILogger<InvitationService> _logger;

    private static readonly ResiliencePipeline RetryPolicy =
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 20,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
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
        IDbConnectionFactory transactionFactory,
        ILogger<InvitationService> logger)
    {
        Verify.That(userRepository).IsNotNull();
        Verify.That(invitationRepository).IsNotNull();
        Verify.That(authEventRepository).IsNotNull();
        Verify.That(dbConnection).IsNotNull();
        Verify.That(transactionFactory).IsNotNull();
        Verify.That(logger).IsNotNull();
        _userRepository = userRepository;
        _invitationRepository = invitationRepository;
        _authEventRepository = authEventRepository;
        _dbConnection = dbConnection;
        _transactionFactory = transactionFactory;
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
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
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
                        "Active invitation refreshed by {IssuedByUserId}", issuedByUserId);
                    return (InvitationTransforms.ToResponse(refreshToken.Raw, newExpiry, frontendBaseUrl), false);
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
                    "Invitation issued by {IssuedByUserId}", issuedByUserId);

                return (InvitationTransforms.ToResponse(invitation, token.Raw, frontendBaseUrl), true);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }, cancellationToken);
    }

    public async Task<User> AcceptAsync(
        string rawToken,
        string googleEmail,
        string googleSub,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        Verify.That(rawToken).IsNotNull().IsNotEmpty();
        Verify.That(googleEmail).IsNotNull().IsNotEmpty();
        Verify.That(googleSub).IsNotNull().IsNotEmpty();
        Verify.That(displayName).IsNotNull().IsNotEmpty();

        return await RetryPolicy.ExecuteAsync(async ct =>
        {
            var tokenHash = InvitationToken.HashRaw(rawToken);
            var acceptedAt = DateTimeOffset.UtcNow;
            var newUserId = Guid.NewGuid();

            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                // Atomic gate: marks consumed_at — serialises concurrent accepts.
                // consumed_by_user_id is set separately below (after user is created) to avoid
                // FK constraint violations in Microsoft.Data.Sqlite 10+ which enforces them by
                // default, unlike previous versions.
                var consumed = await _invitationRepository.TryConsumeAsync(tokenHash, acceptedAt, tx);
                if (!consumed)
                    throw new NotFoundException(
                        "invite.invalid",
                        "This invitation is no longer valid. Please request a new one.");

                var invitation = await _invitationRepository.FindByTokenHashAsync(tokenHash, tx);
                if (invitation is null || !invitation.EmailMatches(googleEmail))
                    throw new BusinessRuleException(
                        "invite.email_mismatch",
                        "This invitation was issued to a different email address.");

                var user = new User(
                    id: newUserId,
                    email: invitation.Email,
                    displayName: displayName,
                    systemRole: SystemRole.Standard,
                    googleSub: googleSub,
                    registeredAt: acceptedAt,
                    lastSignInAt: null);

                await _userRepository.InsertAsync(user, tx);

                // FK constraint satisfied now that the user row exists
                await _invitationRepository.RecordConsumerAsync(tokenHash, newUserId, tx);

                var authEvent = new AuthEvent(
                    Id: Guid.NewGuid(),
                    OccurredAt: acceptedAt,
                    EventType: AuthEventType.InvitationAccepted,
                    UserId: newUserId,
                    Outcome: "success");
                await _authEventRepository.RecordAsync(authEvent, tx);

                tx.Commit();

                _logger.LogInformation("Invitation accepted by user {UserId}", newUserId);
                return user;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }, cancellationToken);
    }
}
