using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Progress;

public sealed class MistakeLog : Entity
{
    private MistakeLog()
    {
    }

    private MistakeLog(
        Guid id,
        Guid userId,
        Guid topicId,
        Guid? questionId,
        Guid? codingChallengeId,
        Guid? scenarioChallengeId,
        string failureType,
        int severity,
        string notes,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        TopicId = topicId;
        QuestionId = questionId;
        CodingChallengeId = codingChallengeId;
        ScenarioChallengeId = scenarioChallengeId;
        FailureType = failureType;
        Severity = severity;
        Notes = notes;
    }

    public Guid UserId { get; private set; }

    public Guid TopicId { get; private set; }

    public Guid? QuestionId { get; private set; }

    public Guid? CodingChallengeId { get; private set; }

    public Guid? ScenarioChallengeId { get; private set; }

    public string FailureType { get; private set; } = string.Empty;

    public int Severity { get; private set; }

    public string Notes { get; private set; } = string.Empty;

    public DateTime? ResolvedAtUtc { get; private set; }

    public static MistakeLog Create(
        Guid userId,
        Guid topicId,
        Guid? questionId,
        Guid? codingChallengeId,
        Guid? scenarioChallengeId,
        string failureType,
        int severity,
        string notes,
        DateTime createdAtUtc)
    {
        return new MistakeLog(
            Guid.NewGuid(),
            userId,
            topicId,
            questionId,
            codingChallengeId,
            scenarioChallengeId,
            failureType,
            severity,
            notes,
            createdAtUtc);
    }

    public void Resolve(DateTime resolvedAtUtc)
    {
        ResolvedAtUtc = resolvedAtUtc;
        Touch(resolvedAtUtc);
    }
}
