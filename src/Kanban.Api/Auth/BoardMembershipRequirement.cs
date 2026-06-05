using Kanban.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Kanban.Api.Auth;

public sealed class BoardMembershipRequirement : IAuthorizationRequirement
{
    public BoardMembershipRequirement(string operation)
    {
        Operation = operation;
    }

    public string Operation { get; }
}

public readonly record struct BoardContext(Guid BoardId, BoardRole ResolvedRole);
