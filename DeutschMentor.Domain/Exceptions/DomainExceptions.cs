namespace DeutschMentor.Domain.Exceptions;

public abstract class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class BadRequestException(string code, string message) : DomainException(code, message);

public sealed class UnauthorizedException(string code, string message) : DomainException(code, message);

public sealed class ForbiddenException(string code, string message) : DomainException(code, message);

public sealed class NotFoundException(string code, string message) : DomainException(code, message);

public sealed class ConflictException(string code, string message) : DomainException(code, message);

public sealed class TooManyRequestsException(string code, string message) : DomainException(code, message);