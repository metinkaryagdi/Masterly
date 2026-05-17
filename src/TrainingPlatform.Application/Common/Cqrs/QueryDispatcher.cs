using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TrainingPlatform.Application.Common.Cqrs;

public sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<TResult> Dispatch<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        await ValidateAsync(query, cancellationToken);

        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.Handle(query, cancellationToken);
    }

    private async Task ValidateAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
    {
        var validators = serviceProvider.GetServices<IValidator<TQuery>>().ToList();
        if (validators.Count == 0)
        {
            return;
        }

        var context = new ValidationContext<TQuery>(query);
        var validationResults = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }
    }
}
