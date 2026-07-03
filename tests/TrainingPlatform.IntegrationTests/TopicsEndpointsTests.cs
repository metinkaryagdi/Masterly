using System.Net.Http.Json;
using TrainingPlatform.Application.Features.Topics;

namespace TrainingPlatform.IntegrationTests;

public sealed class TopicsEndpointsTests(TrainingPlatformApiFactory factory) : IClassFixture<TrainingPlatformApiFactory>
{
    [Fact]
    public async Task Topics_include_pool_counts_and_launchable_samples()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        var topics = await client.GetFromJsonAsync<List<TopicDto>>("/api/topics", ApiFlows.Json);

        Assert.NotNull(topics);
        Assert.NotEmpty(topics);
        Assert.All(topics, topic =>
        {
            // Every seeded topic carries a pool the catalogue can show.
            Assert.True(topic.QuestionCount >= 8, $"{topic.Slug} should expose its question pool size.");
            Assert.NotEmpty(topic.SampleQuestions);
            Assert.All(topic.SampleQuestions, sample => Assert.False(string.IsNullOrWhiteSpace(sample.Title)));
        });

        // Sample question ids must be real: the catalogue launches practice with them.
        var sample = topics[0].SampleQuestions.First(item => item.Type == "Question");
        var questionResponse = await client.GetAsync($"/api/questions/{sample.Id}");
        questionResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Topic_by_id_returns_the_same_enrichment()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);
        var topics = await client.GetFromJsonAsync<List<TopicDto>>("/api/topics", ApiFlows.Json);
        Assert.NotNull(topics);

        var detail = await client.GetFromJsonAsync<TopicDto>($"/api/topics/{topics[0].Id}", ApiFlows.Json);

        Assert.NotNull(detail);
        Assert.Equal(topics[0].QuestionCount, detail.QuestionCount);
        Assert.Equal(topics[0].SampleQuestions.Count, detail.SampleQuestions.Count);
    }
}
