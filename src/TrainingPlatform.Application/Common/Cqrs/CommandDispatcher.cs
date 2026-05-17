using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TrainingPlatform.Application.Common.Cqrs;

public sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public async Task<TResult> Dispatch<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        await ValidateAsync(command, cancellationToken);

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return await handler.Handle(command, cancellationToken);
    }

    private async Task ValidateAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
    {
        var validators = serviceProvider.GetServices<IValidator<TCommand>>().ToList();
        if (validators.Count == 0)
        {
            return;
        }

        var context = new ValidationContext<TCommand>(command);
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
