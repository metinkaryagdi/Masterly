using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.Application.Features.StudyPlans;

internal static class StudyPlanMapper
{
    public static async Task<DailyStudyPlanDto> MapAsync(DailyStudyPlan plan, ITrainingPlatformDbContext dbContext, CancellationToken cancellationToken)
    {
        var questionIds = plan.Items.Where(item => item.ItemType == StudyPlanItemType.Question).Select(item => item.ReferenceId).ToList();
        var codingIds = plan.Items.Where(item => item.ItemType == StudyPlanItemType.CodingChallenge).Select(item => item.ReferenceId).ToList();
        var scenarioIds = plan.Items.Where(item => item.ItemType == StudyPlanItemType.ScenarioChallenge).Select(item => item.ReferenceId).ToList();

        var questionLookup = await dbContext.Questions
            .AsNoTracking()
            .Where(question => questionIds.Contains(question.Id))
            .ToDictionaryAsync(question => question.Id, question => question.Prompt, cancellationToken);

        var codingLookup = await dbContext.CodingChallenges
            .AsNoTracking()
            .Where(challenge => codingIds.Contains(challenge.Id))
            .ToDictionaryAsync(challenge => challenge.Id, challenge => challenge.Title, cancellationToken);

        var scenarioLookup = await dbContext.ScenarioChallenges
            .AsNoTracking()
            .Where(challenge => scenarioIds.Contains(challenge.Id))
            .ToDictionaryAsync(challenge => challenge.Id, challenge => challenge.Title, cancellationToken);

        return new DailyStudyPlanDto(
            plan.Id,
            plan.UserId,
            plan.StudyDateUtc,
            plan.GeneratedAtUtc,
            plan.Status,
            plan.Items
                .OrderBy(item => item.Sequence)
                .Select(item => new DailyStudyPlanItemDto(
                    item.Id,
                    item.ItemType,
                    item.ReferenceId,
                    item.TopicId,
                    item.SourceCategory,
                    item.Sequence,
                    item.Priority,
                    ResolveTitle(item, questionLookup, codingLookup, scenarioLookup),
                    item.IsCompleted))
                .ToList());
    }

    private static string ResolveTitle(
        DailyStudyPlanItem item,
        IReadOnlyDictionary<Guid, string> questionLookup,
        IReadOnlyDictionary<Guid, string> codingLookup,
        IReadOnlyDictionary<Guid, string> scenarioLookup)
    {
        return item.ItemType switch
        {
            StudyPlanItemType.Question => questionLookup.GetValueOrDefault(item.ReferenceId, "Question"),
            StudyPlanItemType.CodingChallenge => codingLookup.GetValueOrDefault(item.ReferenceId, "Coding challenge"),
            StudyPlanItemType.ScenarioChallenge => scenarioLookup.GetValueOrDefault(item.ReferenceId, "Scenario challenge"),
            _ => "Unknown item"
        };
    }
}
