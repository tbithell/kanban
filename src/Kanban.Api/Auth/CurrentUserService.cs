using Microsoft.AspNetCore.Http;

namespace Kanban.Api.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsRegistered =>
        UserId.HasValue;

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst("user_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? SystemRole =>
        _httpContextAccessor.HttpContext?.User.FindFirst("system_role")?.Value;
}
