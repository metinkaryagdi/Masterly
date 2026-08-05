using CodeCraftNet.Domain.Challenges;
using CodeCraftNet.Domain.Identity;
using CodeCraftNet.Domain.Progress;
using CodeCraftNet.Domain.Questions;
using CodeCraftNet.Domain.Topics;

namespace CodeCraftNet.Application.Services;

public interface IDailyStudyPlanService
{
    DailyStudyPlan BuildPlan(
        User user,
        IReadOnlyCollection<Topic> topics,
        IReadOnlyCollection<Question> questions,
        IReadOnlyCollection<CodingChallenge> codingChallenges,
        IReadOnlyCollection<ScenarioChallenge> scenarioChallenges,
        IReadOnlyCollection<TopicProgress> progressEntries,
        IReadOnlyCollection<RevisionSchedule> revisionSchedules,
        IReadOnlyCollection<UserAnswer> recentAnswers,
        DateTime studyDateUtc,
        DateTime generatedAtUtc);
}
