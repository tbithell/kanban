using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required, MinLength(1)] public required string[] AllowedOrigins { get; init; }
}
