using System.Net;
using System.Net.Http.Json;
using TrainingPlatform.Application.Features.Identity;

namespace TrainingPlatform.IntegrationTests;

public sealed class AuthEndpointsTests(TrainingPlatformApiFactory factory) : IClassFixture<TrainingPlatformApiFactory>
{
    [Fact]
    public async Task Register_returns_profile_and_token()
    {
        var client = factory.CreateClient();

        var auth = await ApiFlows.RegisterAsync(client, "register-happy@example.com");

        Assert.NotEqual(Guid.Empty, auth.UserId);
        Assert.Equal("register-happy@example.com", auth.Email);
        Assert.Equal("Integration Tester", auth.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_conflict()
    {
        var client = factory.CreateClient();
        await ApiFlows.RegisterAsync(client, "dupe@example.com");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "dupe@example.com",
            displayName = "Second Account",
            password = "sup3r-secret!",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_token_for_valid_credentials()
    {
        var client = factory.CreateClient();
        await ApiFlows.RegisterAsync(client, "login-happy@example.com");

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login-happy@example.com",
            password = "sup3r-secret!",
        });

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(ApiFlows.Json);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_unauthorized()
    {
        var client = factory.CreateClient();
        await ApiFlows.RegisterAsync(client, "login-wrong-pass@example.com");

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login-wrong-pass@example.com",
            password = "not-the-password",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_endpoints_reject_missing_token()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me/preferences");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
