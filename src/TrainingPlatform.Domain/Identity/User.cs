using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Identity;

public sealed class User : Entity
{
    private User()
    {
    }

    private User(Guid id, string email, string displayName, string passwordHash, DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Preferences = UserPreference.CreateDefault(id, createdAtUtc);
    }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserPreference Preferences { get; private set; } = null!;

    public List<SkillTarget> SkillTargets { get; private set; } = [];

    public static User Create(string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        return new User(Guid.NewGuid(), email.Trim().ToLowerInvariant(), displayName.Trim(), passwordHash, createdAtUtc);
    }

    public void UpdatePreferences(
        int dailyQuestionTarget,
        int dailyStudyMinutes,
        int dailyCodingChallengeTarget,
        int dailyScenarioChallengeTarget,
        bool includeWeekends,
        IEnumerable<string>? goals,
        DateTime updatedAtUtc)
    {
        Preferences.Update(
            dailyQuestionTarget,
            dailyStudyMinutes,
            dailyCodingChallengeTarget,
            dailyScenarioChallengeTarget,
            includeWeekends,
            goals,
            updatedAtUtc);

        Touch(updatedAtUtc);
    }

    public void ReplaceSkillTargets(IEnumerable<SkillTarget> skillTargets, DateTime updatedAtUtc)
    {
        SkillTargets.Clear();
        SkillTargets.AddRange(skillTargets);
        Touch(updatedAtUtc);
    }
}
