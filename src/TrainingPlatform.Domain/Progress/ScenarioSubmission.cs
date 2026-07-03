using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Domain.Progress;

public sealed class ScenarioSubmission : Entity
{
    private ScenarioSubmission()
    {
    }

    private ScenarioSubmission(
        Guid id,
        Guid userId,
        Guid scenarioChallengeId,
        Guid? dailyStudyPlanId,
        string responseText,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        ScenarioChallengeId = scenarioChallengeId;
        DailyStudyPlanId = dailyStudyPlanId;
        ResponseText = responseText;
        Outcome = ChallengeOutcome.PendingReview;
    }

    public Guid UserId { get; private set; }

    public Guid ScenarioChallengeId { get; private set; }

    public Guid? DailyStudyPlanId { get; private set; }

    public string ResponseText { get; private set; } = string.Empty;

    public int? Score { get; private set; }

    public ChallengeOutcome Outcome { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public string Feedback { get; private set; } = string.Empty;

    public static ScenarioSubmission Create(
        Guid userId,
        Guid scenarioChallengeId,
        Guid? dailyStudyPlanId,
        string responseText,
        DateTime createdAtUtc)
    {
        return new ScenarioSubmission(Guid.NewGuid(), userId, scenarioChallengeId, dailyStudyPlanId, responseText, createdAtUtc);
    }

    public void Review(int score, ChallengeOutcome outcome, DateTime reviewedAtUtc)
    {
        Score = score;
        Outcome = outcome;
        ReviewedAtUtc = reviewedAtUtc;
        Touch(reviewedAtUtc);
    }

    public void RecordAutomatedEvaluation(int score, ChallengeOutcome outcome, string feedback, DateTime evaluatedAtUtc)
    {
        Score = score;
        Outcome = outcome;
        Feedback = feedback;
        ReviewedAtUtc = evaluatedAtUtc;
        Touch(evaluatedAtUtc);
    }

    public void AppendFeedback(string additionalFeedback, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(additionalFeedback))
        {
            return;
        }

        Feedback = string.IsNullOrWhiteSpace(Feedback)
            ? additionalFeedback
            : $"{Feedback}\n\n{additionalFeedback}";
        Touch(updatedAtUtc);
    }
}
