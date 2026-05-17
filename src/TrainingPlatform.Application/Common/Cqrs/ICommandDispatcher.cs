namespace TrainingPlatform.Application.Common.Cqrs;

public interface ICommandDispatcher
{
    Task<TResult> Dispatch<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
