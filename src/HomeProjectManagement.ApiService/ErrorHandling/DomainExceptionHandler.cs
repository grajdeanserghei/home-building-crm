using HomeProjectManagement.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HomeProjectManagement.ApiService.ErrorHandling;

/// <summary>
/// Translates deliberate <see cref="DomainException"/>s raised by the domain core into RFC 7807
/// <c>ProblemDetails</c> responses: a <see cref="DomainValidationException"/> becomes 400 Bad Request,
/// a <see cref="DomainConflictException"/> becomes 409 Conflict. Any other exception is left
/// unhandled (returns <c>false</c>) so it surfaces as a 500, keeping genuine defects visible rather
/// than masking them as client errors.
/// </summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        var (statusCode, title) = domainException switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            DomainConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status400BadRequest, "Bad request"),
        };

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = domainException.Message,
        };

        if (domainException is DomainValidationException { ParameterName: { } parameterName })
        {
            problemDetails.Extensions["parameter"] = parameterName;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = domainException,
            ProblemDetails = problemDetails,
        });
    }
}
