using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.AI;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Application.Services;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Questions;

namespace TrainingPlatform.Application.Features.Questions;

/// <summary>
/// Asks the AI model to author a fresh Turkish question for a topic, audits the
/// candidate deterministically, and only persists it when the audit passes. The
/// model is retried up to <see cref="MaxAttempts"/> times; the last audit report
/// is returned either way so an admin can see why a candidate was rejected.
/// </summary>
public sealed record GenerateQuestionCommand(
    Guid TopicId,
    QuestionType QuestionType,
    TopicDifficulty Difficulty,
    IReadOnlyCollection<string> Tags,
    int MaxAttempts = 3) : ICommand<GeneratedQuestionResult>;

public sealed record GeneratedQuestionResult(
    bool Persisted,
    int Attempts,
    IReadOnlyList<string> AuditIssues,
    QuestionDto? Question);

public sealed class GenerateQuestionCommandValidator : AbstractValidator<GenerateQuestionCommand>
{
    public GenerateQuestionCommandValidator()
    {
        RuleFor(command => command.TopicId).NotEmpty();
        RuleFor(command => command.QuestionType).IsInEnum();
        RuleFor(command => command.Difficulty).IsInEnum();
        RuleFor(command => command.MaxAttempts).InclusiveBetween(1, 5);
    }
}

public sealed class GenerateQuestionCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IQuestionGenerationService questionGeneration,
    IGeneratedQuestionAuditor auditor,
    IClock clock) : ICommandHandler<GenerateQuestionCommand, GeneratedQuestionResult>
{
    private const string PromptVersion = "tr-v1";

    public async Task<GeneratedQuestionResult> Handle(GenerateQuestionCommand command, CancellationToken cancellationToken)
    {
        var topic = await dbContext.Topics
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == command.TopicId, cancellationToken)
            ?? throw new NotFoundException("The requested topic was not found.");

        var existingPrompts = await dbContext.Questions
            .Where(question => question.TopicId == command.TopicId)
            .Select(question => question.Prompt)
            .ToListAsync(cancellationToken);

        var tags = command.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? [];
        var request = new QuestionGenerationRequest(
            topic.Id, topic.Name, command.QuestionType, command.Difficulty, tags, PromptVersion);

        IReadOnlyList<string> lastIssues = ["Model geçerli bir soru üretemedi."];

        for (var attempt = 1; attempt <= command.MaxAttempts; attempt++)
        {
            GeneratedQuestion candidate;
            try
            {
                candidate = await questionGeneration.GenerateAsync(request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastIssues = [$"Model çağrısı başarısız oldu: {ex.Message}"];
                continue;
            }

            var report = auditor.Audit(command.QuestionType, candidate, existingPrompts);
            if (!report.Passed)
            {
                lastIssues = report.Issues;
                continue;
            }

            var question = Persist(command, candidate, tags);
            await dbContext.Questions.AddAsync(question, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new GeneratedQuestionResult(true, attempt, Array.Empty<string>(), Map(question));
        }

        return new GeneratedQuestionResult(false, command.MaxAttempts, lastIssues, null);
    }

    private Question Persist(GenerateQuestionCommand command, GeneratedQuestion candidate, IReadOnlyCollection<string> tags)
    {
        return Question.Create(
            command.TopicId,
            command.QuestionType,
            candidate.Prompt.Trim(),
            candidate.Explanation.Trim(),
            command.Difficulty,
            EstimatedSeconds(command.Difficulty),
            PassingScore(command.QuestionType),
            tags.Count > 0 ? tags : DefaultTags(command.QuestionType),
            command.QuestionType == QuestionType.MultipleChoice
                ? Array.Empty<string>()
                : candidate.AcceptedAnswers.Where(answer => !string.IsNullOrWhiteSpace(answer)).ToList(),
            command.QuestionType == QuestionType.MultipleChoice
                ? candidate.Options.Select(option => (option.Text.Trim(), option.IsCorrect))
                : Array.Empty<(string, bool)>(),
            clock.UtcNow);
    }

    private static int EstimatedSeconds(TopicDifficulty difficulty) => difficulty switch
    {
        TopicDifficulty.Fundamental => 45,
        TopicDifficulty.Intermediate => 75,
        TopicDifficulty.Advanced => 150,
        TopicDifficulty.Expert => 210,
        _ => 60
    };

    private static int PassingScore(QuestionType type) => type switch
    {
        QuestionType.MultipleChoice => 100,
        QuestionType.ShortAnswer => 70,
        QuestionType.Scenario => 60,
        _ => 70
    };

    private static string[] DefaultTags(QuestionType type) => type == QuestionType.Scenario
        ? ["scenario", "ai"]
        : ["ai"];

    private static QuestionDto Map(Question question) => new(
        question.Id,
        question.TopicId,
        question.QuestionType,
        question.Prompt,
        question.Explanation,
        question.Difficulty,
        question.EstimatedSolvingTimeSeconds,
        question.MinimumPassingScore,
        question.Tags,
        question.AcceptedAnswers,
        question.Options.OrderBy(option => option.Order)
            .Select(option => new QuestionOptionDto(option.Id, option.Text, option.IsCorrect, option.Order))
            .ToList());
}
