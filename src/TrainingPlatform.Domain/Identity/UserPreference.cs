using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Identity;

public sealed class UserPreference : Entity
{
    private UserPreference()
    {
    }

    private UserPreference(Guid id, Guid userId, DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        DailyQuestionTarget = 10;
        DailyStudyMinutes = 45;
        DailyCodingChallengeTarget = 1;
        DailyScenarioChallengeTarget = 1;
        IncludeWeekends = true;
        Goals = new List<string>();
    }

    public Guid UserId { get; private set; }

    public int DailyQuestionTarget { get; private set; }

    public int DailyStudyMinutes { get; private set; }

    public int DailyCodingChallengeTarget { get; private set; }

    public int DailyScenarioChallengeTarget { get; private set; }

    public bool IncludeWeekends { get; private set; }

    public List<string> Goals { get; private set; } = new();

    public static UserPreference CreateDefault(Guid userId, DateTime createdAtUtc)
    {
        return new UserPreference(Guid.NewGuid(), userId, createdAtUtc);
    }

    public void Update(
        int dailyQuestionTarget,
        int dailyStudyMinutes,
        int dailyCodingChallengeTarget,
        int dailyScenarioChallengeTarget,
        bool includeWeekends,
        IEnumerable<string>? goals,
        DateTime updatedAtUtc)
    {
        DailyQuestionTarget = dailyQuestionTarget;
        DailyStudyMinutes = dailyStudyMinutes;
        DailyCodingChallengeTarget = dailyCodingChallengeTarget;
        DailyScenarioChallengeTarget = dailyScenarioChallengeTarget;
        IncludeWeekends = includeWeekends;

        if (goals is not null)
        {
            Goals = goals
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        Touch(updatedAtUtc);
    }
}
