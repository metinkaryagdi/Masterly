using TrainingPlatform.Domain.Challenges;
using TrainingPlatform.Domain.Identity;
using TrainingPlatform.Domain.Progress;
using TrainingPlatform.Domain.Questions;
using TrainingPlatform.Domain.Topics;

namespace TrainingPlatform.Application.Services;

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
        DateTime studyDateUtc,
        DateTime generatedAtUtc);
}
