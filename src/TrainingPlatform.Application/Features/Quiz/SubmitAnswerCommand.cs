using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Application.Common.Persistence;
using TrainingPlatform.Application.Services;
using TrainingPlatform.Domain.Common.Enumerations;
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
    double ForgettingRisk,
    // Revealed only after the answer is submitted, so the client can highlight
    // the right choice without the read model ever leaking it (see Task 2).
    // Null for non-multiple-choice questions.
    Guid? CorrectOptionId);

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

        // Both rows carry a unique (UserId, TopicId) index. Two concurrent
        // submissions for a topic's first-ever answer would otherwise both take
        // the "create" branch and one would fail with a unique violation. Ensure
        // the rows in isolation, adopting the winner's row on a losing race.
        var progress = await EnsureProgressAsync(command.UserId, question.TopicId, cancellationToken);
        var schedule = await EnsureScheduleAsync(command.UserId, question.TopicId, cancellationToken);

        var evaluation = questionEvaluationService.Evaluate(question, command.SubmittedAnswer, command.SelectedOptionId, command.ResponseTimeSeconds);
        var revision = revisionEngine.Recalculate(progress, schedule, question.Difficulty, topic.DecayRate, evaluation, clock.UtcNow);

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

        // Safe to disclose now that the answer has been submitted; the read model
        // (PracticeQuestionDto) withholds it beforehand.
        var correctOptionId = question.QuestionType == QuestionType.MultipleChoice
            ? question.Options.FirstOrDefault(option => option.IsCorrect)?.Id
            : null;

        return new SubmitAnswerResponse(
            userAnswer.Id,
            evaluation.WasCorrect,
            evaluation.Score,
            evaluation.EvaluationSummary,
            question.Explanation,
            revision.MasteryScore,
            revision.NextReviewAtUtc,
            revision.ForgettingRisk,
            correctOptionId);
    }

    private async Task<TopicProgress> EnsureProgressAsync(Guid userId, Guid topicId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.TopicProgressEntries
            .SingleOrDefaultAsync(entry => entry.UserId == userId && entry.TopicId == topicId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = TopicProgress.Create(userId, topicId, clock.UtcNow);
        await dbContext.TopicProgressEntries.AddAsync(created, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return created;
        }
        catch (DbUpdateException ex) when (PersistenceErrors.IsUniqueViolation(ex))
        {
            // A concurrent request inserted it first. Remove on an Added entity
            // detaches our losing insert; adopt the row the winner committed.
            dbContext.TopicProgressEntries.Remove(created);
            return await dbContext.TopicProgressEntries
                .SingleAsync(entry => entry.UserId == userId && entry.TopicId == topicId, cancellationToken);
        }
    }

    private async Task<RevisionSchedule> EnsureScheduleAsync(Guid userId, Guid topicId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.RevisionSchedules
            .SingleOrDefaultAsync(entry => entry.UserId == userId && entry.TopicId == topicId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = RevisionSchedule.Create(userId, topicId, clock.UtcNow);
        await dbContext.RevisionSchedules.AddAsync(created, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return created;
        }
        catch (DbUpdateException ex) when (PersistenceErrors.IsUniqueViolation(ex))
        {
            dbContext.RevisionSchedules.Remove(created);
            return await dbContext.RevisionSchedules
                .SingleAsync(entry => entry.UserId == userId && entry.TopicId == topicId, cancellationToken);
        }
    }
}
