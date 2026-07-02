using TrainingPlatform.Domain.Identity;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.UnitTests.Domain;

public sealed class OnboardingDomainTests
{
    private static readonly DateTime Now = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UpdatePreferences_normalises_and_deduplicates_goals()
    {
        var user = User.Create("a@b.com", "Test", "hash", Now);

        user.UpdatePreferences(
            dailyQuestionTarget: 8,
            dailyStudyMinutes: 30,
            dailyCodingChallengeTarget: 1,
            dailyScenarioChallengeTarget: 1,
            includeWeekends: true,
            goals: new[] { "  interview ", "perf", "INTERVIEW", "", "  " },
            updatedAtUtc: Now);

        Assert.Collection(user.Preferences.Goals,
            goal => Assert.Equal("interview", goal),
            goal => Assert.Equal("perf", goal));
    }

    [Fact]
    public void UpdatePreferences_with_null_goals_leaves_existing_goals_untouched()
    {
        var user = User.Create("a@b.com", "Test", "hash", Now);
        user.UpdatePreferences(8, 30, 1, 1, true, new[] { "interview" }, Now);

        user.UpdatePreferences(8, 30, 1, 1, true, goals: null, updatedAtUtc: Now.AddMinutes(1));

        Assert.Single(user.Preferences.Goals, "interview");
    }

    [Theory]
    [InlineData(SelfAssessmentLevel.Novice, 20)]
    [InlineData(SelfAssessmentLevel.Familiar, 45)]
    [InlineData(SelfAssessmentLevel.Strong, 70)]
    public void TopicSelfAssessment_mastery_seed_matches_level(SelfAssessmentLevel level, int expected)
    {
        var assessment = TopicSelfAssessment.Create(Guid.NewGuid(), Guid.NewGuid(), level, Now);

        Assert.Equal(expected, assessment.ToMasterySeed());
    }

    [Fact]
    public void TopicProgress_CreateSeeded_sets_initial_mastery_and_clamps_range()
    {
        var seeded = TopicProgress.CreateSeeded(Guid.NewGuid(), Guid.NewGuid(), masterySeed: 70, Now);
        Assert.Equal(70, seeded.MasteryScore);

        var clampedHigh = TopicProgress.CreateSeeded(Guid.NewGuid(), Guid.NewGuid(), masterySeed: 250, Now);
        Assert.Equal(100, clampedHigh.MasteryScore);

        var clampedLow = TopicProgress.CreateSeeded(Guid.NewGuid(), Guid.NewGuid(), masterySeed: -10, Now);
        Assert.Equal(0, clampedLow.MasteryScore);
    }
}
