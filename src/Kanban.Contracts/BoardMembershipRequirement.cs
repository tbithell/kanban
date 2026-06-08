using Microsoft.AspNetCore.Authorization;

namespace Kanban.Contracts;

public sealed class BoardMembershipRequirement(string operation) : IAuthorizationRequirement
{
    public string Operation { get; } = operation;
}
