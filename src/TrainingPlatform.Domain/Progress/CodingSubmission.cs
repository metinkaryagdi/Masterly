using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Domain.Progress;

public sealed class CodingSubmission : Entity
{
    private CodingSubmission()
    {
    }

    private CodingSubmission(
        Guid id,
        Guid userId,
        Guid codingChallengeId,
        Guid? dailyStudyPlanId,
        string submittedCode,
        string notes,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        CodingChallengeId = codingChallengeId;
        DailyStudyPlanId = dailyStudyPlanId;
        SubmittedCode = submittedCode;
        Notes = notes;
        Outcome = ChallengeOutcome.PendingReview;
    }

    public Guid UserId { get; private set; }

    public Guid CodingChallengeId { get; private set; }

    public Guid? DailyStudyPlanId { get; private set; }

    public string SubmittedCode { get; private set; } = string.Empty;

    public string Notes { get; private set; } = string.Empty;

    public int? Score { get; private set; }

    public ChallengeOutcome Outcome { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public int? TestsPassed { get; private set; }

    public int? TestsTotal { get; private set; }

    public string Feedback { get; private set; } = string.Empty;

    public static CodingSubmission Create(
        Guid userId,
        Guid codingChallengeId,
        Guid? dailyStudyPlanId,
        string submittedCode,
        string notes,
        DateTime createdAtUtc)
    {
        return new CodingSubmission(Guid.NewGuid(), userId, codingChallengeId, dailyStudyPlanId, submittedCode, notes, createdAtUtc);
    }

    public void Review(int score, ChallengeOutcome outcome, DateTime reviewedAtUtc)
    {
        Score = score;
        Outcome = outcome;
        ReviewedAtUtc = reviewedAtUtc;
        Touch(reviewedAtUtc);
    }

    public void RecordAutomatedEvaluation(
        int score,
        ChallengeOutcome outcome,
        int testsPassed,
        int testsTotal,
        string feedback,
        DateTime evaluatedAtUtc)
    {
        Score = score;
        Outcome = outcome;
        TestsPassed = testsPassed;
        TestsTotal = testsTotal;
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
