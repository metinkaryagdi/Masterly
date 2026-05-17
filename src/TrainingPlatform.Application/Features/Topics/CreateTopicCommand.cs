using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Topics;

namespace TrainingPlatform.Application.Features.Topics;

public sealed record CreateTopicCommand(
    string Name,
    string Slug,
    string Description,
    TopicDifficulty Difficulty,
    double DecayRate,
    IReadOnlyCollection<Guid> DependencyIds) : ICommand<TopicDto>;

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Slug).NotEmpty().MaximumLength(150).Matches("^[a-z0-9-]+$");
        RuleFor(command => command.Description).NotEmpty().MaximumLength(1000);
        RuleFor(command => command.DecayRate).InclusiveBetween(0.4d, 2.5d);
    }
}

public sealed class CreateTopicCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IClock clock) : ICommandHandler<CreateTopicCommand, TopicDto>
{
    public async Task<TopicDto> Handle(CreateTopicCommand command, CancellationToken cancellationToken)
    {
        var slug = command.Slug.Trim().ToLowerInvariant();
        if (await dbContext.Topics.AnyAsync(topic => topic.Slug == slug, cancellationToken))
        {
            throw new ConflictException("A topic with the same slug already exists.");
        }

        var requestedDependencyIds = command.DependencyIds.Distinct().ToList();
        if (requestedDependencyIds.Count != 0)
        {
            var dependencyCount = await dbContext.Topics.CountAsync(topic => requestedDependencyIds.Contains(topic.Id), cancellationToken);
            if (dependencyCount != requestedDependencyIds.Count)
            {
                throw new NotFoundException("One or more topic dependencies could not be found.");
            }
        }

        var topic = Topic.Create(
            command.Name,
            slug,
            command.Description,
            command.Difficulty,
            command.DecayRate,
            requestedDependencyIds,
            clock.UtcNow);

        await dbContext.Topics.AddAsync(topic, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TopicDto(topic.Id, topic.Name, topic.Slug, topic.Description, topic.Difficulty, topic.DecayRate, topic.Dependencies.Select(item => item.DependsOnTopicId).ToList());
    }
}
