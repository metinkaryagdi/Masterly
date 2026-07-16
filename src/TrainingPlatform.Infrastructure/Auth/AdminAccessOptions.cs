namespace TrainingPlatform.Infrastructure.Auth;

/// <summary>
/// Which accounts are granted the "Admin" role. Config-driven (bound from the
/// "Authorization" section) so no schema/migration is needed to designate an
/// administrator: list their email under Authorization:AdminEmails and the JWT
/// issued at login/registration carries the Admin role claim.
/// </summary>
public sealed class AdminAccessOptions
{
    public const string SectionName = "Authorization";

    public string[] AdminEmails { get; init; } = [];

    public bool IsAdmin(string email)
        => !string.IsNullOrWhiteSpace(email)
           && AdminEmails.Any(admin => string.Equals(admin.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
