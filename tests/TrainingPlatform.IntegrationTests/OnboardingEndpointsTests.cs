using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlatform.Application.Features.Identity;
using TrainingPlatform.Application.Features.Topics;
using TrainingPlatform.Domain.Identity;
using TrainingPlatform.Infrastructure.Persistence;

namespace TrainingPlatform.IntegrationTests;

public sealed class OnboardingEndpointsTests(TrainingPlatformApiFactory factory) : IClassFixture<TrainingPlatformApiFactory>
{
    private static object OnboardingBody(object[] assessments, string[]? goals = null) => new
    {
        dailyQuestionTarget = 8,
        dailyStudyMinutes = 20,
        dailyCodingChallengeTarget = 1,
        dailyScenarioChallengeTarget = 1,
        includeWeekends = true,
        goals = goals ?? ["interview prep", "performance"],
        assessments,
    };

    private async Task<IReadOnlyList<TopicDto>> GetSeededTopicsAsync(HttpClient client)
    {
        var topics = await client.GetFromJsonAsync<List<TopicDto>>("/api/topics", ApiFlows.Json);
        Assert.NotNull(topics);
        Assert.NotEmpty(topics);
        return topics;
    }

    [Fact]
    public async Task CompleteOnboarding_persists_goals_assessments_and_seeds_mastery()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);
        var topics = await GetSeededTopicsAsync(client);
        var novice = topics[0];
        var strong = topics[1];

        var response = await client.PostAsJsonAsync("/api/me/onboarding", OnboardingBody(
        [
            new { topicId = novice.Id, level = "Novice" },
            new { topicId = strong.Id, level = "Strong" },
        ]));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<UserPreferencesDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.Equal(8, dto.DailyQuestionTarget);
        Assert.Equal(20, dto.DailyStudyMinutes);
        Assert.Equal(["interview prep", "performance"], dto.Goals);

        var preferences = await client.GetFromJsonAsync<UserPreferencesDto>("/api/me/preferences", ApiFlows.Json);
        Assert.NotNull(preferences);
        Assert.Equal(["interview prep", "performance"], preferences.Goals);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();

        var assessments = await dbContext.TopicSelfAssessments
            .Where(item => item.TopicId == novice.Id || item.TopicId == strong.Id)
            .ToListAsync();
        Assert.Equal(2, assessments.Count);
        Assert.Equal(SelfAssessmentLevel.Novice, assessments.Single(item => item.TopicId == novice.Id).Level);
        Assert.Equal(SelfAssessmentLevel.Strong, assessments.Single(item => item.TopicId == strong.Id).Level);

        var progress = await dbContext.TopicProgressEntries
            .Where(item => item.TopicId == novice.Id || item.TopicId == strong.Id)
            .ToListAsync();
        Assert.Equal(2, progress.Count);
        Assert.Equal(20, progress.Single(item => item.TopicId == novice.Id).MasteryScore);
        Assert.Equal(70, progress.Single(item => item.TopicId == strong.Id).MasteryScore);
    }

    [Fact]
    public async Task CompleteOnboarding_twice_updates_level_without_duplicating_progress()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);
        var topics = await GetSeededTopicsAsync(client);
        var topic = topics[2];

        var first = await client.PostAsJsonAsync("/api/me/onboarding", OnboardingBody(
            [new { topicId = topic.Id, level = "Novice" }]));
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/me/onboarding", OnboardingBody(
            [new { topicId = topic.Id, level = "Strong" }]));
        second.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();

        var assessments = await dbContext.TopicSelfAssessments
            .Where(item => item.TopicId == topic.Id)
            .ToListAsync();
        var assessment = Assert.Single(assessments);
        Assert.Equal(SelfAssessmentLevel.Strong, assessment.Level);

        // Progress is only seeded when none exists — re-running onboarding must
        // neither duplicate the row nor overwrite mastery earned since.
        var progressEntries = await dbContext.TopicProgressEntries
            .Where(item => item.TopicId == topic.Id)
            .ToListAsync();
        var progress = Assert.Single(progressEntries);
        Assert.Equal(20, progress.MasteryScore);
    }

    [Fact]
    public async Task CompleteOnboarding_with_unknown_topic_returns_not_found()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/me/onboarding", OnboardingBody(
            [new { topicId = Guid.NewGuid(), level = "Novice" }]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteOnboarding_with_duplicate_topic_returns_bad_request()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);
        var topics = await GetSeededTopicsAsync(client);
        var topic = topics[0];

        var response = await client.PostAsJsonAsync("/api/me/onboarding", OnboardingBody(
        [
            new { topicId = topic.Id, level = "Novice" },
            new { topicId = topic.Id, level = "Strong" },
        ]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteOnboarding_without_assessments_still_saves_preferences()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/me/onboarding", OnboardingBody([], goals: ["just browsing"]));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<UserPreferencesDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.Equal(["just browsing"], dto.Goals);
    }

    [Fact]
    public async Task CompleteOnboarding_requires_authentication()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/me/onboarding", OnboardingBody([]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
