using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.Application.Features.StudyPlans;

internal static class StudyPlanMapper
{
    private sealed record ItemMeta(string Title, TopicDifficulty Difficulty, int EstimatedMinutes);

    public static async Task<DailyStudyPlanDto> MapAsync(DailyStudyPlan plan, ITrainingPlatformDbContext dbContext, CancellationToken cancellationToken)
    {
        var questionIds = plan.Items.Where(item => item.ItemType == StudyPlanItemType.Question).Select(item => item.ReferenceId).ToList();
        var codingIds = plan.Items.Where(item => item.ItemType == StudyPlanItemType.CodingChallenge).Select(item => item.ReferenceId).ToList();
        var scenarioIds = plan.Items.Where(item => item.ItemType == StudyPlanItemType.ScenarioChallenge).Select(item => item.ReferenceId).ToList();
        var topicIds = plan.Items.Where(item => item.TopicId.HasValue).Select(item => item.TopicId!.Value).Distinct().ToList();

        var questionLookup = await dbContext.Questions
            .AsNoTracking()
            .Where(question => questionIds.Contains(question.Id))
            .Select(question => new { question.Id, question.Prompt, question.Difficulty, question.EstimatedSolvingTimeSeconds })
            .ToDictionaryAsync(
                row => row.Id,
                row => new ItemMeta(row.Prompt, row.Difficulty, Math.Max(1, (int)Math.Ceiling(row.EstimatedSolvingTimeSeconds / 60d))),
                cancellationToken);

        var codingLookup = await dbContext.CodingChallenges
            .AsNoTracking()
            .Where(challenge => codingIds.Contains(challenge.Id))
            .Select(challenge => new { challenge.Id, challenge.Title, challenge.Difficulty, challenge.EstimatedMinutes })
            .ToDictionaryAsync(
                row => row.Id,
                row => new ItemMeta(row.Title, row.Difficulty, row.EstimatedMinutes),
                cancellationToken);

        var scenarioLookup = await dbContext.ScenarioChallenges
            .AsNoTracking()
            .Where(challenge => scenarioIds.Contains(challenge.Id))
            .Select(challenge => new { challenge.Id, challenge.Title, challenge.Difficulty, challenge.EstimatedMinutes })
            .ToDictionaryAsync(
                row => row.Id,
                row => new ItemMeta(row.Title, row.Difficulty, row.EstimatedMinutes),
                cancellationToken);

        var topicNameLookup = await dbContext.Topics
            .AsNoTracking()
            .Where(topic => topicIds.Contains(topic.Id))
            .ToDictionaryAsync(topic => topic.Id, topic => topic.Name, cancellationToken);

        return new DailyStudyPlanDto(
            plan.Id,
            plan.UserId,
            plan.StudyDateUtc,
            plan.GeneratedAtUtc,
            plan.Status,
            plan.Items
                .OrderBy(item => item.Sequence)
                .Select(item =>
                {
                    var meta = ResolveMeta(item, questionLookup, codingLookup, scenarioLookup);
                    return new DailyStudyPlanItemDto(
                        item.Id,
                        item.ItemType,
                        item.ReferenceId,
                        item.TopicId,
                        item.TopicId.HasValue && topicNameLookup.TryGetValue(item.TopicId.Value, out var topicName) ? topicName : null,
                        item.SourceCategory,
                        item.Sequence,
                        item.Priority,
                        meta?.Title ?? FallbackTitle(item.ItemType),
                        meta?.Difficulty,
                        meta?.EstimatedMinutes,
                        item.IsCompleted);
                })
                .ToList());
    }

    private static ItemMeta? ResolveMeta(
        DailyStudyPlanItem item,
        IReadOnlyDictionary<Guid, ItemMeta> questionLookup,
        IReadOnlyDictionary<Guid, ItemMeta> codingLookup,
        IReadOnlyDictionary<Guid, ItemMeta> scenarioLookup)
    {
        return item.ItemType switch
        {
            StudyPlanItemType.Question => questionLookup.GetValueOrDefault(item.ReferenceId),
            StudyPlanItemType.CodingChallenge => codingLookup.GetValueOrDefault(item.ReferenceId),
            StudyPlanItemType.ScenarioChallenge => scenarioLookup.GetValueOrDefault(item.ReferenceId),
            _ => null
        };
    }

    private static string FallbackTitle(StudyPlanItemType itemType) => itemType switch
    {
        StudyPlanItemType.Question => "Question",
        StudyPlanItemType.CodingChallenge => "Coding challenge",
        StudyPlanItemType.ScenarioChallenge => "Scenario challenge",
        _ => "Unknown item"
    };
}
