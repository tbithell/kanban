namespace Kanban.Api.Auth;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    bool IsRegistered { get; }
    Guid? UserId { get; }
    string? SystemRole { get; }
}
