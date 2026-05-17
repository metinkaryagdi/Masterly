using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Domain.Progress;

public sealed class DailyStudyPlanItem : Entity
{
    private DailyStudyPlanItem()
    {
    }

    private DailyStudyPlanItem(
        Guid id,
        Guid dailyStudyPlanId,
        StudyPlanItemType itemType,
        Guid referenceId,
        Guid? topicId,
        string sourceCategory,
        int sequence,
        double priority,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        DailyStudyPlanId = dailyStudyPlanId;
        ItemType = itemType;
        ReferenceId = referenceId;
        TopicId = topicId;
        SourceCategory = sourceCategory;
        Sequence = sequence;
        Priority = priority;
    }

    public Guid DailyStudyPlanId { get; private set; }

    public StudyPlanItemType ItemType { get; private set; }

    public Guid ReferenceId { get; private set; }

    public Guid? TopicId { get; private set; }

    public string SourceCategory { get; private set; } = string.Empty;

    public int Sequence { get; private set; }

    public double Priority { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public static DailyStudyPlanItem Create(
        Guid dailyStudyPlanId,
        StudyPlanItemType itemType,
        Guid referenceId,
        Guid? topicId,
        string sourceCategory,
        int sequence,
        double priority,
        DateTime createdAtUtc)
    {
        return new DailyStudyPlanItem(
            Guid.NewGuid(),
            dailyStudyPlanId,
            itemType,
            referenceId,
            topicId,
            sourceCategory,
            sequence,
            priority,
            createdAtUtc);
    }

    public void MarkCompleted(DateTime completedAtUtc)
    {
        IsCompleted = true;
        CompletedAtUtc = completedAtUtc;
        Touch(completedAtUtc);
    }
}
