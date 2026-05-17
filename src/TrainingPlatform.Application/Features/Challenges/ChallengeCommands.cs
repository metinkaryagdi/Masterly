using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Domain.Challenges;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.Application.Features.Challenges;

public sealed record CreateCodingChallengeCommand(
    Guid TopicId,
    string Title,
    string Description,
    TopicDifficulty Difficulty,
    int EstimatedMinutes,
    IReadOnlyCollection<string> EvaluationCriteria,
    string StarterCode,
    string ExpectedOutcome) : ICommand<CodingChallengeDto>;

public sealed record CreateScenarioChallengeCommand(
    Guid TopicId,
    string Title,
    string Scenario,
    TopicDifficulty Difficulty,
    int EstimatedMinutes,
    IReadOnlyCollection<string> EvaluationCriteria,
    string ReferenceSolution) : ICommand<ScenarioChallengeDto>;

public sealed record SubmitCodingSubmissionCommand(Guid UserId, Guid CodingChallengeId, Guid? DailyStudyPlanId, string SubmittedCode, string Notes) : ICommand<SubmissionDto>;

public sealed record SubmitScenarioSubmissionCommand(Guid UserId, Guid ScenarioChallengeId, Guid? DailyStudyPlanId, string ResponseText) : ICommand<SubmissionDto>;

public sealed class CreateCodingChallengeCommandValidator : AbstractValidator<CreateCodingChallengeCommand>
{
    public CreateCodingChallengeCommandValidator()
    {
        RuleFor(command => command.TopicId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.EstimatedMinutes).InclusiveBetween(10, 240);
        RuleFor(command => command.EvaluationCriteria).NotEmpty();
    }
}

public sealed class CreateScenarioChallengeCommandValidator : AbstractValidator<CreateScenarioChallengeCommand>
{
    public CreateScenarioChallengeCommandValidator()
    {
        RuleFor(command => command.TopicId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Scenario).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.EstimatedMinutes).InclusiveBetween(10, 240);
        RuleFor(command => command.EvaluationCriteria).NotEmpty();
    }
}

public sealed class SubmitCodingSubmissionCommandValidator : AbstractValidator<SubmitCodingSubmissionCommand>
{
    public SubmitCodingSubmissionCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.CodingChallengeId).NotEmpty();
        RuleFor(command => command.SubmittedCode).NotEmpty();
    }
}

public sealed class SubmitScenarioSubmissionCommandValidator : AbstractValidator<SubmitScenarioSubmissionCommand>
{
    public SubmitScenarioSubmissionCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.ScenarioChallengeId).NotEmpty();
        RuleFor(command => command.ResponseText).NotEmpty();
    }
}

public sealed class CreateCodingChallengeCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IClock clock) : ICommandHandler<CreateCodingChallengeCommand, CodingChallengeDto>
{
    public async Task<CodingChallengeDto> Handle(CreateCodingChallengeCommand command, CancellationToken cancellationToken)
    {
        await ChallengeCommandGuards.EnsureTopicExists(command.TopicId, dbContext, cancellationToken);

        var challenge = CodingChallenge.Create(
            command.TopicId,
            command.Title,
            command.Description,
            command.Difficulty,
            command.EstimatedMinutes,
            command.EvaluationCriteria,
            command.StarterCode,
            command.ExpectedOutcome,
            clock.UtcNow);

        await dbContext.CodingChallenges.AddAsync(challenge, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CodingChallengeDto(
            challenge.Id,
            challenge.TopicId,
            challenge.Title,
            challenge.Description,
            challenge.Difficulty,
            challenge.EstimatedMinutes,
            challenge.EvaluationCriteria,
            challenge.StarterCode,
            challenge.ExpectedOutcome);
    }
}

public sealed class CreateScenarioChallengeCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IClock clock) : ICommandHandler<CreateScenarioChallengeCommand, ScenarioChallengeDto>
{
    public async Task<ScenarioChallengeDto> Handle(CreateScenarioChallengeCommand command, CancellationToken cancellationToken)
    {
        await ChallengeCommandGuards.EnsureTopicExists(command.TopicId, dbContext, cancellationToken);

        var challenge = ScenarioChallenge.Create(
            command.TopicId,
            command.Title,
            command.Scenario,
            command.Difficulty,
            command.EstimatedMinutes,
            command.EvaluationCriteria,
            command.ReferenceSolution,
            clock.UtcNow);

        await dbContext.ScenarioChallenges.AddAsync(challenge, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

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

public sealed class SubmitCodingSubmissionCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IClock clock) : ICommandHandler<SubmitCodingSubmissionCommand, SubmissionDto>
{
    public async Task<SubmissionDto> Handle(SubmitCodingSubmissionCommand command, CancellationToken cancellationToken)
    {
        await ChallengeCommandGuards.EnsureUserAndChallengeExist(command.UserId, command.CodingChallengeId, dbContext, cancellationToken);

        var submission = CodingSubmission.Create(
            command.UserId,
            command.CodingChallengeId,
            command.DailyStudyPlanId,
            command.SubmittedCode,
            command.Notes,
            clock.UtcNow);

        await dbContext.CodingSubmissions.AddAsync(submission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmissionDto(submission.Id, submission.Score, submission.Outcome, submission.CreatedAtUtc);
    }
}

public sealed class SubmitScenarioSubmissionCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IClock clock) : ICommandHandler<SubmitScenarioSubmissionCommand, SubmissionDto>
{
    public async Task<SubmissionDto> Handle(SubmitScenarioSubmissionCommand command, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == command.UserId, cancellationToken);
        var challengeExists = await dbContext.ScenarioChallenges.AnyAsync(challenge => challenge.Id == command.ScenarioChallengeId, cancellationToken);
        if (!userExists || !challengeExists)
        {
            throw new NotFoundException("User or scenario challenge was not found.");
        }

        var submission = ScenarioSubmission.Create(
            command.UserId,
            command.ScenarioChallengeId,
            command.DailyStudyPlanId,
            command.ResponseText,
            clock.UtcNow);

        await dbContext.ScenarioSubmissions.AddAsync(submission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmissionDto(submission.Id, submission.Score, submission.Outcome, submission.CreatedAtUtc);
    }
}

internal static class ChallengeCommandGuards
{
    public static async Task EnsureTopicExists(Guid topicId, ITrainingPlatformDbContext dbContext, CancellationToken cancellationToken)
    {
        var topicExists = await dbContext.Topics.AnyAsync(topic => topic.Id == topicId, cancellationToken);
        if (!topicExists)
        {
            throw new NotFoundException("The requested topic was not found.");
        }
    }

    public static async Task EnsureUserAndChallengeExist(Guid userId, Guid codingChallengeId, ITrainingPlatformDbContext dbContext, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken);
        var challengeExists = await dbContext.CodingChallenges.AnyAsync(challenge => challenge.Id == codingChallengeId, cancellationToken);
        if (!userExists || !challengeExists)
        {
            throw new NotFoundException("User or coding challenge was not found.");
        }
    }
}
