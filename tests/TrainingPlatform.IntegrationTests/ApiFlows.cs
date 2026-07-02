using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrainingPlatform.Application.Features.Identity;

namespace TrainingPlatform.IntegrationTests;

internal static class ApiFlows
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<AuthResponse> RegisterAsync(HttpClient client, string? email = null)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = email ?? $"user-{Guid.NewGuid():N}@example.com",
            displayName = "Integration Tester",
            password = "sup3r-secret!",
        });

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.NotNull(auth);
        return auth;
    }

    public static async Task<HttpClient> RegisteredClientAsync(TrainingPlatformApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
