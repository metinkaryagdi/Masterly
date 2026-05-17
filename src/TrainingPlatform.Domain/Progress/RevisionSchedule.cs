using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Progress;

public sealed class RevisionSchedule : Entity
{
    private RevisionSchedule()
    {
    }

    private RevisionSchedule(Guid id, Guid userId, Guid topicId, DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        TopicId = topicId;
        ReviewIntervalDays = 1;
        ForgettingRisk = 1;
        PriorityScore = 1;
        NextReviewAtUtc = createdAtUtc.Date.AddDays(1);
    }

    public Guid UserId { get; private set; }

    public Guid TopicId { get; private set; }

    public DateTime? LastReviewedAtUtc { get; private set; }

    public DateTime NextReviewAtUtc { get; private set; }

    public int ReviewIntervalDays { get; private set; }

    public double ForgettingRisk { get; private set; }

    public double PriorityScore { get; private set; }

    public bool LastReviewWasSuccessful { get; private set; }

    public double LastReviewQuality { get; private set; }

    public static RevisionSchedule Create(Guid userId, Guid topicId, DateTime createdAtUtc)
    {
        return new RevisionSchedule(Guid.NewGuid(), userId, topicId, createdAtUtc);
    }

    public void Update(
        DateTime? lastReviewedAtUtc,
        DateTime nextReviewAtUtc,
        int reviewIntervalDays,
        double forgettingRisk,
        double priorityScore,
        bool lastReviewWasSuccessful,
        double lastReviewQuality,
        DateTime updatedAtUtc)
    {
        LastReviewedAtUtc = lastReviewedAtUtc;
        NextReviewAtUtc = nextReviewAtUtc;
        ReviewIntervalDays = reviewIntervalDays;
        ForgettingRisk = forgettingRisk;
        PriorityScore = priorityScore;
        LastReviewWasSuccessful = lastReviewWasSuccessful;
        LastReviewQuality = lastReviewQuality;
        Touch(updatedAtUtc);
    }
}
