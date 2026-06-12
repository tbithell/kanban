using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Options;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    [Range(1, int.MaxValue)] public int AnonymousPermitLimit { get; init; } = 10;
    [Range(1, int.MaxValue)] public int AuthenticatedPermitLimit { get; init; } = 100;
    [Range(1, int.MaxValue)] public int MutatingPermitLimit { get; init; } = 30;
}
