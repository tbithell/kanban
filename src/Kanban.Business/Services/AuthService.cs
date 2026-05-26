using System.Data;
using System.Security.Claims;
using Kanban.Business.Interfaces;
using Kanban.DataAccess.Interfaces;
using Microsoft.Extensions.Logging;

namespace Kanban.Business.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthEventRepository _authEventRepository;
    private readonly IDbConnection _dbConnection;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IAuthEventRepository authEventRepository,
        IDbConnection dbConnection,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _authEventRepository = authEventRepository;
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public Task HandleSignInAsync(
        string googleSub,
        string email,
        ClaimsIdentity claimsIdentity,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
