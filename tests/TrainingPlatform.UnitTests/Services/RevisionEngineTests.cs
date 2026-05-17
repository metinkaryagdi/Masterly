using TrainingPlatform.Application.Common.Models;
using TrainingPlatform.Application.Services;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.UnitTests.Services;

public sealed class RevisionEngineTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TopicId = Guid.NewGuid();
    private static readonly DateTime ReviewedAt = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);

    private static (TopicProgress progress, RevisionSchedule schedule) FreshState()
    {
        var progress = TopicProgress.Create(UserId, TopicId, ReviewedAt.AddDays(-1));
        var schedule = RevisionSchedule.Create(UserId, TopicId, ReviewedAt.AddDays(-1));
        return (progress, schedule);
    }

    private static AnswerEvaluationResult Correct(double speed = 1d, double coverage = 1d) =>
        new(true, 100, "answer", "ok", speed, coverage);

    private static AnswerEvaluationResult Wrong(double speed = 0.3d, double coverage = 0d) =>
        new(false, 0, "answer", "no", speed, coverage);

    [Fact]
    public void Correct_answer_raises_mastery_above_starting_score()
    {
        var (progress, schedule) = FreshState();
        var engine = new RevisionEngine();

        var result = engine.Recalculate(progress, schedule, TopicDifficulty.Intermediate, decayRate: 1.0d, Correct(), ReviewedAt);

        Assert.True(result.MasteryScore > progress.MasteryScore,
            $"expected mastery to grow from {progress.MasteryScore}, got {result.MasteryScore}");
    }

    [Fact]
    public void Wrong_answer_schedules_a_one_day_interval()
    {
        var (progress, schedule) = FreshState();
        var engine = new RevisionEngine();

        var result = engine.Recalculate(progress, schedule, TopicDifficulty.Intermediate, decayRate: 1.0d, Wrong(), ReviewedAt);

        Assert.Equal(1, result.ReviewIntervalDays);
        Assert.Equal(ReviewedAt.Date.AddDays(1), result.NextReviewAtUtc);
    }

    [Fact]
    public void High_mastery_stretches_the_review_interval()
    {
        var (progress, schedule) = FreshState();
        progress.ApplyTheoryAttempt(true, responseTimeSeconds: 30, masteryScore: 95, consistencyScore: 0.9d, ReviewedAt.AddDays(-2));
        var engine = new RevisionEngine();

        var result = engine.Recalculate(progress, schedule, TopicDifficulty.Intermediate, decayRate: 1.0d, Correct(), ReviewedAt);

        Assert.True(result.ReviewIntervalDays >= 14,
            $"expected mastery≥90 to push interval to two weeks or more, got {result.ReviewIntervalDays} days");
    }

    [Fact]
    public void Forgetting_risk_and_priority_stay_within_unit_interval()
    {
        var (progress, schedule) = FreshState();
        var engine = new RevisionEngine();

        var result = engine.Recalculate(progress, schedule, TopicDifficulty.Expert, decayRate: 1.5d, Wrong(speed: 0.1d), ReviewedAt);

        Assert.InRange(result.ForgettingRisk, 0d, 1d);
        Assert.InRange(result.PriorityScore, 0d, 1d);
        Assert.InRange(result.MasteryScore, 0, 100);
    }
}
