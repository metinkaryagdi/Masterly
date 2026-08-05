using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CodeCraftNet.Application.Abstractions.Persistence;
using CodeCraftNet.Application.Common.Cqrs;
using CodeCraftNet.Application.Common.Exceptions;

namespace CodeCraftNet.Application.Features.Topics;

public sealed record GetTopicByIdQuery(Guid TopicId) : IQuery<TopicDto>;

public sealed class GetTopicByIdQueryValidator : AbstractValidator<GetTopicByIdQuery>
{
    public GetTopicByIdQueryValidator()
    {
        RuleFor(query => query.TopicId).NotEmpty();
    }
}

public sealed class GetTopicByIdQueryHandler(ICodeCraftNetDbContext dbContext) : IQueryHandler<GetTopicByIdQuery, TopicDto>
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
