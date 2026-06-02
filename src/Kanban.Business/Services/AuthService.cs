using System.Data;
using System.Security.Claims;
using Kanban.Business.Interfaces;
using Kanban.Business.Transforms;
using Kanban.Contracts;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.Events;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Kanban.Business.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthEventRepository _authEventRepository;
    private readonly IDbConnection _dbConnection;
    private readonly IDbConnectionFactory _transactionFactory;
    private readonly ILogger<AuthService> _logger;

    private static readonly ResiliencePipeline RetryPolicy =
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex.Message.Contains("SQLITE_BUSY", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase)),
            })
            .Build();

    public AuthService(
        IUserRepository userRepository,
        IAuthEventRepository authEventRepository,
        IDbConnection dbConnection,
        IDbConnectionFactory transactionFactory,
        ILogger<AuthService> logger)
    {
        Verify.That(userRepository).IsNotNull();
        Verify.That(authEventRepository).IsNotNull();
        Verify.That(dbConnection).IsNotNull();
        Verify.That(transactionFactory).IsNotNull();
        Verify.That(logger).IsNotNull();
        _userRepository = userRepository;
        _authEventRepository = authEventRepository;
        _dbConnection = dbConnection;
        _transactionFactory = transactionFactory;
        _logger = logger;
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Verify.That(userId).IsNotDefault();
        var user = await _userRepository.FindByIdAsync(userId);
        return user is null ? null : UserTransforms.ToDto(user);
    }

    public async Task HandleSignInAsync(
        string googleSub,
        string email,
        ClaimsIdentity claimsIdentity,
        CancellationToken cancellationToken = default)
    {
        Verify.That(googleSub).IsNotNull().IsNotEmpty();
        Verify.That(email).IsNotNull().IsNotEmpty();
        Verify.That(claimsIdentity).IsNotNull();

        await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                var user = await _userRepository.FindByGoogleSubAsync(googleSub, tx);

                if (user is null)
                {
                    var userByEmail = await _userRepository.FindByEmailAsync(email, tx);
                    if (userByEmail is not null && userByEmail.GoogleSub is null)
                    {
                        await _userRepository.LinkGoogleSubAsync(userByEmail.Id, googleSub, tx);
                        userByEmail.LinkGoogleIdentity(googleSub);
                        user = userByEmail;
                    }
                }

                if (user is not null)
                {
                    claimsIdentity.AddClaim(new Claim("user_id", user.Id.ToString()));
                    claimsIdentity.AddClaim(new Claim("system_role",
                        user.SystemRole == SystemRole.Admin ? "admin" : "standard"));

                    var signedInAt = DateTimeOffset.UtcNow;
                    await _userRepository.UpdateLastSignInAsync(user.Id, signedInAt, tx);

                    var authEvent = new AuthEvent(
                        Id: Guid.NewGuid(),
                        OccurredAt: signedInAt,
                        EventType: AuthEventType.SignIn,
                        UserId: user.Id,
                        Outcome: "success");
                    await _authEventRepository.RecordAsync(authEvent, tx);

                    _logger.LogInformation(
                        "User {UserId} signed in with role {SystemRole}", user.Id, user.SystemRole);
                }
                else
                {
                    var authEvent = new AuthEvent(
                        Id: Guid.NewGuid(),
                        OccurredAt: DateTimeOffset.UtcNow,
                        EventType: AuthEventType.SignIn,
                        UserId: null,
                        Outcome: "unregistered");
                    await _authEventRepository.RecordAsync(authEvent, tx);

                    _logger.LogWarning("Unregistered Google user attempted sign-in");
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }, cancellationToken);
    }
}
