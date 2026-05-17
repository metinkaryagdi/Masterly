using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Features.Questions;

public sealed record QuestionOptionDto(Guid Id, string Text, bool IsCorrect, int Order);

public sealed record QuestionDto(
    Guid Id,
    Guid TopicId,
    QuestionType QuestionType,
    string Prompt,
    string Explanation,
    TopicDifficulty Difficulty,
    int EstimatedSolvingTimeSeconds,
    int MinimumPassingScore,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> AcceptedAnswers,
    IReadOnlyCollection<QuestionOptionDto> Options);
