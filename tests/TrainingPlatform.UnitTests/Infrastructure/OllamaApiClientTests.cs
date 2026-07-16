using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TrainingPlatform.Infrastructure.AI;

namespace TrainingPlatform.UnitTests.Infrastructure;

public sealed class OllamaApiClientTests
{
    /// <summary>Records how many requests reach it and returns a canned model reply.</summary>
    private sealed class StubHandler(string modelReply) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var body = $"{{\"response\": {JsonSerializer.Serialize(modelReply)}}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static OllamaApiClient CreateClient(StubHandler handler, bool enabled = true)
    {
        // Mimics AddHttpClient<OllamaApiClient>(...) configuring the typed client
        // once at registration — BaseAddress/Timeout are set here, never per call.
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        var options = Options.Create(new OllamaOptions
        {
            Enabled = enabled,
            BaseUrl = "http://localhost:11434",
            Model = "gemma3:4b",
            TimeoutSeconds = 30
        });

        return new OllamaApiClient(httpClient, options);
    }

    [Fact]
    public async Task GenerateAsync_can_be_called_repeatedly_on_the_same_client()
    {
        var handler = new StubHandler("model output");
        var client = CreateClient(handler);

        // Reproduces the generation retry loop: before the fix the second call
        // threw InvalidOperationException because GenerateAsync reassigned
        // BaseAddress/Timeout after the first request had already started.
        var first = await client.GenerateAsync("prompt-1", CancellationToken.None);
        var second = await client.GenerateAsync("prompt-2", CancellationToken.None);

        Assert.Equal("model output", first);
        Assert.Equal("model output", second);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_throws_when_integration_is_disabled()
    {
        var handler = new StubHandler("ignored");
        var client = CreateClient(handler, enabled: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GenerateAsync("prompt", CancellationToken.None));

        Assert.Equal(0, handler.CallCount);
    }
}
