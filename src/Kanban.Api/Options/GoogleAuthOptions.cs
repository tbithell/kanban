using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Options;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    [Required, MinLength(1)] public required string ClientId { get; init; }
    [Required, MinLength(1)] public required string ClientSecret { get; init; }
}
