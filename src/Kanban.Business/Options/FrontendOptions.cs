namespace Kanban.Business.Options;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public required string BaseUrl { get; init; }
}
