using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.AI;
using TrainingPlatform.Application.Abstractions.Execution;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Application.Common.Persistence;
using TrainingPlatform.Application.Services;
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

public sealed record RunCodingChallengeCommand(Guid UserId, Guid CodingChallengeId, string SubmittedCode) : ICommand<CodeRunDto>;

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

public sealed class RunCodingChallengeCommandValidator : AbstractValidator<RunCodingChallengeCommand>
{
    public RunCodingChallengeCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.CodingChallengeId).NotEmpty();
        RuleFor(command => command.SubmittedCode).NotEmpty().MaximumLength(200_000);
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
            challenge.ExpectedOutcome,
            challenge.HasAutomatedTests,
            challenge.TestCode);
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
    ICodeExecutionService codeExecution,
    ICodeFeedbackService codeFeedback,
    IClock clock) : ICommandHandler<SubmitCodingSubmissionCommand, SubmissionDto>
{
    public async Task<SubmissionDto> Handle(SubmitCodingSubmissionCommand command, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == command.UserId, cancellationToken);
        var challenge = await dbContext.CodingChallenges
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == command.CodingChallengeId, cancellationToken);
        if (!userExists || challenge is null)
        {
            throw new NotFoundException("User or coding challenge was not found.");
        }

        var now = clock.UtcNow;
        var submission = CodingSubmission.Create(
            command.UserId,
            command.CodingChallengeId,
            command.DailyStudyPlanId,
            command.SubmittedCode,
            command.Notes,
            now);

        if (challenge.HasAutomatedTests && codeExecution.IsEnabled)
        {
            try
            {
                var run = await codeExecution.RunAsync(command.SubmittedCode, challenge.TestCode, cancellationToken);
                var (score, outcome, feedback) = ChallengeEvaluation.ForCodeRun(run);
                submission.RecordAutomatedEvaluation(
                    score, outcome, run.Compiled ? run.PassedTests : 0, run.TotalTests, feedback, now);

                await ChallengeCommandGuards.RecordChallengeProgress(
                    dbContext, command.UserId, challenge.TopicId, coding: true,
                    succeeded: outcome == ChallengeOutcome.Passed, now, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Runner unreachable or crashed — the submission stays PendingReview
                // rather than failing the request.
            }
        }

        // Optional AI coaching feedback. It never blocks or fails a submission:
        // when Ollama is disabled or down this silently no-ops.
        try
        {
            var ai = await codeFeedback.EvaluateAsync(
                new CodeFeedbackRequest(challenge.Id, challenge.Description, command.SubmittedCode, "v1"),
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(ai.Content))
            {
                submission.AppendFeedback($"AI geri bildirimi:\n{ai.Content.Trim()}", now);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // AI feedback is best-effort.
        }

        await dbContext.CodingSubmissions.AddAsync(submission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmissionDto(
            submission.Id, submission.Score, submission.Outcome, submission.CreatedAtUtc,
            submission.TestsPassed, submission.TestsTotal, submission.Feedback);
    }
}

public sealed class SubmitScenarioSubmissionCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IScenarioEvaluationService scenarioEvaluation,
    IClock clock) : ICommandHandler<SubmitScenarioSubmissionCommand, SubmissionDto>
{
    public async Task<SubmissionDto> Handle(SubmitScenarioSubmissionCommand command, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(user => user.Id == command.UserId, cancellationToken);
        var challenge = await dbContext.ScenarioChallenges
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == command.ScenarioChallengeId, cancellationToken);
        if (!userExists || challenge is null)
        {
            throw new NotFoundException("User or scenario challenge was not found.");
        }

        var now = clock.UtcNow;
        var submission = ScenarioSubmission.Create(
            command.UserId,
            command.ScenarioChallengeId,
            command.DailyStudyPlanId,
            command.ResponseText,
            now);

        // Deterministic criteria-coverage scoring, mirroring scenario questions.
        var (score, outcome, feedback) = ChallengeEvaluation.ForScenarioResponse(challenge.EvaluationCriteria, command.ResponseText);
        if (outcome != ChallengeOutcome.PendingReview)
        {
            submission.RecordAutomatedEvaluation(score, outcome, feedback, now);
            await ChallengeCommandGuards.RecordChallengeProgress(
                dbContext, command.UserId, challenge.TopicId, coding: false,
                succeeded: outcome == ChallengeOutcome.Passed, now, cancellationToken);
        }

        // Optional AI coaching feedback — best-effort, never blocks the submission.
        try
        {
            var ai = await scenarioEvaluation.EvaluateAsync(
                new ScenarioEvaluationRequest(challenge.Id, challenge.Scenario, command.ResponseText, "v1"),
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(ai.Content))
            {
                submission.AppendFeedback($"AI geri bildirimi:\n{ai.Content.Trim()}", now);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // AI feedback is best-effort.
        }

        await dbContext.ScenarioSubmissions.AddAsync(submission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SubmissionDto(
            submission.Id, submission.Score, submission.Outcome, submission.CreatedAtUtc,
            null, null, submission.Feedback);
    }
}

public sealed class RunCodingChallengeCommandHandler(
    ITrainingPlatformDbContext dbContext,
    ICodeExecutionService codeExecution) : ICommandHandler<RunCodingChallengeCommand, CodeRunDto>
{
    public async Task<CodeRunDto> Handle(RunCodingChallengeCommand command, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.CodingChallenges
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == command.CodingChallengeId, cancellationToken)
            ?? throw new NotFoundException("The requested coding challenge was not found.");

        if (!challenge.HasAutomatedTests)
        {
            return new CodeRunDto(false, false, 0, 0, 0,
                "Bu görevin otomatik testi yok — bunun yerine değerlendirmeye gönder.");
        }

        if (!codeExecution.IsEnabled)
        {
            return new CodeRunDto(false, false, 0, 0, 0,
                "Test çalıştırıcısı bu ortamda kullanılamıyor.");
        }

        var run = await codeExecution.RunAsync(command.SubmittedCode, challenge.TestCode, cancellationToken);
        return new CodeRunDto(true, run.Compiled, run.TotalTests, run.PassedTests, run.FailedTests, run.Output);
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

    /// <summary>Records a challenge attempt (and success) on the user's topic progress.</summary>
    public static async Task RecordChallengeProgress(
        ITrainingPlatformDbContext dbContext,
        Guid userId,
        Guid topicId,
        bool coding,
        bool succeeded,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var progress = await EnsureProgressAsync(dbContext, userId, topicId, now, cancellationToken);

        if (coding)
        {
            progress.ApplyCodingChallengeResult(succeeded, now);
        }
        else
        {
            progress.ApplyScenarioChallengeResult(succeeded, now);
        }
    }

    /// <summary>
    /// Returns the (UserId, TopicId) progress row, creating it if absent. The
    /// insert is isolated so a concurrent first attempt that lost the race to the
    /// unique index is handled by adopting the winner's row instead of failing.
    /// </summary>
    private static async Task<TopicProgress> EnsureProgressAsync(
        ITrainingPlatformDbContext dbContext,
        Guid userId,
        Guid topicId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.TopicProgressEntries
            .SingleOrDefaultAsync(entry => entry.UserId == userId && entry.TopicId == topicId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = TopicProgress.Create(userId, topicId, now);
        await dbContext.TopicProgressEntries.AddAsync(created, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return created;
        }
        catch (DbUpdateException ex) when (PersistenceErrors.IsUniqueViolation(ex))
        {
            dbContext.TopicProgressEntries.Remove(created);
            return await dbContext.TopicProgressEntries
                .SingleAsync(entry => entry.UserId == userId && entry.TopicId == topicId, cancellationToken);
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
