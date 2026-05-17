using System.Text.RegularExpressions;
using TrainingPlatform.Application.Common.Models;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Questions;

namespace TrainingPlatform.Application.Services;

public sealed class QuestionEvaluationService : IQuestionEvaluationService
{
    public AnswerEvaluationResult Evaluate(Question question, string? submittedAnswer, Guid? selectedOptionId, int responseTimeSeconds)
    {
        var normalizedAnswer = Normalize(submittedAnswer);
        var speedScore = Math.Clamp((double)question.EstimatedSolvingTimeSeconds / Math.Max(responseTimeSeconds, 1), 0.25d, 1d);

        return question.QuestionType switch
        {
            QuestionType.MultipleChoice => EvaluateMultipleChoice(question, selectedOptionId, speedScore),
            QuestionType.ShortAnswer => EvaluateShortAnswer(question, normalizedAnswer, speedScore),
            QuestionType.Scenario => EvaluateScenario(question, normalizedAnswer, speedScore),
            _ => throw new InvalidOperationException($"Unsupported question type '{question.QuestionType}'.")
        };
    }

    private static AnswerEvaluationResult EvaluateMultipleChoice(Question question, Guid? selectedOptionId, double speedScore)
    {
        var correctOption = question.Options.Single(option => option.IsCorrect);
        var wasCorrect = selectedOptionId.HasValue && selectedOptionId.Value == correctOption.Id;

        return new AnswerEvaluationResult(
            wasCorrect,
            wasCorrect ? 100 : 0,
            selectedOptionId?.ToString() ?? string.Empty,
            wasCorrect ? "Correct option selected." : "Incorrect option selected.",
            speedScore,
            wasCorrect ? 1d : 0d);
    }

    private static AnswerEvaluationResult EvaluateShortAnswer(Question question, string normalizedAnswer, double speedScore)
    {
        var acceptedAnswers = question.AcceptedAnswers.Select(Normalize).ToList();
        var exactMatch = acceptedAnswers.Any(answer => answer == normalizedAnswer);
        var partialMatch = !exactMatch && acceptedAnswers.Any(answer => normalizedAnswer.Contains(answer) || answer.Contains(normalizedAnswer));
        var score = exactMatch ? 100 : partialMatch ? 70 : 0;

        return new AnswerEvaluationResult(
            score >= question.MinimumPassingScore,
            score,
            normalizedAnswer,
            exactMatch
                ? "Accepted answer matched exactly."
                : partialMatch
                    ? "Answer partially matched the accepted solution."
                    : "Answer did not match the accepted solution.",
            speedScore,
            score / 100d);
    }

    private static AnswerEvaluationResult EvaluateScenario(Question question, string normalizedAnswer, double speedScore)
    {
        var keywords = question.AcceptedAnswers.Select(Normalize).Where(keyword => !string.IsNullOrWhiteSpace(keyword)).ToList();
        if (keywords.Count == 0)
        {
            return new AnswerEvaluationResult(false, 0, normalizedAnswer, "Scenario question is missing evaluation keywords.", speedScore, 0d);
        }

        var matchedKeywords = keywords.Count(keyword => normalizedAnswer.Contains(keyword));
        var coverage = (double)matchedKeywords / keywords.Count;
        var score = (int)Math.Round(coverage * 100, MidpointRounding.AwayFromZero);

        return new AnswerEvaluationResult(
            score >= question.MinimumPassingScore,
            score,
            normalizedAnswer,
            $"Matched {matchedKeywords} of {keywords.Count} expected scenario keywords.",
            speedScore,
            coverage);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lowered = value.Trim().ToLowerInvariant();
        return Regex.Replace(lowered, "\\s+", " ");
    }
}
