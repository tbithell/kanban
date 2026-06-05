using System.Diagnostics;
using FluentValidation;
using Kanban.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.ErrorHandling;

public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Extensions =
                    {
                        ["code"] = "validation.failed",
                        ["errors"] = validationException.Errors
                            .Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                            .ToArray(),
                        ["traceId"] = Activity.Current?.Id
                    }
                }
            });
        }

        if (exception is not DomainException domainException)
            return false;

        var statusCode = domainException switch
        {
            NotFoundException n when n.Code == "invite.invalid" => StatusCodes.Status410Gone,
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ConflictException => StatusCodes.Status409Conflict,
            BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Title = domainException.Message,
                Status = statusCode,
                Extensions =
                {
                    ["code"] = domainException.Code,
                    ["traceId"] = Activity.Current?.Id
                }
            }
        });
    }
}
