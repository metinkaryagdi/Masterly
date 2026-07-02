using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Identity;

public sealed class TopicSelfAssessment : Entity
{
    private TopicSelfAssessment()
    {
    }

    private TopicSelfAssessment(Guid id, Guid userId, Guid topicId, SelfAssessmentLevel level, DateTime assessedAtUtc)
        : base(id, assessedAtUtc)
    {
        UserId = userId;
        TopicId = topicId;
        Level = level;
        AssessedAtUtc = assessedAtUtc;
    }

    public Guid UserId { get; private set; }

    public Guid TopicId { get; private set; }

    public SelfAssessmentLevel Level { get; private set; }

    public DateTime AssessedAtUtc { get; private set; }

    public static TopicSelfAssessment Create(Guid userId, Guid topicId, SelfAssessmentLevel level, DateTime assessedAtUtc)
    {
        return new TopicSelfAssessment(Guid.NewGuid(), userId, topicId, level, assessedAtUtc);
    }

    public void Update(SelfAssessmentLevel level, DateTime assessedAtUtc)
    {
        Level = level;
        AssessedAtUtc = assessedAtUtc;
        Touch(assessedAtUtc);
    }

    public int ToMasterySeed() => Level switch
    {
        SelfAssessmentLevel.Novice => 20,
        SelfAssessmentLevel.Familiar => 45,
        SelfAssessmentLevel.Strong => 70,
        _ => 25,
    };
}
