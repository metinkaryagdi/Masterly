using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Domain.Progress;

public sealed class DailyStudyPlan : Entity
{
    private DailyStudyPlan()
    {
    }

    private DailyStudyPlan(Guid id, Guid userId, DateTime studyDateUtc, DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        StudyDateUtc = studyDateUtc.Date;
        GeneratedAtUtc = createdAtUtc;
        Status = DailyStudyPlanStatus.Active;
    }

    public Guid UserId { get; private set; }

    public DateTime StudyDateUtc { get; private set; }

    public DateTime GeneratedAtUtc { get; private set; }

    public DailyStudyPlanStatus Status { get; private set; }

    public List<DailyStudyPlanItem> Items { get; private set; } = [];

    public static DailyStudyPlan Create(Guid userId, DateTime studyDateUtc, DateTime createdAtUtc)
    {
        return new DailyStudyPlan(Guid.NewGuid(), userId, studyDateUtc, createdAtUtc);
    }

    public void AddItem(
        StudyPlanItemType itemType,
        Guid referenceId,
        Guid? topicId,
        string sourceCategory,
        int sequence,
        double priority,
        DateTime createdAtUtc)
    {
        Items.Add(DailyStudyPlanItem.Create(Id, itemType, referenceId, topicId, sourceCategory, sequence, priority, createdAtUtc));
        Touch(createdAtUtc);
    }

    public void MarkCompleted(DateTime completedAtUtc)
    {
        Status = DailyStudyPlanStatus.Completed;
        Touch(completedAtUtc);
    }
}
