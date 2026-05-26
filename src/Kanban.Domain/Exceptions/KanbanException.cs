namespace Kanban.Domain.Exceptions;

public abstract class KanbanException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public abstract class DomainException(string code, string message) : KanbanException(code, message);

public abstract class InfrastructureException(string code, string message) : KanbanException(code, message);

public sealed class NotFoundException(string code, string message) : DomainException(code, message);

public sealed class ForbiddenException(string code, string message) : DomainException(code, message);

public sealed class ConflictException(string code, string message) : DomainException(code, message);

public sealed class BusinessRuleException(string code, string message) : DomainException(code, message);

public sealed class DataAccessException(string code, string message) : InfrastructureException(code, message);

public sealed class ExternalServiceException(string code, string message) : InfrastructureException(code, message);
