using System.Net;
using System.Net.Http.Json;

namespace TrainingPlatform.IntegrationTests;

/// <summary>
/// The content-authoring and AI-generation endpoints require the Admin role.
/// A normal authenticated learner (not in Authorization:AdminEmails) must be
/// forbidden, while an anonymous caller is unauthenticated.
/// </summary>
public sealed class AuthorizationEndpointsTests(TrainingPlatformApiFactory factory) : IClassFixture<TrainingPlatformApiFactory>
{
    [Fact]
    public async Task Non_admin_cannot_create_a_coding_challenge()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/challenges/coding", new
        {
            topicId = Guid.NewGuid(),
            title = "Rogue challenge",
            description = "should never be created by a normal user",
            difficulty = "Intermediate",
            estimatedMinutes = 30,
            evaluationCriteria = new[] { "x" },
            starterCode = "",
            expectedOutcome = "",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Non_admin_cannot_trigger_ai_question_generation()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/questions/generate", new
        {
            topicId = Guid.NewGuid(),
            questionType = "ShortAnswer",
            difficulty = "Intermediate",
            tags = new[] { "x" },
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_caller_is_unauthorized_on_admin_endpoints()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/questions/generate", new
        {
            topicId = Guid.NewGuid(),
            questionType = "ShortAnswer",
            difficulty = "Intermediate",
            tags = new[] { "x" },
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
