using Microsoft.EntityFrameworkCore;
using CodeCraftNet.Domain.Challenges;
using CodeCraftNet.Domain.Identity;
using CodeCraftNet.Domain.Progress;
using CodeCraftNet.Domain.Questions;
using CodeCraftNet.Domain.Topics;

namespace CodeCraftNet.Application.Abstractions.Persistence;

public interface ICodeCraftNetDbContext
{
    DbSet<User> Users { get; }

    DbSet<UserPreference> UserPreferences { get; }

    DbSet<SkillTarget> SkillTargets { get; }

    DbSet<TopicSelfAssessment> TopicSelfAssessments { get; }

    DbSet<Topic> Topics { get; }

    DbSet<TopicDependency> TopicDependencies { get; }

    DbSet<Question> Questions { get; }

    DbSet<QuestionOption> QuestionOptions { get; }

    DbSet<CodingChallenge> CodingChallenges { get; }

    DbSet<ScenarioChallenge> ScenarioChallenges { get; }

    DbSet<UserAnswer> UserAnswers { get; }

    DbSet<CodingSubmission> CodingSubmissions { get; }

    DbSet<ScenarioSubmission> ScenarioSubmissions { get; }

    DbSet<TopicProgress> TopicProgressEntries { get; }

    DbSet<RevisionSchedule> RevisionSchedules { get; }

    DbSet<DailyStudyPlan> DailyStudyPlans { get; }

    DbSet<DailyStudyPlanItem> DailyStudyPlanItems { get; }

    DbSet<MistakeLog> MistakeLogs { get; }

    DbSet<AIInteractionLog> AIInteractionLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
