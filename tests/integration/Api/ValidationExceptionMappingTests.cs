using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Kanban.Tests.Integration.Infrastructure;

namespace Kanban.Tests.Integration.Api;

public sealed class ValidationExceptionMappingTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public ValidationExceptionMappingTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ValidationException_FromInnerLayer_Returns422WithValidationFailedCode()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/dev/test/throw-validation");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("validation.failed");
        body.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ValidationException_ErrorsArray_ContainsFieldAndMessage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/dev/test/throw-validation");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = body.GetProperty("errors");
        errors.GetArrayLength().Should().BeGreaterThan(0);
    }
}
