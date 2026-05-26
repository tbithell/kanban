using FluentAssertions;
using Kanban.Domain.Exceptions;

namespace Kanban.Tests.Unit.Domain;

public class ExceptionTests
{
    [Fact]
    public void NotFoundException_HasCorrectCode_AndInheritsFromDomainException()
    {
        var ex = new NotFoundException("not_found", "Resource not found");
        ex.Should().BeAssignableTo<DomainException>();
        ex.Code.Should().Be("not_found");
        ex.Message.Should().Be("Resource not found");
    }

    [Fact]
    public void ForbiddenException_HasCorrectCode_AndInheritsFromDomainException()
    {
        var ex = new ForbiddenException("access.denied", "Access denied");
        ex.Should().BeAssignableTo<DomainException>();
        ex.Code.Should().Be("access.denied");
    }

    [Fact]
    public void ConflictException_HasCorrectCode_AndInheritsFromDomainException()
    {
        var ex = new ConflictException("duplicate", "Already exists");
        ex.Should().BeAssignableTo<DomainException>();
        ex.Code.Should().Be("duplicate");
    }

    [Fact]
    public void BusinessRuleException_HasCorrectCode_AndInheritsFromDomainException()
    {
        var ex = new BusinessRuleException("rule.violated", "Business rule violated");
        ex.Should().BeAssignableTo<DomainException>();
        ex.Code.Should().Be("rule.violated");
    }

    [Fact]
    public void DataAccessException_HasCorrectCode_AndInheritsFromInfrastructureException()
    {
        var ex = new DataAccessException("db.failure", "Database failure");
        ex.Should().BeAssignableTo<InfrastructureException>();
        ex.Code.Should().Be("db.failure");
    }

    [Fact]
    public void ExternalServiceException_HasCorrectCode_AndInheritsFromInfrastructureException()
    {
        var ex = new ExternalServiceException("oauth.failure", "OAuth failure");
        ex.Should().BeAssignableTo<InfrastructureException>();
        ex.Code.Should().Be("oauth.failure");
    }

    [Fact]
    public void DomainException_InheritsFromKanbanException()
    {
        var ex = new NotFoundException("x", "x");
        ex.Should().BeAssignableTo<KanbanException>();
    }

    [Fact]
    public void InfrastructureException_InheritsFromKanbanException()
    {
        var ex = new DataAccessException("x", "x");
        ex.Should().BeAssignableTo<KanbanException>();
    }
}
