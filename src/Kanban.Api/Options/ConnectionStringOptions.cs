using System.ComponentModel.DataAnnotations;

namespace Kanban.Api.Options;

public sealed class ConnectionStringOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required, MinLength(1)] public required string Kanban { get; init; }
}
