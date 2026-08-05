using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using CodeCraftNet.Application.Common.Exceptions;

namespace CodeCraftNet.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing request.");
            await WriteProblemAsync(context, exception, environment);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception, IHostEnvironment environment)
    {
        var problem = exception switch
        {
            ValidationException validationException => new ValidationProblemDetails(
                validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()))
            {
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest
            },
            NotFoundException => new ProblemDetails
            {
                Title = "Resource not found.",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            },
            ConflictException => new ProblemDetails
            {
                Title = "Conflict detected.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Title = "Unauthorized.",
                Detail = exception.Message,
                Status = StatusCodes.Status401Unauthorized
            },
            // Unclassified exceptions can carry internal details (connection strings,
            // stack frames, library messages) — only surface them in Development.
            // Production callers get a generic message; the real detail is logged above.
            _ => new ProblemDetails
            {
                Title = "Unexpected server error.",
                Detail = environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError
            }
        };

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
