using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Common.Cqrs;

namespace TrainingPlatform.Application.Features.Topics;

public sealed record GetTopicsQuery : IQuery<IReadOnlyCollection<TopicDto>>;

public sealed class GetTopicsQueryValidator : AbstractValidator<GetTopicsQuery>
{
}

public sealed class GetTopicsQueryHandler(ITrainingPlatformDbContext dbContext) : IQueryHandler<GetTopicsQuery, IReadOnlyCollection<TopicDto>>
{
    public async Task<IReadOnlyCollection<TopicDto>> Handle(GetTopicsQuery query, CancellationToken cancellationToken)
    {
        var topics = await dbContext.Topics
            .AsNoTracking()
            .Include(topic => topic.Dependencies)
            .OrderBy(topic => topic.Name)
            .Select(topic => new TopicDto(
                topic.Id,
                topic.Name,
                topic.Slug,
                topic.Description,
                topic.Difficulty,
                topic.DecayRate,
                topic.Dependencies.Select(dependency => dependency.DependsOnTopicId).ToList()))
            .ToListAsync(cancellationToken);

        return topics;
    }
}
