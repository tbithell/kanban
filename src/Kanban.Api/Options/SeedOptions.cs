using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Options;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    [Required, MinLength(1)] public required string AdminEmail { get; init; }
}
