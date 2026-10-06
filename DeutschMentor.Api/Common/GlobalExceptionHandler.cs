namespace DeutschMentor.Api.Common;

public sealed record ApiError(string Code, string Message);

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, code) = exception switch
        {
            BadRequestException e => (StatusCodes.Status400BadRequest, e.Code),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, e.Code),
            ForbiddenException e => (StatusCodes.Status403Forbidden, e.Code),
            NotFoundException e => (StatusCodes.Status404NotFound, e.Code),
            ConflictException e => (StatusCodes.Status409Conflict, e.Code),
            TooManyRequestsException e => (StatusCodes.Status429TooManyRequests, e.Code),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "server_error")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        var message = status == StatusCodes.Status500InternalServerError
            ? "Something went wrong. Try again."
            : exception.Message;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ApiError(code, message), ct);
        return true;
    }
}