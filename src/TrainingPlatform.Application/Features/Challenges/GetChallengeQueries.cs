using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;

namespace TrainingPlatform.Application.Features.Challenges;

public sealed record GetCodingChallengeByIdQuery(Guid CodingChallengeId) : IQuery<CodingChallengeDto>;

public sealed class GetCodingChallengeByIdQueryValidator : AbstractValidator<GetCodingChallengeByIdQuery>
{
    public GetCodingChallengeByIdQueryValidator()
    {
        RuleFor(query => query.CodingChallengeId).NotEmpty();
    }
}

public sealed class GetCodingChallengeByIdQueryHandler(ITrainingPlatformDbContext dbContext)
    : IQueryHandler<GetCodingChallengeByIdQuery, CodingChallengeDto>
{
    public async Task<CodingChallengeDto> Handle(GetCodingChallengeByIdQuery query, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.CodingChallenges
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == query.CodingChallengeId, cancellationToken)
            ?? throw new NotFoundException("The requested coding challenge was not found.");

        return new CodingChallengeDto(
            challenge.Id,
            challenge.TopicId,
            challenge.Title,
            challenge.Description,
            challenge.Difficulty,
            challenge.EstimatedMinutes,
            challenge.EvaluationCriteria,
            challenge.StarterCode,
            challenge.ExpectedOutcome,
            challenge.HasAutomatedTests,
            challenge.TestCode);
    }
}

public sealed record GetScenarioChallengeByIdQuery(Guid ScenarioChallengeId) : IQuery<ScenarioChallengeDto>;

public sealed class GetScenarioChallengeByIdQueryValidator : AbstractValidator<GetScenarioChallengeByIdQuery>
{
    public GetScenarioChallengeByIdQueryValidator()
    {
        RuleFor(query => query.ScenarioChallengeId).NotEmpty();
    }
}

public sealed class GetScenarioChallengeByIdQueryHandler(ITrainingPlatformDbContext dbContext)
    : IQueryHandler<GetScenarioChallengeByIdQuery, ScenarioChallengeDto>
{
    public async Task<ScenarioChallengeDto> Handle(GetScenarioChallengeByIdQuery query, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.ScenarioChallenges
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == query.ScenarioChallengeId, cancellationToken)
            ?? throw new NotFoundException("The requested scenario challenge was not found.");

        return new ScenarioChallengeDto(
            challenge.Id,
            challenge.TopicId,
            challenge.Title,
            challenge.Scenario,
            challenge.Difficulty,
            challenge.EstimatedMinutes,
            challenge.EvaluationCriteria,
            challenge.ReferenceSolution);
    }
}
