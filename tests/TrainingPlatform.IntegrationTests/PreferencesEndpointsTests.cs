using System.Net;
using System.Net.Http.Json;
using TrainingPlatform.Application.Features.Identity;

namespace TrainingPlatform.IntegrationTests;

public sealed class PreferencesEndpointsTests(TrainingPlatformApiFactory factory) : IClassFixture<TrainingPlatformApiFactory>
{
    [Fact]
    public async Task New_user_gets_default_preferences_with_empty_goals()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var preferences = await client.GetFromJsonAsync<UserPreferencesDto>("/api/me/preferences", ApiFlows.Json);

        Assert.NotNull(preferences);
        Assert.Equal(10, preferences.DailyQuestionTarget);
        Assert.Equal(45, preferences.DailyStudyMinutes);
        Assert.True(preferences.IncludeWeekends);
        Assert.Empty(preferences.Goals);
    }

    [Fact]
    public async Task Update_preferences_normalises_and_deduplicates_goals()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/me/preferences", new
        {
            dailyQuestionTarget = 12,
            dailyStudyMinutes = 30,
            dailyCodingChallengeTarget = 2,
            dailyScenarioChallengeTarget = 0,
            includeWeekends = false,
            goals = new[] { "  interview ", "INTERVIEW", "perf" },
        });

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<UserPreferencesDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.Equal(12, dto.DailyQuestionTarget);
        Assert.False(dto.IncludeWeekends);
        Assert.Equal(["interview", "perf"], dto.Goals);
    }

    [Fact]
    public async Task Update_preferences_with_null_goals_keeps_existing_goals()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var seed = await client.PutAsJsonAsync("/api/me/preferences", new
        {
            dailyQuestionTarget = 12,
            dailyStudyMinutes = 30,
            dailyCodingChallengeTarget = 1,
            dailyScenarioChallengeTarget = 1,
            includeWeekends = true,
            goals = new[] { "interview" },
        });
        seed.EnsureSuccessStatusCode();

        var response = await client.PutAsJsonAsync("/api/me/preferences", new
        {
            dailyQuestionTarget = 15,
            dailyStudyMinutes = 40,
            dailyCodingChallengeTarget = 1,
            dailyScenarioChallengeTarget = 1,
            includeWeekends = true,
        });

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<UserPreferencesDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.Equal(15, dto.DailyQuestionTarget);
        Assert.Equal(["interview"], dto.Goals);
    }

    [Fact]
    public async Task Update_preferences_rejects_blank_goal_entries()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/me/preferences", new
        {
            dailyQuestionTarget = 12,
            dailyStudyMinutes = 30,
            dailyCodingChallengeTarget = 1,
            dailyScenarioChallengeTarget = 1,
            includeWeekends = true,
            goals = new[] { "interview", "" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_preferences_rejects_out_of_range_targets()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/me/preferences", new
        {
            dailyQuestionTarget = 0,
            dailyStudyMinutes = 30,
            dailyCodingChallengeTarget = 1,
            dailyScenarioChallengeTarget = 1,
            includeWeekends = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
