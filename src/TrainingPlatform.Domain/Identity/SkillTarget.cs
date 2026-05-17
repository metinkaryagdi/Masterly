using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Identity;

public sealed class SkillTarget : Entity
{
    private SkillTarget()
    {
    }

    private SkillTarget(Guid id, Guid userId, Guid topicId, int targetMasteryScore, DateTime? targetDateUtc, DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        TopicId = topicId;
        TargetMasteryScore = targetMasteryScore;
        TargetDateUtc = targetDateUtc;
    }

    public Guid UserId { get; private set; }

    public Guid TopicId { get; private set; }

    public int TargetMasteryScore { get; private set; }

    public DateTime? TargetDateUtc { get; private set; }

    public static SkillTarget Create(Guid userId, Guid topicId, int targetMasteryScore, DateTime? targetDateUtc, DateTime createdAtUtc)
    {
        return new SkillTarget(Guid.NewGuid(), userId, topicId, targetMasteryScore, targetDateUtc, createdAtUtc);
    }
}
