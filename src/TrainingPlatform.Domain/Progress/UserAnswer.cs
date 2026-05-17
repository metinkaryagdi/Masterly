using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Progress;

public sealed class UserAnswer : Entity
{
    private UserAnswer()
    {
    }

    private UserAnswer(
        Guid id,
        Guid userId,
        Guid questionId,
        Guid? dailyStudyPlanId,
        string submittedAnswer,
        bool wasCorrect,
        int score,
        int responseTimeSeconds,
        string evaluationSummary,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        QuestionId = questionId;
        DailyStudyPlanId = dailyStudyPlanId;
        SubmittedAnswer = submittedAnswer;
        WasCorrect = wasCorrect;
        Score = score;
        ResponseTimeSeconds = responseTimeSeconds;
        EvaluationSummary = evaluationSummary;
    }

    public Guid UserId { get; private set; }

    public Guid QuestionId { get; private set; }

    public Guid? DailyStudyPlanId { get; private set; }

    public string SubmittedAnswer { get; private set; } = string.Empty;

    public bool WasCorrect { get; private set; }

    public int Score { get; private set; }

    public int ResponseTimeSeconds { get; private set; }

    public string EvaluationSummary { get; private set; } = string.Empty;

    public static UserAnswer Create(
        Guid userId,
        Guid questionId,
        Guid? dailyStudyPlanId,
        string submittedAnswer,
        bool wasCorrect,
        int score,
        int responseTimeSeconds,
        string evaluationSummary,
        DateTime createdAtUtc)
    {
        return new UserAnswer(
            Guid.NewGuid(),
            userId,
            questionId,
            dailyStudyPlanId,
            submittedAnswer,
            wasCorrect,
            score,
            responseTimeSeconds,
            evaluationSummary,
            createdAtUtc);
    }
}
