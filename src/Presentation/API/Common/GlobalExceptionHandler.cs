using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanlamClaims.Application.Common.Exceptions;
using SanlamClaims.Domain.Exceptions;

namespace SanlamClaims.API.Common;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            _logger.LogWarning(
                "Validation failed for {Method} {Path}: {Errors}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                string.Join(", ", validationException.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                },
                cancellationToken);
        }

        if (exception is ClientNotFoundException or PolicyNotFoundException or ClaimNotFoundException)
        {
            return await WriteWarningProblemAsync(httpContext, StatusCodes.Status404NotFound, exception, cancellationToken);
        }

        if (exception is PolicyClientMismatchException)
        {
            return await WriteWarningProblemAsync(httpContext, StatusCodes.Status400BadRequest, exception, cancellationToken);
        }

        if (exception is InvalidClaimStateTransitionException or DbUpdateConcurrencyException)
        {
            var title = exception is DbUpdateConcurrencyException
                ? "The claim was modified by another request. Reload it and try again."
                : exception.Message;

            return await WriteWarningProblemAsync(httpContext, StatusCodes.Status409Conflict, exception, cancellationToken, title);
        }

        if (exception is ExternalSystemException externalSystemException)
        {
            _logger.LogError(
                externalSystemException,
                "External system '{SystemName}' call failed for {Method} {Path}",
                externalSystemException.SystemName,
                httpContext.Request.Method,
                httpContext.Request.Path);

            return await WriteProblemAsync(
                httpContext,
                StatusCodes.Status502BadGateway,
                new ProblemDetails
                {
                    Status = StatusCodes.Status502BadGateway,
                    Title = $"The {externalSystemException.SystemName} system is currently unavailable. Please try again shortly.",
                },
                cancellationToken);
        }

        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        return false;
    }

    private static async Task<bool> WriteProblemAsync(HttpContext httpContext, int statusCode, ProblemDetails problem, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private async Task<bool> WriteWarningProblemAsync(
        HttpContext httpContext,
        int statusCode,
        Exception exception,
        CancellationToken cancellationToken,
        string? title = null)
    {
        _logger.LogWarning(
            "{ExceptionType} on {Method} {Path}: {Message}",
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.Message);

        return await WriteProblemAsync(
            httpContext,
            statusCode,
            new ProblemDetails { Status = statusCode, Title = title ?? exception.Message },
            cancellationToken);
    }
}
