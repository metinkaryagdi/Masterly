using System.Text.RegularExpressions;
using TrainingPlatform.Application.Abstractions.Execution;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Services;

/// <summary>
/// Deterministic scoring for challenge submissions: coding challenges score by
/// test pass ratio, scenario challenges by evaluation-criteria coverage
/// (mirroring how scenario questions are scored).
/// </summary>
public static class ChallengeEvaluation
{
    public const int ScenarioPassingScore = 60;

    public static (int Score, ChallengeOutcome Outcome, string Feedback) ForCodeRun(CodeExecutionResult run)
    {
        if (!run.Compiled)
        {
            return (0, ChallengeOutcome.NeedsWork, run.Output);
        }

        if (run.TotalTests == 0)
        {
            return (0, ChallengeOutcome.NeedsWork, "Test paketi sonuç üretmedi.");
        }

        var score = (int)Math.Round(100d * run.PassedTests / run.TotalTests, MidpointRounding.AwayFromZero);
        var outcome = run.FailedTests == 0 && run.PassedTests == run.TotalTests
            ? ChallengeOutcome.Passed
            : ChallengeOutcome.NeedsWork;

        return (score, outcome, run.Output);
    }

    public static (int Score, ChallengeOutcome Outcome, string Feedback) ForScenarioResponse(
        IReadOnlyCollection<string> evaluationCriteria,
        string responseText)
    {
        var criteria = evaluationCriteria.Where(criterion => !string.IsNullOrWhiteSpace(criterion)).ToList();
        if (criteria.Count == 0)
        {
            return (0, ChallengeOutcome.PendingReview, "Bu görevin değerlendirme kriteri yok; incelemeye alındı.");
        }

        var normalizedResponse = Normalize(responseText);
        var addressed = criteria.Where(criterion => normalizedResponse.Contains(Normalize(criterion))).ToList();
        var missing = criteria.Except(addressed).ToList();

        var score = (int)Math.Round(100d * addressed.Count / criteria.Count, MidpointRounding.AwayFromZero);
        var outcome = score >= ScenarioPassingScore ? ChallengeOutcome.Passed : ChallengeOutcome.NeedsWork;

        var feedback = missing.Count == 0
            ? $"{criteria.Count} değerlendirme kriterinin tamamı karşılandı."
            : $"{criteria.Count} kriterden {addressed.Count} tanesi karşılandı. Henüz değinilmeyenler: {string.Join(", ", missing)}.";

        return (score, outcome, feedback);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value.Trim().ToLowerInvariant(), "\\s+", " ");
    }
}
