using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Features.StudyPlans;

public sealed record DailyStudyPlanItemDto(
    Guid Id,
    StudyPlanItemType ItemType,
    Guid ReferenceId,
    Guid? TopicId,
    string? TopicName,
    string SourceCategory,
    int Sequence,
    double Priority,
    string Title,
    TopicDifficulty? Difficulty,
    int? EstimatedMinutes,
    bool IsCompleted);

public sealed record DailyStudyPlanDto(
    Guid Id,
    Guid UserId,
    DateTime StudyDateUtc,
    DateTime GeneratedAtUtc,
    DailyStudyPlanStatus Status,
    IReadOnlyCollection<DailyStudyPlanItemDto> Items);
