using TrainingPlatform.Application.Services;
using TrainingPlatform.Domain.Challenges;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Identity;
using TrainingPlatform.Domain.Progress;
using TrainingPlatform.Domain.Questions;
using TrainingPlatform.Domain.Topics;

namespace TrainingPlatform.UnitTests.Services;

public sealed class DailyStudyPlanServiceTests
{
    private static readonly DateTime Now = new(2026, 5, 17, 9, 0, 0, DateTimeKind.Utc);

    private static User NewUser(int dailyQuestionTarget = 10)
    {
        var user = User.Create("test@training.dev", "Test User", "hash", Now);
        user.UpdatePreferences(
            dailyQuestionTarget: dailyQuestionTarget,
            dailyStudyMinutes: 45,
            dailyCodingChallengeTarget: 1,
            dailyScenarioChallengeTarget: 1,
            includeWeekends: true,
            goals: null,
            updatedAtUtc: Now);
        return user;
    }

    private static Topic NewTopic(string name, TopicDifficulty difficulty = TopicDifficulty.Intermediate)
        => Topic.Create(name, name.ToLowerInvariant().Replace(' ', '-'), name, difficulty, 1.1d, Array.Empty<Guid>(), Now);

    private static Question NewQuestion(Guid topicId, int index = 0)
        => Question.Create(
            topicId,
            QuestionType.MultipleChoice,
            prompt: $"Q{index}",
            explanation: "explained",
            TopicDifficulty.Intermediate,
            estimatedSolvingTimeSeconds: 60,
            minimumPassingScore: 60,
            tags: Array.Empty<string>(),
            acceptedAnswers: Array.Empty<string>(),
            options: new[] { ("A", false), ("B", true) },
            createdAtUtc: Now.AddSeconds(index));

    [Fact]
    public void Plan_for_a_fresh_user_starts_empty_when_no_questions_exist()
    {
        var user = NewUser();
        var topics = new[] { NewTopic("A") };
        var service = new DailyStudyPlanService();

        var plan = service.BuildPlan(
            user,
            topics,
            questions: Array.Empty<Question>(),
            codingChallenges: Array.Empty<CodingChallenge>(),
            scenarioChallenges: Array.Empty<ScenarioChallenge>(),
            progressEntries: Array.Empty<TopicProgress>(),
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            studyDateUtc: Now.Date,
            generatedAtUtc: Now);

        Assert.Empty(plan.Items);
        Assert.Equal(user.Id, plan.UserId);
    }

    [Fact]
    public void Plan_appends_one_coding_and_one_scenario_challenge_when_available()
    {
        // A single new topic only fills the "New" bucket (10% of the target),
        // so we don't assert a specific question count here — what we *do* care
        // about is that a coding and a scenario challenge get tacked on at the
        // end exactly once each.
        var user = NewUser(dailyQuestionTarget: 10);
        var topic = NewTopic("C# Foundations", TopicDifficulty.Fundamental);
        var questions = Enumerable.Range(0, 5).Select(i => NewQuestion(topic.Id, i)).ToList();
        var coding = CodingChallenge.Create(topic.Id, "Coding A", "desc", TopicDifficulty.Intermediate, 30,
            new[] { "criterion" }, "starter", "expected", Now);
        var scenario = ScenarioChallenge.Create(topic.Id, "Scenario A", "scenario body", TopicDifficulty.Intermediate, 30,
            new[] { "criterion" }, "reference", Now);
        var service = new DailyStudyPlanService();

        var plan = service.BuildPlan(
            user,
            new[] { topic },
            questions,
            new[] { coding },
            new[] { scenario },
            progressEntries: Array.Empty<TopicProgress>(),
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            studyDateUtc: Now.Date,
            generatedAtUtc: Now);

        Assert.Single(plan.Items, item => item.ItemType == StudyPlanItemType.CodingChallenge);
        Assert.Single(plan.Items, item => item.ItemType == StudyPlanItemType.ScenarioChallenge);
    }

    [Fact]
    public void Plan_distributes_questions_across_categories_when_multiple_topics_have_progress()
    {
        var user = NewUser(dailyQuestionTarget: 8);

        var weakTopic   = NewTopic("Weak Topic");
        var recentTopic = NewTopic("Recent Topic");
        var strongTopic = NewTopic("Strong Topic");
        var newTopic    = NewTopic("New Topic");
        var topics = new[] { weakTopic, recentTopic, strongTopic, newTopic };

        // Two questions per topic so each category has supply.
        var questions = topics.SelectMany(topic =>
            Enumerable.Range(0, 2).Select(i => NewQuestion(topic.Id, i))).ToList();

        // Progress entries shape the category each topic falls into.
        var weakProgress = TopicProgress.Create(user.Id, weakTopic.Id, Now.AddDays(-10));
        weakProgress.ApplyTheoryAttempt(false, 60, masteryScore: 30, consistencyScore: 0.3d, Now.AddDays(-2));

        var recentProgress = TopicProgress.Create(user.Id, recentTopic.Id, Now.AddDays(-10));
        recentProgress.ApplyTheoryAttempt(true, 40, masteryScore: 70, consistencyScore: 0.7d, Now.AddDays(-3));

        var strongProgress = TopicProgress.Create(user.Id, strongTopic.Id, Now.AddDays(-30));
        strongProgress.ApplyTheoryAttempt(true, 30, masteryScore: 90, consistencyScore: 0.9d, Now.AddDays(-30));

        var service = new DailyStudyPlanService();

        var plan = service.BuildPlan(
            user,
            topics,
            questions,
            codingChallenges: Array.Empty<CodingChallenge>(),
            scenarioChallenges: Array.Empty<ScenarioChallenge>(),
            progressEntries: new[] { weakProgress, recentProgress, strongProgress },
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            studyDateUtc: Now.Date,
            generatedAtUtc: Now);

        var questionItems = plan.Items.Where(item => item.ItemType == StudyPlanItemType.Question).ToList();
        Assert.NotEmpty(questionItems);
        // No duplicate questions in a single plan.
        Assert.Equal(questionItems.Count, questionItems.Select(item => item.ReferenceId).Distinct().Count());
        // Sequences should be 1..N with no gaps.
        var sequences = questionItems.Select(item => item.Sequence).OrderBy(value => value).ToList();
        Assert.Equal(Enumerable.Range(1, sequences.Count).ToList(), sequences);
    }
}
