using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Application.Services;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.Application.Features.Quiz;

public sealed record SubmitAnswerCommand(
    Guid UserId,
    Guid QuestionId,
    Guid? SelectedOptionId,
    string? SubmittedAnswer,
    int ResponseTimeSeconds,
    Guid? DailyStudyPlanId = null) : ICommand<SubmitAnswerResponse>;

public sealed record SubmitAnswerResponse(
    Guid UserAnswerId,
    bool WasCorrect,
    int Score,
    string EvaluationSummary,
    string Explanation,
    int MasteryScore,
    DateTime NextReviewAtUtc,
    double ForgettingRisk);

public sealed class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.QuestionId).NotEmpty();
        RuleFor(command => command.ResponseTimeSeconds).InclusiveBetween(1, 3600);
    }
}

public sealed class SubmitAnswerCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IQuestionEvaluationService questionEvaluationService,
    IRevisionEngine revisionEngine,
    IClock clock) : ICommandHandler<SubmitAnswerCommand, SubmitAnswerResponse>
{
    public async Task<SubmitAnswerResponse> Handle(SubmitAnswerCommand command, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == command.UserId, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("The requested user was not found.");
        }

        var question = await dbContext.Questions
            .Include(entry => entry.Options)
            .SingleOrDefaultAsync(entry => entry.Id == command.QuestionId, cancellationToken)
            ?? throw new NotFoundException("The requested question was not found.");

        var topic = await dbContext.Topics.SingleAsync(entry => entry.Id == question.TopicId, cancellationToken);
        var progress = await dbContext.TopicProgressEntries.SingleOrDefaultAsync(
                           entry => entry.UserId == command.UserId && entry.TopicId == question.TopicId,
                           cancellationToken)
                       ?? TopicProgress.Create(command.UserId, question.TopicId, clock.UtcNow);

        var schedule = await dbContext.RevisionSchedules.SingleOrDefaultAsync(
                           entry => entry.UserId == command.UserId && entry.TopicId == question.TopicId,
                           cancellationToken)
                       ?? RevisionSchedule.Create(command.UserId, question.TopicId, clock.UtcNow);

        var evaluation = questionEvaluationService.Evaluate(question, command.SubmittedAnswer, command.SelectedOptionId, command.ResponseTimeSeconds);
        var revision = revisionEngine.Recalculate(progress, schedule, question.Difficulty, topic.DecayRate, evaluation, clock.UtcNow);

        if (progress.Id == Guid.Empty || !await dbContext.TopicProgressEntries.AnyAsync(entry => entry.Id == progress.Id, cancellationToken))
        {
            await dbContext.TopicProgressEntries.AddAsync(progress, cancellationToken);
        }

        if (schedule.Id == Guid.Empty || !await dbContext.RevisionSchedules.AnyAsync(entry => entry.Id == schedule.Id, cancellationToken))
        {
            await dbContext.RevisionSchedules.AddAsync(schedule, cancellationToken);
        }

        progress.ApplyTheoryAttempt(evaluation.WasCorrect, command.ResponseTimeSeconds, revision.MasteryScore, revision.ConsistencyScore, clock.UtcNow);
        schedule.Update(clock.UtcNow, revision.NextReviewAtUtc, revision.ReviewIntervalDays, revision.ForgettingRisk, revision.PriorityScore, evaluation.WasCorrect, revision.ReviewQuality, clock.UtcNow);

        var userAnswer = UserAnswer.Create(
            command.UserId,
            command.QuestionId,
            command.DailyStudyPlanId,
            command.SelectedOptionId?.ToString() ?? command.SubmittedAnswer ?? string.Empty,
            evaluation.WasCorrect,
            evaluation.Score,
            command.ResponseTimeSeconds,
            evaluation.EvaluationSummary,
            clock.UtcNow);

        await dbContext.UserAnswers.AddAsync(userAnswer, cancellationToken);

        if (!evaluation.WasCorrect)
        {
            await dbContext.MistakeLogs.AddAsync(
                MistakeLog.Create(
                    command.UserId,
                    question.TopicId,
                    question.Id,
                    null,
                    null,
                    "TheoryQuestion",
                    Math.Max(1, 5 - evaluation.Score / 20),
                    evaluation.EvaluationSummary,
                    clock.UtcNow),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitAnswerResponse(
            userAnswer.Id,
            evaluation.WasCorrect,
            evaluation.Score,
            evaluation.EvaluationSummary,
            question.Explanation,
            revision.MasteryScore,
            revision.NextReviewAtUtc,
            revision.ForgettingRisk);
    }
}
