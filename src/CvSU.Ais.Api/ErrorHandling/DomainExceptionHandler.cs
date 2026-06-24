using CvSU.Ais.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CvSU.Ais.Api.ErrorHandling;

/// <summary>
/// Maps domain exceptions to RFC-7807 problem-details responses, surfacing the
/// stable <see cref="DomainException.Code"/> so clients can branch on rule
/// identity rather than parsing messages. Unhandled exceptions fall through to
/// the framework's 500 handler.
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code) = exception switch
        {
            UnauthorizedTransitionException e => (StatusCodes.Status403Forbidden, e.Code),
            DomainException e => (StatusCodes.Status422UnprocessableEntity, e.Code),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found"),
            _ => (0, string.Empty),
        };

        if (status == 0)
            return false;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = exception.Message,
                Extensions = { ["code"] = code },
            },
        });
    }
}
