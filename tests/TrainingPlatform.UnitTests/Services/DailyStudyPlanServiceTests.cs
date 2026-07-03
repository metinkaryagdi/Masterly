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

    private static Question NewQuestion(Guid topicId, int index = 0, TopicDifficulty difficulty = TopicDifficulty.Intermediate)
        => Question.Create(
            topicId,
            QuestionType.MultipleChoice,
            prompt: $"Q{index}",
            explanation: "explained",
            difficulty,
            estimatedSolvingTimeSeconds: 60,
            minimumPassingScore: 60,
            tags: Array.Empty<string>(),
            acceptedAnswers: Array.Empty<string>(),
            options: new[] { ("A", false), ("B", true) },
            createdAtUtc: Now.AddSeconds(index));

    private static TopicProgress WeakProgress(Guid userId, Guid topicId, int masteryScore = 30)
    {
        var progress = TopicProgress.Create(userId, topicId, Now.AddDays(-10));
        progress.ApplyTheoryAttempt(false, 60, masteryScore, consistencyScore: 0.3d, Now.AddDays(-2));
        return progress;
    }

    private static UserAnswer CorrectAnswer(Guid userId, Guid questionId)
        => UserAnswer.Create(userId, questionId, null, "answer", wasCorrect: true, score: 100, responseTimeSeconds: 30, "ok", Now.AddDays(-1));

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
            recentAnswers: Array.Empty<UserAnswer>(),
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
            recentAnswers: Array.Empty<UserAnswer>(),
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
            recentAnswers: Array.Empty<UserAnswer>(),
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

    [Fact]
    public void Plan_reaches_daily_target_from_pool_even_when_only_one_category_has_topics()
    {
        var user = NewUser(dailyQuestionTarget: 5);
        var topic = NewTopic("Weak Only");
        var questions = Enumerable.Range(0, 10).Select(i => NewQuestion(topic.Id, i)).ToList();
        var service = new DailyStudyPlanService();

        var plan = service.BuildPlan(
            user,
            new[] { topic },
            questions,
            codingChallenges: Array.Empty<CodingChallenge>(),
            scenarioChallenges: Array.Empty<ScenarioChallenge>(),
            progressEntries: new[] { WeakProgress(user.Id, topic.Id) },
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            recentAnswers: Array.Empty<UserAnswer>(),
            studyDateUtc: Now.Date,
            generatedAtUtc: Now);

        // The weak category alone only carries 40% of the target; the top-up
        // pass must fill the rest from the same pool.
        Assert.Equal(5, plan.Items.Count(item => item.ItemType == StudyPlanItemType.Question));
    }

    [Fact]
    public void Plan_skips_questions_answered_correctly_recently_while_fresh_ones_remain()
    {
        var user = NewUser(dailyQuestionTarget: 3);
        var topic = NewTopic("Rotating");
        var questions = Enumerable.Range(0, 6).Select(i => NewQuestion(topic.Id, i)).ToList();
        var answeredCorrectly = questions.Take(3).ToList();
        var recentAnswers = answeredCorrectly.Select(q => CorrectAnswer(user.Id, q.Id)).ToList();
        var service = new DailyStudyPlanService();

        var plan = service.BuildPlan(
            user,
            new[] { topic },
            questions,
            codingChallenges: Array.Empty<CodingChallenge>(),
            scenarioChallenges: Array.Empty<ScenarioChallenge>(),
            progressEntries: new[] { WeakProgress(user.Id, topic.Id) },
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            recentAnswers: recentAnswers,
            studyDateUtc: Now.Date,
            generatedAtUtc: Now);

        var selected = plan.Items.Where(item => item.ItemType == StudyPlanItemType.Question).Select(item => item.ReferenceId).ToHashSet();
        Assert.Equal(3, selected.Count);
        Assert.Empty(selected.Intersect(answeredCorrectly.Select(q => q.Id)));
    }

    [Fact]
    public void Plan_falls_back_to_recently_correct_questions_when_the_pool_runs_dry()
    {
        var user = NewUser(dailyQuestionTarget: 4);
        var topic = NewTopic("Small Pool");
        var questions = Enumerable.Range(0, 4).Select(i => NewQuestion(topic.Id, i)).ToList();
        // Every question was answered correctly yesterday — the plan should
        // still fill rather than come back empty.
        var recentAnswers = questions.Select(q => CorrectAnswer(user.Id, q.Id)).ToList();
        var service = new DailyStudyPlanService();

        var plan = service.BuildPlan(
            user,
            new[] { topic },
            questions,
            codingChallenges: Array.Empty<CodingChallenge>(),
            scenarioChallenges: Array.Empty<ScenarioChallenge>(),
            progressEntries: new[] { WeakProgress(user.Id, topic.Id) },
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            recentAnswers: recentAnswers,
            studyDateUtc: Now.Date,
            generatedAtUtc: Now);

        Assert.Equal(4, plan.Items.Count(item => item.ItemType == StudyPlanItemType.Question));
    }

    [Fact]
    public void Plan_spreads_questions_across_topics_in_the_same_category()
    {
        var user = NewUser(dailyQuestionTarget: 4);
        var first = NewTopic("Weak A");
        var second = NewTopic("Weak B");
        var questions = new[] { first, second }
            .SelectMany(topic => Enumerable.Range(0, 10).Select(i => NewQuestion(topic.Id, i)))
            .ToList();
        var service = new DailyStudyPlanService();

        var plan = service.BuildPlan(
            user,
            new[] { first, second },
            questions,
            codingChallenges: Array.Empty<CodingChallenge>(),
            scenarioChallenges: Array.Empty<ScenarioChallenge>(),
            progressEntries: new[] { WeakProgress(user.Id, first.Id), WeakProgress(user.Id, second.Id) },
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            recentAnswers: Array.Empty<UserAnswer>(),
            studyDateUtc: Now.Date,
            generatedAtUtc: Now);

        var topicsInPlan = plan.Items
            .Where(item => item.ItemType == StudyPlanItemType.Question)
            .Select(item => item.TopicId)
            .Distinct()
            .ToList();

        // Round-robin selection must draw from both weak topics instead of
        // exhausting one topic's pool first.
        Assert.Contains(first.Id, topicsInPlan);
        Assert.Contains(second.Id, topicsInPlan);
    }

    [Fact]
    public void Plan_prefers_the_difficulty_band_matching_topic_mastery()
    {
        var user = NewUser(dailyQuestionTarget: 2);
        var topic = NewTopic("Banded");
        var fundamentals = Enumerable.Range(0, 3).Select(i => NewQuestion(topic.Id, i, TopicDifficulty.Fundamental)).ToList();
        var advanced = Enumerable.Range(3, 3).Select(i => NewQuestion(topic.Id, i, TopicDifficulty.Advanced)).ToList();
        var questions = fundamentals.Concat(advanced).ToList();
        var service = new DailyStudyPlanService();

        var plan = service.BuildPlan(
            user,
            new[] { topic },
            questions,
            codingChallenges: Array.Empty<CodingChallenge>(),
            scenarioChallenges: Array.Empty<ScenarioChallenge>(),
            progressEntries: new[] { WeakProgress(user.Id, topic.Id, masteryScore: 20) },
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            recentAnswers: Array.Empty<UserAnswer>(),
            studyDateUtc: Now.Date,
            generatedAtUtc: Now);

        var selectedIds = plan.Items
            .Where(item => item.ItemType == StudyPlanItemType.Question)
            .Select(item => item.ReferenceId)
            .ToHashSet();

        // Mastery 20 → fundamental band; with enough fundamentals in the pool,
        // no advanced question should be drawn.
        Assert.Equal(2, selectedIds.Count);
        Assert.True(selectedIds.All(id => fundamentals.Any(q => q.Id == id)));
    }

    [Fact]
    public void Plan_selection_is_stable_for_the_same_user_and_day_but_rotates_across_days()
    {
        var user = NewUser(dailyQuestionTarget: 3);
        var topic = NewTopic("Deterministic");
        var questions = Enumerable.Range(0, 12).Select(i => NewQuestion(topic.Id, i)).ToList();
        var progress = new[] { WeakProgress(user.Id, topic.Id) };
        var service = new DailyStudyPlanService();

        DailyStudyPlan Build(DateTime studyDate) => service.BuildPlan(
            user,
            new[] { topic },
            questions,
            codingChallenges: Array.Empty<CodingChallenge>(),
            scenarioChallenges: Array.Empty<ScenarioChallenge>(),
            progressEntries: progress,
            revisionSchedules: Array.Empty<RevisionSchedule>(),
            recentAnswers: Array.Empty<UserAnswer>(),
            studyDateUtc: studyDate,
            generatedAtUtc: Now);

        static List<Guid> QuestionIds(DailyStudyPlan plan) => plan.Items
            .Where(item => item.ItemType == StudyPlanItemType.Question)
            .OrderBy(item => item.Sequence)
            .Select(item => item.ReferenceId)
            .ToList();

        var sameDayFirst = QuestionIds(Build(Now.Date));
        var sameDaySecond = QuestionIds(Build(Now.Date));
        var nextDay = QuestionIds(Build(Now.Date.AddDays(1)));

        Assert.Equal(sameDayFirst, sameDaySecond);
        // With 12 questions and 3 picks, a different day's seed should draw a
        // different set (identical draws would signal the seed is being ignored).
        Assert.NotEqual(sameDayFirst, nextDay);
    }
}
