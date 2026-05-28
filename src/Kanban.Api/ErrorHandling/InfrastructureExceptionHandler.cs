using System.Diagnostics;
using Kanban.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.ErrorHandling;

public sealed class InfrastructureExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not InfrastructureException infrastructureException)
            return false;

        var statusCode = infrastructureException switch
        {
            DataAccessException => StatusCodes.Status500InternalServerError,
            ExternalServiceException => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Title = "A service error occurred.",
                Status = statusCode,
                Extensions =
                {
                    ["code"] = infrastructureException.Code,
                    ["traceId"] = Activity.Current?.Id
                }
            }
        });
    }
}
