namespace CodeCraftNet.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "CodeCraftNet";

    public string Audience { get; init; } = "CodeCraftNet.Client";

    public string SecretKey { get; init; } = "dev-secret-key-change-me-dev-secret-key";

    public int ExpiryMinutes { get; init; } = 120;
}
