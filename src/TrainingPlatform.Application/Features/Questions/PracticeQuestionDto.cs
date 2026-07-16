using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Features.Questions;

/// <summary>
/// The read model handed to learners while they solve a question. It is a strict
/// subset of <see cref="QuestionDto"/>: the answer key never crosses this
/// boundary — options carry no <c>IsCorrect</c> flag, there is no
/// <c>AcceptedAnswers</c> list, and the <c>Explanation</c> (which usually spells
/// out the answer) is withheld. Grading happens server-side; the correct answer
/// and explanation are only revealed afterwards through the submit response.
/// </summary>
public sealed record PracticeQuestionOptionDto(Guid Id, string Text, int Order);

public sealed record PracticeQuestionDto(
    Guid Id,
    Guid TopicId,
    QuestionType QuestionType,
    string Prompt,
    TopicDifficulty Difficulty,
    int EstimatedSolvingTimeSeconds,
    int MinimumPassingScore,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<PracticeQuestionOptionDto> Options);
