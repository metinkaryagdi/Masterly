using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;

namespace TrainingPlatform.Application.Features.Topics;

public sealed record GetTopicByIdQuery(Guid TopicId) : IQuery<TopicDto>;

public sealed class GetTopicByIdQueryValidator : AbstractValidator<GetTopicByIdQuery>
{
    public GetTopicByIdQueryValidator()
    {
        RuleFor(query => query.TopicId).NotEmpty();
    }
}

public sealed class GetTopicByIdQueryHandler(ITrainingPlatformDbContext dbContext) : IQueryHandler<GetTopicByIdQuery, TopicDto>
{
    public async Task<TopicDto> Handle(GetTopicByIdQuery query, CancellationToken cancellationToken)
    {
        var topic = await dbContext.Topics
            .AsNoTracking()
            .Include(entry => entry.Dependencies)
            .SingleOrDefaultAsync(entry => entry.Id == query.TopicId, cancellationToken)
            ?? throw new NotFoundException("The requested topic was not found.");

        var summaries = await TopicContentSummary.LoadAsync(dbContext, [topic.Id], cancellationToken);
        var summary = summaries.GetValueOrDefault(topic.Id, TopicContentSummary.Empty);

        return new TopicDto(
            topic.Id,
            topic.Name,
            topic.Slug,
            topic.Description,
            topic.Difficulty,
            topic.DecayRate,
            topic.Dependencies.Select(dependency => dependency.DependsOnTopicId).ToList(),
            summary.QuestionCount,
            summary.CodingChallengeCount,
            summary.ScenarioCount,
            summary.SampleQuestions);
    }
}
