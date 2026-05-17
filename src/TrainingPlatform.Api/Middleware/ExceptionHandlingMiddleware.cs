using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Application.Common.Exceptions;

namespace TrainingPlatform.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await WriteProblemAsync(context, exception);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception)
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
            _ => new ProblemDetails
            {
                Title = "Unexpected server error.",
                Detail = exception.Message,
                Status = StatusCodes.Status500InternalServerError
            }
        };

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
